# Terraform bootstrap

This root defines the S3 Terraform state bucket and two distinct GitHub OIDC roles. It does not provision the game environment. The protected account profile is used only for the explicitly approved bootstrap and environment Terraform operations; workflow roles are temporary and receive separate permission sets.

The bootstrap root starts with local state under an access-restricted directory on the engineer's machine. Terraform's S3 backend cannot create its own bucket before it has a backend, so create the bucket with local state first, review that result, then initialize again with `-migrate-state` to the `bootstrap/terraform.tfstate` key. The separate environment root uses `environment/terraform.tfstate` and the environment-provisioning role can access only that state object and its `.tflock` file. The release runner receives no Terraform state permission.

The bucket enables AES-256 server-side encryption, versioning, all four S3 public-access blocks, bucket-owner-enforced ownership and a TLS-only bucket policy. `prevent_destroy` protects the state bucket by default. Bootstrap teardown requires an explicit, separately reviewed configuration change after migrating the state back to protected local storage. Versioned state objects and lockfiles require their own cleanup before the bucket can be removed.

## Identity boundaries

The image-publisher role can request an ECR authorization token and push layers/manifests only to the frontend, API and migration repositories. The environment-provisioning role can read/write only the environment state object and lockfile, and can manage the service families used by the exercise in `eu-north-1`. AWS requires wildcard resource scope for some create and discovery calls because the resource ARN does not exist until after creation. The role is therefore service- and region-scoped, not a general-purpose account administrator. Its `iam:PassRole` permission is restricted to the four named EKS and CodeBuild exercise roles.

Neither role can call `secretsmanager:GetSecretValue` or `secretsmanager:PutSecretValue`. Terraform creates only the runtime and migration secret containers; the private database initializer populates their values later. RDS-managed administrator credentials are not read by Terraform or the infrastructure role. The release runner, created with the environment, gets no access to either Terraform state key.

Both workflow roles trust only the GitHub OIDC audience `sts.amazonaws.com` and subject `repo:HarleyBartles/wild-bunch:environment:wild-bunch-learning`. The live repository was created before GitHub's immutable-subject format rollout and its current OIDC customization reports the default name-based subject. Recheck that configuration before applying bootstrap. The environment subject alone does not encode a branch, workflow file or event: configure the GitHub `wild-bunch-learning` environment to allow only `main`, require workflows to run from `refs/heads/main`, and protect the workflows on that branch. If the repository OIDC template changes, stop and update the trust contract after review.

The bootstrap creates the GitHub OIDC provider only when an ARN for a pre-existing provider is not supplied. If one exists, inspect its exact URL and audience with read-only AWS commands, pass its ARN using `existing_github_oidc_provider_arn`, and record that it is externally owned. Terraform then references but never deletes it. Do not import a pre-existing provider into this root unless its ownership has explicitly changed.

## Local credentials

The AWS CLI profile `wild-bunch` is the source identity. If Terraform's AWS SDK credential chain cannot consume that profile's login credentials directly, add a distinct profile to the local shared AWS config file without putting credentials in the repository:

```ini
[profile wild-bunch-terraform]
credential_process = aws configure export-credentials --profile wild-bunch --format process
region = eu-north-1
```

The process profile points to `wild-bunch`, not itself. Select it in PowerShell with `$env:AWS_PROFILE = "wild-bunch-terraform"`; do not print exported credential output or enable shell tracing around the credential process. In a fresh shell, confirm the selected identity without recording its account details in Git. The GitHub workflow uses OIDC and does not use this local profile.

## Initialization and state migration

The following commands validate providers and tests without creating AWS resources:

```powershell
terraform -chdir=deploy/terraform/bootstrap init -backend=false
terraform -chdir=deploy/terraform/bootstrap fmt -check
terraform -chdir=deploy/terraform/bootstrap validate
terraform -chdir=deploy/terraform/bootstrap test
```

Before any approved bootstrap apply, review the live account identity, existing GitHub OIDC providers, account service restrictions and regional capability. Begin with local state and the account profile. Once the bucket exists and its protection has been inspected, create a private backend configuration outside Git with the bucket name from the reviewed output, `key = "bootstrap/terraform.tfstate"`, `region = "eu-north-1"`, and `use_lockfile = true`; then migrate the state using `terraform init -migrate-state -backend-config=<protected-config-file>`. Keep that file access-restricted and omit credentials from it. The state object is sensitive because Terraform state contains resource identifiers and policy configuration.

Do not provision until the human separately approves the concrete cost, region, identity, access rules and lifecycle. `terraform plan` is read-only with respect to AWS resources but contacts AWS to refresh provider data and may contain sensitive account/resource metadata; save any plan only in protected local storage. `terraform apply` is outside this bootstrap implementation authorization.
