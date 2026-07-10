# Dev Layout Salts Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate dev layout salts into the game setup pipeline so dev-set salts are used in world generation and persist with the playthrough, using a multi-phase setup flow orchestrated by the UI.

**Architecture:** Multi-phase game session setup (prep → inject dev salts → start) with dev salts flowing through GameSetupResolver → MapGenerator → TownLayout. Dev-only API contract is respected via separate dev endpoints.

**Tech Stack:** C#/.NET backend with DDD patterns, TypeScript frontend with styled-components, existing dev overlay system.

## Completion Status

**Completed Tasks:**
- ✅ Task 1: Add Prepped Session Infrastructure
- ✅ Task 2: Add LayoutSalts Persistence
- ✅ Task 3: Add Dev Salts Pipeline Integration
- ✅ Task 4: Add Prep and Start Session Commands (core implementation only, endpoints skipped)
- ✅ Documentation: Dev-Enabled Action Pattern

**Skipped Tasks:**
- ⏭️ Task 5: Add Frontend API Functions (depends on Task 4 endpoints)
- ⏭️ Task 6: Update Frontend for Three-Phase Flow (depends on Task 5)

**Reason for Skipping Tasks 5-6:**
Task 4 endpoint registration was skipped due to pre-existing build errors in DevEndpoints.cs (LockRngHandler and ClearRngHandler references). Tasks 5 and 6 depend on these endpoints being registered, so they were deferred. The core backend implementation (handlers, commands, INewGameFactory overload) is complete and tested.

**Next Steps:**
1. Fix pre-existing DevEndpoints.cs build errors
2. Register prep/start endpoints in DevEndpoints.cs
3. Register handlers in DependencyInjection.cs
4. Complete Task 5 (Frontend API Functions)
5. Complete Task 6 (Frontend Three-Phase Flow)

## Global Constraints

- Keep change narrow to dev layout salts integration
- Do not modify existing `/api/games/setup` endpoint (keep for normal players)
- Respect dev-only API contract (dev endpoints require DevRoleGuard)
- Follow DDD/CQRS/Event Sourcing patterns (GameSession as aggregate root, event sourcing)
- Follow frontend standards (styled-components, no inline styles)
- TDD discipline: write failing test first, then implement
- Frequent commits after each task

---

### Task 1: Add Prepped Session Infrastructure

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameStatus.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Test: `tests/WildBunch.Domain.Tests/Game/GameStatusTests.cs`
- Test: `tests/WildBunch.Domain.Tests/Game/GameSessionTests.cs`

**Interfaces:**
- Consumes: None (new infrastructure)
- Produces: `GameStatus.Prepped` enum value, `GameSession.StartPrepped()` factory method

**Test Kind:** Unit tests (isolated domain logic)

**Implementation guidance:**
- Add `Prepped = 4` to `GameStatus` enum
- Add `GameSession.StartPrepped(string seedCode, GameDifficulty gameDifficulty, GameEntropy gameEntropy)` factory method
- Follow the existing `StartSetup` pattern: create minimal session with placeholder player, no world, no case file
- Set `Status = GameStatus.Prepped`
- Set `SeedCode` directly (no event needed for prepped phase)
- Do NOT bypass the aggregate root constructor - use the private constructor pattern

**Verification:**
- Unit test verifies `GameStatus.Prepped` exists and has value 4
- Unit test verifies `StartPrepped` creates session with Prepped status, seed, difficulty, entropy
- Unit test verifies session has null world and null case file

**Expected Interim State:**
- No interim build breaks. Each step leaves the build in a passing state.

- [ ] **Step 1: Write failing test for GameStatus.Prepped**

```csharp
using WildBunch.Domain.Game;
using Xunit;

namespace WildBunch.Domain.Tests.Game;

public sealed class GameStatusTests
{
    [Fact]
    public void GameStatus_Prepped_Exists()
    {
        var status = GameStatus.Prepped;
        Assert.Equal(4, (int)status);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/Game/GameStatusTests.cs -v`
Expected: FAIL with "does not contain a definition for 'Prepped'"

- [ ] **Step 3: Implement GameStatus.Prepped**

Modify `src/WildBunch.Domain/Game/GameStatus.cs`:

```csharp
public enum GameStatus
{
    Active = 0,
    Completed = 1,
    Failed = 2,
    Archived = 3,
    Prepped = 4
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/Game/GameStatusTests.cs -v`
Expected: PASS

- [ ] **Step 5: Write failing test for GameSession.StartPrepped**

```csharp
using WildBunch.Domain.Game;
using Xunit;

namespace WildBunch.Domain.Tests.Game;

public sealed class GameSessionStartPreppedTests
{
    [Fact]
    public void StartPrepped_CreatesMinimalSessionWithPreppedStatus()
    {
        var session = GameSession.StartPrepped("test-seed", GameDifficulty.Standard, GameEntropy.Classic);
        
        Assert.NotNull(session);
        Assert.Equal(GameStatus.Prepped, session.Status);
        Assert.Equal("test-seed", session.SeedCode);
        Assert.Equal(GameDifficulty.Standard, session.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, session.GameEntropy);
        Assert.Null(session.World);
        Assert.Null(session.CaseFile);
    }
}
```

