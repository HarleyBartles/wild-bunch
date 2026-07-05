# Town Hub Phaser Surface Design

## Overview
Top-down town layout where players click on buildings to navigate. Each town has a unique building arrangement based on its available services, with layouts that persist across revisits. The surface uses Phaser for rendering while React manages state and backend integration.

## Architecture

### React Host Component
**File**: `src/WildBunch.Web/src/components/town-hub/PhaserTownHubHost.tsx`

Follows the existing `PhaserMapHost` pattern:
- Manages Phaser game instance lifecycle (creation, updates, destruction)
- Receives town layout data from backend via DTOs
- Passes town data to Phaser scene via constructor
- Handles interaction callbacks (building clicks, navigation requests)
- Integrates with existing `useGameSession` hook for state management

### Phaser Scene
**File**: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`

Extends `Phaser.Scene`:
- Renders top-down town layout with building sprites
- Manages player character sprite and movement
- Handles click interactions on buildings
- Auto-walks character to clicked destinations
- Visual feedback for available/unavailable buildings

### Data Source
**Backend Extension**: Extend the `Town` record in `WorldModels.cs` with a `TownLayout? Layout` property, and extend `TownDto` in `GameDtos.cs` with a `TownLayoutDto? Layout` property. There is no `TownDefinition` type — the town model is the `Town` record:

```csharp
// Domain model
public sealed class TownLayout
{
    public required IReadOnlyList<BuildingPlacement> Buildings { get; init; }
    public required (int X, int Y) PlayerSpawnPosition { get; init; }
}

public sealed class BuildingPlacement
{
    public required string BuildingId { get; init; }
    public required BuildingKind Kind { get; init; }
    public required (int X, int Y) Position { get; init; }
    public required int Rotation { get; init; } // 0, 90, 180, 270 degrees
}

public enum BuildingKind
{
    // Baseline navigation buildings — always present in every town layout,
    // derived from the existing TownHubSurface card grid (AvailableActionKind-driven).
    Store,
    Sheriff,
    Saloon,
    Trailhead,
    // Service-driven optional buildings — derived from TownServices flags on the Town record.
    Telegraph,
    // Future building types (Stable, Doctor, etc.) will be added here as services are introduced.
    Stable,
    Doctor
}
```

> **Note:** `BuildingKind` is a visual representation enum for rendering and click-to-navigate routing, not a service flag. `TownServices` is the domain service flag. See the Building and Navigation Model section in the implementation plan for the full mapping.

**DTO Extension**:
```csharp
public sealed record TownLayoutDto(
    IReadOnlyList<BuildingPlacementDto> Buildings,
    (int X, int Y) PlayerSpawnPosition);

public sealed record BuildingPlacementDto(
    string BuildingId,
    BuildingKind Kind,
    (int X, int Y) Position,
    int Rotation);
