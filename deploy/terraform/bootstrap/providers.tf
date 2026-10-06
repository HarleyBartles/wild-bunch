provider "aws" {
  region = var.aws_region
}

data "aws_caller_identity" "current" {}

data "aws_partition" "current" {}

locals {
  github_oidc_subject = "repo:${var.github_repository}:environment:${var.github_environment}"
  state_bucket_name   = "wild-bunch-terraform-state-${data.aws_caller_identity.current.account_id}-${var.aws_region}"
  state_bucket_arn    = "arn:${data.aws_partition.current.partition}:s3:::${local.state_bucket_name}"
  image_repository_arns = [
    for repository in var.image_repository_names :
    "arn:${data.aws_partition.current.partition}:ecr:${var.aws_region}:${data.aws_caller_identity.current.account_id}:repository/${repository}"
  ]
  environment_role_arns = [
    for role_name in local.environment_role_names :
    "arn:${data.aws_partition.current.partition}:iam::${data.aws_caller_identity.current.account_id}:role/${role_name}"
  ]
  environment_policy_arns = [
    "arn:${data.aws_partition.current.partition}:iam::${data.aws_caller_identity.current.account_id}:policy/wild-bunch-learning-*"
  ]
  environment_codebuild_project_arns = [
    "arn:${data.aws_partition.current.partition}:codebuild:${var.aws_region}:${data.aws_caller_identity.current.account_id}:project/wild-bunch-learning-codebuild-release",
    "arn:${data.aws_partition.current.partition}:codebuild:${var.aws_region}:${data.aws_caller_identity.current.account_id}:project/wild-bunch-learning-codebuild-database-init",
  ]
  environment_role_names = [
    "wild-bunch-learning-eks-cluster",
    "wild-bunch-learning-eks-node",
    "wild-bunch-learning-eks-vpc-cni",
    "wild-bunch-learning-codebuild-release",
    "wild-bunch-learning-codebuild-database-init",
  ]
  environment_state_key             = "environment/terraform.tfstate"
  environment_state_object          = "${local.state_bucket_arn}/${local.environment_state_key}"
  environment_lock_object           = "${local.environment_state_object}.tflock"
  bootstrap_state_key               = "bootstrap/terraform.tfstate"
  release_state_prefix              = "releases/"
  github_oidc_provider_arn          = var.existing_github_oidc_provider_arn != null ? var.existing_github_oidc_provider_arn : aws_iam_openid_connect_provider.github[0].arn
  github_oidc_provider_count        = var.existing_github_oidc_provider_arn == null ? 1 : 0
  github_oidc_provider_expected_arn = "arn:${data.aws_partition.current.partition}:iam::${data.aws_caller_identity.current.account_id}:oidc-provider/token.actions.githubusercontent.com"
  github_environment_trust_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid    = "GitHubActionsProtectedEnvironment"
      Effect = "Allow"
      Action = "sts:AssumeRoleWithWebIdentity"
      Principal = {
        Federated = local.github_oidc_provider_arn
      }
      Condition = {
        StringEquals = {
          "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
          "token.actions.githubusercontent.com:sub" = local.github_oidc_subject
        }
      }
    }]
  })
}
