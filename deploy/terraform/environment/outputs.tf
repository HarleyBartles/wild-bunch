output "environment_contract" {
  description = "Non-secret inputs used by the release and inventory tools; export outside Git."
  value = jsonencode({
    region                 = local.region
    owner_id               = local.owner_id
    cluster_name           = aws_eks_cluster.learning.name
    namespace              = local.namespace
    kubernetes_context     = aws_eks_cluster.learning.arn
    image_repositories     = local.image_repositories
    runtime_secret_arn     = aws_secretsmanager_secret.runtime.arn
    migration_secret_arn   = aws_secretsmanager_secret.migration.arn
    admin_secret_arn       = aws_db_instance.learning.master_user_secret[0].secret_arn
    database_host          = aws_db_instance.learning.address
    database_port          = aws_db_instance.learning.port
    database_ca_identifier = aws_db_instance.learning.ca_cert_identifier
    initializer_project    = aws_codebuild_project.database_initializer.name
    release_project        = aws_codebuild_project.release_runner.name
    release_bucket         = var.state_bucket_name
    release_prefix         = "releases/"
  })
}

output "github_connection_arn" {
  description = "GitHub App connection that requires one-time human authorization in the AWS console."
  value       = aws_codeconnections_connection.github.arn
}

output "cluster_security_group_id" {
  description = "EKS-managed control-plane security group for read-only topology inspection."
  value       = aws_eks_cluster.learning.vpc_config[0].cluster_security_group_id
}

output "private_subnet_ids" {
  description = "Worker, database and private CodeBuild subnets."
  value       = aws_subnet.private[*].id
}
