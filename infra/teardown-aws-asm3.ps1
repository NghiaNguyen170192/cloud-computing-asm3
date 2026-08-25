# Tear down every aws-asm3-* resource in AWS Academy (us-east-1).
# Requires: aws CLI, started Learner Lab. Does not print the RDS password.
$ErrorActionPreference = "Continue"
$Region = "us-east-1"
$Prefix = "aws-asm3"
$Account = (aws sts get-caller-identity --query Account --output text --region $Region)
if (-not $Account -or $Account -eq "None") {
  throw "AWS credentials are not available. Start Learner Lab and retry."
}
Write-Host "Account=$Account Region=$Region tearing down $Prefix"

# --- Elastic Beanstalk ---
foreach ($envName in @("$Prefix-donor-env", "$Prefix-admin-env")) {
  $status = aws elasticbeanstalk describe-environments --environment-names $envName --query Environments[0].Status --output text --region $Region 2>$null
  if ($status -and $status -ne "None" -and $status -ne "Terminated") {
    Write-Host "Terminating EB environment $envName ($status)"
    aws elasticbeanstalk terminate-environment --environment-name $envName --terminate-resources --region $Region | Out-Null
  } else {
    Write-Host "EB environment $envName already gone"
  }
}

foreach ($envName in @("$Prefix-donor-env", "$Prefix-admin-env")) {
  Write-Host "Waiting for $envName terminated..."
  aws elasticbeanstalk wait environment-terminated --environment-name $envName --region $Region 2>$null
}

foreach ($appName in @("$Prefix-donor-ui", "$Prefix-admin-ui")) {
  Write-Host "Deleting EB application $appName"
  aws elasticbeanstalk delete-application --application-name $appName --terminate-env-by-force --region $Region 2>$null | Out-Null
}

# --- API Gateway ---
$httpApiName = "$Prefix-http-api"
$apisJson = aws apigatewayv2 get-apis --output json --region $Region
$apis = $apisJson | ConvertFrom-Json
foreach ($api in @($apis.Items)) {
  if ($api.Name -eq $httpApiName) {
    Write-Host "Deleting HTTP API $($api.ApiId)"
    aws apigatewayv2 delete-api --api-id $api.ApiId --region $Region | Out-Null
  }
}

# --- Lambda ---
foreach ($fn in @("$Prefix-api", "$Prefix-outbox-worker")) {
  Write-Host "Deleting Lambda $fn"
  aws lambda delete-function --function-name $fn --region $Region 2>$null | Out-Null
}

# --- RDS ---
$dbId = "$Prefix-postgres"
$dbState = aws rds describe-db-instances --db-instance-identifier $dbId --query DBInstances[0].DBInstanceStatus --output text --region $Region 2>$null
if ($LASTEXITCODE -eq 0 -and $dbState -and $dbState -ne "None") {
  Write-Host "Deleting RDS $dbId ($dbState)"
  aws rds delete-db-instance --db-instance-identifier $dbId --skip-final-snapshot --delete-automated-backups --region $Region | Out-Null
  Write-Host "Waiting for RDS deleted (about 15 minutes)..."
  aws rds wait db-instance-deleted --db-instance-identifier $dbId --region $Region
} else {
  Write-Host "RDS $dbId already gone"
}

aws rds delete-db-subnet-group --db-subnet-group-name "$Prefix-db-subnets" --region $Region 2>$null | Out-Null
aws rds delete-db-parameter-group --db-parameter-group-name "$Prefix-postgres16" --region $Region 2>$null | Out-Null

# --- S3 ---
$buckets = @(
  "$Prefix-receipts-$Account",
  "$Prefix-deploy-$Account",
  "$Prefix-analytics-$Account"
)
foreach ($b in $buckets) {
  Write-Host "Emptying and deleting bucket $b"
  aws s3 rm "s3://$b" --recursive --region $Region 2>$null | Out-Null
  aws s3api delete-bucket --bucket $b --region $Region 2>$null | Out-Null
}

# --- Glue / Athena ---
aws glue delete-database --name "aws_asm3_donation" --region $Region 2>$null | Out-Null
aws athena delete-work-group --work-group "$Prefix-analytics" --recursive-delete-option --region $Region 2>$null | Out-Null

# --- CloudWatch log groups ---
$logGroups = @(
  "/aws/lambda/$Prefix-api",
  "/aws/lambda/$Prefix-outbox-worker",
  "/aws/apigateway/$Prefix-http-api",
  "/aws/rds/instance/$Prefix-postgres/postgresql"
)
$ebLogs = aws logs describe-log-groups --log-group-name-prefix "/aws/elasticbeanstalk/$Prefix-" --query logGroups[].logGroupName --output text --region $Region 2>$null
if ($ebLogs -and $ebLogs -ne "None") {
  $logGroups += ($ebLogs -split '\s+')
}
foreach ($lg in ($logGroups | Select-Object -Unique)) {
  if ($lg) {
    Write-Host "Deleting log group $lg"
    aws logs delete-log-group --log-group-name $lg --region $Region 2>$null | Out-Null
  }
}

# --- Security groups (after ENIs are gone) ---
Start-Sleep -Seconds 15
foreach ($sgName in @("$Prefix-rds-sg", "$Prefix-eb-sg")) {
  $sgId = aws ec2 describe-security-groups --filters "Name=group-name,Values=$sgName" --query SecurityGroups[0].GroupId --output text --region $Region 2>$null
  if ($sgId -and $sgId -ne "None") {
    Write-Host "Deleting SG $sgName $sgId"
    aws ec2 delete-security-group --group-id $sgId --region $Region 2>$null | Out-Null
  }
}

# --- Resource group + SSM ---
aws resource-groups delete-group --group-name "donation-asm3-rg" --region $Region 2>$null | Out-Null
aws ssm delete-parameter --name "/cosc29800/asm3/rds/master-password" --region $Region 2>$null | Out-Null

Write-Host "=== aws-asm3 teardown submitted ==="
Write-Host "RDS password file was left in place for the next provision."
