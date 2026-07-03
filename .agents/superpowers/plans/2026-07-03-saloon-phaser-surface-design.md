# Saloon Phaser Surface Design

## Overview
Interior saloon scene where players click on NPCs and objects to investigate, buy drinks/meals, and play poker. Each town has a unique saloon layout with consistent positioning on re-visit. The surface uses Phaser for rendering while React manages state and backend integration.

## Architecture

### React Host Component
**File**: `src/WildBunch.Web/src/components/saloon/PhaserSaloonHost.tsx`

Follows the existing `PhaserMapHost` pattern:
- Manages Phaser game instance lifecycle (creation, updates, destruction)
- Receives saloon layout data from backend via DTOs
- Passes saloon data to Phaser scene via constructor
- Handles interaction callbacks (NPC clicks, zone interactions)
- Integrates with existing `useGameSession` hook for state management

### Phaser Scene
**File**: `src/WildBunch.Web/src/components/saloon/SaloonScene.ts`

Extends `Phaser.Scene`:
- Renders top-down saloon interior with furniture and NPCs
- Manages player character sprite and movement
- Handles click interactions on NPCs and interaction zones
- Auto-walks character to clicked destinations
- Visual feedback for available interactions

### Aggregate Architecture
**New Aggregate**: `SaloonAggregate`

**File**: `src/WildBunch.Domain/Game/SaloonAggregate.cs`

```csharp
public sealed class SaloonAggregate
{
    public SaloonId Id { get; }
    public TownId TownId { get; }
    public SaloonLayout Layout { get; }
    public SaloonState State { get; }
    
    // Investigation state
    public bool HasLookedAround { get; }
    public bool HasGatheredGossip { get; }
    public SaloonPersonOfInterest? ActivePersonOfInterest { get; }
    
    // Economy state (future)
    public decimal PlayerTab { get; }
    public int DrinksPurchased { get; }
    public int MealsPurchased { get; }
    
    // Poker state (future)
    public PokerGameState? PokerGame { get; }
    
    // Command methods
    public SaloonInvestigationResult LookAround(SaloonLookAroundContext context);
    public SaloonInvestigationResult GatherGossip(SaloonGossipContext context);
    public SaloonConfrontationResult ConfrontPersonOfInterest(SaloonConfrontationContext context);
    
    // Future methods
    public SaloonPurchaseResult BuyDrink(SaloonPurchaseContext context);
    public SaloonPurchaseResult BuyMeal(SaloonPurchaseContext context);
    public PokerGameResult StartPoker(PokerStartContext context);
    public PokerGameResult PlayPokerHand(PokerHandContext context);
}
```

### Data Source
**Backend Extension**: New saloon layout data structures:

```csharp
// Domain model
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

**DTO Extension**:
```csharp
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

## Scene Layout

### Visual Style
- **Perspective**: Top-down 2D view
- **Aesthetic**: Western paper/ink style with sepia tones, interior lighting
- **Background**: Wooden floor texture with wall boundaries
- **Furniture**: Simple paper/ink style furniture shapes
- **NPCs**: Simple paper/ink style character sprites with distinctive features

### Zone Layout
- **Bar Area**: Bartender NPC, drink/meal interaction zone
- **Table Areas**: Patron NPCs, gossip interaction zones
- **Poker Table**: Poker interaction zone (future)
- **Person of Interest**: Special NPC with confrontation interaction
- **Entrance**: Player spawn position, exit zone

### NPC Rendering
- **Bartender**: Distinctive apron sprite, always at bar
- **Patrons**: Randomized appearance, seated at tables
- **Person of Interest**: Distinctive appearance, highlighted when present
- **Available NPCs**: Highlighted with warm sepia tones
- **Unavailable NPCs**: Grayed out or not rendered

### Character Movement
- **Spawn position**: Near entrance
- **Movement**: Straight-line interpolation to clicked NPC/zone
- **Speed**: Moderate walking pace
- **Pathfinding**: Simple direct path (saloon interiors are open layouts)

## Data Flow

### Backend Data Generation
1. **World Generation**: Extend `SeedWorldBuilder` to generate saloon layouts during world creation
2. **Layout Algorithm**: Seeded random placement based on saloon size and town prosperity
3. **Persistence**: Store layout data in `TownDefinition` as part of world generation
4. **Consistency**: Same town always has same saloon layout across visits

### React Integration
1. **Load Phase**: React fetches saloon layout data via new endpoint `/api/games/{sessionId}/saloon-layout`
2. **Render Phase**: React passes layout data to Phaser scene constructor
3. **Update Phase**: React re-renders Phaser scene when person of interest changes
4. **Interaction Phase**: Phaser scene calls React callbacks for NPC/zone clicks

