# Geometry-First Map Generation - Plan 1b: Event Boundary

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the generated world event-derived instead of snapshot-only. Add `WorldGenerated` and `StartingTownSelected` domain events to the event stream so the world can be reconstructed by replaying events, not just from the JSON snapshot cache. Fix `TownSnapshot` to persist `IsOutlier`.

**Architecture:** The current flow passes the `World` as a constructor argument to `GameSession` and stores it only in the JSON snapshot — it's not in the event stream, which violates the event-sourcing contract (ADR-0028). This plan adds a `WorldGenerated` event (carrying towns, trails, seed, salt, entropy) emitted during setup, and a `StartingTownSelected` event (carrying the chosen town ID) emitted before `GameStarted`. `Apply(WorldGenerated)` sets `World` from the event. `Apply(StartingTownSelected)` records the town choice. The snapshot continues to cache the world, but the event is now the source of truth — replay without a snapshot produces the same world.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3, existing event-sourced GameSession aggregate

## Prerequisites

- Plan 0 (Clean Slate) must be complete.
- Plan 1a (Core Pipeline) must be complete — `MapGenerator.Generate` must exist and pass its unit tests.

## Current event flow (what we're changing)

```
CompletePlayerSetupHandler
  → _newGameFactory.ResolveWorld(...) → (world, caseFile, seedCodeText)
  → GameSession.StartSetup(playerName, world, caseFile, difficulty, entropy, seedCode)
    → constructor sets World = world directly (NOT from event)
    → emits PlayerSetupCompleted { PlayerName, GameDifficulty, GameEntropy, SeedCode }
    → Apply(PlayerSetupCompleted) sets SeedCode, Difficulty, Entropy, Player — NOT World

ViewPrologueHandler
  → session.ViewPrologue(revealedSuspectIdentifier)
  → emits PrologueViewed { RevealedSuspectIdentifier }

CompleteGameStartHandler
  → session.CompleteGameStart(startingTownId, wallet, inventory)
  → emits GameStarted { ..., StartingTownId, ... }
  → Apply(GameStarted) sets Player, Status, Difficulty, SaltSource, Entropy, SeedCode
```

## Target event flow (after this plan)

```
CompletePlayerSetupHandler
  → _newGameFactory.ResolveWorld(...) → (world, caseFile, seedCodeText)
  → GameSession.StartSetup(playerName, world, caseFile, difficulty, entropy, seedCode, saltSource)
    → emits PlayerSetupCompleted { PlayerName, GameDifficulty, GameEntropy, SeedCode }
    → emits WorldGenerated { SeedCode, SaltSource, GameEntropy, Towns, Trails }
    → Apply(PlayerSetupCompleted) sets SeedCode, Difficulty, Entropy, Player
    → Apply(WorldGenerated) sets World from Towns/Trails

ViewPrologueHandler (unchanged)
  → session.ViewPrologue(revealedSuspectIdentifier)
  → emits PrologueViewed { RevealedSuspectIdentifier }

CompleteGameStartHandler
  → session.SelectStartingTown(startingTownId)  ← NEW
    → emits StartingTownSelected { StartingTownId }
    → Apply(StartingTownSelected) records the town choice
  → session.CompleteGameStart(wallet, inventory)  ← modified (no longer takes startingTownId)
    → emits GameStarted { PlayerName, StartingTownId, ... }
    → Apply(GameStarted) sets Player, Status, etc.
```

## Files

**New files:**
- `src/WildBunch.Domain/Events/WorldGenerated.cs` — domain event carrying the generated world
- `src/WildBunch.Domain/Events/StartingTownSelected.cs` — domain event carrying the town choice
- `src/WildBunch.Domain/World/WorldSnapshot.cs` — public snapshot records for towns/trails (the event carries these)
- `tests/WildBunch.Domain.Tests/WorldGeneratedEventTests.cs` — tests for event round-trip
- `tests/WildBunch.Domain.Tests/StartingTownSelectedEventTests.cs` — tests for event round-trip

