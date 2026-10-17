resource "aws_secretsmanager_secret" "runtime" {
  name                    = "wild-bunch-learning/runtime"
  description             = "Wild Bunch application database identity. Value is created by the private database initializer."
  recovery_window_in_days = 0
  tags                    = merge(local.tags, { Purpose = "application-runtime-credential" })
}

resource "aws_secretsmanager_secret" "migration" {
  name                    = "wild-bunch-learning/migration"
  description             = "Wild Bunch schema migration database identity. Value is created by the private database initializer."
  recovery_window_in_days = 0
  tags                    = merge(local.tags, { Purpose = "application-migration-credential" })
}
