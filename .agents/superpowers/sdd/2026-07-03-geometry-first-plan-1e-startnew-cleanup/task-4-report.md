# Task 4 Report: Migrate Domain.Tests files with single StartNew call sites

**Status:** DONE_WITH_CONCERNS  
**Commit:** `8d09605` — `refactor: migrate Domain.Tests single-call-site files to canonical start flow`  
**Date:** 2026-07-03

## Summary

Migrated 20 Domain.Tests files (25 call sites) from `GameSession.StartNew(...)` to `TestSessionFactory.StartGameCanonical(...)`. All 525 Domain.Tests pass. One pre-existing production bug in `Apply(GameStarted)` was discovered and fixed during migration.

## Files Migrated (20 files, 25 call sites)

### INLINE migration with replay-stream updates (2 files)

**ClockTurnCorrectionTests.cs** (`CreateDefaultSessionWithUncommittedGameStarted`):
- Changed `out GameStarted gameStarted` → `out IReadOnlyList<IDomainEvent> setupEvents`
- Replaced `GameSession.StartNew(...)` with `TestSessionFactory.StartGameCanonical(...)`
- Replaced `gameStarted = Assert.IsType<GameStarted>(session.UncommittedEvents.Single())` with `setupEvents = session.UncommittedEvents.ToList()`
- Updated replay stream: `new[] { gameStarted }.Concat(contextEvents)` → `setupEvents.Concat(contextEvents)`
- Updated comment: "capture the GameStarted event" → "capture the setup events"

**BountySaloonEventSourcingTests.cs** (`CreateConfrontableSaloonSessionWithUncommittedGameStarted`):
- Same pattern as ClockTurnCorrectionTests — `out GameStarted` → `out IReadOnlyList<IDomainEvent> setupEvents`
- Replaced `GameSession.StartNew(...)` with `TestSessionFactory.StartGameCanonical(...)`
- Updated replay stream: `new[] { gameStarted }.Concat(...)` → `setupEvents.Concat(...)`
- Updated comment

### Comment-only update (1 file)

**GameSessionArchiveTests.cs:210**:
- Replaced `GameSession.StartNew(...)` with `TestSessionFactory.StartGameCanonical(...)`, added `GameDifficulty.Standard` (no difficulty was specified in original)
- Updated L34 comment: "StartNew emits GameStarted; archive appends PlaythroughArchived." → "Canonical start flow emits setup events; archive appends PlaythroughArchived."
- Assertions (`OfType<PlaythroughArchived>().Single()` and version-delta) remain valid — no assertion changes needed

### Simple FACTORY_DELEGATE (17 files, 22 call sites)

All replaced `GameSession.StartNew(` with `TestSessionFactory.StartGameCanonical(`. Files where the original `StartNew` call did NOT specify `gameDifficulty` (relying on the legacy `Standard` default) had `GameDifficulty.Standard` passed explicitly to preserve behavior, since `StartGameCanonical` defaults to `GameDifficulty.Easy`.

**Files with `GameDifficulty.Standard` added (13 files, 16 call sites):**
- `PurchaseBeatCostTests.cs:75`
- `GameSessionPurchaseTests.cs:175`
- `GameSessionSheriffTurnInTests.cs:247`
- `GameSessionWantedSuspectPresenceTests.cs:82`
- `GameSessionWantedSuspectConfrontationTests.cs:313`
- `GameSessionWantedPostersTests.cs:216`
- `JournalResolverTests.cs:112` (also added `using WildBunch.Domain.Travel;`)
- `GameSessionBountyLoopCoordinatorTests.cs:73`
- `BountySettlementPolicyTests.cs:114`
- `BeatModelEconomyTests.cs:138`
- `GameSessionSaloonWantedSuspectLoopTests.cs:171,196` (2 calls)
- `GameSessionUnrelatedCriminalLedgerWiringTests.cs:171,233` (2 calls, also added `using WildBunch.Domain.Travel;`)
- `ActionAvailabilityResolverTests.cs:157,188` (2 calls)