```

## Scene Layout

### Visual Style
- **Perspective**: Top-down 2D view
- **Aesthetic**: Western paper/ink style with sepia tones
- **Background**: Parchment texture with dirt road rendering
- **Buildings**: Paper/ink style building footprints with service-specific icons
- **Character**: Simple paper/ink style character sprite

### Building Rendering
- **Available buildings**: Highlighted with warm sepia tones, interactive cursor
- **Unavailable buildings**: Grayed out, non-interactive
- **Current location**: Building with distinctive border/glow
- **Building sprites**: Simple geometric shapes with icons (store = box, sheriff = star, saloon = drink glass, etc.)

### Character Movement
- **Spawn position**: Center of town or last known location
- **Movement**: Straight-line interpolation to clicked building
- **Speed**: Moderate walking pace (1-2 seconds for typical town distances)
- **Pathfinding**: Simple direct path (no collision detection needed for open town layout)

## Data Flow

### Backend Data Generation
1. **World Generation**: Integrate `TownLayoutGenerator` into the world construction pipeline by post-processing the `World` returned by `SeedWorldFactory.CreateWorld` in `MapGenerator.Generate` (where `GameSetupDeterministicSource` is in scope). Do NOT change `SeedWorldFactory.CreateWorld`'s signature — attach layouts using `with` expressions on the returned `World.Towns`. `SeedWorldFactory.CreateCanonicalWorld` (start-screen map) does NOT need layouts.
2. **Layout Algorithm**: Seeded random placement using existing `GameSetupDeterministicSource` — no unseeded random calls. Based on town services and map layout palette.
3. **Persistence**: Store layout data on the `Town` record's `Layout` property as part of world generation. The layout flows into `TownSnapshot` via `FromDomain` and is carried by the `WorldGenerated` event — this is the event-sourced source of truth. JSON snapshots are cache.
4. **Event Replay**: `TownSnapshot.ToDomain` restores the layout during `RehydrateFromEvents`. A parity test verifies command-path and replay-path convergence.
5. **Consistency**: Same seed + same town identity + same `TownServices` produces same layout for each town

### React Integration
1. **Load Phase**: React reads town layout data from the existing `GameSessionDto.World.Towns[].Layout` via `useGameSession` — no separate endpoint
2. **Render Phase**: React passes layout data to Phaser scene constructor
3. **Update Phase**: React re-renders Phaser scene when town changes or layout updates
4. **Interaction Phase**: Phaser scene calls React callbacks for building clicks

### Building Click Routing

Building clicks do **not** introduce new backend enter-building commands in this slice. Phaser scene clicks call React callbacks, which route through the existing frontend place navigation in `TownHubSurface.tsx`:

1. **Building Click**: Phaser scene calls a React callback with the `BuildingKind`
2. **Frontend Navigation**: React maps the `BuildingKind` to the existing `onPlaceChange` callback (e.g., `BuildingKind.Store` → `onPlaceChange("store")` → `StorePlace`)
3. **Scene Update**: React updates Phaser scene with new state (e.g., building availability changes) when the available action set changes

This preserves the existing place surfaces (`StorePlace`, `SheriffPlace`, `SaloonPlace`, `TravelPrepSurface`) and the existing `GameSession` command route. No `EnterStoreCommand` or `POST /enter-store` endpoints are added in this slice.

**Telegraph deferred:** The Telegraph building is rendered visually but not clickable in this slice. There is no `TelegraphPlace` surface and the current `TownHubSurface.tsx` has no telegraph card. Telegraph actions (`FollowTelegraphLeads`) are handled via action handlers, not place navigation. A future issue ([BUNCH-136](https://linear.app/harleys-workspace/issue/BUNCH-136/telegraphaggregate-extraction-and-telegraph-place-surface)) will extract a `TelegraphAggregate` and add the place surface.

## Domain Integration

### Town Record Extension
**File**: `src/WildBunch.Domain/World/WorldModels.cs`

The static town model is the `Town` record in `WorldModels.cs`. `TownAggregate` (at `src/WildBunch.Domain/Game/TownAggregate.cs`) is a session-owned child component of `GameSession` that pairs the static `Town` definition with visit-scoped `TownVisitState`. It holds a `Town Definition` property — adding `Layout` to the `Town` record flows through `TownAggregate.Definition` automatically. No change to `TownAggregate` is needed.

Add an optional Layout property to the `Town` record:
```csharp
public sealed record Town(
    TownId Id,
    string Name,
    TownServices Services,
    TownProsperity Prosperity = TownProsperity.Prosperous,
    TownSourceCatalog? SourceCatalog = null,
    int MapX = 0,
    int MapY = 0,
    bool IsOutlier = false,
    TownLayout? Layout = null)
```

### TownSnapshot Event-Sourcing Round-Trip
**File**: `src/WildBunch.Domain/World/WorldSnapshot.cs`

The world is event-sourced via the `WorldGenerated` domain event, which carries a `WorldSnapshot` containing `TownSnapshot` records. `TownSnapshot.FromDomain`/`ToDomain` currently round-trip: Id, Name, Services, Prosperity, MapX, MapY, IsOutlier. **They must also round-trip `Layout`**, or event replay will reconstruct towns without layouts and the JSON snapshot cache will lose them.

Update `TownSnapshot` to carry `TownLayout? Layout`:
```csharp
public sealed record TownSnapshot(
    string Id,
    string Name,
    TownServices Services,
    TownProsperity Prosperity,
    int MapX,
    int MapY,
    bool IsOutlier,
    TownLayout? Layout = null)
{
    public static TownSnapshot FromDomain(Town town)
        => new(town.Id.Value, town.Name, town.Services, town.Prosperity, town.MapX, town.MapY, town.IsOutlier, town.Layout);

    public Town ToDomain()
        => new(new TownId(Id), Name, Services, Prosperity, MapX: MapX, MapY: MapY, IsOutlier: IsOutlier, Layout: Layout);
}
```

A parity test must verify that event replay (`RehydrateFromEvents`) preserves layouts.

### Building Source Mapping

Buildings come from two sources:

**Baseline navigation buildings** — always present, derived from the existing `TownHubSurface` card grid which is driven by `AvailableActionKind` from `ActionAvailabilityResolver`:
- Store → always available (every town has a shop; `BuySupplies` is always available)
- Sheriff → always available (sheriff records / wanted posters are baseline sources in `TownSourceCatalog.Default`)
- Saloon → always available (saloon look-around / local gossip are baseline sources in `TownSourceCatalog.Default`)
- Trailhead → always available when `AvailableActionKind.Travel` is present

**Service-driven optional buildings** — derived from `TownServices` flags on the `Town` record:
- `TownServices.Telegraph` → Telegraph building
- Future `TownServices` flags will add their corresponding buildings

> **Note:** `TownServices` currently has only `None = 0` and `Telegraph = 1`. There are no `HasStore`, `HasSheriff`, `HasSaloon`, `HasStable`, or `HasDoctor` flags. Store/Sheriff/Saloon/Trailhead are baseline navigation buildings, not service-driven.

### Layout Generation Algorithm
**File**: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`

