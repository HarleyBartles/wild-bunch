# Private AWS, Kubernetes and Terraform learning deployment

## Purpose and boundaries

Deploy the existing Wild Bunch pre-alpha as a practical learning exercise for senior full-stack engineering interviews. The engineer already knows CI/CD, has authored GitHub YAML and Azure release pipelines, and has used Docker. The learning targets are AWS semantics, Kubernetes operation, Terraform lifecycle management, and applying existing engineering principles through those tools with AI as the knowledge bridge.

The outcome is a system the engineer can explain, inspect, change and diagnose. AI may supply provider syntax, research, implementation and diagnostic hypotheses throughout the exercise. The engineer remains responsible for requirements, trade-offs, review, verification and explaining observed behavior. There is no requirement to complete an artificial assessment without AI.

The deployment is private and disposable. Public access waits for player identity and authorization that isolate each player's games. Implementing that capability is outside this work. Its absence is a product discovery to discuss in the eventual portfolio article: making software deployable can reveal the next valuable product work.

This specification authorizes no paid provisioning. Local tooling setup has been separately authorized. Implementation and AWS execution follow their own approvals; creating an AWS project or authenticating a tool does not authorize infrastructure creation.

Do not add game features, public hosting, player authentication, a demonstration service, high-availability claims, a service mesh, GitOps controller, autoscaling experiment, or a general-purpose infrastructure platform. The portfolio article is a later deliverable, not part of deployment implementation.

## Existing application seams

- [`httpClient.ts`](../../src/WildBunch.Web/src/api/httpClient.ts) defaults to `http://localhost:5275`. An explicitly empty `VITE_API_BASE_URL` selects same-origin requests. Vite substitutes this value during the frontend build, not at container startup.
- [`vite.config.ts`](../../src/WildBunch.Web/vite.config.ts) copies production assets from the sibling `WildBunch.Assets` project. A frontend container build must include these inputs, not just the web directory. Browser routes require a static-server fallback to `index.html`; API requests and missing asset paths must retain meaningful errors.
- [`Program.cs`](../../src/WildBunch.Api/Program.cs) applies database migrations before serving requests and exposes `/health` as an unconditional HTTP success. It enables the localhost CORS policy and OpenAPI only in `Development`.
- [`PersistenceDbContextOptions.cs`](../../src/WildBunch.Persistence/PersistenceDbContextOptions.cs) requires `ConnectionStrings:WildBunchPostgresDb`, supplied by environment variable `ConnectionStrings__WildBunchPostgresDb` in containers. The API targets .NET 10 and persists through PostgreSQL.
- [`DevRoleGuard.cs`](../../src/WildBunch.Api/Dev/DevRoleGuard.cs) denies developer operations outside `Development`. The developer routes remain registered. [`AppShell.tsx`](../../src/WildBunch.Web/src/shell/AppShell.tsx) still offers the developer overlay without a build-environment gate. Preserve the backend restriction; do not select `Development` to make the deployed controls work. Explain the existing UI limitation without expanding this slice into shell redesign.
- The API has no configured player authentication or session ownership authorization. A game identifier is not an authorization boundary.
- Schema migrations, serialized snapshots and event payload versions are separate compatibility concerns. Preserve existing event sourcing, projection and upcaster ownership; do not redesign persistence for deployment.

## Architecture

| Component | Responsibility |
|---|---|
| Docker Desktop with WSL 2, Windows Subsystem for Linux | Build and run Linux containers on the developer machine. |
| kind, Kubernetes IN Docker | Disposable local Kubernetes cluster, with explicit creation and removal. |
| kubectl | Inspect and operate local and AWS Kubernetes clusters. |
| Terraform | Describe and manage AWS infrastructure, plan changes, maintain resource state, and destroy managed resources. |
| GitHub Actions | Orchestrate validation, artifact publication, infrastructure lifecycle and application releases. |
| ECR, Elastic Container Registry | Store immutable frontend, API and migration images. |
| EKS, Elastic Kubernetes Service | AWS-managed Kubernetes control plane, with a managed EC2 worker node group. EC2 means Elastic Compute Cloud, AWS virtual machines. |
| RDS, Relational Database Service | Private, single-AZ PostgreSQL database. AZ means Availability Zone. |
| CodeBuild | Temporary GitHub Actions runner inside the AWS network, executing database setup and release jobs. |
| Secrets Manager | Store separate database credentials and the RDS-managed administrator credential. |
| S3, Simple Storage Service | Protected remote Terraform state and lockfiles. |

