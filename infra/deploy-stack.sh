#!/usr/bin/env bash
# Deploy / update the aws-asm3 CloudFormation stack (AWS Academy Learner Lab).
set -euo pipefail

REGION="${AWS_REGION:-us-east-1}"
STACK_NAME="${STACK_NAME:-aws-asm3-donation}"
TEMPLATE="$(cd "$(dirname "$0")" && pwd)/aws-asm3-stack.yaml"
DB_PASSWORD="${DB_PASSWORD:-}"

if [[ -z "${DB_PASSWORD}" ]]; then
  if command -v openssl >/dev/null 2>&1; then
    DB_PASSWORD="$(openssl rand -base64 18 | tr -d '/+=' | cut -c1-20)Aa1!"
  else
    echo "Set DB_PASSWORD env var (min 8 chars)." >&2
    exit 1
  fi
fi

echo "Deploying stack ${STACK_NAME} in ${REGION}..."
aws cloudformation deploy \
  --region "${REGION}" \
  --stack-name "${STACK_NAME}" \
  --template-file "${TEMPLATE}" \
  --parameter-overrides \
    "DbUsername=donationadmin" \
    "DbPassword=${DB_PASSWORD}" \
  --capabilities CAPABILITY_NAMED_IAM \
  --tags \
    "Project=aws-asm3" \
    "Name=aws-asm3-donation-stack" \
    "Assignment=COSC29800-A3"

echo
echo "Stack outputs:"
aws cloudformation describe-stacks \
  --region "${REGION}" \
  --stack-name "${STACK_NAME}" \
  --query "Stacks[0].Outputs" \
  --output table

echo
echo "RDS password stored in SSM: /aws-asm3/rds/master-password"
echo "Resource group: aws-asm3-donation (tag Project=aws-asm3)"
