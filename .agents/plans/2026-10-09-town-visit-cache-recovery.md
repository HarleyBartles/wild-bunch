# Recover the TownVisit Cache Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Rebuild missing or malformed `TownVisitState` cache data from the ordered event stream so resumed commands and player reads retain the player's actual town-visit state.

**Architecture:** `TownVisitState` is event-backed aggregate state cached as an optional component. A persisted `GameStarted` event establishes that a current town visit exists; when its component is absent or cannot be decoded, command and player-read loaders must rebuild the session through the existing full replay path. Setup-phase sessions without `GameStarted` retain their legitimate absent town state, and reads never write repaired caches.

**Tech Stack:** .NET 10, C#, EF Core, PostgreSQL, xUnit, existing event replay and persistence loaders.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially the cache/recovery contract and settled rule that event history is authoritative while current caches supply state.

**Execution Strategy:** `executing-plans` - command and read reconstruction share one event-backed boundary, and the PostgreSQL test must falsify both loaders against the same damaged stored state before the shared recovery behavior is changed.

## Global Constraints

- Preserve event history as the only authority for what happened; cache data is rebuildable state.
- Keep CQRS strict: command loading may reconstruct aggregate state, player reads return safe read models, and neither read path writes back.
- Preserve setup-phase absence when no `GameStarted` event establishes a town visit.
- Do not add domain events, change event payloads or versions, add a database migration, or invent visit/source facts.
- Repair a damaged component only through an ordinary later legal aggregate save and unit of work.
- Advance `Directory.Build.props` once for this PR from `0.1.0-dev.24` to `0.1.0-dev.25`; it remains the only authored application version.
- Use the canonical fail-fast `py -3 tools/run.py ci --check` gate and the repository's `develop` PR delivery contract.

## Review Focus

- A missing row after real town-visit events must not become an unusable command aggregate or a fresh empty player view; the recovery test independently asserts a consumed source and the event-recorded saloon person.
- Setup-phase event history without `GameStarted` must not be forced to invent a town visit; test or preserve the existing setup-load behavior when routing missing optional components.
- A present JSON `null` cache is syntactically valid JSON but invalid component state; both loaders must recover it while preserving the payload and component version during reads.
- Corrupt authoritative event history must still fail through replay; an invalid optional cache must not hide event decode/reconstruction errors.
- A later legal save must persist recovered state through the normal unit of work, and a fresh load must retain the independently captured visit facts.

---

### Task 1: Save the JIT plan before implementation

**Files:** `.agents/plans/2026-10-09-town-visit-cache-recovery.md`.

**Consumes:** Refreshed `origin/develop` at PR #209 merge `91596e35c44c7bbc0debeaa06abb94cfd4b6dde7`.

**Produces:** A committed plan for the missing and malformed `TownVisitState` cache gap, before changing predecessor artifacts, version identity, tests, or implementation.

- [ ] Confirm the exact event-established current-town boundary from `GameSession.Apply(GameStarted)`, `GameSession.RehydrateFromEvents`, command loading, read loading and current PostgreSQL fixtures.
- [ ] Commit only this plan as the first branch commit; record its SHA in the plan after commit.

Plan-only commit: `498bedc8847cfee503b6cdf076bfadaba9c96b28`.

### Task 2: Retire the completed Journey predecessor and advance row 07

**Files:** `.agents/plans/2026-10-09-missing-active-journey-cache-recovery.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `docs/features.md`; `Directory.Build.props`.

**Consumes:** PR #209's merged source, merge, matching tree and hosted canonical gate, verified against refreshed `develop`.

**Produces:** A truthful PR #209 delivery record, this plan as the current row 07 plan, the explicit optional-component recovery scope, and `0.1.0-dev.25`.

- [ ] Verify PR #209 merged to `develop` from source `abe034db6a924b80d5d459de47ea4375b0509f42`, merge `91596e35c44c7bbc0debeaa06abb94cfd4b6dde7`, matching tree `dcd74a324d7f316a2606dcdaca1bbbd6dd0f536a`, and hosted canonical gate run `37885001280` passed on that exact source.
- [ ] In the first substantive commit, record those exact facts and the missing active-Journey result in row 07; retire `.agents/plans/2026-10-09-missing-active-journey-cache-recovery.md` and remove its stale link.
- [ ] Set this plan as the current row 07 plan and leave row 07 executing because additional nested and optional component recovery remains open.
- [ ] Update PLAT-001 and the persistence test follow-up to say missing active Journey recovery is closed while TownVisit recovery remains open; do not claim broader optional-component recovery.
- [ ] Confirm ADR-0028 remains truthful; the implementation follows its current rebuildable-cache decision and does not require an ADR change.
- [ ] Advance `Directory.Build.props` once from `0.1.0-dev.24` to `0.1.0-dev.25`; do not edit generated web identity output.
- [ ] Inspect and commit the intended successor-artifact diff before adding or changing tests and implementation.

### Task 3: Witness TownVisit cache loss at PostgreSQL boundaries

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs` and only directly required fixtures.

