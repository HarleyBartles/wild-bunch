# Geometry-First Map Generation - Plan 2: Wire & Integration

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Wire `MapGenerator.Generate` into the game-setup pipeline, delete the stub `SeedWorldBuilder.CreateWorld`, add an integration-level smoke test through the full pipeline, and clean up unused helpers.

**Architecture:** Three tasks. Task 1 swaps the orchestrator call, fixes the one test that calls the stub directly, and deletes the stub. Task 2 creates a minimal integration smoke test through `SeededNewGameFactory` (the existing `MapGeneratorTests.cs` already covers unit-level pipeline assertions; the existing Application.Tests already assert `NotEmpty` trails through the full pipeline). Task 3 deletes unused `ComputeStableHash` overloads and runs final verification.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3

## Prerequisites

- Plan 0 (Clean Slate) must be complete.
- Plans 1a-1e must be complete -- `MapGenerator.Generate` exists, `StartNew` is deleted, canonical start flow is in place, `CaseFileSnapshot` carries all 14 fields.
- Plan 1f (Clean Handoff) must be complete -- ADRs and decomposition audit are fresh, tracked-items doc exists.

## Verified codebase state (Plan 1f head)

- `GameSetupResolver.cs:55` calls `SeedWorldBuilder.CreateWorld(seedWorld, source, entropy.GameEntropy, mysteryTruth.SaltSource)` -- this is the single production call site to swap.
- `SeedWorldBuilder.CreateWorld` signature: `(SeedWorld, GameSetupDeterministicSource, GameEntropy = Boring, SaltSource? = null)` -- has defaults for last 2 params.
- `MapGenerator.Generate` signature: `(SeedWorld, GameSetupDeterministicSource, GameEntropy, SaltSource?)` -- NO defaults for last 2 params. All 4 args must be passed explicitly.
- `GameSetupResolverTests.cs:71` calls `SeedWorldBuilder.CreateWorld(seedWorld, new GameSetupDeterministicSource(seedWorld.SeedCodeText), GameEntropy.Classic)` directly -- passes 3 args, relies on `saltSource = null` default. This is the ONE test that will break when `CreateWorld` is deleted. Fix: replace with `MapGenerator.Generate(seedWorld, new GameSetupDeterministicSource(seedWorld.SeedCodeText), GameEntropy.Classic, null)`.
- `MapGeneratorTests.cs` already exists with 10 tests covering: Boring determinism, non-Boring determinism, connected graph, planar graph, 2-6 day distance range, outlier 6-day single incident trail, Boring no outlier, town count placement, cluster count assignment. These are unit-level tests calling `MapGenerator.Generate` directly.
- `GetStartingTownMapHandlerTests.cs` already asserts `Assert.NotEmpty` on trails, positive ride-day distances, and town connectivity -- goes through `SeededNewGameFactory.ResolveWorld` -> `GameSetupResolver` -> (currently stub) `CreateWorld`. Will automatically exercise the real pipeline after Task 1.
- `GetWorldMapHandlerTests.cs` already asserts `Assert.NotEmpty(result.Trails)`.
- `StartingTownMapEndpointTests.cs` already asserts `Assert.NotEmpty(map.Trails)` and positive ride-day distances. (Integration test -- requires Docker/Testcontainers, cannot verify locally.)
- `SeedWorldResolverTests.cs:179` already asserts `Assert.Equal(8, resolved.TownCount)` and `Assert.Equal(8, resolved.SelectedTownIds.Count)`.
- `SeedWorldBuilderTests.cs` tests `NonNegativeModulo`, `CreateCanonicalWorld` (prosperity/telegraph), `StartingTownPolicy`, `TownCount` -- none call `CreateWorld`, none need changes.
- Zero `Assert.Empty` trail assertions exist in `tests/` (verified by grep).
- `SeedWorldBuilder.ComputeStableHash` has 3 private overloads (lines 62, 74, 86) with no call sites -- unused, to be deleted in Task 3.
- `MapLayoutPalette` enum already deleted in Plan 0. Zero code references; only historical doc comments in `SeedWorldResolver.cs`.

## Tasks

### Task 1: Wire MapGenerator Into GameSetupResolver

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs`
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`
- Modify: `tests/WildBunch.GameContent.Tests/GameSetupResolverTests.cs`

**Changes:**

- [x] **Step 1: Swap the call in GameSetupResolver.cs**

In `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs`, line 55, replace:
```csharp
var world = SeedWorldBuilder.CreateWorld(seedWorld, source, entropy.GameEntropy, mysteryTruth.SaltSource);
```
with:
```csharp
var world = MapGenerator.Generate(seedWorld, source, entropy.GameEntropy, mysteryTruth.SaltSource);
```

- [x] **Step 2: Fix GameSetupResolverTests.cs:71**

In `tests/WildBunch.GameContent.Tests/GameSetupResolverTests.cs`, line 71, replace:
```csharp
var expectedStartingTown = SeedWorldBuilder.CreateWorld(seedWorld, new GameSetupDeterministicSource(seedWorld.SeedCodeText), GameEntropy.Classic).Towns.First().Id;
```
with:
```csharp
var expectedStartingTown = MapGenerator.Generate(seedWorld, new GameSetupDeterministicSource(seedWorld.SeedCodeText), GameEntropy.Classic, null).Towns.First().Id;
```

