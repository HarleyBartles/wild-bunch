resource "aws_iam_openid_connect_provider" "github" {
  count = local.github_oidc_provider_count

  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]
}

resource "aws_iam_role" "image_publisher" {
  name                 = "wild-bunch-learning-image-publisher"
  description          = "Publishes only Wild Bunch learning images from the protected GitHub environment."
  assume_role_policy   = local.github_environment_trust_policy
  max_session_duration = 3600
  tags = {
    Project   = "wild-bunch-learning"
    ManagedBy = "terraform"
  }

  lifecycle {
    precondition {
      condition     = var.existing_github_oidc_provider_arn == null || var.existing_github_oidc_provider_arn == local.github_oidc_provider_expected_arn
      error_message = "The external GitHub OIDC provider must belong to this account and use token.actions.githubusercontent.com."
    }
  }
}

locals {
  image_publisher_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "EcrAuthorizationToken"
        Effect   = "Allow"
        Action   = "ecr:GetAuthorizationToken"
        Resource = "*"
      },
      {
        Sid    = "PublishOnlyLearningImages"
        Effect = "Allow"
        Action = [
          "ecr:BatchCheckLayerAvailability",
          "ecr:CompleteLayerUpload",
          "ecr:InitiateLayerUpload",
          "ecr:PutImage",
          "ecr:UploadLayerPart",
        ]
        Resource = local.image_repository_arns
      }
    ]
  })
}

resource "aws_iam_role_policy" "image_publisher" {
  name   = "publish-three-learning-images"
  role   = aws_iam_role.image_publisher.id
  policy = local.image_publisher_policy
}

resource "aws_iam_role" "environment_provisioner" {
  name                 = "wild-bunch-learning-infrastructure"
  description          = "Manages the exercise Terraform environment and its separately named service roles."
  assume_role_policy   = local.github_environment_trust_policy
  max_session_duration = 3600
  tags = {
    Project   = "wild-bunch-learning"
    ManagedBy = "terraform"
  }
}

