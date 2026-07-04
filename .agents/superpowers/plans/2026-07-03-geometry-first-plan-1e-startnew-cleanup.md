# Geometry-First Map Generation - Plan 1e: Remove StartNew — Cleanup (Test Migration + Deletion + Round-Trip Proof)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Complete the removal of `GameSession.StartNew` by migrating all remaining 65 direct test call sites to the canonical start flow, cleaning up the legacy `RecaptureGameStartedForReplay` alias and stale variable names, fixing `CreateBaselineCaseFileFor` to carry non-investigation case file state, deleting `StartNew` and `SeededNewGameFactory.Create`, and adding the definitive round-trip proof that a session created through the canonical flow can be fully rehydrated from its event stream alone — including ALL 14 `CaseFile` fields.

**Architecture:** Plan 1d rewrote the test helper factories (`TestSessionFactory`, `TravelTestFactory`, `StubNewGameFactory`) to use the canonical flow and completed `CaseFileSnapshot` with all 14 `CaseFile` fields. This plan handles the remaining 65 test files that call `GameSession.StartNew(...)` directly, cleans up legacy artifacts from Plan 1d (the `RecaptureGameStartedForReplay` alias, stale variable names, `CreateBaselineCaseFileFor` data loss), deletes `StartNew` and `SeededNewGameFactory.Create`, and adds a comprehensive round-trip proof test covering all `CaseFile` fields.

**Discovery-first approach:** Task 1 runs the full test suite after Plan 1d and produces an enumerated failure list. Tasks 2-5 fix the enumerated failures in batches by test project. This converts an open-ended "fix whatever breaks" into a deterministic checklist.

**Greenfield Context:** This is a greenfield project with no backward compatibility requirements. Database can be dropped/rebuilt as needed. No migration for existing sessions is required.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3, existing event-sourced GameSession aggregate

## Prerequisites

- Plan 0 (Clean Slate) must be complete.
- Plan 1a (Core Pipeline) must be complete.
- Plan 1b (Event Boundary) must be complete.
- Plan 1c (CaseFile Event Boundary) must be complete.
- **Plan 1d (Remove StartNew — Core) must be complete** — `CaseFileSnapshot` must carry all 14 `CaseFile` fields, `TestSessionFactory`/`TravelTestFactory`/`StubNewGameFactory` must use the canonical flow, and `GameSessionEventSourcingTests.cs` must pass.

## Starting state

After Plan 1d:
- `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs` carries all 14 `CaseFile` fields (PublicClues, KnownWarrants, PublicWarrants, KillerReleaseThreshold, KillerReleaseProgress, SuspectTurfAssignments, WantedSuspectConfrontations, SheriffTurnInSettlements, Accusation, DiscoveredSuspectIds).
- `TestSessionFactory`, `TravelTestFactory`, and `StubNewGameFactory` use `StartGameCanonical` (canonical flow).
- `GameSessionEventSourcingTests.cs` passes with canonical flow assertions (version 6, 6 events).
- `TravelTestFactory.RecaptureSetupEventsForReplay` returns all 6 setup events (not just `GameStarted`).
- **65 test files still call `GameSession.StartNew(...)` directly** — these will fail to compile once `StartNew` is deleted.
- `GameSession.StartNew` still exists (not yet deleted).
- `SeededNewGameFactory.Create` still exists and calls `StartNew` (not yet deleted).
- **Legacy alias `RecaptureGameStartedForReplay`** still exists in `TravelTestFactory` — all 8 external callers use the old name, only 2 internal calls use `RecaptureSetupEventsForReplay`. Variables named `gameStarted` in 5 blast-radius files actually hold `IReadOnlyList<IDomainEvent>` (6 events).
- **`CreateBaselineCaseFileFor` drops non-investigation state** — `killerReleaseThreshold`, `killerReleaseProgress`, `suspectTurfAssignments`, `wantedSuspectConfrontations`, `sheriffTurnInSettlements` default to empty/0/2 during replay. Latent, not active (no replay test asserts on these fields yet).

## Lessons learned from Plan 1d

These lessons shape this plan:

1. **Task ordering matters for compilation.** `SeededNewGameFactory.Create` calls `StartNew`. Deleting `StartNew` before `SeededNewGameFactory.Create` breaks the build. This plan deletes `SeededNewGameFactory.Create` first, then `StartNew`.

2. **Scope expansion is normal.** Plan 1d's Task 3 absorbed Task 4's scope because fixing `RecaptureSetupEventsForReplay` required migrating `TravelTestFactory`'s remaining `StartNew` calls. Expect similar cross-task dependencies here — if migrating a test file reveals a helper method that also calls `StartNew`, migrate the helper too.

3. **`CaseFileSnapshot` is complete — the round-trip proof must verify ALL fields.** Plan 1d's original round-trip proof only checked `PublicClues`. The new proof must verify warrants, killer release gate, turf assignments, confrontations, settlements, accusation, and discovered suspect IDs.

4. **Legacy cleanup is real work.** The `RecaptureGameStartedForReplay` alias and stale `gameStarted` variable names are readability/maintainability debt that Plan 1d deferred. This plan pays that debt before deletion.

5. **`CreateBaselineCaseFileFor` is a latent replay bug.** It drops 5 case file fields during replay reconstruction. This plan fixes it so the round-trip proof can verify all fields.

## Migration pattern (used for all 65 call sites)

Every `StartNew` call follows the same shape. The migration is mechanical:

