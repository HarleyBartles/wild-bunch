# Private learning deployment teardown

Teardown is a reviewed two-root operation. Terraform success means the provider accepted the requested changes; it does not prove that delayed AWS deletion completed or that untracked/versioned resources are absent. Run the read-only inventory after each stage and keep every resource marked `remaining: true` on the cleanup list until a later inspection confirms absence.

## Before starting

1. Use the AWS profile `wild-bunch`, region `eu-north-1`, and verify the active account identity against the account recorded in the protected local state. Do not put account identifiers, state, plans, credentials, or raw AWS output in GitHub artifacts.
2. Save the exact `environment_contract` output privately outside the repository and run `py -3 -m tools.deployment.inventory --environment <protected-path>`. The report is read-only. A permission or discovery error is an incomplete report, never an empty inventory.
3. Confirm all CodeBuild builds and Kubernetes migration Jobs have stopped. Stop/review transient application workloads before destroying infrastructure. Preserve the latest known-good non-secret release manifest and the Terraform recovery state until inventory verification finishes.
4. Inspect the reviewed environment destroy plan and confirm that every target belongs to `wild-bunch-learning`. Never select resources for deletion merely because they appeared in an account-wide listing.

## Destroy the environment root

1. Remove application workload objects and active Jobs through the documented release controls; verify the migration process is no longer running before removing database access.
2. Review and apply the exact environment Terraform destroy plan using the authorized infrastructure workflow or local identity. This root owns the EKS cluster/node group/add-ons and their IAM roles, ECR repositories, RDS PostgreSQL instance and subnet/parameter groups, runtime/migration secret containers, VPC/subnets/route tables/NAT gateway/EIP/security groups, CodeBuild projects/webhook/connection and their log groups, and the EKS control-plane log group. It does not own the bootstrap S3 bucket, GitHub OIDC provider supplied as pre-existing, or the protected bootstrap roles.
3. Run inventory again. ECR repositories and their images, RDS instance and its AWS-managed master secret, runtime/migration secrets, CodeBuild connection/projects/log groups, EC2 instances/volumes, NAT gateway, EIP, EKS resources, and the VPC must all be explicitly absent. A secret with `DeletedDate` is `scheduled-deletion` and remains outstanding until `DescribeSecret` reports it absent. A NAT gateway in `deleting` or EIP still allocated/associated remains outstanding. Investigate every EC2 volume or tagged resource still returned; never delete an unmatched resource based on resemblance alone.
4. Correct orphaned exercise resources only after matching their exact identifiers, ownership tags, and protected state history. Do not broaden IAM permissions or delete an unowned/pre-existing OIDC provider to make the report green.

## Migrate bootstrap state, then remove bootstrap

1. Before deleting the bucket, initialize the bootstrap root with its protected local backend configuration and migrate state out of the S3 backend. Verify the local state exists, is access-restricted, can be read by Terraform, and describes the retained bootstrap resources. Keep a protected backup copy. This migration is the final point at which the S3 state backend is needed.
2. Confirm the environment root has already been destroyed and its state remains available locally for reconciliation. Confirm the bucket itself is owned by this exercise and contains no objects outside the exercise's bootstrap/environment/plans/releases scope. Inventory all versions and delete markers with `inventory`; `list_object_versions` metadata is used without downloading object contents.
3. Remove the exercise's private release manifests and any remaining plans only after retaining the necessary local recovery copy. Remove every object version and delete marker under the exact bucket and keys, including `bootstrap/terraform.tfstate`, `bootstrap/terraform.tfstate.tflock`, `environment/terraform.tfstate`, `environment/terraform.tfstate.tflock`, `plans/`, and `releases/`. Re-list versions and markers and require an empty result before deleting the bucket. The lifecycle rule for plans does not replace this check.
4. Review and apply the bootstrap destroy plan last. It removes the three exercise-owned GitHub OIDC roles and their policies, any OIDC provider created by this bootstrap, bucket controls and bucket. If a pre-existing GitHub OIDC provider was supplied to bootstrap, preserve it. Do not remove GitHub repository environments, GitHub App installations, or other externally managed configuration as part of Terraform teardown.
5. Run a final inventory while the local contract and state are retained. Confirm bucket, all versions/delete markers, exercise-owned roles/provider, and all environment resources are absent. Any denial, scheduled deletion, lingering volume, log group, EIP, NAT gateway, image, secret, connection, or state version means teardown remains incomplete. Recheck delayed removals until AWS confirms absence, then deliberately dispose of protected local state and recovery copies outside Git.

## Inventory command and interpretation

```powershell
py -3 -m tools.deployment.inventory inspect --environment <protected-path-to-environment-contract.json>
```

Exit code `0` means every resource the inspector could discover was absent; `1` means one or more discovered/exact-contract resources remain; `2` means inspection failed or ownership could not be established. None of these codes substitutes for reviewing the JSON resource list. The AWS Resource Groups Tagging API is filtered by both `Project=wild-bunch-learning` and `OwnerId=wild-bunch-learning`; exact environment contract ARNs, ECR repository names, the S3 contract bucket, and exercise key prefixes add explicit checks. The inventory never reads secret values or S3 object bodies and never deletes anything. An empty report is useful only when all required discovery calls succeeded and the ownership scope is known.
