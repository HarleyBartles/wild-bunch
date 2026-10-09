# Explicit Persistence Rehydration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace reflection-based session and component restoration with explicit restoration operations while preserving resumable gameplay behavior.

**Architecture:** Persistence remains responsible for decoding stored DTOs and deciding whether to load a snapshot or replay events. Domain exposes narrow internal restoration operations for state it owns, and Persistence uses those operations without reflecting over private constructors or fields. The event stream remains authoritative, and this slice changes no event or schema contract.

**Tech Stack:** .NET 10, C#, xUnit, EF Core, PostgreSQL, Wild Bunch persistence loader.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 07; `.agents/playbooks/decision-records.md`.

**Execution Strategy:** `executing-plans` — one cohesive snapshot restoration boundary spans Domain, Persistence and PostgreSQL behavior proof; the same-session continuation and next-command checks must exercise one integrated restore path, so separate task handoffs would add context and review overhead without producing independently useful software.

## Global Constraints

- Immutable typed event history remains the source of truth; snapshots and components remain rebuildable caches.
- Normal loads use a valid current cache; invalid cache recovery replays supported ordered events once for the selected coherent stream position.
- Queries remain read-only, and only an ordinary command save may repair a cache in this slice.
- Keep event types, payloads, schema versions, upcasters, migrations, and ADR decisions unchanged.
- Keep Persistence DTOs and EF storage types out of Domain.
- Advance the sole authored application version in `Directory.Build.props` from `0.1.0-dev.42` to `0.1.0-dev.43` once for this implementation PR.
- The PR targets `develop`; the exact final PR head must pass the hosted `Canonical tracked commit gate`, and the resulting `develop` merge commit must pass its push-triggered gate before the next slice.

## Review Focus

- Resuming in the same town action context must not advance the clock or emit another context event; a later legal command must append at the persisted stream tail.
- Snapshot-derived seed, start-flow phase, action-context town, diary and pending supported state must remain available after deserialization, while post-snapshot event replay still overrides snapshot values in order.
- Malformed required snapshot fields and invalid authoritative history must continue to fail or recover through the existing typed cache and replay paths, rather than escaping through incidental reflection failures.

---

### Task 1: Retire the completed confrontation plan and record PR #227

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, `docs/features.md` PLAT-001, and `Directory.Build.props`; delete `.agents/plans/2026-10-09-casefile-confrontation-cache-recovery.md`; create this plan.

**Interfaces:** Consume PR #227 source `bbc410410b20944031ca76c403eb88bec866e47e`, merge `6b868ac6825b6a43a5f2f7d10dc712c37038840c`, PR gate run `37973791460`, push gate run `37974244981`, and version `0.1.0-dev.42`. Record confrontation cache recovery as delivered and select explicit persistence rehydration as the next row 07 slice.

- [x] Verify PR #227 is merged to `develop`, its source is `bbc410410b20944031ca76c403eb88bec866e47e`, its merge is `6b868ac6825b6a43a5f2f7d10dc712c37038840c`, and both hosted runs succeeded on their respective exact source and merge commits.
- [x] Compare the completed confrontation plan with its merged implementation, PostgreSQL behavior proof, review, hosted gate and roadmap scope; classify its complete scope as shipped before retiring it.
- [x] Record PR #227's source, merge, both hosted runs, review outcome and `.42` version in row 07 and the persistence follow-up. Record covered confrontation fields, event-time action clock, turn-in consequence, read no-writeback, ordinary-save repair and compatibility boundaries.
- [x] Update PLAT-001 with the confrontation evidence and state that later mutable CaseFile progress, other restoration seams, event compatibility and migration questions remain open. Do not claim row 07 complete.
- [x] Point row 07 at this plan, retain its executing status, retire only the completed confrontation plan and stale links, and bump `Directory.Build.props` from `.42` to `.43`.
- [x] Stage only the intended successor files, inspect the full staged diff and `git diff --cached --check`, then commit as `docs: plan explicit persistence rehydration`; let the normal check-only hook validate the staged candidate.

**Expected:** PR #227 and its actual gate evidence are recorded, its completed plan is retired in successor history, and row 07 has a bounded `.43` plan for explicit restoration behavior.

### Task 2: Restore snapshots through explicit Domain operations

