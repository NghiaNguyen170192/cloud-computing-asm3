# Provision aws-asm3-* resources in AWS Academy (us-east-1) and tag Project=aws-asm3.
# Requires: aws CLI, active Learner Lab credentials.
$ErrorActionPreference = "Continue"
$Region = "us-east-1"
$Account = (aws sts get-caller-identity --query Account --output text)
$Prefix = "aws-asm3"
$Tags = "Key=Project,Value=aws-asm3 Key=Assignment,Value=COSC29800-A3"
$VpcId = "vpc-0cd83a2dc1b09d1e4"
$Subnets = @("subnet-0e43067a277531b6d", "subnet-0ebbc28d107f9091a", "subnet-0cd0784673be06ac1")
$subnetCsv = [string]::Join(",", $Subnets)
$LabRoleArn = "arn:aws:iam::${Account}:role/LabRole"
$LabInstanceProfile = "LabInstanceProfile"

function Tag-Name([string]$name) { "Key=Name,Value=$name" }

Write-Host "Account=$Account Region=$Region"

# --- S3 ---
$buckets = @(
  "$Prefix-receipts-$Account",
  "$Prefix-deploy-$Account",
  "$Prefix-analytics-$Account"
)
foreach ($b in $buckets) {
  aws s3api head-bucket --bucket $b 2>$null | Out-Null
  if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating bucket $b"
    aws s3api create-bucket --bucket $b --region $Region | Out-Null
  }
  aws s3api put-bucket-encryption --bucket $b --server-side-encryption-configuration '{\"Rules\":[{\"ApplyServerSideEncryptionByDefault\":{\"SSEAlgorithm\":\"AES256\"}}]}' | Out-Null
  aws s3api put-public-access-block --bucket $b --public-access-block-configuration BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true | Out-Null
  aws s3api put-bucket-tagging --bucket $b --tagging "TagSet=[{Key=Project,Value=aws-asm3},{Key=Name,Value=$b},{Key=Assignment,Value=COSC29800-A3}]" | Out-Null
}

$ReceiptsBucket = "$Prefix-receipts-$Account"
$DeployBucket = "$Prefix-deploy-$Account"
$AnalyticsBucket = "$Prefix-analytics-$Account"

# --- Security groups ---
function Ensure-Sg([string]$name, [string]$desc) {
  $id = aws ec2 describe-security-groups --filters "Name=group-name,Values=$name" "Name=vpc-id,Values=$VpcId" --query "SecurityGroups[0].GroupId" --output text --region $Region 2>$null
  if ($id -and $id -ne "None" -and $LASTEXITCODE -eq 0) { return $id }
  Write-Host "Creating SG $name"
  $id = aws ec2 create-security-group --group-name $name --description $desc --vpc-id $VpcId --region $Region --query GroupId --output text
  aws ec2 create-tags --resources $id --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=$name" "Key=Assignment,Value=COSC29800-A3" --region $Region | Out-Null
  return $id
}

$RdsSg = Ensure-Sg "$Prefix-rds-sg" "aws-asm3 RDS PostgreSQL"
$EbSg = Ensure-Sg "$Prefix-eb-sg" "aws-asm3 Elastic Beanstalk"

# Ingress (ignore if exists)
aws ec2 authorize-security-group-ingress --group-id $RdsSg --protocol tcp --port 5432 --cidr 0.0.0.0/0 --region $Region 2>$null | Out-Null
aws ec2 authorize-security-group-ingress --group-id $EbSg --protocol tcp --port 80 --cidr 0.0.0.0/0 --region $Region 2>$null | Out-Null
aws ec2 authorize-security-group-ingress --group-id $EbSg --protocol tcp --port 443 --cidr 0.0.0.0/0 --region $Region 2>$null | Out-Null