**Files with `GameDifficulty.Easy` already specified (4 files, 8 call sites):**
- `TravelRulesProfileTests.cs:75`
- `GameSessionJourneyHistoryTests.cs:87`
- `TownActionAvailabilityTests.cs:48`
- `GameSessionResolverWiringTests.cs:131,188,244,304` (4 calls)

## Production Bug Fix

### Bug: `Apply(GameStarted)` does not update `_currentTown` to the starting town

**File:** `src/WildBunch.Domain/Game/GameSession.cs:1104`

**Root cause:** The canonical start flow (`StartSetup` → `ViewPrologue` → `SelectStartingTown` → `CompleteGameStart`) creates a placeholder player in `StartSetup` with `world.Towns.First().Id` as the current town. The `GameSession` constructor sets `_currentTown` (a readonly `TownAggregate`) from this placeholder player's current town. When `CompleteGameStart` emits `GameStarted` with the actual starting town, `Apply(GameStarted)` updates `Player.CurrentTownId` but does NOT update `_currentTown`. This means `CurrentTown` (and `CurrentTownSlotIndex`) remain stuck on the first town in the world, not the selected starting town.

**Why it was masked:** All already-migrated `TestSessionFactory` factory methods (`CreateDefault`, `CreateWithConfrontableSaloonSuspect`, etc.) use the first town in the world as the starting town, so `_currentTown` was correct by coincidence. The bug only manifests when the starting town differs from `world.Towns.First()`.

**Why `StartNew` didn't have this bug:** `StartNew` creates the placeholder player with `startingTown.Id` (the actual starting town), so the constructor sets `_currentTown` correctly.

**Why rehydration doesn't have this bug:** `RehydrateFromEvents` creates the placeholder player with `gameStarted.StartingTownId` (extracted from the event stream), so the constructor sets `_currentTown` correctly.

**Fix:** Added a check in `Apply(GameStarted)` to call `_currentTown.EnterTown(World.GetTown(e.StartingTownId))` when `_currentTown.TownId` doesn't match `e.StartingTownId`. This is a no-op during rehydration (where `_currentTown` is already correct) and only activates in the live command path when the starting town differs from the first town.

**Test that exposed the bug:** `GameSessionResolverWiringTests.ReadWantedPosters_FreshSessionsInDifferentStartingTownsSurfaceDifferentFirstWarrants` — creates two sessions with different starting towns (townA slot 0, townB slot 1) and asserts they surface different warrants. Without the fix, both sessions had `_currentTown` stuck on townA (slot 0), so both surfaced the same warrant.

## Test Results

- **Domain.Tests:** 525 passed, 0 failed, 0 skipped
- **Application.Tests:** 204 passed, 0 failed
- **GameContent.Tests:** 139 passed, 0 failed
- **Api.Tests:** 1 passed, 0 failed
- **Integration.Tests:** 141 failed, 27 passed — **all pre-existing** (verified by stashing changes and running: same 141 failures). Failures are PostgreSQL connection string issues and scenario seed catalog encoding issues, unrelated to this migration.

## Concerns

1. **Production bug fix scope:** The `Apply(GameStarted)` fix touches production code (`src/WildBunch.Domain/Game/GameSession.cs`), not just test files. This was necessary to make the migration correct — without it, the canonical start flow produces different `_currentTown` state than the rehydration path when the starting town isn't the first town, violating event sourcing principles. The fix is minimal (8 lines) and is a no-op in the rehydration path.

2. **`GameDifficulty` namespace:** `GameDifficulty` is in `WildBunch.Domain.Travel`, not `WildBunch.Domain.Game`. Two files (`JournalResolverTests.cs`, `GameSessionUnrelatedCriminalLedgerWiringTests.cs`) needed `using WildBunch.Domain.Travel;` added because they previously didn't reference `GameDifficulty` (relying on the `StartNew` default).

3. **Brief says "25 files" but lists 20:** The task brief header says "25 files, 1 call site each" but actually lists 16 single-call files + 4 multi-call files = 20 files total. All 20 listed files were migrated.