Extend existing coordinate system:
```csharp
public static class TownLayoutGenerator
{
    public static TownLayout GenerateLayout(
        TownServices services,
        MapLayoutPalette mapLayout,
        int townSlot,
        int townCount,
        GameSetupDeterministicSource source)
    {
        // Use seeded random to place buildings
        // Ensure minimum spacing between buildings
        // Place trailhead at town edge
        // Cluster service buildings near center
        // Return deterministic layout based on seed
    }
}
```

## Technical Decisions

### Layout Generation Strategy
- **Seeded Random**: Use existing `GameSetupDeterministicSource` for reproducible layouts
- **Baseline + Service-Driven**: Always emit baseline navigation buildings (Store, Sheriff, Saloon, Trailhead); emit service-driven optional buildings (Telegraph) only when the corresponding `TownServices` flag is set
- **Positional Rules**: Trailhead at edge, other buildings clustered, player spawn in center
- **Consistency**: Same town always has same layout across visits

### Asset Strategy (Phase 1)
- **Procedural Generation**: Use Phaser graphics primitives (rectangles, circles, text)
- **Building Sprites**: Simple colored rectangles with icon overlays
- **Character Sprite**: Simple circle with directional indicator
- **Background**: Solid color with simple line rendering for roads

### Asset Strategy (Phase 2 - Deferred)
- **Artist-Created Assets**: Detailed building sprites with western aesthetic
- **Character Animation**: Walking animation frames
- **Environmental Details**: Trees, rocks, terrain details
- **Lighting Effects**: Atmospheric lighting for time of day

### State Management
- **React-Driven**: React manages all state, Phaser is pure renderer
- **No Local State**: Phaser scenes don't maintain state, only render what React passes
- **Callback Pattern**: Phaser scenes call React callbacks for interactions
- **Reactivity**: React `useEffect` triggers Phaser scene updates when DTOs change

### Performance Considerations
- **Single Instance**: One Phaser game instance per town hub surface
- **Cleanup**: Proper destruction of Phaser instance when leaving town
- **Memory**: Reuse textures/sprites across towns where possible
- **Rendering**: Simple 2D rendering, minimal performance impact

## API Changes

### No New Endpoints

The layout rides the existing `GameSessionDto` → `WorldDto` → `TownDto.Layout` path via the existing `GetGameSessionHandler`. No separate `GET /town-layout` endpoint is created. This follows the established CQRS read path and avoids a redundant read surface for the same data. The frontend already fetches `GameSessionDto` via `useGameSession`.

> **No enter-building endpoints in this slice.** Building clicks route through the existing frontend place navigation (`onPlaceChange` → existing place surfaces), not new backend commands. See the Building Click Routing section above.

### DTO Changes
- Extend `TownDto` to include `TownLayoutDto? Layout` (deliberate minimal extension — the existing `TownDto` carries only Id, Name, Services, MapX, MapY; Layout is added because the frontend needs it for rendering)
- Add `BuildingKind` enum to shared types
- Add `BuildingPlacementDto` to shared types

## Migration Strategy

