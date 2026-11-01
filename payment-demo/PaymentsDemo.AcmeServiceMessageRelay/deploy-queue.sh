#!/usr/bin/env bash

set -euo pipefail

ENDPOINT=http://localhost:4566
REGION=af-south-1
ACCOUNT=000000000000
awsl() { aws --endpoint-url "$ENDPOINT" --region "$REGION" "$@"; }

# 1. Dead-letter queue: 14 days retention so failed intents can be inspected
awsl sqs create-queue --queue-name payment-intents-dlq \
  --attributes MessageRetentionPeriod=1209600

DLQ_ARN=$(awsl sqs get-queue-attributes \
  --queue-url "$ENDPOINT/$ACCOUNT/payment-intents-dlq" \
  --attribute-names QueueArn --query Attributes.QueueArn --output text)

# 2. Main queue: redrive to the DLQ after 5 receives, 6x the relay's 60s timeout
#    for visibility, 4 days retention
cat > /tmp/payment-intents.json <<EOF
{
  "RedrivePolicy": "{\"deadLetterTargetArn\":\"$DLQ_ARN\",\"maxReceiveCount\":\"5\"}",
  "VisibilityTimeout": "360",
  "MessageRetentionPeriod": "345600"
}
EOF

awsl sqs create-queue --queue-name payment-intents --attributes file:///tmp/payment-intents.json

# 3. Check the result
awsl sqs get-queue-attributes \
  --queue-url "$ENDPOINT/$ACCOUNT/payment-intents" --attribute-names RedrivePolicy VisibilityTimeout MessageRetentionPeriod