The browser connects to a loopback port forwarded to the frontend. The frontend serves the production build and proxies `/api` to the API's internal Kubernetes Service. The API connects to PostgreSQL. Only the frontend is port-forwarded for the player journey; the API and database have no public entry point.

Local and AWS deployments share application images, routing, probe behavior, credential separation and release semantics. Environment-specific configuration supplies image locations, database endpoints, TLS settings and local database resources. Kubernetes manifests have a shared base and local/AWS overlays; use Kustomize, the manifest customization support included in kubectl, rather than introducing Helm for these few workloads.

Use one frontend replica and one API replica at steady state. AWS worker capacity must fit system workloads, the applications, a migration Job and an overlapping replacement API Pod. Configure application updates with zero unavailable replicas and one additional replica during rollout. This demonstrates readiness-gated releases, not resilience to node or availability-zone loss. Resource requests and limits are explicit and informed by local observation. Exact machine sizes and probe timings are implementation choices to validate, not invented performance guarantees.

## Local environment

Use Linux images and a Kubernetes minor version also supported by EKS in Stockholm. The current local cluster uses Kubernetes 1.36; verify regional support again before provisioning and pin compatible tool and node-image versions in the implementation guidance.

Run local PostgreSQL in the cluster with a PersistentVolumeClaim, which requests storage independent of the API Pod. Local database and workload credentials are disposable and distinct from AWS credentials. PostgreSQL availability gates migrations and application readiness. API replacement must preserve the game; deletion of the entire local cluster may discard local game data. No cross-cluster local backup guarantee is required.

Use a consistent loopback browser origin and port for the exercise because the browser stores the current game identifier in local storage. Port-forwarding selects a Pod and may disconnect when that Pod is replaced; reconnect the forward when necessary. Do not confuse a disconnected forward with lost application state or claim that port-forwarding demonstrates public load balancing.

## AWS network and access

All regional deployment resources live in `eu-north-1`, Europe/Stockholm, matching the new AWS experience's selected region. Do not assume that a paid plan removes project-level region or permission restrictions. The Agent Toolkit service endpoint is separate from the deployment region.

Create a dedicated VPC, Virtual Private Cloud, with subnets across at least two Availability Zones. Place worker nodes, the database and CodeBuild execution in private subnets. A single NAT, Network Address Translation, gateway in a public subnet provides outbound access through an internet gateway. The single gateway is an intentional disposable-environment availability trade-off. Do not expose applications through a public load balancer, public IP, NodePort, ingress or public database endpoint.

Enable private EKS administration access for AWS execution, plus a public administration endpoint restricted to the engineer's current public IP and authenticated AWS identity. This endpoint is not a public game route. IP changes require an explicit infrastructure update; never resolve connection failures by allowing all internet addresses. Local browser traffic travels through authenticated kubectl port-forwarding. Network placement and authorization are independent controls.

Security groups restrict PostgreSQL access to the application nodes and the explicitly authorized database-initialization execution. The database listens on its private endpoint. Connections from AWS workloads use TLS, Transport Layer Security, with certificate and hostname verification against the RDS certificate authority. Local PostgreSQL's TLS policy may differ through the local overlay.

The frontend and API use ClusterIP Services, internal Kubernetes service addresses. No Kubernetes network-policy claim is made without configuring and verifying a supporting enforcement mechanism. AWS node security groups alone do not isolate individual Pods from each other.

Keep logs sufficient for startup, migration and release diagnosis with bounded retention. Do not log connection strings, credentials, secret contents or hidden gameplay truth. No monitoring platform or alerting programme is required for this exercise.

## Infrastructure ownership and bootstrap

Maintain separate Terraform root configurations for bootstrap and the disposable environment. Bootstrap owns protected state storage and the GitHub-to-AWS trust and roles needed by later infrastructure workflows. Environment configuration owns the VPC, routes, NAT gateway, worker group, EKS cluster, ECR repositories, RDS database, secret containers, CodeBuild projects and associated scoped permissions. Kubernetes application resources remain owned by release manifests rather than a Terraform Kubernetes provider coupled to cluster creation.

The bootstrap begins locally using the authenticated `wild-bunch` AWS profile. Existing AWS project access is a prerequisite, not an infrastructure resource claimed to be created by this repository. Initially protect local bootstrap state outside Git, then migrate it to a separate key in the protected S3 backend. The environment uses a different state key. Enable encryption, public-access blocking, versioning and native S3 lockfiles. Do not introduce deprecated DynamoDB state locking.

