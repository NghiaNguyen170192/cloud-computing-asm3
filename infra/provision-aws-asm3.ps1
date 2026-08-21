# Provision aws-asm3-* resources in AWS Academy (us-east-1) and tag Project=aws-asm3.
# Requires: aws CLI, active Learner Lab credentials.
$ErrorActionPreference = "Continue"
$Region = "us-east-1"
$Account = (aws sts get-caller-identity --query Account --output text)
$Prefix = "aws-asm3"
$Tags = "Key=Project,Value=aws-asm3 Key=Assignment,Value=COSC29800-A3"
$VpcId = "vpc-0cd83a2dc1b09d1e4"
$Subnets = @("subnet-0e43067a277531b6d", "subnet-0ebbc28d107f9091a", "subnet-0cd0784673be06ac1")
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
    --backup-retention-period 1 `
    --no-deletion-protection `
    --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=$dbId" "Key=Assignment,Value=COSC29800-A3" `
    --region $Region | Out-Null
} else {
  Write-Host "RDS already exists ($dbState)"
}

aws ssm put-parameter --name "/cosc29800/asm3/rds/master-password" --type String --value $dbPass --overwrite --region $Region 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
  aws ssm add-tags-to-resource --resource-type Parameter --resource-id "/cosc29800/asm3/rds/master-password" --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=aws-asm3-rds-password" --region $Region 2>$null | Out-Null
} else {
  Write-Host "SSM put-parameter skipped (password file still at $dbPassPath)"
}

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

# --- Elastic Beanstalk ---
$ebApp = "$Prefix-donation-ui"
$ebEnv = "$Prefix-ui-env"
$apps = aws elasticbeanstalk describe-applications --application-names $ebApp --query "Applications[0].ApplicationName" --output text --region $Region 2>$null
if (-not $apps -or $apps -eq "None") {
  Write-Host "Creating EB application"
  aws elasticbeanstalk create-application --application-name $ebApp --description "aws-asm3 Blazor UI" --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=$ebApp" --region $Region | Out-Null
}

# Sample app version so env can launch
$sampleKey = "beanstalk/sample-app.zip"
# minimal zip: Procfile-less placeholder - use AWS sample by creating empty web
$sampleDir = Join-Path $env:TEMP "aws-asm3-eb-sample"
New-Item -ItemType Directory -Force -Path $sampleDir | Out-Null
"OK aws-asm3" | Set-Content (Join-Path $sampleDir "health.html") -Encoding ascii
$sampleZip = Join-Path $env:TEMP "aws-asm3-eb-sample.zip"
if (Test-Path $sampleZip) { Remove-Item $sampleZip -Force }
Compress-Archive -Path (Join-Path $sampleDir "*") -DestinationPath $sampleZip -Force
aws s3 cp $sampleZip "s3://$DeployBucket/$sampleKey" --region $Region | Out-Null
aws elasticbeanstalk create-application-version `
  --application-name $ebApp `
  --version-label "bootstrap-1" `
  --source-bundle "S3Bucket=$DeployBucket,S3Key=$sampleKey" `
  --region $Region 2>$null | Out-Null

$envs = aws elasticbeanstalk describe-environments --application-name $ebApp --environment-names $ebEnv --query "Environments[0].Status" --output text --region $Region 2>$null
if (-not $envs -or $envs -eq "None") {
  Write-Host "Creating EB environment $ebEnv (5-10 min)..."
  aws elasticbeanstalk create-environment `
    --application-name $ebApp `
    --environment-name $ebEnv `
    --solution-stack-name "64bit Amazon Linux 2023 v3.11.6 running .NET 10" `
    --version-label "bootstrap-1" `
    --option-settings `
      "Namespace=aws:autoscaling:launchconfiguration,OptionName=IamInstanceProfile,Value=$LabInstanceProfile" `
      "Namespace=aws:autoscaling:launchconfiguration,OptionName=InstanceType,Value=t3.small" `
      "Namespace=aws:autoscaling:launchconfiguration,OptionName=SecurityGroups,Value=$EbSg" `
      "Namespace=aws:elasticbeanstalk:environment,OptionName=EnvironmentType,Value=SingleInstance" `
      "Namespace=aws:elasticbeanstalk:environment,OptionName=ServiceRole,Value=LabRole" `
      "Namespace=aws:ec2:vpc,OptionName=VPCId,Value=$VpcId" `
      "Namespace=aws:ec2:vpc,OptionName=Subnets,Value=$([string]::Join(',', $Subnets))" `
      "Namespace=aws:elasticbeanstalk:application:environment,OptionName=PROJECT,Value=aws-asm3" `
      "Namespace=aws:elasticbeanstalk:application:environment,OptionName=ObjectStorage__BucketName,Value=$ReceiptsBucket" `
      "Namespace=aws:elasticbeanstalk:application:environment,OptionName=DonationApi__BaseUrl,Value=$HttpApiUrl" `
    --tags "Key=Project,Value=aws-asm3" "Key=Name,Value=$ebEnv" "Key=Assignment,Value=COSC29800-A3" `
    --region $Region | Out-Null
} else {
  Write-Host "EB env status: $envs"
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
  ebApplication     = $ebApp
  ebEnvironment     = $ebEnv
  rdsInstanceId     = $dbId
  glueDatabase      = $glueDb
  athenaWorkGroup   = "$Prefix-analytics"
  dbPasswordFile    = $dbPassPath
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
aws resource-groups list-group-resources --group-name "$Prefix-donation" --region $Region --output table
