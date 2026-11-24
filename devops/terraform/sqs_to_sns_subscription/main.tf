resource "aws_sns_topic_subscription" "this" {
  count = (var.create_subscription ? 1 : 0 )
  topic_arn = var.sns_topic_arn
  protocol  = "sqs"
  endpoint  = var.sqs_queue_arn
  filter_policy = length(var.filter_policy) > 0 ? var.filter_policy : null
  raw_message_delivery = true
}

resource "aws_sqs_queue_policy" "queue_policy" {
  count = (var.create_subscription && var.add_policy ? 1 : 0 )
  lifecycle {
    create_before_destroy = true
  }
  queue_url = var.sqs_queue_url
  policy = <<POLICY
{
  "Version": "2012-10-17",
  "Id": "sqspolicy",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": "*",
      "Action": "sqs:SendMessage",
      "Resource": "${var.sqs_queue_arn}",
      "Condition": {
        "ArnEquals": {
          "aws:SourceArn": "${var.sns_topic_arn}"
        }
      }
    }
  ]
}
POLICY
}