# --- RDS ---
$dbPassPath = Join-Path $env:USERPROFILE ".aws\aws-asm3-db-password.txt"
if (-not (Test-Path $dbPassPath)) {
  $bytes = New-Object byte[] 16
  [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
  $dbPass = ([Convert]::ToBase64String($bytes) -replace '[/+=]', 'x') + "Aa1!"
  Set-Content $dbPassPath -Value $dbPass -NoNewline
} else {
  $dbPass = Get-Content $dbPassPath -Raw
}

$subnetGroup = "$Prefix-db-subnets"
aws rds describe-db-subnet-groups --db-subnet-group-name $subnetGroup --region $Region 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
  Write-Host "Creating DB subnet group"
  aws rds create-db-subnet-group `
    --db-subnet-group-name $subnetGroup `
    --db-subnet-group-description "aws-asm3 donation" `
    --subnet-ids $Subnets `
    --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=$subnetGroup" "Key=Assignment,Value=COSC29800-A3" `
    --region $Region | Out-Null
}

$dbId = "$Prefix-postgres"
$dbState = aws rds describe-db-instances --db-instance-identifier $dbId --query "DBInstances[0].DBInstanceStatus" --output text --region $Region 2>$null
if ($LASTEXITCODE -ne 0 -or -not $dbState -or $dbState -eq "None") {
  Write-Host "Creating RDS $dbId (10-15 min)..."
  aws rds create-db-instance `
    --db-instance-identifier $dbId `
    --db-instance-class db.t3.micro `
    --engine postgres `
    --engine-version 16.15 `
    --master-username donationadmin `
    --master-user-password $dbPass `
    --allocated-storage 20 `
    --storage-type gp2 `
    --db-name donation `
    --vpc-security-group-ids $RdsSg `
    --db-subnet-group-name $subnetGroup `
    --publicly-accessible `
    --enable-cloudwatch-logs-exports postgresql `
    --backup-retention-period 1 `
    --no-deletion-protection `
    --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=$dbId" "Key=Assignment,Value=COSC29800-A3" `
    --region $Region | Out-Null
} else {
  Write-Host "RDS already exists ($dbState)"
}

aws rds create-db-parameter-group --db-parameter-group-name "$Prefix-postgres16" --db-parameter-group-family postgres16 --description "aws-asm3 postgres CloudWatch logging" --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=$Prefix-postgres16" --region $Region 2>$null | Out-Null
aws rds modify-db-parameter-group --db-parameter-group-name "$Prefix-postgres16" --parameters "ParameterName=log_connections,ParameterValue=1,ApplyMethod=immediate" "ParameterName=log_disconnections,ParameterValue=1,ApplyMethod=immediate" --region $Region 2>$null | Out-Null
aws rds modify-db-instance --db-instance-identifier $dbId --db-parameter-group-name "$Prefix-postgres16" --cloudwatch-logs-export-configuration EnableLogTypes=postgresql --apply-immediately --region $Region 2>$null | Out-Null

# Do not store the RDS password in SSM or repo outputs. Keep it only in
# %USERPROFILE%\.aws\aws-asm3-db-password.txt (never commit that file).

# --- Lambda placeholder zip ---
$lambdaDir = Join-Path $env:TEMP "aws-asm3-lambda"
New-Item -ItemType Directory -Force -Path $lambdaDir | Out-Null
@'
import json, os
def handler(event, context):
    return {
        "statusCode": 200,
        "headers": {"content-type": "application/json"},
        "body": json.dumps({
            "service": os.environ.get("SERVICE_NAME", "aws-asm3"),
            "status": "ok",
            "receiptsBucket": os.environ.get("RECEIPTS_BUCKET"),
        }),
    }
'@ | Set-Content (Join-Path $lambdaDir "index.py") -Encoding ascii
$zipPath = Join-Path $env:TEMP "aws-asm3-lambda.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $lambdaDir "*") -DestinationPath $zipPath -Force

