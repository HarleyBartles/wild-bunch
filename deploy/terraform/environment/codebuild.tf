resource "aws_codeconnections_connection" "github" {
  name          = "wild-bunch-learning-github"
  provider_type = "GitHub"
  tags          = local.tags
}

resource "aws_cloudwatch_log_group" "release_runner" {
  name              = "/aws/codebuild/${local.role_names.release}"
  retention_in_days = 7
  tags              = local.tags
}

resource "aws_cloudwatch_log_group" "database_initializer" {
  name              = "/aws/codebuild/${local.role_names.initializer}"
  retention_in_days = 7
  tags              = local.tags
}

resource "aws_codebuild_project" "release_runner" {
  name                   = local.role_names.release
  description            = "Private GitHub Actions runner for trusted Wild Bunch application releases."
  service_role           = aws_iam_role.codebuild_release.arn
  build_timeout          = 60
  queued_timeout         = 10
  concurrent_build_limit = 1
  badge_enabled          = false
  tags                   = local.tags

  artifacts { type = "NO_ARTIFACTS" }

  environment {
    compute_type                = "BUILD_GENERAL1_MEDIUM"
    image                       = "aws/codebuild/amazonlinux-x86_64-standard:5.0"
    type                        = "LINUX_CONTAINER"
    image_pull_credentials_type = "CODEBUILD"
    privileged_mode             = false
  }

  source {
    type            = "GITHUB"
    location        = "https://github.com/HarleyBartles/wild-bunch.git"
    buildspec       = ""
    git_clone_depth = 0
    auth {
      type     = "CODECONNECTIONS"
      resource = aws_codeconnections_connection.github.arn
    }
  }

  vpc_config {
    vpc_id             = aws_vpc.learning.id
    subnets            = aws_subnet.private[*].id
    security_group_ids = [aws_security_group.codebuild_release.id]
  }

  logs_config {
    cloudwatch_logs {
      status      = "ENABLED"
      group_name  = aws_cloudwatch_log_group.release_runner.name
      stream_name = "runner"
    }
    s3_logs { status = "DISABLED" }
  }

  depends_on = [aws_iam_role_policy.codebuild_release_vpc, aws_iam_role_policy.codebuild_release]
}

resource "aws_codebuild_webhook" "release_runner" {
  project_name = aws_codebuild_project.release_runner.name
  build_type   = "BUILD"

  filter_group {
    filter {
      type    = "EVENT"
      pattern = "WORKFLOW_JOB_QUEUED"
    }
    filter {
      type    = "HEAD_REF"
      pattern = "^refs/heads/main$"
    }
    filter {
      type    = "WORKFLOW_NAME"
      pattern = "^Wild Bunch Learning Release$"
    }
  }
}

resource "aws_codebuild_project" "database_initializer" {
  name                   = local.role_names.initializer
  description            = "Private, separately invoked Wild Bunch database identity initialization."
  service_role           = aws_iam_role.codebuild_initializer.arn
  build_timeout          = 45
  queued_timeout         = 10
  concurrent_build_limit = 1
  badge_enabled          = false
  tags                   = local.tags

  artifacts { type = "NO_ARTIFACTS" }

  environment {
    compute_type                = "BUILD_GENERAL1_SMALL"
    image                       = "aws/codebuild/amazonlinux-x86_64-standard:5.0"
    type                        = "LINUX_CONTAINER"
    image_pull_credentials_type = "CODEBUILD"
    privileged_mode             = false
    environment_variable {
      name  = "AWS_DEFAULT_REGION"
      value = local.region
      type  = "PLAINTEXT"
    }
    environment_variable {
      name  = "DATABASE_HOST"
      value = aws_db_instance.learning.address
      type  = "PLAINTEXT"
    }
    environment_variable {
      name  = "DATABASE_PORT"
      value = "5432"
      type  = "PLAINTEXT"
    }
    environment_variable {
      name  = "ADMIN_SECRET_ARN"
      value = aws_db_instance.learning.master_user_secret[0].secret_arn
      type  = "PLAINTEXT"
    }
    environment_variable {
      name  = "RUNTIME_SECRET_ARN"
      value = aws_secretsmanager_secret.runtime.arn
      type  = "PLAINTEXT"
    }
    environment_variable {
      name  = "MIGRATION_SECRET_ARN"
      value = aws_secretsmanager_secret.migration.arn
      type  = "PLAINTEXT"
    }
  }

  source {
    type            = "GITHUB"
    location        = "https://github.com/HarleyBartles/wild-bunch.git"
    buildspec       = "deploy/database/buildspec.yml"
    git_clone_depth = 0
    auth {
      type     = "CODECONNECTIONS"
      resource = aws_codeconnections_connection.github.arn
    }
  }

  vpc_config {
    vpc_id             = aws_vpc.learning.id
    subnets            = aws_subnet.private[*].id
    security_group_ids = [aws_security_group.codebuild_initializer.id]
  }

  logs_config {
    cloudwatch_logs {
      status      = "ENABLED"
      group_name  = aws_cloudwatch_log_group.database_initializer.name
      stream_name = "initializer"
    }
    s3_logs { status = "DISABLED" }
  }

  depends_on = [aws_iam_role_policy.codebuild_initializer_vpc, aws_iam_role_policy.codebuild_initializer]
}
