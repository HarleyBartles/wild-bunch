# Application release

A release is an application version plus its database compatibility claim, immutable configuration references and three image identities. Terraform provisions AWS resources; this command changes Kubernetes application state after the separate database migration Job completes.

Each release ID is unique. AWS IDs combine the full source commit, GitHub run ID and run attempt. Local IDs combine the full source commit and a distinct invocation suffix. The API ConfigMap and runtime/migration Secrets include that release ID in their names and are immutable. API and migration Pods use separate Secrets; the manifest saved for recovery contains references, never credential values.

Before the migration runs, the operator supplies a compatibility acknowledgement with explicit `schema:`, `constraints:`, `data/events:`, `writes:` and `recovery:` assessments. The recovery field identifies the currently recorded known-good release, or `none` for a first release. This is a human compatibility decision, not an automated schema proof. For this exercise the schema remains unchanged and the exercise demonstrates application release recovery.

The release controller records the selected secret version IDs, prepares immutable resources, inspects the deterministic migration Job name, and creates a Job only if it does not exist. The Job uses `backoffLimit: 0` and remains available for inspection. A failed migration stops before any application change. A timeout or lost runner leaves the result uncertain; inspect and resume the same Job before considering a new release attempt. Never automatically rerun or reverse a migration.

After the Job completes, the controller uses runtime credentials to confirm migration history is present, applies the frontend/API workload manifest by image digest, waits for both Deployments, checks running image identities, and probes the loopback frontend, release header and API readiness endpoint. The new API's readiness check verifies its required EF migration IDs without requiring exact equality with the database history. Only after those checks pass does the controller mark the release known-good and update the private release pointer.

The previous pointer is not changed when migration, secret preparation, rollout or HTTP verification fails. A failed app rollout may leave the previous ready Pod serving while Kubernetes reports the Deployment as incomplete. This does not make the failed release successful; explicitly restore the stored previous manifest when recovery is selected.

The local exercise invokes the same controller after a clean commit and image build:

```powershell
py -3 -m tools.deployment.local release
```

The trusted AWS release workflow supplies its environment and release JSON to the corresponding command inside the private CodeBuild runner:

```powershell
py -3 -m tools.deployment.release deploy --environment <protected-environment-contract.json> --release <protected-release-contract.json>
```

Inspect a record without credential values:

```powershell
py -3 -m tools.deployment.release inspect --environment <protected-environment-contract.json> --release-id <release-id>
```

The AWS release contract must contain three ECR digest references and an acknowledgement tied to the currently known-good release. Task 8 wires source trust and GitHub workflow controls around these commands. Until then, the command is an implementation seam, not a privileged workflow entry point.
