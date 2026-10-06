mock_provider "aws" {}

override_data {
  target = data.aws_caller_identity.current
  values = {
    account_id = "123456789012"
    arn        = "arn:aws:iam::123456789012:role/environment-test"
    user_id    = "AIDATESTROLE"
  }
}

override_data {
  target = data.aws_partition.current
  values = { partition = "aws" }
}

override_data {
  target = data.aws_availability_zones.available
  values = { names = ["eu-north-1a", "eu-north-1b", "eu-north-1c"] }
}

override_resource {
  target = aws_iam_role.eks_cluster
  values = { id = "wild-bunch-learning-eks-cluster", name = "wild-bunch-learning-eks-cluster", arn = "arn:aws:iam::123456789012:role/wild-bunch-learning-eks-cluster" }
}

override_resource {
  target = aws_iam_role.eks_node
  values = { id = "wild-bunch-learning-eks-node", name = "wild-bunch-learning-eks-node", arn = "arn:aws:iam::123456789012:role/wild-bunch-learning-eks-node" }
}

override_resource {
  target = aws_iam_role.vpc_cni
  values = { id = "wild-bunch-learning-eks-vpc-cni", name = "wild-bunch-learning-eks-vpc-cni", arn = "arn:aws:iam::123456789012:role/wild-bunch-learning-eks-vpc-cni" }
}

override_resource {
  target = aws_iam_role.codebuild_release
  values = { id = "wild-bunch-learning-codebuild-release", name = "wild-bunch-learning-codebuild-release", arn = "arn:aws:iam::123456789012:role/wild-bunch-learning-codebuild-release" }
}

override_resource {
  target = aws_iam_role.codebuild_initializer
  values = { id = "wild-bunch-learning-codebuild-database-init", name = "wild-bunch-learning-codebuild-database-init", arn = "arn:aws:iam::123456789012:role/wild-bunch-learning-codebuild-database-init" }
}

override_resource {
  target = aws_iam_openid_connect_provider.cluster
  values = { arn = "arn:aws:iam::123456789012:oidc-provider/oidc.eks.eu-north-1.amazonaws.com/id/TEST" }
}

override_resource {
  target = aws_codeconnections_connection.github
  values = { arn = "arn:aws:codeconnections:eu-north-1:123456789012:connection/12345678-1234-1234-1234-123456789012" }
}

override_resource {
  target = aws_ecr_repository.frontend
  values = { arn = "arn:aws:ecr:eu-north-1:123456789012:repository/wild-bunch-frontend" }
}

override_resource {
  target = aws_ecr_repository.api
  values = { arn = "arn:aws:ecr:eu-north-1:123456789012:repository/wild-bunch-api" }
}

override_resource {
  target = aws_ecr_repository.migrations
  values = { arn = "arn:aws:ecr:eu-north-1:123456789012:repository/wild-bunch-migrations" }
}

override_resource {
  target = aws_secretsmanager_secret.runtime
  values = { arn = "arn:aws:secretsmanager:eu-north-1:123456789012:secret:wild-bunch-learning/runtime-ABC123" }
}

override_resource {
  target = aws_secretsmanager_secret.migration
  values = { arn = "arn:aws:secretsmanager:eu-north-1:123456789012:secret:wild-bunch-learning/migration-DEF456" }
}

override_resource {
  target = aws_db_instance.learning
  values = {
    master_user_secret = [{ secret_arn = "arn:aws:secretsmanager:eu-north-1:123456789012:secret:rds!db-TEST", secret_status = "active" }]
  }
}

override_resource {
  target = aws_nat_gateway.learning
  values = { id = "nat-0123456789abcdef0" }
}

override_resource {
  target = aws_launch_template.nodes
  values = { id = "lt-0123456789abcdef0", latest_version = 1 }
}

override_resource {
  target = aws_security_group.nodes
  values = { id = "sg-00000000000000001" }
}

override_resource {
  target = aws_security_group.codebuild_initializer
  values = { id = "sg-00000000000000002" }
}

override_resource {
  target = aws_security_group.codebuild_release
  values = { id = "sg-00000000000000003" }
}