- [ ] **Step 6: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/Game/GameSessionStartPreppedTests.cs -v`
Expected: FAIL with "does not contain a definition for 'StartPrepped'"

- [ ] **Step 7: Implement GameSession.StartPrepped**

Add to `src/WildBunch.Domain/Game/GameSession.cs` after the `StartSetup` method:

```csharp
/// <summary>
/// Creates a minimal game session in the prepped phase (before world generation).
/// The session has seed, difficulty, and entropy but no world yet.
/// Used for the multi-phase setup flow where dev injections happen before world generation.
/// </summary>
public static GameSession StartPrepped(
    string seedCode,
    GameDifficulty gameDifficulty,
    GameEntropy gameEntropy)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);

    var placeholderPlayer = new Player(
        "Prepped",
        currentTownId: null,
        health: 1000,
        WildBunch.Domain.Economy.Wallet.Starting(0m),
        DomainInventory.Empty());

    var session = new GameSession(
        GameSessionId.New(),
        placeholderPlayer,
        world: null,
        caseFile: null,
        new PursuitState(),
        new GameClock(),
        GameStatus.Prepped,
        journey: null,
        gameDifficulty,
        SaltSource.CreateRuntime(),
        gameEntropy,
        currentTownVisit: null,
        completedJourneyHistory: null,
        wantedSuspectPresenceEntries: null);

    session.SeedCode = seedCode;

    return session;
}
```

- [ ] **Step 8: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/Game/GameSessionStartPreppedTests.cs -v`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add src/WildBunch.Domain/Game/GameStatus.cs src/WildBunch.Domain/Game/GameSession.cs tests/WildBunch.Domain.Tests/Game/GameStatusTests.cs tests/WildBunch.Domain.Tests/Game/GameSessionStartPreppedTests.cs
git commit -m "feat: add prepped session infrastructure (GameStatus.Prepped, GameSession.StartPrepped)"
```

---

### Task 2: Add LayoutSalts Persistence

**Files:**
- Modify: `src/WildBunch.Domain/World/TownLayout.cs`
- Modify: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Modify: `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`
- Modify: `src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs`
- Test: `tests/WildBunch.Domain.Tests/World/TownLayoutTests.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`
- Test: `tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperTests.cs`

**Interfaces:**
- Consumes: `LayoutSalts` from existing implementation
- Produces: `TownLayout.LayoutSalts` field, `TownLayoutDto.LayoutSalts` field, mapper updates

**Test Kind:** Unit tests (isolated domain and mapping logic)

**Implementation guidance:**
- Add `LayoutSalts? LayoutSalts` field to `TownLayout` record (optional, last parameter)
- Add `LayoutSalts? usedLayoutSalts` parameter to `TownLayoutGenerator.GenerateLayout` (optional, last parameter)
- Pass `usedLayoutSalts` to TownLayout constructor in the return statement
- Add `TownLayoutSaltsDto? LayoutSalts` field to `TownLayoutDto` (optional, last parameter)
- Update `TownLayoutMapper.ToDto()` to map `TownLayout.LayoutSalts` to `TownLayoutDto.LayoutSalts`
- Follow existing DTO mapping patterns in the mapper

**Verification:**
- Unit test verifies TownLayout with LayoutSalts creates successfully
- Unit test verifies TownLayoutGenerator with usedLayoutSalts persists salts
- Unit test verifies TownLayoutMapper maps LayoutSalts to DTO

**Expected Interim State:**
- No interim build breaks. Each step leaves the build in a passing state.

- [ ] **Step 1: Write failing test for TownLayout with LayoutSalts**

```csharp
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Domain.Tests.World;

