# Public playability and versioned releases

Status: approved for implementation planning on 2026-10-06. This document describes the agreed direction; planning approval does not authorize implementation or changes to the purchased server.

**Dated clarification, 2026-10-07:** The user subsequently settled confirmed start over as immediate durable archival, independent of creating a replacement. That confirmed archival is not rolled back if the player leaves setup or never creates another game. The atomic-replacement language below must not override that rule; creation/retry still protects the per-user active-playthrough invariant and cannot archive another user's game. See the [lifecycle decision](../investigations/stable-0.1.0/2026-10-07-playthrough-lifecycle-contract.md) and proposed [baseline specification](2026-10-07-stable-0.1.0-baseline.md). Stable 0.1.0 cleanup precedes the separate account and deployment work.

## Outcome and scope

Make Wild Bunch a public browser game on a subdomain of `harleybartles.com`, with Google sign-in, private user-owned playthroughs, and a repeatable versioned release process. Establish the release foundations before continuing feature iteration on `develop`. The initial game remains an incomplete pre-alpha; completing gameplay and promising stability are outside this scope.

Use the existing OVHcloud VPS-1, Terraform, and Docker Compose. Run public and preprod as separate deployments on that host. Kubernetes, AWS adoption, automatic scaling, and a hosted develop environment are not initial requirements.

Preserve DDD, CQRS, event sourcing, and the existing migration strategy. Public access requires effective user isolation and absence of developer capabilities, regardless of the game's alpha status.

## Current gaps

- `WildBunch.Api/Program.cs` and `Games/GameEndpoints.cs` configure no authentication or game authorization.
- `CompletePlayerSetupHandler` queries every active session and archives them when creating a replacement. The rule currently has database-wide scope.
- `GameSessionEntity` and repository interfaces carry no owner identity. Queries and commands select sessions by game ID.
- The web app remembers a game ID in local storage rather than resolving an authenticated user's active game.
- `DevRoleGuard` allows calls based on `IsDevelopment()`. Dev routes are always registered and `AppShell` always renders the dev menu.
- CI and contribution guidance target `main`; the web package declares `0.0.0`. Release branches, versioned artifacts, and environment deployment workflows must be introduced.

These observations identify affected seams, not instructions to preserve those implementations.

## User and playthrough model

`User` denotes the person entitled to play, represented by an immutable internal `UserId`. `Player` remains the character inside a `GameSession`, with its existing name, health, wallet, inventory, and location. A character name is not an account identifier.

The identity boundary associates a verified Google account with `UserId`. Use the provider's stable subject identity, not email, as the external lookup key. First successful sign-in creates the association automatically; later sign-ins resolve the same user, including concurrent first sign-ins. Google is the only initial provider. No profile subsystem, local passwords, MFA subsystem, provider-linking UI, or user administration UI is required.

Gameplay accepts `UserId` without depending on Google claims, authentication cookies, or identity-provider libraries. A user marker does not require a rich aggregate with hypothetical behaviour. Identity records and authentication belong outside the gameplay aggregate.

Every newly created playthrough has exactly one immutable owner. A creation event establishes its `UserId`; applying that event reconstructs ownership. Any owner field used to select persisted sessions is a projection of event-backed ownership. A database column or browser-supplied value cannot independently establish a game fact.

This applies to player setup and developer preparation alike. The current zero-event `StartPrepped` path cannot establish new owned playthroughs through a snapshot alone. It must emit an ownership-establishing creation event and have a replay path while preserving the existing prepped-state semantics. Developer creation uses the authenticated preprod user and does not bypass the per-user active-playthrough rule. Supported historical zero-event data remains subject to the existing snapshot exception and the unowned-data transition policy below.

A user may own many `GameSession` aggregates over time, but at most one may be active. An unfinished setup counts as active. Starting over archives only that user's active playthrough and creates its replacement through the respective aggregates' events. The replacement is atomic: failure does not leave the old playthrough archived without its replacement. Concurrent starts must preserve the per-user invariant. The domain rule spans sessions; the implementation must coordinate and enforce it without giving a session permission to mutate another aggregate directly.

Owner transfer is not supported. Pre-existing unowned local streams must not be silently assigned to a Google user or made publicly accessible. The transition may explicitly discard those development playthroughs, separately from schema migration. It must not fabricate historical ownership in an upcaster.

## Sign-in, access, and resume

Anyone with a Google account may sign in to public and play. A validated sign-in establishes the internal user identity and a protected authentication cookie. The browser and API share the environment's origin. Use HTTPS-only, HttpOnly cookies and protection against forged state-changing requests. Validate the sign-in response through established authentication middleware, including the intended application and issuer. Unauthenticated game requests cannot read or change playthroughs.

