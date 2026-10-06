mock_provider "aws" {
  mock_data "aws_iam_policy_document" {
    defaults = {
      json = "{\"Version\":\"2012-10-17\",\"Statement\":[]}"
    }
  }
}

override_data {
  target = data.aws_iam_policy_document.environment_provisioner
  values = {
    json = "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Sid\":\"CreateOnlyNamedApplicationSecrets\",\"Effect\":\"Allow\",\"Action\":[\"secretsmanager:CreateSecret\"],\"Resource\":\"*\",\"Condition\":{\"StringEquals\":{\"secretsmanager:Name\":[\"wild-bunch-learning/runtime\",\"wild-bunch-learning/migration\"]}}},{\"Sid\":\"MaintainOnlyApplicationSecretContainers\",\"Effect\":\"Allow\",\"Action\":[\"secretsmanager:DeleteSecret\",\"secretsmanager:DescribeSecret\",\"secretsmanager:ListSecretVersionIds\",\"secretsmanager:TagResource\",\"secretsmanager:UntagResource\"],\"Resource\":[\"arn:aws:secretsmanager:eu-north-1:123456789012:secret:wild-bunch-learning/runtime-*\",\"arn:aws:secretsmanager:eu-north-1:123456789012:secret:wild-bunch-learning/migration-*\"]}]}"
  }
}

override_data {
  target = data.aws_caller_identity.current
  values = {
    account_id = "123456789012"
    arn        = "arn:aws:iam::123456789012:user/terraform-test"
    user_id    = "AIDATESTUSER"
  }
}

override_data {
  target = data.aws_partition.current
  values = {
    partition = "aws"
  }
}

override_resource {
  target          = aws_iam_openid_connect_provider.github[0]
  override_during = plan
  values = {
    arn = "arn:aws:iam::123456789012:oidc-provider/token.actions.githubusercontent.com"
  }
}

run "protects_state_and_trusts_only_the_learning_environment" {
  command = plan

  assert {
    condition     = aws_s3_bucket_versioning.state.versioning_configuration[0].status == "Enabled"
    error_message = "State versioning must remain enabled for recovery."
  }

  assert {
    condition     = one(aws_s3_bucket_server_side_encryption_configuration.state.rule).apply_server_side_encryption_by_default[0].sse_algorithm == "AES256"
    error_message = "The state bucket must use server-side encryption."
  }

  assert {
    condition     = aws_s3_bucket_public_access_block.state.block_public_acls && aws_s3_bucket_public_access_block.state.block_public_policy && aws_s3_bucket_public_access_block.state.ignore_public_acls && aws_s3_bucket_public_access_block.state.restrict_public_buckets
    error_message = "Every S3 public-access block must be enabled."
  }

  assert {
    condition     = output.github_oidc_subject == "repo:HarleyBartles/wild-bunch:environment:wild-bunch-learning"
    error_message = "OIDC trust must identify the exact repository environment."
  }

  assert {
    condition     = jsondecode(aws_iam_role.image_publisher.assume_role_policy).Statement[0].Condition.StringEquals["token.actions.githubusercontent.com:sub"] == "repo:HarleyBartles/wild-bunch:environment:wild-bunch-learning"
    error_message = "The image-publisher trust must use the exact environment subject."
  }

  assert {
    condition     = sort(jsondecode(aws_iam_role_policy.image_publisher.policy).Statement[1].Resource) == sort(local.image_repository_arns)
    error_message = "The image-publisher role must be limited to the three exercise repositories."
  }

  assert {
    condition     = jsondecode(aws_iam_role_policy.database_initializer_invoker.policy).Statement[0].Action == "codebuild:StartBuild" && jsondecode(aws_iam_role_policy.database_initializer_invoker.policy).Statement[0].Resource == "arn:aws:codebuild:eu-north-1:123456789012:project/wild-bunch-learning-codebuild-database-init" && jsondecode(aws_iam_role_policy.database_initializer_invoker.policy).Statement[1].Action == "codebuild:BatchGetBuilds" && !contains(flatten([for statement in jsondecode(aws_iam_role_policy.database_initializer_invoker.policy).Statement : [statement.Action]]), "secretsmanager:GetSecretValue")
    error_message = "The database-init workflow role can invoke and poll only its private initializer, without database-secret access."
  }

  assert {
    condition     = one([for statement in jsondecode(data.aws_iam_policy_document.environment_provisioner.json).Statement : statement if statement.Sid == "CreateOnlyNamedApplicationSecrets"]).Condition.StringEquals["secretsmanager:Name"] == ["wild-bunch-learning/runtime", "wild-bunch-learning/migration"]
    error_message = "The environment provisioner may create only the two named application secret containers."
  }

  assert {
    condition     = one([for statement in jsondecode(data.aws_iam_policy_document.environment_provisioner.json).Statement : statement if statement.Sid == "MaintainOnlyApplicationSecretContainers"]).Resource == ["arn:aws:secretsmanager:eu-north-1:123456789012:secret:wild-bunch-learning/runtime-*", "arn:aws:secretsmanager:eu-north-1:123456789012:secret:wild-bunch-learning/migration-*"]
    error_message = "The environment provisioner may maintain only the two application secret containers."
  }
}

run "rejects_a_different_repository" {
  command = plan

  variables {
    github_repository = "untrusted/fork"
  }

  expect_failures = [var.github_repository]
}

run "rejects_a_different_environment" {
  command = plan

  variables {
    github_environment = "unprotected"
  }

  expect_failures = [var.github_environment]
}

run "rejects_a_different_region" {
  command = plan

  variables {
    aws_region = "us-east-1"
  }

  expect_failures = [var.aws_region]
}
