# Tracked Items

Open items that are documented in the codebase (skipped tests, comments) but not yet assigned to a plan or fixed.

## 1. TownStates parity gap (non-first-town starts)

**Status:** Accepted-for-now, assigned to a future plan (not Plan 1f, not Plan 2).

**Symptom:** When the starting town differs from `world.Towns.First()`, the live session has a phantom `TownVisitState` entry for the first town that does not appear after rehydration from events.

**Location:** Skipped test at `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs:484` -- `NonFirstStartingTown_TownStates_Parity_Between_Live_And_Rehydrated`.

**Root cause:** `StartSetup` creates a `TownVisitState` entry for the placeholder town (the first town in the world) before `Apply(GameStarted)` updates `_currentTown` to the actual starting town. On rehydration, `Apply(GameStarted)` sets `_currentTown` directly without creating the phantom entry.

**Fix direction:** Either (a) do not create a `TownVisitState` entry for the placeholder town in `StartSetup`, or (b) clear the phantom entry when `Apply(GameStarted)` updates `_currentTown`. This is a production behavior change and requires its own plan with tests.

**Why it matters:** Event-sourcing correctness. The live session and the rehydrated session must have identical state. A phantom `TownStates` entry means the live session's town-visit history disagrees with its event stream, which could cause projection drift or incorrect visit-number assertions.

**History:** Discovered during Plan 1e Task 4 (Domain.Tests migration). The `Apply(GameStarted)` `_currentTown` fix in Plan 1e improved the situation (the rehydrated session now correctly sets `_currentTown`) but did not address the phantom `TownStates` entry.