override_resource {
  target = aws_eks_cluster.learning
  values = {
    arn        = "arn:aws:eks:eu-north-1:123456789012:cluster/wild-bunch-learning"
    identity   = [{ oidc = [{ issuer = "https://oidc.eks.eu-north-1.amazonaws.com/id/TEST" }] }]
    vpc_config = { cluster_security_group_id = "sg-00000000000000004", endpoint_private_access = true, endpoint_public_access = true, public_access_cidrs = ["203.0.113.7/32"], subnet_ids = ["subnet-00000000000000001", "subnet-00000000000000002"], vpc_id = "vpc-0123456789abcdef0" }
  }
}

override_resource {
  target = aws_eks_node_group.learning
  values = {
    instance_types = ["m7i.large"]
    scaling_config = { min_size = 1, desired_size = 1, max_size = 2 }
  }
}

override_resource {
  target = aws_codebuild_project.release_runner
  values = { vpc_config = { security_group_ids = ["sg-00000000000000003"], subnets = ["subnet-00000000000000001", "subnet-00000000000000002"], vpc_id = "vpc-0123456789abcdef0" } }
}

override_resource {
  target = aws_codebuild_project.database_initializer
  values = { vpc_config = { security_group_ids = ["sg-00000000000000002"], subnets = ["subnet-00000000000000001", "subnet-00000000000000002"], vpc_id = "vpc-0123456789abcdef0" } }
}