**Consumes:** A normally created `GameSession`, real `LookAroundSaloon()` events, the production component serializer, command repository, player read repository and PostgreSQL fixture.

**Produces:** Falsifiable missing-row and malformed-cache cases proving both load paths restore event-established town visit state without read-time writeback.

- [ ] Add a PostgreSQL theory for `TownVisitState` cache damage cases `missing-row` and `null-root` using a real started session after `LookAroundSaloon()` has emitted `SaloonPersonOfInterestSpotted`.
- [ ] Before damaging the cache, independently capture the current town id, visit number, saloon-source-spent state, active person id/descriptor/kind from the aggregate, and the corresponding facts from the typed event stream.
- [ ] In the `missing-row` case delete only the `townVisitState` component; in the `null-root` case replace only its payload with JSON `null` while retaining its component version.
- [ ] Run fresh player-read and command repository loads and assert the current town, visit number, spent saloon source and active person facts match the event-established values; do not compare only two loader results.
- [ ] During both reads assert the cache remains absent or byte-for-byte unchanged and its version remains unchanged when present; also preserve envelope versions, ordered stored events and travel-diary rows.
- [ ] Add a negative case where the TownVisit cache is damaged and required event history cannot reconstruct the session; assert the replay failure escapes rather than becoming empty/default town state.
- [ ] Run the new cases before the production fix and observe the current missing-row loss or malformed-cache exception for the intended assertions.

### Task 4: Rebuild only event-established TownVisit state

**Files:** `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`; focused integration tests.

**Consumes:** The PostgreSQL red tests, `GameSession.RehydrateFromEvents`, `SessionRebuilder`, and the existing typed invalid-component cache fallback.

**Produces:** Equivalent event-established TownVisit state for command and player-read paths, while preserving valid setup-phase absence, read-only recovery and fail-closed history.

- [ ] Treat an absent `townVisitState` component as a replay trigger only when ordered events include the `GameStarted` fact that establishes a current town; do not reinterpret setup-only sessions as having a current town.
- [ ] Wrap undecodable `TownVisitState` cache shapes in the existing typed invalid-component-cache exception so both loaders use their existing full-replay fallback.
- [ ] Preserve valid `TownVisitState` fast-path behavior, and leave all event decoding/upcasting/reconstruction exceptions uncaught by the cache-shape fallback.
- [ ] Run the new PostgreSQL damage cases and existing setup-phase persistence tests; prove both loaders recover the captured facts and neither writes back during reads.
- [ ] Extend the case through one legal follow-up command and normal unit-of-work save; assert the component returns at the current version and a fresh load preserves the same event-established facts.

### Task 5: Record the bounded result and deliver the slice

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `docs/features.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; this plan; changed source and tests.

**Consumes:** Focused PostgreSQL proof, the baseline specification, ADR-0028, event-sourcing doctrine, feature-matrix and decision-record playbooks, and applicable backend/review unslop profiles.

**Produces:** A dated, narrow TownVisit recovery disposition, updated PLAT-001 assessment, reviewed PR, and evidence for the committed development version.

- [ ] Record the exact `TownVisitState` missing/null-root recovery behavior, event-established facts, valid setup-phase absence, read no-writeback, legal-save repair, and unrecoverable-history negative in PS-04/05; retain the original audit findings.
- [ ] Update PLAT-001 only with the behavior proven in this plan; leave other optional components, other malformed shapes and remaining projection recovery open.
- [ ] Run focused PostgreSQL tests, migration inventory and `py -3 tools/run.py ci --check`; confirm generated web identity reports `0.1.0-dev.25` and there is no event or migration diff.
- [ ] Complete whole-branch review against this plan, baseline specification, PS-04/05, ADR-0028, event-sourcing doctrine, unslop and code-review runbook; resolve every actionable finding and inspect the final committed head.
- [ ] Publish and attach a Draft PR to `develop`, verify its exact source head and body, mark it ready after review and local validation pass, and verify the hosted canonical gate on that exact SHA before merging under the active epic authorization.
- [ ] Verify merge to `develop`, fast-forward `Z:\wild-bunch`, and clean only the verified merged worktree and branch; retain this plan through its completing PR so successor-slice retirement occurs in the next row 07 plan.

The PostgreSQL lifecycle helper is `tools/postgres-dev.ps1 ensure`. The command-bus target for focused integration tests is `py -3 tools/run.py dotnet-test --check --verbose -- --filter 'FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.ReadModel_DamagedTownVisitCacheRecoversWithoutWritingBack'`; the bus configures the shared `localhost:5435` database. The canonical check-only gate is `py -3 tools/run.py ci --check`. Before publication, compare the actual diff with ADR-0028 and PLAT-001 through their playbooks and make the author and reviewer checks explicit.

Before implementation, read `.agents/runbooks/implementing.md` and `.agents/playbooks/testing.md`. For final validation, stage the intended candidate and let the check-only pre-commit hook run the canonical fail-fast gate; do not run the same full gate immediately before that commit. Run explicit `py -3 tools/run.py ci --check` when checking an uncommitted tree or when a later change means the hook result no longer represents the candidate being published.