Note: `MapGenerator.Generate` has no default for `saltSource` (unlike `CreateWorld` which defaulted it to null), so `null` must be passed explicitly as the 4th arg.

- [x] **Step 3: Delete the stub CreateWorld method in SeedWorldBuilder.cs**

In `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`, delete the `CreateWorld` method (lines 18-56, including the XML doc comment starting at line 18). Keep all other members: `CreateCanonicalWorld`, `NonNegativeModulo`, `ComputeStableHash` overloads (Task 3 deletes the unused ones), `IsCanonicalSeedWorld`.

- [x] **Step 4: Build and run affected tests**

```bash
dotnet build src/WildBunch.GameContent/WildBunch.GameContent.csproj
dotnet test tests/WildBunch.GameContent.Tests/ --filter "GameSetupResolverTests|SeededNewGameFactoryTests|SeedWorldBuilderTests|MapGeneratorTests"
```
Expected: PASS -- all tests pass with `MapGenerator.Generate` wired in. The `MapGeneratorTests` already call `MapGenerator.Generate` directly so they are unaffected. The `GameSetupResolverTests` and `SeededNewGameFactoryTests` now exercise the real pipeline through `GameSetupResolver`.

- [x] **Step 5: Run Application.Tests (they exercise the full pipeline)**

```bash
dotnet test tests/WildBunch.Application.Tests/ --filter "GetStartingTownMapHandlerTests|GetWorldMapHandlerTests"
```
Expected: PASS -- these tests already assert `Assert.NotEmpty` on trails and positive ride-day distances. They now exercise the real pipeline automatically.

- [x] **Step 6: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/GameSetupResolver.cs src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs tests/WildBunch.GameContent.Tests/GameSetupResolverTests.cs
git commit -m "feat: wire MapGenerator into GameSetupResolver, delete stub CreateWorld"
```

### Task 2: Add integration-level pipeline smoke test

**Context:** `MapGeneratorTests.cs` already has 10 unit-level tests covering pipeline output (determinism, connectivity, planarity, distance range, outlier, town count, cluster count). The existing Application.Tests (`GetStartingTownMapHandlerTests`, `GetWorldMapHandlerTests`) already assert `NotEmpty` trails through the full `SeededNewGameFactory` path. The only missing coverage is a GameContent.Tests-level integration test that verifies the full `SeededNewGameFactory` -> `GameSetupResolver` -> `MapGenerator.Generate` path produces a world with real (non-stub) trails.

**Files:**
- Create: `tests/WildBunch.GameContent.Tests/GeometryPipelineTests.cs`

**No other test files need changes.** The Application.Tests and Integration.Tests already assert `NotEmpty` trails and will automatically exercise the real pipeline after Task 1. `SeedWorldBuilderTests` and `SeedWorldResolverTests` don't call `CreateWorld` and don't need changes.

- [x] **Step 1: Create GeometryPipelineTests.cs**

Create `tests/WildBunch.GameContent.Tests/GeometryPipelineTests.cs` with 2 integration-level tests that go through the full `SeededNewGameFactory` path (not calling `MapGenerator.Generate` directly):

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class GeometryPipelineTests
{
    private static GameSession CreateSessionThroughFullPipeline(GameEntropy entropy = GameEntropy.Boring)
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var difficulty = DifficultyEnvelope.For(GameDifficulty.Standard);
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());
        var (world, caseFile, seedCodeText, saltSource) = factory.ResolveWorld(
            "Test Player", difficulty.Difficulty, seedWorld.SeedCode.ToString("D"), entropy);
        var session = GameSession.StartSetup(
            "Test Player", world, caseFile, difficulty.Difficulty, entropy, seedCodeText, saltSource);
        return session;
    }

    [Fact]
    public void FullPipeline_ProducesWorldWithRealTrailsAndCoordinates()
    {
        var session = CreateSessionThroughFullPipeline();

        // 8 towns from canonical seed
        Assert.Equal(8, session.World.Towns.Count);

        // All towns have positive coordinates (clustered placement, not placeholder zeros)
        Assert.All(session.World.Towns, town =>
        {
            Assert.True(town.MapX > 0, $"Town {town.Name} has non-positive MapX: {town.MapX}");
            Assert.True(town.MapY > 0, $"Town {town.Name} has non-positive MapY: {town.MapY}");
        });

        // Non-empty trails (real MST graph, not stub linear chain)
        Assert.NotEmpty(session.World.Trails);

        // All trails have ride-day distances in 2-6 day range
        Assert.All(session.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));

        // All trail endpoints reference towns in the world
        var townIds = session.World.Towns.Select(t => t.Id).ToHashSet();
        Assert.All(session.World.Trails, trail =>
        {
            Assert.Contains(trail.FromTownId, townIds);
            Assert.Contains(trail.ToTownId, townIds);
        });
    }

    [Fact]
    public void FullPipeline_BoringMode_SameSeedProducesSameWorld()
    {
        var sessionA = CreateSessionThroughFullPipeline(GameEntropy.Boring);
        var sessionB = CreateSessionThroughFullPipeline(GameEntropy.Boring);

        var townsA = sessionA.World.Towns.ToArray();
        var townsB = sessionB.World.Towns.ToArray();

        Assert.Equal(townsA.Length, townsB.Length);
        for (var i = 0; i < townsA.Length; i++)
        {
            Assert.Equal(townsA[i].MapX, townsB[i].MapX);
            Assert.Equal(townsA[i].MapY, townsB[i].MapY);
        }
        Assert.Equal(sessionA.World.Trails.Count, sessionB.World.Trails.Count);
    }

    private sealed class TestFixedSaltSourceFactory : ISaltSourceFactory
    {
        public SaltSource Create(string? setupSeedCode, GameDifficulty gameDifficulty)
            => SaltSource.CreateFixed("test-fixed-salt");
    }
}
```