### Backend Commands
1. **NPC Click**: React sends command to backend (e.g., `GatherGossipCommand`)
2. **State Update**: Backend processes command via `SaloonAggregate`, returns updated session state
3. **Scene Update**: React updates Phaser scene with new state (e.g., NPC availability changes)

## Domain Integration

### SaloonAggregate Events
**New Events**:
```csharp
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

public sealed record DrinkPurchased : IDomainEvent
{
    public required TownId TownId { get; init; }
    public required decimal Cost { get; init; }
    public required decimal WalletAfter { get; init; }
}

public sealed record MealPurchased : IDomainEvent
{
    public required TownId TownId { get; init; }
    public required decimal Cost { get; init; }
    public required int HealthRestored { get; init; }
    public required decimal WalletAfter { get; init; }
}

public sealed record PokerHandPlayed : IDomainEvent
{
    public required TownId TownId { get; init; }
    public required PokerHandResult Result { get; init; }
    public required decimal AmountWon { get; init; }
    public required decimal WalletAfter { get; init; }
}
```

### Investigation Source Mapping
- `SaloonLookAround` → Click saloon area/look around zone
- `LocalGossip` → Click patron NPCs
- `PersonOfInterest` → Click person-of-interest NPC
- Future: `BuyDrink` → Click bar zone
- Future: `BuyMeal` → Click bar zone
- Future: `PlayPoker` → Click poker table zone

### Layout Generation Algorithm
**File**: `src/WildBunch.GameContent/NewGame/SaloonLayoutGenerator.cs`

```csharp
public static class SaloonLayoutGenerator
{
    public static SaloonLayout GenerateLayout(
        TownServices services,
        MapLayoutPalette mapLayout,
        int townSlot,
        int townCount,
        GameSetupDeterministicSource source)
    {
        // Use seeded random to place furniture
        // Place bar at back of saloon
        // Place tables scattered around
        // Place poker table in corner (if saloon large enough)
        // Place bartender at bar
        // Place patron NPCs at tables
        // Return deterministic layout based on seed
    }
}
```

## Technical Decisions

### Layout Generation Strategy
- **Seeded Random**: Use existing `GameSetupDeterministicSource` for reproducible layouts
- **Size-Based**: Larger towns get larger saloons with more furniture
- **Prosperity-Based**: More prosperous towns get better furniture/decorations
- **Consistency**: Same town always has same saloon layout across visits

### Asset Strategy (Phase 1)
- **Procedural Generation**: Use Phaser graphics primitives (rectangles, circles, text)
- **Furniture Sprites**: Simple colored rectangles with shape variations
- **NPC Sprites**: Simple circles with color coding and distinctive features
- **Background**: Solid color with simple line rendering for walls/floor

### Asset Strategy (Phase 2 - Deferred)
- **Artist-Created Assets**: Detailed furniture sprites with western aesthetic
- **NPC Animation**: Idle animations, walking animations
- **Environmental Details**: Lighting effects, shadows, atmosphere
- **Sound Effects**: Ambient saloon sounds, interaction sounds

### State Management
- **React-Driven**: React manages all state, Phaser is pure renderer
- **No Local State**: Phaser scenes don't maintain state, only render what React passes
- **Callback Pattern**: Phaser scenes call React callbacks for interactions
- **Reactivity**: React `useEffect` triggers Phaser scene updates when DTOs change

### Performance Considerations
- **Single Instance**: One Phaser game instance per saloon surface
- **Cleanup**: Proper destruction of Phaser instance when leaving saloon
- **Memory**: Reuse textures/sprites across saloons where possible
- **Rendering**: Simple 2D rendering, minimal performance impact

## API Changes

### New Endpoints
```csharp
// Get saloon layout for current town
GET /api/games/{sessionId}/saloon-layout
Response: SaloonLayoutDto

// Saloon investigation commands (existing, moved to SaloonAggregate)
POST /api/games/{sessionId}/look-around-saloon
POST /api/games/{sessionId}/gather-local-gossip
POST /api/games/{sessionId}/confront-saloon-person-of-interest

// Future economy commands
POST /api/games/{sessionId}/buy-drink
POST /api/games/{sessionId}/buy-meal

// Future poker commands
POST /api/games/{sessionId}/start-poker
POST /api/games/{sessionId}/play-poker-hand
```

### DTO Changes
- Add `SaloonLayoutDto` to shared types
- Add `FurniturePlacementDto` to shared types
- Add `NpcPlacementDto` to shared types
- Add `SaloonInvestigationKind` enum to shared types
- Add future poker/economy DTOs

## Migration Strategy

