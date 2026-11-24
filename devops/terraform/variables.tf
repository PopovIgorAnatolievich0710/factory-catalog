variable "git_api_token_id" {
  type    = string
  default = ""
}

variable "kms_key_alias" {
  type = string
}

variable "build_tag" {
  type    = string
  default = "test"
}

variable "app_version" {
  type = string
}

variable "branch_name" {
  type    = string
  default = "test"
}

variable "repo_name" {
  type    = string
  default = "test"
}

variable "project_name" {
  type = string
}

variable "deploy_env" {
  type = string
}

variable "k8s_namespace" {
  type = string
}

variable "liveness_probe_path" {
  type = string
}

variable "readiness_probe_path" {
  type = string
}

variable "has_db" {
  type = string
}

variable "database_name" {
  type = string
}

variable "region" {
  type    = string
  default = "eu-west-1"
}

variable "account-id" {
  type    = string
  default = "815199475415"
}

variable "default_tags" {
  type = map(any)
  default = {
    project-group = "DevOps"
    Terraform     = "true"
  }
}

variable "rds_vpc_id" {
  type    = string
  default = "vpc-70f98c15"
}

variable "rds_instance_class" {
  type    = string
  default = "db.t3.small"
}

variable "context_hash_main" {
  type = string
}

variable "context_hash_balancer" {
  type = string
}

variable "context_hash_ui" {
  type = string
}

variable "azure_tenant_id" {
  type    = string
  default = "26c34b0b-8ce7-4c06-97fb-3d5915066e98"
}

variable "azure_app_registrations" {
  type = map(object({
    api_client_id = string
    ui_client_id  = string
    backend_scope = string
  }))
  default = {
    dev = {
      api_client_id = "REPLACE_WITH_DEV_AZURE_API_CLIENT_ID"
      ui_client_id  = "REPLACE_WITH_DEV_AZURE_UI_CLIENT_ID"
      backend_scope = "api://REPLACE_WITH_DEV_AZURE_API_CLIENT_ID/REPLACE_WITH_SCOPE_NAME"
    }
    stg = {
      api_client_id = "REPLACE_WITH_STG_AZURE_API_CLIENT_ID"
      ui_client_id  = "REPLACE_WITH_STG_AZURE_UI_CLIENT_ID"
      backend_scope = "api://REPLACE_WITH_STG_AZURE_API_CLIENT_ID/REPLACE_WITH_SCOPE_NAME"
    }
    prod = {
      api_client_id = "REPLACE_WITH_PROD_AZURE_API_CLIENT_ID"
      ui_client_id  = "REPLACE_WITH_PROD_AZURE_UI_CLIENT_ID"
      backend_scope = "api://REPLACE_WITH_PROD_AZURE_API_CLIENT_ID/REPLACE_WITH_SCOPE_NAME"
    }
  }
}

variable "master_catalog_username" {
  type    = string
  default = "REPLACE_WITH_MASTER_CATALOG_USERNAME"
}

variable "shipment_service_username" {
  type    = string
  default = "admin"
}

variable "sap_export_prefix" {
  type    = string
  default = "sap-export"
}

variable "sap_export_writer_arns" {
  type    = list(string)
  default = []
}

variable "ocs_order_items_filter_policy" {
  type    = string
  default = ""
}

variable "supplier_event_types" {
  type = list(string)
  default = [
    "TenenGroup.Supplier.Events.ItemSent",
    "TenenGroup.Supplier.Events.REPLACE_WITH_REPRODUCED_EVENT_NAME"
  ]
}

locals {
  managed_env  = contains(["prod", "stg", "dev"], var.deploy_env)
  resource_env = local.managed_env ? var.deploy_env : "dev"

  create_rds                   = (contains(["master", "release", "develop"], var.branch_name) && var.has_db == "true" ? true : false)
  rds_aws_route53_zone         = lookup({ stg = "stg.eks.tenengroup.com.", prod = "eks.tenengroup.com." }, var.deploy_env, "dev.eks.tenengroup.com.")
  database_host_for_ext_access = length(aws_route53_record.rds_route53_record) > 0 ? trimsuffix("${aws_route53_record.rds_route53_record[0].name}.${local.rds_aws_route53_zone}", ".") : null
  database_host_for_k8s_access = (contains(["master", "release", "develop"], var.branch_name) ? local.database_host_for_ext_access : "${var.project_name}-${var.branch_name}-postgresql")

  helm_release_name = "${var.project_name}-${var.branch_name}"
  ingress_class     = "tg-internal"

  main_host         = lookup({ stg = "${var.project_name}-${var.branch_name}.stg.eks.tenengroup.com", prod = "${var.project_name}.eks.tenengroup.com" }, var.deploy_env, "${var.project_name}-${var.branch_name}.dev.eks.tenengroup.com")
  main_app_url_full = "https://${local.main_host}"

  balancer_host         = lookup({ stg = "${var.project_name}-${var.branch_name}-balancer.stg.eks.tenengroup.com", prod = "${var.project_name}-balancer.eks.tenengroup.com" }, var.deploy_env, "${var.project_name}-${var.branch_name}-balancer.dev.eks.tenengroup.com")
  balancer_app_url_full = "https://${local.balancer_host}"

  ui_host         = lookup({ stg = "${var.project_name}-${var.branch_name}-ui.stg.eks.tenengroup.com", prod = "${var.project_name}-ui.eks.tenengroup.com" }, var.deploy_env, "${var.project_name}-${var.branch_name}-ui.dev.eks.tenengroup.com")
  ui_app_url_full = "https://${local.ui_host}"

  dev_wildcard_cert_name = "replica-wildcard-dev.eks.tenengroup.com"

  catalog_internal_url = "http://${local.helm_release_name}-main-main"
  service_role_arn     = "arn:aws:iam::${var.account-id}:role/eks-tenenkube-${var.k8s_namespace}"

  azure_app_registration = var.azure_app_registrations[local.resource_env]
}
