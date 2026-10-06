locals {
  eks_cluster_trust_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Action    = "sts:AssumeRole"
      Principal = { Service = "eks.amazonaws.com" }
    }]
  })
  eks_node_trust_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Action    = "sts:AssumeRole"
      Principal = { Service = "ec2.amazonaws.com" }
    }]
  })
  vpc_cni_trust_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Action    = "sts:AssumeRoleWithWebIdentity"
      Principal = { Federated = aws_iam_openid_connect_provider.cluster.arn }
      Condition = {
        StringEquals = {
          "${replace(aws_eks_cluster.learning.identity[0].oidc[0].issuer, "https://", "")}:aud" = "sts.amazonaws.com"
          "${replace(aws_eks_cluster.learning.identity[0].oidc[0].issuer, "https://", "")}:sub" = "system:serviceaccount:kube-system:aws-node"
        }
      }
    }]
  })
}

resource "aws_iam_role" "eks_cluster" {
  name               = local.role_names.cluster
  assume_role_policy = local.eks_cluster_trust_policy
  tags               = local.tags
}

resource "aws_iam_role_policy_attachment" "eks_cluster" {
  role       = aws_iam_role.eks_cluster.name
  policy_arn = "arn:${local.partition}:iam::aws:policy/AmazonEKSClusterPolicy"
}

resource "aws_iam_role" "eks_node" {
  name               = local.role_names.node
  assume_role_policy = local.eks_node_trust_policy
  tags               = local.tags
}

resource "aws_iam_role_policy_attachment" "eks_node_worker" {
  role       = aws_iam_role.eks_node.name
  policy_arn = "arn:${local.partition}:iam::aws:policy/AmazonEKSWorkerNodePolicy"
}

resource "aws_iam_role_policy_attachment" "eks_node_ecr" {
  role       = aws_iam_role.eks_node.name
  policy_arn = "arn:${local.partition}:iam::aws:policy/AmazonEC2ContainerRegistryPullOnly"
}

resource "aws_iam_openid_connect_provider" "cluster" {
  url            = aws_eks_cluster.learning.identity[0].oidc[0].issuer
  client_id_list = ["sts.amazonaws.com"]
}

resource "aws_iam_role" "vpc_cni" {
  name               = local.role_names.vpc_cni
  assume_role_policy = local.vpc_cni_trust_policy
  tags               = local.tags
}

resource "aws_iam_role_policy_attachment" "vpc_cni" {
  role       = aws_iam_role.vpc_cni.name
  policy_arn = "arn:${local.partition}:iam::aws:policy/AmazonEKS_CNI_Policy"
}

resource "aws_eks_cluster" "learning" {
  name     = local.cluster_name
  role_arn = aws_iam_role.eks_cluster.arn
  version  = var.eks_kubernetes_version
  tags     = local.tags

  access_config {
    authentication_mode                         = "API_AND_CONFIG_MAP"
    bootstrap_cluster_creator_admin_permissions = false
  }

  enabled_cluster_log_types = ["api", "audit", "authenticator"]

  vpc_config {
    subnet_ids              = aws_subnet.private[*].id
    endpoint_private_access = true
    endpoint_public_access  = true
    public_access_cidrs     = [var.engineer_ipv4_cidr]
  }

  depends_on = [aws_iam_role_policy_attachment.eks_cluster, aws_cloudwatch_log_group.eks]
}

resource "aws_cloudwatch_log_group" "eks" {
  name              = "/aws/eks/${local.cluster_name}/cluster"
  retention_in_days = 7
  tags              = local.tags
}

resource "aws_eks_access_entry" "engineer" {
  cluster_name      = aws_eks_cluster.learning.name
  principal_arn     = var.engineer_iam_role_arn
  kubernetes_groups = []
  type              = "STANDARD"
  tags              = local.tags
}

resource "aws_eks_access_policy_association" "engineer_admin" {
  cluster_name  = aws_eks_cluster.learning.name
  principal_arn = aws_eks_access_entry.engineer.principal_arn
  policy_arn    = "arn:${local.partition}:eks::aws:cluster-access-policy/AmazonEKSClusterAdminPolicy"
  access_scope {
    type = "cluster"
  }
}

resource "aws_eks_access_entry" "release" {
  cluster_name      = aws_eks_cluster.learning.name
  principal_arn     = aws_iam_role.codebuild_release.arn
  kubernetes_groups = ["wild-bunch-learning-release"]
  type              = "STANDARD"
  tags              = local.tags
}

resource "aws_eks_addon" "vpc_cni" {
  cluster_name                = aws_eks_cluster.learning.name
  addon_name                  = "vpc-cni"
  addon_version               = var.vpc_cni_addon_version
  service_account_role_arn    = aws_iam_role.vpc_cni.arn
  resolve_conflicts_on_create = "OVERWRITE"
  resolve_conflicts_on_update = "PRESERVE"
  tags                        = local.tags
  depends_on                  = [aws_iam_role_policy_attachment.vpc_cni]
}

resource "aws_eks_addon" "coredns" {
  cluster_name                = aws_eks_cluster.learning.name
  addon_name                  = "coredns"
  addon_version               = var.coredns_addon_version
  resolve_conflicts_on_create = "OVERWRITE"
  resolve_conflicts_on_update = "PRESERVE"
  tags                        = local.tags
  depends_on                  = [aws_eks_node_group.learning]
}

resource "aws_eks_addon" "kube_proxy" {
  cluster_name                = aws_eks_cluster.learning.name
  addon_name                  = "kube-proxy"
  addon_version               = var.kube_proxy_addon_version
  resolve_conflicts_on_create = "OVERWRITE"
  resolve_conflicts_on_update = "PRESERVE"
  tags                        = local.tags
  depends_on                  = [aws_eks_node_group.learning]
}

resource "aws_launch_template" "nodes" {
  name_prefix            = "wild-bunch-learning-eks-"
  update_default_version = true
  vpc_security_group_ids = [aws_security_group.nodes.id, aws_eks_cluster.learning.vpc_config[0].cluster_security_group_id]
  tag_specifications {
    resource_type = "instance"
    tags          = merge(local.tags, { Name = "wild-bunch-learning-eks-node" })
  }
  tag_specifications {
    resource_type = "volume"
    tags          = local.tags
  }
  tags = local.tags
}

resource "aws_eks_node_group" "learning" {
  cluster_name    = aws_eks_cluster.learning.name
  node_group_name = "wild-bunch-learning-workers"
  node_role_arn   = aws_iam_role.eks_node.arn
  subnet_ids      = aws_subnet.private[*].id
  capacity_type   = "ON_DEMAND"
  instance_types  = ["m7i.large"]
  ami_type        = "AL2023_x86_64_STANDARD"
  disk_size       = 40
  version         = var.eks_kubernetes_version
  tags            = local.tags

  scaling_config {
    desired_size = 1
    min_size     = 1
    max_size     = 2
  }

  launch_template {
    id      = aws_launch_template.nodes.id
    version = aws_launch_template.nodes.latest_version
  }

  update_config {
    max_unavailable = 1
  }

  depends_on = [
    aws_iam_role_policy_attachment.eks_node_worker,
    aws_iam_role_policy_attachment.eks_node_ecr,
    aws_eks_addon.vpc_cni,
    aws_cloudwatch_log_group.eks,
    aws_vpc_security_group_ingress_rule.eks_api_from_codebuild,
    aws_vpc_security_group_ingress_rule.eks_api_from_nodes,
    aws_vpc_security_group_ingress_rule.nodes_from_eks_control_plane,
    aws_vpc_security_group_ingress_rule.nodes_https_from_eks_control_plane,
  ]
}
