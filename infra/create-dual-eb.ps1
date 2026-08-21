# Create separate Elastic Beanstalk apps for donor UI and admin UI, then terminate the old combined env.
$ErrorActionPreference = "Continue"
$Region = "us-east-1"
$Options = "d:\source\RMIT\master-of-ai-new\2026-semester-02\cloud-computing-asm3\infra\eb-option-settings.json"
$DeployBucket = "aws-asm3-deploy-026652000073"
$SampleKey = "beanstalk/sample-app.zip"

function Ensure-App([string]$name, [string]$desc) {
  aws elasticbeanstalk describe-applications --application-names $name --query "Applications[0].ApplicationName" --output text --region $Region 2>$null | Out-Null
  if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating application $name"
    aws elasticbeanstalk create-application --application-name $name --description $desc --tags "Key=Project,Value=aws-asm3" --region $Region | Out-Null
  }
}

function Ensure-Env([string]$app, [string]$envName) {
  $status = aws elasticbeanstalk describe-environments --application-name $app --environment-names $envName --query "Environments[0].Status" --output text --region $Region 2>$null
  if ($LASTEXITCODE -eq 0 -and $status -and $status -ne "None" -and $status -ne "Terminated") {
    Write-Host "Environment $envName already exists ($status)"
    return
  }
  Write-Host "Creating environment $envName under $app"
  aws elasticbeanstalk create-application-version `
    --application-name $app `
    --version-label "bootstrap-1" `
    --source-bundle "S3Bucket=$DeployBucket,S3Key=$SampleKey" `
    --region $Region 2>$null | Out-Null
  aws elasticbeanstalk create-environment `
    --application-name $app `
    --environment-name $envName `
    --solution-stack-name "64bit Amazon Linux 2023 v3.11.6 running .NET 10" `
    --version-label "bootstrap-1" `
    --option-settings "file://$Options" `
    --tags "Key=Project,Value=aws-asm3" "Key=Assignment,Value=COSC29800-A3" `
    --region $Region | Out-Null
}

Ensure-App "aws-asm3-donor-ui" "Hope and Help donor Blazor UI"
Ensure-App "aws-asm3-admin-ui" "Donation admin Blazor UI"
Ensure-Env "aws-asm3-donor-ui" "aws-asm3-donor-env"
Ensure-Env "aws-asm3-admin-ui" "aws-asm3-admin-env"

# Terminate the old combined environment (Severe / wrong app)
$old = aws elasticbeanstalk describe-environments --environment-names aws-asm3-ui-env --query "Environments[0].Status" --output text --region $Region 2>$null
if ($old -and $old -ne "None" -and $old -ne "Terminated" -and $old -ne "Terminating") {
  Write-Host "Terminating old combined env aws-asm3-ui-env ($old)"
  aws elasticbeanstalk terminate-environment --environment-name aws-asm3-ui-env --region $Region | Out-Null
}

Write-Host "DONE_EB_SPLIT"
aws elasticbeanstalk describe-environments --environment-names aws-asm3-donor-env,aws-asm3-admin-env,aws-asm3-ui-env --query "Environments[].{Name:EnvironmentName,Status:Status,Health:Health,CNAME:CNAME}" --output table --region $Region