**Before:**
```csharp
var session = GameSession.StartNew(
    playerName, world, caseFile, startingTownId,
    wallet, inventory, gameDifficulty, saltSource, gameEntropy, seedCode);
```

**After (inline):**
```csharp
var session = GameSession.StartSetup(
    playerName, world, caseFile, gameDifficulty, gameEntropy,
    seedCode ?? "test-seed", saltSource ?? SaltSource.CreateFixed(string.Empty));
session.ViewPrologue("test-prologue-descriptor");
session.SelectStartingTown(startingTownId ?? world.Towns.First().Id);
session.CompleteGameStart(wallet, inventory);
```

**After (via TestSessionFactory helper — for Domain.Tests files):**
```csharp
var session = TestSessionFactory.StartGameCanonical(
    playerName, world, caseFile, startingTownId,
    wallet, inventory, gameDifficulty, saltSource, gameEntropy, seedCode);
```

**Decision rule:** If the test file is in `WildBunch.Domain.Tests` and already references `TestSessionFactory`, use `TestSessionFactory.StartGameCanonical`. Otherwise, inline the 4-step canonical flow. For `WildBunch.Application.Tests` and `WildBunch.Integration.Tests`, inline the flow (they don't reference `Domain.Tests`).

**Important:** Most tests call `MarkEventsCommitted()` after session creation. The canonical flow produces 6 setup events (version 6) instead of 1 (version 1). Tests that call `MarkEventsCommitted()` before asserting on operation events are unaffected. Tests that assert on initial `UncommittedEvents.Count`, `Version`, or specific event indices need assertion updates.

---

## Phase 1: Discovery

### Task 1: Discovery pass — enumerate all remaining StartNew call sites and test failures

**Goal:** Produce an enumerated list of all 65 remaining `StartNew` call sites and any test failures caused by Plan 1d's factory migration. This list drives Tasks 3-5.

**Files:**
- No modifications yet — this task produces a report.

- [x] **Step 1: Grep for all remaining StartNew calls in test files**

Run:
```bash
rg -l "GameSession\.StartNew" tests/ --type cs
```
Capture the file list. These are the 65 files that need migration.

- [x] **Step 2: Run the full Domain.Tests suite and capture failures**

Run:
```bash
dotnet test tests/WildBunch.Domain.Tests/ 2>&1 | tee plan-1e-domain-failures.txt
```
Capture: test name, file, line, assertion message. Filter to failures caused by the canonical flow change (event count, version, event index, rehydration). Ignore unrelated failures.

- [x] **Step 3: Run the full Application.Tests suite and capture failures**

Run:
```bash
dotnet test tests/WildBunch.Application.Tests/ 2>&1 | tee plan-1e-application-failures.txt
```

- [x] **Step 4: Run the full Integration.Tests suite and capture failures (if Docker available)**

Run:
```bash
dotnet test tests/WildBunch.Integration.Tests/ 2>&1 | tee plan-1e-integration-failures.txt
```
If Testcontainers/Docker is not available, note this and skip. Integration tests will be verified in the final phase.

- [x] **Step 5: Produce the enumerated migration list**

From the grep results and test failures, create a checklist. For each file, record:
- File path
- Number of `StartNew` call sites
- Migration strategy: `FACTORY_DELEGATE` (use `TestSessionFactory.StartGameCanonical`) or `INLINE` (inline the 4-step flow)
- Whether assertions need updating (event count, version, event index)

Write the checklist to `.agents/superpowers/sdd/2026-07-03-geometry-first-plan-1e-startnew-cleanup/discovery-report.md`.

- [x] **Step 6: Commit the discovery report**

```bash
git add .agents/superpowers/sdd/2026-07-03-geometry-first-plan-1e-startnew-cleanup/discovery-report.md
git commit -m "docs: Plan 1e discovery report — enumerate remaining StartNew call sites"
```

---

## Phase 2: Legacy Cleanup (Plan 1d deferred items)

**Goal:** Clean up the legacy artifacts Plan 1d deferred: the `RecaptureGameStartedForReplay` alias, stale `gameStarted` variable names, stale comment, and `CreateBaselineCaseFileFor` data loss. Do this BEFORE migrating the 65 call sites so the migration uses clean naming.

### Task 2: Rename RecaptureGameStartedForReplay callers + fix stale variable names

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelTestFactory.cs` — delete the legacy alias
- Modify: `tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs` — rename caller + variable
- Modify: `tests/WildBunch.Domain.Tests/DevTravelOverrideTests.cs` — rename 3 callers + variables
- Modify: `tests/WildBunch.Domain.Tests/GameSessionDevDifficultyTests.cs` — rename caller + variable
- Modify: `tests/WildBunch.Domain.Tests/GameSessionDevEntropyTests.cs` — rename caller + variable
- Modify: `tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs` — rename 3 callers + variables + fix stale comment

**Context:** Plan 1d renamed the method body to `RecaptureSetupEventsForReplay` but kept `RecaptureGameStartedForReplay` as a forwarding alias. All 8 external callers still use the old name. Variables named `gameStarted` / `originalGameStarted` now hold `IReadOnlyList<IDomainEvent>` (6 events), not a single `GameStarted`. One stale comment in `JournalLogProjectorEquivalenceTests.cs:44` says "Capture GameStarted BEFORE any travel commands" — should reference setup events.

- [x] **Step 1: Rename all callers from RecaptureGameStartedForReplay to RecaptureSetupEventsForReplay**

In each of the 6 files listed above, replace `RecaptureGameStartedForReplay` with `RecaptureSetupEventsForReplay`. There are 8 call sites total:
- `DevSaloonOverrideTests.cs` — 1 call
- `DevTravelOverrideTests.cs` — 3 calls
- `GameSessionDevDifficultyTests.cs` — 1 call
- `GameSessionDevEntropyTests.cs` — 1 call
- `JournalLogProjectorEquivalenceTests.cs` — 3 calls (including the one with the stale comment)

- [x] **Step 2: Rename variables from gameStarted to setupEvents**

In each file, rename variables that hold the result of `RecaptureSetupEventsForReplay`:
- `var gameStarted = ...` → `var setupEvents = ...`
- `var originalGameStarted = ...` → `var originalSetupEvents = ...`
- Update all references to the renamed variable within the same method

- [x] **Step 3: Fix stale comment in JournalLogProjectorEquivalenceTests.cs**

Find the comment that says "Capture GameStarted BEFORE any travel commands" and update it to "Capture setup events BEFORE any travel commands".

- [x] **Step 4: Delete the legacy alias from TravelTestFactory.cs**

Delete the `RecaptureGameStartedForReplay` method (the forwarding alias):
```csharp
internal static IReadOnlyList<IDomainEvent> RecaptureGameStartedForReplay(GameSession session)
    => RecaptureSetupEventsForReplay(session);
```

- [x] **Step 5: Run Domain.Tests to verify**

Run: `dotnet test tests/WildBunch.Domain.Tests/`
Expected: All 525 tests pass. No behavioral change — pure rename.

- [x] **Step 6: Commit**

```bash
git add tests/WildBunch.Domain.Tests/
git commit -m "refactor: rename RecaptureGameStartedForReplay to RecaptureSetupEventsForReplay

Plan 1d kept RecaptureGameStartedForReplay as a legacy alias. All 8
external callers now use the canonical name RecaptureSetupEventsForReplay.
Variables renamed from gameStarted to setupEvents to reflect that they
hold IReadOnlyList<IDomainEvent> (6 setup events), not a single GameStarted.
Deleted the legacy alias."
```

### Task 3: Fix CreateBaselineCaseFileFor to carry non-investigation case file state

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TestSessionFactory.cs:648-663`

**Context:** `CreateBaselineCaseFileFor` reconstructs a baseline case file for replay tests. It currently carries over `Suspects`, `TrueCulpritId`, `OpeningLead`, `DiscoveredSuspectIds`, `PublicClues`, `PublicWarrants` but drops `killerReleaseThreshold`, `killerReleaseProgress`, `suspectTurfAssignments`, `wantedSuspectConfrontations`, `sheriffTurnInSettlements`, and `KnownWarrants` (sets them to empty/0/2). This is a latent replay bug — if a session created via `CreateWithKillerReleaseGateOpen` (threshold=2, progress=2) is replayed, the rehydrated session would have progress=0.

- [x] **Step 1: Update CreateBaselineCaseFileFor to carry over all fields**

In `TestSessionFactory.cs`, update the `CreateBaselineCaseFileFor` method to carry over the missing fields from `originalCase`:

```csharp
public static CaseFile CreateBaselineCaseFileFor(GameSession session)
{
    ArgumentNullException.ThrowIfNull(session);

    var originalCase = session.CaseFile;
    return new CaseFile(
        accusation: null,
        originalCase.Suspects,
        originalCase.TrueCulpritId,
        originalCase.OpeningLead,
        knownClues: Array.Empty<Clue>(),
        discoveredSuspectIds: originalCase.DiscoveredSuspectIds,
        publicClues: ReconstructPublicClues(originalCase),
        killerReleaseThreshold: originalCase.KillerReleaseThreshold,
        killerReleaseProgress: originalCase.KillerReleaseProgress,
        knownWarrants: Array.Empty<Warrant>(),
        publicWarrants: ReconstructPublicWarrants(originalCase),
        suspectTurfAssignments: originalCase.SuspectTurfAssignments,
        wantedSuspectConfrontations: originalCase.WantedSuspectConfrontations,
        sheriffTurnInSettlements: originalCase.SheriffTurnInSettlements);
}
```

Note: `knownClues` and `knownWarrants` are set to empty because the baseline represents pre-investigation state. The other fields (killer release gate, turf assignments, confrontations, settlements) are setup-time state, not investigation-discovered state, so they should be preserved.

- [x] **Step 2: Run Domain.Tests to verify**

Run: `dotnet test tests/WildBunch.Domain.Tests/`
Expected: All 525 tests pass. The replay tests that use `CreateBaselineCaseFileFor` now preserve non-investigation state.

- [x] **Step 3: Commit**

```bash
git add tests/WildBunch.Domain.Tests/TestSessionFactory.cs
git commit -m "fix: CreateBaselineCaseFileFor preserves non-investigation case file state

CreateBaselineCaseFileFor was dropping killerReleaseThreshold,
killerReleaseProgress, suspectTurfAssignments, wantedSuspectConfrontations,
and sheriffTurnInSettlements during replay reconstruction. These are
setup-time state, not investigation-discovered state, so they should
be preserved. This was a latent replay bug — no test asserted on these
fields after replay, but the round-trip proof test will."
```

---

## Phase 3: Migrate Domain.Tests direct call sites

**Goal:** Migrate all `StartNew` calls in `WildBunch.Domain.Tests` test files that call `StartNew` directly (not through factories). These files can use `TestSessionFactory.StartGameCanonical` since they're in the same project.

### Task 4: Migrate Domain.Tests files with single StartNew call sites

**Files (25 files, 1 call site each):**
- `tests/WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs:148`
- `tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs:141`
- `tests/WildBunch.Domain.Tests/GameSessionArchiveTests.cs:210`
- `tests/WildBunch.Domain.Tests/TravelRulesProfileTests.cs:75`
- `tests/WildBunch.Domain.Tests/PurchaseBeatCostTests.cs:75`
- `tests/WildBunch.Domain.Tests/GameSessionPurchaseTests.cs:175`
- `tests/WildBunch.Domain.Tests/GameSessionSheriffTurnInTests.cs:247`
- `tests/WildBunch.Domain.Tests/GameSessionWantedSuspectPresenceTests.cs:82`
- `tests/WildBunch.Domain.Tests/GameSessionJourneyHistoryTests.cs:87`
- `tests/WildBunch.Domain.Tests/GameSessionWantedSuspectConfrontationTests.cs:313`
- `tests/WildBunch.Domain.Tests/GameSessionWantedPostersTests.cs:216`
- `tests/WildBunch.Domain.Tests/TownActionAvailabilityTests.cs:48`
- `tests/WildBunch.Domain.Tests/JournalResolverTests.cs:112`
- `tests/WildBunch.Domain.Tests/GameSessionBountyLoopCoordinatorTests.cs:73`
- `tests/WildBunch.Domain.Tests/BountySettlementPolicyTests.cs:114`
- `tests/WildBunch.Domain.Tests/BeatModelEconomyTests.cs:138`

**Files with 2-4 call sites:**
- `tests/WildBunch.Domain.Tests/GameSessionSaloonWantedSuspectLoopTests.cs:171,196` (2 calls)
- `tests/WildBunch.Domain.Tests/GameSessionUnrelatedCriminalLedgerWiringTests.cs:171,233` (2 calls)
- `tests/WildBunch.Domain.Tests/ActionAvailabilityResolverTests.cs:157,188` (2 calls)
- `tests/WildBunch.Domain.Tests/GameSessionResolverWiringTests.cs:131,188,244,304` (4 calls)

**Pattern for each file:** Replace `GameSession.StartNew(` with `TestSessionFactory.StartGameCanonical(`. Add `using static WildBunch.Domain.Tests.TestSessionFactory;` if not already present, or use the fully qualified `TestSessionFactory.StartGameCanonical(...)`.

- [x] **Step 1: Migrate all single-call-site files**

For each file listed above, replace `GameSession.StartNew(` with `TestSessionFactory.StartGameCanonical(`. Check each call site for assertion updates (see Step 2).

- [x] **Step 2: Check for assertion updates**

For each migrated file, check if the test asserts on:
- `UncommittedEvents.Count` right after creation (without `MarkEventsCommitted()`) → update to 6
- `Version` right after creation → update to 6
- `UncommittedEvents[0]` expecting `GameStarted` → use `UncommittedEvents.OfType<GameStarted>().Single()`

If the test calls `MarkEventsCommitted()` before any assertions on events/version, no assertion update is needed.

- [x] **Step 3: Run Domain.Tests to verify**

Run: `dotnet test tests/WildBunch.Domain.Tests/`
Expected: All migrated tests pass.

- [x] **Step 4: Commit**

```bash
git add tests/WildBunch.Domain.Tests/
git commit -m "refactor: migrate Domain.Tests single-call-site files to canonical start flow

Migrated 25 test files from GameSession.StartNew to
TestSessionFactory.StartGameCanonical. Updated assertions where
tests checked initial event count or version without
MarkEventsCommitted()."
```

### Task 5: Migrate Domain.Tests files with multiple StartNew call sites

**Files (high call-count files):**
- `tests/WildBunch.Domain.Tests/TravelResolverTests.cs` — 17 call sites
- `tests/WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs` — 6 call sites
- `tests/WildBunch.Domain.Tests/GameSessionInvestigationActionsTests.cs` — 7 call sites
- `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs` — 6 call sites

**Pattern:** Same as Task 4 — replace `GameSession.StartNew(` with `TestSessionFactory.StartGameCanonical(`. For files with many call sites, use find-and-replace within the file. Check each call site for assertion updates.

- [x] **Step 1: Migrate TravelResolverTests.cs (17 calls)**

Replace all 17 `GameSession.StartNew(` with `TestSessionFactory.StartGameCanonical(`. Check lines 397, 416, 454, 457, 1012, 1025, 1050, 1084, 1105, 1131, 1155, 1179, 1203, 1227, 1251, 1287, 1322 for assertion updates.

- [x] **Step 2: Migrate TravelDayPlanGeneratorTests.cs (6 calls)**

Replace all 6 `GameSession.StartNew(` with `TestSessionFactory.StartGameCanonical(`. Check lines 457, 501, 539, 567, 595, 623 for assertion updates.

- [x] **Step 3: Migrate GameSessionInvestigationActionsTests.cs (7 calls)**

Replace all 7 `GameSession.StartNew(` with `TestSessionFactory.StartGameCanonical(`. Check lines 347, 446, 504, 570, 644, 710, 762 for assertion updates.

- [x] **Step 4: Migrate GameSessionSaloonPersonOfInterestTests.cs (6 calls)**

Replace all 6 `GameSession.StartNew(` with `TestSessionFactory.StartGameCanonical(`. Check lines 428, 475, 522, 541, 573, 605 for assertion updates.

- [x] **Step 5: Run Domain.Tests to verify**

Run: `dotnet test tests/WildBunch.Domain.Tests/`
Expected: All tests pass.

- [x] **Step 6: Commit**

```bash
git add tests/WildBunch.Domain.Tests/
git commit -m "refactor: migrate Domain.Tests multi-call-site files to canonical start flow

Migrated TravelResolverTests (17 calls), TravelDayPlanGeneratorTests
(6 calls), GameSessionInvestigationActionsTests (7 calls), and
GameSessionSaloonPersonOfInterestTests (6 calls) from StartNew to
TestSessionFactory.StartGameCanonical."
```

---

## Phase 4: Migrate Application.Tests and Integration.Tests call sites

**Goal:** Migrate all `StartNew` calls in `WildBunch.Application.Tests` and `WildBunch.Integration.Tests`. These projects don't reference `Domain.Tests`, so the canonical flow must be inlined.

### Task 6: Migrate Application.Tests files

**Files (33 files, 1 call site each unless noted):**
- `tests/WildBunch.Application.Tests/TurnInToSheriffHandlerTests.cs:101`
- `tests/WildBunch.Application.Tests/TravelToTownHandlerTests.cs:120`
- `tests/WildBunch.Application.Tests/SaloonPersonOfInterestDescriptorParityTests.cs:155,174` (2 calls)
- `tests/WildBunch.Application.Tests/ResolveJourneyEncounterHandlerTests.cs:210`
- `tests/WildBunch.Application.Tests/ReadWantedPostersHandlerTests.cs:180`
- `tests/WildBunch.Application.Tests/QueryHandlersAreReadOnlyTests.cs:56`
- `tests/WildBunch.Application.Tests/PurchaseStoreItemHandlerTests.cs:196`
- `tests/WildBunch.Application.Tests/PreviewTravelHandlerTests.cs:72`
- `tests/WildBunch.Application.Tests/InvestigationSourceHandlerTests.cs:162`
- `tests/WildBunch.Application.Tests/InspectNoticeBoardHandlerTests.cs:125`
- `tests/WildBunch.Application.Tests/GetTownStoreOffersHandlerTests.cs:99`
- `tests/WildBunch.Application.Tests/GetJournalHandlerTests.cs:188`
- `tests/WildBunch.Application.Tests/GetGameSessionHandlerTests.cs:194`
- `tests/WildBunch.Application.Tests/GetAvailableActionsHandlerTests.cs:81`
- `tests/WildBunch.Application.Tests/Execution/GameSessionCommandHandlerTests.cs:140`
- `tests/WildBunch.Application.Tests/Dev/SetDevEntropyHandlerTests.cs:74`
- `tests/WildBunch.Application.Tests/Dev/GetTravelDevContextHandlerTests.cs:84`
- `tests/WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs:134`
- `tests/WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.cs:206`
- `tests/WildBunch.Application.Tests/Dev/ForceTravelOverrideHandlerTests.cs:88`
- `tests/WildBunch.Application.Tests/Dev/ForceSaloonOverrideHandlerTests.cs:168`
- `tests/WildBunch.Application.Tests/Dev/ForceDevSaltSourceHandlerTests.cs:139`
- `tests/WildBunch.Application.Tests/Dev/ForceDevDifficultyHandlerTests.cs:75`
- `tests/WildBunch.Application.Tests/Dev/ClearTravelOverrideHandlerTests.cs:73`
- `tests/WildBunch.Application.Tests/Dev/ClearSaloonOverrideHandlerTests.cs:84`
- `tests/WildBunch.Application.Tests/Dev/ClearDevSaltSourceHandlerTests.cs:72`
- `tests/WildBunch.Application.Tests/ConfrontWantedSuspectHandlerTests.cs:81`
- `tests/WildBunch.Application.Tests/ConfrontSaloonWantedSuspectHandlerTests.cs:87`
- `tests/WildBunch.Application.Tests/CompletePlayerSetupOneActivePlaythroughTests.cs:175`
- `tests/WildBunch.Application.Tests/CheckSheriffRecordsHandlerTests.cs:125`
- `tests/WildBunch.Application.Tests/CaseBoardMapperTests.cs:364`
- `tests/WildBunch.Application.Tests/ArchivePlaythroughHandlerTests.cs:71`
- `tests/WildBunch.Application.Tests/AdvanceTravelDayHandlerTests.cs:179,209,239,278` (4 calls)

**Pattern for each file:** Inline the 4-step canonical flow. Replace:
```csharp
var session = GameSession.StartNew(playerName, world, caseFile, townId, wallet, inventory, ...);
```
With:
```csharp
var session = GameSession.StartSetup(playerName, world, caseFile, difficulty, entropy, seedCode ?? "test-seed", saltSource ?? SaltSource.CreateFixed("test"));
session.ViewPrologue("test-prologue-descriptor");
session.SelectStartingTown(townId);
session.CompleteGameStart(wallet, inventory);
```

Many of these test files have a private helper method that creates the session. If so, only the helper needs changing. Check each file for a `CreateSession` or similar private method.

- [x] **Step 1: Migrate all Application.Tests files**

For each file listed above, replace `GameSession.StartNew(...)` with the inlined 4-step canonical flow. If the file has a private `CreateSession` helper, change only the helper.

- [x] **Step 2: Check for assertion updates**

Same rules as Task 4: update event count (6), version (6), or event index assertions if the test doesn't call `MarkEventsCommitted()` first.

- [x] **Step 3: Run Application.Tests to verify**

Run: `dotnet test tests/WildBunch.Application.Tests/`
Expected: All tests pass.

- [x] **Step 4: Commit**

```bash
git add tests/WildBunch.Application.Tests/
git commit -m "refactor: migrate Application.Tests to canonical start flow

Migrated 33 Application.Tests files from GameSession.StartNew to
the inlined 4-step canonical flow. Updated assertions where needed."
```

### Task 7: Migrate Integration.Tests files

**Files (8 files):**
- `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs:612,654` (2 calls)
- `tests/WildBunch.Integration.Tests/EventSourcingEndToEndTests.cs:67`
- `tests/WildBunch.Integration.Tests/UnrelatedCriminalLedgerPersistenceTests.cs:143`
- `tests/WildBunch.Integration.Tests/PostgreSqlPersistenceTests.cs:242`
- `tests/WildBunch.Integration.Tests/MigrationTests.cs:132`
- `tests/WildBunch.Integration.Tests/GameSessionDifficultyPersistenceTests.cs:433,481,695` (3 calls)
- `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs:551,613,653,683,721,745,771,823,861` (9 calls)
- `tests/WildBunch.Integration.Tests/Acceptance/SaloonConfrontationAcceptanceTests.cs:226,245` (2 calls)

**Pattern:** Same as Task 6 — inline the 4-step canonical flow. These tests require Docker/Testcontainers.

- [x] **Step 1: Migrate all Integration.Tests files**

For each file listed above, replace `GameSession.StartNew(...)` with the inlined 4-step canonical flow.

- [x] **Step 2: Check for assertion updates**

Same rules as Tasks 4 and 6.

- [x] **Step 3: Run Integration.Tests to verify (if Docker available)**

Run: `dotnet test tests/WildBunch.Integration.Tests/`
Expected: All tests pass. If Docker is not available, note this and skip — these will be verified in the final phase.

- [x] **Step 4: Commit**

```bash
git add tests/WildBunch.Integration.Tests/
git commit -m "refactor: migrate Integration.Tests to canonical start flow

Migrated 8 Integration.Tests files (20 call sites) from
GameSession.StartNew to the inlined 4-step canonical flow.
Updated assertions where needed."
```

---

## Phase 5: Delete StartNew and SeededNewGameFactory.Create

**Goal:** Delete `SeededNewGameFactory.Create` first (it calls `StartNew`), then delete `StartNew` itself. This ordering avoids a compile break — deleting `StartNew` first would leave `SeededNewGameFactory.Create` referencing a deleted method.

### Task 8: Delete SeededNewGameFactory.Create and remove from INewGameFactory

**Files:**
- Modify: `src/WildBunch.GameContent/Abstractions/INewGameFactory.cs` — remove `Create` method from interface
- Modify: `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs` — delete `Create` method
- Modify: `tests/WildBunch.Application.Tests/TestDoubles/StubNewGameFactory.cs` — delete `Create` method

**Context:** `INewGameFactory.Create` is not called by any production handler. Production uses `ResolveWorld` and `ResolveStartingResources`. The `Create` method was only used by tests, which now use the canonical flow directly. `SeededNewGameFactory.Create` calls `GameSession.StartNew` — deleting it first prevents a compile break when `StartNew` is deleted in Task 9.

- [x] **Step 1: Remove Create from INewGameFactory interface**

In `src/WildBunch.GameContent/Abstractions/INewGameFactory.cs`, delete the `Create` method declaration:
```csharp
GameSession Create(
    string playerName,
    GameDifficulty gameDifficulty = GameDifficulty.Standard,
    string? setupSeedCode = null,
    GameEntropy gameEntropy = GameEntropy.Classic,
    string? startingTownId = null);
```

Keep `ResolveWorld` and `ResolveStartingResources`.

- [x] **Step 2: Delete Create from SeededNewGameFactory**

In `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`, delete the `Create` method (lines 28-56).

- [x] **Step 3: Delete Create from StubNewGameFactory**

In `tests/WildBunch.Application.Tests/TestDoubles/StubNewGameFactory.cs`, delete the `Create` method and its tracking lists (`RequestedPlayerNames`, `RequestedGameDifficulties`, etc.) if they're only used by `Create`. Check for callers of these tracking lists first — if any test asserts on them, those assertions need updating.

- [x] **Step 4: Check for remaining INewGameFactory.Create callers**

Run:
```bash
rg "\.Create\(" tests/ src/ --type cs | rg -i "newgame|newGameFactory|seededNewGame"
```
Expected: No matches. If any remain (e.g. `SeededNewGameFactoryTests.cs`), migrate them to use `ResolveWorld` + canonical flow, or delete the test if it was specifically testing `Create`.

- [x] **Step 5: Build and run tests**

Run: `dotnet build && dotnet test tests/WildBunch.Domain.Tests/ tests/WildBunch.Application.Tests/`
Expected: Build succeeds, all tests pass.

- [x] **Step 6: Commit**

```bash
git add src/WildBunch.GameContent/Abstractions/INewGameFactory.cs src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs tests/WildBunch.Application.Tests/TestDoubles/StubNewGameFactory.cs
git commit -m "refactor: delete INewGameFactory.Create — dead in production

INewGameFactory.Create was not called by any production handler.
Production uses ResolveWorld + ResolveStartingResources with the
canonical start flow. All test callers have been migrated. Removes
Create from the interface, SeededNewGameFactory, and StubNewGameFactory."
```

### Task 9: Delete GameSession.StartNew

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` — delete both `StartNew` overloads

**Context:** All 65 test call sites have been migrated. `SeededNewGameFactory.Create` (the only production caller) was deleted in Task 8. `StartNew` is now dead code.

- [x] **Step 1: Verify no remaining StartNew calls exist in code**

Run:
```bash
rg "GameSession\.StartNew" src/ tests/ --type cs
```
Expected: No matches. All call sites should be migrated by Tasks 4-7. If any remain, migrate them before proceeding.

- [x] **Step 2: Delete both StartNew overloads from GameSession.cs**

In `src/WildBunch.Domain/Game/GameSession.cs`, delete both `StartNew` overloads and their XML doc comments. The methods to delete are:

```csharp
public static GameSession StartNew(string playerName, DomainWorld world, CaseFile caseFile, TownId? startingTownId = null)
    => StartNew(playerName, world, caseFile, startingTownId, wallet: null, inventory: null, gameDifficulty: GameDifficulty.Standard, seedCode: null);

public static GameSession StartNew(
    string playerName,
    DomainWorld world,
    CaseFile caseFile,
    TownId? startingTownId,
    WildBunch.Domain.Economy.Wallet? wallet,
    DomainInventory? inventory,
    GameDifficulty gameDifficulty = GameDifficulty.Standard,
    SaltSource? saltSource = null,
    GameEntropy gameEntropy = GameEntropy.Classic,
    string? seedCode = null)
{
    // ... full body ...
}
```

- [x] **Step 3: Build to verify no compile errors**

Run: `dotnet build src/WildBunch.Domain/`
Expected: Build succeeds (no code references `StartNew` anymore)

- [x] **Step 4: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSession.cs
git commit -m "refactor: delete GameSession.StartNew

All call sites have been migrated to the canonical start flow
(StartSetup → ViewPrologue → SelectStartingTown → CompleteGameStart).
StartNew was a convenience factory that emitted only GameStarted,
making sessions non-rehydratable from events alone. The canonical
flow emits 6 events and is fully event-sourced."
```

---

## Phase 6: Round-Trip Proof

**Goal:** Add the definitive round-trip proof that a session created through the canonical flow can be fully rehydrated from its event stream alone — including ALL 14 `CaseFile` fields (Suspects, TrueCulpritId, OpeningLead, KnownClues, PublicClues, Accusation, DiscoveredSuspectIds, KillerReleaseThreshold, KillerReleaseProgress, KnownWarrants, PublicWarrants, SuspectTurfAssignments, WantedSuspectConfrontations, SheriffTurnInSettlements), World, and all player state.

### Task 10: Add canonical flow round-trip proof test covering all CaseFile fields

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs` — add round-trip proof test

**Context:** Plan 1d's original round-trip proof only checked `PublicClues`. Plan 1d completed `CaseFileSnapshot` with all 14 fields. This test proves ALL fields survive round-trip. It also verifies that `CreateBaselineCaseFileFor` (fixed in Task 3) preserves non-investigation state during replay.

- [x] **Step 1: Write the round-trip proof test**

Add this test to `GameSessionEventSourcingTests.cs`:

```csharp
[Fact]
public void CanonicalStart_FullRoundTrip_Rehydrates_CompleteState_FromEvents()
{
    // Create a session with PublicClues through the canonical flow
    var session = TestSessionFactory.CreateWithPublicClue(
        InvestigationSourceKind.LocalGossip, "A dusty boot print.");

    // Perform an operation to prove post-start events also survive replay
    var resolver = new TownStoreCatalogResolver();
    var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
        .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
    session.Purchase(offer, 1);

    // Collect ALL events (6 setup + operation events)
    var events = session.UncommittedEvents.ToList();
    session.MarkEventsCommitted();

    // Rehydrate from events alone — no external world/caseFile references
    // beyond the world placeholder (which is overwritten by WorldGenerated)
    var placeholderWorld = CreateWorld(); // Overwritten by Apply(WorldGenerated)
    var rehydrated = GameSession.RehydrateFromEvents(
        session.Id,
        placeholderWorld,
        events);

    // Prove full state reconstruction
    Assert.Equal(session.Id, rehydrated.Id);
    Assert.Equal(session.Player.Name, rehydrated.Player.Name);
    Assert.Equal(session.Player.CurrentTownId, rehydrated.Player.CurrentTownId);
    Assert.Equal(session.Player.Health, rehydrated.Player.Health);
    Assert.Equal(session.Player.Wallet.Cash, rehydrated.Player.Wallet.Cash);
    Assert.Equal(session.GameDifficulty, rehydrated.GameDifficulty);
    Assert.Equal(session.GameEntropy, rehydrated.GameEntropy);
    Assert.Equal(session.SeedCode, rehydrated.SeedCode);
    Assert.Equal(session.Version, rehydrated.Version);
    Assert.Equal(StartFlowPhase.GameStarted, rehydrated.StartFlowPhase);

    // Prove CaseFile is reconstructed from CaseFileGenerated event (not external)
    Assert.Equal(session.CaseFile.Suspects.Count, rehydrated.CaseFile.Suspects.Count);
    Assert.Equal(session.CaseFile.TrueCulpritId, rehydrated.CaseFile.TrueCulpritId);
    Assert.Equal(session.CaseFile.OpeningLead, rehydrated.CaseFile.OpeningLead);

    // Prove PublicClues survive round-trip (the original data loss gap)
    Assert.Equal(session.CaseFile.PublicClues.Count, rehydrated.CaseFile.PublicClues.Count);
    Assert.Equal(session.CaseFile.PublicClues[0].Id, rehydrated.CaseFile.PublicClues[0].Id);
    Assert.Equal(session.CaseFile.PublicClues[0].Description, rehydrated.CaseFile.PublicClues[0].Description);

    // Prove KnownClues survive round-trip
    Assert.Equal(session.CaseFile.KnownClues.Count, rehydrated.CaseFile.KnownClues.Count);

    // Prove KillerReleaseGate survives round-trip
    Assert.Equal(session.CaseFile.KillerReleaseThreshold, rehydrated.CaseFile.KillerReleaseThreshold);
    Assert.Equal(session.CaseFile.KillerReleaseProgress, rehydrated.CaseFile.KillerReleaseProgress);

    // Prove DiscoveredSuspectIds survive round-trip
    Assert.Equal(session.CaseFile.DiscoveredSuspectIds.Count, rehydrated.CaseFile.DiscoveredSuspectIds.Count);

    // Prove SuspectTurfAssignments survive round-trip
    Assert.Equal(session.CaseFile.SuspectTurfAssignments.Count, rehydrated.CaseFile.SuspectTurfAssignments.Count);

    // Prove WantedSuspectConfrontations survive round-trip
    Assert.Equal(session.CaseFile.WantedSuspectConfrontations.Count, rehydrated.CaseFile.WantedSuspectConfrontations.Count);

    // Prove SheriffTurnInSettlements survive round-trip
    Assert.Equal(session.CaseFile.SheriffTurnInSettlements.Count, rehydrated.CaseFile.SheriffTurnInSettlements.Count);

    // Prove KnownWarrants survive round-trip
    Assert.Equal(session.CaseFile.KnownWarrants.Count, rehydrated.CaseFile.KnownWarrants.Count);

    // Prove PublicWarrants survive round-trip
    Assert.Equal(session.CaseFile.PublicWarrants.Count, rehydrated.CaseFile.PublicWarrants.Count);

    // Prove Accusation survives round-trip
    Assert.Equal(session.CaseFile.Accusation, rehydrated.CaseFile.Accusation);

    // Prove operation events survived replay
    Assert.Equal(session.Player.Inventory.GetQuantity(DomainItemKind.Food),
        rehydrated.Player.Inventory.GetQuantity(DomainItemKind.Food));

    Assert.Empty(rehydrated.UncommittedEvents);
}
```

- [x] **Step 2: Run the test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs --filter "CanonicalStart_FullRoundTrip"`
Expected: PASS — full state reconstructed from events alone, including all 14 CaseFile fields.

- [x] **Step 3: Commit**

```bash
git add tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs
git commit -m "test: add canonical flow round-trip proof covering all 14 CaseFile fields

Definitive proof that a session created through the canonical start
flow (StartSetup → ViewPrologue → SelectStartingTown → CompleteGameStart)
can be fully rehydrated from its event stream alone — including all 14
CaseFile fields (Suspects, TrueCulpritId, OpeningLead, KnownClues,
PublicClues, Accusation, DiscoveredSuspectIds, KillerReleaseThreshold,
KillerReleaseProgress, KnownWarrants, PublicWarrants, SuspectTurfAssignments,
WantedSuspectConfrontations, SheriffTurnInSettlements), World, player
state, and operation events. This closes the original data-loss gaps
fixed in Plan 1d and proves the fix is complete."
```

---

## Verification

- [x] **Final verification: No StartNew references remain anywhere**

Run:
```bash
rg "StartNew" src/ tests/ --type cs
```
Expected: No matches in any `.cs` file. (Plan/documentation `.md` files may still reference it historically — that's fine.)

- [x] **Final verification: No RecaptureGameStartedForReplay references remain**

Run:
```bash
rg "RecaptureGameStartedForReplay" tests/ --type cs
```
Expected: No matches. The legacy alias was deleted in Task 2.

- [x] **Final verification: No INewGameFactory.Create references remain**

Run:
```bash
rg "\.Create\(" tests/ src/ --type cs | rg -i "newgame|newGameFactory|seededNewGame"
```
Expected: No matches.

- [x] **Final verification: Full Domain.Tests suite passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/`
Expected: All tests pass.

- [x] **Final verification: Full Application.Tests suite passes**

Run: `dotnet test tests/WildBunch.Application.Tests/`
Expected: All tests pass.

- [x] **Final verification: Full Integration.Tests suite passes (if Docker available)**

Run: `dotnet test tests/WildBunch.Integration.Tests/`
Expected: All tests pass.

- [x] **Final verification: Full GameContent.Tests suite passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/`
Expected: All tests pass. (SeededNewGameFactoryTests may need updates if they tested `Create` — those should have been migrated in Task 8.)

- [x] **Final verification: Build the entire solution**

Run: `dotnet build`
Expected: Build succeeds with no errors or warnings related to `StartNew` or `INewGameFactory.Create`.
