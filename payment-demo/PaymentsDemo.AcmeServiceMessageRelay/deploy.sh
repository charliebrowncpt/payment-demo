#!/usr/bin/env bash
# Deploy or update the notification handler Lambda and bind it to its SQS queue.
#
# Usage:
#   ./deploy-notification-handler.sh [create|update|auto]
#     create  fail if the function already exists
#     update  fail if the function does not exist
#     auto    create if missing, otherwise update (default)
#
# Override any setting with an environment variable, for example:
#   BATCH_SIZE=5 ./deploy-notification-handler.sh auto
#   ENDPOINT="" ./deploy-notification-handler.sh auto   # real AWS
set -euo pipefail

MODE="${1:-auto}"
case "$MODE" in
  create|update|auto) ;;
  *) echo "Usage: $0 [create|update|auto]" >&2; exit 1 ;;
esac

FUNCTION_NAME="payment-notification-handler"
ZIP_PATH="payment-notification-handler.zip"
HANDLER="notification-lambda::PaymentsDemo.PaymentNotificationHandler.Function::FunctionHandler"
RUNTIME="dotnet10"
ROLE_ARN="arn:aws:iam::000000000000:role/lambda-role"
MEMORY_MB="512"
TIMEOUT_S="30"
REGION="af-south-1"
ACCOUNT_ID="000000000000"
QUEUE_NAME="payment-intents"
BATCH_SIZE="10"
# Set to ReportBatchItemFailures only if the handler returns SQSBatchResponse.
FUNCTION_RESPONSE_TYPES="${FUNCTION_RESPONSE_TYPES:-}"
# Optional, for example "Variables={KEY=value,OTHER=value}".
ENV_VARS="${ENV_VARS:-}"
# LocalStack endpoint. Set ENDPOINT="" to target real AWS.
ENDPOINT="${ENDPOINT-http://localhost:4566}"

[[ -f "$ZIP_PATH" ]] || { echo "Zip not found: $ZIP_PATH" >&2; exit 1; }

ENDPOINT_ARGS=()
if [[ -n "$ENDPOINT" ]]; then
  ENDPOINT_ARGS=(--endpoint-url "$ENDPOINT")
  export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
  export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"
fi

aws_cli() { command aws "${ENDPOINT_ARGS[@]}" --region "$REGION" "$@"; }

ENV_ARGS=()
[[ -n "$ENV_VARS" ]] && ENV_ARGS=(--environment "$ENV_VARS")

FRT_ARGS=()
[[ -n "$FUNCTION_RESPONSE_TYPES" ]] && FRT_ARGS=(--function-response-types "$FUNCTION_RESPONSE_TYPES")

QUEUE_ARN="arn:aws:sqs:$REGION:$ACCOUNT_ID:$QUEUE_NAME"

function_exists() {
  aws_cli lambda get-function --function-name "$FUNCTION_NAME" >/dev/null 2>&1
}

create_function() {
  echo "Creating function $FUNCTION_NAME"
  aws_cli lambda create-function \
    --function-name "$FUNCTION_NAME" \
    --runtime "$RUNTIME" \
    --handler "$HANDLER" \
    --role "$ROLE_ARN" \
    --memory-size "$MEMORY_MB" \
    --timeout "$TIMEOUT_S" \
    --zip-file "fileb://$ZIP_PATH" \
    ${ENV_ARGS[@]+"${ENV_ARGS[@]}"} >/dev/null
}

update_function() {
  echo "Updating code for $FUNCTION_NAME"
  aws_cli lambda update-function-code \
    --function-name "$FUNCTION_NAME" \
    --zip-file "fileb://$ZIP_PATH" >/dev/null
  aws_cli lambda wait function-updated-v2 --function-name "$FUNCTION_NAME"

  echo "Updating configuration for $FUNCTION_NAME"
  aws_cli lambda update-function-configuration \
    --function-name "$FUNCTION_NAME" \
    --runtime "$RUNTIME" \
    --handler "$HANDLER" \
    --role "$ROLE_ARN" \
    --memory-size "$MEMORY_MB" \
    --timeout "$TIMEOUT_S" \
    ${ENV_ARGS[@]+"${ENV_ARGS[@]}"} >/dev/null
  aws_cli lambda wait function-updated-v2 --function-name "$FUNCTION_NAME"
}

bind_queue() {
  aws_cli sqs get-queue-url --queue-name "$QUEUE_NAME" >/dev/null \
    || { echo "Queue $QUEUE_NAME does not exist" >&2; exit 1; }

  local uuid
  uuid=$(aws_cli lambda list-event-source-mappings \
    --function-name "$FUNCTION_NAME" \
    --event-source-arn "$QUEUE_ARN" \
    --query 'EventSourceMappings[0].UUID' --output text)

  if [[ -z "$uuid" || "$uuid" == "None" ]]; then
    echo "Binding $QUEUE_NAME to $FUNCTION_NAME (batch size $BATCH_SIZE)"
    aws_cli lambda create-event-source-mapping \
      --function-name "$FUNCTION_NAME" \
      --event-source-arn "$QUEUE_ARN" \
      --batch-size "$BATCH_SIZE" \
      ${FRT_ARGS[@]+"${FRT_ARGS[@]}"} >/dev/null
  else
    echo "Mapping already exists ($uuid), syncing batch size to $BATCH_SIZE"
    aws_cli lambda update-event-source-mapping \
      --uuid "$uuid" \
      --batch-size "$BATCH_SIZE" \
      ${FRT_ARGS[@]+"${FRT_ARGS[@]}"} >/dev/null
  fi
}

case "$MODE" in
  create)
    if function_exists; then
      echo "$FUNCTION_NAME already exists. Use update or auto." >&2; exit 1
    fi
    create_function
    ;;
  update)
    if ! function_exists; then
      echo "$FUNCTION_NAME does not exist. Use create or auto." >&2; exit 1
    fi
    update_function
    ;;
  auto)
    if function_exists; then update_function; else create_function; fi
    ;;
esac

aws_cli lambda wait function-active-v2 --function-name "$FUNCTION_NAME"
bind_queue
echo "Done: $FUNCTION_NAME deployed and bound to $QUEUE_NAME"