function Ensure-Lambda([string]$name, [string]$serviceName) {
  aws lambda get-function --function-name $name --region $Region 2>$null | Out-Null
  if ($LASTEXITCODE -eq 0) {
    Write-Host "Updating Lambda $name"
    aws lambda update-function-code --function-name $name --zip-file "fileb://$zipPath" --region $Region | Out-Null
  } else {
    Write-Host "Creating Lambda $name"
    aws lambda create-function `
      --function-name $name `
      --runtime python3.12 `
      --role $LabRoleArn `
      --handler index.handler `
      --zip-file "fileb://$zipPath" `
      --timeout 30 `
      --memory-size 512 `
      --environment "Variables={RECEIPTS_BUCKET=$ReceiptsBucket,SERVICE_NAME=$serviceName,PROJECT=aws-asm3}" `
      --tags "Project=aws-asm3,Name=$name,Assignment=COSC29800-A3" `
      --region $Region | Out-Null
  }
}

Ensure-Lambda "$Prefix-api" "aws-asm3-api"
Ensure-Lambda "$Prefix-outbox-worker" "aws-asm3-outbox-worker"
$ApiFnArn = aws lambda get-function --function-name "$Prefix-api" --query Configuration.FunctionArn --output text --region $Region

# --- HTTP API ---
$apiId = aws apigatewayv2 get-apis --query "Items[?Name=='$Prefix-http-api'].ApiId" --output text --region $Region
if (-not $apiId -or $apiId -eq "None") {
  Write-Host "Creating HTTP API"
  $apiId = aws apigatewayv2 create-api `
    --name "$Prefix-http-api" `
    --protocol-type HTTP `
    --cors-configuration AllowOrigins="*",AllowMethods="GET,POST,PUT,PATCH,DELETE,OPTIONS",AllowHeaders="*" `
    --tags "Project=aws-asm3,Name=$Prefix-http-api,Assignment=COSC29800-A3" `
    --region $Region `
    --query ApiId --output text
}
$integId = aws apigatewayv2 get-integrations --api-id $apiId --query "Items[0].IntegrationId" --output text --region $Region 2>$null
if (-not $integId -or $integId -eq "None") {
  $integId = aws apigatewayv2 create-integration `
    --api-id $apiId `
    --integration-type AWS_PROXY `
    --integration-uri $ApiFnArn `
    --payload-format-version 2.0 `
    --region $Region `
    --query IntegrationId --output text
}
$routeId = aws apigatewayv2 get-routes --api-id $apiId --query "Items[?RouteKey=='`$default'].RouteId" --output text --region $Region 2>$null
if (-not $routeId -or $routeId -eq "None") {
  aws apigatewayv2 create-route --api-id $apiId --route-key '$default' --target "integrations/$integId" --region $Region | Out-Null
}
$stage = aws apigatewayv2 get-stages --api-id $apiId --query "Items[?StageName=='`$default'].StageName" --output text --region $Region 2>$null
if (-not $stage -or $stage -eq "None") {
  aws apigatewayv2 create-stage --api-id $apiId --stage-name '$default' --auto-deploy --tags "Project=aws-asm3,Name=$Prefix-http-api-stage" --region $Region | Out-Null
}
aws lambda add-permission `
  --function-name "$Prefix-api" `
  --statement-id apigw-invoke `
  --action lambda:InvokeFunction `
  --principal apigateway.amazonaws.com `
  --source-arn "arn:aws:execute-api:${Region}:${Account}:${apiId}/*" `
  --region $Region 2>$null | Out-Null

$HttpApiUrl = "https://${apiId}.execute-api.${Region}.amazonaws.com"

