# Private AWS, Kubernetes and Terraform Learning Deployment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deploy the existing Wild Bunch game privately on local Kubernetes and temporary AWS infrastructure, demonstrate persistence and release recovery, and remove the AWS environment completely.

**Architecture:** Production frontend, API and migration images share a source commit. Kustomize supplies local PostgreSQL or private RDS configuration. Terraform owns AWS infrastructure; GitHub Actions publishes images and orchestrates a temporary CodeBuild runner inside the private network for database initialization and application releases.

**Tech Stack:** .NET 10, EF Core 10, React/Vite, PostgreSQL 16, Linux containers, kind, Kubernetes 1.36, Kustomize, Terraform 1.16.5, AWS provider 6.x, ECR, EKS, RDS, CodeBuild, Secrets Manager, S3 and GitHub Actions.

**Spec:** [Approved deployment specification](../specs/2026-10-06-private-aws-kubernetes-learning-deployment.md). Read both artifacts before execution.

**Execution Strategy:** `executing-plans`, sequentially. Container configuration, database identities, migration execution and recovery manifests form one release contract; local observations feed the AWS configuration. Preserve that context through execution and obtain a fresh whole-branch review before handoff.

## Global Constraints

- The user has explicitly authorized implementation and a draft PR. This does not authorize paid AWS provisioning or public exposure. Before any AWS create/destroy action, obtain explicit authorization for the concrete reviewed environment, costs, access and synthetic data.
- Use the existing worktree `Z:/_agent-worktrees/wild-bunch/codex/aws-kubernetes-learning-design`; inspect its current state before resuming. Keep transient evidence and protected local state outside Git in `Z:/_agent-scratch/wild-bunch/codex-aws-kubernetes-learning-design` with access restricted to the engineer. Scratch placement alone is not access protection.
- Keep all regional AWS resources in `eu-north-1`. No public game endpoint, player authentication, game feature, demonstration service, service mesh, GitOps controller or autoscaling experiment.
- Preserve existing development convenience behind `Development`, existing developer-operation denial in `Production`, event sourcing, payload versions and upcasters. Do not use developer mutations for the deployment demonstrations.
- No credentials, populated Kubernetes Secrets, Terraform state, saved plans, raw logs, screenshots or test receipts in Git or public workflow artifacts. Never enable shell tracing around secrets or pass connection strings on command lines.
- Use one frontend and one API replica, with `maxUnavailable: 0` and `maxSurge: 1`. Do not describe this as high availability. Do not claim Pod network isolation without a verified enforcement mechanism.
- Terraform infrastructure and application release remain separate. Database migrations precede app rollout; application recovery never automatically reverses schema changes.
- Every command wrapper checks exit status and fails explicitly. A timeout is an uncertain result requiring inspection, not permission to rerun a mutation.
- Follow [planning](../runbooks/planning.md), [implementation](../runbooks/implementing.md), [testing](../playbooks/testing.md), [security](../playbooks/security.md) and the specification's doctrine links. Keep Markdown prose on one physical line per paragraph or list item.
- Normal commits run the tracked hook. Do not run the canonical gate redundantly just before a hooked commit or use `--no-verify`. Before uncommitted completion claims use `py -3 tools/run.py ci --check`; a successful hook supplies canonical evidence for its commit. Deployment demonstrations supply separate runtime evidence.

## Review Focus

1. Database absent, unavailable or missing required migrations: readiness fails, liveness remains healthy, startup does not migrate in Production, and a compatible later migration does not disqualify the old image. Covered by Task 1 HTTP/PostgreSQL tests and Task 4 outage exercise.
2. Partial credential initialization: reruns preserve passwords and data; a missing secret for an existing role or mismatched credential stops safely. Covered by Task 3 initializer tests against PostgreSQL and a simulated secret store.
3. Failed or uncertain migration/release: no app rollout after migration failure, no duplicate job after timeout, and full known-good configuration survives failed rollout. Covered by Task 7 release-controller tests and Task 10 runtime recovery.
4. Untrusted source or identity: a fork, PR ref or unapproved workflow cannot publish, initialize the database or run a privileged private runner. Covered by Task 8 reference-validation tests and Task 10 denied-context checks.
5. Private networking and incomplete destruction: no public game/database route; cleanup reports remaining owned resources, scheduled deletions and state versions instead of reporting success from an exit code. Covered by Task 6 mocked Terraform contracts, Task 9 inventory tests and Task 11 AWS inspection.

---

## File ownership and shared interfaces

