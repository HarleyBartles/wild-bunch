# Town Source Refresh Metadata Retirement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the inert town-source refresh-policy model while preserving one investigation opportunity per source per town visit and event-derived restoration of that behavior.

**Architecture:** `TownVisitTownState.VisitNumber` and each source state's `LastCheckedVisitNumber` remain the only state needed to enforce first-check/repeat behavior. Remove refresh policy and refresh timestamp metadata from Domain and the component snapshot; bump the current component version so old cache rows rebuild from the authoritative event stream. Do not change event facts or the player rule in ADR-0008.

**Tech Stack:** C#/.NET 10, xUnit, Entity Framework Core, PostgreSQL, repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 16 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; DN-14 in `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline. The Domain rule, serialized cache shape, version-triggered event rebuild, tests, and dated finding disposition form one coupled change. Inline execution preserves one view of the event and snapshot boundaries; the nearest alternative, task-level subagent execution, would add handoffs without creating independent reviewable slices. A fresh whole-branch review will independently assess the resulting PR.

## Global Constraints

- Preserve ADR-0008: a source yields new information once per town visit; returning to a town creates a new opportunity and retains that town's history.
- Keep `VisitNumber` and `LastCheckedVisitNumber` as the behavior state; do not replace them with UI state or source-catalog refresh calls.
- Do not change domain events, event ordering, source eligibility, clue outcomes, or player-facing behavior.
- Component cache JSON shape changes require a version bump; `ProjectionVersions` currently applies one shared version to all components, so advance it from 1 to 2 and rely on event replay to rebuild stale rows. No SQL migration or event upcaster is needed.
- Do not add tests asserting removed APIs, fields, or serialized property absence. Prove repeat/return behavior and that a stale cache contradicted by event history is rebuilt.
- Keep the `.110` version during this plan-only commit; the first substantive commit retires the `.110` predecessor plan and advances `Directory.Build.props` once to `0.1.0-dev.111`.
- Deliver through a reviewed PR to `develop`, wait for exact-head hosted CI, fast-forward the primary checkout after merge, remove the clean worktree before deleting its paired local/upstream branch refs, and verify all three are absent.

## Review Focus

- A repeated source check in one visit must still return `RepeatNoNewInfo`, while returning to that town permits one new check and preserves its history; `TownAggregateTests` and `TownVisitStateTests` cover the observable rule.
- A version-1 `townVisitState` cache that falsely says an event-recorded source is unspent must not override history; a PostgreSQL command load must reconstruct the source as spent and leave read-time storage unchanged.
- Starting a game, restoring a component snapshot, and entering a town must no longer depend on priming inert refresh metadata.

---

### Task 1: Commit the JIT plan before implementation

**Files:**
- Add: `.agents/plans/2026-10-12-row16-dn14-refresh-policy-retirement.md`

- [ ] Commit this plan by itself before editing source, tests, investigation, roadmap, or version files.

### Task 2: Retire refresh metadata and preserve event-backed visit behavior

**Files:**
- Modify: `src/WildBunch.Domain/World/TownSourceModels.cs`
- Modify: `src/WildBunch.Domain/Game/TownSourceVisitState.cs`
- Modify: `src/WildBunch.Domain/Game/TownVisitState.cs`
- Modify: `src/WildBunch.Domain/Game/TownAggregate.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`
- Modify: `src/WildBunch.Persistence/Versioning/ProjectionVersions.cs`
- Modify: `tests/WildBunch.Domain.Tests/TownVisitStateTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/TownAggregateTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-domain-test-followup.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-domain-helper-retirement.md`

**Interfaces:**
- Consumes: ADR-0008's per-town-visit repeat rule, event replay as authority for component restoration, and PR #295's merged DN-14 helper disposition.
- Produces: Source visit bookkeeping based only on visit/check numbers; a version-2 component cache without refresh-only metadata; behavior tests for repeat/return and stale-cache recovery; a dated DN-14 disposition closing only the refresh-policy finding; and row-16 routing to `0.1.0-dev.111`.

- [ ] **Step 1: Strengthen the behavior tests before removing metadata.** In `TownVisitStateTests`, remove `PrimeCurrentTown()` and assertions about refresh policy and refreshed visit number; keep assertions that the first check is new, the same-visit repeat returns no new information, and a return to the town allows a new check. In `TownAggregateTests`, remove `PrimeCurrentTown()` while retaining the same source and wanted-poster first/repeat/return outcomes.