### Phase 1: Data Model
1. Add `TownLayout` domain model and DTOs
2. Add `Layout` property to `Town` record in `WorldModels.cs`
3. Update `TownSnapshot.FromDomain`/`ToDomain` to round-trip `Layout` (event-sourcing integrity)
4. Add layout generation algorithm (`TownLayoutGenerator` with deterministic seed plumbing)
5. Integrate layout generation into `MapGenerator.Generate` by post-processing the `World` returned by `SeedWorldFactory.CreateWorld` (do NOT change `SeedWorldFactory.CreateWorld`'s signature)
6. **No Migration Needed**: Greenfield project, can drop and rebuild database

### Phase 2: Backend Integration
1. Extend `TownDto` with `Layout` and update `GameSessionMapper.ToDto(DomainTown town)` to map it (no separate endpoint — layout rides existing `GameSessionDto`)
2. Add event replay parity test verifying layouts survive `RehydrateFromEvents`
3. No new enter-building commands in this slice — building clicks route through existing frontend place navigation
4. **No Migration Needed**: Greenfield project, can drop and rebuild database

### Phase 3: Frontend Integration
1. Create `PhaserTownHubHost` component
2. Create `TownHubScene` Phaser scene
3. Integrate with existing `GameFlowRouter`
4. Replace React town hub cards with Phaser surface
5. **No Migration Needed**: UI change, no data migration needed

## Testing Strategy

### Domain Tests
- Test layout generation determinism (same seed + same town identity + same TownServices = same layout)
- Test service-to-building mapping
- Test layout constraints (spacing, clustering rules)
- Test edge cases (single building towns, maximum building towns)
- Test `TownSnapshot.FromDomain`/`ToDomain` round-trips `Layout`
- Test event replay parity: layouts survive `RehydrateFromEvents` (command-path and replay-path converge)

### Integration Tests
- Test layout is present in `GameSessionDto.World.Towns[].Layout` via existing `GetGameSessionHandler`
- Test layout persistence across session load/save (event stream + snapshot cache)
- Test React-Phaser data flow
- Test building click interactions

### Frontend Tests
- Test Phaser scene creation/destruction
- Test building click callbacks
- Test character movement
- Test visual feedback (available/unavailable buildings)

### Phaser Tests
- Test scene rendering with different layouts
- Test interaction zones (building click detection)
- Test character movement interpolation
- Test cleanup/memory management

## Success Criteria

### Functional Requirements
- Players can click buildings to navigate
- Each town has unique, consistent layout
- Building availability reflected visually
- Character walks to clicked destination
- Layout persists across town revisits
- Integration with existing game flow

### Non-Functional Requirements
- Performance: Scene renders in < 100ms
- Memory: No memory leaks on scene destruction
- Accessibility: Keyboard navigation support
- Responsiveness: Works on different screen sizes
- Maintainability: Clear separation of React/Phaser responsibilities

## Open Questions

1. **Asset Timing**: When to switch from procedural to artist-created assets?
2. **Pathfinding**: Is simple straight-line movement sufficient, or need A* pathfinding?
3. **Camera**: Fixed camera or zoom/pan support?
4. **Animation**: Character walking animation priority?
5. **Sound**: Sound effects for building entry/character movement?

## Dependencies

### Existing Code
- `PhaserMapHost` pattern for React-Phaser integration
- `SeedWorldFactory` for town construction (renamed from `SeedWorldCatalog` by BUNCH-135; `SeedWorldBuilder` has been deleted)
- `MapGenerator.Generate` as the layout integration site (post-processes `World` after `SeedWorldFactory.CreateWorld` using `with` expressions — `GameSetupDeterministicSource` is in scope here)
- `Town` record in `WorldModels.cs` for service mapping and layout storage
- `TownAggregate` at `src/WildBunch.Domain/Game/TownAggregate.cs` — session-owned child component of `GameSession` that carries `Town Definition`; adding `Layout` to `Town` flows through automatically
- `TownSnapshot` in `WorldSnapshot.cs` for event-sourced round-trip (must be updated to carry Layout)
- `WorldGenerated` event for event-sourced world persistence
- `GameSetupDeterministicSource` for seeded layout generation
- `ActionAvailabilityResolver` for available action derivation
- `TownSourceCatalog.Default` for baseline investigation sources
- `GetGameSessionHandler` / `GameSessionMapper` for existing read path (layout rides this path)
- `useGameSession` hook for state management
- `GameFlowRouter` for navigation

### New Code Required
- `TownLayout` domain model and DTOs
- `TownLayoutGenerator` for layout generation
- `PhaserTownHubHost` React component
- `TownHubScene` Phaser scene
- Layout snapshot serialization

### No New API Endpoint

No separate `GET /town-layout` endpoint is created. The layout rides the existing `GameSessionDto` → `WorldDto` → `TownDto.Layout` path via the existing `GetGameSessionHandler`. This follows the established CQRS read path and avoids a redundant read surface for the same data. The frontend already fetches `GameSessionDto` via `useGameSession`.

## Risks and Mitigations

### Risk: Layout Generation Complexity
**Mitigation**: Start with simple grid-based layout, evolve to more sophisticated algorithm

### Risk: Phaser Performance
**Mitigation**: Use simple 2D rendering, test with maximum building count (20 buildings)

### Risk: State Synchronization
**Mitigation**: React-driven updates, no local Phaser state, clear callback pattern

### Risk: Asset Pipeline
**Mitigation**: Phase 1 procedural assets, defer artist assets until layout patterns are stable

### Risk: Breaking Changes
**Mitigation**: Gradual migration, backward compatibility for existing saves, thorough testing