**Modified files:**
- `src/WildBunch.Domain/Game/StartFlowPhase.cs` — add `StartingTownSelected` phase
- `src/WildBunch.Domain/Game/GameSession.cs` — emit `WorldGenerated` in `StartSetup`, add `SelectStartingTown` method, modify `CompleteGameStart` to not take `startingTownId`
- `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` — add `WorldGenerated` and `StartingTownSelected` to `ApplyEvent` dispatch
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs` — add `WorldGenerated` and `StartingTownSelected` to `ResolveEventType`
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs` — fix `TownSnapshot` to persist `IsOutlier`; make `WorldSnapshot`/`TownSnapshot`/`TrailSnapshot` public for event use
- `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs` — verify world comes from event replay, not just snapshot
- `src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs` — pass `saltSource` to `StartSetup`
- `src/WildBunch.Application/Games/Commands/CompleteGameStartHandler.cs` — call `SelectStartingTown` before `CompleteGameStart`
- `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs` — expose `saltSource` from `ResolveWorld` for the setup handler
- `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs` — update if signature changes

---

## Task 1: Create Public World Snapshot Types + Fix IsOutlier

**Files:**
- Create: `src/WildBunch.Domain/World/WorldSnapshot.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`

**Interfaces:**
- Produces: `WorldSnapshot`, `TownSnapshot`, `TrailSnapshot` as public records in `WildBunch.Domain.World`. Used by Task 2 (WorldGenerated event) and the persistence layer.

The persistence layer currently has these as `private sealed record` inside the serializer class. They need to be public so the `WorldGenerated` event can carry them. Move them to the domain project and update the serializer to use the public types.

- [ ] **Step 1: Create public snapshot records in the domain**

Create `src/WildBunch.Domain/World/WorldSnapshot.cs`:

```csharp
namespace WildBunch.Domain.World;

/// <summary>
/// Immutable snapshot of a generated world for event storage and replay.
/// Carried by the WorldGenerated domain event.
/// </summary>
public sealed record WorldSnapshot(IReadOnlyList<TownSnapshot> Towns, IReadOnlyList<TrailSnapshot> Trails)
{
    public static WorldSnapshot FromDomain(World world)
        => new(
            world.Towns.Select(TownSnapshot.FromDomain).ToArray(),
            world.Trails.Select(TrailSnapshot.FromDomain).ToArray());

    public World ToDomain()
        => new(Towns.Select(TownSnapshot.ToDomain), Trails.Select(TrailSnapshot.ToDomain));
}

public sealed record TownSnapshot(
    string Id,
    string Name,
    TownServices Services,
    TownProsperity Prosperity,
    int MapX,
    int MapY,
    bool IsOutlier)
{
    public static TownSnapshot FromDomain(Town town)
        => new(town.Id.Value, town.Name, town.Services, town.Prosperity, town.MapX, town.MapY, town.IsOutlier);

    public Town ToDomain()
        => new(new TownId(Id), Name, Services, Prosperity, MapX: MapX, MapY: MapY, IsOutlier: IsOutlier);
}

public sealed record TrailSnapshot(
    string Id,
    string FromTownId,
    string ToTownId,
    TrailRisk Risk,
    TrailTerrain Terrain,
    WaterFeature WaterFeature,
    decimal RideDayDistance)
{
    public static TrailSnapshot FromDomain(Trail trail)
        => new(trail.Id.Value, trail.FromTownId.Value, trail.ToTownId.Value, trail.Risk, trail.Terrain, trail.WaterFeature, trail.RideDayDistance);

    public Trail ToDomain()
        => new(new TrailId(Id), new TownId(FromTownId), new TownId(ToTownId), Risk, Terrain, WaterFeature, RideDayDistance);
}
```

Note: `TownSnapshot` now includes `Prosperity` and `IsOutlier` — the persistence layer was missing both. `IsOutlier` is critical for the outlier guarantee. `Prosperity` was also missing (it defaults to `Prosperous` on reload, which is wrong for non-uniform palettes).