Review Terraform plans before apply, distinguish update from replacement, and preserve state through failures. Do not upload state or saved plans to public workflow artifacts or commit them to Git. Infrastructure workflows serialize access; backend locking remains necessary even when GitHub concurrency groups also exist. A failed apply can have created resources, so inspect state and AWS before deciding the next action.

GitHub-hosted runners use OIDC, OpenID Connect, to obtain temporary AWS credentials. Trust is restricted to this repository and the intended deployment environment and workflow branch. CodeBuild uses scoped service-role credentials. Provisioning authority is separate from ordinary image publication and application release authority. Bootstrap identities are broader but are not reused by normal application releases.

The CodeBuild-to-GitHub connection requires a one-time human authorization, preferably through the supported GitHub App connection. Terraform manages the connection resource and runner infrastructure where supported; the human grants GitHub access. Restrict each CodeBuild `UseConnection` permission to `HarleyBartles/wild-bunch`, read-only Git pull and the `main` branch, and request a commit with a branch-qualified source version so the branch condition is evaluated. Document this prerequisite rather than claiming that Terraform eliminates identity consent. Bootstrap and teardown must operate from GitHub-hosted runners or the local machine without depending on a runner inside the environment being created or destroyed.

## Database users and secrets

RDS manages its administrator password in Secrets Manager. A separately invoked database-initialization operation executes within the private network, obtains that credential and establishes an application database, migration owner and restricted runtime user. Its bootstrap role is distinct from the ordinary release role. Initialization is idempotent: rerunning it preserves established passwords and game data rather than silently regenerating credentials or resetting the database.

The migration identity owns the application's schema and can apply EF Core, Entity Framework Core, migrations. The application identity can read and modify the required game data but cannot create, alter or drop schema objects. Initial grants and default privileges must allow the runtime identity to use tables and sequences created by future migrations. Verify these with real PostgreSQL behavior, including denied schema modification under runtime credentials.

Generate application and migration passwords in a controlled initialization step and store them directly in separate Secrets Manager secrets. Terraform manages secret containers and permissions, not plaintext secret versions or randomly generated passwords persisted in state. Administrator credentials are unavailable to ordinary release jobs and API Pods.

The ordinary deployment runner retrieves only the application and migration secrets, then creates or updates distinct Kubernetes Secrets in the application namespace without printing their values. The API receives only its application connection string; the migration Job receives only its migration connection string. No AWS credentials or secret-reading permission are required in the API Pod. Restrict Kubernetes secret access and do not commit populated Secret manifests. Kubernetes Secret encoding is not encryption and does not replace access control.

For this short-lived environment, automatic rotation is not required for application and migration passwords. A deliberate credential replacement updates PostgreSQL and Secrets Manager, refreshes the corresponding Kubernetes Secret and restarts the affected workload in a controlled sequence. Existing processes do not automatically reload environment variables. RDS-managed administrator rotation is independent of these credentials; initialization always retrieves its current value. Do not add a secret synchronization operator or rotation framework.

## Container and application changes

Create production frontend and .NET API images using multi-stage builds. Include only runtime artifacts in final images, use non-root processes where supported, and exclude local credentials, development settings, source assets not needed at runtime and repository scratch. Set container listening ports explicitly. Verify that the .NET publish output and frontend assets work in Linux containers without development launch profiles.

Build the frontend with an explicitly empty API base URL. Serve static files using a production server with same-origin API proxying, browser-route fallback and appropriate asset handling. Preserve API path prefixes and error responses. The deployed API uses `Production`; developer operations return denied responses.

Remove unconditional migration execution from deployed API startup. Retain development convenience only behind an explicit `Development` boundary. The release creates a separate migration image containing an EF Core migration bundle built from the same source commit as the application. It runs without a development SDK in the deployed API container. The pipeline supplies its connection string securely and checks its exit status and target migration history.

Keep `/health` as a process-liveness endpoint and add database-aware readiness at `/health/ready`. Readiness uses the runtime database identity to check connectivity and that migration history includes the migrations required by that application image, without exposing diagnostic details. Later compatible migrations may also be present; do not require exact equality with the image's latest migration, which would reject the old application during additive evolution. A healthy connection and migration history alone do not certify gameplay, actual schema integrity or payload compatibility. A database outage makes the API unready without triggering database-dependent liveness restart storms. Configure a startup probe to give initialization time before liveness takes effect. The frontend also has a suitable serving check.

