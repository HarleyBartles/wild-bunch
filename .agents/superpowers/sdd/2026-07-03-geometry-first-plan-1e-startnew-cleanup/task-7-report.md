# Task 7 Report: Migrate Integration.Tests files to canonical start flow

## Status: DONE_WITH_CONCERNS

## Summary

Migrated 8 Integration.Tests files (22 call sites) from `GameSession.StartNew(...)` to the inlined 4-step canonical flow (`StartSetup` -> `ViewPrologue` -> `SelectStartingTown` -> `CompleteGameStart`). Integration.Tests does NOT reference Domain.Tests, so the canonical flow was inlined directly (same approach as Task 6 for Application.Tests). Build succeeds with 0 errors. Domain.Tests (525 passed) and Application.Tests (204 passed) show no regressions. Integration.Tests could not be run (Docker/Testcontainers unavailable).

## Commit

- **SHA:** `9a0cdca`
- **Subject:** `refactor: migrate Integration.Tests to canonical start flow`
- **Files changed:** 8 files, 212 insertions(+), 115 deletions(-)

## Test Results

- **Build:** 0 errors, 0 warnings
- **Domain.Tests:** 525 passed, 0 failed, 0 skipped (269 ms)
- **Application.Tests:** 204 passed, 0 failed, 0 skipped (307 ms)
- **Integration.Tests:** SKIPPED — Docker/Testcontainers not available

## Critical Finding: Discovery Report Event Count Was Wrong

The discovery report stated the canonical flow produces **4** start events. In reality, `StartSetup` emits **3** events (`PlayerSetupCompleted`, `WorldGenerated`, `CaseFileGenerated`), plus `ViewPrologue` (1), `SelectStartingTown` (1), and `CompleteGameStart` (1) = **6 start events total**. All assertion updates in the 2 INLINE files were calculated based on the actual count of 6, not the discovery report's incorrect count of 4.

The 6 start events in order:
1. `PlayerSetupCompleted`
2. `WorldGenerated`
3. `CaseFileGenerated`
4. `PrologueViewed`
5. `StartingTownSelected`
6. `GameStarted`

## What Was Done

### 1. INLINE migration with assertion updates (2 files)

**EventSourcingEndToEndTests.cs** (1 call site in `CreateSession`):
- Inlined 4-step canonical flow with `GameDifficulty.Standard`, `GameEntropy.Classic`, `"test-seed"`, `SaltSource.CreateFixed("test")`
- Updated assertions in `FullFlow_CreateStoreReloadCommandStoreReplayProject`:
  - `Assert.Single(session.UncommittedEvents)` → `Assert.Equal(6, ...)` 
  - `Assert.IsType<GameStarted>(session.UncommittedEvents[0])` → `Assert.IsType<PlayerSetupCompleted>(...)`
  - Version expectations: 1→6 (after start), 3→8 (after purchase)
  - Event stream count: 3→8, event type/index assertions shifted (action events at indices 6,7)
  - Audit entries: 3→8, first entry type `GameStarted`→`PlayerSetupCompleted`, action entries at indices 6,7

**EventStorePersistenceTests.cs** (2 call sites in `CreateSession` and `CreateSessionWithWarrantedSaloonSuspect`):
- Inlined 4-step canonical flow in both helpers
- `CreateSession`: `GameDifficulty.Standard`, `SaltSource.CreateFixed("test")`
- `CreateSessionWithWarrantedSaloonSuspect`: `GameDifficulty.Easy`, `SaltSource.CreateFixed(string.Empty)` (preserved from original)
- Updated assertions across 7 tests:
  - `StoreAsync_AppendsEventsToStoredEventsTable`: 1→6 stored events, first type `GameStarted`→`PlayerSetupCompleted`
  - `StoreAsync_PurchaseAppendsStoreItemPurchasedEvent`: 3→8 stored events, action events at indices 6,7, sequence 8
  - `GetEventStreamAsync_ReturnsTypedEventsInOrder`: 3→8 events, action events at indices 6,7
  - `GetEventStreamAsync_FromVersion`: `fromVersion: 1` → `fromVersion: 6`
  - `GetEventStreamAsync_ReturnsInvestigationPerformed`: 3→8 stored/stream events, action events at indices 6,7
  - `GetEventStreamAsync_ReturnsBountySaloonEvents`: 3→8 events, action events at indices 6,7
  - `CommitAsync_TranslatesUniqueIndexViolation`: version 1→6, sequence 2→7
  - `GetByIdAsync_WithLaggingSnapshot_LoadsAggregateWithVersionEqualToStreamVersion`: SnapshotVersion 1→6, StreamVersion 3→8, Version 3→8
  - `GetByIdAsync_WithLaggingSnapshot_DoesNotDuplicateAggregateLogEntries`: SnapshotVersion 1→6, StreamVersion 3→8; log entry count stays 2 (only `GameStarted` produces an Opening entry; other start events produce no log entries)