- [ ] **Step 2: Update persistence serializer to use public types**

In `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`:
- Delete the private `WorldSnapshot`, `TownSnapshot`, `TrailSnapshot` records
- Replace `WorldSnapshot.FromDomain(world)` calls with `global::WildBunch.Domain.World.WorldSnapshot.FromDomain(world)`
- Replace `WorldSnapshot.ToDomain(snapshot)` calls with `snapshot.ToDomain()`
- Replace `TownSnapshot.FromDomain(town)` calls with `global::WildBunch.Domain.World.TownSnapshot.FromDomain(town)`
- Replace `TownSnapshot.ToDomain(snapshot)` calls with `snapshot.ToDomain()`
- Replace `TrailSnapshot.FromDomain(trail)` calls with `global::WildBunch.Domain.World.TrailSnapshot.FromDomain(trail)`
- Replace `TrailSnapshot.ToDomain(snapshot)` calls with `snapshot.ToDomain()`
- Add `using WildBunch.Domain.World;` if not already present (the file already has this using)

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: PASS

- [ ] **Step 4: Run existing tests to verify no regressions**

Run: `dotnet test tests/WildBunch.GameContent.Tests/; dotnet test tests/WildBunch.Domain.Tests/`
Expected: PASS

- [ ] **Step 5: Commit**

`git add -A; git commit -m "feat: public WorldSnapshot/TownSnapshot/TrailSnapshot in domain, fix IsOutlier and Prosperity persistence"`

## Task 2: Create WorldGenerated Domain Event

**Files:**
- Create: `src/WildBunch.Domain/Events/WorldGenerated.cs`

**Interfaces:**
- Produces: `WorldGenerated` event record carrying `SeedCode`, `SaltSource`, `GameEntropy`, `WorldSnapshot`. Used by Tasks 3-5.

- [ ] **Step 1: Create the event**

Create `src/WildBunch.Domain/Events/WorldGenerated.cs`:

