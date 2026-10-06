# Terraform environment

This root owns only the disposable Wild Bunch learning environment. It requires the bootstrap S3 bucket and its distinct `environment/terraform.tfstate` backend key. The provisioning role may manage this key; release and initializer roles receive no Terraform-state access.

The environment is private by default: worker and database subnets have no public IP assignment, PostgreSQL accepts traffic only from worker and initializer security groups, CodeBuild runs in private subnets, and the game has no load balancer. One NAT gateway provides outbound access. EKS enables private API access and a public administrative endpoint restricted to the supplied engineer IPv4 `/32`; the endpoint requires AWS IAM authentication. The release CodeBuild security group can reach the EKS API but cannot connect to PostgreSQL; a separate initializer group can reach PostgreSQL but not the EKS API.

The CodeConnections resource starts pending until a human authorizes its GitHub App installation. Terraform cannot grant repository access on GitHub. Before using either project, complete that consent and restrict the GitHub deployment environment to `main`; the release webhook separately filters for the exact workflow name and main branch. Verify those filters against the actual webhook before privileged activation.

The environment creates empty runtime and migration secret containers. It never sets their values. RDS manages its own administrator password in a separate Secrets Manager secret. Only the manually invoked private initializer role can read that administrator credential; the release role can read application and migration credentials but has no database security-group path.

EKS 1.36 and the three managed add-on versions are pinned to the values currently documented by AWS. Recheck EKS and add-on availability in `eu-north-1`, RDS PostgreSQL 16.15/class availability and instance capacity, subnet/AZ support, and the engineer's current public IP immediately before a separately authorized apply. Do not widen the CIDR or silently choose another version or instance size to work around a restriction.

The RDS instance is deliberately single-AZ, has no backup retention or final snapshot, permits immediate disposal, uses 20 GiB encrypted gp3 storage, and does not enable performance monitoring. This is synthetic learning data in a temporary environment, not a production database recommendation. The custom PostgreSQL 16 parameter group forces TLS; the API must validate the RDS certificate chain and endpoint hostname using the hash-pinned regional root bundle described in [the AWS bootstrap runbook](../../runbooks/aws-bootstrap.md).

The EKS control plane logs API, audit and authenticator events for seven days. CodeBuild logs also expire after seven days. ECR repositories use immutable tags and force-delete only their own contents during authorized teardown. The EKS worker role can pull the three private repositories. The VPC CNI uses a separate web-identity role rather than the broad node identity.

The `environment_contract` output is JSON text with only non-secret release metadata. Save it to access-restricted local storage and never commit it. Terraform state and saved plans contain account and resource metadata and remain in protected storage.

Offline validation commands:

```powershell
terraform -chdir=deploy/terraform/environment init -backend=false
terraform -chdir=deploy/terraform/environment fmt -check -recursive
terraform -chdir=deploy/terraform/environment validate
terraform -chdir=deploy/terraform/environment test
```

Do not apply this root until the exact plan, human identity, GitHub connection consent, private routes and security-group paths, costs, cleanup, service limits and regional capacity have been reviewed and explicitly authorized. Never create the environment by applying a plan file that was not inspected against its exact Terraform configuration and state.
