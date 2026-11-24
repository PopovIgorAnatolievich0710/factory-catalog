locals {
  balancer_event_topics = {
    allocation_decided = "${var.project_name}-${var.deploy_env}-balancer-allocation-decided"
    quote_produced     = "${var.project_name}-${var.deploy_env}-balancer-quote-produced"
  }
}

resource "aws_sns_topic" "balancer_events" {
  for_each = local.balancer_event_topics
  name     = each.value
  tags     = var.default_tags
}

resource "aws_sns_topic_policy" "balancer_events" {
  for_each = aws_sns_topic.balancer_events
  arn      = each.value.arn
  policy = jsonencode({
    Version = "2012-10-17"
    Id      = "snspolicy"
    Statement = [
      {
        Sid       = "AllowAccountManage"
        Effect    = "Allow"
        Principal = { AWS = "*" }
        Action = [
          "sns:GetTopicAttributes",
          "sns:SetTopicAttributes",
          "sns:AddPermission",
          "sns:RemovePermission",
          "sns:DeleteTopic",
          "sns:Subscribe",
          "sns:ListSubscriptionsByTopic",
          "sns:Publish"
        ]
        Resource = each.value.arn
        Condition = {
          StringEquals = {
            "AWS:SourceOwner" = var.account-id
          }
        }
      },
      {
        Sid       = "AllowServiceRolePublish"
        Effect    = "Allow"
        Principal = { AWS = local.service_role_arn }
        Action    = "sns:Publish"
        Resource  = each.value.arn
      }
    ]
  })
}