```csharp
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the game world was generated from the seed code, salt source, and entropy.
/// Carries the full world snapshot (towns + trails) so the world can be reconstructed
/// by replaying this event without re-running the generation pipeline.
/// This is the event-sourced source of truth for the world — the JSON snapshot
/// is a cache of this event's payload.
/// </summary>
public sealed record WorldGenerated : IDomainEvent
{
    public required string SeedCode { get; init; }
    public required SaltSource SaltSource { get; init; }
    public required GameEntropy GameEntropy { get; init; }
    public required WorldSnapshot World { get; init; }
    public DateTimeOffset OccurredAt => DateTimeOffset.UtcNow;
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: PASS

- [ ] **Step 3: Commit**

`git add -A; git commit -m "feat: add WorldGenerated domain event carrying world snapshot"`

## Task 3: Create StartingTownSelected Domain Event

**Files:**
- Create: `src/WildBunch.Domain/Events/StartingTownSelected.cs`
- Modify: `src/WildBunch.Domain/Game/StartFlowPhase.cs` — add `StartingTownSelected` phase

**Interfaces:**
- Produces: `StartingTownSelected` event carrying `StartingTownId`. Used by Tasks 4-5.

- [ ] **Step 1: Create the event**

Create `src/WildBunch.Domain/Events/StartingTownSelected.cs`:

```csharp
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player selected their starting town from the generated world.
/// This is a distinct fact from GameStarted — the player chooses a town,
/// then the game starts from that choice.
/// </summary>
public sealed record StartingTownSelected : IDomainEvent
{
    public required TownId StartingTownId { get; init; }
    public DateTimeOffset OccurredAt => DateTimeOffset.UtcNow;
}
```

- [ ] **Step 2: Add StartingTownSelected phase to StartFlowPhase enum**

In `src/WildBunch.Domain/Game/StartFlowPhase.cs`, add a new phase between `PrologueViewed` and `GameStarted`:

```csharp
public enum StartFlowPhase
{
    NotStarted = 0,
    SetupComplete = 1,
    PrologueViewed = 2,
    /// <summary>
    /// Player has selected a starting town. StartingTownSelected event has been emitted.
    /// </summary>
    StartingTownSelected = 3,
    /// <summary>
    /// Player has selected a starting town and the game has started.
    /// GameStarted event has been emitted.
    /// </summary>
    GameStarted = 4
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: PASS — note: existing code that checks `StartFlowPhase.GameStarted` still works because the enum value changed from 3 to 4, but no code uses the numeric value directly (all comparisons use the named enum members).

- [ ] **Step 4: Run existing tests to check for enum value breakage**

Run: `dotnet test tests/WildBunch.Domain.Tests/`
Expected: PASS — if any tests fail due to the enum value change, fix them inline (they should use named members, not numeric values).

- [ ] **Step 5: Commit**

`git add -A; git commit -m "feat: add StartingTownSelected domain event and StartFlowPhase.StartingTownSelected"`

## Task 4: Update GameSession to Emit and Apply New Events

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`

**Interfaces:**
- Consumes: `WorldGenerated` from Task 2, `StartingTownSelected` from Task 3
- Produces: `GameSession.StartSetup` that emits `WorldGenerated`, `GameSession.SelectStartingTown` method, modified `GameSession.CompleteGameStart` that no longer takes `startingTownId`

- [ ] **Step 1: Update StartSetup to emit WorldGenerated**

In `src/WildBunch.Domain/Game/GameSession.cs`, modify the `StartSetup` method to accept a `SaltSource` parameter and emit `WorldGenerated` after `PlayerSetupCompleted`:

```csharp
public static GameSession StartSetup(
    string playerName,
    DomainWorld world,
    CaseFile caseFile,
    GameDifficulty gameDifficulty,
    GameEntropy gameEntropy,
    string seedCode,
    SaltSource saltSource)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
    ArgumentNullException.ThrowIfNull(world);
    ArgumentNullException.ThrowIfNull(caseFile);
    ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);

    var setupEvent = new PlayerSetupCompleted
    {
        PlayerName = playerName,
        GameDifficulty = gameDifficulty,
        GameEntropy = gameEntropy,
        SeedCode = seedCode
    };

    var worldEvent = new WorldGenerated
    {
        SeedCode = seedCode,
        SaltSource = saltSource,
        GameEntropy = gameEntropy,
        World = WorldSnapshot.FromDomain(world)
    };

    var placeholderPlayer = new Player(
        playerName,
        world.Towns.First().Id,
        health: StartingHealthFor(gameDifficulty),
        WildBunch.Domain.Economy.Wallet.Starting(25m),
        DomainInventory.Empty());

    var session = new GameSession(
        GameSessionId.New(),
        placeholderPlayer,
        world, // Still passed to constructor for initial setup; Apply(WorldGenerated) will set it on replay
        caseFile,
        new PursuitState(),
        new GameClock(),
        GameStatus.Active,
        journey: null,
        gameDifficulty,
        saltSource,
        gameEntropy,
        currentTownVisit: null,
        Array.Empty<TravelJourneySnapshot>(),
        Array.Empty<WantedSuspectPresenceEntry>());

    session.Apply(setupEvent);
    session._uncommittedEvents.Add(setupEvent);
    session.Apply(worldEvent);
    session._uncommittedEvents.Add(worldEvent);

    return session;
}
```

Note: The `World` property is `public DomainWorld World { get; }` — it's set only in the constructor. For event replay to set it, we need to make it settable via a backing field. Add a private setter or use the rehydrator's `SetBackingField` pattern. The simplest approach: change `World` to have a private setter:

```csharp
public DomainWorld World { get; private set; } = null!;
```

And in `Apply(WorldGenerated)`:
```csharp
private void Apply(WorldGenerated e)
{
    World = e.World.ToDomain();
    _version++;
}
```

- [ ] **Step 2: Add Apply(WorldGenerated) and Apply(StartingTownSelected) methods**

In `GameSession.cs`, add:

```csharp
private void Apply(WorldGenerated e)
{
    World = e.World.ToDomain();
    _version++;
}

