# AWS infra — `aws-asm3-*` (COSC29800 Assignment 3)

Resources are tagged **`Project=aws-asm3`** and selected by Resource Group **`donation-asm3-rg`**
(Console → Resource Groups & Tag Editor). Group names cannot start with `AWS`, so the group is `donation-asm3-rg` while resource names use the `aws-asm3-*` prefix.

Live outputs: [`aws-asm3-outputs.json`](aws-asm3-outputs.json)  
Architecture: [`../SOLUTION_ARCHITECTURE.md`](../SOLUTION_ARCHITECTURE.md)  
Data flow: [`../DATA_FLOW.md`](../DATA_FLOW.md)

AWS Academy `voclabs` cannot create CloudFront (`cloudfront:CreateDistribution` denied). UIs are reached on Elastic Beanstalk CNAMEs.

## Provisioned resources

| Resource | Name | Role |
|---|---|---|
| Resource Group | `donation-asm3-rg` (filter `Project=aws-asm3`) | Console grouping |
| S3 | `aws-asm3-receipts-{account}` | Receipt PDFs |
| S3 | `aws-asm3-deploy-{account}` | Deploy artifacts |
| S3 | `aws-asm3-analytics-{account}` | Athena results |
| RDS PostgreSQL 16 | `aws-asm3-postgres` | OLTP database `donation` |
| Lambda | `aws-asm3-api` | ASP.NET Core 10 API (`DISABLE_OUTBOX_PROCESSOR=true`) |
| Lambda | `aws-asm3-outbox-worker` | Placeholder stub (seed drains outbox) |
| API Gateway HTTP | `aws-asm3-http-api` | Edge for `/health` and `/api/v1` |
| Elastic Beanstalk | `aws-asm3-donor-ui` / `aws-asm3-donor-env` | Donor UI |
| Elastic Beanstalk | `aws-asm3-admin-ui` / `aws-asm3-admin-env` | Admin UI |
| Athena | workgroup `aws-asm3-analytics` | Analytics |
| Glue | DB `aws_asm3_donation` | Athena catalog |

Uses Academy **`LabRole`** + **`LabInstanceProfile`**.

## Tear down, provision, deploy

Learner Lab must be **started**.

```powershell
cd cloud-computing-asm3
powershell -NoProfile -ExecutionPolicy Bypass -File .\infra\teardown-aws-asm3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\infra\provision-aws-asm3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\infra\deploy-aws-asm3.ps1
```

RDS password (generated once, reused): `%USERPROFILE%\.aws\aws-asm3-db-password.txt`  
User: `donationadmin` · DB: `donation` · Port: `5432`

`deploy-aws-asm3.ps1` does not write a PostgreSQL connection string. It sets Lambda `RDS_ENDPOINT` / `RDS_PASSWORD`; the API builds the URI at runtime.

Do **not** put Learner Lab access keys, session tokens, or the RDS password in GitHub Actions secrets. Those credentials expire every lab session and must stay on the machine that runs `deploy-aws-asm3.ps1`.

```powershell
aws resource-groups list-group-resources --group-name donation-asm3-rg
aws resourcegroupstaggingapi get-resources --tag-filters Key=Project,Values=aws-asm3
```

## CloudWatch Logs

```powershell
aws logs tail /aws/lambda/aws-asm3-api --follow
aws logs tail /aws/apigateway/aws-asm3-http-api --follow
aws logs tail /aws/elasticbeanstalk/aws-asm3-donor-env/var/log/web.stdout.log --follow
aws logs tail /aws/elasticbeanstalk/aws-asm3-admin-env/var/log/web.stdout.log --follow
aws logs tail /aws/rds/instance/aws-asm3-postgres/postgresql --follow
```

## Smoke checks

```powershell
# URLs: infra/aws-asm3-outputs.json
curl.exe https://<httpApiUrl>/health
curl.exe "https://<httpApiUrl>/api/v1/contacts?`$top=2"
```

## Migrate / seed RDS (1,000 contacts)

On a brand-new RDS instance use `-m -s` (do not pass `-d` unless you intend to wipe). Seed default is 1,000 contacts (`SEED_DONATION_COUNT`). This drains the outbox per gift and can take a long time against RDS.

```powershell
$pwd = (Get-Content "$env:USERPROFILE\.aws\aws-asm3-db-password.txt" -Raw).Trim()
$rds = (Get-Content .\infra\aws-asm3-outputs.json | ConvertFrom-Json).rdsEndpoint
$bucket = (Get-Content .\infra\aws-asm3-outputs.json | ConvertFrom-Json).receiptsBucket
$env:Database__ApplicationConnectionString = "Host=$rds;Port=5432;Database=donation;Username=donationadmin;Password=$pwd;SSL Mode=Require;Trust Server Certificate=true"
$env:Database__Provider = "postgresql"
$env:Database__MigrationsAssembly = "NetCore.Donation.Infrastructure.Database"
$env:ObjectStorage__BucketName = $bucket
$env:ObjectStorage__Region = "us-east-1"
$env:SEED_DONATION_COUNT = "1000"
cd src
dotnet run --project client\NetCore.Donation.Migration --no-launch-profile -c Release -- -m -s
```

## Files

| File | Purpose |
|---|---|
| `provision-aws-asm3.ps1` | Create/update tagged resources via CLI |
| `teardown-aws-asm3.ps1` | Delete the same tagged resources |
| `deploy-aws-asm3.ps1` | Publish API Lambda + Beanstalk UIs from this machine |
| `resource-group.json` | Tag-based resource group (`donation-asm3-rg`) |
| `apigw-access-logs.json` | Rewritten by provision with the current API id |
| `aws-asm3-outputs.json` | Live names, URLs, and RDS endpoint |
