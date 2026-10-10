# Persist startup operation identity

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make initial setup and free first arrival retry-safe without allowing setup to silently replace an active playthrough.

**Architecture:** Persist a narrowly scoped startup-operation record containing operation ID, operation kind, canonical request fingerprint and resulting session ID. Commit it atomically with the session event append through the existing unit of work. The operation record answers whether a transport retry is the same request; the event stream remains the sole game-state authority. Do not introduce a generic command receipt framework, ownership/account model, or cache of serialized command responses. Exact retries load the referenced session from event-derived state and return its current representation without repeating effects.

**Tech Stack:** ASP.NET Core, C# application command handlers, EF Core/PostgreSQL, event-sourced `GameSession`, React, TanStack Query, xUnit, Vitest and repository command bus.

**Spec:** [Stable 0.1.0 baseline, PG-001/PG-002](../specs/2026-10-07-stable-0.1.0-baseline.md#product-boundary), [playthrough lifecycle contract](../investigations/stable-0.1.0/2026-10-07-playthrough-lifecycle-contract.md), [ADR-0034](../../docs/decisions/ADR-0034-playthrough-archive-lifecycle-and-one-active-playthrough-invariant.md), and [ADR-0039](../../docs/decisions/ADR-0039-seeded-game-setup-and-free-first-arrival.md).

**Execution Strategy:** Execute inline under the active Stable 0.1.0 goal. Keep the persisted receipt limited to setup and first arrival. Build the server contract and persistence proof before wiring browser retry state; the web must send the exact saved operation identity and payload after an uncertain response.

## Global Constraints

- `Directory.Build.props` remains the only authored application version source; this PR uses `0.1.0-dev.68`.
- Setup creates a session only when no active playthrough exists. It never archives or otherwise changes an active session. Confirmed Start Over archives first as its own event-backed operation; replacement setup follows separately.
- A setup retry with the same operation ID and same canonical request returns the same session. Reusing that ID with a different request conflicts. A different operation ID while an active session exists conflicts without mutating it.
- First arrival remains one atomic command that selects the town and completes the free arrival. A same-ID, same-request retry returns the original session state without appending events. Reusing the ID with a different town conflicts; a new ID after arrival conflicts even when the town is unchanged.
- A persisted operation record contains no game facts and does not replace event replay. Do not store a serialized response that becomes stale after later gameplay.
- Only the two startup operations in this plan use this mechanism. Do not build generic idempotency middleware or a reusable receipt platform.
- Preserve exact event semantics: setup settles world and case truth before prologue; first arrival causes no journey time or travel event; replay never rerolls.
- A new forward migration is required. Preserve all existing migrations and use the approved disposable-playtest-data policy; do not drop the database or rewrite migration history.
- Browser storage may retain one pending setup operation ID and its exact submitted payload until its outcome is known. It remains a retry aid, never game authority. Do not add accounts, ownership, multiple playthrough selection, cross-tab sync, or SignalR.
- Follow strict CQRS, aggregate and unit-of-work boundaries, fail-fast command bus gates, and the required test falsification procedure. Tests must assert observable outcomes and prove they fail for the intended reason.
- Each plan is based on refreshed `develop`, advances the sole authored version once, commits the plan before implementation, delivers by PR to `develop`, passes hosted CI on the exact PR head, and is retired by the next successor after merge evidence is recorded.

## Review Focus

- Replaying setup with the exact key and payload returns the original session even after the browser lost the original response; it does not create another session or append events.
- An active session remains active and byte-for-byte event-history equivalent when a distinct setup operation is rejected.
- A confirmed archive remains durable even if the user never creates a replacement; only after that archive may a new setup operation succeed.
- Replaying first arrival does not append duplicate `StartingTownSelected` or `GameStarted` events or reapply initial resources. The original starting town is preserved even if the session later travels elsewhere.
- Same operation ID with different canonical payload is a conflict; a new operation ID after a phase transition is also a conflict.
- Two concurrent deliveries of the same operation ID produce one committed session/arrival and both callers can resolve the same result after the unique-key race.
- If setup's response is lost across refresh, the browser resends the same ID and exact form values, including the seed. A fresh visit without a pending operation still receives a fresh editable seed.
- Missing/malformed operation identity fails before mutation. Unknown transport outcomes retain pending operation state; definitive validation/conflict responses do not masquerade as successful creation.

---

### Task 1: Commit the JIT plan and retire the completed predecessor

**Files:**
- Create: `.agents/plans/2026-10-10-startup-operation-identity.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `docs/decisions/ADR-0034-playthrough-archive-lifecycle-and-one-active-playthrough-invariant.md`
- Modify: `docs/decisions/ADR-0039-seeded-game-setup-and-free-first-arrival.md`
- Modify: `Directory.Build.props`
- Retire: `.agents/plans/2026-10-10-startup-recovery.md`

**Interfaces:**
- Consumes: develop merge `ec75980a877acff0047852a4883f3532d419b0df`, merged PR #253 and the accepted persisted-operation-ID decision.
- Produces: a committed `.68` plan, truthful startup/archival contracts, row 09 delivery evidence and retirement of the completed `.67` plan.

- [ ] Record the exact PR #253 source, merge and exact-head PR/develop hosted-gate results in row 09; preserve the unknown PR #252 develop-push outcome.
- [ ] Update the baseline contract with persisted operation IDs/fingerprints for setup and first arrival, exact-retry behavior, changed-payload conflict, active-setup rejection and no generic receipt platform.
- [ ] Add dated ADR corrections without erasing the historical implementation fact in ADR-0034; add the settled retry identity to ADR-0039.
- [ ] Advance the sole authored version to `0.1.0-dev.68`, point row 09 at this plan, retire the completed `.67` predecessor and commit all planning changes before implementation.
- [ ] Run the normal check-only commit hook and ensure it validates the staged candidate without mutation.

### Task 2: Persist the two startup operation records atomically

**Files:**
- Create: Application startup-operation model/repository abstraction and Persistence EF entity/configuration.
- Modify: `src/WildBunch.Persistence/WildBunchDbContext.cs`, `EfGameSessionUnitOfWork.cs`, service registration and model snapshot.
- Create: a forward EF migration for the startup-operation table and unique operation-ID constraint.
- Modify: setup and game-start command records/handlers and API endpoints in `src/WildBunch.Api/Games/GameSessionEndpoints.cs`.
- Replace: `tests/WildBunch.Application.Tests/Handlers/CompletePlayerSetupOneActivePlaythroughTests.cs` behavior that protects setup auto-archival.
- Add: falsifiable application and PostgreSQL integration behavior tests under the owning test projects.

**Interfaces:**
- Consumes: setup and game-start commands with operation identity, the existing event-sourced session repository and shared unit of work.
- Produces: persisted operation lookup/add behavior, API conflict responses, and atomic event-plus-operation commits for setup/arrival.

- [ ] Add failing behavior tests proving exact setup retry returns the same session with no duplicate events; changed payload under that key conflicts; and a distinct setup operation while active preserves the existing event stream and status.
- [ ] Add failing behavior tests proving only an already committed `PlaythroughArchived` permits a new setup operation.
- [ ] Add failing behavior tests proving exact arrival retry appends no events, changed town under the same operation ID conflicts, and a new operation after arrival conflicts even for the same town.
- [ ] Add a failing PostgreSQL persistence test proving the operation record and event stream commit or roll back together and survive fresh context/load reconstruction.
- [ ] Add a failing concurrent duplicate-operation test proving unique-key contention resolves to one operation and one event append; reload after the losing transaction using a clean tracker.
- [ ] Implement a scoped `StartupOperation` record with ID, kind, canonical SHA-256 request fingerprint and session ID. Define canonicalization explicitly and include every semantic command input; exclude transport-only metadata.
- [ ] Ensure the existing unit-of-work transaction commits receipt and event append atomically. Handle only the named operation-ID unique violation as an operation race, clear tracked state after rollback, and resolve the existing receipt; do not swallow unrelated database failures.
- [ ] Require and validate an operation ID at the HTTP boundary. Missing/malformed IDs return 400; changed-payload reuse and illegal new operations return 409 without event mutation.
- [ ] Make setup reject an active session instead of auto-archiving it. Preserve the existing archive event and start-over command as the only archive path.
- [ ] Make first arrival reject a new operation after the phase transition. Resolve exact retries by operation receipt and return event-derived current session state, preserving the originally selected town in event history.
- [ ] Run focused application and PostgreSQL integration tests, proving each new negative test fails before implementation and passes afterward.

### Task 3: Make the browser retry the same pending setup and arrival

**Files:**
- Modify: `src/WildBunch.Web/src/api/wildBunchApi.ts` and the relevant request types.
- Modify: `src/WildBunch.Web/src/flow/PreSessionSurface.tsx`, `useStartFlow.ts` and startup mutation ownership.
- Modify: `src/WildBunch.Web/src/state/GameSessionProvider.tsx` or the existing session-storage owner only where needed to restore pending setup intent.
- Modify: the first-town selection slice and relevant startup tests.

**Interfaces:**
- Consumes: server-required operation IDs and conflict/error contracts.
- Produces: an operation ID per intentional setup/arrival action; a persisted pending setup containing that ID and exact form values; retry/resolution that cannot silently create another setup.

- [ ] Add a failing browser behavior test proving a lost setup response followed by refresh resends the same operation ID and exact form payload, then adopts the resulting server session and clears pending state.
- [ ] Add a failing browser behavior test proving a new setup visit with no pending submission generates a fresh seed and a new operation ID.
- [ ] Add a failing browser behavior test proving an uncertain setup result retains the pending operation and blocks a second intentional setup until resolution; definitive validation errors allow correction with a new operation.
- [ ] Add a failing browser behavior test proving first-arrival retry uses the same ID/town after uncertainty and uses event-derived session state after success.
- [ ] Implement the narrow pending-operation storage and HTTP transport. Keep all game facts in server responses and never infer success from error text.
- [ ] Preserve the one-map semantic flow and existing saved-session bootstrap/reconciliation from PR #253.
- [ ] Run focused web tests and the web command-bus target.

### Task 4: Reconcile evidence and deliver the slice

**Files:**
- Modify: `docs/features.md` PG-001/PG-002 evidence only to the degree demonstrated by tests.
- Review: implementation against ADR-0028, ADR-0034, ADR-0039, the lifecycle contract, feature matrix and scoped unslop profiles.
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` in the next successor after merge.

**Interfaces:**
- Consumes: committed source, focused and full validation, independent whole-branch review, exact PR head and hosted gate.
- Produces: reviewed `.68` PR to `develop`, with operation retry and lifecycle evidence and no broadened account or infrastructure scope.

- [ ] Ensure the local PostgreSQL cluster with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`; run focused persistence/API/web tests, then the canonical `py -3 tools/run.py ci --check` gate.
- [ ] Inspect generated migration and fresh-database behavior; do not claim proof from a stale local schema. Preserve the migration chain.
- [ ] Review the full diff for strict CQRS, event-only game truth, transaction atomicity, unsafe retries, stale predecessor references, and change-detector tests. Correct issues and rerun relevant tests.
- [ ] Run an independent whole-branch review, address findings, and commit with the check-only hook.
- [ ] Publish the PR to `develop`, verify the exact source SHA and clean mergeability, make it ready for hosted CI, require the canonical hosted gate to pass on that exact SHA, then merge.
- [ ] In the next fresh successor worktree, record merge and develop-push hosted-gate evidence and retire this completed plan in the successor's first substantive commit.

## Acceptance

- Setup and first arrival have persisted operation identity and exact request matching; a lost response can be retried without duplicate game effects.
- Setup never archives an active session. The only archive-producing path remains confirmed Start Over, whose archive survives without replacement creation.
- Operation receipts remain retry metadata; event history remains sufficient to reconstruct game state and the selected initial town.
- PostgreSQL proves atomic persistence, fresh-load behavior and duplicate-operation race resolution.
- Browser tests prove the pending setup payload survives refresh and exact retries while a new visit receives a fresh seed.
- Hosted canonical CI passes on the exact PR source head before merge; develop's merge push gate is observed and recorded by the successor.

## Explicit Exclusions

Do not add Google/OIDC accounts, per-user ownership, multi-playthrough selection, generic idempotency middleware, response-body caching, SignalR/cross-tab synchronization, automatic cleanup/retention, distributed locks, deployment changes or new gameplay. Do not change event payloads unless a test demonstrates the settled startup facts cannot be reconstructed; return any such durable design gap to brainstorming before changing event truth.