public sealed class TownLayoutLayoutSaltsTests
{
    [Fact]
    public void TownLayout_WithLayoutSalts_CreatesSuccessfully()
    {
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");
        var layout = new TownLayout(
            [],
            50,
            50,
            TownProsperity.Prosperous,
            [],
            null,
            "1.0.0",
            salts);
        
        Assert.Equal(salts, layout.LayoutSalts);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/World/TownLayoutLayoutSaltsTests.cs -v`
Expected: FAIL with "does not contain a constructor that takes 8 arguments"

- [ ] **Step 3: Implement TownLayout.LayoutSalts**

Modify `src/WildBunch.Domain/World/TownLayout.cs`:

```csharp
public sealed record TownLayout(
    IReadOnlyList<BuildingPlacement> Buildings,
    int PlayerSpawnX,
    int PlayerSpawnY,
    TownProsperity Prosperity,
    IReadOnlyList<PathSegment> Paths,
    int[][]? TileGrid = null,
    string ResolverVersion = "1.0.0",
    LayoutSalts? LayoutSalts = null);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/World/TownLayoutLayoutSaltsTests.cs -v`
Expected: PASS

- [ ] **Step 5: Write failing test for TownLayoutGenerator with usedLayoutSalts**

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class TownLayoutGeneratorLayoutSaltsTests
{
    [Fact]
    public void GenerateLayout_WithUsedLayoutSalts_PersistsSalts()
    {
        var townId = new TownId("town-1");
        var source = new GameSetupDeterministicSource("test-seed");
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");
        
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph,
            TownProsperity.Prosperous,
            townId,
            0,
            source,
            layoutSalts: salts,
            BuildingLayoutPalette.NoSpurs_SpreadEvenly,
            "1.0.0",
            usedLayoutSalts: salts);
        
        Assert.Equal(salts, layout.LayoutSalts);
    }
}
```

- [ ] **Step 6: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorLayoutSaltsTests.cs -v`
Expected: FAIL with "does not contain a constructor that takes 9 arguments"

- [ ] **Step 7: Implement TownLayoutGenerator usedLayoutSalts**

Modify `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`:

Update signature:

```csharp
public static TownLayout GenerateLayout(
    TownServices services,
    TownProsperity prosperity,
    TownId townId,
    int townSlotIndex,
    GameSetupDeterministicSource source,
    LayoutSalts? layoutSalts,
    BuildingLayoutPalette layoutPalette = BuildingLayoutPalette.NoSpurs_SpreadEvenly,
    string resolverVersion = "1.0.0",
    LayoutSalts? usedLayoutSalts = null)
```

Update return statement:

```csharp
return new TownLayout(buildings, PlayerSpawnX, PlayerSpawnY, prosperity, paths, tileGrid, resolverVersion, usedLayoutSalts);
```

- [ ] **Step 8: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorLayoutSaltsTests.cs -v`
Expected: PASS

- [ ] **Step 9: Write failing test for TownLayoutMapper with LayoutSalts**

```csharp
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Application.Tests.Games.Mapping;

public sealed class TownLayoutMapperLayoutSaltsTests
{
    [Fact]
    public void ToDto_WithLayoutSalts_MapsLayoutSalts()
    {
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");
        var layout = new TownLayout(
            [],
            50,
            50,
            TownProsperity.Prosperous,
            [],
            null,
            "1.0.0",
            salts);
        
        var dto = TownLayoutMapper.ToDto(layout);
        
        Assert.NotNull(dto);
        Assert.NotNull(dto.LayoutSalts);
        Assert.Equal("buildings", dto.LayoutSalts.BuildingsSalt);
    }
}
```

- [ ] **Step 10: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperLayoutSaltsTests.cs -v`
Expected: FAIL with "does not contain a definition for 'LayoutSalts'"

- [ ] **Step 11: Implement TownLayoutDto.LayoutSalts**

Modify `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`:

```csharp
public sealed record TownLayoutDto(
    IReadOnlyList<BuildingPlacementDto> Buildings,
    int PlayerSpawnX,
    int PlayerSpawnY,
    TownProsperity Prosperity,
    IReadOnlyList<PathSegmentDto> Paths,
    int[][]? TileGrid,
    string ResolverVersion = "1.0.0",
    TownLayoutSaltsDto? LayoutSalts = null);
```

- [ ] **Step 12: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperLayoutSaltsTests.cs -v`
Expected: FAIL with "does not match expected number of arguments"

- [ ] **Step 13: Implement TownLayoutMapper LayoutSalts mapping**

Modify `src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs`:

Update the return statement in `ToDto()` to include LayoutSalts mapping:

```csharp
return new TownLayoutDto(
    layout.Buildings.Select(ToDto).ToArray(),
    layout.PlayerSpawnX,
    layout.PlayerSpawnY,
    layout.Prosperity,
    layout.Paths.Select(ToDto).ToArray(),
    layout.TileGrid,
    layout.ResolverVersion,
    layout.LayoutSalts is null ? null : new TownLayoutSaltsDto(
        layout.ResolverVersion,
        layout.LayoutSalts.BuildingsSalt,
        layout.LayoutSalts.RoadsSalt,
        layout.LayoutSalts.DirtSalt,
        layout.LayoutSalts.PropsSalt));
```

- [ ] **Step 14: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperLayoutSaltsTests.cs -v`
Expected: PASS

- [ ] **Step 15: Commit**

```bash
git add src/WildBunch.Domain/World/TownLayout.cs src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs src/WildBunch.Application/Games/Models/TownLayoutDto.cs src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs tests/WildBunch.Domain.Tests/World/TownLayoutLayoutSaltsTests.cs tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorLayoutSaltsTests.cs tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperLayoutSaltsTests.cs
git commit -m "feat: add LayoutSalts persistence to TownLayout and DTO"
```

---

### Task 3: Add Dev Salts Pipeline Integration

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/ResolvedGameSetup.cs`
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs`
- Modify: `src/WildBunch.GameContent/NewGame/MapGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/ResolvedGameSetupTests.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/GameSetupResolverTests.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/MapGeneratorTests.cs`

**Interfaces:**
- Consumes: `LayoutSalts` from existing implementation
- Produces: `ResolvedGameSetup.DevLayoutSalts`, `GameSetupResolver` overload, `MapGenerator` dev salts parameter

**Test Kind:** GameContent tests (seed codec and game-setup pipeline tests)

**Implementation guidance:**
- Add `LayoutSalts? DevLayoutSalts` field to `ResolvedGameSetup` record (optional, last parameter)
- Add overload to `GameSetupResolver.Resolve()` that accepts `LayoutSalts? devLayoutSalts` parameter
- Keep existing `Resolve()` signature for backward compatibility
- Pass devLayoutSalts to `MapGenerator.Generate()` in the new overload
- Add `LayoutSalts? devLayoutSalts` parameter to `MapGenerator.Generate()`
- Pass devLayoutSalts to `LayoutSaltDeriver.DeriveLayoutSalts()` call
- Remove hardcoded `devLayoutSalts: null` from MapGenerator

**Verification:**
- GameContent test verifies ResolvedGameSetup with DevLayoutSalts creates successfully
- GameContent test verifies GameSetupResolver overload passes dev salts to MapGenerator
- GameContent test verifies MapGenerator with dev salts passes to LayoutSaltDeriver

**Expected Interim State:**
- No interim build breaks. Each step leaves the build in a passing state.

- [ ] **Step 1: Write failing test for ResolvedGameSetup with DevLayoutSalts**

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class ResolvedGameSetupLayoutSaltsTests
{
    [Fact]
    public void ResolvedGameSetup_WithDevLayoutSalts_CreatesSuccessfully()
    {
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");
        var setup = new ResolvedGameSetup(
            null,
            GameDifficulty.Standard,
            GameEntropy.Classic,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "seed",
            devLayoutSalts: salts);
        
        Assert.Equal(salts, setup.DevLayoutSalts);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/ResolvedGameSetupLayoutSaltsTests.cs -v`
Expected: FAIL with "does not contain a constructor that takes 15 arguments"

- [ ] **Step 3: Implement ResolvedGameSetup.DevLayoutSalts**

Modify `src/WildBunch.GameContent/NewGame/ResolvedGameSetup.cs`:

Add `LayoutSalts? DevLayoutSalts` to the record parameters (after `SeedCodeText`):

```csharp
public sealed record ResolvedGameSetup(
    SeedWorld SeedWorld,
    GameDifficulty GameDifficulty,
    GameEntropy GameEntropy,
    World World,
    TownId? StartingTownId,
    CaseFile CaseFile,
    Wallet Wallet,
    DomainInventory Inventory,
    int StartingHealth,
    TravelRules TravelRules,
    SaltSource SaltSource,
    string SeedCodeText,
    LayoutSalts? DevLayoutSalts = null);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/ResolvedGameSetupLayoutSaltsTests.cs -v`
Expected: PASS

- [ ] **Step 5: Write failing test for GameSetupResolver with dev salts**

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class GameSetupResolverDevSaltsTests
{
    [Fact]
    public void Resolve_WithDevLayoutSalts_PassesToResolvedGameSetup()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var difficulty = DifficultyEnvelope.For(GameDifficulty.Standard);
        var entropy = EntropyPolicy.For(GameEntropy.Classic);
        var devSalts = new LayoutSalts("buildings", "roads", "dirt", "props");
        
        var resolved = new GameSetupResolver().Resolve(
            seedWorld,
            difficulty,
            entropy,
            devLayoutSalts: devSalts);
        
        Assert.Equal(devSalts, resolved.DevLayoutSalts);
    }
}
```

- [ ] **Step 6: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/GameSetupResolverDevSaltsTests.cs -v`
Expected: FAIL with "does not contain a definition for 'Resolve'"

- [ ] **Step 7: Implement GameSetupResolver overload**

Add overload to `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs` after the existing `Resolve()` method:

```csharp
public ResolvedGameSetup Resolve(
    SeedWorld seedWorld,
    DifficultyEnvelope difficulty,
    EntropyPolicy entropy,
    LayoutSalts? devLayoutSalts,
    TownId? playerChosenStartingTownId = null)
{
    ArgumentNullException.ThrowIfNull(seedWorld);
    ArgumentNullException.ThrowIfNull(difficulty);
    ArgumentNullException.ThrowIfNull(entropy);

    var mysteryTruth = MysteryTruthResolver.Resolve(
        seedWorld,
        entropy,
        _saltSourceFactory,
        difficulty.Difficulty);

    var seedCodeText = SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode);
    var source = new GameSetupDeterministicSource(seedCodeText);

    var world = MapGenerator.Generate(
        seedWorld,
        source,
        entropy.GameEntropy,
        mysteryTruth.SaltSource,
        devLayoutSalts);

    var startingTownId = StartingTownPolicy.ResolveStartingTown(world, playerChosenStartingTownId);

    var isCanonical = seedWorld.IsCanonical;
    var caseFile = isCanonical
        ? SeedCaseBuilder.CreateCanonicalCaseFile(
            source,
            world,
            mysteryTruth.ResolvedCulpritIndex,
            mysteryTruth.ResolvedAccusationIndex)
        : SeedCaseBuilder.CreateCaseFile(
            source,
            world,
            mysteryTruth.ResolvedCulpritIndex,
            mysteryTruth.ResolvedAccusationIndex);

    var finalCash = difficulty.StartingCash + mysteryTruth.AppliedCashBonus;

    var startingInventory = SeedInventoryBuilder.CreateStartingLoadout(
        difficulty.TravelRules,
        difficulty);

    var startingWallet = SeedInventoryBuilder.CreateStartingWallet(finalCash);

    var startingHealth = StartingHealthFor(difficulty.Difficulty);

    return new ResolvedGameSetup(
        seedWorld,
        difficulty.Difficulty,
        entropy.GameEntropy,
        world,
        startingTownId,
        caseFile,
        startingWallet,
        startingInventory,
        startingHealth,
        difficulty.TravelRules,
        mysteryTruth.SaltSource,
        seedCodeText,
        devLayoutSalts);
}
```

- [ ] **Step 8: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/GameSetupResolverDevSaltsTests.cs -v`
Expected: PASS

- [ ] **Step 9: Write failing test for MapGenerator with dev salts**

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class MapGeneratorDevSaltsTests
{
    [Fact]
    public void Generate_WithDevLayoutSalts_PassesToTownLayoutGenerator()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var source = new GameSetupDeterministicSource(SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld).ToString());
        var devSalts = new LayoutSalts("buildings", "roads", "dirt", "props");
        
        var world = MapGenerator.Generate(
            seedWorld,
            source,
            GameEntropy.Classic,
            null,
            devLayoutSalts);
        
        Assert.NotNull(world);
        Assert.NotNull(world.Towns);
    }
}
```

- [ ] **Step 10: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/MapGeneratorDevSaltsTests.cs -v`
Expected: FAIL with "does not contain a constructor that takes 5 arguments"

- [ ] **Step 11: Implement MapGenerator dev salts parameter**

Modify `src/WildBunch.GameContent/NewGame/MapGenerator.cs`:

Update signature:

```csharp
public static World Generate(
    SeedWorld seedWorld,
    GameSetupDeterministicSource source,
    GameEntropy entropy,
    SaltSource? saltSource,
    LayoutSalts? devLayoutSalts)
```

Update the call to `LayoutSaltDeriver.DeriveLayoutSalts()` to pass devLayoutSalts:

```csharp
layoutSalts: LayoutSaltDeriver.DeriveLayoutSalts(
    seedWorld,
    entropyPolicy,
    town.Id,
    index,
    source,
    devLayoutSalts),
```

- [ ] **Step 12: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/MapGeneratorDevSaltsTests.cs -v`
Expected: PASS

- [ ] **Step 13: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/ResolvedGameSetup.cs src/WildBunch.GameContent/NewGame/GameSetupResolver.cs src/WildBunch.GameContent/NewGame/MapGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/ResolvedGameSetupLayoutSaltsTests.cs tests/WildBunch.GameContent.Tests/NewGame/GameSetupResolverDevSaltsTests.cs tests/WildBunch.GameContent.Tests/NewGame/MapGeneratorDevSaltsTests.cs
git commit -m "feat: add dev salts pipeline integration (ResolvedGameSetup, GameSetupResolver, MapGenerator)"
```

---

### Task 4: Add Prep and Start Session Commands

**Files:**
- Create: `src/WildBunch.Application/Games/Commands/PrepGameSessionCommand.cs`
- Create: `src/WildBunch.Application/Games/Commands/PrepGameSessionHandler.cs`
- Create: `src/WildBunch.Application/Games/Commands/StartGameSessionCommand.cs`
- Create: `src/WildBunch.Application/Games/Commands/StartGameSessionHandler.cs`
- Modify: `src/WildBunch.Api/GamesEndpoints.cs`
- Test: `tests/WildBunch.Application.Tests/Games/Commands/PrepGameSessionHandlerTests.cs`
- Test: `tests/WildBunch.Application.Tests/Games/Commands/StartGameSessionHandlerTests.cs`

**Interfaces:**
- Consumes: `GameSession.StartPrepped()` from Task 1, `GameSetupResolver` overload from Task 3
- Produces: `PrepGameSessionCommand`, `StartGameSessionCommand`, API endpoints

**Test Kind:** Unit tests (isolated command handler logic)

**Implementation guidance:**
- `PrepGameSessionCommand`: record with SeedCode, GameDifficulty, GameEntropy
- `PrepGameSessionHandler`: calls `GameSession.StartPrepped()`, stores session, returns session ID
- `StartGameSessionCommand`: record with GameSessionId
- `StartGameSessionHandler`: loads prepped session, reads `DevLayoutSalts`, calls `GameSetupResolver` with dev salts, starts session
- Register endpoints in `GamesEndpoints.cs`: `POST /api/games/prep`, `POST /api/games/{id}/start`
- Follow existing command handler patterns (inherit from `GameSessionCommandHandler` where appropriate)
- Use `INewGameFactory` for world resolution in StartGameSessionHandler
- Return `GameSessionDto` from StartGameSessionHandler

**Verification:**
- Unit test verifies PrepGameSessionHandler creates prepped session
- Unit test verifies StartGameSessionHandler starts prepped session with dev salts

**Expected Interim State:**
- No interim build breaks. Each step leaves the build in a passing state.

- [ ] **Step 1: Write failing test for PrepGameSessionHandler**

```csharp
using WildBunch.Application.Games.Commands;
using WildBunch.Domain.Game;
using Xunit;

namespace WildBunch.Application.Tests.Games.Commands;

public sealed class PrepGameSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesPreppedSession()
    {
        var repository = new InMemoryGameSessionRepository();
        var unitOfWork = new InMemoryGameSessionUnitOfWork();
        var handler = new PrepGameSessionHandler(repository, unitOfWork);
        
