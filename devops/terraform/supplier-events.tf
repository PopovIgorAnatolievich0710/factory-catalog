resource "aws_sqs_queue" "dlq_supplier_events" {
  name                       = "DLQ-${var.project_name}-${var.deploy_env}-supplier-events"
  delay_seconds              = 0
  message_retention_seconds  = 1209600
  receive_wait_time_seconds  = 0
  visibility_timeout_seconds = 60
  tags                       = var.default_tags
}

resource "aws_sqs_queue" "supplier_events" {
  name                       = "${var.project_name}-${var.deploy_env}-supplier-events"
  delay_seconds              = 0
  message_retention_seconds  = 1209600
  receive_wait_time_seconds  = 0
  visibility_timeout_seconds = 120
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dlq_supplier_events.arn
    maxReceiveCount     = 5
  })
  tags = var.default_tags
}

locals {
  supplier-events-topic-arn = "arn:aws:sns:${var.region}:${var.account-id}:supplier-${local.resource_env}-events"
}

resource "aws_sqs_queue_policy" "supplier_events" {
  queue_url = aws_sqs_queue.supplier_events.id
  policy = jsonencode({
    Version = "2012-10-17"
    Id      = "sqspolicy"
    Statement = [
      {
        Sid       = "AllowServiceRoleConsume"
        Effect    = "Allow"
        Principal = { AWS = local.service_role_arn }
        Action = [
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:ChangeMessageVisibility",
          "sqs:GetQueueAttributes",
          "sqs:GetQueueUrl"
        ]
        Resource = aws_sqs_queue.supplier_events.arn
      },
      {
        Sid       = "AllowSupplierTopicSend"
        Effect    = "Allow"
        Principal = "*"
        Action    = "sqs:SendMessage"
        Resource  = aws_sqs_queue.supplier_events.arn
        Condition = {
          ArnEquals = {
            "aws:SourceArn" = local.supplier-events-topic-arn
          }
        }
      }
    ]
  })
}

module "supplier_events_queue_to_supplier_events" {
  source              = "./sqs_to_sns_subscription"
  create_subscription = local.managed_env
  add_policy          = false
  sns_topic_arn       = local.supplier-events-topic-arn
  sqs_queue_arn       = aws_sqs_queue.supplier_events.arn
  sqs_queue_url       = aws_sqs_queue.supplier_events.id
  filter_policy       = jsonencode({ event_type = var.supplier_event_types })
}
