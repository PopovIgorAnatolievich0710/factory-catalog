resource "aws_sqs_queue" "dlq_ocs_order_items" {
  name                       = "DLQ-${var.project_name}-${var.deploy_env}-ocs-order-items"
  delay_seconds              = 0
  message_retention_seconds  = 1209600
  receive_wait_time_seconds  = 0
  visibility_timeout_seconds = 60
  tags                       = var.default_tags
}

resource "aws_sqs_queue" "ocs_order_items" {
  name                       = "${var.project_name}-${var.deploy_env}-ocs-order-items"
  delay_seconds              = 0
  message_retention_seconds  = 1209600
  receive_wait_time_seconds  = 0
  visibility_timeout_seconds = 160
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dlq_ocs_order_items.arn
    maxReceiveCount     = 5
  })
  tags = var.default_tags
}

locals {
  ocs-events-topic-arn = "arn:aws:sns:${var.region}:${var.account-id}:order-core-service-${local.resource_env}-domain-events"
}

resource "aws_sqs_queue_policy" "ocs_order_items" {
  queue_url = aws_sqs_queue.ocs_order_items.id
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
        Resource = aws_sqs_queue.ocs_order_items.arn
      },
      {
        Sid       = "AllowOcsTopicSend"
        Effect    = "Allow"
        Principal = "*"
        Action    = "sqs:SendMessage"
        Resource  = aws_sqs_queue.ocs_order_items.arn
        Condition = {
          ArnEquals = {
            "aws:SourceArn" = local.ocs-events-topic-arn
          }
        }
      }
    ]
  })
}

module "ocs_order_items_queue_to_ocs_domain_events" {
  source              = "./sqs_to_sns_subscription"
  create_subscription = local.managed_env
  add_policy          = false
  sns_topic_arn       = local.ocs-events-topic-arn
  sqs_queue_arn       = aws_sqs_queue.ocs_order_items.arn
  sqs_queue_url       = aws_sqs_queue.ocs_order_items.id
  filter_policy       = var.ocs_order_items_filter_policy
}