private void Apply(StartingTownSelected e)
{
    StartFlowPhase = StartFlowPhase.StartingTownSelected;
    _version++;
}
```

- [ ] **Step 3: Add SelectStartingTown method**

In `GameSession.cs`, add a new method that emits `StartingTownSelected`:

```csharp
/// <summary>
/// Records the player's starting town choice. Emits StartingTownSelected.
/// Must be called after ViewPrologue and before CompleteGameStart.
/// </summary>
public void SelectStartingTown(TownId startingTownId)
{
    ArgumentNullException.ThrowIfNull(startingTownId);

    if (StartFlowPhase == StartFlowPhase.StartingTownSelected)
        return; // Idempotent

    if (StartFlowPhase != StartFlowPhase.PrologueViewed)
        throw new InvalidOperationException("Cannot select starting town before viewing the prologue.");

    var town = World.GetTown(startingTownId);

    var e = new StartingTownSelected
    {
        StartingTownId = startingTownId
    };

    Apply(e);
    _uncommittedEvents.Add(e);
}
```

- [ ] **Step 4: Modify CompleteGameStart to not take startingTownId**

The current `CompleteGameStart(TownId startingTownId, Wallet?, Inventory?)` should become `CompleteGameStart(Wallet?, Inventory?)` — it reads the starting town from the `StartingTownSelected` event (stored in a field). Add a field to track the selected town:

```csharp
private TownId? _selectedStartingTownId;

// In Apply(StartingTownSelected):
_selectedStartingTownId = e.StartingTownId;

// In CompleteGameStart:
public void CompleteGameStart(
    WildBunch.Domain.Economy.Wallet? wallet = null,
    DomainInventory? inventory = null)
{
    if (StartFlowPhase == StartFlowPhase.GameStarted)
        return;

    if (StartFlowPhase != StartFlowPhase.StartingTownSelected)
        throw new InvalidOperationException("Cannot complete game start before selecting a starting town.");

    var startingTownId = _selectedStartingTownId
        ?? throw new InvalidOperationException("No starting town selected.");
    var startingTown = World.GetTown(startingTownId);
    var startingHealth = StartingHealthFor(GameDifficulty);
    var resolvedWallet = wallet ?? WildBunch.Domain.Economy.Wallet.Starting(25m);
    var resolvedInventory = inventory ?? DomainInventory.Empty();

    var e = new GameStarted
    {
        PlayerName = Player.Name,
        StartingTownId = startingTown.Id,
        StartingTownName = startingTown.Name,
        StartingHealth = startingHealth,
        StartingWallet = resolvedWallet.Cash,
        StartingInventoryItems = resolvedInventory.Items.ToArray(),
        GameDifficulty = GameDifficulty,
        SaltSource = SaltSource,
        GameEntropy = GameEntropy,
        SeedCode = SeedCode
    };

    Apply(e);
    _uncommittedEvents.Add(e);
}
```

Also update the `StartNew` shortcut method to call `SelectStartingTown` then `CompleteGameStart` instead of the old flow.

- [ ] **Step 5: Update ApplyEvent dispatcher**

In `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`, add cases for the new events:

```csharp
case WorldGenerated wg:
    session.Apply(wg);
    break;
case StartingTownSelected sts:
    session.Apply(sts);
    break;