- [ ] **Step 2: Add and run a failing PostgreSQL stale-cache behavior test.** In `EfGameSessionRepositoryTests`, create a normal session, record `GatherLocalGossip`, and persist it. Change the stored `townVisitState` JSON so the event-established local-gossip source has a `lastCheckedVisitNumber` that disagrees with the recorded event, while retaining component version 1. Load through the command repository and assert that `CurrentTownVisit.IsSpent(LocalGossip)` is true from replay. Also assert the read did not rewrite the contradictory component payload, version, event rows, or stream envelope. Run `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~EfGameSessionRepositoryTests` with shared PostgreSQL running; verify the focused new behavior fails because version 1 still trusts the contradictory cache.

- [ ] **Step 3: Remove the refresh-policy-only model.** Delete `TownSourceRefreshPolicy`, `TownSourceDefinition.RefreshPolicy`, `TownSourceVisitState.RefreshPolicy`, `LastRefreshedVisitNumber`, `IsFreshForVisit`, and `RefreshForVisit`. Simplify source-state constructors and lookup/check methods to use only town, source kind, and last checked visit. Remove `RefreshSources`, `TownVisitState.PrimeCurrentTown`, `TownAggregate.PrimeCurrentTown`, and their calls from `GameSession` construction and `Apply(GameStarted)`. Preserve `EnterTown` visit increment and saloon-person clearing.

- [ ] **Step 4: Update the component snapshot and version.** Remove refresh-policy and last-refreshed fields from `TownSourceVisitStateSnapshot`; serialize and restore only fields needed for check history. Set the shared `ProjectionVersions.ComponentVersion` to 2 and update its comment. Keep event payloads and SQL migrations unchanged; stale version-1 component caches must flow through the existing `PersistedPayloadLoader` event reconstruction.

- [ ] **Step 5: Run focused tests and inspect migration behavior.** Run `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~TownVisitStateTests|FullyQualifiedName~TownAggregateTests` and `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~EfGameSessionRepositoryTests`. Confirm the new stale-cache behavior passes, the existing town-visit persistence behavior still passes, and invalid event history still fails closed. Ensure PostgreSQL is available through `.	oolspostgres-dev.ps1 ensure` before the integration lane.

- [ ] **Step 6: Keep the investigation and test audit truthful.** Add a dated DN-14 entry stating that `RefreshForVisit` applied the same metadata assignment for every policy and default, all live definitions used the same policy, and the actual revisit rule remains `VisitNumber` plus `LastCheckedVisitNumber`. Mark only this finding closed and correct the `TownSourceVisitState.cs` and `TownSourceModels.cs` file dispositions. Update the Domain test follow-up so its prior conditional recommendation to test `OnTownReturn` is closed by removing that unsupported policy and retaining the proven first/repeat/return behavior tests. Update the Persistence test follow-up with the version-1 contradictory-cache test and version-2 event-rebuild result, including the read no-writeback boundary. Do not rewrite ADR-0008 because its player rule remains implemented; leave the feature matrix and baseline specification unchanged because no product promise changed.

- [ ] **Step 7: Retire the predecessor and advance the successor slice.** In the first substantive commit, delete the completed `.110` plan, update row 16 to point at this `.111` plan and record only completed evidence available at that point, and change `Directory.Build.props` exactly once from `0.1.0-dev.110` to `0.1.0-dev.111`. Keep this plan and roadmap links valid until the successor PR merges.

- [ ] **Step 8: Review, validate, and deliver.** Run `git diff --check`, focused tests, and the repository's staged-snapshot hook on the intended change. Inspect the complete diff for remaining source/test references to removed policy fields, ensure no refresh metadata remains in component serialization, and verify the stale-cache test fails if the component version is reverted to 1. Request one fresh whole-branch review, fix findings, and ensure hosted exact-head CI passes before merging to `develop`. After merge, fast-forward the primary checkout; remove the clean worktree first, then delete local/upstream branch refs and verify their absence.

**Validation:** The domain tests prove the gameplay rule at the command-owning Domain boundary. The PostgreSQL test proves that cache versioning hands authority back to ordered events when an old component cache contradicts an event-established fact, and that read recovery does not mutate storage. The canonical staged hook and exact-head hosted CI validate the complete delivery tree.
