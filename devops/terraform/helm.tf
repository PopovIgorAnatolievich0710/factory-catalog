resource "local_file" "chart-yaml" {
  content = templatefile("../helm/chart/Chart.yaml.tpl", {
    project_name = var.project_name
    branch_name  = var.branch_name
  })
  filename = "../helm/chart/Chart.yaml"
}

resource "null_resource" "fix_bug" {
  triggers = {
    always_run = timestamp()
  }
  provisioner "local-exec" {
    command = <<EOC
pending_upgrade_revision=`helm-v3 list -n ${var.k8s_namespace} --filter "${local.helm_release_name}" --pending --output json | jq -r '.[].revision'`
if [[ $pending_upgrade_revision != "" ]]; then
  kubectl delete secrets -n ${var.k8s_namespace} "sh.helm.release.v1.${local.helm_release_name}.v$pending_upgrade_revision"
fi
EOC
  }
}

resource "helm_release" "main" {
  lifecycle {
    ignore_changes = [version]
  }
  depends_on = [
    local_file.chart-yaml,
    aws_db_instance.rds-db,
    null_resource.db_init_script,
    null_resource.fix_bug,
    aws_sqs_queue_policy.ocs_order_items,
    aws_sqs_queue_policy.supplier_events,
    aws_sns_topic_policy.balancer_events,
    aws_s3_bucket_policy.sap_export
  ]
  chart             = "../helm/chart"
  name              = local.helm_release_name
  namespace         = var.k8s_namespace
  dependency_update = true

  values = [templatefile("../helm/chart/values.yaml", {
    deploy_env                   = var.deploy_env
    aspnetcore_environment       = (var.deploy_env == "prod" ? "Production" : "Staging")
    k8s_namespace                = var.k8s_namespace
    build_tag                    = var.build_tag
    is_pdb_enabled               = (var.deploy_env == "prod" ? true : false)
    app_version                  = var.app_version
    release_name                 = local.helm_release_name
    project_name                 = var.project_name
    replica_count                = 2
    priorityClassName            = lookup({ stg = "stg", prod = "prod" }, var.deploy_env, "dev-pr")
    preferredCapacityType        = lookup({ stg = "ON_DEMAND", prod = "ON_DEMAND" }, var.deploy_env, "SPOT")
    context_hash_main            = var.context_hash_main
    context_hash_balancer        = var.context_hash_balancer
    context_hash_ui              = var.context_hash_ui
    liveness_probe_path          = var.liveness_probe_path
    readiness_probe_path         = var.readiness_probe_path
    ingress_class                = local.ingress_class
    cert_manager_issuer_name     = lookup({ stg = "letsencrypt", prod = "letsencrypt" }, var.deploy_env, "")
    cert_renewal_time            = lookup({ stg = "720h", prod = "720h" }, var.deploy_env, "")
    app_host_main                = local.main_host
    app_host_balancer            = local.balancer_host
    app_host_ui                  = local.ui_host
    cert_secret_name_main        = lookup({ stg = local.main_host, prod = local.main_host }, var.deploy_env, local.dev_wildcard_cert_name)
    cert_secret_name_balancer    = lookup({ stg = local.balancer_host, prod = local.balancer_host }, var.deploy_env, local.dev_wildcard_cert_name)
    cert_secret_name_ui          = lookup({ stg = local.ui_host, prod = local.ui_host }, var.deploy_env, local.dev_wildcard_cert_name)
    main_url                     = local.main_app_url_full
    balancer_url                 = local.balancer_app_url_full
    ui_url                       = local.ui_app_url_full
    catalog_internal_url         = local.catalog_internal_url
    k8s_postgresql_enabled       = (var.has_db != "false" ? (local.create_rds == true ? false : true) : false)
    database_name                = var.database_name
    database_host_for_k8s_access = local.database_host_for_k8s_access
    db_migrations_username       = "migrations_user"
    db_password_migrations       = data.aws_kms_secrets.env_vars.plaintext["DB_PASSWORD_MIGRATIONS"]
    POSTGRESQL_ADMIN_PASSWORD    = data.aws_kms_secrets.env_vars.plaintext["POSTGRESQL_ADMIN_PASSWORD"]
    api_service_username         = "admin"
    api_service_user_password    = data.aws_kms_secrets.env_vars.plaintext["API_SERVICE_USER_PASSWORD"]
    azure_tenant_id              = var.azure_tenant_id
    azure_api_client_id          = local.azure_app_registration.api_client_id
    azure_ui_client_id           = local.azure_app_registration.ui_client_id
    azure_backend_scope          = local.azure_app_registration.backend_scope
    group_permissions_url        = lookup({ prod = "https://cdn-mnn.s3.eu-west-1.amazonaws.com/Config/GroupPermissions.json", stg = "https://cdn-mnn.s3.eu-west-1.amazonaws.com/Config/StgGroupPermissions.json" }, var.deploy_env, "https://cdn-mnn.s3.eu-west-1.amazonaws.com/Config/DevGroupPermissions.json")
    services_config_url          = (var.deploy_env == "prod" ? "https://cdn-mnn.s3.eu-west-1.amazonaws.com/Config/BackOfficeSystems.json" : "https://cdn-mnn.s3.eu-west-1.amazonaws.com/Config/BackOfficeSystemsDev.json")
    cors_allowed_hosts           = local.ui_app_url_full
    log_level                    = (var.deploy_env == "prod" ? "Information" : "Debug")
    aws_region                   = var.region
    aws_access_key               = lookup(data.aws_kms_secrets.env_vars.plaintext, "AWS_ACCESS_KEY", "")
    aws_secret_key               = lookup(data.aws_kms_secrets.env_vars.plaintext, "AWS_SECRET_KEY", "")
    datadog_api_key              = data.aws_kms_secrets.env_vars.plaintext["DD_API_KEY"]
    sap_export_bucket_name       = local.sap_export_bucket_name
    sap_export_prefix            = var.sap_export_prefix
    sap_export_cron              = "0 2 * * *"
    master_catalog_base_url      = lookup({ prod = "https://catalog.tenengroup.com", stg = "https://tg-catalog-release.stg.eks.tenengroup.com" }, var.deploy_env, "https://tg-catalog-develop.dev.eks.tenengroup.com")
    master_catalog_username      = var.master_catalog_username
    master_catalog_password      = data.aws_kms_secrets.env_vars.plaintext["MASTER_CATALOG_PASSWORD"]
    master_catalog_cron          = "0 3 * * *"
    shipment_service_base_url    = lookup({ prod = "https://shipment-service.eks.tenengroup.com", stg = "https://shipment-service-release.stg.eks.tenengroup.com" }, var.deploy_env, "https://shipment-service-develop.dev.eks.tenengroup.com")
    shipment_service_username    = var.shipment_service_username
    shipment_service_password    = data.aws_kms_secrets.env_vars.plaintext["SHIPMENT_SERVICE_API_PASSWORD"]
    ocs_order_items_queue_url    = aws_sqs_queue.ocs_order_items.id
    supplier_events_queue_url    = aws_sqs_queue.supplier_events.id
    allocation_decided_topic_arn = aws_sns_topic.balancer_events["allocation_decided"].arn
    quote_produced_topic_arn     = aws_sns_topic.balancer_events["quote_produced"].arn
  })]

  set {
    name  = "folder-checksum"
    value = sha1(join("", sort([for f in fileset("${path.module}/../helm/chart/templates", "**") : filesha1("${path.module}/../helm/chart/templates/${f}")])))
  }

  dynamic "set" {
    for_each = {
      "deployments.main.containers.main.resources.requests.cpu"        = "10m"
      "deployments.main.containers.main.resources.requests.memory"     = "500Mi"
      "deployments.main.containers.main.resources.limits.cpu"          = (local.resource_env == "dev" ? "1000m" : "4000m")
      "deployments.main.containers.main.resources.limits.memory"       = (local.resource_env == "dev" ? "1024Mi" : "4096Mi")
      "deployments.balancer.containers.main.resources.requests.cpu"    = "10m"
      "deployments.balancer.containers.main.resources.requests.memory" = "500Mi"
      "deployments.balancer.containers.main.resources.limits.cpu"      = (local.resource_env == "dev" ? "1000m" : "4000m")
      "deployments.balancer.containers.main.resources.limits.memory"   = (local.resource_env == "dev" ? "1024Mi" : "4096Mi")
      "deployments.ui.containers.main.resources.requests.cpu"          = "10m"
      "deployments.ui.containers.main.resources.requests.memory"       = "100Mi"
      "deployments.ui.containers.main.resources.limits.cpu"            = "1000m"
      "deployments.ui.containers.main.resources.limits.memory"         = "512Mi"
    }
    content {
      name  = set.key
      value = set.value
    }
  }
}

resource "null_resource" "delete_configmaps" {
  triggers = {
    namespace    = var.k8s_namespace
    release_name = local.helm_release_name
  }
  provisioner "local-exec" {
    when    = destroy
    command = "kubectl delete configmaps -n ${self.triggers.namespace} --selector=release=${self.triggers.release_name} || :"
  }
}

data "kubernetes_service" "db_main" {
  count      = (var.database_name != "" ? (local.create_rds ? 0 : 1) : 0)
  depends_on = [helm_release.main]
  metadata {
    name      = "${var.project_name}-${var.branch_name}-postgresql"
    namespace = var.k8s_namespace
  }
}