# --- Outbox worker (scheduled drain; request Lambda does not poll) ---
$outboxDir = Join-Path $env:TEMP "aws-asm3-outbox-worker"
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
$outboxZip = Join-Path $env:TEMP "aws-asm3-outbox-worker.zip"
if (Test-Path $outboxZip) { Remove-Item $outboxZip -Force }
python -c "import zipfile,pathlib; z=zipfile.ZipFile(r'$outboxZip','w'); p=pathlib.Path(r'$outboxDir')/'index.py'; z.write(p,'index.py'); z.close()"
aws lambda update-function-code --function-name "$Prefix-outbox-worker" --zip-file "fileb://$outboxZip" --region $Region | Out-Null
aws lambda wait function-updated --function-name "$Prefix-outbox-worker" --region $Region
aws lambda update-function-configuration `
  --function-name "$Prefix-outbox-worker" `
  --timeout 60 `
  --environment "Variables={HTTP_API_URL=$HttpApiUrl,RECEIPTS_BUCKET=$ReceiptsBucket,SERVICE_NAME=aws-asm3-outbox-worker,PROJECT=aws-asm3}" `
  --region $Region | Out-Null
$OutboxFnArn = aws lambda get-function --function-name "$Prefix-outbox-worker" --query Configuration.FunctionArn --output text --region $Region
$ruleName = "$Prefix-outbox-schedule"
aws events put-rule `
  --name $ruleName `
  --schedule-expression "rate(1 minute)" `
  --state ENABLED `
  --description "Drain aws-asm3 donation outbox" `
  --region $Region | Out-Null
aws events tag-resource --resource-arn "arn:aws:events:${Region}:${Account}:rule/$ruleName" --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=$ruleName" "Key=Assignment,Value=COSC29800-A3" --region $Region 2>$null | Out-Null
aws lambda add-permission `
  --function-name "$Prefix-outbox-worker" `
  --statement-id events-outbox `
  --action lambda:InvokeFunction `
  --principal events.amazonaws.com `
  --source-arn "arn:aws:events:${Region}:${Account}:rule/$ruleName" `
  --region $Region 2>$null | Out-Null
aws events put-targets --rule $ruleName --targets "Id=outbox-worker,Arn=$OutboxFnArn" --region $Region | Out-Null

$apiLogGroup = "/aws/apigateway/$Prefix-http-api"
aws logs create-log-group --log-group-name $apiLogGroup --region $Region 2>$null | Out-Null
aws logs put-retention-policy --log-group-name $apiLogGroup --retention-in-days 7 --region $Region 2>$null | Out-Null
$apiLogJson = Join-Path (Split-Path $PSCommandPath) "apigw-access-logs.json"
$accessLog = [ordered]@{
  ApiId = $apiId
  StageName = '$default'
  AccessLogSettings = [ordered]@{
    DestinationArn = "arn:aws:logs:${Region}:${Account}:log-group:$apiLogGroup"
    Format = '{"requestId":"$context.requestId","method":"$context.httpMethod","path":"$context.path","status":"$context.status","integrationError":"$context.integrationErrorMessage"}'
  }
}
$accessLog | ConvertTo-Json -Depth 5 | Set-Content $apiLogJson -Encoding utf8
aws apigatewayv2 update-stage --cli-input-json "file://$apiLogJson" --region $Region 2>$null | Out-Null

# --- Elastic Beanstalk (separate donor + admin) ---
$donorApp = "$Prefix-donor-ui"
$donorEnv = "$Prefix-donor-env"
$adminApp = "$Prefix-admin-ui"
$adminEnv = "$Prefix-admin-env"

$sampleKey = "beanstalk/sample-app.zip"
$sampleDir = Join-Path $env:TEMP "aws-asm3-eb-sample"
New-Item -ItemType Directory -Force -Path $sampleDir | Out-Null
"OK aws-asm3" | Set-Content (Join-Path $sampleDir "health.html") -Encoding ascii
$sampleZip = Join-Path $env:TEMP "aws-asm3-eb-sample.zip"
if (Test-Path $sampleZip) { Remove-Item $sampleZip -Force }
# Prefer python zip with forward slashes (Windows Compress-Archive breaks EB unzip)
python -c "import zipfile,pathlib; z=zipfile.ZipFile(r'$sampleZip', 'w'); p=pathlib.Path(r'$sampleDir')/'health.html'; z.write(p, 'health.html'); z.close()"
aws s3 cp $sampleZip "s3://$DeployBucket/$sampleKey" --region $Region | Out-Null

foreach ($pair in @(
  @{ App = $donorApp; Env = $donorEnv; Desc = "Hope and Help donor Blazor UI" },
  @{ App = $adminApp; Env = $adminEnv; Desc = "Donation admin Blazor UI" }
)) {
  $apps = aws elasticbeanstalk describe-applications --application-names $pair.App --query "Applications[0].ApplicationName" --output text --region $Region 2>$null
  if (-not $apps -or $apps -eq "None") {
    Write-Host "Creating EB application $($pair.App)"
    aws elasticbeanstalk create-application --application-name $pair.App --description $pair.Desc --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=$($pair.App)" --region $Region | Out-Null
  }

  aws elasticbeanstalk create-application-version `
    --application-name $pair.App `
    --version-label "bootstrap-1" `
    --source-bundle "S3Bucket=$DeployBucket,S3Key=$sampleKey" `
    --region $Region 2>$null | Out-Null

  $envs = aws elasticbeanstalk describe-environments --application-name $pair.App --environment-names $pair.Env --query Environments[0].Status --output text --region $Region 2>$null
  if (-not $envs -or $envs -eq "None" -or $envs -eq "Terminated") {
    Write-Host "Creating EB environment $($pair.Env) (5-10 min)..."
    $optFile = Join-Path $env:TEMP "aws-asm3-eb-$($pair.Env).json"
    @(
      @{ Namespace = "aws:autoscaling:launchconfiguration"; OptionName = "IamInstanceProfile"; Value = $LabInstanceProfile }
      @{ Namespace = "aws:autoscaling:launchconfiguration"; OptionName = "InstanceType"; Value = "t3.small" }
      @{ Namespace = "aws:autoscaling:launchconfiguration"; OptionName = "SecurityGroups"; Value = $EbSg }
      @{ Namespace = "aws:elasticbeanstalk:environment"; OptionName = "EnvironmentType"; Value = "SingleInstance" }
      @{ Namespace = "aws:elasticbeanstalk:environment"; OptionName = "ServiceRole"; Value = "LabRole" }
      @{ Namespace = "aws:ec2:vpc"; OptionName = "VPCId"; Value = $VpcId }
      @{ Namespace = "aws:ec2:vpc"; OptionName = "Subnets"; Value = $subnetCsv }
      @{ Namespace = "aws:elasticbeanstalk:application:environment"; OptionName = "ASPNETCORE_ENVIRONMENT"; Value = "Production" }
      @{ Namespace = "aws:elasticbeanstalk:application:environment"; OptionName = "ASPNETCORE_URLS"; Value = "http://127.0.0.1:5000" }
      @{ Namespace = "aws:elasticbeanstalk:application:environment"; OptionName = "PROJECT"; Value = "aws-asm3" }
      @{ Namespace = "aws:elasticbeanstalk:application:environment"; OptionName = "ObjectStorage__BucketName"; Value = $ReceiptsBucket }
      @{ Namespace = "aws:elasticbeanstalk:application:environment"; OptionName = "DonationApi__BaseUrl"; Value = $HttpApiUrl }
      @{ Namespace = "aws:elasticbeanstalk:application:environment"; OptionName = "ApiBaseAddress"; Value = $HttpApiUrl }
      @{ Namespace = "aws:elasticbeanstalk:cloudwatch:logs"; OptionName = "StreamLogs"; Value = "true" }
      @{ Namespace = "aws:elasticbeanstalk:cloudwatch:logs"; OptionName = "RetentionInDays"; Value = "7" }
      @{ Namespace = "aws:elasticbeanstalk:cloudwatch:logs:health"; OptionName = "HealthStreamingEnabled"; Value = "true" }
      @{ Namespace = "aws:elasticbeanstalk:cloudwatch:logs:health"; OptionName = "RetentionInDays"; Value = "7" }
    ) | ConvertTo-Json | ForEach-Object { [System.IO.File]::WriteAllText($optFile, $_, (New-Object System.Text.UTF8Encoding $false)) }
    aws elasticbeanstalk create-environment `
      --application-name $pair.App `
      --environment-name $pair.Env `
      --solution-stack-name "64bit Amazon Linux 2023 v3.11.6 running .NET 10" `
      --version-label "bootstrap-1" `
      --option-settings "file://$optFile" `
      --tags "Key=Project,Value=aws-asm3" "Key=Assignment,Value=COSC29800-A3" `
      --region $Region | Out-Null
  } else {
    Write-Host "EB env $($pair.Env) status: $envs"
  }
}

