# Publish API + Blazor UIs to existing aws-asm3-* resources (Windows).
# Requires: aws CLI, started Learner Lab, provision-aws-asm3.ps1 already run.
# Does not print the RDS password.
$ErrorActionPreference = "Stop"
$Region = "us-east-1"
$Prefix = "aws-asm3"
$Root = Split-Path (Split-Path $PSCommandPath)
$Src = Join-Path $Root "src"
$Account = (aws sts get-caller-identity --query Account --output text --region $Region)
if (-not $Account -or $Account -eq "None") {
  throw "AWS credentials are not available. Start Learner Lab and retry."
}

$ReceiptsBucket = "$Prefix-receipts-$Account"
$DeployBucket = "$Prefix-deploy-$Account"
$ApiFn = "$Prefix-api"
$OutboxFn = "$Prefix-outbox-worker"
$DonorApp = "$Prefix-donor-ui"
$DonorEnv = "$Prefix-donor-env"
$AdminApp = "$Prefix-admin-ui"
$AdminEnv = "$Prefix-admin-env"
$Sha = (git -C $Root rev-parse --short HEAD 2>$null)
if (-not $Sha) { $Sha = Get-Date -Format "yyyyMMddHHmmss" }
$Sha = "$Sha-$(Get-Date -Format 'HHmmss')"

$apisJson = aws apigatewayv2 get-apis --output json --region $Region
$apiId = ($apisJson | ConvertFrom-Json).Items | Where-Object { $_.Name -eq "$Prefix-http-api" } | Select-Object -ExpandProperty ApiId -First 1
if (-not $apiId -or $apiId -eq "None") { throw "HTTP API $Prefix-http-api not found. Run provision first." }
$HttpApiUrl = "https://${apiId}.execute-api.${Region}.amazonaws.com"
$RdsEndpoint = aws rds describe-db-instances --db-instance-identifier "$Prefix-postgres" --query DBInstances[0].Endpoint.Address --output text --region $Region
$dbPassPath = Join-Path $env:USERPROFILE ".aws\aws-asm3-db-password.txt"
if (-not (Test-Path $dbPassPath)) { throw "RDS password file not found: $dbPassPath" }
# Password stays in the local file. Lambda gets RDS_* parts; the API builds the URI at runtime.
$dbPass = (Get-Content $dbPassPath -Raw).Trim()

$Work = Join-Path $env:TEMP "aws-asm3-deploy"
if (Test-Path $Work) { Remove-Item $Work -Recurse -Force }
New-Item -ItemType Directory -Force -Path $Work | Out-Null

function Zip-Dir([string]$sourceDir, [string]$zipPath) {
  if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
  $zipPy = @"
import zipfile, pathlib
root = pathlib.Path(r'$sourceDir')
with zipfile.ZipFile(r'$zipPath', 'w', zipfile.ZIP_DEFLATED) as z:
    for p in root.rglob('*'):
        if p.is_file():
            z.write(p, p.relative_to(root).as_posix())
"@
  $zipPy | python -
}

Write-Host "Building solution..."
dotnet restore (Join-Path $Src "NetCore.Donation.slnx") | Out-Null
dotnet build (Join-Path $Src "NetCore.Donation.slnx") -c Release --no-restore

Write-Host "Publishing API (linux-x64)..."
$apiOut = Join-Path $Work "api-publish"
dotnet publish (Join-Path $Src "client\NetCore.Donation.Api\NetCore.Donation.Api.csproj") -c Release -r linux-x64 --self-contained false -o $apiOut
$apiZip = Join-Path $Work "aws-asm3-api.zip"
Zip-Dir $apiOut $apiZip
$apiKey = "lambda/aws-asm3-api-$Sha.zip"
aws s3 cp $apiZip "s3://$DeployBucket/$apiKey" --region $Region | Out-Null
aws lambda update-function-code --function-name $ApiFn --s3-bucket $DeployBucket --s3-key $apiKey --region $Region | Out-Null
aws lambda wait function-updated --function-name $ApiFn --region $Region

