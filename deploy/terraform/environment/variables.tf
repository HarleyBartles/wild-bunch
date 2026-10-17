variable "aws_region" {
  description = "The sole AWS region used by the exercise."
  type        = string
  default     = "eu-north-1"
  validation {
    condition     = var.aws_region == "eu-north-1"
    error_message = "This learning environment is restricted to eu-north-1."
  }
}

variable "state_bucket_name" {
  description = "The protected bucket created by the bootstrap root."
  type        = string
  validation {
    condition     = startswith(var.state_bucket_name, "wild-bunch-terraform-state-")
    error_message = "The environment backend and release manifests must use the exercise bootstrap bucket."
  }
}

variable "engineer_iam_role_arn" {
  description = "IAM role ARN granted administrative EKS access for the exercise."
  type        = string
  validation {
    condition     = can(regex("^arn:[a-z0-9-]+:iam::[0-9]{12}:role/.+$", var.engineer_iam_role_arn))
    error_message = "Supply an IAM role ARN for the engineer's authenticated AWS identity."
  }
}

variable "engineer_ipv4_cidr" {
  description = "The engineer's current public IPv4 address as one /32 for EKS API access."
  type        = string
  validation {
    condition     = can(cidrhost(var.engineer_ipv4_cidr, 0)) && can(regex("^([0-9]{1,3}\\.){3}[0-9]{1,3}/32$", var.engineer_ipv4_cidr))
    error_message = "The public EKS endpoint must be restricted to one explicit IPv4 /32."
  }
}

variable "eks_kubernetes_version" {
  description = "Pinned EKS minor version; verify regional availability immediately before apply."
  type        = string
  default     = "1.36"
  validation {
    condition     = var.eks_kubernetes_version == "1.36"
    error_message = "This learning exercise pins Kubernetes 1.36."
  }
}

variable "vpc_cni_addon_version" {
  description = "Pinned VPC CNI add-on version verified for Kubernetes 1.36."
  type        = string
  default     = "v1.23.1-eksbuild.1"
}

variable "coredns_addon_version" {
  description = "Pinned CoreDNS add-on version verified for Kubernetes 1.36."
  type        = string
  default     = "v1.14.7-eksbuild.10"
}

variable "kube_proxy_addon_version" {
  description = "Pinned kube-proxy add-on version verified for Kubernetes 1.36."
  type        = string
  default     = "v1.36.0-eksbuild.21"
}