run "keeps_the_environment_private_and_routes_only_private_subnets_through_nat" {
  command = apply

  variables {
    state_bucket_name     = "wild-bunch-terraform-state-123456789012-eu-north-1"
    engineer_iam_role_arn = "arn:aws:iam::123456789012:role/learning-engineer"
    engineer_ipv4_cidr    = "203.0.113.7/32"
  }

  assert {
    condition     = aws_vpc.learning.cidr_block == "10.42.0.0/16"
    error_message = "The learning VPC must use its dedicated non-overlapping address range."
  }

  assert {
    condition     = length(aws_subnet.public) == 2 && length(aws_subnet.private) == 2 && aws_subnet.public[0].availability_zone != aws_subnet.public[1].availability_zone
    error_message = "Public and private subnets must span two distinct Availability Zones."
  }

  assert {
    condition     = alltrue([for subnet in aws_subnet.private : !subnet.map_public_ip_on_launch])
    error_message = "Private subnets must never assign public IPv4 addresses to launched instances."
  }

  assert {
    condition     = aws_route.private_egress[0].nat_gateway_id == aws_nat_gateway.learning.id && aws_route.private_egress[1].nat_gateway_id == aws_nat_gateway.learning.id
    error_message = "Both private subnets must use the single intentional NAT gateway."
  }

  assert {
    condition     = aws_eks_cluster.learning.vpc_config[0].endpoint_private_access
    error_message = "The EKS administration endpoint must be privately accessible."
  }

  assert {
    condition     = aws_eks_cluster.learning.vpc_config[0].endpoint_public_access
    error_message = "The EKS public administration endpoint must remain enabled for the engineer's restricted `/32`."
  }

  assert {
    condition     = toset(aws_eks_cluster.learning.vpc_config[0].public_access_cidrs) == toset([var.engineer_ipv4_cidr])
    error_message = "The EKS public endpoint must allow only the engineer's `/32`."
  }

  assert {
    condition     = aws_eks_cluster.learning.version == "1.36"
    error_message = "The cluster must use the agreed pinned Kubernetes minor."
  }

  assert {
    condition     = toset(aws_eks_node_group.learning.instance_types) == toset(["m7i.large"])
    error_message = "The managed worker group must use the agreed instance family."
  }

  assert {
    condition     = aws_eks_node_group.learning.scaling_config[0].min_size == 1 && aws_eks_node_group.learning.scaling_config[0].desired_size == 1 && aws_eks_node_group.learning.scaling_config[0].max_size == 2
    error_message = "The managed worker group must stay at one node with one replacement slot."
  }

  assert {
    condition     = aws_db_instance.learning.engine_version == "16.15" && aws_db_instance.learning.instance_class == "db.t4g.small" && aws_db_instance.learning.allocated_storage == 20 && aws_db_instance.learning.storage_encrypted && !aws_db_instance.learning.publicly_accessible && !aws_db_instance.learning.multi_az && aws_db_instance.learning.backup_retention_period == 0 && aws_db_instance.learning.skip_final_snapshot
    error_message = "RDS must remain an encrypted, private, single-AZ, deliberately disposable PostgreSQL instance."
  }

  assert {
    condition     = aws_vpc_security_group_ingress_rule.database_nodes.referenced_security_group_id == aws_security_group.nodes.id && aws_vpc_security_group_ingress_rule.database_initializer.referenced_security_group_id == aws_security_group.codebuild_initializer.id
    error_message = "PostgreSQL must accept traffic from workers and the initializer only, not the release runner."
  }

  assert {
    condition     = aws_ecr_repository.frontend.image_tag_mutability == "IMMUTABLE" && aws_ecr_repository.api.image_tag_mutability == "IMMUTABLE" && aws_ecr_repository.migrations.image_tag_mutability == "IMMUTABLE"
    error_message = "All three private application image repositories must reject mutable tags."
  }

  assert {
    condition     = aws_secretsmanager_secret.runtime.name == "wild-bunch-learning/runtime" && aws_secretsmanager_secret.migration.name == "wild-bunch-learning/migration" && aws_db_instance.learning.manage_master_user_password
    error_message = "Application secret containers must be separate from the RDS-managed administrator credential."
  }

  assert {
    condition     = !contains(flatten([for statement in jsondecode(aws_iam_role_policy.codebuild_release.policy).Statement : [statement.Action]]), "secretsmanager:PutSecretValue") && !contains(flatten([for statement in jsondecode(aws_iam_role_policy.codebuild_release.policy).Statement : [statement.Resource]]), aws_db_instance.learning.master_user_secret[0].secret_arn)
    error_message = "The release identity must neither change application credentials nor read the RDS administrator credential."
  }

  assert {
    condition     = contains(flatten([for statement in jsondecode(aws_iam_role_policy.codebuild_initializer.policy).Statement : [statement.Resource]]), aws_db_instance.learning.master_user_secret[0].secret_arn) && !contains(flatten([for statement in jsondecode(aws_iam_role_policy.codebuild_initializer.policy).Statement : [statement.Action]]), "eks:DescribeCluster")
    error_message = "Only the separate initializer identity may read the RDS administrator credential, and it has no EKS access."
  }

  assert {
    condition     = toset(aws_codebuild_project.release_runner.vpc_config[0].security_group_ids) == toset([aws_security_group.codebuild_release.id])
    error_message = "The private release runner must use its own security group."
  }

  assert {
    condition     = toset(aws_codebuild_project.database_initializer.vpc_config[0].security_group_ids) == toset([aws_security_group.codebuild_initializer.id])
    error_message = "The private database initializer must use a different security group."
  }

  assert {
    condition     = length(aws_codebuild_webhook.release_runner.filter_group) == 1
    error_message = "The release runner webhook must have one conjunctive trust filter group."
  }

  assert {
    condition     = aws_cloudwatch_log_group.eks.retention_in_days == 7 && aws_cloudwatch_log_group.release_runner.retention_in_days == 7 && aws_cloudwatch_log_group.database_initializer.retention_in_days == 7
    error_message = "AWS control-plane and private-runner logs must have bounded seven-day retention."
  }
}

run "rejects_an_open_eks_administration_endpoint" {
  command = plan

  variables {
    state_bucket_name     = "wild-bunch-terraform-state-123456789012-eu-north-1"
    engineer_iam_role_arn = "arn:aws:iam::123456789012:role/learning-engineer"
    engineer_ipv4_cidr    = "0.0.0.0/0"
  }

  expect_failures = [var.engineer_ipv4_cidr]
}

run "rejects_an_ipv6_administration_endpoint" {
  command = plan

  variables {
    state_bucket_name     = "wild-bunch-terraform-state-123456789012-eu-north-1"
    engineer_iam_role_arn = "arn:aws:iam::123456789012:role/learning-engineer"
    engineer_ipv4_cidr    = "2001:db8::/32"
  }

  expect_failures = [var.engineer_ipv4_cidr]
}

run "rejects_a_different_region" {
  command = plan

  variables {
    aws_region            = "us-east-1"
    state_bucket_name     = "wild-bunch-terraform-state-123456789012-eu-north-1"
    engineer_iam_role_arn = "arn:aws:iam::123456789012:role/learning-engineer"
    engineer_ipv4_cidr    = "203.0.113.7/32"
  }

  expect_failures = [var.aws_region]
}
