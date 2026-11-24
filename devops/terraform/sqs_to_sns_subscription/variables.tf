variable "sns_topic_arn" {
  type = string
}

variable "sqs_queue_arn" {
  type = string
}

variable "sqs_queue_url" {
  type = string
}

variable "filter_policy" {
  type = string
  default = ""
}

variable "create_subscription" {
  type = bool
  default = false
}

variable "add_policy" {
  type = bool
  default = true
}