$envFile = Join-Path $Work "api-env.json"
$envObj = @{
  Variables = @{
    ASPNETCORE_ENVIRONMENT = "Production"
    Database__Provider = "postgresql"
    Database__MigrationsAssembly = "NetCore.Donation.Infrastructure.Database"
    ObjectStorage__BucketName = $ReceiptsBucket
    ObjectStorage__Region = $Region
    ObjectStorage__ForcePathStyle = "false"
    ObjectStorage__CreateBucketIfNotExists = "false"
    Logging__Path = "/tmp/logs"
    RECEIPTS_BUCKET = $ReceiptsBucket
    RDS_ENDPOINT = $RdsEndpoint
    RDS_USERNAME = "donationadmin"
    RDS_DATABASE = "donation"
    RDS_PASSWORD = $dbPass
    PROJECT = "aws-asm3"
    SERVICE_NAME = "aws-asm3-api"
    DISABLE_OUTBOX_PROCESSOR = "true"
    GIT_SHA = "$Sha"
  }
}
$envObj | ConvertTo-Json -Depth 5 -Compress | Set-Content $envFile -Encoding ascii
try {
  aws lambda update-function-configuration `
    --function-name $ApiFn `
    --runtime dotnet10 `
    --handler NetCore.Donation.Api `
    --timeout 60 `
    --memory-size 1024 `
    --environment "file://$envFile" `
    --region $Region | Out-Null
} finally {
  Remove-Item $envFile -Force -ErrorAction SilentlyContinue
}
aws lambda wait function-updated --function-name $ApiFn --region $Region

Write-Host "Refreshing outbox worker..."
$outboxDir = Join-Path $Work "lambda-outbox"
New-Item -ItemType Directory -Force -Path $outboxDir | Out-Null
@'
import json, os, urllib.error, urllib.request
def handler(event, context):
    base = os.environ.get("HTTP_API_URL", "").rstrip("/")
    if not base:
        return {"statusCode": 500, "body": json.dumps({"error": "HTTP_API_URL is not set"})}
    req = urllib.request.Request(
        base + "/api/v1/outbox-messages/process",
        data=b"{}",
        method="POST",
        headers={"content-type": "application/json"},
    )
    try:
        with urllib.request.urlopen(req, timeout=50) as resp:
            return {"statusCode": resp.status, "body": resp.read().decode("utf-8")}
    except urllib.error.HTTPError as exc:
        return {"statusCode": exc.code, "body": exc.read().decode("utf-8")}
'@ | Set-Content (Join-Path $outboxDir "index.py") -Encoding ascii
$outboxZip = Join-Path $Work "aws-asm3-outbox.zip"
Zip-Dir $outboxDir $outboxZip
aws lambda update-function-code --function-name $OutboxFn --zip-file "fileb://$outboxZip" --region $Region | Out-Null
aws lambda wait function-updated --function-name $OutboxFn --region $Region
aws lambda update-function-configuration `
  --function-name $OutboxFn `
  --timeout 60 `
  --environment "Variables={HTTP_API_URL=$HttpApiUrl,RECEIPTS_BUCKET=$ReceiptsBucket,RDS_ENDPOINT=$RdsEndpoint,GIT_SHA=$Sha,PROJECT=aws-asm3,SERVICE_NAME=aws-asm3-outbox-worker}" `
  --region $Region | Out-Null
$OutboxFnArn = aws lambda get-function --function-name $OutboxFn --query Configuration.FunctionArn --output text --region $Region
$ruleName = "$Prefix-outbox-schedule"
aws events put-rule --name $ruleName --schedule-expression "rate(1 minute)" --state ENABLED --description "Drain aws-asm3 donation outbox" --region $Region | Out-Null
aws lambda add-permission --function-name $OutboxFn --statement-id events-outbox --action lambda:InvokeFunction --principal events.amazonaws.com --source-arn "arn:aws:events:${Region}:${Account}:rule/$ruleName" --region $Region 2>$null | Out-Null
aws events put-targets --rule $ruleName --targets "Id=outbox-worker,Arn=$OutboxFnArn" --region $Region | Out-Null

function Publish-Ui([string]$csproj, [string]$labelPrefix, [string]$app, [string]$envName, [string]$s3Name) {
  Write-Host "Publishing $app..."
  $outDir = Join-Path $Work $s3Name
  dotnet publish $csproj -c Release -r linux-x64 --self-contained false -o $outDir
  $zip = Join-Path $Work "$s3Name.zip"
  Zip-Dir $outDir $zip
  $key = "beanstalk/$s3Name-$Sha.zip"
  aws s3 cp $zip "s3://$DeployBucket/$key" --region $Region | Out-Null
  $label = "$labelPrefix-$Sha"
  aws elasticbeanstalk create-application-version `
    --application-name $app `
    --version-label $label `
    --source-bundle "S3Bucket=$DeployBucket,S3Key=$key" `
    --tags "Key=Project,Value=aws-asm3" `
    --region $Region 2>$null | Out-Null
  aws elasticbeanstalk update-environment `
    --application-name $app `
    --environment-name $envName `
    --version-label $label `
    --option-settings `
      "Namespace=aws:elasticbeanstalk:application:environment,OptionName=ASPNETCORE_ENVIRONMENT,Value=Production" `
      "Namespace=aws:elasticbeanstalk:application:environment,OptionName=ASPNETCORE_URLS,Value=http://127.0.0.1:5000" `
      "Namespace=aws:elasticbeanstalk:application:environment,OptionName=ApiBaseAddress,Value=$HttpApiUrl" `
      "Namespace=aws:elasticbeanstalk:application:environment,OptionName=DonationApi__BaseUrl,Value=$HttpApiUrl" `
      "Namespace=aws:elasticbeanstalk:application:environment,OptionName=ObjectStorage__BucketName,Value=$ReceiptsBucket" `
      "Namespace=aws:elasticbeanstalk:application:environment,OptionName=ObjectStorage__Region,Value=$Region" `
    --region $Region | Out-Null
}

Write-Host "Waiting for Beanstalk environments Ready before deploy..."
aws elasticbeanstalk wait environment-exists --environment-names $DonorEnv --region $Region 2>$null | Out-Null
$donorStatus = aws elasticbeanstalk describe-environments --environment-names $DonorEnv --query "Environments[0].Status" --output text --region $Region
Write-Host "Donor EB status: $donorStatus"

Publish-Ui (Join-Path $Src "client\NetCore.Donation.UI\NetCore.Donation.UI.csproj") "donor" $DonorApp $DonorEnv "aws-asm3-donor"
Publish-Ui (Join-Path $Src "client\NetCore.Donation.Admin\NetCore.Donation.Admin.csproj") "admin" $AdminApp $AdminEnv "aws-asm3-admin"

$donorUrl = aws elasticbeanstalk describe-environments --environment-names $DonorEnv --query "Environments[0].CNAME" --output text --region $Region
$adminUrl = aws elasticbeanstalk describe-environments --environment-names $AdminEnv --query "Environments[0].CNAME" --output text --region $Region
Write-Host "=== aws-asm3 deploy complete ==="
Write-Host "Donor UI:  http://$donorUrl"
Write-Host "Admin UI:  http://$adminUrl"
Write-Host "HTTP API:  $HttpApiUrl"
Write-Host "Receipts:  $ReceiptsBucket"
Write-Host "RDS:       $RdsEndpoint"
