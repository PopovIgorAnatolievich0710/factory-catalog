resource "aws_security_group" "rds-sg" {
  count  = local.create_rds ? 1 : 0
  name   = "${var.project_name}-${var.deploy_env}-rds-sg"
  vpc_id = var.rds_vpc_id
  tags   = var.default_tags
}

resource "aws_security_group_rule" "rds_inbound_access" {
  count             = local.create_rds ? 1 : 0
  from_port         = 5432
  protocol          = "tcp"
  security_group_id = aws_security_group.rds-sg[count.index].id
  to_port           = 5432
  type              = "ingress"
  cidr_blocks       = ["10.0.0.0/8", "192.168.0.0/16"]
}

resource "aws_db_instance" "rds-db" {
  lifecycle {
    ignore_changes = [
      snapshot_identifier,
      username,
      password
    ]
  }
  count                       = local.create_rds ? 1 : 0
  identifier_prefix           = "${var.project_name}-${var.deploy_env}"
  allocated_storage           = (var.deploy_env == "prod" ? 80 : 20)
  max_allocated_storage       = (var.deploy_env == "prod" ? 200 : null)
  storage_type                = "gp2"
  engine                      = "postgres"
  engine_version              = "15.17"
  instance_class              = var.rds_instance_class
  db_name                     = var.database_name
  username                    = data.aws_kms_secrets.env_vars.plaintext["POSTGRESQL_ADMIN_USERNAME"]
  password                    = data.aws_kms_secrets.env_vars.plaintext["POSTGRESQL_ADMIN_PASSWORD"]
  parameter_group_name        = "default.postgres15"
  db_subnet_group_name        = "default-${var.rds_vpc_id}"
  vpc_security_group_ids      = [aws_security_group.rds-sg[count.index].id]
  allow_major_version_upgrade = false
  auto_minor_version_upgrade  = true
  backup_retention_period     = (var.deploy_env == "prod" ? 35 : 7)
  copy_tags_to_snapshot       = true
  backup_window               = "04:00-05:00"
  maintenance_window          = "Sat:00:00-Sat:03:00"
  multi_az                    = var.deploy_env == "prod"
  skip_final_snapshot         = true
  deletion_protection         = true
  apply_immediately           = true
  tags                        = var.default_tags
}

data "aws_route53_zone" "zone" {
  name         = local.rds_aws_route53_zone
  private_zone = false
}

resource "aws_route53_record" "rds_route53_record" {
  count   = local.create_rds ? 1 : 0
  zone_id = data.aws_route53_zone.zone.zone_id
  name    = "${var.project_name}-${var.deploy_env}-db-rds"
  type    = "CNAME"
  ttl     = "60"
  records = [aws_db_instance.rds-db[count.index].address]
}

resource "local_file" "db_init_script" {
  count = var.database_name != "" ? 1 : 0
  content = templatefile("../helm/chart/configs/db_init_script.sql.tpl", {
    database_name          = var.database_name
    db_password_migrations = data.aws_kms_secrets.env_vars.plaintext["DB_PASSWORD_MIGRATIONS"]
  })
  filename = "../helm/chart/configs/db_init_script.sql"
}

resource "null_resource" "db_init_script" {
  count = local.create_rds ? 1 : 0
  triggers = {
    always_run = timestamp()
  }
  provisioner "local-exec" {
    command = "apk --update add postgresql-client && export PGPASSWORD='${data.aws_kms_secrets.env_vars.plaintext["POSTGRESQL_ADMIN_PASSWORD"]}' && psql -v ON_ERROR_STOP=1 -h '${local.database_host_for_ext_access}' -U '${data.aws_kms_secrets.env_vars.plaintext["POSTGRESQL_ADMIN_USERNAME"]}' -d '${var.database_name}' -f ${local_file.db_init_script[0].filename}"
  }
}