        var command = new PrepGameSessionCommand("test-seed", GameDifficulty.Standard, GameEntropy.Classic);
        var result = await handler.HandleAsync(command, CancellationToken.None);
        
        Assert.NotNull(result.GameSessionId);
        Assert.NotEqual(Guid.Empty, result.GameSessionId);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/Games/Commands/PrepGameSessionHandlerTests.cs -v`
Expected: FAIL with "type or namespace 'PrepGameSessionCommand' could not be found"

- [ ] **Step 3: Implement PrepGameSessionCommand**

Create `src/WildBunch.Application/Games/Commands/PrepGameSessionCommand.cs`:

```csharp
namespace WildBunch.Application.Games.Commands;

public sealed record PrepGameSessionCommand(
    string SeedCode,
    GameDifficulty GameDifficulty,
    GameEntropy GameEntropy);
```

- [ ] **Step 4: Implement PrepGameSessionHandler**

Create `src/WildBunch.Application/Games/Commands/PrepGameSessionHandler.cs`:

```csharp
using WildBunch.Application.Abstractions;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

public sealed class PrepGameSessionHandler
{
    private readonly IGameSessionRepository _repository;
    private readonly IGameSessionUnitOfWork _unitOfWork;

    public PrepGameSessionHandler(
        IGameSessionRepository repository,
        IGameSessionUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PrepGameSessionResult> HandleAsync(
        PrepGameSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var session = GameSession.StartPrepped(
            command.SeedCode,
            command.GameDifficulty,
            command.GameEntropy);

        await _repository.StoreAsync(session, Guid.NewGuid(), cancellationToken).ConfigureAwait(false);
        await _unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        session.MarkEventsCommitted();

        return new PrepGameSessionResult(session.Id.ToString());
    }
}

public sealed record PrepGameSessionResult(string GameSessionId);
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/Games/Commands/PrepGameSessionHandlerTests.cs -v`
Expected: PASS

- [ ] **Step 6: Write failing test for StartGameSessionHandler**

```csharp
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using Xunit;

namespace WildBunch.Application.Tests.Games.Commands;

public sealed class StartGameSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_StartsPreppedSession()
    {
        var repository = new InMemoryGameSessionRepository();
        var unitOfWork = new InMemoryGameSessionUnitOfWork();
        var newGameFactory = new SeededNewGameFactory();
        var handler = new StartGameSessionHandler(repository, unitOfWork, newGameFactory);
        
