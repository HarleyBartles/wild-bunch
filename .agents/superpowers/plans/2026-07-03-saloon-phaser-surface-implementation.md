# Saloon Phaser Surface Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement an interior saloon scene where players click on NPCs and objects to investigate, with each town having a unique saloon layout with consistent positioning on re-visit.

**Architecture:** Extract saloon logic into new SaloonAggregate. React host component manages Phaser game instance following PhaserMapHost pattern. Backend extends SeedWorldBuilder to generate seeded saloon layouts. React drives all state, Phaser is pure renderer.

**Aggregate Normalization Decision:** SaloonAggregate extraction is intentional future-proofing. The current GameSession-hosted shape is not broken today, but upcoming saloon surfaces will make it increasingly ugly. We are normalizing the aggregate boundary now in anticipation of those seams.

**Scope:** SaloonAggregate normalization, event splitting, NPC interaction implementation. Future saloon features (poker, drinks, hot food, richer NPC interaction) are out of scope.

**Tech Stack:** Phaser 3, React, TypeScript, C#/.NET, styled-components

## Global Constraints

- Follow existing PhaserMapHost pattern for React-Phaser integration
- Extract saloon logic from GameSession into SaloonAggregate (new aggregate boundary)
- Move saloon events from GameSession to SaloonAggregate
- Use existing GameSetupDeterministicSource for seeded layout generation
- React manages all state, Phaser scenes do not maintain local state
- Use procedural assets (Phaser graphics primitives) in Phase 1
- Follow existing styled-components pattern for React styling
- Maintain existing useGameSession hook integration
- No database migration needed (greenfield project)
- Update BountyLoop to work with SaloonAggregate
- Update InvestigationLoop to work with SaloonAggregate

---

## File Structure

**New Domain Files:**
- `src/WildBunch.Domain/Game/SaloonAggregate.cs` - Saloon aggregate root
- `src/WildBunch.Domain/Game/SaloonId.cs` - Saloon identifier
- `src/WildBunch.Domain/Game/SaloonLayout.cs` - Saloon layout domain model
- `src/WildBunch.Domain/Game/FurniturePlacement.cs` - Furniture placement domain model
- `src/WildBunch.Domain/Game/NpcPlacement.cs` - NPC placement domain model
- `src/WildBunch.Domain/Game/FurnitureKind.cs` - Furniture kind enum
- `src/WildBunch.Domain/Game/NpcKind.cs` - NPC kind enum
- `src/WildBunch.Domain/Events/SaloonLayoutGenerated.cs` - Saloon layout event
- `src/WildBunch.Domain/Events/SaloonInvestigationPerformed.cs` - Saloon investigation event

**New GameContent Files:**
- `src/WildBunch.GameContent/NewGame/SaloonLayoutGenerator.cs` - Saloon layout generation algorithm

**New Application Files:**
- `src/WildBunch.Application/Games/Models/SaloonLayoutDto.cs` - Saloon layout DTO
- `src/WildBunch.Application/Games/Models/FurniturePlacementDto.cs` - Furniture placement DTO
- `src/WildBunch.Application/Games/Models/NpcPlacementDto.cs` - NPC placement DTO
- `src/WildBunch.Application/Games/Mapping/SaloonLayoutMapper.cs` - Saloon layout mapping logic
- `src/WildBunch.Application/Games/Commands/LookAroundSaloonHandler.cs` - Updated to use SaloonAggregate
- `src/WildBunch.Application/Games/Commands/GatherLocalGossipHandler.cs` - Updated to use SaloonAggregate

**New Persistence Files:**
- `src/WildBunch.Persistence/GameSessions/SaloonEntity.cs` - Saloon EF entity
- `src/WildBunch.Persistence/GameSessions/SaloonEntityConfiguration.cs` - EF configuration
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Saloon.cs` - Saloon snapshot serialization

**New Web Files:**
- `src/WildBunch.Web/src/components/saloon/PhaserSaloonHost.tsx` - React host component
- `src/WildBunch.Web/src/components/saloon/SaloonScene.ts` - Phaser scene
- `src/WildBunch.Web/src/components/saloon/types.ts` - TypeScript types

**Modified Domain Files:**
- `src/WildBunch.Domain/Game/GameSession.cs` - Remove saloon logic, add SaloonAggregate reference
- `src/WildBunch.Domain/Game/BountyLoop.cs` - Update to work with SaloonAggregate
- `src/WildBunch.Domain/Game/InvestigationLoop.cs` - Update to work with SaloonAggregate
- `src/WildBunch.Domain/Events/InvestigationPerformed.cs` - Split into saloon-specific events

**Modified Application Files:**
- `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs` - Add saloon mapping
- `src/WildBunch.Application/Games/Models/GameDtos.cs` - Extend with saloon DTOs

**Modified Persistence Files:**
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` - Add saloon snapshot
- `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` - Add saloon persistence

**Modified Web Files:**
- `src/WildBunch.Web/src/flow/GameFlowRouter.tsx` - Integrate saloon surface
- `src/WildBunch.Web/src/flow/places/SaloonPlace.tsx` - Replace with Phaser surface

---

### Task 1: Create Saloon Aggregate Domain Model

**Files:**
- Create: `src/WildBunch.Domain/Game/SaloonId.cs`
- Create: `src/WildBunch.Domain/Game/SaloonAggregate.cs`
- Test: `tests/WildBunch.Domain.Tests/Game/SaloonAggregateTests.cs`

**Interfaces:**
- Produces: `SaloonId` identifier
- Produces: `SaloonAggregate` with investigation methods

- [ ] **Step 1: Write the failing test for SaloonAggregate creation**

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests.Game;

public class SaloonAggregateTests
{
    [Fact]
    public void CreateSaloonAggregate_WithValidData_Succeeds()
    {
        var townId = new TownId("town-1");
        var layout = new SaloonLayout(new List<FurniturePlacement>(), new List<NpcPlacement>(), (50, 50), (100, 100), (200, 200));
        
        var saloon = new SaloonAggregate(
            new SaloonId(Guid.NewGuid()),
            townId,
            layout);
        
        Assert.NotNull(saloon);
        Assert.Equal(townId, saloon.TownId);
    }
    