Use a persistent authentication cookie with a 30-day lifetime and sliding renewal. Google refresh-token storage is not required for this initial sign-in-only integration. Signing out clears the environment's authentication cookie. If authentication is lost or expires, sign-in resolves the same user and reloads server state; the browser must not automatically repeat an action whose outcome is unknown.

All game reads and writes authorize the authenticated user against the target playthrough, including setup continuation, archive, journal, travel, maps, store operations, and HUD/diary projections. Game IDs do not grant access. An unauthorized user's target is returned as unavailable without disclosing game contents. Static public content and health endpoints may remain anonymous when they contain no private game state. The browser cannot select the owner by submitting a `UserId`.

An authenticated current-playthrough lookup is the authority for Continue. Local storage and route parameters are optional presentation hints. They cannot select a different user's game, assign ownership, or determine the existence of an active playthrough. Changing signed-in accounts clears previous account views and cached game data before resolving the new user's game.

Returning users with an active playthrough can Continue or Start over. Continue loads the last committed state, including partially completed setup. Start over requires confirmation. Users with no active playthrough enter setup. Leaving, closing a tab, signing out, or closing the browser neither archives nor advances a playthrough. The same behaviour applies across browsers and devices.

Multiple tabs and devices may operate on the same game. Other views may remain stale until refreshed. Commands must validate current authoritative state and preserve invariants under concurrency; stale or conflicting actions may be rejected with a refresh instruction. A response lost after commit is resolved by loading state rather than assuming the action failed. SignalR, automatic cross-tab synchronization, and a general command-replay/idempotency subsystem are deferred.

## Public and preprod boundary

| Environment | Admission | Capabilities | Data |
|---|---|---|---|
| Public | Any verified Google account | Player APIs and UI | Public database and volumes |
| Preprod | Only the owner's verified Google identity | Player APIs, existing dev APIs, and dev UI | Separate preprod database and volumes |

Preprod admission is configured against a verified stable identity, not an email label or a client-selected developer flag. It denies access to other Google users even when they have valid public accounts. This is an environment policy; the gameplay model does not need a developer-role subsystem.

Public does not register dev routes and its frontend artifact excludes the dev menu and panels. Direct requests to dev URLs cannot access hidden truth or mutate game state. Hiding a menu, naming a route `/api/dev`, or relying on a reverse-proxy rule alone is insufficient.

Preprod runs production-style hosting configuration with explicitly enabled dev capabilities. Replace the current development-environment access assumption with authenticated environment policy. Existing dev commands still prepare state through `GameSession` and dev events, preserving the dev-overlay doctrine. Local dev access must remain explicitly confined to local development; it cannot become an anonymous bypass in a hosted environment.

The environments have separate credentials, authentication keys, host-scoped cookies, networks, database containers, and persistent volumes. Neither application receives the other's database credentials. Only the reverse proxy exposes the web applications; database ports are not published externally. A shared host remains a shared resource and failure boundary, not separate machine-level isolation.

Copying a reported production playthrough into preprod is deferred. Future diagnostic copying must preserve source history and provenance while permitting independent preprod continuation. That future operation does not transfer ownership of or grant access to the original production playthrough. This release requires no export/import or bug-reporting workflow.

## Containers and infrastructure

The purchased server is `vps-0346462f.vps.ovh.net`, OVHcloud VPS-1 2027, in the UK, with 2 vCores, 4 GB RAM, 40 GB disk, and Ubuntu 26.04. This is the existing infrastructure target, not a request to purchase another server. The actual capacity of both environments must be measured with the packaged application.

Import the existing VPS into Terraform state and reconcile its actual configuration. Infrastructure plans must expose replacement, reinstallation, and deletion. The adoption must not reimage or replace the purchased machine. Keep state and credentials outside source control; protect state as potentially sensitive data and retain it outside the server it manages. Credentials are supplied through an approved secret mechanism, never checked-in files or command arguments printed in logs.

Use reproducible host configuration for SSH access, Docker, deployment directories, and network controls. SSH keys replace the initial password for routine deployment access. The initial password remains in the owner's password manager. Host bootstrap and application release deployment are separate operations; deploying a game version must not reprovision the VPS.

Use two Compose projects and a shared HTTPS reverse proxy. Each project contains its application and PostgreSQL database with its own durable volumes. Serve browser assets and API under the same origin per environment. Exact subdomain labels and Google application registration values are deployment inputs; choosing them does not change this design. Keep the existing main website outside this deployment's scope.

Build containers in CI. Pin deployed artifacts by digest, retain bounded image and log history, and expose disk/memory usage so the 40 GB host does not accumulate unbounded release data. Application replacement, reboot, and ordinary deploys retain database and authentication-key volumes. The existing provider backup is part of the recovery surface, not a substitute for migrations. Before a destructive data operation, identify its scope and a recovery point, or explicitly accept the intended loss. No availability SLA, multi-host failover, or continuous database replication is required.

