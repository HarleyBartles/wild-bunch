# Task 1 Report: Fix SeedWorldMapLayout crash on derived town names

## What I Implemented

Replaced the hardcoded `TownCoordinates` dictionary in `SeedWorldMapLayout.cs` (keyed by town ID strings like "pinecross", "redmesa" — only 9 entries) with a deterministic slot-based coordinate generator. Slot 0 is placed at center (400, 450); remaining slots are arranged in a ring of radius 250 around the center, with the angle evenly distributed. This works for any town count (6-20) and any seed-derived town name from the 40-entry name pool.

The `GetMapTrails()` method was unchanged in logic (it already used the world's trails directly) but kept consistent with the spec's layout.

Updated 3 existing tests in `GetStartingTownMapHandlerTests.cs` that asserted on hardcoded town IDs and trail IDs no longer produced by the seed-derived canonical world:
- `ReturnsAllEightSeededTowns`: kept count=8 assertion; replaced hardcoded town ID checks with structural validity checks (non-empty id/name, distinct ids).
- `TrailEdgesCarryCorrectRideDayDistances`: updated to use slot-based trail IDs (`trail-0-1`, `trail-0-2`, etc.) with the canonical variant's distances.
- `TrailEdgesCoverAllNineSeededTrails` → `TrailEdgesCoverAllSeededTrails`: updated expected count from 9 to 12 (the canonical 8-town world has 12 slot-based trails).

Added the new test `GetMapTowns_DoesNotCrashWithDerivedTownNames` per the task spec.

## TDD Evidence

### RED (before implementation)
Command: `dotnet test --filter "FullyQualifiedName~GetStartingTownMapHandlerTests"`
Result: 6/6 FAILED with `KeyNotFoundException: The given key 'lostcanyon' was not present in the dictionary.`
The failure was expected because the seed-derived canonical world produces town names from the 40-entry name pool (e.g. "lostcanyon"), which are not in the hardcoded 9-entry `TownCoordinates` dictionary.

### GREEN (after implementation)
Command: `dotnet test --filter "FullyQualifiedName~GetStartingTownMapHandlerTests"`
Result: `Passed! - Failed: 0, Passed: 7, Skipped: 0, Total: 7, Duration: 52 ms`

## Files Changed
- `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs` — replaced hardcoded dictionary with slot-based coordinate generator
- `tests/WildBunch.Application.Tests/GetStartingTownMapHandlerTests.cs` — added crash-guard test, updated 3 tests to use slot-based trail IDs and canonical counts

## Test Results
- `GetStartingTownMapHandlerTests`: 7/7 passing, output pristine (no warnings in the test assembly output)
- Full `WildBunch.Application.Tests`: 177 passed, 4 failed. The 4 failures are **pre-existing** from the broader BUNCH-107 refactor (verified by reverting only my 2 files and confirming the same 4 tests still fail). They are in `GetStartingTownsHandlerTests`, `PurchaseStoreItemHandlerTests`, and `GetTownStoreOffersHandlerTests` — outside this task's scope.

## Self-Review Findings
- Implementation matches the task spec's suggested code exactly.
- Only the 2 in-scope files were modified and committed; the other 89 uncommitted files in the worktree belong to the broader BUNCH-107 refactor.
- The 4 pre-existing Application.Tests failures are noted as a concern (below) but are not caused by this change.

## Concerns
- 4 pre-existing test failures in `WildBunch.Application.Tests` (GetStartingTownsHandlerTests, PurchaseStoreItemHandlerTests, GetTownStoreOffersHandlerTests) exist in the worktree independent of this task. They are part of the broader BUNCH-107 refactor and will need to be addressed by their respective task slices.
