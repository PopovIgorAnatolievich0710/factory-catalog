output "MAIN_URL_FULL_LINK" {
  value = local.main_app_url_full
}

output "BALANCER_URL_FULL_LINK" {
  value = local.balancer_app_url_full
}

output "UI_URL_FULL_LINK" {
  value = local.ui_app_url_full
}

output "DB_URL" {
  value = local.database_host_for_ext_access != null ? local.database_host_for_ext_access : "db-by-port.dev.eks.tenengroup.com:${(data.kubernetes_service.db_main[0].spec != null ? data.kubernetes_service.db_main[0].spec.0.port.0.node_port : "null")}"
}

output "DB_NAME" {
  value = var.database_name
}

output "RDS_DB_IDENTIFIER" {
  value = length(aws_db_instance.rds-db) > 0 ? aws_db_instance.rds-db[0].identifier : null
}

output "SAP_EXPORT_BUCKET" {
  value = "s3://${local.sap_export_bucket_name}/${var.sap_export_prefix}"
}

output "OCS_ORDER_ITEMS_QUEUE_URL" {
  value = aws_sqs_queue.ocs_order_items.id
}

output "SUPPLIER_EVENTS_QUEUE_URL" {
  value = aws_sqs_queue.supplier_events.id
}

output "ALLOCATION_DECIDED_TOPIC_ARN" {
  value = aws_sns_topic.balancer_events["allocation_decided"].arn
}

output "QUOTE_PRODUCED_TOPIC_ARN" {
  value = aws_sns_topic.balancer_events["quote_produced"].arn
}
