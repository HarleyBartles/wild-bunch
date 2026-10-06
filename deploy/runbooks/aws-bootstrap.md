# AWS bootstrap and access

The bootstrap and environment Terraform roots are source for a private, disposable learning deployment. Applying them creates billable AWS resources. This runbook prepares the workflow; it does not authorize an apply. Before the later execution stage, review the exact account identity, plans, expected cost, network access, IAM roles, GitHub connection, and teardown process together.

## Identity setup

Use the AWS login profile `wild-bunch` only as the local bootstrap identity. Never place credentials in repository files or Terraform variables. If Terraform cannot consume the login profile directly, configure a distinct profile in the local shared AWS config:

```ini
[profile wild-bunch-terraform]
credential_process = aws configure export-credentials --profile wild-bunch --format process
region = eu-north-1
```

Select it with `$env:AWS_PROFILE = "wild-bunch-terraform"`. The credential process writes temporary credential data to Terraform's process environment; do not print it, redirect it to a file, or enable shell tracing. Confirm the current caller identity using a read-only call without recording account details in Git. GitHub workflows use OpenID Connect role assumption, not the local profile.

## Bootstrap order

1. Initialize `deploy/terraform/bootstrap` with `-backend=false` and protected local state. Review the plan and apply only after a separate explicit approval of the concrete paid resources.
2. Confirm the state bucket is encrypted, versioned, private, TLS-only and protected from ordinary Terraform destruction. Prepare an access-restricted backend configuration outside the repository with bucket, region `eu-north-1`, key `bootstrap/terraform.tfstate` and native `use_lockfile = true`; migrate the local state into it.
3. Initialize `deploy/terraform/environment` against the distinct `environment/terraform.tfstate` key. The environment provisioning identity can access that state and lockfile only. Keep both local recovery copies outside Git and access restricted.
4. Configure the GitHub `wild-bunch-learning` environment to allow only `main` and require trusted workflow files on that branch. The OIDC subject contains the environment but not the branch or workflow. Confirm the live subject format still matches the bootstrap trust before using the role.
5. Apply the environment only from its reviewed infrastructure workflow or local bootstrap procedure. Complete the GitHub App installation consent for the Terraform-created CodeConnections connection before starting either private CodeBuild project.

Do not save Terraform plans, state, populated secrets, or AWS output in public workflow artifacts. A Terraform plan refreshes AWS metadata and may reveal account/resource information, so keep saved plans in protected local storage and apply the exact reviewed plan.

## Private network and workload boundary

The game has no public endpoint. Worker nodes, RDS PostgreSQL and both CodeBuild projects use private subnets. One NAT gateway allows outbound package, source and AWS API traffic. The release runner has an EKS API path but no database security-group path. The separate initializer has a PostgreSQL path but no EKS API path. PostgreSQL accepts port 5432 from the worker security group and initializer security group only.

EKS has both a private administration endpoint and a public administration endpoint restricted to the engineer's current IPv4 `/32`; AWS IAM authentication is also required. If the address changes, review and update the single `/32`. Never substitute `0.0.0.0/0`. The EKS public endpoint is for cluster administration, not player traffic. Application Services remain `ClusterIP`; the engineer uses authenticated local `kubectl port-forward` from their `/32`-restricted workstation, and opens the game at loopback in their own browser.

The deployment has no network-policy claim. Node security groups apply to ENIs/nodes and do not isolate individual Pods. The EKS release identity is mapped to one namespace Role; it does not receive cluster administrator access. API and frontend Pods do not receive AWS identities or automatically mounted Kubernetes service-account tokens.

## Database credentials and TLS

RDS manages the administrator password in its own Secrets Manager secret. Terraform creates the empty runtime and migration secret containers without secret versions. Only the database-initializer CodeBuild role can read the RDS administrator value and write the two application secret values. The initializer-invoker role can only start and poll that private project. The release CodeBuild role can read application and migration secrets for a release, but its security group cannot connect to PostgreSQL and it cannot read the administrator secret.

The checked-in regional root bundle at `deploy/database/certs/eu-north-1-bundle.pem` came from the [AWS RDS regional trust store](https://truststore.pki.rds.amazonaws.com/eu-north-1/eu-north-1-bundle.pem). Its current SHA-256 is `8d8dc42958c7b9351846609d552b4b38f3b19c92c9d4816b2f814fd148fe18be`. API and migration image builds verify the digest and install it at `/app/certs/rds-ca-bundle.pem`; the initializer verifies the same input and uses `sslmode=verify-full`. Refresh it from the [AWS RDS TLS guidance](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.SSL.html), inspect that it contains regional root certificates, update the recorded digest in both Dockerfiles and the initializer buildspec, rebuild the images, and perform the incorrect-CA negative test before deployment. Never disable hostname or certificate verification to make a connection succeed.

## Operational boundaries

The bootstrap bucket also stores non-secret release manifests under `releases/`; release roles have no access to either Terraform state key. The infrastructure identity cannot read or write secret values. CodeBuild CloudWatch log groups and EKS control-plane logs retain seven days. Do not print database credentials, connection strings or hidden game state in those logs.

Terraform can create the AWS-side GitHub connection resource, but a human must authorize its GitHub App installation. Recheck the current EKS minor, EKS add-on versions, RDS PostgreSQL `16.15` and `db.t4g.small` availability, two supported AZs, EKS/CNI capacity and the engineer's current `/32` immediately before any apply. If the agreed shape is unavailable or denied by the new AWS experience, stop and return the observed blocker for a design decision.

See [bootstrap root details](../terraform/bootstrap/README.md), [environment root details](../terraform/environment/README.md), and [teardown procedure](teardown.md). Keep the eventual teardown sequence explicit: stop workloads and initializer jobs, destroy the environment, verify remaining resources and scheduled removals, migrate bootstrap state back to protected local storage, remove only owned release objects and every bucket state version/lockfile, then destroy bootstrap roles, owned GitHub trust and the bucket last.
