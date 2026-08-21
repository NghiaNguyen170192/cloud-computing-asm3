# AWS infra — `aws-asm3-*` (COSC29800 Assignment 3)

Resources are tagged **`Project=aws-asm3`** and selected by Resource Group **`donation-asm3-rg`**
(Console → Resource Groups & Tag Editor). Group names cannot start with `AWS`, so the group is `donation-asm3-rg` while resource names use the `aws-asm3-*` prefix.

Live outputs: [`aws-asm3-outputs.json`](aws-asm3-outputs.json)

## Provisioned resources

| Resource | Name | Rubric |
|---|---|---|
| Resource Group | `donation-asm3-rg` (filter `Project=aws-asm3`) | Console grouping |
| S3 | `aws-asm3-receipts-026652000073` | Receipts (3) |
| S3 | `aws-asm3-deploy-026652000073` | Deploy artifacts |
| S3 | `aws-asm3-analytics-026652000073` | Athena results (3) |
| RDS PostgreSQL | `aws-asm3-postgres` | DB (3) |
| Lambda | `aws-asm3-api`, `aws-asm3-outbox-worker` | Compute (6) |
| API Gateway HTTP | `aws-asm3-http-api` → `https://d5i11y0jd8.execute-api.us-east-1.amazonaws.com` | Edge (6) |
| Elastic Beanstalk | app `aws-asm3-donation-ui` / env `aws-asm3-ui-env` | UI (6) |
| Athena | workgroup `aws-asm3-analytics` | Analytics (3) |
| Glue | DB `aws_asm3_donation` | Athena catalog |

Uses Academy **`LabRole`** + **`LabInstanceProfile`**.

## Re-provision from laptop

Learner Lab must be **started**.

```powershell
cd cloud-computing-asm3
powershell -NoProfile -ExecutionPolicy Bypass -File .\infra\provision-aws-asm3.ps1
```

RDS password (generated): `%USERPROFILE%\.aws\aws-asm3-db-password.txt`  
User: `donationadmin` · DB: `donation` · Port: `5432`

```powershell
aws resource-groups list-group-resources --group-name donation-asm3-rg
aws resourcegroupstaggingapi get-resources --tag-filters Key=Project,Values=aws-asm3
```

## GitHub Actions deploy

Workflow: [`.github/workflows/deploy-aws-asm3.yml`](../.github/workflows/deploy-aws-asm3.yml)

Add repo secrets (refresh when Learner Lab restarts):

| Secret | Source |
|---|---|
| `AWS_ACCESS_KEY_ID` | Learner Lab AWS Details |
| `AWS_SECRET_ACCESS_KEY` | Learner Lab |
| `AWS_SESSION_TOKEN` | Learner Lab |

On push to `main` / `master` / `donation-implementation` (or **Run workflow**):

1. Resolves `aws-asm3-*` resources by name  
2. Builds .NET solution  
3. Publishes Admin UI → S3 deploy bucket → Elastic Beanstalk  
4. Updates Lambda zip + env (`RECEIPTS_BUCKET`, `RDS_ENDPOINT`, `GIT_SHA`)

## Smoke checks

```powershell
curl https://d5i11y0jd8.execute-api.us-east-1.amazonaws.com/
# EB (Grey until first real app version succeeds):
# http://aws-asm3-ui-env.eba-mrhkizyq.us-east-1.elasticbeanstalk.com
```

## Next app wiring

- Point `DonationApiClient` at the HTTP API URL.
- RDS connection string + run Migration.
- Real S3 bucket name (drop MinIO ServiceUrl).
- Replace Python Lambda shim with .NET API package when ready.
- Deploy donor UI as a second EB env or combine sites.

## Files

| File | Purpose |
|---|---|
| `provision-aws-asm3.ps1` | Create/update tagged resources via CLI |
| `eb-option-settings.json` | Beanstalk VPC/instance settings |
| `resource-group.json` | Tag-based resource group |
| `aws-asm3-stack.yaml` | Optional CloudFormation (Academy early-validation can fail; CLI path is canonical) |
| `deploy-aws-asm3.yml` | GitHub Actions deploy |