# --- Athena + Glue ---
aws athena create-work-group `
  --name "$Prefix-analytics" `
  --configuration "ResultConfiguration={OutputLocation=s3://$AnalyticsBucket/athena-results/},EnforceWorkGroupConfiguration=true" `
  --description "aws-asm3 donation analytics" `
  --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=$Prefix-analytics" `
  --region $Region 2>$null | Out-Null

$glueDb = "aws_asm3_donation"
aws glue create-database --database-input "Name=$glueDb,Description=aws-asm3 Athena catalog" --region $Region 2>$null | Out-Null

# --- Resource Group ---
$queryJson = Join-Path (Split-Path $PSCommandPath) "resource-group.json"
aws resource-groups create-group --cli-input-json "file://$queryJson" --region $Region 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
  aws resource-groups update-group --group-name "donation-asm3-rg" --resource-query "file://$queryJson" --region $Region 2>$null | Out-Null
}

# --- Outputs file ---
$out = [ordered]@{
  account           = $Account
  region            = $Region
  projectTag        = "aws-asm3"
  resourceGroup     = "donation-asm3-rg"
  receiptsBucket    = $ReceiptsBucket
  deployBucket      = $DeployBucket
  analyticsBucket   = $AnalyticsBucket
  httpApiUrl        = $HttpApiUrl
  apiFunction       = "$Prefix-api"
  outboxFunction    = "$Prefix-outbox-worker"
  ebDonorApplication = $donorApp
  ebDonorEnvironment = $donorEnv
  ebAdminApplication = $adminApp
  ebAdminEnvironment = $adminEnv
  rdsInstanceId     = $dbId
  glueDatabase      = $glueDb
  athenaWorkGroup   = "$Prefix-analytics"
  cloudWatch = @{
    apiLambda     = "/aws/lambda/$Prefix-api"
    apiGateway    = "/aws/apigateway/$Prefix-http-api"
    donorUi       = "/aws/elasticbeanstalk/$donorEnv/var/log/web.stdout.log"
    adminUi       = "/aws/elasticbeanstalk/$adminEnv/var/log/web.stdout.log"
    postgres      = "/aws/rds/instance/$dbId/postgresql"
  }
}
$outPath = Join-Path (Split-Path $PSCommandPath) "aws-asm3-outputs.json"
$out | ConvertTo-Json | Set-Content $outPath -Encoding utf8
Write-Host ""
Write-Host "=== aws-asm3 provision submitted ==="
$out.GetEnumerator() | ForEach-Object { Write-Host ("{0}: {1}" -f $_.Key, $_.Value) }
Write-Host "Waiting for RDS available..."
aws rds wait db-instance-available --db-instance-identifier $dbId --region $Region
$endpoint = aws rds describe-db-instances --db-instance-identifier $dbId --query "DBInstances[0].Endpoint.Address" --output text --region $Region
Write-Host "RDS endpoint: $endpoint"
$out.rdsEndpoint = $endpoint
$out | ConvertTo-Json | Set-Content $outPath -Encoding utf8
Write-Host "Wrote $outPath"
aws resource-groups list-group-resources --group-name "donation-asm3-rg" --region $Region --output table
