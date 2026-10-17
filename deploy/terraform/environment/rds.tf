resource "aws_db_subnet_group" "learning" {
  name       = "wild-bunch-learning"
  subnet_ids = aws_subnet.private[*].id
  tags       = merge(local.tags, { Name = "wild-bunch-learning" })
}

resource "aws_db_parameter_group" "postgres" {
  name        = "wild-bunch-learning-postgres16"
  family      = "postgres16"
  description = "Require TLS for Wild Bunch learning database connections."
  parameter {
    name         = "rds.force_ssl"
    value        = "1"
    apply_method = "pending-reboot"
  }
  tags = local.tags
}

resource "aws_db_instance" "learning" {
  identifier                  = "wild-bunch-learning"
  engine                      = "postgres"
  engine_version              = "16.15"
  instance_class              = "db.t4g.small"
  allocated_storage           = 20
  storage_type                = "gp3"
  storage_encrypted           = true
  username                    = "wild_bunch_admin"
  manage_master_user_password = true
  port                        = 5432
  db_subnet_group_name        = aws_db_subnet_group.learning.name
  vpc_security_group_ids      = [aws_security_group.database.id]
  parameter_group_name        = aws_db_parameter_group.postgres.name
  publicly_accessible         = false
  multi_az                    = false
  backup_retention_period     = 0
  deletion_protection         = false
  skip_final_snapshot         = true
  delete_automated_backups    = true
  apply_immediately           = true
  auto_minor_version_upgrade  = false
  copy_tags_to_snapshot       = true
  tags                        = merge(local.tags, { Name = "wild-bunch-learning" })

  depends_on = [aws_vpc_security_group_ingress_rule.database_nodes, aws_vpc_security_group_ingress_rule.database_initializer]
}