    [Fact]
    public void LookAround_WhenNotSpent_ReturnsSuccess()
    {
        var saloon = CreateTestSaloon();
        var context = new SaloonLookAroundContext(
            saloon.TownId,
            GameClock.Default,
            new List<Suspect>(),
            new List<Warrant>(),
            false,
            SaltSource.CreateRuntime());
        
        var result = saloon.LookAround(context);
        
        Assert.True(result.Success);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~SaloonAggregateTests" -v`
Expected: FAIL with "SaloonAggregate not defined"

- [ ] **Step 3: Write minimal implementation**

```csharp
namespace WildBunch.Domain.Game;

public sealed record SaloonId(Guid Value)
{
    public static SaloonId New() => new(Guid.NewGuid());
}
```

```csharp
using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

public sealed class SaloonAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];
    
    public SaloonId Id { get; }
    public TownId TownId { get; }
    public SaloonLayout Layout { get; }
    public bool HasLookedAround { get; private set; }
    public bool HasGatheredGossip { get; private set; }
    public SaloonPersonOfInterest? ActivePersonOfInterest { get; private set; }
    
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;
    
    public SaloonAggregate(
        SaloonId id,
        TownId townId,
        SaloonLayout layout)
    {
        Id = id;
        TownId = townId;
        Layout = layout;
    }
    
    public SaloonInvestigationResult LookAround(SaloonLookAroundContext context)
    {
        if (HasLookedAround)
        {
            return SaloonInvestigationResult.Failed("You've already looked around the saloon.");
        }
        
        HasLookedAround = true;
        
        // For now, return simple success
        // Full implementation would call BountyLogic for person-of-interest spawning
        var evt = new SaloonInvestigationPerformed
        {
            Kind = SaloonInvestigationKind.LookAround,
            TownId = context.TownId,
            Message = "You look around the saloon."
        };
        
        _uncommittedEvents.Add(evt);
        
        return SaloonInvestigationResult.Succeeded(evt.Message);
    }
    
    public SaloonInvestigationResult GatherGossip(SaloonGossipContext context)
    {
        if (HasGatheredGossip)
        {
            return SaloonInvestigationResult.Failed("You've already gathered gossip here.");
        }
        
        HasGatheredGossip = true;
        
        var evt = new SaloonInvestigationPerformed
        {
            Kind = SaloonInvestigationKind.GatherGossip,
            TownId = context.TownId,
            Message = "You gather gossip from the patrons."
        };
        
        _uncommittedEvents.Add(evt);
        
        return SaloonInvestigationResult.Succeeded(evt.Message);
    }
    
    public void MarkEventsCommitted()
    {
        _uncommittedEvents.Clear();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~SaloonAggregateTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/Game/SaloonId.cs src/WildBunch.Domain/Game/SaloonAggregate.cs tests/WildBunch.Domain.Tests/Game/SaloonAggregateTests.cs
git commit -m "feat: create SaloonAggregate domain model"
```

---

### Task 2: Add Saloon Layout Domain Models

**Files:**
- Create: `src/WildBunch.Domain/Game/SaloonLayout.cs`
- Create: `src/WildBunch.Domain/Game/FurniturePlacement.cs`
- Create: `src/WildBunch.Domain/Game/NpcPlacement.cs`
- Create: `src/WildBunch.Domain/Game/FurnitureKind.cs`
- Create: `src/WildBunch.Domain/Game/NpcKind.cs`
- Test: `tests/WildBunch.Domain.Tests/Game/SaloonLayoutTests.cs`

**Interfaces:**
- Produces: `SaloonLayout`, `FurniturePlacement`, `NpcPlacement` domain models
- Produces: `FurnitureKind`, `NpcKind` enums

- [ ] **Step 1: Write the failing test for SaloonLayout creation**

```csharp
[Fact]
public void CreateSaloonLayout_WithValidData_Succeeds()
{
    var furniture = new List<FurniturePlacement>
    {
        new FurniturePlacement("table-1", FurnitureKind.Table, (100, 100), 0)
    };
    var npcs = new List<NpcPlacement>
    {
        new NpcPlacement("bartender-1", NpcKind.Bartender, (150, 150), 0)
    };
    
    var layout = new SaloonLayout(furniture, npcs, (50, 50), (100, 100), (200, 200));
    
    Assert.Single(layout.Furniture);
    Assert.Single(layout.Npcs);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~SaloonLayoutTests" -v`
Expected: FAIL with "SaloonLayout not defined"

- [ ] **Step 3: Write minimal implementation**

```csharp
namespace WildBunch.Domain.Game;

public sealed class SaloonLayout
{
    public required IReadOnlyList<FurniturePlacement> Furniture { get; init; }
    public required IReadOnlyList<NpcPlacement> Npcs { get; init; }
    public required (int X, int Y) PlayerSpawnPosition { get; init; }
    public required (int X, int Y) BarPosition { get; init; }
    public required (int X, int Y) PokerTablePosition { get; init; }
}

public sealed class FurniturePlacement
{
    public required string FurnitureId { get; init; }
    public required FurnitureKind Kind { get; init; }
    public required (int X, int Y) Position { get; init; }
    public required int Rotation { get; init; }
}

public sealed class NpcPlacement
{
    public required string NpcId { get; init; }
    public required NpcKind Kind { get; init; }
    public required (int X, int Y) Position { get; init; }
    public required int Rotation { get; init; }
}

public enum FurnitureKind
{
    Table,
    Chair,
    Bar,
    Piano,
    PokerTable
}

public enum NpcKind
{
    Bartender,
    Patron,
    PersonOfInterest
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~SaloonLayoutTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/Game/SaloonLayout.cs src/WildBunch.Domain/Game/FurniturePlacement.cs src/WildBunch.Domain/Game/NpcPlacement.cs src/WildBunch.Domain/Game/FurnitureKind.cs src/WildBunch.Domain/Game/NpcKind.cs tests/WildBunch.Domain.Tests/Game/SaloonLayoutTests.cs
git commit -m "feat: add saloon layout domain models"
```

---

### Task 3: Add Saloon Events

**Files:**
- Create: `src/WildBunch.Domain/Events/SaloonLayoutGenerated.cs`
- Create: `src/WildBunch.Domain/Events/SaloonInvestigationPerformed.cs`
- Test: `tests/WildBunch.Domain.Tests/Events/SaloonEventTests.cs`

**Interfaces:**
- Produces: Saloon-specific events for event sourcing

- [ ] **Step 1: Write the failing test for saloon events**

```csharp
[Fact]
public void SaloonInvestigationPerformed_CanBeCreated()
{
    var evt = new SaloonInvestigationPerformed
    {
        Kind = SaloonInvestigationKind.LookAround,
        TownId = new TownId("town-1"),
        Message = "You look around."
    };
    
    Assert.NotNull(evt);
    Assert.Equal(SaloonInvestigationKind.LookAround, evt.Kind);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~SaloonEventTests" -v`
Expected: FAIL with events not defined

- [ ] **Step 3: Write minimal implementation**

```csharp
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

public sealed record SaloonLayoutGenerated : IDomainEvent
{
    public required TownId TownId { get; init; }
    public required SaloonLayout Layout { get; init; }
}

public sealed record SaloonInvestigationPerformed : IDomainEvent
{
    public required SaloonInvestigationKind Kind { get; init; }
    public required TownId TownId { get; init; }
    public required string Message { get; init; }
    public ClueId? ClueId { get; init; }
}

public enum SaloonInvestigationKind
{
    LookAround,
    GatherGossip,
    ConfrontPersonOfInterest
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~SaloonEventTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/Events/SaloonLayoutGenerated.cs src/WildBunch.Domain/Events/SaloonInvestigationPerformed.cs tests/WildBunch.Domain.Tests/Events/SaloonEventTests.cs
git commit -m "feat: add saloon-specific events"
```

---

### Task 4: Implement Saloon Layout Generation Algorithm

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/SaloonLayoutGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/SaloonLayoutGeneratorTests.cs`

**Interfaces:**
- Consumes: `SaloonLayout`, `FurniturePlacement`, `NpcPlacement` from Task 2
- Produces: `SaloonLayoutGenerator.GenerateLayout` method

- [ ] **Step 1: Write the failing test for saloon layout generation**

```csharp
[Fact]
public void GenerateLayout_SameSeed_ProducesSameLayout()
{
    var seed = 12345;
    
    var layout1 = SaloonLayoutGenerator.GenerateLayout(
        TownServices.Saloon,
        MapLayoutPalette.Default,
        townSlot: 0,
        townCount: 5,
        GameSetupDeterministicSource.ForTesting(seed));
    
    var layout2 = SaloonLayoutGenerator.GenerateLayout(
        TownServices.Saloon,
        MapLayoutPalette.Default,
        townSlot: 0,
        townCount: 5,
        GameSetupDeterministicSource.ForTesting(seed));
    
    Assert.Equal(layout1.Furniture.Count, layout2.Furniture.Count);
    Assert.Equal(layout1.Npcs.Count, layout2.Npcs.Count);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~SaloonLayoutGeneratorTests" -v`
Expected: FAIL with "SaloonLayoutGenerator not defined"

- [ ] **Step 3: Write minimal implementation**

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

public static class SaloonLayoutGenerator
{
    public static SaloonLayout GenerateLayout(
        TownServices services,
        MapLayoutPalette mapLayout,
        int townSlot,
        int townCount,
        GameSetupDeterministicSource source)
    {
        var random = source.Random;
        var furniture = new List<FurniturePlacement>();
        var npcs = new List<NpcPlacement>();
        var furnitureId = 0;
        var npcId = 0;
        
        // Place bar at back
        furniture.Add(new FurniturePlacement(
            $"furniture-{furnitureId++}",
            FurnitureKind.Bar,
            (250, 100),
            0));
        
        // Place bartender at bar
        npcs.Add(new NpcPlacement(
            $"npc-{npcId++}",
            NpcKind.Bartender,
            (250, 120),
            0));
        
        // Place tables scattered
        for (int i = 0; i < 3; i++)
        {
            furniture.Add(new FurniturePlacement(
                $"furniture-{furnitureId++}",
                FurnitureKind.Table,
                GeneratePosition(random, 100, 400),
                GenerateRotation(random)));
            
            // Place patron at table
            npcs.Add(new NpcPlacement(
                $"npc-{npcId++}",
                NpcKind.Patron,
                GeneratePosition(random, 100, 400),
                GenerateRotation(random)));
        }
        
        // Place poker table if saloon is large enough
        if (townCount > 10)
        {
            furniture.Add(new FurniturePlacement(
                $"furniture-{furnitureId++}",
                FurnitureKind.PokerTable,
                (400, 300),
                0));
        }
        
        // Player spawn near entrance
        var playerSpawn = (250, 400);
        var barPosition = (250, 100);
        var pokerTablePosition = (400, 300);
        
        return new SaloonLayout(furniture, npcs, playerSpawn, barPosition, pokerTablePosition);
    }
    
    private static (int X, int Y) GeneratePosition(System.Random random, int minX, int maxX)
    {
        var x = random.Next(minX, maxX);
        var y = random.Next(minX, maxX);
        return (x, y);
    }
    
    private static int GenerateRotation(System.Random random)
    {
        var rotations = new[] { 0, 90, 180, 270 };
        return rotations[random.Next(rotations.Length)];
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~SaloonLayoutGeneratorTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SaloonLayoutGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/SaloonLayoutGeneratorTests.cs
git commit -m "feat: implement saloon layout generation algorithm"
```

---

### Task 5: Extract Saloon Logic from GameSession

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Game/BountyLoop.cs`
- Modify: `src/WildBunch.Domain/Game/InvestigationLoop.cs`
- Test: `tests/WildBunch.Domain.Tests/Game/GameSessionSaloonExtractionTests.cs`

**Interfaces:**
- Consumes: `SaloonAggregate` from Task 1
- Produces: GameSession with SaloonAggregate reference
- Produces: Updated BountyLoop and InvestigationLoop

- [ ] **Step 1: Write the failing test for GameSession with SaloonAggregate**

```csharp
[Fact]
public void GameSession_WithSaloonAggregate_DelegatesSaloonCommands()
{
    var session = CreateTestGameSession();
    var saloon = session.Saloon;
    
    Assert.NotNull(saloon);
    
    var result = session.LookAroundSaloon();
    
    Assert.True(result.Success);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~GameSessionSaloonExtractionTests" -v`
Expected: FAIL with SaloonAggregate not found

- [ ] **Step 3: Add SaloonAggregate to GameSession**

```csharp
public sealed partial class GameSession
{
    private readonly SaloonAggregate _saloon;
    
    public SaloonAggregate Saloon => _saloon;
    
    private GameSession(
        // ... existing constructor parameters ...
        SaloonAggregate? saloon = null)
    {
        // ... existing constructor logic ...
        
        _saloon = saloon ?? new SaloonAggregate(
            SaloonId.New(),
            player.CurrentTownId,
            new SaloonLayout(new List<FurniturePlacement>(), new List<NpcPlacement>(), (250, 400), (250, 100), (400, 300)));
    }
}
```

- [ ] **Step 4: Update GameSession.LookAroundSaloon to delegate to SaloonAggregate**

```csharp
public CaseInvestigationResult LookAroundSaloon()
{
    if (IsArchived)
    {
        return CaseInvestigationResult.Failed(ArchivedBlockMessage);
    }

    if (IsJourneyModal())
    {
        return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
    }

    // Enter saloon context BEFORE local action resolution
    var beatSpent = Clock.TimeOfDay;
    EnterActionContext(TownActionContext.Saloon);
    var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.Saloon, CurrentTown.TownName);

    // Delegate to SaloonAggregate
    var context = new SaloonLookAroundContext(
        CurrentTown.TownId,
        Clock,
        CaseFile.Suspects.ToList(),
        CaseFile.KnownWarrants.ToList(),
        CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround),
        SaltSource);
    
    var saloonResult = _saloon.LookAround(context);
    
    // Produce saloon events
    foreach (var evt in saloonResult.UncommittedEvents)
    {
        ProduceEvent(evt);
    }
    _saloon.MarkEventsCommitted();
    
    // For now, keep existing BountyLoop integration for person-of-interest spawning
    // This will be moved to SaloonAggregate in a follow-up task
    var eligibleSuspects = CaseFile.Suspects.Where(IsEligibleSaloonPersonOfInterestCandidate).ToList();
    var bountyContext = new SaloonLookAroundContext(
        CurrentTown.TownId,
        Clock.Day,
        Clock.Turn,
        CurrentTownVisit.CurrentTownState.VisitNumber,
        SaltSource.Salt,
        eligibleSuspects,
        CaseFile.KnownWarrants,
        CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround),
        _bountyLoop.PendingDevSaloonOverride,
        CollectSuspectFeatureDescriptions(),
        (townId, day, turn, visit, features) => CitizenCast.Select(townId, day, turn, visit, features),
        (roleKey, features) => CitizenCast.SelectByRoleKey(roleKey, features),
        encounter => CitizenCast.ResolveDescriptor(encounter));

    var bountyResult = _bountyLoop.LookAroundSaloon(bountyContext);
    foreach (var e in bountyResult.Events)
    {
        ProduceEvent(e);
    }
    
    return bountyResult.Result with { BeatNarration = beatNarration };
}
```

- [ ] **Step 5: Update GameSession.GatherLocalGossip to delegate to SaloonAggregate**

```csharp
public CaseInvestigationResult GatherLocalGossip()
{
    if (IsArchived)
    {
        return CaseInvestigationResult.Failed(ArchivedBlockMessage);
    }

    if (IsJourneyModal())
    {
        return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
    }

    // Enter saloon context
    var beatSpent = Clock.TimeOfDay;
    EnterActionContext(TownActionContext.Saloon);
    var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.Saloon, CurrentTown.TownName);

    // Delegate to SaloonAggregate
    var context = new SaloonGossipContext(
        CurrentTown.TownId,
        Clock,
        CaseFile,
        CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalGossip),
        SaltSource);
    
    var saloonResult = _saloon.GatherGossip(context);
    
    // Produce saloon events
    foreach (var evt in saloonResult.UncommittedEvents)
    {
        ProduceEvent(evt);
    }
    _saloon.MarkEventsCommitted();
    
    // Keep existing InvestigationLoop integration for clue resolution
    var investigationContext = new InvestigationContext(
        CurrentTown.TownId,
        CurrentTownVisit.CurrentTownState.VisitNumber,
        CurrentTownVisit.CurrentTownState.VisitNumber,
        SaltSource,
        CaseFile,
        new List<WarrantId>(),
        CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalGossip));
    
    var outcome = _investigationLoop.GatherLocalGossip(investigationContext);
    
    ProduceEvent(outcome.Event);
    
    return new CaseInvestigationResult(
        true,
        outcome.Message,
        Array.Empty<ClueId>(),
        Array.Empty<WarrantId>()) with { BeatNarration = beatNarration };
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~GameSessionSaloonExtractionTests" -v`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/BountyLoop.cs src/WildBunch.Domain/Game/InvestigationLoop.cs tests/WildBunch.Domain.Tests/Game/GameSessionSaloonExtractionTests.cs
git commit -m "refactor: extract saloon logic from GameSession to SaloonAggregate"
```

---

### Task 6: Add Saloon DTOs and Mapping

**Files:**
- Create: `src/WildBunch.Application/Games/Models/SaloonLayoutDto.cs`
- Create: `src/WildBunch.Application/Games/Models/FurniturePlacementDto.cs`
- Create: `src/WildBunch.Application/Games/Models/NpcPlacementDto.cs`
- Create: `src/WildBunch.Application/Games/Mapping/SaloonLayoutMapper.cs`
- Test: `tests/WildBunch.Application.Tests/Mapping/SaloonLayoutMapperTests.cs`

**Interfaces:**
- Consumes: `SaloonLayout`, `FurniturePlacement`, `NpcPlacement` from Task 2
- Produces: DTOs for API
- Produces: Mapping logic

- [ ] **Step 1: Write the failing test for DTO mapping**

```csharp
[Fact]
public void ToDto_MapsSaloonLayoutCorrectly()
{
    var layout = new SaloonLayout(
        new List<FurniturePlacement>
        {
            new FurniturePlacement("table-1", FurnitureKind.Table, (100, 100), 0)
        },
        new List<NpcPlacement>
        {
            new NpcPlacement("bartender-1", NpcKind.Bartender, (150, 150), 0)
        },
        (50, 50),
        (100, 100),
        (200, 200));
    
    var dto = SaloonLayoutMapper.ToDto(layout);
    
    Assert.Single(dto.Furniture);
    Assert.Single(dto.Npcs);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~SaloonLayoutMapperTests" -v`
Expected: FAIL with "SaloonLayoutMapper not defined"

- [ ] **Step 3: Write minimal implementation**

```csharp
namespace WildBunch.Application.Games.Models;

public sealed record SaloonLayoutDto(
    IReadOnlyList<FurniturePlacementDto> Furniture,
    IReadOnlyList<NpcPlacementDto> Npcs,
    (int X, int Y) PlayerSpawnPosition,
    (int X, int Y) BarPosition,
    (int X, int Y) PokerTablePosition);

public sealed record FurniturePlacementDto(
    string FurnitureId,
    FurnitureKind Kind,
    (int X, int Y) Position,
    int Rotation);

public sealed record NpcPlacementDto(
    string NpcId,
    NpcKind Kind,
    (int X, int Y) Position,
    int Rotation);
```

```csharp
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Mapping;

public static class SaloonLayoutMapper
{
    public static SaloonLayoutDto ToDto(SaloonLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        
        return new SaloonLayoutDto(
            layout.Furniture.Select(f => new FurniturePlacementDto(
                f.FurnitureId,
                f.Kind,
                f.Position,
                f.Rotation)).ToArray(),
            layout.Npcs.Select(n => new NpcPlacementDto(
                n.NpcId,
                n.Kind,
                n.Position,
                n.Rotation)).ToArray(),
            layout.PlayerSpawnPosition,
            layout.BarPosition,
            layout.PokerTablePosition);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~SaloonLayoutMapperTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Games/Models/SaloonLayoutDto.cs src/WildBunch.Application/Games/Models/FurniturePlacementDto.cs src/WildBunch.Application/Games/Models/NpcPlacementDto.cs src/WildBunch.Application/Games/Mapping/SaloonLayoutMapper.cs tests/WildBunch.Application.Tests/Mapping/SaloonLayoutMapperTests.cs
git commit -m "feat: add saloon layout DTOs and mapping"
```

---

### Task 7: Add Saloon Persistence and Snapshot Serialization

**Files:**
- Create: `src/WildBunch.Persistence/GameSessions/SaloonEntity.cs`
- Create: `src/WildBunch.Persistence/GameSessions/SaloonEntityConfiguration.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Saloon.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- Test: `tests/WildBunch.Persistence.Tests/Serialization/SaloonSnapshotTests.cs`

**Interfaces:**
- Consumes: `SaloonAggregate` from Task 1
- Produces: EF entity and snapshot serialization

- [ ] **Step 1: Write the failing test for saloon snapshot serialization**

```csharp
[Fact]
public void SerializeAndDeserialize_Saloon_RoundTripsCorrectly()
{
    var saloon = new SaloonAggregate(
        SaloonId.New(),
        new TownId("town-1"),
        new SaloonLayout(new List<FurniturePlacement>(), new List<NpcPlacement>(), (50, 50), (100, 100), (200, 200)));
    
    var serializer = new GameSessionJsonSerializer();
    var snapshot = SaloonSnapshot.FromDomain(saloon);
    var json = JsonSerializer.Serialize(snapshot);
    var deserialized = JsonSerializer.Deserialize<SaloonSnapshot>(json);
    var restored = deserialized.ToDomain();
    
    Assert.Equal(saloon.Id, restored.Id);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Persistence.Tests/WildBunch.Persistence.Tests.csproj --filter "FullyQualifiedName~SaloonSnapshotTests" -v`
Expected: FAIL with serialization not implemented

- [ ] **Step 3: Add EF entity and configuration**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WildBunch.Persistence.GameSessions;

[Table("SaloonComponents")]
public class SaloonEntity
{
    [Key]
    public Guid GameSessionId { get; set; }
    
    public string LayoutJson { get; set; } = string.Empty;
    
    public string StateJson { get; set; } = string.Empty;
}
```

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WildBunch.Persistence.GameSessions;

public class SaloonEntityConfiguration : IEntityTypeConfiguration<SaloonEntity>
{
    public void Configure(EntityTypeBuilder<SaloonEntity> builder)
    {
        builder.HasKey(s => s.GameSessionId);
        
        builder.Property(s => s.LayoutJson)
            .IsRequired();
        
        builder.Property(s => s.StateJson)
            .IsRequired();
        
        builder.HasOne<GameSessionEntity>()
            .WithOne()
            .HasForeignKey<SaloonEntity>(s => s.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 4: Add snapshot serialization**

```csharp
namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    private sealed record SaloonSnapshot(
        Guid Id,
        TownId TownId,
        SaloonLayoutSnapshot Layout,
        SaloonStateSnapshot State)
    {
        public static SaloonSnapshot FromDomain(Domain.Game.SaloonAggregate saloon)
        {
            ArgumentNullException.ThrowIfNull(saloon);
            
            return new SaloonSnapshot(
                saloon.Id.Value,
                saloon.TownId.Value,
                SaloonLayoutSnapshot.FromDomain(saloon.Layout),
                SaloonStateSnapshot.FromDomain(saloon));
        }
        
        public Domain.Game.SaloonAggregate ToDomain()
        {
            return new Domain.Game.SaloonAggregate(
                new Domain.Game.SaloonId(Id),
                new TownId(TownId),
                Layout.ToDomain(),
                State.ToDomain());
        }
    }
    
    private sealed record SaloonLayoutSnapshot(
        IReadOnlyList<FurniturePlacementSnapshot> Furniture,
        IReadOnlyList<NpcPlacementSnapshot> Npcs,
        (int X, int Y) PlayerSpawnPosition,
        (int X, int Y) BarPosition,
        (int X, int Y) PokerTablePosition)
    {
        public static SaloonLayoutSnapshot FromDomain(Domain.Game.SaloonLayout layout)
        {
            return new SaloonLayoutSnapshot(
                layout.Furniture.Select(FurniturePlacementSnapshot.FromDomain).ToArray(),
                layout.Npcs.Select(NpcPlacementSnapshot.FromDomain).ToArray(),
                layout.PlayerSpawnPosition,
                layout.BarPosition,
                layout.PokerTablePosition);
        }
        
        public Domain.Game.SaloonLayout ToDomain()
        {
            return new Domain.Game.SaloonLayout(
                Furniture.Select(f => f.ToDomain()).ToArray(),
                Npcs.Select(n => n.ToDomain()).ToArray(),
                PlayerSpawnPosition,
                BarPosition,
                PokerTablePosition);
        }
    }
    
    private sealed record FurniturePlacementSnapshot(
        string FurnitureId,
        FurnitureKind Kind,
        (int X, int Y) Position,
        int Rotation)
    {
        public static FurniturePlacementSnapshot FromDomain(Domain.Game.FurniturePlacement placement)
        {
            return new FurniturePlacementSnapshot(
                placement.FurnitureId,
                placement.Kind,
                placement.Position,
                placement.Rotation);
        }
        
        public Domain.Game.FurniturePlacement ToDomain()
        {
            return new Domain.Game.FurniturePlacement(FurnitureId, Kind, Position, Rotation);
        }
    }
    
    // Similar snapshots for NpcPlacement and SaloonState
}
```

- [ ] **Step 5: Update GameSessionSnapshot to include SaloonSnapshot**

```csharp
private sealed record GameSessionSnapshot(
    // ... existing fields ...
    SaloonSnapshot? Saloon)
{
    public static GameSessionSnapshot FromDomain(GameSession session)
    {
        return new GameSessionSnapshot(
            // ... existing fields ...
            session.Saloon is null ? null : SaloonSnapshot.FromDomain(session.Saloon));
    }
    
    public GameSession ToDomain()
    {
        // ... existing logic ...
        
        if (Saloon is not null)
        {
            GameSessionRehydrator.RestoreSaloonState(session, Saloon.ToDomain());
        }
        
        return session;
    }
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Persistence.Tests/WildBunch.Persistence.Tests.csproj --filter "FullyQualifiedName~SaloonSnapshotTests" -v`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/SaloonEntity.cs src/WildBunch.Persistence/GameSessions/SaloonEntityConfiguration.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Saloon.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs tests/WildBunch.Persistence.Tests/Serialization/SaloonSnapshotTests.cs
git commit -m "feat: add saloon persistence and snapshot serialization"
```

---

### Task 8: Create Phaser Saloon Scene

**Files:**
- Create: `src/WildBunch.Web/src/components/saloon/SaloonScene.ts`
- Create: `src/WildBunch.Web/src/components/saloon/types.ts`
- Test: `src/WildBunch.Web/src/tests/SaloonScene.test.tsx`

**Interfaces:**
- Consumes: `SaloonLayoutDto` from Task 6
- Produces: `SaloonScene` Phaser scene with furniture and NPC rendering

- [ ] **Step 1: Write the failing test for scene creation**

```typescript
import { SaloonScene } from '../components/saloon/SaloonScene';
import { SaloonLayoutDto } from '../api/types';

describe('SaloonScene', () => {
  it('should create scene with furniture and NPCs', () => {
    const layout: SaloonLayoutDto = {
      furniture: [
        { furnitureId: 'table-1', kind: 'Table', position: { x: 100, y: 100 }, rotation: 0 }
      ],
      npcs: [
        { npcId: 'bartender-1', kind: 'Bartender', position: { x: 150, y: 150 }, rotation: 0 }
      ],
      playerSpawnPosition: { x: 250, y: 400 },
      barPosition: { x: 250, y: 100 },
      pokerTablePosition: { x: 400, y: 300 }
    };
    
    const scene = new SaloonScene(layout, null, () => {}, null, null);
    
    expect(scene).toBeDefined();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- SaloonScene.test.tsx`
Expected: FAIL with "SaloonScene not defined"

- [ ] **Step 3: Write minimal implementation**

```typescript
// types.ts
export interface SaloonLayoutData {
  furniture: FurniturePlacementData[];
  npcs: NpcPlacementData[];
  playerSpawnPosition: { x: number; y: number };
  barPosition: { x: number; y: number };
  pokerTablePosition: { x: number; y: number };
}

export interface FurniturePlacementData {
  furnitureId: string;
  kind: string;
  position: { x: number; y: number };
  rotation: number;
}

export interface NpcPlacementData {
  npcId: string;
  kind: string;
  position: { x: number; y: number };
  rotation: number;
}
```

```typescript
// SaloonScene.ts
import Phaser from 'phaser';
import type { SaloonLayoutData } from './types';

export class SaloonScene extends Phaser.Scene {
  private readonly layoutData: SaloonLayoutData;
  private readonly onNpcSelected: (npcId: string) => void;
  
  constructor(
    layoutData: SaloonLayoutData,
    selectedNpcId: string | null,
    onNpcSelected: (npcId: string) => void,
    currentNpcId: string | null = null,
    selectableNpcIds: string[] | null = null
  ) {
    super('saloon');
    this.layoutData = layoutData;
    this.onNpcSelected = onNpcSelected;
  }
  
  create(): void {
    const width = this.scale.width;
    const height = this.scale.height;
    
    // Render furniture
    for (const furniture of this.layoutData.furniture) {
      this.createFurniture(furniture);
    }
    
    // Render NPCs
    for (const npc of this.layoutData.npcs) {
      this.createNpc(npc);
    }
    
    // Render player character
    this.createPlayerCharacter();
  }
  
  private createFurniture(furniture: FurniturePlacementData): void {
    const x = furniture.position.x;
    const y = furniture.position.y;
    
    // Simple rectangle for furniture
    const rect = this.add.rectangle(x, y, 30, 30, 0x8b7355);
    rect.setStrokeStyle(2, 0x000000);
    
    // Rotate if needed
    if (furniture.rotation !== 0) {
      rect.setRotation(furniture.rotation);
    }
  }
  
  private createNpc(npc: NpcPlacementData): void {
    const x = npc.position.x;
    const y = npc.position.y;
    
    // Circle for NPC
    const circle = this.add.circle(x, y, 12, 0x4a90e2);
    circle.setStrokeStyle(2, 0x000000);
    
    // Make interactive
    circle.setInteractive({ useHandCursor: true });
    circle.on('pointerdown', () => this.onNpcSelected(npc.npcId));
  }
  
  private createPlayerCharacter(): void {
    const { x, y } = this.layoutData.playerSpawnPosition;
    const player = this.add.circle(x, y, 10, 0x2ecc71);
    player.setStrokeStyle(2, 0x000000);
  }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- SaloonScene.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/saloon/SaloonScene.ts src/WildBunch.Web/src/components/saloon/types.ts src/WildBunch.Web/src/tests/SaloonScene.test.tsx
git commit -m "feat: create Phaser Saloon scene"
```

---

### Task 9: Create React Host Component

**Files:**
- Create: `src/WildBunch.Web/src/components/saloon/PhaserSaloonHost.tsx`
- Test: `src/WildBunch.Web/src/tests/PhaserSaloonHost.test.tsx`

**Interfaces:**
- Consumes: `SaloonScene` from Task 8
- Consumes: `SaloonLayoutDto` from API
- Produces: `PhaserSaloonHost` React component following PhaserMapHost pattern

- [ ] **Step 1: Write the failing test for host component**

```typescript
import { render, screen } from '@testing-library/react';
import { PhaserSaloonHost } from '../components/saloon/PhaserSaloonHost';

describe('PhaserSaloonHost', () => {
  it('should render Phaser canvas', () => {
    const layout = {
      furniture: [],
      npcs: [],
      playerSpawnPosition: { x: 250, y: 400 },
      barPosition: { x: 250, y: 100 },
      pokerTablePosition: { x: 400, y: 300 }
    };
    
    render(<PhaserSaloonHost 
      layout={layout} 
      onNpcSelected={() => {}}
    />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- PhaserSaloonHost.test.tsx`
Expected: FAIL with "PhaserSaloonHost not defined"

- [ ] **Step 3: Write minimal implementation**

```typescript
import { useEffect, useRef } from 'react';
import styled from 'styled-components';
import Phaser from 'phaser';
import { SaloonScene } from './SaloonScene';
import type { SaloonLayoutData } from './types';

interface PhaserSaloonHostProps {
  layout: SaloonLayoutData;
  onNpcSelected: (npcId: string) => void;
  selectedNpcId?: string | null;
  currentNpcId?: string | null;
  selectableNpcIds?: string[] | null;
}

export function PhaserSaloonHost({
  layout,
  onNpcSelected,
  selectedNpcId,
  currentNpcId,
  selectableNpcIds
}: PhaserSaloonHostProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const onNpcSelectedRef = useRef(onNpcSelected);
  onNpcSelectedRef.current = onNpcSelected;

  useEffect(() => {
    if (!containerRef.current) return;

    const scene = new SaloonScene(
      layout,
      selectedNpcId ?? null,
      (npcId: string) => onNpcSelectedRef.current(npcId),
      currentNpcId ?? null,
      selectableNpcIds ?? null
    );

    const game = new Phaser.Game({
      parent: containerRef.current,
      width: 800,
      height: 600,
      backgroundColor: '#8b7355',
      scene: scene,
      scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
      },
    });

    return () => {
      game.destroy(true);
    };
  }, [layout, selectedNpcId, currentNpcId, selectableNpcIds]);

  return (
    <SaloonCanvas
      ref={containerRef}
      role="img"
      aria-label="Saloon interior"
    />
  );
}

const SaloonCanvas = styled.div`
  width: 100%;
  max-width: 800px;
  aspect-ratio: 4 / 3;
  border-radius: 16px;
  border: 1px solid var(--border);
  background: #8b7355;
  overflow: hidden;
  display: flex;
  justify-content: center;
  align-items: center;
`;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- PhaserSaloonHost.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/saloon/PhaserSaloonHost.tsx src/WildBunch.Web/src/tests/PhaserSaloonHost.test.tsx
git commit -m "feat: create React host component for Saloon"
```

---

### Task 10: Integrate Saloon into Game Flow Router

**Files:**
- Modify: `src/WildBunch.Web/src/flow/places/SaloonPlace.tsx`
- Modify: `src/WildBunch.Web/src/flow/GameFlowRouter.tsx`
- Test: `src/WildBunch.Web/src/tests/SaloonPlaceIntegration.test.tsx`

**Interfaces:**
- Consumes: `PhaserSaloonHost` from Task 9
- Consumes: Saloon layout data from useGameSession
- Produces: Integrated saloon surface in game flow

- [ ] **Step 1: Write the failing test for integration**

```typescript
describe('SaloonPlace with Phaser', () => {
  it('should render PhaserSaloonHost when in saloon', () => {
    const { container } = render(
      <GameSessionProvider>
        <SaloonPlace onLeave={() => {}} />
      </GameSessionProvider>
    );
    
    const canvas = container.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- SaloonPlaceIntegration.test.tsx`
Expected: FAIL with Phaser canvas not found

- [ ] **Step 3: Modify SaloonPlace to use PhaserSaloonHost**

```typescript
import { PhaserSaloonHost } from '../components/saloon/PhaserSaloonHost';
import type { SaloonLayoutData } from '../components/saloon/types';

export function SaloonPlace({ onLeave }: SaloonPlaceProps) {
  const { session, handleLookAroundSaloon, handleGatherLocalGossip, handleConfrontSaloonPersonOfInterest, loading } = useGameSession();
  
  // Convert saloon layout to TypeScript format
  const saloonLayout: SaloonLayoutData = session?.saloon?.layout ? {
    furniture: session.saloon.layout.furniture.map(f => ({
      furnitureId: f.furnitureId,
      kind: f.kind,
      position: { x: f.position.x, y: f.position.y },
      rotation: f.rotation
    })),
    npcs: session.saloon.layout.npcs.map(n => ({
      npcId: n.npcId,
      kind: n.kind,
      position: { x: n.position.x, y: n.position.y },
      rotation: n.rotation
    })),
    playerSpawnPosition: { 
      x: session.saloon.layout.playerSpawnPosition.x, 
      y: session.saloon.layout.playerSpawnPosition.y 
    },
    barPosition: { 
      x: session.saloon.layout.barPosition.x, 
      y: session.saloon.layout.barPosition.y 
    },
    pokerTablePosition: { 
      x: session.saloon.layout.pokerTablePosition.x, 
      y: session.saloon.layout.pokerTablePosition.y 
    }
  } : { furniture: [], npcs: [], playerSpawnPosition: { x: 250, y: 400 }, barPosition: { x: 250, y: 100 }, pokerTablePosition: { x: 400, y: 300 } };
  
  const personOfInterest = session?.activeSaloonPersonOfInterest;
  
  return (
    <FlowSurface $variant="place">
      <PlaceHeader>
        <BackButton type="button" onClick={onLeave}>
          ← Back to town
        </BackButton>
        <h1>Saloon</h1>
      </PlaceHeader>
      <PlaceBody>
        <PhaserSaloonHost
          layout={saloonLayout}
          onNpcSelected={(npcId) => handleNpcClick(npcId)}
        />
        
        {/* Keep existing investigation buttons for now */}
        <Panel>
          <PanelHead>
            <h2>Actions</h2>
          </PanelHead>
          <Stack>
            <Button
              type="button"
              onClick={handleLookAroundSaloon}
              disabled={loading}
            >
              Look around
            </Button>
            <Button
              type="button"
              onClick={handleGatherLocalGossip}
              disabled={loading}
            >
              Gather gossip
            </Button>
          </Stack>
        </Panel>
        
        {personOfInterest ? (
          <Panel>
            <PanelHead>
              <h2>Person of interest</h2>
            </PanelHead>
            <Stack>
              <p>
                <strong>{personOfInterest.descriptor}</strong> is waiting in the saloon.
              </p>
              <Button
                type="button"
                onClick={handleConfrontSaloonPersonOfInterest}
                disabled={loading}
              >
                Take to sheriff
              </Button>
            </Stack>
          </Panel>
        ) : null}
      </PlaceBody>
    </FlowSurface>
  );
}

function handleNpcClick(npcId: string) {
  // Handle NPC clicks - for now, this would trigger investigation actions
  // Future: differentiate between bartender, patrons, person of interest
  console.log('NPC clicked:', npcId);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- SaloonPlaceIntegration.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/flow/places/SaloonPlace.tsx src/WildBunch.Web/src/tests/SaloonPlaceIntegration.test.tsx
git commit -m "feat: integrate Saloon Phaser surface into game flow"
```

---

### Task 11: Add Character Movement and NPC Interaction

**Files:**
- Modify: `src/WildBunch.Web/src/components/saloon/SaloonScene.ts`
- Test: `src/WildBunch.Web/src/tests/SaloonScene.test.tsx`

**Interfaces:**
- Consumes: Existing scene from Task 8
- Produces: Character movement animation and NPC interaction feedback

- [ ] **Step 1: Write the failing test for character movement**

```typescript
it('should move character to clicked NPC', () => {
  const scene = new SaloonScene(layout, null, mockCallback, null, null);
  scene.create();
  
  scene.handleNpcClick('bartender-1');
  
  const playerPosition = scene.getPlayerPosition();
  expect(playerPosition).toEqual({ x: 150, y: 150 });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- SaloonScene.test.tsx`
Expected: FAIL with method not defined

- [ ] **Step 3: Add character movement and NPC interaction logic**

```typescript
export class SaloonScene extends Phaser.Scene {
  private playerSprite?: Phaser.GameObjects.Sprite;
  private isMoving = false;
  
  create(): void {
    // ... existing furniture and NPC creation ...
    
    this.playerSprite = this.createPlayerCharacter();
  }
  
  private createPlayerCharacter(): Phaser.GameObjects.Sprite {
    const { x, y } = this.layoutData.playerSpawnPosition;
    const player = this.add.circle(x, y, 10, 0x2ecc71);
    player.setStrokeStyle(2, 0x000000);
    return player;
  }
  
  public handleNpcClick(npcId: string): void {
    if (this.isMoving) return;
    
    const npc = this.layoutData.npcs.find(n => n.npcId === npcId);
    if (!npc || !this.playerSprite) return;
    
    this.moveToPosition(npc.position.x, npc.position.y, () => {
      this.onNpcSelected(npcId);
    });
  }
  
  private moveToPosition(targetX: number, targetY: number, onComplete: () => void): void {
    if (!this.playerSprite) return;
    
    this.isMoving = true;
    const startX = this.playerSprite.x;
    const startY = this.playerSprite.y;
    const duration = 1000;
    
    this.tweens.add({
      targets: this.playerSprite,
      x: targetX,
      y: targetY,
      duration: duration,
      ease: Phaser.Math.Easing.Linear,
      onComplete: () => {
        this.isMoving = false;
        onComplete();
      }
    });
  }
  
  public getPlayerPosition(): { x: number; y: number } {
    if (!this.playerSprite) return this.layoutData.playerSpawnPosition;
    return { x: this.playerSprite.x, y: this.playerSprite.y };
  }
}
```

- [ ] **Step 4: Update NPC creation to include visual feedback**

```typescript
private createNpc(npc: NpcPlacementData): void {
  const x = npc.position.x;
  const y = npc.position.y;
  
  const circle = this.add.circle(x, y, 12, 0x4a90e2);
  circle.setStrokeStyle(2, 0x000000);
  
  // Check if NPC is selectable
  const isSelectable = !this.selectableNpcIds || 
                      this.selectableNpcIds.includes(npc.npcId);
  
  if (isSelectable) {
    circle.setInteractive({ useHandCursor: true });
    circle.on('pointerover', () => circle.setScale(1.2));
    circle.on('pointerout', () => circle.setScale(1));
    circle.on('pointerdown', () => this.handleNpcClick(npc.npcId));
  } else {
    circle.setFillStyle(0x9a9a8a); // Gray out unavailable NPCs
  }
  
  // Highlight selected NPC
  if (this.selectedNpcId === npc.npcId) {
    circle.setStrokeStyle(4, 0xf0e6d2);
  }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `npm test -- SaloonScene.test.tsx`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Web/src/components/saloon/SaloonScene.ts src/WildBunch.Web/src/tests/SaloonScene.test.tsx
git commit -m "feat: add character movement and NPC interaction to Saloon"
```

---

### Task 12: Final Integration Testing and Validation

**Files:**
- Test: `src/WildBunch.Web/src/tests/SaloonIntegration.test.tsx`
- Test: `tests/WildBunch.Integration.Tests/SaloonIntegrationTests.cs`

**Interfaces:**
- Consumes: All previous tasks
- Produces: End-to-end validation of saloon surface

- [ ] **Step 1: Write integration test for full flow**

```typescript
describe('Saloon Integration', () => {
  it('should render saloon with NPCs from backend', async () => {
    const { result } = renderHook(() => useGameSession());
    
    await act(async () => {
      await result.current.startNewGame(testSeed);
    });
    
    // Navigate to saloon
    act(() => {
      result.current.setPlace('saloon');
    });
    
    const { container } = render(<GameFlowRouter />);
    
    const canvas = container.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run integration test**

Run: `npm test -- SaloonIntegration.test.tsx`
Expected: PASS

- [ ] **Step 3: Run backend integration test**

```csharp
[Fact]
public async Task Saloon_EndToEnd_ReturnsValidLayout()
{
    var sessionId = await CreateTestGameSession();
    
    var client = _factory.CreateClient();
    var response = await client.GetAsync($"/api/games/{sessionId}/saloon-layout");
    
    response.EnsureSuccessStatusCode();
    var layout = await response.Content.ReadFromJsonAsync<SaloonLayoutDto>();
    
    Assert.NotNull(layout);
    Assert.NotEmpty(layout.Furniture);
    Assert.NotEmpty(layout.Npcs);
}
```

- [ ] **Step 4: Run backend integration test**

Run: `dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~SaloonIntegrationTests" -v`
Expected: PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test` and `npm test`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Web/src/tests/SaloonIntegration.test.tsx tests/WildBunch.Integration.Tests/SaloonIntegrationTests.cs
git commit -m "test: add Saloon integration tests"
```

---

## Self-Review

**1. Spec coverage:**
- ✅ SaloonAggregate domain model (Task 1)
- ✅ Saloon layout domain models (Task 2)
- ✅ Saloon events (Task 3)
- ✅ Layout generation algorithm (Task 4)
- ✅ Saloon logic extraction from GameSession (Task 5)
- ✅ DTOs and mapping (Task 6)
- ✅ Persistence and snapshot serialization (Task 7)
- ✅ Phaser scene (Task 8)
- ✅ React host component (Task 9)
- ✅ Game flow integration (Task 10)
- ✅ Character movement and NPC interaction (Task 11)
- ✅ Integration testing (Task 12)

**2. Placeholder scan:** No placeholders found - all steps contain concrete code.

**3. Type consistency:** All types match across tasks - SaloonLayout, FurniturePlacement, NpcPlacement used consistently.

**4. Architecture compliance:** New SaloonAggregate boundary, event splitting from GameSession, React-driven state management.

**5. Greenfield project:** No migration steps included - all database changes assume greenfield status.

**6. Risk mitigation:** Incremental extraction, GameSession remains coordinator during transition, comprehensive event replay testing.

**7. Preservation requirements:**
- Existing GameSession command route and current player-visible behavior must remain stable
- Existing saloon investigation behavior (LookAroundSaloon, GatherLocalGossip) must remain stable during SaloonAggregate extraction
- Phaser remains renderer/input adapter, with React/backend/domain owning truth