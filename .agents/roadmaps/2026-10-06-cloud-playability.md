# Cloud playability roadmap

**Authority:** [Approved specification](../specs/2026-10-06-cloud-playability-and-releases.md). Planning is authorized; implementation and server changes require their own handoff. This roadmap sequences delivery, not purchases.

**Outcome:** Public Google sign-in, private persistent playthroughs, developer controls confined to owner-only preprod, and versioned releases deployed to the purchased OVH VPS.

## Delivery sequence

| # | Title | Status | Plan File | Commit | PR | Rating | Notes |
|---|---|---|---|---|---|---|---|
| 1 | Stable 0.1.0 investigation and baseline | writing | [Interactive investigation](../investigations/stable-0.1.0/2026-10-06-stable-0.1.0-investigation.md) | See Git history | None | Conversation only | Read the user's selected files in full, discuss findings and agree the baseline; retain the already-authorized unslop and scoped-router work. The earlier blocked release-foundation draft was superseded by the accepted baseline specification and cleanup roadmap; its implementation checklist was not adopted. |
| 2 | Authenticated owned playthroughs | pending | Not authored | None | None | Conversation only | Google identity, event-backed ownership, atomic per-user active-session rule, authorization and browser resume. |
| 3 | Public and preprod capability boundary | pending | Not authored | None | None | Conversation only | Public artifacts exclude dev UI and APIs; hosted preprod admits only Harley's stable Google identity. |
| 4 | Reproducible container runtime | pending | Not authored | None | None | Conversation only | Same-origin web/API, separate Compose projects and PostgreSQL volumes, controlled migrations, durable authentication keys. |
| 5 | Adopt and bootstrap the existing VPS | pending | Not authored | None | None | Conversation only | Terraform import without replacement, SSH keys, reproducible host configuration, DNS/TLS, capacity and recovery verification. |
| 6 | Candidate promotion and public release deployment | pending | Not authored | None | None | Conversation only | Selected release-branch CD to preprod; GitHub Release publication promotes the tested digests and matching main source tree. |

Write only the next implementation plan after inspecting the repository then. Pending rows define bounded outcomes and dependencies, not permission to improvise implementation from this table.

## Slice boundaries and exits

The first row is governed by the approved [stable 0.1.0 baseline specification](../specs/2026-10-07-stable-0.1.0-baseline.md) and its [cleanup roadmap](2026-10-07-stable-0.1.0-cleanup.md), supported by the [separate investigation records](../investigations/stable-0.1.0/README.md). Brainstorm and accept its implementation plans iteratively. The release-foundation draft alone cannot deliver that row or bypass cleanup.

### 1. Stable 0.1.0 investigation and release foundation

Investigate interactively before defining remediation or executing the versioning draft. The user supplies the files to read in full; record findings and agreed baseline decisions in the [investigation tracker](../investigations/stable-0.1.0/2026-10-06-stable-0.1.0-investigation.md). Classify maintainability and harness issues separately from slop when appropriate. Recording a finding does not authorize its correction. Keep the completed, explicitly retained unslop adoption and scoped-router changes visible in that tracker.

After baseline agreement and approved remediation, reconcile the provisional release-foundation plan: establish `0.1.0` as the first proposed baseline version, a single authored version source and derived API/web build identity. Preserve the canonical tracked-hook validation lane and run it on `develop`, `release/*`, `hotfix/*` and `main`. Document feature, release, hotfix and merge-back routing, with immutable tags. Produce a reviewable change that can become the first development release before account or hosting work. Creating remote branches, changing repository rules and publishing the first release are explicit subsequent operational actions, not proof supplied by a local document.

### 2. Authenticated owned playthroughs

Deliver one complete local player journey: Google sign-in resolves `UserId`, creates or resumes the user's active session, and sign-out leaves committed state intact. Ownership begins in events for setup and dev-prepped creation; full replay reconstructs it. Coordinate restart and creation transactionally across session aggregates, with database enforcement of at most one active session per user. First sign-in races resolve one identity. Every player read and write, including projections, authorizes ownership. Account switching clears cached game data; stale actions remain server-validated and uncertain responses cause state reload. Validate two-user isolation, concurrent starts, rollback and older-event migration against real PostgreSQL. Existing local unowned data has an explicit transition path; never synthesize an owner in historical events. No public hosting exit is claimed here.

