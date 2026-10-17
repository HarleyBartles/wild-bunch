provider "aws" {
  region = var.aws_region
}

data "aws_caller_identity" "current" {}
data "aws_partition" "current" {}
data "aws_availability_zones" "available" {
  state = "available"
}

locals {
  owner_id           = "wild-bunch-learning"
  namespace          = "wild-bunch-learning"
  cluster_name       = "wild-bunch-learning"
  region             = var.aws_region
  account_id         = data.aws_caller_identity.current.account_id
  partition          = data.aws_partition.current.partition
  availability_zones = slice(sort(data.aws_availability_zones.available.names), 0, 2)

  tags = {
    Project   = local.owner_id
    OwnerId   = local.owner_id
    ManagedBy = "terraform"
  }

  role_names = {
    cluster     = "wild-bunch-learning-eks-cluster"
    node        = "wild-bunch-learning-eks-node"
    vpc_cni     = "wild-bunch-learning-eks-vpc-cni"
    release     = "wild-bunch-learning-codebuild-release"
    initializer = "wild-bunch-learning-codebuild-database-init"
  }

  image_repositories = {
    frontend   = aws_ecr_repository.frontend.repository_url
    api        = aws_ecr_repository.api.repository_url
    migrations = aws_ecr_repository.migrations.repository_url
  }

  image_repository_arns = [
    aws_ecr_repository.frontend.arn,
    aws_ecr_repository.api.arn,
    aws_ecr_repository.migrations.arn,
  ]
}
