#!/bin/bash
set -euo pipefail

awslocal s3 mb "s3://${SAP_EXPORT_BUCKET}"

for queue in "${OCS_ORDER_ITEMS_QUEUE}" "${SUPPLIER_EVENTS_QUEUE}"; do
  awslocal sqs create-queue --queue-name "DLQ-${queue}"
  awslocal sqs create-queue --queue-name "${queue}" --attributes "{\"RedrivePolicy\":\"{\\\"deadLetterTargetArn\\\":\\\"arn:aws:sqs:${AWS_DEFAULT_REGION}:000000000000:DLQ-${queue}\\\",\\\"maxReceiveCount\\\":\\\"5\\\"}\"}"
done

awslocal sns create-topic --name "${ALLOCATION_DECIDED_TOPIC}"
awslocal sns create-topic --name "${QUOTE_PRODUCED_TOPIC}"
