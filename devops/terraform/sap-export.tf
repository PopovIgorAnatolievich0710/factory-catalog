locals {
  sap_export_bucket_name = "tg-${var.project_name}-${local.resource_env}"
}

resource "aws_s3_bucket" "sap_export" {
  lifecycle {
    prevent_destroy = true
  }
  count  = local.managed_env ? 1 : 0
  bucket = local.sap_export_bucket_name
  tags   = var.default_tags
}

resource "aws_s3_bucket_public_access_block" "sap_export" {
  count  = local.managed_env ? 1 : 0
  bucket = aws_s3_bucket.sap_export[0].id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "sap_export" {
  count  = local.managed_env ? 1 : 0
  bucket = aws_s3_bucket.sap_export[0].id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_versioning" "sap_export" {
  count  = local.managed_env ? 1 : 0
  bucket = aws_s3_bucket.sap_export[0].id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_policy" "sap_export" {
  count      = local.managed_env ? 1 : 0
  bucket     = aws_s3_bucket.sap_export[0].id
  depends_on = [aws_s3_bucket_public_access_block.sap_export]

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = concat(
      [
        {
          Sid       = "AllowServiceRoleReadExport"
          Effect    = "Allow"
          Principal = { AWS = local.service_role_arn }
          Action    = "s3:GetObject"
          Resource  = "${aws_s3_bucket.sap_export[0].arn}/${var.sap_export_prefix}/*"
        },
        {
          Sid       = "AllowServiceRoleListExport"
          Effect    = "Allow"
          Principal = { AWS = local.service_role_arn }
          Action = [
            "s3:ListBucket",
            "s3:GetBucketLocation"
          ]
          Resource = aws_s3_bucket.sap_export[0].arn
        }
      ],
      length(var.sap_export_writer_arns) == 0 ? [] : [
        {
          Sid       = "AllowSapExportWriters"
          Effect    = "Allow"
          Principal = { AWS = var.sap_export_writer_arns }
          Action    = "s3:PutObject"
          Resource  = "${aws_s3_bucket.sap_export[0].arn}/${var.sap_export_prefix}/*"
        }
      ]
    )
  })
}
