# Nullable Player.CurrentTownId Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate the placeholder `Player.CurrentTownId` by making it nullable (`TownId?`), set to `null` during setup phase, and set to the real starting town at `Apply(GameStarted)`.

**Architecture:** The `Player` aggregate's `CurrentTownId` becomes `TownId?`. During setup phase (before `GameStarted`), it is `null`. All domain commands, application handlers, persistence snapshots, and DTOs that touch `CurrentTownId` are updated to handle null. Gameplay command handlers add setup-phase guards that return `Failed` results (following the existing `IsArchived`/`IsJourneyModal` pattern). Test code that accesses `CurrentTownId` on started-game sessions adds `.Value` or `!` since the value is guaranteed non-null in those contexts.

**Tech Stack:** C# 14, .NET 10, xUnit, PostgreSQL (Testcontainers)

## Global Constraints

- The domain owns rules and invariants (DDD aggregate root pattern)
- Commands return `Failed` results for invariant violations, not exceptions (existing pattern)
- `GameSession` is the live-play Aggregate Root
- Event-sourced flows: commands produce typed domain events, apply them through `Apply`
- No new dependencies

---

## Current State

The src/ changes (Tasks 1-2) are **already complete** — a subagent made them before being canceled. The build fails only on test files (135 errors, all `CS1503: cannot convert from 'TownId?' to 'TownId'`). The remaining work is fixing test compilation errors.

### Task 1: Domain + Application + Persistence src/ changes (DONE)

**Status:** Complete. 16 src/ files modified:

- `Player.cs`: `CurrentTownId` changed from `TownId` to `TownId?`, constructor parameter changed to `TownId?`
- `GameSession.cs`: `StartSetup` passes `null`, `Apply(PlayerSetupCompleted)` passes `null`, constructor guards null, `ArchivePlaythrough` null-safe
- `JournalSnapshot.cs`: `CurrentTownId` and `CurrentTownName` changed to nullable
- `JournalResolver.cs`: Handles null `CurrentTownId`
- `ActionAvailabilityResolver.cs`: Added setup-phase guard, uses `.Value`
- `GameDtos.cs`: `PlayerDto.CurrentTownId` changed to `string?`
- `GameSessionMapper.cs`: Uses `?.Value`, read-model saloon POI guarded
- `JournalMapper.cs`: Handles null `CurrentTownId`/`CurrentTownName`
- `JournalDto.cs`: `CurrentTown` changed to `JournalTownDto?`
- `GameSessionReadModel.cs`: `TownVisitState` changed to `TownVisitState?`
- `PreviewTravelHandler.cs`: Added setup-phase guard, uses `.Value`
- `TravelToTownHandler.cs`: Added setup-phase guard, uses `.Value`
- `PurchaseStoreItemHandler.cs`: Added setup-phase guard, uses `.Value`
- `ArchivePlaythroughHandler.cs`: Null-safe town fields
- `GameSessionReadStoreLoader.cs`: Handles null `CurrentTownId` in both methods
- `GameSessionJsonSerializer.Components.cs`: `PlayerSnapshot.CurrentTownId` nullable, handles null

### Task 2: Fix test compilation errors

**Files:**
- Modify: ~38 test files across `tests/WildBunch.Domain.Tests/`, `tests/WildBunch.Application.Tests/`, `tests/WildBunch.Integration.Tests/`, `tests/WildBunch.GameContent.Tests/`

**Interfaces:**
- Consumes: `Player.CurrentTownId` is now `TownId?` (nullable)
- Produces: All test code compiles and passes

**Pattern:** Every test error is `CS1503: cannot convert from 'TownId?' to 'TownId'`. The fix is to add `.Value` or `!` to `Player.CurrentTownId` accesses in test code. All test accesses are on started-game sessions where `CurrentTownId` is guaranteed non-null.

- [ ] **Step 1: Fix all test compilation errors**

The errors fall into these categories:
1. `session.Player.CurrentTownId` passed to a method expecting `TownId` → add `.Value`
2. `session.Player.CurrentTownId` compared to a `TownId` → add `.Value`
3. `player.CurrentTownId` passed to `new TownVisitState(...)` → add `.Value`
4. `player.CurrentTownId` passed to `World.GetTown(...)` → add `.Value`
5. `snapshot.CurrentTownId` (from `JournalSnapshot`) passed where `TownId` expected → add `.Value`

Run: `dotnet build` — fix every `CS1503` and `CS0266` error by adding `.Value` to the `CurrentTownId` access.

- [ ] **Step 2: Build all projects**

Run: `dotnet build`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: Run Domain tests**

Run: `dotnet test tests/WildBunch.Domain.Tests/ --no-build`
Expected: 527 passed, 0 failed

- [ ] **Step 4: Run Application tests**

Run: `dotnet test tests/WildBunch.Application.Tests/ --no-build`
Expected: 204 passed, 0 failed

- [ ] **Step 5: Run GameContent tests**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --no-build`
Expected: 138 passed, 0 failed

- [ ] **Step 6: Run Integration tests**

Run: `.\scripts\postgres-dev.ps1 test -- tests/WildBunch.Integration.Tests --no-build`
Expected: 170 passed, 0 failed

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "fix: make Player.CurrentTownId nullable to eliminate placeholder town

Player.CurrentTownId is now TownId? — null during setup phase, set to the
real starting town at Apply(GameStarted). This eliminates the placeholder
world.Towns.First().Id that was being set during StartSetup and replaced
at Apply(GameStarted).

All gameplay command handlers add setup-phase guards returning Failed
results, following the existing IsArchived/IsJourneyModal pattern.

ArchivePlaythrough is now null-safe for setup-phase sessions — a player
can archive and start over at any point after the event stream begins.

Dev mappers return null town fields for setup-phase sessions.

Generated with [Devin](https://devin.ai)

Co-Authored-By: Devin <158243242+devin-ai-integration[bot]@users.noreply.github.com>"
```
