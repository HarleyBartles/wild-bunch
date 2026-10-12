# Retire Compiler and Initializer Smoke Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove live tests that merely echo C# construction or implementation shape while retaining meaningful game, replay, serialization, generator, and persistence-boundary behavior proof.

**Architecture:** This is a test-quality slice only. Domain and GameContent smoke is removed at its current test owner; a PostgreSQL-backed event-store test observes the persisted payload boundary required by ADR-0028, while existing production paths continue to prove event creation and replay.

**Tech Stack:** .NET 10, xUnit, PostgreSQL integration tests, repository command bus.

**Spec:** [Stable 0.1.0 test-quality investigation, N-08](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md#n-08-compiler-and-initializer-smoke-sprawls-across-eventmodel-files)

**Execution Strategy:** `executing-plans` - the deletions, one integration-boundary assertion, and its investigation disposition share one test-quality decision; inline execution avoids splitting the evidence and review context.

## Global Constraints

- Start from the current `origin/develop` tip in a fresh canonical worktree; deliver the slice by PR to `develop`.
- Advance the single authored version in `Directory.Build.props` from `0.1.0-dev.122` to `0.1.0-dev.123` in the first substantive implementation commit; generated mirrors remain build-owned.
- Retire the N-07 plan in the first substantive implementation commit; retain this N-08 plan through its delivery and retire it in the next successor slice.
- Prove the new event-payload boundary assertion can fail by temporarily adding `Sequence` to the persisted event payload, then restore the production file exactly before validating the green state.
- Do not add tests for C# record, assignment, enum membership, or property-list shape. Preserve behavior tests that consume generation, commands, events, snapshots, projections, serialization, or persistence.
- Keep ADR-0028 truthful; no event architecture decision or product feature changes in this slice.
- Before requesting review, compare the actual diff with relevant ADRs and the feature matrix. After merge, require passing exact-head and develop-push gates, fast-forward the primary checkout, and remove the clean worktree before its exact local and upstream branch refs.

## Review Focus

- Persisted occurrence metadata (`sequence`, event identity, timestamps, schema, correlation, causation, and stream identity) must remain in the event-store envelope rather than the serialized domain-fact payload; prove this through the persisted purchase event and demonstrate the test fails under a deliberate `Sequence` payload mutation.
- Travel-day advancement must advance exactly one day and return the clock to morning in one observed command; consolidate the two tests for this same operation without losing either assertion.

---

### Task 1: Retire smoke tests and preserve the persisted event boundary

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs`
- Delete: `tests/WildBunch.Domain.Tests/World/LayoutSaltsTests.cs`
- Delete: `tests/WildBunch.Domain.Tests/World/SeedWorldTests.cs`
- Delete: `tests/WildBunch.Domain.Tests/World/TownLayoutLayoutSaltsTests.cs`
- Delete: `tests/WildBunch.Domain.Tests/StartingTownSelectedEventTests.cs`
- Delete: `tests/WildBunch.Domain.Tests/Events/TypedDomainEventTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs`
- Delete: `tests/WildBunch.GameContent.Tests/NewGame/PaletteSpecTests.cs`
- Delete: `tests/WildBunch.Domain.Tests/BeatModelRollupPreservationTests.cs`
- Create: `tests/WildBunch.Domain.Tests/AdvanceJourneyDayTests.cs`
- Delete: `tests/WildBunch.Domain.Tests/Mappers/TravelDiarySnapshotShapeTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row17-source-shape-detector-retirement.md`
- Modify: `.agents/plans/2026-10-12-row17-compiler-smoke-retirement.md`

**Interfaces:**
- Consumes: `StoreItemPurchased` persisted by `EfGameSessionRepository`, with infrastructure envelope fields stored on its event row.
- Produces: one falsifiable integration assertion that the serialized purchase payload contains domain facts but excludes envelope-only fields; existing event-generation and replay contracts remain owned by Domain and Integration tests.

- [x] Add an assertion to `EventStorePersistenceTests.StoreAsync_PurchaseAppendsStoreItemPurchasedEvent` that parses the persisted purchase row's `PayloadJson` and rejects the lower-camel-case envelope field names `eventId`, `sequence`, `occurredAtUtc`, `schemaVersion`, `correlationId`, `causationId`, and `streamId`. Keep the existing assertions that the event row has type `StoreItemPurchased` and sequence 8, so the test observes both the external payload and its separate persisted envelope.
- [x] Run `.\tools\postgres-dev.ps1 ensure`, then run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~StoreAsync_PurchaseAppendsStoreItemPurchasedEvent`. Confirm it passes on the unmodified event record.
- [x] Temporarily add `public long Sequence { get; init; }` to `src/WildBunch.Domain/Events/StoreItemPurchased.cs`. Rerun the focused integration test and confirm it fails because `sequence` appears in the serialized payload. Restore `StoreItemPurchased.cs` and confirm `git diff -- src/WildBunch.Domain/Events/StoreItemPurchased.cs` is empty; rerun the focused integration test and confirm it passes.
- [x] Delete the constructor-storage, record-equality, init-only, marker-interface, envelope-property-list, palette-assignment, enum-membership, and duplicated `TravelDiaryDayState` reflection smoke described by N-08. Consolidate the two `AdvanceJourneyDay` fragments into one test that asserts both one-day advancement and morning reset, removing the stale Task 4 characterization narrative. Keep `SeedWorldResolverCodecTests.Resolve_DecodesAll16PaletteValues`, generator determinism/salt tests, `BuildingLayoutCatalogTests`, `GameSessionEventSourcingTests`, `TravelDiaryDayProjectorParityTests`, and actual event-store serialization/replay tests; do not replace deleted smoke with a count or absence assertion.
- [x] Record a dated N-08 disposition in the investigation. State which currently present smoke was retired and why; record that `GameSessionDtoProjectionFieldsTests` and `ResolvedGameSetupLayoutSaltsTests` are already absent because commit `290b9cf4` on 2026-10-08 removed them during the incomplete developer salt-control retirement; preserve the active behavioral consumer tests named above. Do not infer that this slice caused their removal.
- [x] Update roadmap row 17 with PR #307 delivery facts: source `731285a134df5e43445c336afe5d99425894b86d`, merge `dddc6ac7511d534476191cdfaea973c243d05aef`, matching tree `147e71a9f0782638f96f5f81c085a74c2934938f`, whole-branch review clean, exact-head gate `38302286595` passed, develop-push gate `38302942362` passed, primary checkout fast-forwarded, and worktree plus local/upstream refs removed and verified. Set N-08 as the active JIT plan and preserve row 17 as incomplete.
- [x] Set the sole authored version to `0.1.0-dev.123`; retire the N-07 plan in this same first substantive implementation commit. Commit the plan separately first without changing the development version. Use the normal staged-snapshot hook for both commits; do not run a redundant full gate immediately around a successful hooked commit.
- [x] Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj` and `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj`; then rerun the focused PostgreSQL integration test from Step 2. Stage the complete intended implementation tree and commit as `test: retire compiler initializer smoke` through the normal fail-fast canonical hook; compare all test and build results with the claims above.

### Task 2: Review, publish, merge, and clean the execution unit

**Files:**
- Review the full branch diff against ADR-0028, the N-08 disposition, the feature matrix, and the Review Focus.

- [ ] Verify no product source, persistence schema, release feature, or ADR decision changed; verify the new integration test fails under the specified payload mutation and passes after restoration.
- [ ] Obtain a fresh whole-branch review, resolve actionable findings with focused red/green evidence, and rerun the canonical gate through a normal hooked commit if the tree changes.
- [ ] Publish PR `test: retire compiler initializer smoke` to `develop`, independently confirm the author's ADR check and reviewer check, mark it ready, and require the exact-head canonical hosted gate to pass.
- [ ] Squash merge under the authorized epic flow; verify the develop-push gate passes and the merged tree equals the reviewed source tree.
- [ ] Fast-forward the primary checkout. Verify the merged PR head, remove the clean worktree first, explicitly delete the exact local and upstream refs, and verify all three are absent before creating the next plan worktree.
- [ ] Keep this plan for the next successor slice to retire; do not claim row 17 complete while N-09 and later findings remain.