```

- [ ] **Step 6: Update event serialization**

In `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`, add to `ResolveEventType`:

```csharp
nameof(WorldGenerated) => typeof(WorldGenerated),
nameof(StartingTownSelected) => typeof(StartingTownSelected),
```

- [ ] **Step 7: Build**

Run: `dotnet build`
Expected: There will be compile errors in callers of `StartSetup` (missing `saltSource` param) and `CompleteGameStart` (extra `startingTownId` param). Fix these in Task 5.

- [ ] **Step 8: Commit (may not compile yet — Task 5 fixes the callers)**

Do not commit yet. Proceed to Task 5.

## Task 5: Update Application Handlers and Factory

**Files:**
- Modify: `src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs`
- Modify: `src/WildBunch.Application/Games/Commands/CompleteGameStartHandler.cs`
- Modify: `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`

- [ ] **Step 1: Update SeededNewGameFactory.ResolveWorld to expose saltSource**

The `ResolveWorld` method needs to return the `SaltSource` so the setup handler can pass it to `StartSetup`. Change the return type:

```csharp
public (World World, CaseFile CaseFile, string SeedCodeText, SaltSource SaltSource) ResolveWorld(
    string playerName,
    GameDifficulty gameDifficulty,
    string? setupSeedCode,
    GameEntropy gameEntropy)
{
    var seed = ParseOrGenerateSeed(setupSeedCode);
    var seedWorld = SeedWorldResolver.Resolve(seed);
    var difficulty = DifficultyEnvelope.For(gameDifficulty);
    var entropy = EntropyPolicy.For(gameEntropy);
    var resolvedSetup = _setupResolver.Resolve(
        seedWorld, difficulty, entropy, playerChosenStartingTownId: null);

    return (resolvedSetup.World, resolvedSetup.CaseFile, resolvedSetup.SeedCodeText, resolvedSetup.SaltSource);
}
```

- [ ] **Step 2: Update CompletePlayerSetupHandler**

In `src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs`, update the `ResolveWorld` call and `StartSetup` call:

```csharp
var (world, caseFile, seedCodeText, saltSource) = _newGameFactory.ResolveWorld(
    command.PlayerName, command.GameDifficulty, command.SeedCode, command.GameEntropy);

var newSession = GameSession.StartSetup(
    command.PlayerName,
    world,
    caseFile,
    command.GameDifficulty,
    command.GameEntropy,
    seedCodeText,
    saltSource);
```

- [ ] **Step 3: Update CompleteGameStartHandler**

In `src/WildBunch.Application/Games/Commands/CompleteGameStartHandler.cs`, add `SelectStartingTown` before `CompleteGameStart`:

```csharp
var startingTownId = new TownId(command.StartingTownId);

session.SelectStartingTown(startingTownId);

var (wallet, inventory) = _newGameFactory.ResolveStartingResources(session.GameDifficulty);

session.CompleteGameStart(wallet, inventory);
```

- [ ] **Step 4: Build**

Run: `dotnet build`
Expected: PASS — all compile errors resolved

- [ ] **Step 5: Run non-integration tests**

Run: `dotnet test tests/WildBunch.GameContent.Tests/; dotnet test tests/WildBunch.Domain.Tests/; dotnet test tests/WildBunch.Application.Tests/`
Expected: Some tests may fail if they call `StartSetup` or `CompleteGameStart` with the old signatures. Fix them inline.

- [ ] **Step 6: Commit**

`git add -A; git commit -m "feat: emit WorldGenerated and StartingTownSelected events in setup flow, fix callers"`

## Task 6: Write Event Round-Trip Tests

**Files:**
- Create: `tests/WildBunch.Domain.Tests/WorldGeneratedEventTests.cs`
- Create: `tests/WildBunch.Domain.Tests/StartingTownSelectedEventTests.cs`

- [ ] **Step 1: Write WorldGenerated event tests**

Create `tests/WildBunch.Domain.Tests/WorldGeneratedEventTests.cs`:

```csharp
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Domain.Tests;

