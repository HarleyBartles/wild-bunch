# Prove Versioned Diary Cache Behavior Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace projection-version constant detectors and empty-rebuild assertions with behavior proof for current diary-cache loading and stale or mixed-cache reconstruction from nonempty event history.

**Architecture:** Persistence owns component and diary payload version checks, while typed events remain authoritative and `TravelDiaryDayProjector` rebuilds the diary view. Integration tests exercise the production `PersistedPayloadLoader` boundary with a deterministic event-backed travel day and independently asserted diary facts; no production behavior changes are expected.

**Tech Stack:** .NET 10, xUnit, Wild Bunch command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially the cache/recovery and history-support contracts; [test-quality investigation N-11](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md#n-11-version-completeness-and-stale-diary-rebuilding-have-weak-oracles).

**Execution Strategy:** `executing-plans` - the version constant detector, its payload-loader replacements, and their shared deterministic event fixture form one test-boundary change; one inline context avoids duplicating setup and keeps the test oracle, version semantics, and dated audit disposition together. A fresh implementer per task would add handoff cost without an independent review boundary.

## Global Constraints

- Start from the current `origin/develop` in this fresh canonical worktree and deliver one PR to `develop`.
- Keep the plan-only commit at `0.1.0-dev.125`; advance the sole authored version in `Directory.Build.props` to `0.1.0-dev.126` in the first substantive implementation commit.
- Retire the N-10 plan in the first substantive implementation commit; retain this plan through delivery for its next substantive successor to assess.
- Test the production `PersistedPayloadLoader` and `TravelDiaryDayProjector` boundaries; do not add production code or a projection-version registry.
- Remove assertions that freeze `ProjectionVersions.DiaryDay` at 1 or enumerate component names through `ForComponent`, whose current implementation returns one constant independent of the name.
- Retain future-event rejection, current component payload loading, stale component reconstruction, missing-component behavior, registered event upcasts, and runtime upcaster-chain gap guards.
- Make stale and mixed diary rebuilds use a real nonempty `TravelDayAdvanced` event and assert exact diary day, towns, resource outcome, and an event-authored entry without deriving expected values by calling the same projector.
- Before review, compare the actual diff with ADR-0028 and `docs/features.md`; no durable decision, product promise, dependency, or disposition changes, but PLAT-001's evidence must record the new diary-cache behavior proof and its loader-level limit.
- Use the normal staged-snapshot hook, which validates without mutating or staging files; do not run `git commit --no-verify`.
- Require fresh whole-branch review, passing exact-head hosted CI, and passing develop-push CI before paired worktree and branch cleanup.

## Review Focus

- Stale diary schema must discard stale payloads and rebuild a populated event-derived day; returning an empty diary or stale content must fail the test.
- Any stale row in a mixed cache must discard all cached rows, including valid-current rows with recognizable decoy content.
- A current diary schema with matching stream version, day count, and contiguous sequence must return stored diary content rather than silently rebuilding it.
- No test should assert a projection-version integer or claim completeness from a hand-maintained component-name list.

---

### Task 1: Prove supported diary payload-version behavior

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/Versioning/VersionMismatchBehaviorTests.cs`
- Modify: `docs/features.md`
- Delete: `tests/WildBunch.Integration.Tests/Versioning/ProjectionVersionCompletenessTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row17-n10-projector-nonmutation.md`
- Modify: `.agents/plans/2026-10-12-row17-n11-diary-version-rebuild.md`

**Interfaces:**
- Consumes: `PersistedPayloadLoader.LoadDiaryDays(stored, events, streamVersion, projectionStreamVersion, projectionDayCount)`, `ProjectionVersions.DiaryDay`, `TravelDiaryDayProjector`, and the typed `TravelDayAdvanced` event sequence produced by `GameSession` travel.
- Produces: focused integration tests proving current cached diary reads and stale/mixed cache rebuilds against independent nonempty expected facts; a dated N-11 disposition and a roadmap entry that records N-10 delivery and marks N-11 active.

- [x] **Step 1: Replace integer/name detectors and strengthen rebuild assertions.** Delete the two facts in `ProjectionVersionCompletenessTests`; keep behavior coverage in `VersionMismatchBehaviorTests`. Extend its existing session fixture through a real short journey, force a quiet travel day with the distinctive event message `A sagebrush wind marked the day's quiet miles.`, and retain the generated `TravelDayAdvanced` in the event list. Assert the fixture contains exactly one travel-day event with that message before using it. Change the stale diary version to `ProjectionVersions.DiaryDay - 1`, rather than a hard-coded current/future integer. In the stale test, serialize a valid cached day based on the same deterministic scenario but with a cache-only entry, store it at the prior schema version, and require the returned list to contain exactly one rebuilt day with `DayNumber == 1`, origin `Pinecross`, destination `Quartzsite`, `FoodDelta == -1`, `CurrentFood == 3`, and exactly the event-authored entry. Do not compare to `projector.Project(events)` or use only a count assertion.

  Use the existing session fixture's world and inventory to create the preview through `TravelResolver.PreviewJourney`, start that journey through `GameSession.StartJourney`, call `ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet, "A sagebrush wind marked the day's quiet miles."))`, and require a successful `AdvanceJourneyDay`. Assert one `TravelDayAdvanced` event whose `DayEntries` contains that exact message. For an independently recognizable valid cache row, project those events once to construct serialized storage data, then use a record `with` expression to replace only `Entries`; the stale/mixed assertions themselves must use the fixed expected fields and message above, never that projector output as their expected result.

  For the mixed-version test, keep one valid current-version row whose serialized day contains `Discard this cached diary line.`, plus one stale row whose payload remains malformed. Supply stream metadata that otherwise matches both rows. Require the rebuilt result to contain the event entry and exclude the cache-only line. The stale single-version test's valid cache-only payload makes wrong-branch failure observable; the malformed mixed row separately preserves proof that an invalid stale row is discarded with the complete cache.

  Add `LoadDiaryDays_CurrentVersion_UsesStoredJson`: prepare a valid projected day from the deterministic event fixture, change only its `Entries` to `Persist this current-version diary line.`, serialize it with the production serializer at `ProjectionVersions.DiaryDay`, and pass matching stream version, day count, and contiguous sequence. Assert the returned single day contains that exact persisted line and not the event fixture's distinctive line. This proves the current-cache path without freezing the version integer.

- [x] **Step 2: Run the focused lane and prove the assertions detect the wrong behavior.** Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~VersionMismatchBehaviorTests`. For the stale-path proof, temporarily replace `var hasCurrentSchema = ordered.All(day => day.SchemaVersion == ProjectionVersions.DiaryDay);` in `PersistedPayloadLoader.LoadDiaryDays` with `var hasCurrentSchema = true;`; the stale test must fail at the fixed event-entry assertion because it receives the valid stale cache-only line. Restore `PersistedPayloadLoader.cs` byte-for-byte, verify its diff is empty, and rerun the focused test successfully. For the current-version proof, temporarily add `return _diaryDayProjector.Project(events).Days;` as the first statement in `LoadDiaryDays`; the current-version test must fail because the event entry replaces the persisted marker. Restore and rerun. Do not commit either mutation.

- [x] **Step 3: Record dispositions and successor delivery metadata.** Add a dated N-11 disposition that deletes the version-1 integer and component-name detectors, describes the current/stale/mixed cache behavior now exercised, and preserves the unrelated future-event, upcast and chain-gap guards. Update PLAT-001's dated evidence in `docs/features.md` to describe these new loader-level tests without implying PostgreSQL recovery proof. Update row 17 with PR #310, reviewed source `cffe3471a42e8f48e57fafee89d157c828479b3c`, merge `08c120b4d2c71b95ad5aed261682ca53ceeae5e1`, matching tree `248ee78bb2d2e94857e8e282a18c2ff6db6f338e`, exact-head gate `38314430456`, develop-push gate `38315022543`, and verified paired worktree/ref cleanup. Set N-11 active at `.126` and leave row 17 incomplete.

- [x] **Step 4: Advance version and retire the predecessor plan.** In the first substantive implementation commit, change the sole authored version in `Directory.Build.props` from `0.1.0-dev.125` to `0.1.0-dev.126`, delete the N-10 plan, and retain this plan. Review the intended staged diff and commit through the normal check-only hook.

- [x] **Step 5: Run focused and complete validation.** Rerun the focused `VersionMismatchBehaviorTests` command. Run `.\tools\postgres-dev.ps1 ensure`, then `py -3 tools/run.py ci --check`. Confirm there are no unexpected skips and no production-source changes.

- [ ] **Step 6: Review, publish and close the execution unit.** Run completion-readiness and fresh whole-branch review against this plan, the N-11 investigation, the stable baseline specification, ADR-0028 and the feature matrix. Resolve actionable findings with focused proof and a normal hooked commit. Publish one PR to `develop`, independently confirm the author ADR and feature-matrix checks, mark it ready, and require the exact-head canonical hosted gate to pass. Squash merge under the authorized epic flow; require the develop-push gate to pass. Fast-forward the primary checkout, verify merge containment and tree equality, remove this clean worktree first, delete only its exact local and upstream refs, and verify all three are absent before starting another plan worktree. Keep row 17 incomplete.