Note: The `CreateSessionThroughFullPipeline` helper follows the same pattern as `GetStartingTownMapHandlerTests.CreateTestSession` and `GetWorldMapHandlerTests.CreateTestSession` -- it goes through `SeededNewGameFactory.ResolveWorld` -> `GameSetupResolver` -> `MapGenerator.Generate`. The `TestFixedSaltSourceFactory` is the same pattern used in those test files.

- [x] **Step 2: Build and run the new tests**

```bash
dotnet build tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj
dotnet test tests/WildBunch.GameContent.Tests/ --filter "GeometryPipelineTests"
```
Expected: PASS -- both tests pass.

- [x] **Step 3: Run the full GameContent.Tests suite to verify no regressions**

```bash
dotnet test tests/WildBunch.GameContent.Tests/
```
Expected: PASS -- 141+2 = 143 passed (139 existing + 2 new + 2 from MapGeneratorTests if not already counted), 0 failed.

- [x] **Step 4: Commit**

```bash
git add tests/WildBunch.GameContent.Tests/GeometryPipelineTests.cs
git commit -m "test: add integration-level pipeline smoke test through SeededNewGameFactory"
```

### Task 3: Final Verification and Cleanup

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs` (delete unused `ComputeStableHash` overloads)

- [x] **Step 1: Confirm MapLayoutPalette is gone (confirmation only)**

```bash
rg -n "MapLayoutPalette" src/ tests/
```
Expected: zero matches in code. Historical doc comments in `SeedWorldResolver.cs` (lines 55-75) describing the v8-v16 codec evolution are expected and should NOT be changed.

- [x] **Step 2: Delete unused ComputeStableHash overloads**

In `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`, delete the 3 private `ComputeStableHash` overloads at lines 62, 74, and 86. These have no call sites anywhere in `src/` or `tests/` (verified by grep). Keep all other members.

Verify no call sites before deleting:
```bash
rg -n "ComputeStableHash" src/ tests/
```
Expected: only the 3 definitions in `SeedWorldBuilder.cs`, zero call sites.

- [x] **Step 3: Build the full solution**

```bash
dotnet build
```
Expected: 0 errors, 0 warnings.

- [x] **Step 4: Run Domain.Tests**

```bash
dotnet test tests/WildBunch.Domain.Tests/
```
Expected: 526 passed, 0 failed, 1 skipped (TownStates parity-gap test).

- [x] **Step 5: Run Application.Tests**

```bash
dotnet test tests/WildBunch.Application.Tests/
```
Expected: 204 passed, 0 failed.

- [x] **Step 6: Run GameContent.Tests**

```bash
dotnet test tests/WildBunch.GameContent.Tests/
```
Expected: 143 passed, 0 failed (139 existing + 2 new GeometryPipelineTests + 2 existing MapGeneratorTests... verify exact count at execution time).

- [x] **Step 7: Verify no stale references remain**

```bash
rg -n "SeedWorldBuilder\.CreateWorld" src/ tests/
```
Expected: zero matches (the stub is deleted, all call sites use `MapGenerator.Generate`).

- [x] **Step 8: Commit (if any cleanup changes were made)**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs
git commit -m "chore: delete unused ComputeStableHash overloads"
```

## Definition of Done

- [x] Prerequisites confirmed: Plans 0-1f complete, `MapGenerator.Generate` exists, `SeedWorldBuilder.CreateWorld` stub exists
- [x] `GameSetupResolver` calls `MapGenerator.Generate` instead of `SeedWorldBuilder.CreateWorld`
- [x] `GameSetupResolverTests.cs:71` uses `MapGenerator.Generate` (not the deleted stub)
- [x] `SeedWorldBuilder.CreateWorld` stub is deleted; kept members remain
- [x] `GeometryPipelineTests.cs` exists with 2 integration-level tests through the full pipeline
- [x] `MapLayoutPalette` references confirmed absent (already deleted in Plan 0)
- [x] Unused `ComputeStableHash` overloads deleted
- [x] Full solution builds with 0 errors, 0 warnings
- [x] Domain.Tests (526+1skip), Application.Tests (204), GameContent.Tests (143) all pass
- [x] Zero `SeedWorldBuilder.CreateWorld` references remain in `src/` or `tests/`