**Files:** Modify `src/WildBunch.Domain/Game/GameSession.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Rehydration.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Travel.cs`, `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs`, and `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; delete `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs`.

**Interfaces:** Replace reflection-based construction and backing-field writes with narrow internal Domain constructors, factories or restoration methods that accept Domain values. Keep JSON-to-Domain mapping in the serializer and stream-position selection in the repository. Remove `GameSessionRehydrator` if its remaining useful operations can live at their owning Domain or serializer boundary; do not retain a generic field-name setter. Preserve snapshot version initialization before post-snapshot events are applied, and preserve the rule that those events override snapshot-derived values.

- [x] Extend PostgreSQL-backed repository tests before changing production code. In `SnapshotLoad_PreservesCurrentActionContext`, persist a session after entering Saloon, reload it from a current snapshot, call `EnterActionContext(Saloon)`, and assert it returns false, emits no event, and leaves day and turn unchanged. Then make one legal purchase, commit it, fresh-load, and assert the purchase is present and the stream contains exactly one new purchase fact after the previous tail. In `SaveAndLoadTravelDiaryRoundTripsStructuredDiaryState`, capture generated `Terrain`, `RouteWaterSecure` and `CanteenChargesPerDay` from the Domain diary day before saving and assert the reloaded diary day preserves all three independently observable values.
- [x] Run both focused behavior tests before implementation and capture the successful baseline. Then temporarily omit only action-context restoration in the actual repository snapshot path and confirm the same-context assertion fails with the lost Saloon context. Temporarily omit one travel-diary snapshot field and confirm its persisted-value assertion fails. Restore each production path before continuing.
- [x] Replace private-constructor reflection with a direct, explicit Domain restoration boundary. Keep current town creation, journey state, completed journeys, suspect-presence state, diary state and action-context state in their owning Domain operations; do not expose Persistence snapshot DTOs to Domain.
- [x] Replace reflective `GameClock` and `PursuitState` field writes with their explicit state-setting operations. Preserve null-required-field classification in the serializer so malformed current caches continue to enter the existing recovery path or fail closed.
- [x] Replace reflective restoration of GameSession version, seed and start-flow phase with explicit internal operations. Preserve snapshot version as the starting version when replaying a tail, and preserve `SeedCode` and derived phase when the snapshot is already current.
- [x] Remove `SetBackingField`, constructor discovery, `GetInternalProperty`, `SetInternalProperty` and reflection from the production persistence rehydration path. Restore the internal `TravelDiaryDayState` fields through an explicit object initializer or Domain-owned factory; do not add a test that asserts private member names or that reflection is absent. Prove repository behavior through the PostgreSQL scenarios and existing supported round-trip/recovery tests.
- [x] Run the relevant PostgreSQL tests with `.\tools\postgres-dev.ps1 ensure`, including the new continuation and travel-diary behavior, current action-context recovery, start-flow phase restore, current required-component recovery, and the existing event-sourcing end-to-end flow. Confirm the event-only and snapshot paths both support the same next legal action using independently expected wallet, inventory, context and stream-tail facts.
- [x] Update the persistence follow-up with the actual behavior gap, test sensitivity result, restoration fields covered and explicit remaining boundaries. Keep ADR-0028 and ADR-0038 unchanged because this implements their existing cache and consistency decisions; do not modify feature promises.
- [x] Run `py -3 tools/run.py ci --check`, inspect and stage the intended source, test and evidence changes, then make a normal hooked implementation commit without running the full canonical gate immediately around the successful commit.
- [ ] Complete a fresh whole-branch review against this plan, the baseline spec, event-sourcing integrity doctrine, ADR-0028, ADR-0038 and applicable unslop guidance. Publish a Draft PR to `develop`, verify the PR head equals the reviewed source, and verify the hosted `Canonical tracked commit gate` succeeds on that exact head. Repair any hosted failure and verify the final head again before merging.
- [ ] Merge only after the PR's exact-head hosted gate succeeds. Verify the push-triggered canonical gate succeeds on the resulting `develop` merge SHA before starting the next row 07 slice; leave this plan for retirement by the next substantive successor.

**Expected:** Persistence restores the supported session state through explicit Domain-owned operations, with no reflection-based private-field mutation. A resumed player can continue in the same context without spending another turn, and their next command commits once at the correct stream tail. Existing cache recovery, event replay and malformed-history behavior remain valid.