## Migration and development-data policy

Database schema changes use the established EF migration chain. Immutable persisted event versions pass through registered upcasters and the canonical loader. Projection versions rebuild derived state from events according to the existing integrity doctrine. New ownership state must converge between the command and replay paths.

Alpha users accept incomplete gameplay, bugs, interruptions, and explicit loss of development playthroughs. Show that expectation in the player experience. Compatibility is not a promise that every old playthrough survives every release, but the migration strategy remains mandatory. Never treat dropping the database and rebuilding it as the normal upgrade procedure.

Discarding playthroughs is a separately scoped operation. It does not implicitly discard user identities, schema history, credentials, or infrastructure state. Normal deployment neither truncates game data nor removes volumes. Historical streams with genuinely unknown ownership cannot become playable through an invented migration default.

Release deployment runs the appropriate schema migrations in a controlled step before the application is declared ready. Migration failure stops deployment rather than masking failure with a reset. An earlier container version is not automatically compatible with a migrated database. Recovery must use a compatible artifact, a forward fix, or an explicitly selected restore point; automatic database downgrade is not required.

## Gitflow and release identity

The release foundation plans to adopt the AOM `gitflow` and `semver` standards. Row 18 owns the JIT implementation plan, subscription, repository-owned certification, and durable lifecycle routing; no standard pin or runbook change is made by this specification update. Marketplace PR #349 merged at `7a20ef191bb6be3b67fbbb9e86bd7b0050f96fc5` and was the assessed proposal. Row 18 will pin the accepted immutable definitions current when that plan is written.

Preserve the settled flow: `develop` is the feature-integration and default branch; ordinary feature branches start from current `develop` and merge by PR into `develop`, with squash merges permitted. Cut `release/*` from `develop` for stabilization, versioning, and migration validation. Promote a reviewed release to `main` with a merge commit and reconcile it to `develop` with a merge commit. Hotfixes start from the released `main` state, promote to `main`, then reconcile to `develop` and every affected active release line. Verify current remote refs, PR targets, merge methods, and hosting policy at publication; the written branch names do not prove remote enforcement.

The intended SemVer subject is the Wild Bunch browser-game product, not internal implementation details or independently versioned event/schema formats. The `0.y.z` line is pre-1.0 development and makes no public API or gameplay-compatibility promise; `0.1.0` identifies the qualified initial baseline, not a guarantee that every pre-release playthrough remains usable. The existing event/schema migration strategy remains mandatory for data the repository declares supported. This epic does not decide a `1.0.0` compatibility contract or require a stable 1.0 release; determine future compatibility expectations only when that release is genuinely in scope.

`Directory.Build.props` is the single authored application version. The web build derives its runtime `version.json` identity from evaluated MSBuild properties, and the repository command bus validates the generated identity while rejecting duplicate application-version fields in npm metadata. Each ordinary merge into `develop`, including documentation and tooling changes, receives one unique `0.1.0-dev.N` checkpoint per merged PR, never per commit. The planned release procedure uses `0.1.0-rc.N` for changed and requalified release candidates, beginning at `rc.1`; rerunning validation on identical source does not consume another candidate number. Promote the verified candidate to stable `0.1.0` only from the exact qualified source. A published version and its artifact identity are immutable. Event schemas and payload versions remain independent compatibility authorities.

Release and hotfix reconciliation follows the pinned joint cadence: if integration is left at the released baseline, keep the stable version until a later development merge diverges; if next-release development is already underway, preserve that core and advance its next unique checkpoint for the reconciliation PR. Reconcile stale or duplicate development checkpoint proposals against the latest `develop` before merge. Development checkpoints do not create tags or GitHub Releases; the GitHub Release remains the authorized public-deployment trigger described by this specification.

The first releases establish a reproducible, validated base and the actual release process. They do not require finishing gameplay or deploying publicly before the account and environment boundaries exist. Infrastructure and account work then use that process, and subsequent features continue through `develop`. This describes release intent, not an implementation task plan.

Preprod continuously deploys passing updates from the selected release branch. Ordinary feature PRs and arbitrary branches cannot overwrite it. Only one selected candidate owns this environment at a time; deployments are serialized so older runs cannot replace newer candidates after they finish. Hotfix candidates use the same preprod validation route.

Each candidate produces an immutable artifact set from one source revision: the API image and the public and preprod frontend variants. Public frontend output excludes dev code; preprod uses the dev-enabled variant. CI exercises the public variant and disabled-dev API configuration as well as the preprod variant, so testing the dev interface alone cannot qualify the public artifact. Build-time variant choices are recorded in release metadata.

