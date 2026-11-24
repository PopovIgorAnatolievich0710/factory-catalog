locals {
  encrypted-secrets = (var.deploy_env == "prod" ? local.encrypted-secrets-prod : local.encrypted-secrets-dev)
  encrypted-secrets-dev = {
    "POSTGRESQL_ADMIN_USERNAME"     = "REPLACE_WITH_KMS_CIPHERTEXT"
    "POSTGRESQL_ADMIN_PASSWORD"     = "REPLACE_WITH_KMS_CIPHERTEXT"
    "DB_PASSWORD_MIGRATIONS"        = "REPLACE_WITH_KMS_CIPHERTEXT"
    "API_SERVICE_USER_PASSWORD"     = "REPLACE_WITH_KMS_CIPHERTEXT"
    "DD_API_KEY"                    = "REPLACE_WITH_KMS_CIPHERTEXT"
    "MASTER_CATALOG_PASSWORD"       = "REPLACE_WITH_KMS_CIPHERTEXT"
    "SHIPMENT_SERVICE_API_PASSWORD" = "REPLACE_WITH_KMS_CIPHERTEXT"
  }
  encrypted-secrets-prod = {
    "POSTGRESQL_ADMIN_USERNAME"     = "REPLACE_WITH_KMS_CIPHERTEXT"
    "POSTGRESQL_ADMIN_PASSWORD"     = "REPLACE_WITH_KMS_CIPHERTEXT"
    "DB_PASSWORD_MIGRATIONS"        = "REPLACE_WITH_KMS_CIPHERTEXT"
    "API_SERVICE_USER_PASSWORD"     = "REPLACE_WITH_KMS_CIPHERTEXT"
    "DD_API_KEY"                    = "REPLACE_WITH_KMS_CIPHERTEXT"
    "MASTER_CATALOG_PASSWORD"       = "REPLACE_WITH_KMS_CIPHERTEXT"
    "SHIPMENT_SERVICE_API_PASSWORD" = "REPLACE_WITH_KMS_CIPHERTEXT"
  }
}

data "aws_kms_key" "encryption_key" {
  key_id = "alias/${var.kms_key_alias}"
}

data "aws_kms_secrets" "env_vars" {
  dynamic "secret" {
    for_each = local.encrypted-secrets
    content {
      name    = secret.key
      payload = secret.value
    }
  }
}
