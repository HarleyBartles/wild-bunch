resource "aws_vpc" "learning" {
  cidr_block           = "10.42.0.0/16"
  enable_dns_support   = true
  enable_dns_hostnames = true
  tags                 = merge(local.tags, { Name = "wild-bunch-learning" })
}

resource "aws_internet_gateway" "learning" {
  vpc_id = aws_vpc.learning.id
  tags   = merge(local.tags, { Name = "wild-bunch-learning" })
}

resource "aws_subnet" "public" {
  count = 2

  vpc_id                  = aws_vpc.learning.id
  availability_zone       = local.availability_zones[count.index]
  cidr_block              = cidrsubnet(aws_vpc.learning.cidr_block, 8, count.index)
  map_public_ip_on_launch = false
  tags                    = merge(local.tags, { Name = "wild-bunch-learning-public-${count.index + 1}" })
}

resource "aws_subnet" "private" {
  count = 2

  vpc_id                  = aws_vpc.learning.id
  availability_zone       = local.availability_zones[count.index]
  cidr_block              = cidrsubnet(aws_vpc.learning.cidr_block, 8, count.index + 10)
  map_public_ip_on_launch = false
  tags                    = merge(local.tags, { Name = "wild-bunch-learning-private-${count.index + 1}" })
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.learning.id
  tags   = merge(local.tags, { Name = "wild-bunch-learning-public" })
}

resource "aws_route" "public_internet" {
  route_table_id         = aws_route_table.public.id
  destination_cidr_block = "0.0.0.0/0"
  gateway_id             = aws_internet_gateway.learning.id
}

resource "aws_route_table_association" "public" {
  count          = 2
  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}

resource "aws_eip" "nat" {
  domain = "vpc"
  tags   = merge(local.tags, { Name = "wild-bunch-learning-nat" })
}

resource "aws_nat_gateway" "learning" {
  allocation_id     = aws_eip.nat.id
  subnet_id         = aws_subnet.public[0].id
  connectivity_type = "public"
  depends_on        = [aws_internet_gateway.learning]
  tags              = merge(local.tags, { Name = "wild-bunch-learning" })
}

resource "aws_route_table" "private" {
  count  = 2
  vpc_id = aws_vpc.learning.id
  tags   = merge(local.tags, { Name = "wild-bunch-learning-private-${count.index + 1}" })
}

resource "aws_route" "private_egress" {
  count                  = 2
  route_table_id         = aws_route_table.private[count.index].id
  destination_cidr_block = "0.0.0.0/0"
  nat_gateway_id         = aws_nat_gateway.learning.id
}

resource "aws_route_table_association" "private" {
  count          = 2
  subnet_id      = aws_subnet.private[count.index].id
  route_table_id = aws_route_table.private[count.index].id
}

resource "aws_security_group" "nodes" {
  name        = "wild-bunch-learning-eks-nodes"
  description = "Private worker-node network boundary for Wild Bunch learning workloads."
  vpc_id      = aws_vpc.learning.id
  egress      = []
  tags        = merge(local.tags, { Name = "wild-bunch-learning-eks-nodes" })
}

resource "aws_vpc_security_group_egress_rule" "nodes_all" {
  security_group_id = aws_security_group.nodes.id
  cidr_ipv4         = "0.0.0.0/0"
  ip_protocol       = "-1"
  description       = "Outbound dependencies and AWS APIs via the private-subnet NAT route."
}

resource "aws_vpc_security_group_ingress_rule" "nodes_intercommunication" {
  security_group_id            = aws_security_group.nodes.id
  referenced_security_group_id = aws_security_group.nodes.id
  ip_protocol                  = "-1"
  description                  = "Required worker and Pod communication among this cluster's nodes."
}

resource "aws_security_group" "codebuild_release" {
  name        = "wild-bunch-learning-codebuild-release"
  description = "Private application release runner; no direct database route."
  vpc_id      = aws_vpc.learning.id
  egress      = []
  tags        = merge(local.tags, { Name = "wild-bunch-learning-codebuild-release" })
}

resource "aws_vpc_security_group_egress_rule" "codebuild_release_all" {
  security_group_id = aws_security_group.codebuild_release.id
  cidr_ipv4         = "0.0.0.0/0"
  ip_protocol       = "-1"
  description       = "GitHub Actions, EKS API and dependency traffic via NAT."
}

resource "aws_security_group" "codebuild_initializer" {
  name        = "wild-bunch-learning-codebuild-database-init"
  description = "Private one-shot database initialization, with no EKS API access."
  vpc_id      = aws_vpc.learning.id
  egress      = []
  tags        = merge(local.tags, { Name = "wild-bunch-learning-codebuild-database-init" })
}

resource "aws_vpc_security_group_egress_rule" "codebuild_initializer_all" {
  security_group_id = aws_security_group.codebuild_initializer.id
  cidr_ipv4         = "0.0.0.0/0"
  ip_protocol       = "-1"
  description       = "GitHub source and dependency traffic via the private-subnet NAT route."
}

resource "aws_security_group" "database" {
  name        = "wild-bunch-learning-postgres"
  description = "Private PostgreSQL ingress from application nodes and the isolated initializer."
  vpc_id      = aws_vpc.learning.id
  egress      = []
  tags        = merge(local.tags, { Name = "wild-bunch-learning-postgres" })
}

resource "aws_vpc_security_group_ingress_rule" "database_nodes" {
  security_group_id            = aws_security_group.database.id
  referenced_security_group_id = aws_security_group.nodes.id
  from_port                    = 5432
  to_port                      = 5432
  ip_protocol                  = "tcp"
  description                  = "Application traffic from the EKS worker-node security group."
}

resource "aws_vpc_security_group_ingress_rule" "database_initializer" {
  security_group_id            = aws_security_group.database.id
  referenced_security_group_id = aws_security_group.codebuild_initializer.id
  from_port                    = 5432
  to_port                      = 5432
  ip_protocol                  = "tcp"
  description                  = "Database initialization from the private CodeBuild security group."
}

resource "aws_vpc_security_group_ingress_rule" "eks_api_from_codebuild" {
  security_group_id            = aws_eks_cluster.learning.vpc_config[0].cluster_security_group_id
  referenced_security_group_id = aws_security_group.codebuild_release.id
  from_port                    = 443
  to_port                      = 443
  ip_protocol                  = "tcp"
  description                  = "Private EKS API access from the deployment runner."
}

resource "aws_vpc_security_group_ingress_rule" "eks_api_from_nodes" {
  security_group_id            = aws_eks_cluster.learning.vpc_config[0].cluster_security_group_id
  referenced_security_group_id = aws_security_group.nodes.id
  from_port                    = 443
  to_port                      = 443
  ip_protocol                  = "tcp"
  description                  = "Kubernetes API traffic from EKS worker nodes."
}

resource "aws_vpc_security_group_ingress_rule" "nodes_from_eks_control_plane" {
  security_group_id            = aws_security_group.nodes.id
  referenced_security_group_id = aws_eks_cluster.learning.vpc_config[0].cluster_security_group_id
  from_port                    = 1025
  to_port                      = 65535
  ip_protocol                  = "tcp"
  description                  = "EKS control-plane initiated kubelet and webhook traffic."
}

resource "aws_vpc_security_group_ingress_rule" "nodes_https_from_eks_control_plane" {
  security_group_id            = aws_security_group.nodes.id
  referenced_security_group_id = aws_eks_cluster.learning.vpc_config[0].cluster_security_group_id
  from_port                    = 443
  to_port                      = 443
  ip_protocol                  = "tcp"
  description                  = "EKS control-plane HTTPS traffic to worker nodes."
}