### Phase 1: Data Model (Highest Complexity)
1. Create `SaloonAggregate` domain model
2. Add saloon-specific events (`SaloonInvestigationPerformed`, etc.)
3. Extend `SeedWorldBuilder` to generate saloon layouts
4. Add saloon layout generation algorithm
5. Update snapshot format to include saloon state
6. **No Migration Needed**: Greenfield project, can drop and rebuild database

### Phase 2: Backend Refactoring (High Complexity)
1. Move saloon logic from `GameSession` to `SaloonAggregate`
2. Move saloon events from GameSession to SaloonAggregate
3. Update `BountyLoop` to work with `SaloonAggregate`
4. Add saloon layout endpoint
5. Update repository to persist saloon aggregate
6. **No Migration Needed**: Greenfield project, can drop and rebuild database

### Phase 3: Frontend Integration (Medium Complexity)
1. Create `PhaserSaloonHost` component
2. Create `SaloonScene` Phaser scene
3. Integrate with existing `GameFlowRouter`
4. Replace React saloon panels with Phaser surface
5. **No Migration Needed**: UI change, no data migration needed

### Phase 4: Future Features (Low Complexity)
1. Add drink/meal economy features
2. Add poker game features
3. Extend saloon layout generation for new features
4. **No Migration Needed**: Feature additions, no breaking changes

## Testing Strategy

### Domain Tests
- Test saloon aggregate command methods
- Test saloon event production and application
- Test layout generation determinism
- Test investigation source mapping
- Test future poker/economy logic

### Integration Tests
- Test saloon layout endpoint returns valid data
- Test saloon aggregate persistence across session load/save
- Test React-Phaser data flow
- Test NPC interaction callbacks
- Test command handler integration with new aggregate

### Frontend Tests
- Test Phaser scene creation/destruction
- Test NPC click callbacks
- Test character movement
- Test visual feedback (available/unavailable NPCs)
- Test zone interaction detection

### Phaser Tests
- Test scene rendering with different layouts
- Test interaction zones (NPC click detection)
- Test character movement interpolation
- Test cleanup/memory management
- Test future poker UI rendering

## Success Criteria

### Functional Requirements
- Players can click NPCs to investigate
- Each town has unique, consistent saloon layout
- NPC availability reflected visually
- Character walks to clicked NPC/zone
- Layout persists across saloon revisits
- Integration with existing investigation flow
- Future: Drink/meal economy works
- Future: Poker game works

### Non-Functional Requirements
- Performance: Scene renders in < 100ms
- Memory: No memory leaks on scene destruction
- Accessibility: Keyboard navigation support
- Responsiveness: Works on different screen sizes
- Maintainability: Clear separation of React/Phaser responsibilities

## Open Questions

1. **Aggregate Extraction**: How to handle existing saloon state in `TownVisitState` during migration?
2. **Event Splitting**: Which existing events should become SaloonAggregate events?
3. **BountyLoop Coupling**: How to decouple `BountyLoop` from `GameSession` for saloon logic?
4. **Poker Complexity**: How complex should poker game be (simple vs full poker rules)?
5. **Economy Balance**: How to price drinks/meals for game balance?

## Dependencies

### Existing Code
- `PhaserMapHost` pattern for React-Phaser integration
- `SeedWorldBuilder` for coordinate derivation
- `TownAggregate` for town service mapping
- `BountyLoop` for person-of-interest logic
- `InvestigationLoop` for gossip logic
- `useGameSession` hook for state management
- `GameFlowRouter` for navigation

### New Code Required
- `SaloonAggregate` domain model
- Saloon-specific events
- `SaloonLayoutGenerator` for layout generation
- `PhaserSaloonHost` React component
- `SaloonScene` Phaser scene
- Saloon layout API endpoint
- Saloon aggregate repository
- Saloon snapshot serialization

## Risks and Mitigations

### Risk: Aggregate Extraction Complexity
**Mitigation**: Incremental migration, keep GameSession as coordinator during transition, thorough event replay testing

### Risk: BountyLoop Coupling
**Mitigation**: Extract saloon-specific logic from BountyLoop, pass SaloonAggregate as context, maintain backward compatibility

### Risk: Event Stream Splitting
**Mitigation**: Clear event ownership boundaries, comprehensive event replay tests, migration path for existing saves

### Risk: Poker Game Complexity
**Mitigation**: Start with simple poker variant, defer complex rules to future iterations, focus on core loop first

### Risk: Asset Pipeline
**Mitigation**: Phase 1 procedural assets, defer artist assets until layout patterns are stable, reuse furniture sprites across saloons

### Risk: Performance with Many NPCs
**Mitigation**: Limit NPC count based on saloon size, use sprite pooling, test with maximum NPC count (10 NPCs)