Finalization must bind the `main` release tag to the source tree tested in preprod and its artifact digests. A merge resolution or other source change creates a new candidate and requires renewed preprod validation. Differences in merge commit IDs alone do not justify rebuilding equivalent tested source. Version metadata changes are source changes and belong in the candidate before validation.

Publishing the matching GitHub Release triggers automatic public deployment of that approved artifact set using public configuration. Creating a draft release does not deploy. No separate deploy-button ritual is required. The workflow rejects an untested artifact, an unrelated tag, or a tag outside the finalized `main` release history. Publishing may deploy a `0.y.z` release even while the game is described as pre-alpha or alpha.

Deployments serialize migrations and application replacement, preserve data volumes, and verify readiness plus a player-route smoke check. A failed readiness check is reported as a failed deployment, never as a successful release merely because containers started. Short interruptions are acceptable. The deployed version is observable for troubleshooting without exposing secrets or hidden game truth.

A hosted develop environment is deferred. When introduced, it will track `develop` for integration playtesting beyond the dev machine, with its own data and restricted access. Preprod remains the release-candidate environment.

## Acceptance and validation ownership

Acceptance focuses on observable boundaries and negative cases. Do not add tests that merely detect filenames, source strings, or the existence of this specification. Follow the repository's existing test-lane ownership.

- Domain/application behaviour: player creation and developer preparation emit ownership, full replay without snapshots restores it, owner transfer is impossible, setup counts as active, and restarting a user affects only that user's sessions. A failed or concurrent replacement preserves the at-most-one-active invariant without partial archival.
- HTTP/PostgreSQL integration: two independently authenticated users can create and continue their own games. Neither can read, mutate, archive, or obtain projections for the other's game, even with a valid ID. Expired/invalid authentication and forged mutations fail. Concurrent first sign-ins resolve one internal user. The authenticated current-game lookup works without browser-local IDs.
- Environment integration: public has no reachable dev query or mutation route; preprod admits only the configured owner identity. Public cookies cannot authenticate preprod. Database and credential separation is checked against the deployed configuration. Anonymous hosted dev access is denied.
- Browser behaviour: Google sign-in, Continue, confirmed Start over, partial-setup resume, sign-out/sign-in, account switching, and refresh of a stale tab follow server state. Lost-response recovery does not automatically resubmit gameplay actions. Public assets have no dev UI.
- Migration/replay integration: representative supported older event payloads pass through upcasters and reconstruct state; projection rebuild converges with command state. Missing or future unsupported event versions fail closed. Deployment upgrades schema without clearing game or user data. Explicit playthrough discard remains separate.
- Release/infrastructure behaviour: adopting the existing VPS produces no replacement plan; repeat host configuration does not reinstall it. Candidate CI precedes preprod CD. Release publication selects the tested digests and finalized source tree, while draft publication state and unqualified tags cannot deploy public. Migration/readiness failure prevents a success report. Container replacement retains saved games.
- Capacity/recovery: measure both environments on the purchased host, demonstrate bounded image/log retention, and identify the provider backup recovery path. No throughput or availability claim follows solely from the purchased RAM and CPU count.

Product work uses the established domain/application, API, real PostgreSQL integration, and web lanes. Repository and workflow changes use the canonical `py -3 tools/run.py ci --check` and normal staged-snapshot hook. Deployment evidence comes from the actual environment and GitHub workflow; local tests alone do not establish cloud playability.

## Applicable guidance and external references

- [Architecture guardrails](../doctrine/architecture-guardrails.md), [gameplay invariants](../doctrine/gameplay-invariants.md), and [event-sourcing integrity](../doctrine/event-sourcing-integrity.md).
- [Frontend standards](../doctrine/frontend-standards.md), [dev-overlay doctrine](../doctrine/dev-overlay.md), and [security playbook](../playbooks/security.md).
- [Coding discipline](../doctrine/coding-discipline.md), [validation policy](../doctrine/validation-policy.md), [testing playbook](../playbooks/testing.md), [artifact custody](../doctrine/artifact-custody.md), and [writing contract](../unslop/writing.md).
- [Google identity guidance](https://developers.google.com/identity/openid-connect/openid-connect), [ASP.NET external sign-in without Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/social-without-identity?view=aspnetcore-10.0), and [resource authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased?view=aspnetcore-10.0).
- [Gitflow](https://nvie.com/posts/a-successful-git-branching-model/), [SemVer](https://semver.org/), and [Docker Compose project separation](https://docs.docker.com/compose/how-tos/project-name/).
- [OVH Terraform VPS resource](https://github.com/ovh/terraform-provider-ovh/blob/master/docs/resources/vps.md). Pricing and stock are purchase-time inputs, not enduring architecture requirements.
