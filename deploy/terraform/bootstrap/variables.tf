variable "aws_region" {
  description = "The sole AWS region for this exercise."
  type        = string
  default     = "eu-north-1"

  validation {
    condition     = var.aws_region == "eu-north-1"
    error_message = "This learning environment is restricted to eu-north-1."
  }
}

variable "github_repository" {
  description = "The exact repository trusted to request deployment credentials."
  type        = string
  default     = "HarleyBartles/wild-bunch"

  validation {
    condition     = var.github_repository == "HarleyBartles/wild-bunch"
    error_message = "The OIDC trust is restricted to HarleyBartles/wild-bunch."
  }
}

variable "github_environment" {
  description = "The protected GitHub Actions environment named in the OIDC subject."
  type        = string
  default     = "wild-bunch-learning"

  validation {
    condition     = var.github_environment == "wild-bunch-learning"
    error_message = "The OIDC trust is restricted to the wild-bunch-learning environment."
  }
}

variable "existing_github_oidc_provider_arn" {
  description = "Optional ARN of a pre-existing GitHub OIDC provider. Set only after read-only inspection confirms its URL and client ID."
  type        = string
  default     = null
  nullable    = true

  validation {
    condition = var.existing_github_oidc_provider_arn == null || can(regex(
      "^arn:[a-z0-9-]+:iam::[0-9]{12}:oidc-provider/token\\.actions\\.githubusercontent\\.com$",
      var.existing_github_oidc_provider_arn
    ))
    error_message = "An existing provider must be the account's token.actions.githubusercontent.com IAM OIDC provider ARN."
  }
}

variable "image_repository_names" {
  description = "Exercise ECR repositories whose tags are published by the separate image-publisher role."
  type        = set(string)
  default     = ["wild-bunch-frontend", "wild-bunch-api", "wild-bunch-migrations"]

  validation {
    condition = var.image_repository_names == toset([
      "wild-bunch-frontend",
      "wild-bunch-api",
      "wild-bunch-migrations",
    ])
    error_message = "The image role may publish only the frontend, API and migration repositories."
  }
}