public sealed class WorldGeneratedEventTests
{
    [Fact]
    public void WorldGenerated_CarriesWorldSnapshotThatReconstructsToIdenticalWorld()
    {
        var town = new Town(new TownId("t1"), "Test Town", TownServices.Telegraph, TownProsperity.Boomtown, MapX: 100, MapY: 200, IsOutlier: false);
        var trail = new Trail(new TrailId("trail-0-1"), new TownId("t1"), new TownId("t2"), TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m);
        var world = new World(new[] { town }, new[] { trail });

        var snapshot = WorldSnapshot.FromDomain(world);
        var evt = new WorldGenerated
        {
            SeedCode = "test-seed",
            SaltSource = SaltSource.CreateFixed("test-salt"),
            GameEntropy = GameEntropy.Boring,
            World = snapshot
        };

        var reconstructed = evt.World.ToDomain();

        Assert.Single(reconstructed.Towns);
        var reconstructedTown = reconstructed.Towns.First();
        Assert.Equal("t1", reconstructedTown.Id.Value);
        Assert.Equal("Test Town", reconstructedTown.Name);
        Assert.Equal(TownServices.Telegraph, reconstructedTown.Services);
        Assert.Equal(TownProsperity.Boomtown, reconstructedTown.Prosperity);
        Assert.Equal(100, reconstructedTown.MapX);
        Assert.Equal(200, reconstructedTown.MapY);
        Assert.False(reconstructedTown.IsOutlier);
    }

    [Fact]
    public void WorldGenerated_PreservesIsOutlierFlag()
    {
        var outlier = new Town(new TownId("t-outlier"), "Outlier Town", TownServices.None, TownProsperity.Poor, MapX: 500, MapY: 500, IsOutlier: true);
        var world = new World(new[] { outlier }, Array.Empty<Trail>());

        var snapshot = WorldSnapshot.FromDomain(world);
        var reconstructed = snapshot.ToDomain();

        Assert.True(reconstructed.Towns.First().IsOutlier);
    }
}
```

- [ ] **Step 2: Write StartingTownSelected event tests**

Create `tests/WildBunch.Domain.Tests/StartingTownSelectedEventTests.cs`:

```csharp
using WildBunch.Domain.Events;
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Domain.Tests;

public sealed class StartingTownSelectedEventTests
{
    [Fact]
    public void StartingTownSelected_CarriesTownId()
    {
        var townId = new TownId("hardpan");
        var evt = new StartingTownSelected { StartingTownId = townId };
        Assert.Equal("hardpan", evt.StartingTownId.Value);
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/WildBunch.Domain.Tests/ --filter "WorldGeneratedEventTests|StartingTownSelectedEventTests"`
Expected: PASS

- [ ] **Step 4: Run full non-integration test suite**

Run: `dotnet test tests/WildBunch.GameContent.Tests/; dotnet test tests/WildBunch.Domain.Tests/; dotnet test tests/WildBunch.Application.Tests/`
Expected: PASS

- [ ] **Step 5: Commit**

`git add -A; git commit -m "test: add WorldGenerated and StartingTownSelected event round-trip tests"`

## Definition of Done

- [ ] `WorldGenerated` event exists and carries `WorldSnapshot` (towns + trails with `IsOutlier` and `Prosperity`)
- [ ] `StartingTownSelected` event exists and carries `TownId`
- [ ] `StartFlowPhase` has `StartingTownSelected` phase between `PrologueViewed` and `GameStarted`
- [ ] `GameSession.StartSetup` emits both `PlayerSetupCompleted` and `WorldGenerated`
- [ ] `Apply(WorldGenerated)` sets `World` from the event payload
- [ ] `Apply(StartingTownSelected)` records the town choice and transitions to `StartingTownSelected` phase
- [ ] `CompleteGameStart` no longer takes `startingTownId` — it reads from the `StartingTownSelected` event
- [ ] `SelectStartingTown` method exists and emits `StartingTownSelected`
- [ ] Event replay dispatcher handles both new events
- [ ] Event serializer can serialize/deserialize both new events
- [ ] `TownSnapshot` persists `IsOutlier` and `Prosperity`
- [ ] Non-integration tests pass
- [ ] The world is reconstructable from the event stream alone (not just the snapshot cache)