### 2. FACTORY_DELEGATE migration (6 files, 19 call sites)

All replaced `GameSession.StartNew(...)` with the inlined 4-step canonical flow. No assertion changes needed.

**Import additions:**
- `UnrelatedCriminalLedgerPersistenceTests.cs` — added `using WildBunch.Domain.Travel;` (needed `GameDifficulty`/`GameEntropy`)

**Default parameter handling:**
- **gameDifficulty:** `GameDifficulty.Standard` passed explicitly where `StartNew` defaulted to Standard (12 call sites had no explicit difficulty)
- **gameEntropy:** `GameEntropy.Classic` passed explicitly where `StartNew` defaulted (all call sites except `CreateSessionWithSeedCode` which passed a custom entropy parameter)
- **seedCode:** `"test-seed"` used as default where `StartNew` defaulted to null (all call sites except `CreateSessionWithSeedCode` which passed a real seed code parameter)
- **saltSource:** `SaltSource.CreateFixed("test")` used as default where `StartNew` defaulted to `CreateRuntime()`; existing explicit salt sources (e.g., `DeterministicSaltSource = SaltSource.CreateFixed(string.Empty)`) were preserved

### 3. Minor comment update (1 file)

**EfGameSessionRepositoryTests.cs:70** — Updated comment from "Seed code is restored from the GameStarted event via event replay" to "Seed code is restored from the start flow events via event replay". The `SeedCode` assertion itself remains valid.

## Files Migrated (8 files, 22 call sites)

| File | Call Sites | Strategy | Notes |
|------|-----------|----------|-------|
| EventSourcingEndToEndTests.cs | 1 | INLINE | +assertion updates (6 start events) |
| EventStorePersistenceTests.cs | 2 | INLINE | +assertion updates across 7 tests |
| UnrelatedCriminalLedgerPersistenceTests.cs | 1 | FACTORY_DELEGATE | +import |
| PostgreSqlPersistenceTests.cs | 1 | FACTORY_DELEGATE | DeterministicSaltSource preserved |
| MigrationTests.cs | 1 | FACTORY_DELEGATE | |
| GameSessionDifficultyPersistenceTests.cs | 3 | FACTORY_DELEGATE | |
| EfGameSessionRepositoryTests.cs | 9 | FACTORY_DELEGATE | +minor comment update |
| Acceptance/SaloonConfrontationAcceptanceTests.cs | 2 | FACTORY_DELEGATE | |

## Concerns

1. **Integration.Tests could not be run.** Docker/Testcontainers is not available on this machine. The 2 INLINE files (`EventSourcingEndToEndTests`, `EventStorePersistenceTests`) are the highest risk — their assertion updates were calculated by static analysis (6 start events instead of 1) and must be verified with Docker before Plan 1e closes.

2. **Discovery report event count was wrong.** The discovery report stated the canonical flow produces 4 start events, but it actually produces 6 (StartSetup emits 3 events: PlayerSetupCompleted, WorldGenerated, CaseFileGenerated; plus PrologueViewed, StartingTownSelected, GameStarted). All assertion updates were based on the correct count of 6. This discrepancy should be noted for any future tasks that reference the discovery report.

3. **Salt source default changed from `CreateRuntime()` to `CreateFixed("test")`** for call sites that didn't specify a salt source. This makes tests more deterministic but is a behavior change. Call sites that explicitly specified `DeterministicSaltSource` (SaltSource.CreateFixed(string.Empty)) preserved that value.

4. **Seed code default changed from null to `"test-seed"`** for call sites that didn't specify one. The canonical flow's `StartSetup` requires a non-null seed code. The `CreateSessionWithSeedCode` helper in EfGameSessionRepositoryTests preserved its real seed code parameter.
