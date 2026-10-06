output "state_bucket_name" {
  description = "Versioned, encrypted and private S3 bucket for the two Terraform state keys and release manifests."
  value       = aws_s3_bucket.state.bucket
}

output "bootstrap_state_key" {
  description = "Backend key for bootstrap state after it is migrated from protected local storage."
  value       = local.bootstrap_state_key
}

output "environment_state_key" {
  description = "Backend key available only to the environment provisioning role."
  value       = local.environment_state_key
}

output "release_state_prefix" {
  description = "Private S3 prefix reserved for non-secret immutable release manifests."
  value       = local.release_state_prefix
}

output "github_oidc_subject" {
  description = "Exact GitHub OIDC subject trusted by the two separate workflow roles."
  value       = local.github_oidc_subject
}

output "github_oidc_provider_arn" {
  description = "Managed GitHub OIDC provider, or the externally owned provider ARN supplied after inspection."
  value       = local.github_oidc_provider_arn
}

output "image_publisher_role_arn" {
  description = "GitHub Actions role limited to pushing the three immutable application repositories."
  value       = aws_iam_role.image_publisher.arn
}

output "environment_provisioner_role_arn" {
  description = "GitHub Actions role for the separate environment Terraform root."
  value       = aws_iam_role.environment_provisioner.arn
}

output "database_initializer_invoker_role_arn" {
  description = "GitHub Actions role limited to starting and polling the private database initializer project."
  value       = aws_iam_role.database_initializer_invoker.arn
}
