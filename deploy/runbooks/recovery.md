# Application recovery

Application recovery restores a previously recorded known-good frontend/API manifest, including its exact image references, immutable ConfigMap name and runtime Secret name. It retrieves the credential values from the recorded Secrets Manager version IDs into process memory, confirms the immutable Kubernetes Secrets still match, reapplies the stored manifest, verifies both image identities and readiness, then updates the private current-release pointer.

```powershell
py -3 -m tools.deployment.local recover --release-id <known-good-release-id>
py -3 -m tools.deployment.release recover --environment <protected-environment-contract.json> --release-id <known-good-release-id>
```

Recovery does not run a migration and does not undo one. The compatibility acknowledgement was required before migration so the previous application can run against the resulting schema and any constraints, snapshots, event payloads and writes. If that compatibility claim is no longer true, stop and choose a data/schema recovery decision rather than restoring an incompatible binary.

If a release Job is failed, inspect its status and logs first. Do not delete it or reuse its release ID to cause another migration execution. If the Job is running or its completion is uncertain after a timeout, observe the same Job; runner loss is not proof that the database operation stopped. Use a distinct release ID only after deliberate diagnosis and approval.

If the application rollout fails, the controller keeps the previous known-good pointer and reports failure. Record Pod readiness, rollout conditions, image IDs and the API readiness result, then explicitly restore the known-good release. Kubernetes `rollout undo` is insufficient because it does not restore the full ConfigMap and Secret references. Check the browser journey after recovery; passing readiness alone does not prove saved game behavior.

Release records, rendered non-secret manifests, source digests and credential VersionIds live in protected local storage for kind or under the private S3 `releases/` prefix for AWS. They contain no credential values. Failed releases may leave immutable Secrets, ConfigMaps and retained Jobs; keep these until the exercise teardown inventory accounts for them. Do not delete recovery dependencies during the exercise.