The dependency vulnerability reported during initial investigation requires current advisory and reachability assessment before deployment. Resolve exploitable deployment-path issues within scope, or explicitly surface a blocking dependency issue; a private environment is not evidence that a dependency is safe. Do not perform unrelated dependency upgrades.

## Release contract

Infrastructure lifecycle and application release are separate manually invoked workflows. Keep the existing canonical CI, Continuous Integration, gate authoritative. Application release is CD, Continuous Delivery, triggered deliberately for a specific trusted commit. Resolve a supplied reference to a full commit and require it to belong to the approved deployment branch. Privileged CodeBuild jobs never execute arbitrary fork or pull-request code. Scope runner event filters and workflow access accordingly; a runner label alone is not an authorization boundary.

Validate and build on a GitHub-hosted runner, publish application and migration images to ECR, and pass immutable digests and the source commit to the AWS-hosted runner. Configure immutable image tags as an additional protection; tags alone are not release identity. Pin workflow actions and relevant build dependencies through repository conventions.

The release runner prepares namespace-scoped configuration and credentials, runs exactly one migration Job and waits for successful completion. Migration jobs use unique release identities and no automatic job retry for a failed migration. A timeout or lost runner does not prove the job stopped: inspect the existing job before any retry. Release concurrency prevents overlapping schema changes and application updates; never automatically cancel an in-progress migration to start a newer release.

Migration review covers compatibility with the currently serving application and recovery candidate, including schema, constraints, serialized snapshots, event payloads and data written by the newer version. Prefer compatible additive evolution and delayed removal. No artificial database migration or product feature is required for the learning evidence.

After migration success, update the frontend and API to the selected image digests, wait for readiness-gated rollout, verify actual running image identities and execute application checks. A failed migration stops before application rollout. A failed application rollout is a reported failure even if the previous replica remains healthy. Kubernetes does not automatically restore the desired release merely because rollout progress failed.

For recovery, restore the previous known-good release manifest and image digests explicitly, with the matching non-secret configuration and compatible secret references. Do not rely only on `kubectl rollout undo`, which does not restore arbitrary ConfigMaps or Secrets. Verify readiness, image identity and gameplay again. Keep rollback dependencies available until recovery is demonstrated; do not overwrite or garbage-collect credentials or configuration still needed by the previous release.

Never automatically reverse a database migration during application rollback. If the old application cannot use the resulting schema or new data, stop and choose an explicit forward correction or data-recovery procedure. Database restoration can lose subsequent writes; it is outside the required demonstration and must not be represented as an ordinary image rollback.

## Required demonstrations

| Scenario | Observable proof |
|---|---|
| Infrastructure lifecycle | Review a plan, create the environment from code, inspect actual resources, destroy it, and recreate it from the same configuration. Identify human identity and GitHub consent prerequisites. |
| Player journey | Through the forwarded frontend, create a game, complete the normal setup/start flow and perform a real game mutation, such as purchasing an item or advancing travel. Verify the resulting state through the API and persistence. No developer mutation endpoints. |
| API replacement | Record the same game's identifier and persisted mutation, replace the API Pod, verify a new Pod identity and resume the same game, then perform another mutation. Browser storage alone is insufficient proof. |
| Application update | Publish and deploy a distinct immutable image through a deployment-only packaging or serving-configuration change, such as a frontend release-identification response header. Verify the new digest and continued gameplay. No game feature is invented to produce a release. |
| Failed release and recovery | Apply a deliberately incorrect API readiness path while leaving database schema and credentials unchanged. Observe readiness failure, old-replica availability through the frontend's API Service, rollout failure and diagnostic evidence. Restore the previous manifest explicitly and verify the game again. The failure is identified honestly as a release-configuration fault. |
| Database failure distinction | Make PostgreSQL unavailable locally, verify readiness fails while liveness remains healthy, restore PostgreSQL and verify recovery. Do not infer this behavior only from endpoint unit tests. |
| Permission boundaries | Runtime credentials can perform the player journey but cannot change schema. Production developer operations are denied. Untrusted workflow contexts cannot obtain privileged deployment execution. |
| Teardown | Verify absence of exercise-owned billable and access resources after destruction, rather than treating Terraform's exit status alone as sufficient proof. |

Use Kubernetes events, Pod status, logs, image digests, HTTP behavior, migration history and actual persisted game state as evidence. Explain what each observation proves and what it does not. Local evidence does not substitute for AWS networking, identity or resource lifecycle evidence.