### 3. Public and preprod capability boundary

Use slice 2's identity boundary to restrict hosted preprod to the configured stable Google subject. Replace `IsDevelopment()` as hosted authorization. Build public and preprod frontend variants from one source revision; public excludes dev code and registers no dev endpoints. Exercise both API configurations, non-owner preprod admission, anonymous dev requests and direct public dev URLs. Local development remains explicitly local. Preserve game-event mutation and ownership rules in developer preparation. Exclude a general role system and production-to-preprod stream copying.

### 4. Reproducible container runtime

Package slice 3's API and frontend variants for same-origin hosting. Use PostgreSQL containers with durable volumes and two isolated Compose projects, joined only as required to a shared HTTPS proxy. Keep database ports private; separate credentials, auth keys, host cookies and networks. Move schema migration into a serialized deploy step while preserving local development usability, registered upcasters and projection rebuild behavior. Replacement and restart preserve identities and playthroughs. Demonstrate upgrade without a reset and failure without a false readiness result on a local production-style runtime. No Kubernetes or managed database purchase.

### 5. Adopt and bootstrap the existing VPS

Import `vps-0346462f.vps.ovh.net` into protected Terraform state outside the managed machine. Reconcile actual provider settings without reinstalling or replacing it. Configure routine SSH key access, Docker, deployment directories, bounded logging/image retention and network controls reproducibly. Exact public/preprod DNS names, Google client values, owner subject, SSH key and Terraform credential/state destination are execution inputs supplied through suitable secret/configuration paths. Establish DNS and HTTPS for both origins without modifying the portfolio site. Measure both environment stacks on 2 vCores, 4 GB RAM and 40 GB disk; adjust measured limits before declaring capacity sufficient. Identify and exercise an appropriate backup/recovery path without destroying the purchased server or live data. Hosting mutation is separately authorized after the code/configuration is reviewable.

### 6. Candidate promotion and public release deployment

Build one immutable API/public-web/preprod-web artifact set per tested source revision, with digest and variant metadata. Only the selected release or hotfix branch can deploy preprod; serialize deployments and reject superseded runs. Require a preprod playtest of the public configuration as well as the dev-enabled view. Finalized main tag must have the tested source tree, including version changes; a differing merge resolution needs a new candidate. Publishing its GitHub Release deploys the approved public digests; drafts, untested candidates and unrelated tags cannot deploy. Verify migrations, readiness and a player-route smoke check, retain data on ordinary replacement, and expose deployed version. Compatible rollback, forward repair or explicitly chosen restore is the recovery route. Complete a real candidate-to-public rehearsal before claiming cloud playability.

## Release progression

Slice 1 makes a development baseline release possible. Slices 2-5 can ship successive `0.y.z` foundation releases through that workflow, without making the game public prematurely. Slice 6 enables public promotion only after account and capability isolation exist. The incomplete game remains pre-alpha or alpha regardless of its numeric release version. A published version is never overwritten.

## Coverage and deferred work

The specification's user model, persistence and resume requirements belong to slice 2; hosted developer isolation belongs to slice 3; environment data separation and controlled migrations belong to slice 4; infrastructure adoption, capacity and recovery belong to slice 5; candidate immutability and promotion belong to slice 6. Release identity and contributor routing begin in slice 1 and remain inputs to every later slice.

Deferred: simultaneous selectable playthroughs, profile/account administration, additional sign-in providers, SignalR, automatic command replay, diagnostic stream copy, hosted develop environment, multi-host availability, Kubernetes and gameplay completion. Normal EF migrations, event upcasters and projection rebuilds remain mandatory despite disposable alpha playthroughs.

## Handoff notes

The existing VPS is already purchased. PostgreSQL is self-hosted in the proposed Compose projects; OVH Web Cloud Databases and additional disk are optional future responses to measured needs, not prerequisites. Planning has made no changes to the VPS, provider billing, GitHub rules, remote branches or DNS.