| Paths | Responsibility |
|---|---|
| `src/WildBunch.Api/Program.cs`, `src/WildBunch.Api/Health/DatabaseReadinessCheck.cs` | Development-only migration startup and generic liveness/readiness HTTP contract. |
| `tests/WildBunch.Integration.Tests/Deployment/` | Real PostgreSQL and HTTP proof of the new startup/probe boundary. |
| `.dockerignore`, `deploy/containers/{api,frontend,migrations}.Dockerfile`, `deploy/containers/nginx.conf` | Root-context production builds, non-root serving, migration bundle and frontend routing. |
| `deploy/kubernetes/base/`, `deploy/kubernetes/overlays/{local,aws}/`, `deploy/kubernetes/jobs/migrate.yaml` | Workloads, services, probes, local PVC and environment customization. No populated Secret source. |
| `deploy/database/init.py`, `deploy/database/roles.sql`, `deploy/database/requirements.txt` | Idempotent private database initialization and role grants. SQL identifiers are fixed; passwords are supplied through parameterized driver operations. |
| `deploy/terraform/bootstrap/`, `deploy/terraform/environment/` | Separate state/trust and disposable infrastructure roots, provider locks and mocked contract tests. |
| `tools/deployment/{process,contracts,images,local,release,source,inventory}.py`, `tools/deployment/tests/`, `tools/deployment/requirements.txt` | Small operational modules, explicit subprocess handling, validated non-secret contracts, release ordering and ownership-aware inspection. |
| `.github/workflows/{ci,learning-infrastructure,learning-release,learning-database-init}.yml` | Existing authoritative CI plus separately invoked trusted infrastructure, release and initialization workflows. |
| `deploy/README.md`, `deploy/runbooks/{local,aws-bootstrap,release,recovery,teardown}.md` | Reusable operating instructions and explanation of what observations prove. No development diary. |

Use Python 3.12 for portable orchestration on Windows and Linux. The operational entry point is `python -m tools.deployment.<module>` from the repository root; Windows may use `py -3`. Use subprocess argument lists, not shell-built commands. `process.run_checked(args: list[str], *, env: dict[str, str] | None = None, stdin: str | None = None) -> str` returns stdout or raises a redacted error; sensitive operations return only selected metadata. Do not expose secret-bearing output through generic exceptions.

`EnvironmentContract` contains `region`, `owner_id`, `cluster_name`, `namespace`, three ECR repository URLs, the runtime and migration secret ARNs, RDS endpoint/CA identifier, private runner project names, and the private release-storage bucket/prefix. It contains no credentials. `ReleaseContract` contains `release_id`, full `source_sha`, three `repository@sha256:digest` image references, immutable ConfigMap and Secret names, and a compatibility acknowledgement identifying the previous recovery release. Serialize these contracts as JSON outside Git; reject unknown fields, invalid digests, wrong region and namespace/owner mismatch.

Use namespace `wild-bunch-learning`, frontend port 8080, API port 8080 and stable browser origin `http://127.0.0.1:8088`. Release IDs combine the full source SHA and GitHub run ID/attempt, or a local monotonically distinct invocation ID. Distinct releases never reuse migration Job names. Persist non-secret rendered release manifests and contracts privately under `releases/<release_id>/` in the bootstrap S3 bucket. This is operational recovery state, not a repository test receipt. Keep credential values out of that storage.

## Task 1: Separate API startup, liveness and readiness

**Files:** Modify `src/WildBunch.Api/Program.cs`; create `src/WildBunch.Api/Health/DatabaseReadinessCheck.cs`, `tests/WildBunch.Integration.Tests/Deployment/DeploymentApiFactory.cs`, and `tests/WildBunch.Integration.Tests/Deployment/DeploymentHealthTests.cs`. Reuse `PostgreSqlTestDatabase` and existing fixtures; do not change domain/application APIs.

**Consumes:** Existing persistence registration, EF migration assembly and `ConnectionStrings__WildBunchPostgresDb`. **Produces:** `/health` returns 200 for a serving process; `/health/ready` returns 200 only when the runtime identity connects and required migration IDs are present, otherwise generic 503.

