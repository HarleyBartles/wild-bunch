locals {
  codebuild_trust_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Action    = "sts:AssumeRole"
      Principal = { Service = "codebuild.amazonaws.com" }
    }]
  })

  codebuild_vpc_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "CreateInterfacesOnlyInExercisePrivateSubnets"
        Effect   = "Allow"
        Action   = "ec2:CreateNetworkInterface"
        Resource = "*"
        Condition = {
          ArnEquals = {
            "ec2:Vpc"           = "arn:${local.partition}:ec2:${local.region}:${local.account_id}:vpc/${aws_vpc.learning.id}"
            "ec2:Subnet"        = aws_subnet.private[*].arn
            "ec2:SecurityGroup" = [aws_security_group.codebuild_release.arn, aws_security_group.codebuild_initializer.arn]
          }
        }
      },
      {
        Sid      = "ManageAndDescribeRegionalCodeBuildInterfaces"
        Effect   = "Allow"
        Action   = ["ec2:DeleteNetworkInterface", "ec2:DescribeDhcpOptions", "ec2:DescribeNetworkInterfaces", "ec2:DescribeSecurityGroups", "ec2:DescribeSubnets", "ec2:DescribeVpcs"]
        Resource = "*"
        Condition = {
          StringEquals = { "aws:RequestedRegion" = local.region }
        }
      },
      {
        Sid      = "AuthorizeCodeBuildOnlyForPrivateSubnets"
        Effect   = "Allow"
        Action   = "ec2:CreateNetworkInterfacePermission"
        Resource = "arn:${local.partition}:ec2:${local.region}:${local.account_id}:network-interface/*"
        Condition = {
          StringEquals = { "ec2:AuthorizedService" = "codebuild.amazonaws.com" }
          ArnEquals    = { "ec2:Subnet" = aws_subnet.private[*].arn }
        }
      },
    ]
  })

  codebuild_release_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "RetrieveOnlyApplicationCredentials"
        Effect   = "Allow"
        Action   = ["secretsmanager:DescribeSecret", "secretsmanager:GetSecretValue"]
        Resource = [aws_secretsmanager_secret.runtime.arn, aws_secretsmanager_secret.migration.arn]
      },
      {
        Sid      = "InspectAndPullOnlyApplicationImages"
        Effect   = "Allow"
        Action   = ["ecr:BatchCheckLayerAvailability", "ecr:BatchGetImage", "ecr:DescribeImages", "ecr:GetDownloadUrlForLayer"]
        Resource = local.image_repository_arns
      },
      {
        Sid      = "AuthorizePrivateRegistryPull"
        Effect   = "Allow"
        Action   = "ecr:GetAuthorizationToken"
        Resource = "*"
      },
      {
        Sid      = "DescribeOnlyLearningCluster"
        Effect   = "Allow"
        Action   = "eks:DescribeCluster"
        Resource = aws_eks_cluster.learning.arn
      },
      {
        Sid      = "WriteOnlyPrivateReleaseManifests"
        Effect   = "Allow"
        Action   = ["s3:GetObject", "s3:PutObject"]
        Resource = "arn:${local.partition}:s3:::${var.state_bucket_name}/releases/*"
      },
      {
        Sid      = "ListOnlyPrivateReleasePrefix"
        Effect   = "Allow"
        Action   = "s3:ListBucket"
        Resource = "arn:${local.partition}:s3:::${var.state_bucket_name}"
        Condition = {
          StringLike = { "s3:prefix" = "releases/*" }
        }
      },
      {
        Sid      = "WriteReleaseBuildLogs"
        Effect   = "Allow"
        Action   = ["logs:CreateLogStream", "logs:PutLogEvents"]
        Resource = "arn:${local.partition}:logs:${local.region}:${local.account_id}:log-group:/aws/codebuild/${local.role_names.release}:log-stream:*"
      },
      {
        Sid      = "UseTheAuthorizedGitHubConnection"
        Effect   = "Allow"
        Action   = "codeconnections:UseConnection"
        Resource = aws_codeconnections_connection.github.arn
        Condition = {
          StringEquals = {
            "codeconnections:FullRepositoryId"            = "HarleyBartles/wild-bunch"
            "codeconnections:OwnerId"                     = "HarleyBartles"
            "codeconnections:RepositoryName"              = "wild-bunch"
            "codeconnections:BranchName"                  = "main"
            "codeconnections:ProviderAction"              = "GitPull"
            "codeconnections:ProviderPermissionsRequired" = "read_only"
          }
        }
      },
    ]
  })

  codebuild_initializer_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "ReadAdministratorAndEstablishedApplicationCredentials"
        Effect   = "Allow"
        Action   = ["secretsmanager:DescribeSecret", "secretsmanager:GetSecretValue"]
        Resource = [aws_db_instance.learning.master_user_secret[0].secret_arn, aws_secretsmanager_secret.runtime.arn, aws_secretsmanager_secret.migration.arn]
      },
      {
        Sid      = "CreateAndUpdateOnlyApplicationCredentialValues"
        Effect   = "Allow"
        Action   = "secretsmanager:PutSecretValue"
        Resource = [aws_secretsmanager_secret.runtime.arn, aws_secretsmanager_secret.migration.arn]
      },
      {
        Sid      = "WriteInitializerBuildLogs"
        Effect   = "Allow"
        Action   = ["logs:CreateLogStream", "logs:PutLogEvents"]
        Resource = "arn:${local.partition}:logs:${local.region}:${local.account_id}:log-group:/aws/codebuild/${local.role_names.initializer}:log-stream:*"
      },
      {
        Sid      = "UseTheAuthorizedGitHubConnection"
        Effect   = "Allow"
        Action   = "codeconnections:UseConnection"
        Resource = aws_codeconnections_connection.github.arn
        Condition = {
          StringEquals = {
            "codeconnections:FullRepositoryId"            = "HarleyBartles/wild-bunch"
            "codeconnections:OwnerId"                     = "HarleyBartles"
            "codeconnections:RepositoryName"              = "wild-bunch"
            "codeconnections:BranchName"                  = "main"
            "codeconnections:ProviderAction"              = "GitPull"
            "codeconnections:ProviderPermissionsRequired" = "read_only"
          }
        }
      },
    ]
  })
}

resource "aws_iam_role" "codebuild_release" {
  name               = local.role_names.release
  assume_role_policy = local.codebuild_trust_policy
  tags               = local.tags
}

resource "aws_iam_role" "codebuild_initializer" {
  name               = local.role_names.initializer
  assume_role_policy = local.codebuild_trust_policy
  tags               = local.tags
}

resource "aws_iam_role_policy" "codebuild_release_vpc" {
  name   = "private-vpc-network-interfaces"
  role   = aws_iam_role.codebuild_release.id
  policy = local.codebuild_vpc_policy
}

resource "aws_iam_role_policy" "codebuild_initializer_vpc" {
  name   = "private-vpc-network-interfaces"
  role   = aws_iam_role.codebuild_initializer.id
  policy = local.codebuild_vpc_policy
}

resource "aws_iam_role_policy" "codebuild_release" {
  name   = "release-only-exercise-access"
  role   = aws_iam_role.codebuild_release.id
  policy = local.codebuild_release_policy
}

resource "aws_iam_role_policy" "codebuild_initializer" {
  name   = "database-initialization-only-access"
  role   = aws_iam_role.codebuild_initializer.id
  policy = local.codebuild_initializer_policy
}