## Teardown and custody

All exercise game data is synthetic and disposable. Complete teardown discards it. Remove release workloads and transient jobs, then destroy the environment while state storage and external execution still exist. Include ECR images, NAT gateway and public addresses, disks, database, managed administrator secret, application and migration secrets, CodeBuild projects, connections and logs. Where AWS retention or secret-deletion recovery windows leave resources scheduled for removal, identify them and verify their eventual disposition rather than claiming immediate absence.

Destroy bootstrap last. Its backend cannot safely delete its own state storage mid-operation: migrate bootstrap state to protected local storage before final destruction, retain it until cleanup is verified, then dispose of it deliberately outside Git. Versioned S3 state objects and lockfiles require explicit removal before bucket deletion. Do not use indiscriminate cleanup against pre-existing AWS or GitHub resources; discover ownership from Terraform and exercise-specific tags and names.

Keep scripts, manifests, infrastructure definitions, behavioral tests and explanatory operating guidance in Git. Do not commit command transcripts, successful test receipts, screenshots, raw state, credentials or a diary of development activity. Temporary investigation evidence belongs in branch scratch. Capture a small set of redacted examples for the later article in its own authorized workflow.

The article should distinguish existing CI/CD and Docker experience, newly demonstrated AWS/Kubernetes/Terraform capability, AI assistance, engineering decisions and verified behavior from production operations experience. It should include failures and corrections and the discovery that independent player access is the next product capability needed before public hosting.

Kubernetes is chosen for learning. For ordinary Wild Bunch hosting, evaluate simpler container hosting and static frontend hosting against workload scale, team ownership and operational requirements before selecting Kubernetes. Completing this exercise does not establish that EKS is the best product hosting choice.

## Repository guidance and validation

Follow [coding discipline](../doctrine/coding-discipline.md), [architecture guardrails](../doctrine/architecture-guardrails.md), [frontend standards](../doctrine/frontend-standards.md), [validation policy](../doctrine/validation-policy.md), [artifact custody](../doctrine/artifact-custody.md) and [completed-artifact doctrine](../doctrine/completed-artifacts.md). Use the [security](../playbooks/security.md), [testing](../playbooks/testing.md), [design](../runbooks/design.md), [planning](../runbooks/planning.md) and [PR](../runbooks/pr.md) entrypoints as applicable. Preserve the [operating-standards subscriptions](../contracts/operating-standards.json) and their certification; this specification introduces no new operating-standard subscription.

Implementation supplies focused API configuration and probe behavior tests, real PostgreSQL migration and privilege checks, container-build and local-cluster checks, browser evidence for the player journey, Terraform formatting/validation/plan checks, and separately verified AWS deployment evidence. Add tests only for genuine behavioral gaps, including expected failures; do not add tests that match source strings or merely assert files exist.

The canonical repository gate remains `py -3 tools/run.py ci --check`, covering repository checks, `dotnet build`, `dotnet test`, and the web dependency install, TypeScript checks, Vitest tests and production build. Normal hooked commits and the matching GitHub checks provide delivery evidence. The gate does not prove a deployed environment; the demonstrations above do.

The implementation plan chooses file layout, instance sizes, resource requests, probe timeouts, database engine patch version, build-image versions and exact IAM, Identity and Access Management, policy statements against current documentation and observed requirements. It must preserve the ownership boundaries and behavior in this specification. If the new AWS experience's permissions or service restrictions prevent the agreed topology, return the concrete blocker for a design decision rather than silently substituting a different architecture.

## Reference documentation

- [Terraform S3 backend and native locking](https://developer.hashicorp.com/terraform/language/backend/s3)
- [GitHub Actions AWS OIDC authentication](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-aws)
- [CodeBuild-hosted GitHub Actions runners](https://docs.aws.amazon.com/codebuild/latest/userguide/action-runner.html)
- [EKS administration endpoint configuration](https://docs.aws.amazon.com/eks/latest/userguide/config-cluster-endpoint.html)
- [RDS-managed administrator credentials](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/rds-secrets-manager.html)
- [RDS PostgreSQL TLS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/PostgreSQL.Concepts.General.SSL.html)
- [EF Core migration deployment](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
- [Kubernetes startup, readiness and liveness probes](https://kubernetes.io/docs/concepts/workloads/pods/probes/)
- [Kubernetes deployment behavior](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/)
- [Supported services in the new AWS experience](https://docs.aws.amazon.com/accounts/latest/reference/supported-services-sign-up-new.html)