data "aws_iam_policy_document" "environment_provisioner" {
  statement {
    sid    = "ListOnlyEnvironmentStateKey"
    effect = "Allow"
    actions = [
      "s3:ListBucket",
    ]
    resources = [local.state_bucket_arn]
    condition {
      test     = "StringLike"
      variable = "s3:prefix"
      values = [
        local.environment_state_key,
        "${local.environment_state_key}.tflock",
      ]
    }
  }

  statement {
    sid    = "ReadWriteEnvironmentState"
    effect = "Allow"
    actions = [
      "s3:GetObject",
      "s3:PutObject",
    ]
    resources = [local.environment_state_object]
  }

  statement {
    sid       = "LockOnlyEnvironmentState"
    effect    = "Allow"
    actions   = ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"]
    resources = [local.environment_lock_object]
  }

  statement {
    sid    = "VpcAndNetworkLifecycleInSelectedRegion"
    effect = "Allow"
    actions = [
      "ec2:AllocateAddress",
      "ec2:AssociateRouteTable",
      "ec2:AssociateVpcCidrBlock",
      "ec2:AttachInternetGateway",
      "ec2:AuthorizeSecurityGroupEgress",
      "ec2:AuthorizeSecurityGroupIngress",
      "ec2:CreateInternetGateway",
      "ec2:CreateNatGateway",
      "ec2:CreateRoute",
      "ec2:CreateRouteTable",
      "ec2:CreateSecurityGroup",
      "ec2:CreateSubnet",
      "ec2:CreateTags",
      "ec2:CreateVpc",
      "ec2:DeleteInternetGateway",
      "ec2:DeleteNatGateway",
      "ec2:DeleteRoute",
      "ec2:DeleteRouteTable",
      "ec2:DeleteSecurityGroup",
      "ec2:DeleteSubnet",
      "ec2:DeleteTags",
      "ec2:DeleteVpc",
      "ec2:DescribeAddresses",
      "ec2:DescribeAvailabilityZones",
      "ec2:DescribeInternetGateways",
      "ec2:DescribeNatGateways",
      "ec2:DescribeNetworkInterfaces",
      "ec2:DescribeRouteTables",
      "ec2:DescribeSecurityGroups",
      "ec2:DescribeSubnets",
      "ec2:DescribeTags",
      "ec2:DescribeVpcs",
      "ec2:DetachInternetGateway",
      "ec2:DisassociateRouteTable",
      "ec2:DisassociateVpcCidrBlock",
      "ec2:ModifySubnetAttribute",
      "ec2:ModifyVpcAttribute",
      "ec2:ReleaseAddress",
      "ec2:ReplaceRoute",
      "ec2:RevokeSecurityGroupEgress",
      "ec2:RevokeSecurityGroupIngress",
    ]
    resources = ["*"]
    condition {
      test     = "StringEquals"
      variable = "aws:RequestedRegion"
      values   = [var.aws_region]
    }
  }

  statement {
    sid    = "EksClusterNodegroupAddonAndAccessLifecycle"
    effect = "Allow"
    actions = [
      "eks:AssociateAccessPolicy",
      "eks:CreateAccessEntry",
      "eks:CreateAddon",
      "eks:CreateCluster",
      "eks:CreateNodegroup",
      "eks:DeleteAccessEntry",
      "eks:DeleteAddon",
      "eks:DeleteCluster",
      "eks:DeleteNodegroup",
      "eks:DescribeAccessEntry",
      "eks:DescribeAddon",
      "eks:DescribeAddonVersions",
      "eks:DescribeCluster",
      "eks:DescribeNodegroup",
      "eks:DescribeUpdate",
      "eks:DisassociateAccessPolicy",
      "eks:ListAccessEntries",
      "eks:ListAddons",
      "eks:ListAssociatedAccessPolicies",
      "eks:ListClusters",
      "eks:ListNodegroups",
      "eks:ListUpdates",
      "eks:TagResource",
      "eks:UntagResource",
      "eks:UpdateClusterConfig",
    ]
    resources = ["*"]
    condition {
      test     = "StringEquals"
      variable = "aws:RequestedRegion"
      values   = [var.aws_region]
    }
  }

  statement {
    sid    = "RdsAndSecretMetadataLifecycle"
    effect = "Allow"
    actions = [
      "rds:AddTagsToResource",
      "rds:CreateDBInstance",
      "rds:CreateDBParameterGroup",
      "rds:CreateDBSubnetGroup",
      "rds:DeleteDBInstance",
      "rds:DeleteDBParameterGroup",
      "rds:DeleteDBSubnetGroup",
      "rds:DescribeDBEngineVersions",
      "rds:DescribeDBInstances",
      "rds:DescribeDBParameterGroups",
      "rds:DescribeDBSubnetGroups",
      "rds:DescribeOrderableDBInstanceOptions",
      "rds:ListTagsForResource",
      "rds:ModifyDBInstance",
      "rds:ModifyDBParameterGroup",
      "rds:RemoveTagsFromResource",
      "secretsmanager:CreateSecret",
      "secretsmanager:DeleteSecret",
      "secretsmanager:DescribeSecret",
      "secretsmanager:ListSecretVersionIds",
      "secretsmanager:TagResource",
      "secretsmanager:UntagResource",
    ]
    resources = ["*"]
    condition {
      test     = "StringEquals"
      variable = "aws:RequestedRegion"
      values   = [var.aws_region]
    }
  }

  statement {
    sid    = "ImageRegistryRepositoryLifecycle"
    effect = "Allow"
    actions = [
      "ecr:CreateRepository",
      "ecr:DeleteLifecyclePolicy",
      "ecr:DeleteRepository",
      "ecr:DeleteRepositoryPolicy",
      "ecr:DescribeRepositories",
      "ecr:GetLifecyclePolicy",
      "ecr:GetRepositoryPolicy",
      "ecr:ListTagsForResource",
      "ecr:PutImageTagMutability",
      "ecr:PutLifecyclePolicy",
      "ecr:SetRepositoryPolicy",
      "ecr:TagResource",
      "ecr:UntagResource",
    ]
    resources = ["*"]
    condition {
      test     = "StringEquals"
      variable = "aws:RequestedRegion"
      values   = [var.aws_region]
    }
  }

  statement {
    sid    = "CodeBuildAndGitHubConnectionLifecycle"
    effect = "Allow"
    actions = [
      "codebuild:BatchGetProjects",
      "codebuild:BatchGetReportGroups",
      "codebuild:CreateProject",
      "codebuild:CreateWebhook",
      "codebuild:DeleteProject",
      "codebuild:DeleteWebhook",
      "codebuild:DescribeCodeCoverages",
      "codebuild:DescribeTestCases",
      "codebuild:ListBuildsForProject",
      "codebuild:UpdateProject",
      "codebuild:UpdateWebhook",
      "codeconnections:CreateConnection",
      "codeconnections:DeleteConnection",
      "codeconnections:GetConnection",
      "codeconnections:ListConnections",
      "codeconnections:TagResource",
      "codeconnections:UntagResource",
    ]
    resources = ["*"]
    condition {
      test     = "StringEquals"
      variable = "aws:RequestedRegion"
      values   = [var.aws_region]
    }
  }

  statement {
    sid       = "PassOnlyRegionalCodeConnectionsToCodeBuild"
    effect    = "Allow"
    actions   = ["codeconnections:PassConnection"]
    resources = ["arn:${data.aws_partition.current.partition}:codeconnections:${var.aws_region}:${data.aws_caller_identity.current.account_id}:connection/*"]
  }

  statement {
    sid    = "ControlPlaneLogGroupLifecycle"
    effect = "Allow"
    actions = [
      "logs:CreateLogGroup",
      "logs:DeleteLogGroup",
      "logs:DeleteRetentionPolicy",
      "logs:DescribeLogGroups",
      "logs:ListTagsForResource",
      "logs:PutRetentionPolicy",
      "logs:TagResource",
      "logs:UntagResource",
    ]
    resources = ["*"]
    condition {
      test     = "StringEquals"
      variable = "aws:RequestedRegion"
      values   = [var.aws_region]
    }
  }

  statement {
    sid    = "CreateAndMaintainNamedExerciseRoles"
    effect = "Allow"
    actions = [
      "iam:AttachRolePolicy",
      "iam:CreatePolicy",
      "iam:CreatePolicyVersion",
      "iam:CreateRole",
      "iam:DeletePolicy",
      "iam:DeletePolicyVersion",
      "iam:DeleteRole",
      "iam:DeleteRolePolicy",
      "iam:DetachRolePolicy",
      "iam:GetPolicy",
      "iam:GetPolicyVersion",
      "iam:GetRole",
      "iam:GetRolePolicy",
      "iam:ListAttachedRolePolicies",
      "iam:ListPolicyVersions",
      "iam:ListRoleTags",
      "iam:PassRole",
      "iam:PutRolePolicy",
      "iam:TagRole",
      "iam:UntagRole",
      "iam:UpdateAssumeRolePolicy",
    ]
    resources = concat(local.environment_role_arns, local.environment_policy_arns)
  }

  statement {
    sid       = "CreateOnlyRequiredServiceLinkedRoles"
    effect    = "Allow"
    actions   = ["iam:CreateServiceLinkedRole"]
    resources = ["*"]
    condition {
      test     = "StringEquals"
      variable = "iam:AWSServiceName"
      values   = ["eks.amazonaws.com", "eks-nodegroup.amazonaws.com", "autoscaling.amazonaws.com", "rds.amazonaws.com"]
    }
  }
}

resource "aws_iam_role_policy" "environment_provisioner" {
  name   = "provision-exercise-environment"
  role   = aws_iam_role.environment_provisioner.id
  policy = data.aws_iam_policy_document.environment_provisioner.json
}