- [x] Build an isolated factory accepting `environment`, database connection and whether to initialize the schema. Write HTTP tests for Production on an empty database, a fully migrated database, an unavailable database, and a database with a later migration-history row. Assert the empty database still has no migration-history table after startup. Test Development separately to preserve its existing migration convenience. Never stop the shared developer PostgreSQL service for a test.
- [x] Start the test database with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`; run `dotnet test tests/WildBunch.Integration.Tests --filter FullyQualifiedName~DeploymentHealthTests`. Establish failures for the missing readiness endpoint and Production startup mutation.
- [x] Gate the existing call and register an ASP.NET Core readiness health check. Use a fresh scoped DbContext, bounded connection/command timeout and cancellation; obtain required IDs from the image's EF migration assembly and applied IDs from the database. Do not compare only latest IDs or require equality. The implementation shape is:

```csharp
if (app.Environment.IsDevelopment())
{
    app.Services.ApplyWildBunchMigrations();
}
// DatabaseReadinessCheck checks connectivity and this subset condition:
// requiredIds.All(appliedIds.Contains)
// Failures yield a generic unhealthy result; HTTP output contains no exception.
```

- [x] Keep the liveness path independent of the readiness check. Map readiness with `HealthCheckOptions.Predicate` selecting only the database check and a generic response writer. The readiness check must not apply migrations or deserialize whole games.
- [x] Run the focused tests and existing Production developer-denial tests with `dotnet test tests/WildBunch.Integration.Tests --filter "FullyQualifiedName~DeploymentHealthTests|FullyQualifiedName~DevEndpointTests"`. Commit the task normally. Exit: API can start before migration in Production and reports its unavailable dependency accurately.

## Task 2: Build production Linux images and verify routing

**Files:** Create `.dockerignore`, the three Dockerfiles, `deploy/containers/nginx.conf`, `tools/deployment/process.py`, `tools/deployment/images.py`, initial `tools/deployment/tests/test_process.py`, and container instructions in `deploy/README.md`; modify `src/WildBunch.Web/package-lock.json` and `package.json` only if the advisory assessment requires a focused fix.

**Consumes:** Task 1 endpoint contract and existing frontend assets. **Produces:** Local tags `wild-bunch/{frontend,api,migrations}:<full-sha>`, all built from root context for `linux/amd64`.

- [x] Run `npm audit --omit=dev --json` in `src/WildBunch.Web`, inspect the current seroval advisory and actual dependency/import reachability using primary upstream sources. Make only a necessary compatible dependency correction and run the web tests/build; if remediation needs product or framework redesign, report the concrete blocker before deploying. Store no audit receipt in Git.
- [x] Implement argument-list subprocess execution with nonzero-exit propagation and redaction. Unit tests execute a child process that exits 7 and a child receiving secret stdin; assert failure reports neither stdin nor secret output. Run `py -3 -m unittest discover -s tools/deployment/tests -v`.
- [x] Build the API with a pinned .NET 10 SDK builder and ASP.NET 10 runtime; set `ASPNETCORE_HTTP_PORTS=8080`, `ASPNETCORE_ENVIRONMENT=Production`, and non-root runtime user. Build an EF migration bundle from `WildBunch.Persistence` with startup project `WildBunch.Api`, target `linux-x64`; copy it, its required runtime files and sanitized configuration into a separate runtime image. Supply the connection through the environment, never `--connection` arguments.

```text
dotnet ef migrations bundle --project src/WildBunch.Persistence --startup-project src/WildBunch.Api --configuration Release --self-contained --runtime linux-x64 --output /out/efbundle
```

- [x] Build frontend from root context, including `src/WildBunch.Assets/production`; set `ENV VITE_API_BASE_URL=""` before `npm run build`. Use a pinned non-root Nginx image on 8080. Pin base-image digests after inspecting available supported images; commit the resulting exact digests and EF tool lock. No floating `latest` tags.
- [x] Configure Nginx `location /api/ { proxy_pass http://api:8080; }` so the prefix survives; add `location = /api` with equivalent proxy behavior. Give known asset prefixes and file extensions `try_files $uri =404`; browser routes use `try_files $uri $uri/ /index.html`. Add a static `/health` and non-secret `X-Wild-Bunch-Release` response header populated at build time from the full SHA.
- [x] Implement `images build --source-sha <full-sha>` validating the checked-out source identity and building all three images. Verify `docker inspect` reports non-root users, final API image has no SDK, a browser deep link serves HTML, a missing asset returns 404 and a missing API route preserves API 404. Run actual images on a disposable internal Docker network with no database password in command arguments; stop/remove only those named containers/network afterward. Commit normally. Exit: reproducible runnable production artifacts with intact assets and routing.

## Task 3: Initialize restricted database identities safely

**Files:** Create `deploy/database/init.py`, `roles.sql`, pinned `requirements.txt`, `tools/deployment/tests/test_database_init.py`, and `deploy/database/tests/test_postgres_roles.py`; create `tools/deployment/requirements.txt` for operational dependencies.

**Consumes:** Task 2 migration image and PostgreSQL; later AWS execution supplies secret ARNs and private endpoint. **Produces:** Database `wildbunch`, schema `public` owned by `wildbunch_migration`, runtime role `wildbunch_runtime`, and separate JSON credential secrets with `username`, `password`, `host`, `port`, `database`. Never a Terraform secret version.

- [x] Define `initialize_database(admin_connection, secret_store, endpoint) -> None` with a small secret-store interface `read(name) -> dict | None`, `create_if_absent(name, value) -> dict`. Production uses boto3 Secrets Manager; tests use an in-memory adapter. Generate passwords with `secrets`, create a secret only when absent and recover its established value on rerun. Treat access errors as errors, not missing secrets.
- [x] Write tests for rerun preservation, a secret created before database failure, an existing role with a missing secret, a credential mismatch, and secret-store denial. For the latter three, refuse automatic password reset and explain the explicit reconciliation action without logging credentials. The critical rerun assertion is:

```python
initialize_database(admin_connection, store, endpoint)
before = store.read("runtime")["password"]
initialize_database(admin_connection, store, endpoint)
assert store.read("runtime")["password"] == before
```

- [x] Revoke public database/schema creation privileges, create identities with no superuser, role-creation or database-creation authority, and make the migration role own its schema and objects. Grant runtime CONNECT, schema USAGE, table SELECT/INSERT/UPDATE/DELETE and required sequence USAGE/SELECT. Set default privileges as the migration owner for future tables/sequences. Quote identifiers and the non-bindable role password with psycopg composition; bind ordinary query values. Reconnect using each persisted credential to verify established users instead of rewriting their password.
- [x] Against a disposable local PostgreSQL database, run the migration bundle under the migration identity, perform a normal game persistence operation under runtime credentials, then attempt `CREATE TABLE`, `ALTER TABLE` and `DROP TABLE` as runtime and require permission-denied errors. Create an additional temporary table/sequence as migration owner and verify future grants work; remove them before readiness/game checks. Rerun initialization and verify the game remains.
- [x] Run `py -3 -m unittest discover -s tools/deployment/tests -v` and `py -3 -m unittest discover -s deploy/database/tests -v` with credentials supplied through process environment. Commit normally. Exit: initialization survives partial failure without silently rotating credentials and the API identity cannot migrate.

## Task 4: Run the complete private release locally

**Files:** Create `deploy/kubernetes/base/{kustomization,namespace,frontend,api,services}.yaml`, `deploy/kubernetes/overlays/local/{kustomization,postgres,config}.yaml`, `deploy/kubernetes/jobs/migrate.yaml`, `tools/deployment/contracts.py`, `tools/deployment/local.py`, `tools/deployment/tests/test_contracts.py`, and `deploy/runbooks/local.md`.

**Consumes:** Tasks 1-3 images, roles and probes. **Produces:** A Production game on `kind-wild-bunch-learning` using a persistent PostgreSQL volume, separate runtime/migration Secrets, and a completed unique migration Job before app rollout.

- [ ] Validate contract inputs before invoking kubectl: full SHA, digest shape for AWS, local image-tag exceptions restricted to the local context, namespace and owner. Negative tests reject a wrong context, wrong region, malformed digest and attempts to mix owner IDs.
- [ ] Define PostgreSQL 16.15 with a PVC, internal Service and `pg_isready` probe. Generate disposable local credentials into memory, initialize roles, then pass Secret manifests via kubectl stdin. No secrets in the overlay or generated files inside Git. Use `kind load docker-image` with the three exact local tags.
- [ ] Start API requests at 250m CPU/256Mi and limits 1 CPU/512Mi; frontend at 100m/64Mi and limits 500m/128Mi; migration at 250m/256Mi and limits 1 CPU/512Mi; local PostgreSQL at 250m/256Mi and limits 1 CPU/512Mi. Observe scheduling and memory during the player journey; adjust documented requests if actual use requires it, without inventing capacity claims.
- [ ] Add startup probes at `/health` every 2 seconds with threshold 60, liveness at `/health` every 10 seconds with threshold 3, and API readiness at `/health/ready` every 5 seconds with threshold 2 and 2-second timeout. Frontend readiness checks its serving endpoint. Use ClusterIP Services, explicit container ports, non-root security contexts, dropped capabilities and no privilege escalation. Disable service-account token automount for application/migration Pods. Mount only each workload's own secret and configuration.
- [ ] Implement `local up`, `local release`, `local down`, and `local port-forward`; all mutation commands require the exact kind context. `up` creates/configures local dependencies, `release` runs the unique Job with `backoffLimit: 0`, `activeDeadlineSeconds: 300`, waits before applying app workloads, and retains failed jobs for inspection. `down` explains synthetic-data deletion and removes only this exercise's resources. Use `kubectl --context kind-wild-bunch-learning -n wild-bunch-learning port-forward --address 127.0.0.1 service/frontend 8088:8080`.
- [ ] Render with `kubectl kustomize deploy/kubernetes/overlays/local`, apply dependencies, complete the migration and verify `kubectl rollout status` for both deployments. Through the browser complete setup, start, and buy an available item; verify cash/inventory through `/api/games/{id}`. Record API Pod UID transiently, delete that Pod, verify a different UID and another persisted mutation on the same game. Reconnect the port-forward if necessary, keeping origin unchanged.
- [ ] Scale local PostgreSQL to zero, wait for readiness to fail, and query API liveness directly through a temporary loopback API forward; require 200 and no liveness restart increase. Restore PostgreSQL and verify readiness and the same game recover. Reuse existing Production developer-denial tests and verify a denied developer request in the deployed API. Commit normally. Exit: local runtime proves production startup, persistence and dependency failure behavior.

## Task 5: Describe Terraform state, bootstrap trust and credential prerequisites

**Files:** Create `deploy/terraform/bootstrap/{versions,providers,state,identity,variables,outputs}.tf`, `.terraform.lock.hcl`, `tests/bootstrap.tftest.hcl`, `deploy/runbooks/aws-bootstrap.md`; modify `.gitignore` for Terraform caches/state/plan files and operational JSON outside tracked fixtures.

**Consumes:** Agreed repository `HarleyBartles/wild-bunch`, AWS project/account access and the `wild-bunch` login profile. **Produces:** Protected S3 backend, bootstrap/environment keys, image-publisher and environment-provisioning roles, and private release-state prefix. No AWS creation in this task.

- [ ] Pin Terraform `= 1.16.5`, AWS provider `~> 6.0`, and commit the exact provider lock chosen by initialization for Windows and Linux. Use direct provider resources rather than a third-party EKS module so resource ownership stays inspectable. Bootstrap starts with local state outside Git; document the separate protected backend configuration and migration to `bootstrap/terraform.tfstate`. Environment uses `environment/terraform.tfstate`.
- [ ] Configure S3 encryption, versioning, public-access blocking and `use_lockfile = true`. State permissions distinguish bucket listing, state objects and lockfile objects. Keep release-state access limited to `releases/*`; release jobs cannot read Terraform state.
- [ ] Define OIDC trust for `aud = sts.amazonaws.com` and `sub = repo:HarleyBartles/wild-bunch:environment:wild-bunch-learning`. GitHub environment deployment rules restrict execution to `main`; workflow guards require the workflow itself to run on `refs/heads/main`. Explain that an environment subject does not itself encode branch or workflow authorization. Reuse/import an existing GitHub OIDC provider if discovered, recording external ownership; never delete it during teardown if pre-existing.
- [ ] Give image publication only ECR token/push permissions for the three exercise repository ARNs. Give infrastructure role state-key access and service-specific environment lifecycle permissions, with `iam:PassRole` restricted to named exercise roles and intended services. Enumerate necessary EC2/VPC, EKS, RDS, ECR, CodeBuild, connection, Secrets Manager metadata, logs and IAM role operations; explain required wildcard create/discovery permissions. No `AdministratorAccess`, no `secretsmanager:GetSecretValue` in ordinary infrastructure/image publication roles.
- [ ] Document local Terraform credential resolution without printing exported credentials. Prefer the existing profile if the locked provider supports login credentials; otherwise create a separate local `wild-bunch-terraform` profile using `credential_process = aws configure export-credentials --profile wild-bunch --format process`. Do not make that profile recursively refer to itself. Verify identity using provider account metadata in a read-only plan when AWS execution is authorized.
- [ ] Run `terraform -chdir=deploy/terraform/bootstrap init -backend=false`, `terraform -chdir=deploy/terraform/bootstrap fmt -check`, `terraform -chdir=deploy/terraform/bootstrap validate` and `terraform -chdir=deploy/terraform/bootstrap test` with mocked AWS provider. Contract tests assert encrypted/private/versioned storage and expected trust subject, and reject inappropriate repository/environment inputs. Commit normally. Exit: bootstrap source validates without creating resources.

## Task 6: Describe the disposable AWS environment

**Files:** Create `deploy/terraform/environment/{versions,providers,backend,network,eks,ecr,rds,secrets,codebuild,permissions,variables,outputs}.tf`, `.terraform.lock.hcl`, `tests/environment.tftest.hcl`, `deploy/kubernetes/overlays/aws/{kustomization,config}.yaml` and initializer CodeBuild configuration `deploy/database/buildspec.yml`.

**Consumes:** Task 5 backend and role identities; Tasks 2-4 workload resource needs. **Produces:** `EnvironmentContract` output and managed infrastructure source. No apply or paid provisioning in this task.

- [ ] Use VPC `10.42.0.0/16`, two public subnets and two private subnets in distinct supported AZs. Configure one NAT gateway/EIP, private default routes through it, and no public IP assignment to workers, RDS or CodeBuild. Validate the engineer's EKS public administration CIDR is one IPv4 `/32`, rejecting `0.0.0.0/0`; enable private cluster access simultaneously. Review subnet discovery tags so no application load balancer is accidentally requested.
- [ ] Set EKS 1.36, one managed AL2023 x86_64 node group of `m7i.large`, desired/minimum 1 and maximum 2 for managed replacement capacity, with no autoscaler. Fit system workloads, migration and surge Pods; verify instance/AZ availability and supported EKS minor again before apply. Enable API/audit/authenticator control-plane logs with 7-day retention. Pin managed add-on versions compatible with the chosen cluster version during read-only preflight; do not silently select an unsupported alternative.
- [ ] Use RDS PostgreSQL `16.15`, `db.t4g.small`, 20GiB encrypted gp3 storage, single AZ, no public access, managed master password, no automatic minor upgrade during the short exercise, and no final snapshot for explicitly disposable data. Configure PostgreSQL 16 parameter group with `rds.force_ssl=1`. Before apply recheck engine/class availability. Allow port 5432 only from the node-group SG and initializer CodeBuild SG. Create runtime/migration secret containers without Terraform plaintext secret versions.
- [ ] Give nodes only EKS node/ECR pull responsibilities, including the required CNI identity arrangement. Give the private release runner EKS discovery, its namespace-scoped Kubernetes access, read of runtime/migration secrets, scoped ECR inspection and private release-storage access. Give the separate initializer project master/runtime/migration secret access and writes only to the two generated credential secrets. Neither role shares infrastructure-provisioning authority. Enumerate exact named-resource ARNs in policies.
- [ ] Establish EKS access entries for the engineer and release identity. Engineer initially installs namespace/RBAC using authenticated admin access; release identity uses a namespace Role with Deployments, Services, ConfigMaps, Secrets, Jobs and read-only Pod/event/log inspection. Do not grant cluster-admin to the release runner. Node kubelet needs to pull images but API Pods receive no AWS identity or mounted Kubernetes token.
- [ ] Create immutable-tag ECR repositories and force-delete only exercise-owned repositories on authorized teardown. Create a GitHub App CodeConnections connection, private CodeBuild runner project and separate initializer project. Configure CodeBuild ENI permissions with VPC/subnet restrictions, bounded execution timeout and 7-day logs. Pin a supported CodeBuild Linux image; install locked operational dependencies, kubectl 1.36.x and Terraform only where needed. Initializer source resolves only the trusted full SHA; its service role retrieves the current master secret inside the private network.
- [ ] Restrict runner webhook to `WORKFLOW_JOB_QUEUED`, repository, `HEAD_REF = ^refs/heads/main$` and exact release workflow name, using provider-supported filters. Verify behavior against current AWS docs and the resulting webhook rather than assuming every filter applies identically across event types. If branch filtering is ineffective, stop privileged runner activation until an equivalent enforced restriction is reviewed. Terraform owns the connection resource; the human completes GitHub App authorization before it becomes usable.
- [ ] Supply RDS CA trust to API/migration images or mounts using the verified regional CA bundle; use Npgsql `SSL Mode=VerifyFull` with explicit root certificate and matching DNS hostname. Hash/pin the retrieved trust bundle input and document refresh. Never use `Trust Server Certificate=true` to resolve a TLS failure. AWS overlay has no PostgreSQL workload and no public service type.
- [ ] Run `terraform -chdir=deploy/terraform/environment init -backend=false`, `fmt -check`, `validate`, and `test`. Mocked tests assert private database/worker placement, restricted endpoint CIDR, managed master password, separate secret containers/roles, no public game route and expected outputs. Invalid CIDR/region must fail variable validation. Render the AWS Kustomize overlay with synthetic non-secret inputs to verify syntax. Commit normally. Exit: reviewable environment graph and configuration, with live provisioning evidence explicitly outstanding.

## Task 7: Implement migration ordering and explicit release recovery

**Files:** Create `tools/deployment/release.py`, `tools/deployment/tests/test_release.py`, and `deploy/runbooks/{release,recovery}.md`; extend contract validation and the migration Job template.

**Consumes:** Tasks 1-6 workload/secret/interface contracts. **Produces:** `release deploy --environment <json-path> --release <json-path>`, `release inspect --release-id <id>`, and `release recover --release-id <known-good-id>` with nonzero failure status and private recoverable manifests.

- [ ] Make namespace-scoped configuration and Secret names immutable per release. Fetch only runtime/migration secret values in the authorized runner, construct verified connection strings in memory, and apply populated Secrets via stdin without echoes. API and migration objects reference different Secret names. Persist the non-secret manifest plus credential source VersionIds for diagnosis; never persist values or overwrite previous release dependencies. Do not rotate passwords during the update/recovery exercise.
- [ ] Implement release ordering as the following bounded state machine. Keep migration Jobs retained for inspection and prohibit automatic retry:

```text
validate contracts and trusted source
inspect any existing Job for this release ID
prepare immutable ConfigMaps and per-release Secrets
create migration Job only if no such Job exists
wait for Job completion; failed/uncertain -> fail without app apply
verify required migration IDs with runtime credentials
save previous known-good contract and rendered manifest privately
apply new frontend/API manifest using digest images
wait for both rollout statuses; verify imageID and HTTP/game checks
mark this release known-good only after verification
```

- [ ] Write unit tests around injected command/secret/storage adapters, testing observable call order rather than YAML strings: migration failure never calls app apply; a timeout reports the existing Job and never creates another; a failed app rollout never advances known-good; recover applies the full stored manifest/configuration references. Test secret retrieval/apply failure emits no values and cannot mark success.
- [ ] Set release concurrency to queue, not cancel running jobs. A retry inspects the existing Job: completed allows continuation; running requires waiting/inspection; failed requires deliberate diagnosis and a distinct authorized attempt. Runner loss does not delete a Job or prove the database operation stopped. Apply no automatic down migration.
- [ ] Before migrating, require a concise compatibility acknowledgement covering the current and recovery app against schema, constraints, stored snapshots/events and writes made by the new release. Missing acknowledgement stops release. For this exercise both releases use the same schema; do not invent a migration. If compatibility cannot be established later, stop automatic rollback and use an explicit data/schema recovery decision.
- [ ] Run `py -3 -m unittest discover -s tools/deployment/tests -v`; exercise the same release/recover logic locally by substituting the local secret adapter and artifact directory outside Git. Deliberately set API readiness path to `/health/incorrect`, observe rollout failure while the previous ready replica serves, restore the full known-good manifest and resume the existing game. Commit normally. Exit: migration and deployment failures have explicit, tested recovery behavior.

## Task 8: Wire trusted GitHub workflows and offline checks

**Files:** Create the three learning workflows, `tools/deployment/source.py`, `tools/deployment/tests/test_source.py`; modify `.github/workflows/ci.yml` only to add deployment unit tests and offline Terraform/manifests validation; extend `deploy/README.md`.

**Consumes:** Tasks 5-7 source and operational commands. **Produces:** Separate manual infrastructure lifecycle, database initialization and image/release workflows, all requiring trusted source and preserving existing canonical CI authority.

- [ ] Implement `resolve_trusted_source(ref: str, approved_branch: str = "main") -> str`: fetch trusted origin, resolve a full commit, require ancestry on `origin/main`, reject PR/fork refs and unexpected repositories, and require privileged workflow execution itself on main. Validate ref inputs before passing subprocess arguments. Unit tests use temporary Git repositories with main, unrelated history and malicious/ref-option inputs; include accepted historical main commit and rejected unmerged branch.
- [ ] Infrastructure workflow has explicit `plan`, `apply`, `destroy-plan` and `destroy` modes, `wild-bunch-learning` environment, OIDC credentials and one non-cancelling infrastructure concurrency group. Review a saved plan before apply; hold it only on the authorized runner/protected storage and apply the reviewed exact plan. Do not upload it publicly. Bootstrap creation/final destruction stays local; environment create/destroy runs outside private CodeBuild. Use backend locking in addition to concurrency.
- [ ] Release workflow validates/builds the selected full SHA on a GitHub-hosted runner, runs canonical CI plus focused deployment checks, publishes three ECR images with immutable tags, resolves repository digests and transfers only the non-secret ReleaseContract. The private runner uses `runs-on: codebuild-<project-name>-${{ github.run_id }}-${{ github.run_attempt }}` and receives no arbitrary code ref. Keep `permissions` minimal and pin actions to reviewed full commit SHAs. CodeBuild service credentials supply private execution permissions; never copy an engineer's login credentials into GitHub.
- [ ] Database-init workflow explicitly starts the separate private initializer project for the trusted SHA and waits for status. Do not run master-secret initialization in the ordinary release job. Human GitHub App consent and GitHub environment rules are documented prerequisites; source can describe them but cannot claim consent was provisioned by Terraform.
- [ ] CI runs `python -m unittest discover -s tools/deployment/tests -v`, mocked Terraform `test` plus `fmt -check`/`validate`, and Kustomize render validation without AWS credentials. Preserve `py -3 tools/run.py ci --check` and existing PostgreSQL behavior. Document which container/local/AWS checks require external runtimes and are separate from this gate. Commit normally. Exit: machinery is reviewable before paid execution; PR/fork contexts have no path to privileged private execution.

## Task 9: Prepare ownership-aware inspection and teardown guidance

**Files:** Create `tools/deployment/inventory.py`, `tools/deployment/tests/test_inventory.py`, `deploy/runbooks/teardown.md`; maintain `deploy/README.md` links.

**Consumes:** Tasks 5-8 resource ownership, state keys and `EnvironmentContract`. **Produces:** `inventory inspect --environment <json-path>` and a teardown procedure ready for review before any paid creation.

- [ ] Implement read-only inventory returning named owned resources and actual status from state plus exercise tags/names. With injected AWS adapters test that scheduled secret deletion and still-present NAT/EIP/log groups count as remaining, foreign ownership is refused, and permission-denied discovery cannot become an empty inventory. Include ECR images, EC2 volumes, RDS-managed secrets, CodeBuild connections/logs and S3 versions/delete markers in inspection scope.
- [ ] Write the exact environment-then-bootstrap destruction sequence used by Task 11, including bootstrap backend migration to protected local storage before removing the bucket. Tie every inspection to owned names/ARNs; never delete solely from an account-wide service listing. Scheduled deletion remains outstanding until verified removed.
- [ ] Run `py -3 -m unittest discover -s tools/deployment/tests -v`, perform local synthetic-inventory checks and commit normally. Exit: cleanup source and guidance are concrete at the AWS approval boundary.

## AWS execution boundary

Tasks 1-9 build and verify source and local behavior after implementation is approved. Before Task 10, present the actual Terraform plans, identities, network routes, resource inventory, expected ongoing charges, GitHub consent steps and teardown procedure. Obtain explicit authorization for paid creation, the exercise's planned destroy/recreate cycle and final synthetic-data deletion. Do not infer this authorization from plan approval, cost indifference, AWS login or earlier ambiguous “Approved” messages. Human approval, GitHub App consent and merge into trusted main are prerequisites, not unchecked implementation tasks.

## Task 10: Execute and verify the AWS learning exercise

**Files:** Update reusable guidance in `deploy/runbooks/aws-bootstrap.md`, `release.md` and `recovery.md` only when actual behavior exposes a correction; any implementation corrections belong to their owning source files and tests. Keep runtime evidence outside Git.

**Consumes:** Committed reviewed Tasks 1-9, trusted-main workflow availability, authorized paid execution and GitHub consent. **Produces:** Verified AWS lifecycle, player journey, Pod replacement, update and failed-release recovery evidence.

- [ ] Read-only preflight confirms actual identity/account, `eu-north-1`, service/project restrictions, EKS 1.36 availability, instance/AZ capacity choices, RDS 16.15/class support and current engineer IPv4 CIDR. Use the official AWS MCP server for interactive AWS inspection. Stop on permission/topology blockers instead of silently broadening access or substituting architecture.
- [ ] Review/apply bootstrap using protected local state, migrate bootstrap state into its S3 key, initialize the separate environment backend, review/apply the environment, finish human GitHub authorization, and configure main-only environment rules. Inspect actual EKS endpoints, routes, security groups, identities, RDS private/TLS settings and webhook filters against the reviewed design.
- [ ] Before gameplay, destroy the empty environment and recreate it from the same committed configuration while bootstrap remains. Verify that state reflects destruction and actual owned resources disappear or report pending removals. Repeated initialization must support both fresh and recreated resources without accidentally reusing deleted credentials.
- [ ] Install namespace/RBAC as the engineer, invoke database initialization, then run the trusted release workflow. Verify runtime DDL denial, migration Job success/history, digest image identities, API readiness and developer-operation denial. Verify TLS rejects an incorrect CA/hostname in an isolated negative connection check; do not alter production trust to obtain a passing connection.
- [ ] Connect through loopback frontend forwarding. Complete a real setup/start/purchase journey, confirm persisted cash/inventory via API, replace the API Pod, verify new UID and resume the same game with another mutation. Inspect persistence using the permitted private connection, without exposing hidden gameplay truth in logs.
- [ ] Build/publish a distinct frontend image using a deployment-only release-header change, then release immutable digests and confirm header, actual image IDs and continued gameplay. Run the deliberately incorrect readiness-path release, confirm old replica serves while rollout fails, inspect events/conditions and explicitly recover the previous full manifest. Confirm the pipeline reports failure and the original game remains usable.
- [ ] Attempt an untrusted ref through the source validator and a non-main/non-approved workflow context through GitHub controls. Require denial before privileged AWS execution; do not intentionally run arbitrary fork code inside the private runner to prove rejection. Inspect effective IAM/RBAC access, including master-secret denial to release identity and absence of AWS credentials in the API Pod.
- [ ] Correct any observed gap with focused behavior proof and a normal hooked commit. Exit: each required AWS demonstration has direct evidence; local checks and unit tests are not substituted for networking/identity/lifecycle proof.

## Task 11: Verify teardown and hand off reusable machinery

**Files:** Correct owning deployment files/runbooks only if execution exposes a gap; narrowly update `CONTRIBUTING.md` to point to deployment guidance. Runtime inventory/state stays outside Git.

**Consumes:** Task 9 inspection tooling and Task 10 managed environment, state and operational recovery objects. **Produces:** Verified teardown and a verified absence report in the conversation.

- [ ] After the demonstrations, remove application workloads and active/transient Jobs, inspect migration execution has stopped, then review/apply environment destruction from the local machine or GitHub-hosted runner. Include ECR contents, NAT/EIP, EC2/volumes, RDS, generated and managed secrets, runner/initializer projects, connection and logs. Report AWS-scheduled deletion explicitly and recheck eventual removal; never claim immediate absence from Terraform success alone.
- [ ] Migrate bootstrap state back to protected local storage while the backend still exists. Remove only exercise-owned private release objects, all S3 state object versions/delete markers and lockfiles before deleting the bucket. Destroy bootstrap roles and owned trust resources last; preserve pre-existing OIDC providers or GitHub resources. Keep local recovery state until AWS inventory verification completes, then deliberately dispose of it outside Git.
- [ ] Run the repository's required final validation through the normal hooked correction commit or canonical uncommitted gate as appropriate, and verify the current diff contains only intended machinery/guidance. Report existing skips separately. Obtain fresh whole-branch review using the repository review runbook; fix material findings and reverify the changed behavior. Publishing a PR requires the user's publication instruction or the approved execution handoff scope.
- [ ] Hand the engineer reusable commands and an explanation exercise: follow browser-to-database traffic, distinguish Terraform state from infrastructure, identify AWS/Kubernetes identities, explain migrations versus app/payload versions, diagnose the failed release, and account for teardown. Discuss simpler static/container hosting as the product alternative to learning-driven EKS. Do not write the portfolio article in this slice or call the exercise production operations experience.

**Exit:** Source and operating guidance are verified, the required observations are explained accurately, and every exercise-owned AWS resource is absent or explicitly tracked until its verified removal. Retain this plan/spec while they still govern incomplete exercise work; apply repository completed-artifact custody only when their full scope is actually complete.

## Documentation used at implementation seams

- [AWS CLI credential-process export](https://docs.aws.amazon.com/cli/latest/reference/configure/export-credentials.html) explains bridging the local login profile to SDK/provider consumers without static keys.
- [Terraform S3 backend](https://developer.hashicorp.com/terraform/language/backend/s3) defines native lockfiles and backend permissions; [provider locks](https://developer.hashicorp.com/terraform/language/files/dependency-lock) explain exact resolved versions.
- [CodeBuild Actions runner](https://docs.aws.amazon.com/codebuild/latest/userguide/action-runner.html) and [Terraform webhook resource](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/codebuild_webhook) define execution labels and filters. Verify filters for the actual event type before assigning privilege.
- [RDS PostgreSQL release calendar](https://docs.aws.amazon.com/AmazonRDS/latest/PostgreSQLReleaseNotes/postgresql-release-calendar.html) and live `DescribeDBEngineVersions`/`DescribeOrderableDBInstanceOptions` support the selected 16.15 target; repeat regional checks before provisioning.
- The approved specification links primary EKS, GitHub OIDC, RDS credentials/TLS, EF migration and Kubernetes probe/deployment documentation. Read those sources at the corresponding task rather than treating remembered provider syntax as current authority.