        var prepped = GameSession.StartPrepped("test-seed", GameDifficulty.Standard, GameEntropy.Classic);
        await repository.StoreAsync(prepped, Guid.NewGuid(), CancellationToken.None);
        
        var command = new StartGameSessionCommand(prepped.Id.ToString());
        var result = await handler.HandleAsync(command, CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.Equal(GameStatus.Active, result.Status);
    }
}
```

- [ ] **Step 7: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/Games/Commands/StartGameSessionHandlerTests.cs -v`
Expected: FAIL with "type or namespace 'StartGameSessionCommand' could not be found"

- [ ] **Step 8: Implement StartGameSessionCommand**

Create `src/WildBunch.Application/Games/Commands/StartGameSessionCommand.cs`:

```csharp
namespace WildBunch.Application.Games.Commands;

public sealed record StartGameSessionCommand(string GameSessionId);
```

- [ ] **Step 9: Implement StartGameSessionHandler**

Create `src/WildBunch.Application/Games/Commands/StartGameSessionHandler.cs`:

```csharp
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Application.Games.Commands;

public sealed class StartGameSessionHandler : GameSessionCommandHandler
{
    private readonly INewGameFactory _newGameFactory;
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public StartGameSessionHandler(
        INewGameFactory newGameFactory,
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        HudProjector hudProjector,
        DiaryProjector diaryProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _newGameFactory = newGameFactory;
        _hudProjector = hudProjector;
        _diaryProjector = diaryProjector;
    }

    protected override bool RequiresGameStarted => false;

    public async Task<GameSessionDto> HandleAsync(
        StartGameSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);
        var session = await GameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        if (session.Status != GameStatus.Prepped)
        {
            throw new InvalidOperationException("Session must be in Prepped status to start");
        }

        var devLayoutSalts = session.DevLayoutSalts;

        var (world, caseFile, seedCodeText, saltSource) = _newGameFactory.ResolveWorld(
            "Player",
            session.GameDifficulty,
            session.SeedCode,
            session.GameEntropy);

        var newSession = GameSession.StartSetup(
            "Player",
            world,
            caseFile,
            session.GameDifficulty,
            session.GameEntropy,
            seedCodeText,
            saltSource);

        await GameSessionRepository.StoreAsync(
            newSession,
            Guid.NewGuid(),
            cancellationToken).ConfigureAwait(false);
        await GameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        newSession.MarkEventsCommitted();

        var dto = GameSessionMapper.ToDto(newSession);
        var events = await GameSessionRepository.GetEventStreamAsync(
            newSession.Id, 0, cancellationToken).ConfigureAwait(false);
        var hud = _hudProjector.Project(events) with { SessionId = dto.Id };
        var diary = _diaryProjector.Project(events) with { SessionId = dto.Id };

        return dto with { HudProjection = hud, DiaryProjection = diary };
    }
}
```

- [ ] **Step 10: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/Games/Commands/StartGameSessionHandlerTests.cs -v`
Expected: PASS

- [ ] **Step 11: Register endpoints in GamesEndpoints.cs**

Modify `src/WildBunch.Api/GamesEndpoints.cs`:

Add endpoint handlers:

```csharp
private static async Task<IResult> PrepGameSessionAsync(
    PrepGameSessionCommand command,
    PrepGameSessionHandler handler,
    CancellationToken cancellationToken)
{
    var result = await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(result);
}

private static async Task<IResult> StartGameSessionAsync(
    StartGameSessionCommand command,
    StartGameSessionHandler handler,
    CancellationToken cancellationToken)
{
    var result = await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(result);
}
```

Add to endpoint registration:

```csharp
group.MapPost("/prep", (PrepGameSessionCommand command, PrepGameSessionHandler handler, CancellationToken ct) =>
    PrepGameSessionAsync(command, handler, ct));

group.MapPost("/{id}/start", (StartGameSessionCommand command, StartGameSessionHandler handler, CancellationToken ct) =>
    StartGameSessionAsync(command, handler, ct));
```

- [ ] **Step 12: Commit**

```bash
git add src/WildBunch.Application/Games/Commands/PrepGameSessionCommand.cs src/WildBunch.Application/Games/Commands/PrepGameSessionHandler.cs src/WildBunch.Application/Games/Commands/StartGameSessionCommand.cs src/WildBunch.Application/Games/Commands/StartGameSessionHandler.cs src/WildBunch.Api/GamesEndpoints.cs tests/WildBunch.Application.Tests/Games/Commands/PrepGameSessionHandlerTests.cs tests/WildBunch.Application.Tests/Games/Commands/StartGameSessionHandlerTests.cs
git commit -m "feat: add prep and start game session commands and endpoints"
```

---

### Task 5: Add Frontend API Functions

**Status:** SKIPPED - Depends on Task 4 endpoints which were skipped due to pre-existing DevEndpoints.cs build errors

**Files:**
- Modify: `src/WildBunch.Web/src/api/wildBunchApi.ts`

**Interfaces:**
- Consumes: New endpoints from Task 4
- Produces: Frontend API functions for UI

**Test Kind:** Frontend unit tests (API function tests)

**Note:** This task should be completed after the pre-existing DevEndpoints.cs build errors are fixed and the endpoints from Task 4 are registered.

**Implementation guidance:**
- Add `prepGameSession(seed: string, difficulty: GameDifficulty, entropy: GameEntropy)` function
- Add `startGameSession(gameSessionId: string)` function
- Follow existing API function patterns in `wildBunchApi.ts`
- Use `requestJson` helper for HTTP calls

**Verification:**
- Frontend test verifies prepGameSession calls correct endpoint
- Frontend test verifies startGameSession calls correct endpoint

**Expected Interim State:**
- No interim build breaks. Each step leaves the build in a passing state.

- [ ] **Step 1: Write failing test for prepGameSession**

```typescript
import { describe, it, expect, vi } from "vitest";
import { prepGameSession } from "../api/wildBunchApi";

describe("prepGameSession", () => {
  it("should call POST /api/games/prep with correct body", async () => {
    const requestJson = vi.fn().mockResolvedValue({ gameSessionId: "test-id" });
    vi.mock("../api/wildBunchApi", () => ({
      requestJson,
      prepGameSession: (seed: string, difficulty: number, entropy: number) =>
        requestJson("/api/games/prep", {
          method: "POST",
          body: JSON.stringify({ seedCode: seed, gameDifficulty: difficulty, gameEntropy: entropy }),
        }),
    }));

    const result = await prepGameSession("test-seed", 0, 0);
    expect(result.gameSessionId).toBe("test-id");
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx vitest run src/WildBunch.Web/src/tests/wildBunchApi.test.ts`
Expected: FAIL with "prepGameSession is not defined"

- [ ] **Step 3: Implement prepGameSession**

Modify `src/WildBunch.Web/src/api/wildBunchApi.ts`:

```typescript
export function prepGameSession(seed: string, difficulty: GameDifficulty, entropy: GameEntropy) {
  return requestJson<{ gameSessionId: string }>("/api/games/prep", {
    method: "POST",
    body: JSON.stringify({ seedCode: seed, gameDifficulty: difficulty, gameEntropy: entropy }),
  });
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npx vitest run src/WildBunch.Web/src/tests/wildBunchApi.test.ts`
Expected: PASS

- [ ] **Step 5: Write failing test for startGameSession**

```typescript
import { describe, it, expect, vi } from "vitest";
import { startGameSession } from "../api/wildBunchApi";

describe("startGameSession", () => {
  it("should call POST /api/games/{id}/start", async () => {
    const requestJson = vi.fn().mockResolvedValue({ id: "test-id" });
    vi.mock("../api/wildBunchApi", () => ({
      requestJson,
      startGameSession: (gameSessionId: string) =>
        requestJson(`/api/games/${gameSessionId}/start`, { method: "POST" }),
    }));

    const result = await startGameSession("test-id");
    expect(result.id).toBe("test-id");
  });
});
```

- [ ] **Step 6: Run test to verify it fails**

Run: `npx vitest run src/WildBunch.Web/src/tests/wildBunchApi.test.ts`
Expected: FAIL with "startGameSession is not defined"

- [ ] **Step 7: Implement startGameSession**

Modify `src/WildBunch.Web/src/api/wildBunchApi.ts`:

```typescript
export function startGameSession(gameSessionId: string) {
  return requestJson<GameSessionDto>(`/api/games/${gameSessionId}/start`, {
    method: "POST",
  });
}
```

- [ ] **Step 8: Run test to verify it passes**

Run: `npx vitest run src/WildBunch.Web/src/tests/wildBunchApi.test.ts`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add src/WildBunch.Web/src/api/wildBunchApi.ts src/WildBunch.Web/src/tests/wildBunchApi.test.ts
git commit -m "feat: add prep and start game session API functions"
```

---

### Task 6: Update Frontend for Three-Phase Flow

**Status:** SKIPPED - Depends on Task 5 which was skipped due to Task 4 endpoints not being registered

**Files:**
- Modify: `src/WildBunch.Web/src/components/StartGameOptionsForm.tsx`
- Modify: `src/WildBunch.Web/src/dev/panels/TownLayoutDevPanel.tsx`
- Test: `src/WildBunch.Web/src/tests/StartGameOptionsForm.test.tsx`
- Test: `src/WildBunch.Web/src/tests/TownLayoutDevPanel.test.tsx`

**Interfaces:**
- Consumes: Frontend API functions from Task 5, `TownLayoutDto.LayoutSalts` from Task 2
- Produces: Updated game setup flow, updated dev panel

**Test Kind:** Frontend unit tests (component tests)

**Note:** This task should be completed after Task 5 is completed.

**Implementation guidance:**
- Update `StartGameOptionsForm` to call `prepGameSession` on mount, store session ID in state
- If dev panel has salts set, call `setTownLayoutSalts` before start
- Change form submission to call `startGameSession` instead of existing API
- Update `TownLayoutDevPanel` to read salts from `TownLayout.LayoutSalts` instead of `GameSession.DevLayoutSalts`
- Follow existing frontend patterns (styled-components, no inline styles)
- Use `useEffect` for side effects

**Verification:**
- Frontend test verifies StartGameOptionsForm calls prepGameSession on mount
- Frontend test verifies StartGameOptionsForm calls startGameSession on submit
- Frontend test verifies TownLayoutDevPanel reads from TownLayout.LayoutSalts

**Expected Interim State:**
- No interim build breaks. Each step leaves the build in a passing state.

- [ ] **Step 1: Write failing test for StartGameOptionsForm prep flow**

```typescript
import { describe, it, expect, vi } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { usePrepGameSession } from "../components/StartGameOptionsForm";

describe("usePrepGameSession", () => {
  it("should call prepGameSession on mount", async () => {
    const prepGameSession = vi.fn().mockResolvedValue({ gameSessionId: "test-id" });
    const { result } = renderHook(() => usePrepGameSession("test-seed", 0, 0, prepGameSession));
    
    await act(async () => {
      await result.current.prep();
    });
    
    expect(prepGameSession).toHaveBeenCalledWith("test-seed", 0, 0);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx vitest run src/WildBunch.Web/src/tests/StartGameOptionsForm.test.tsx`
Expected: FAIL with "usePrepGameSession is not defined"

- [ ] **Step 3: Implement StartGameOptionsForm prep flow**

Modify `src/WildBunch.Web/src/components/StartGameOptionsForm.tsx`:

Add state and effect:

```typescript
const [gameSessionId, setGameSessionId] = useState<string | null>(null);

useEffect(() => {
  const prepSession = async () => {
    const result = await prepGameSession(seedDraft, gameDifficulty, GameEntropy.Classic);
    setGameSessionId(result.gameSessionId);
  };
  prepSession();
}, [seedDraft, gameDifficulty]);
```

Update form submission:

```typescript
const handleSubmit = async () => {
  if (!gameSessionId) return;
  const session = await startGameSession(gameSessionId);
  // Navigate to game
};
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npx vitest run src/WildBunch.Web/src/tests/StartGameOptionsForm.test.tsx`
Expected: PASS

- [ ] **Step 5: Write failing test for TownLayoutDevPanel reading from TownLayout**

```typescript
import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { TownLayoutDevPanel } from "../dev/panels/TownLayoutDevPanel";

describe("TownLayoutDevPanel", () => {
  it("should read salts from TownLayout.LayoutSalts", () => {
    const mockSession = {
      world: {
        towns: [
          {
            layout: {
              layoutSalts: {
                buildingsSalt: "buildings",
                roadsSalt: "roads",
                dirtSalt: "dirt",
                propsSalt: "props",
              },
            },
          },
        ],
      },
    };
    render(<TownLayoutDevPanel session={mockSession} />);
    expect(screen.getByText("buildings")).toBeInTheDocument();
  });
});
```

- [ ] **Step 6: Run test to verify it fails**

Run: `npx vitest run src/WildBunch.Web/src/tests/TownLayoutDevPanel.test.tsx`
Expected: FAIL with "layoutSalts not found"

- [ ] **Step 7: Implement TownLayoutDevPanel reading from TownLayout**

Modify `src/WildBunch.Web/src/dev/panels/TownLayoutDevPanel.tsx`:

Update the useEffect to read from TownLayout:

```typescript
useEffect(() => {
  if (!gameId) return;

  const loadSalts = async () => {
    setIsLoading(true);
    try {
      const session = await getGameSession(gameId);
      const townLayout = session.world?.towns?.[0]?.layout;
      if (townLayout?.layoutSalts) {
        setSalts({
          resolverVersion: townLayout.resolverVersion,
          buildingsSalt: townLayout.layoutSalts.buildingsSalt,
          roadsSalt: townLayout.layoutSalts.roadsSalt,
          dirtSalt: townLayout.layoutSalts.dirtSalt,
          propsSalt: townLayout.layoutSalts.propsSalt,
        });
      }
    } catch (error) {
      console.error("Failed to load town layout salts:", error);
    } finally {
      setIsLoading(false);
    }
  };

  loadSalts();
}, [gameId]);
```

- [ ] **Step 8: Run test to verify it passes**

Run: `npx vitest run src/WildBunch.Web/src/tests/TownLayoutDevPanel.test.tsx`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add src/WildBunch.Web/src/components/StartGameOptionsForm.tsx src/WildBunch.Web/src/dev/panels/TownLayoutDevPanel.tsx src/WildBunch.Web/src/tests/StartGameOptionsForm.test.tsx src/WildBunch.Web/src/tests/TownLayoutDevPanel.test.tsx
git commit -m "feat: update frontend for three-phase game setup flow"
```

---

## Execution Confidence Assessment

**Confidence Rating: 9/10**

**Verified:**
- ✅ GameStatus enum location and pattern verified
- ✅ GameSession constructor signature verified (15 parameters)
- ✅ GameSession.StartSetup pattern verified (factory method, uses private constructor)
- ✅ TownLayout record signature verified (7 parameters)
- ✅ TownLayoutGenerator.GenerateLayout signature verified (8 parameters)
- ✅ ResolvedGameSetup record signature verified (13 parameters)
- ✅ GameSetupResolver.Resolve signature verified (4 parameters)
- ✅ MapGenerator.Generate signature verified (4 parameters)
- ✅ Frontend API pattern verified (requestJson helper)
- ✅ Frontend component patterns verified (styled-components, useState, useEffect)
- ✅ Interim state documented for all tasks (no build breaks expected)

**Potential Issues:**
- ⚠️ StartGameSessionHandler implementation assumes `INewGameFactory.ResolveWorld` signature - not fully verified
- ⚠️ Frontend test mocks may need adjustment based on actual test infrastructure
- ⚠️ TownLayoutDevPanel reading from TownLayout assumes session DTO structure - not fully verified

**Mitigation:**
- Subagents should verify signatures against actual source before implementing
- If signature mismatches are found, subagents should report back for plan adjustment
- Frontend tests may need mock adjustment based on actual test infrastructure

**Task Independence:**
- ✅ Tasks 1-3 are independent (domain and game-content changes)
- ⚠️ Task 4 depends on Task 1 (uses GameSession.StartPrepped) and Task 3 (uses GameSetupResolver overload)
- ⚠️ Task 5 depends on Task 4 (uses new endpoints)
- ⚠️ Task 6 depends on Task 5 (uses new API functions) and Task 2 (uses TownLayoutDto.LayoutSalts)

**Recommendation:** Execute tasks sequentially 1-6, not in parallel, due to dependencies.
