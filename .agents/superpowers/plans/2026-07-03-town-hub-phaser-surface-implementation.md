# Town Hub Phaser Surface Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **BUNCH-135 merged:** `SeedWorldCatalog` has been renamed to `SeedWorldFactory`. `SeedWorldBuilder` has been deleted; `IsCanonicalSeedWorld` is now `SeedWorld.IsCanonical`. All references in this plan have been updated to use `SeedWorldFactory`.

**Linear issue:** [BUNCH-130](https://linear.app/harleys-workspace/issue/BUNCH-130/town-hub-phaser-surface)

**Goal:** Implement a top-down town layout where players click on buildings to navigate, with each town having a unique building arrangement based on available services and layouts that persist across visits.

**Architecture:** React host component manages Phaser game instance lifecycle following the existing PhaserMapHost pattern. Backend extends the world construction pipeline to generate seeded town layouts. The `Town` record in `WorldModels.cs` gets a `Layout` property. `TownSnapshot` round-trips the layout so the `WorldGenerated` event carries it and event replay preserves it. The existing `GameSessionDto` / `TownDto` carries the layout to the frontend — no separate endpoint. React drives all state, Phaser is pure renderer.

**Architecture stack:** This repo uses DDD + CQRS + Event Sourcing. `GameSession` is the live-play aggregate root. `TownAggregate` is a session-owned child component of `GameSession` that pairs the static `Town` definition with visit-scoped `TownVisitState`. The `Town` record is the static world definition; adding `Layout` to it flows through `TownAggregate.Definition` automatically. World state is event-sourced via `WorldGenerated` (carrying `WorldSnapshot`); JSON snapshots are cache. No new backend commands are introduced — building clicks route through existing frontend place navigation.

**Scope:** Town layout generation, Town model extension, TownSnapshot event-sourcing round-trip, React/Phaser integration for building navigation. Future town features (new building types, dynamic building availability changes, richer town events) are out of scope.

**Tech Stack:** Phaser 3, React, TypeScript, C#/.NET, styled-components

## Global Constraints

- Follow existing PhaserMapHost pattern for React-Phaser integration
- Use existing seed/world-construction pipeline for seeded layout generation (GameSetupDeterministicSource / MapGenerator / SeedWorldFactory)
- Extend the `Town` record in `WorldModels.cs`; `TownAggregate.Definition` carries the layout automatically
- Update `TownSnapshot.FromDomain`/`ToDomain` to round-trip the layout — the `WorldGenerated` event is the source of truth for the world
- React manages all state, Phaser scenes do not maintain local state
- Use procedural assets (Phaser graphics primitives) in Phase 1
- Follow existing styled-components pattern for React styling
- Maintain existing useGameSession hook integration
- No new backend enter-building commands — building clicks route through existing frontend place navigation
- No database migration needed (greenfield project)
- Layout generation must be deterministic given the same seed + town identity + TownServices — no unseeded random calls

---

## Building and Navigation Model

This is the core model decision that reconciles rendering, domain services, and the existing player-visible town hub.

### Two building categories

**Baseline navigation buildings** — always present in every town layout, derived from the existing `TownHubSurface` card grid (which is driven by `AvailableActionKind` from `ActionAvailabilityResolver`). These preserve current player-visible navigation behavior:

- `BuildingKind.Store` — always available (every town has a shop; `BuySupplies` is always available per `ActionAvailabilityResolver`)
- `BuildingKind.Sheriff` — always available (sheriff records / wanted posters are baseline sources in `TownSourceCatalog.Default`)
- `BuildingKind.Saloon` — always available (saloon look-around / local gossip are baseline sources in `TownSourceCatalog.Default`)
- `BuildingKind.Trailhead` — always available when `AvailableActionKind.Travel` is present (exit point for travel)

**Service-driven optional buildings** — derived from `TownServices` flags on the `Town` record. These are additive and do not affect baseline navigation:

- `TownServices.Telegraph` → `BuildingKind.Telegraph` (telegraph office building)
- Future `TownServices` flags will add their corresponding buildings

### BuildingKind vs TownServices

`BuildingKind` is a domain enum that describes the generated shape of the world (what buildings exist in a town). It is NOT a service flag — `TownServices` is the domain service flag on the `Town` record. `BuildingKind` lives in the Domain layer because it is part of the world's generated shape (persisted in `TownSnapshot` / `WorldGenerated`), not merely a rendering detail. The frontend also uses it for click-to-navigate routing, but its source of truth is the domain.

The layout generator maps from both sources to `BuildingKind` during layout generation:

- Baseline navigation buildings are always emitted (Store, Sheriff, Saloon, Trailhead).
- Service-driven buildings are emitted only when the corresponding `TownServices` flag is set.

### Navigation routing

Building clicks in the Phaser scene map to URL navigation via TanStack Router (`useNavigate`) — no new backend commands. BUNCH-124 replaced the old `GameFlowRouter` / `onPlaceChange` callback pattern with URL-based routing:

- `BuildingKind.Store` → `navigate({ to: "/town/store" })` → `StorePlace`
- `BuildingKind.Sheriff` → `navigate({ to: "/town/sheriff" })` → `SheriffPlace`
- `BuildingKind.Saloon` → `navigate({ to: "/town/saloon" })` → `SaloonPlace`
- `BuildingKind.Trailhead` → `navigate({ to: "/town/trailhead" })` → `TravelPrepSurface`

**Telegraph deferred to future slice:** The current `TownHubSurface.tsx` has no telegraph card — telegraph actions (`SendTelegram`, `FollowTelegraphLeads`) are handled via action handlers, not as a place you navigate to from the town hub. There is no `TelegraphPlace` surface and no `/town/telegraph` route. In this slice, the Telegraph building is rendered visually in the layout but is **not clickable** — it has no navigation routing. Clicking it is a no-op (or shows a "coming soon" tooltip). A future issue ([BUNCH-136](https://linear.app/harleys-workspace/issue/BUNCH-136/telegraphaggregate-extraction-and-telegraph-place-surface)) will add a telegraph place surface, route, and wire up the click. This keeps the slice focused on replacing the existing card grid with a Phaser surface without inventing new place surfaces.

### Building availability

Building availability (clickable vs grayed-out) is driven by the same `AvailableActionKind` set that the current card grid uses. The Phaser scene receives the available action kinds from React and maps them to building availability:

- Store available when `AvailableActionKind.BuySupplies` present
- Sheriff available when `AvailableActionKind.ReadWantedPosters` or `AvailableActionKind.CheckSheriffRecords` present
- Saloon available when `AvailableActionKind.LookAroundSaloon` or `AvailableActionKind.GatherLocalGossip` present
- Trailhead available when `AvailableActionKind.Travel` present
- Telegraph: rendered visually but not clickable in this slice (see Navigation routing above — no `/town/telegraph` route exists)

This preserves the exact current player-visible behavior: the same buildings are clickable under the same conditions as the current card grid.

---

## Concrete Specifications

These specifications close the design-decision gaps that would otherwise force implementers to improvise. Implementers should transcribe these, not design them.

### Domain model shapes

**`BuildingKind` enum** (`src/WildBunch.Domain/World/BuildingKind.cs`):

```csharp
public enum BuildingKind
{
    Store = 0,
    Sheriff = 1,
    Saloon = 2,
    Trailhead = 3,
    Telegraph = 4,
    // Future: Doctor, Stable, etc.
}
```

**`BuildingPlacement` record** (`src/WildBunch.Domain/World/BuildingPlacement.cs`):

```csharp
public sealed record BuildingPlacement(
    BuildingKind Kind,
    int X,           // Pixel position in scene coordinates (0-based)
    int Y,           // Pixel position in scene coordinates (0-based)
    int Width = 60,  // Building footprint width in pixels
    int Height = 50); // Building footprint height in pixels
```

No `Rotation` — all buildings are axis-aligned rectangles for Phase 1. No `BuildingId` — `BuildingKind` is the identity within a town layout (each kind appears at most once per town).

**`TownLayout` record** (`src/WildBunch.Domain/World/TownLayout.cs`):

```csharp
public sealed record TownLayout(
    IReadOnlyList<BuildingPlacement> Buildings,
    int PlayerSpawnX,  // Pixel position where player stands
    int PlayerSpawnY);
```

No `PlayerSpawnPosition` value object — use two `int` fields to keep the model simple and avoid inventing a `Position` value object that doesn't exist yet. If a `Position` value object is introduced later, these can be refactored.

### TownLayoutGenerator algorithm

**Scene dimensions:** 800x500 pixels (matches PhaserMapHost's game dimensions).

**Layout algorithm (grid-based, deterministic):**

1. **Player spawn:** Center of scene — `(400, 250)`.
2. **Trailhead:** Placed at the right edge — `(720, 250)`, size 60x50. Represents the exit point for travel.
3. **Store, Sheriff, Saloon:** Placed in a row along the top of the scene, evenly spaced:
   - Store: `(100, 100)`, size 60x50
   - Sheriff: `(370, 100)`, size 60x50
   - Saloon: `(640, 100)`, size 60x50
4. **Telegraph (if `TownServices.Telegraph` is set):** Placed below the row — `(370, 350)`, size 60x50.
5. **Deterministic variation:** Use `GameSetupDeterministicSource.PickIndex(label, count)` to add small deterministic jitter (±20 pixels) to each building's X and Y position, seeded by the town's `Id.Value` and slot index. This ensures same seed + same town = same layout, while making towns look slightly different.

**Method signature:**

```csharp
public static TownLayout GenerateLayout(
    TownServices services,
    TownId townId,
    int townSlotIndex,
    int totalTownCount,
    GameSetupDeterministicSource source,
    SaltSource? saltSource)
```

The `source` provides deterministic rolls via `source.Roll(label)` or `source.PickIndex(label, count)`. Use labels like `$"town-{townId.Value}-building-{kind}-x"` and `$"town-{townId.Value}-building-{kind}-y"` for deterministic jitter. `saltSource` is passed for future salt-aware variation but is not required for Phase 1.

### PhaserTownHubHost prop interface

Following the `PhaserMapHost` pattern (constructor data, ref-based callbacks, `useEffect` lifecycle):

```typescript
interface PhaserTownHubHostProps {
  layout: TownLayoutDto | null | undefined;
  availableActions: AvailableActionKind[];
  onBuildingSelected: (kind: BuildingKind) => void;
}
```

- `layout`: The current town's layout from `currentTown?.layout` via `useGameSession()`. Null while loading.
- `availableActions`: The current available action kinds from `useGameSession().actions`. Used to determine building availability (clickable vs grayed-out).
- `onBuildingSelected`: Callback fired when the player clicks a building. `TownHubSurface` maps this to `navigate({ to: route })` per the navigation routing section.

### TownHubScene structure

Following the `StartingTownMapScene` pattern (same file as host, constructor data, public methods for testing):

```typescript
export class TownHubScene extends Phaser.Scene {
  private readonly layout: TownLayoutDto;
  private readonly availableActions: AvailableActionKind[];
  private readonly onBuildingSelected: (kind: BuildingKind) => void;

  constructor(
    layout: TownLayoutDto,
    availableActions: AvailableActionKind[],
    onBuildingSelected: (kind: BuildingKind) => void,
  ) {
    super("town-hub");
    this.layout = layout;
    this.availableActions = availableActions;
    this.onBuildingSelected = onBuildingSelected;
  }

  // Public for testing — calls onBuildingSelected with the kind
  selectBuilding(kind: BuildingKind): void {
    if (!isBuildingAvailable(kind, this.availableActions)) return;
    this.onBuildingSelected(kind);
  }

  create(): void {
    // Render buildings as rectangles at their layout positions
    // Render player as a circle at PlayerSpawnX/PlayerSpawnY
    // Set up interactive hit areas on buildings that call this.selectBuilding(kind)
    // Telegraph building is rendered but has no interactive hit area (not clickable)
  }
}
```

**Game config** (same as PhaserMapHost):

```typescript
const game = new Phaser.Game({
  parent: containerRef.current,
  width: 800,
  height: 500,
  backgroundColor: "#c4a87a",  // Western dusty ground tone
  scene,
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH,
  },
});
```

**Visual rendering (Phase 1 — procedural graphics primitives):**
- Buildings: `this.add.rectangle(x, y, width, height, color)` — Store (brown 0x8b6914), Sheriff (blue 0x4a6a8a), Saloon (red 0x8b3a3a), Trailhead (green 0x5a7a4a), Telegraph (gray 0x6a6a6a)
- Player: `this.add.circle(spawnX, spawnY, 12, 0xffd700)` — gold circle
- Available buildings: full opacity, 2px white border highlight
- Unavailable buildings: 0.4 opacity, no border
- Telegraph building: 0.6 opacity (rendered but not interactive), no border
- Building labels: `this.add.text(x, y + height/2 + 12, label, { fontSize: '12px', color: '#fff' }).setOrigin(0.5)` — "Store", "Sheriff", "Saloon", "Trailhead", "Telegraph"

**Player walking animation (on building click) — DEFERRED to follow-up:**
- On `selectBuilding(kind)`, before calling the callback, tween the player circle from current position to the clicked building's position:
  `this.tweens.add({ targets: playerCircle, x: building.x, y: building.y, duration: 800, ease: 'linear' })`
- After the tween completes, call `this.onBuildingSelected(kind)`.
- If the player is already at the building, call the callback immediately.
- Telegraph building: no tween, no callback (no-op).
- **Note:** The walking tween was deferred during BUNCH-130 execution. The player circle renders at spawn position as a static visual anchor. `selectBuilding` calls `onBuildingSelected` directly without animation. A follow-up issue should implement the tween for UX polish.

**Building availability mapping:**

```typescript
function isBuildingAvailable(kind: BuildingKind, actions: AvailableActionKind[]): boolean {
  switch (kind) {
    case BuildingKind.Store: return actions.includes(AvailableActionKind.BuySupplies);
    case BuildingKind.Sheriff: return actions.includes(AvailableActionKind.ReadWantedPosters)
      || actions.includes(AvailableActionKind.CheckSheriffRecords);
    case BuildingKind.Saloon: return actions.includes(AvailableActionKind.LookAroundSaloon)
      || actions.includes(AvailableActionKind.GatherLocalGossip);
    case BuildingKind.Trailhead: return actions.includes(AvailableActionKind.Travel);
    case BuildingKind.Telegraph: return false; // Not clickable in this slice
  }
}
```

### Test mock pattern (PhaserTownHubHost.test.tsx and TownHubScene.test.ts)

Follow the exact `PhaserMapHost.test.tsx` mock pattern:

```typescript
const mockState = vi.hoisted(() => ({
  games: [] as Array<{ config: { scene: TownHubScene }; destroyed: boolean; destroy: () => void }>,
}));

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    public destroyed = false;
    constructor(config: unknown) {
      this.config = config;
      mockState.games.push(this as never);
    }
    destroy() { this.destroyed = true; }
  }
  class Scene { constructor(_key?: string) {} }
  const Scale = { FIT: 0, CENTER_BOTH: 0 };
  return { default: { Game, Scene, Scale }, Game, Scene, Scale };
});
```

Access scene via `mockState.games[0].config.scene`. Call `scene.selectBuilding(BuildingKind.Store)` to test navigation callbacks. Verify game destruction via `mockState.games[0].destroyed`.

---

## File Structure

**New Domain Files:**

- `src/WildBunch.Domain/World/TownLayout.cs` - Town layout domain model
- `src/WildBunch.Domain/World/BuildingPlacement.cs` - Building placement domain model
- `src/WildBunch.Domain/World/BuildingKind.cs` - Building kind enum (domain concept: part of the world's generated shape)

**New GameContent Files:**

- `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs` - Layout generation algorithm (deterministic, uses existing seed plumbing)

**New Application Files:**

- `src/WildBunch.Application/Games/Models/TownLayoutDto.cs` - Town layout DTO
- `src/WildBunch.Application/Games/Models/BuildingPlacementDto.cs` - Building placement DTO
- `src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs` - Layout mapping logic

**New Web Files:**

- `src/WildBunch.Web/src/components/town-hub/PhaserTownHubHost.tsx` - React host component
- `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts` - Phaser scene
- `src/WildBunch.Web/src/components/town-hub/types.ts` - TypeScript types

**Modified Domain Files:**

- `src/WildBunch.Domain/World/WorldModels.cs` - Add `Layout` property to `Town` record
- `src/WildBunch.Domain/World/WorldSnapshot.cs` - Update `TownSnapshot.FromDomain`/`ToDomain` to round-trip `Layout`

**Modified GameContent Files:**

- `src/WildBunch.GameContent/NewGame/MapGenerator.cs` - Post-process World after `SeedWorldFactory.CreateWorld` call to attach layouts using `with` expressions (source is in scope here, not in `CreateWorld`)

**Modified Application Files:**

- `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs` - Map layout via `TownLayoutMapper`
- `src/WildBunch.Application/Games/Models/GameDtos.cs` - Extend `TownDto` with `Layout` (deliberate minimal-DTO extension — only add what the frontend needs)

**Modified Web Files:**

- `src/WildBunch.Web/src/api/types.ts` - Add `layout` to frontend `TownDto`, add `TownLayoutDto` / `BuildingPlacementDto` / `BuildingKind` types
- `src/WildBunch.Web/src/flow/TownHubSurface.tsx` - Replace card grid with Phaser surface, use `useNavigate` for building click routing
- `src/WildBunch.Web/src/shell/router.tsx` - No changes needed (routes already exist for `/town`, `/town/store`, `/town/sheriff`, `/town/saloon`, `/town/trailhead` from BUNCH-124)

**Not created (architecture decision):**

- No separate `GET /town-layout` API endpoint — the layout rides the existing `GameSessionDto` → `WorldDto` → `TownDto.Layout` path via the existing `GetGameSessionHandler`. This follows the established CQRS read path and avoids a redundant read surface for the same data.
- No new backend enter-building commands — building clicks are frontend view transitions, not domain mutations.

---

### Task 1: Add Town Layout Domain Models

**Files:**

- Create: `src/WildBunch.Domain/World/TownLayout.cs`
- Create: `src/WildBunch.Domain/World/BuildingPlacement.cs`
- Create: `src/WildBunch.Domain/World/BuildingKind.cs`
- Test: `tests/WildBunch.Domain.Tests/World/TownLayoutTests.cs`

**Interfaces:**

- Produces: `TownLayout` with Buildings and PlayerSpawnPosition
- Produces: `BuildingPlacement` with BuildingId, Kind, Position, Rotation
- Produces: `BuildingKind` enum with Store, Sheriff, Saloon, Trailhead (baseline) and Telegraph (service-driven), with room for future building types

**Concrete shapes:** See "Concrete Specifications" section above for exact record definitions, BuildingKind enum values, and field types. Implementers should transcribe those definitions, not design them.

**Architecture note:** `BuildingKind` is a domain concept — part of the world's generated shape, persisted in `TownSnapshot` / `WorldGenerated`. It is not merely a rendering detail. The frontend uses it for click-to-navigate routing, but its source of truth is the domain.

- [ ] **Step 1: Write the failing test for TownLayout creation**

Write test that verifies TownLayout can be created with buildings and player spawn position. Include at least one baseline building (Store) and one service-driven building (Telegraph).

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TownLayoutTests" -v`
Expected: FAIL with "TownLayout not defined"

- [ ] **Step 3: Write minimal implementation**

Create `TownLayout`, `BuildingPlacement`, and `BuildingKind` in `src/WildBunch.Domain/World/`. Follow existing record pattern in WorldModels.cs. `BuildingKind` must include: `Store`, `Sheriff`, `Saloon`, `Trailhead` (baseline navigation buildings), `Telegraph` (service-driven), with room for future building types.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TownLayoutTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/World/TownLayout.cs src/WildBunch.Domain/World/BuildingPlacement.cs src/WildBunch.Domain/World/BuildingKind.cs tests/WildBunch.Domain.Tests/World/TownLayoutTests.cs
git commit -m "feat: add town layout domain models"
```

---

### Task 2: Extend Town Record with Layout Property

**Files:**

- Modify: `src/WildBunch.Domain/World/WorldModels.cs`
- Test: `tests/WildBunch.Domain.Tests/World/WorldModelsTests.cs`

**Interfaces:**

- Consumes: `TownLayout` from Task 1
- Produces: `Town` record with `Layout` property

**Architecture note:** The `Town` record is the static world definition. `TownAggregate` (a session-owned child component of `GameSession` at `src/WildBunch.Domain/Game/TownAggregate.cs`) holds a `Town Definition` property — adding `Layout` to `Town` flows through `TownAggregate.Definition` automatically. No change to `TownAggregate` is needed.

- [ ] **Step 1: Write the failing test for Town with Layout**

Write test that verifies Town record can hold a Layout property.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~WorldModelsTests" -v`
Expected: FAIL with Layout property not defined

- [ ] **Step 3: Add Layout property to Town record**

Add optional `TownLayout? Layout = null` property to the `Town` record in WorldModels.cs. Follow existing record pattern with default parameters. The Town record currently has parameters: Id, Name, Services, Prosperity, SourceCatalog, MapX, MapY, IsOutlier. Add Layout as an additional optional parameter.

- [ ] **Step 4: Update Town construction calls**

Update Town construction calls in `SeedWorldFactory` and test fixtures to pass layout parameter (use null for now — layout generation is integrated in Task 5).

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~WorldModelsTests" -v`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/World/WorldModels.cs tests/WildBunch.Domain.Tests/World/WorldModelsTests.cs
git commit -m "feat: add Layout property to Town record"
```

---

### Task 3: Update TownSnapshot Round-Trip for Layout

**Files:**

- Modify: `src/WildBunch.Domain/World/WorldSnapshot.cs`
- Test: `tests/WildBunch.Domain.Tests/World/WorldSnapshotTests.cs`

**Interfaces:**

- Consumes: `TownLayout` from Task 1, `Town.Layout` from Task 2
- Produces: `TownSnapshot` that round-trips `Layout` through `FromDomain`/`ToDomain`

**Architecture note (critical — event sourcing integrity):** The world is event-sourced via the `WorldGenerated` domain event, which carries a `WorldSnapshot` containing `TownSnapshot` records. `TownSnapshot.FromDomain`/`ToDomain` currently round-trip: Id, Name, Services, Prosperity, MapX, MapY, IsOutlier. If `Layout` is added to `Town` but NOT to `TownSnapshot`, then `WorldGenerated` events won't carry the layout, event replay (`RehydrateFromEvents`) will reconstruct towns WITHOUT layouts, the JSON snapshot cache will lose layouts, and parity tests will break. This task closes that gap.

- [ ] **Step 1: Write the failing test for TownSnapshot layout round-trip**

Write test that verifies `TownSnapshot.FromDomain` preserves `Layout` and `ToDomain` restores it. Include both a town with a layout and a town with null layout.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~WorldSnapshotTests" -v`
Expected: FAIL — Layout not carried by TownSnapshot

- [ ] **Step 3: Update TownSnapshot to round-trip Layout**

Add `TownLayout? Layout` to the `TownSnapshot` record. Update `FromDomain` to map `town.Layout` into the snapshot. Update `ToDomain` to pass `Layout` to the `Town` constructor.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~WorldSnapshotTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/World/WorldSnapshot.cs tests/WildBunch.Domain.Tests/World/WorldSnapshotTests.cs
git commit -m "feat: round-trip TownLayout through TownSnapshot for event sourcing"
```

---

### Task 4: Implement Town Layout Generation Algorithm

**Files:**

- Create: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`

**Interfaces:**

- Consumes: `TownLayout`, `BuildingPlacement`, `BuildingKind` from Task 1
- Consumes: `TownServices` from `Town` record
- Produces: `TownLayoutGenerator.GenerateLayout` method

**Architecture note (seed plumbing):** Per the GameContent AGENTS.md and architecture guardrails: "Seeded setup and randomness require explicit seams. Avoid unseeded random calls and hidden world-start defaults in domain or application code." The generator MUST use the existing `GameSetupDeterministicSource` / seed plumbing for reproducible layouts. Same seed + same town identity + same `TownServices` must produce the same layout. No `new Random()` or `Random.Shared` calls.

- [ ] **Step 1: Write the failing test for layout generation**

Write test that verifies:

- Same seed produces same layout (determinism)
- Layout always includes baseline navigation buildings: Store, Sheriff, Saloon, Trailhead
- Layout includes Telegraph building when `TownServices.Telegraph` is set
- Layout excludes Telegraph building when `TownServices.None`
- Trailhead placed at town edge, player spawn in center

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TownLayoutGeneratorTests" -v`
Expected: FAIL with "TownLayoutGenerator not defined"

- [ ] **Step 3: Write minimal implementation**

Create `TownLayoutGenerator` in `src/WildBunch.GameContent/NewGame/`. Follow the algorithm and method signature in the "Concrete Specifications > TownLayoutGenerator algorithm" section above. Use `GameSetupDeterministicSource.PickIndex(label, count)` for deterministic jitter on building positions. Always emit baseline navigation buildings (Store, Sheriff, Saloon, Trailhead). Emit Telegraph only when `TownServices.Telegraph` is set. Place trailhead at right edge, player spawn in center, other buildings in a row along the top.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TownLayoutGeneratorTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs
git commit -m "feat: implement deterministic town layout generation algorithm"
```

---

### Task 5: Integrate Layout Generation into World Construction Pipeline

**Files:**

- Modify: `src/WildBunch.GameContent/NewGame/MapGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/MapGeneratorTests.cs` (or the relevant pipeline test)

**Interfaces:**

- Consumes: `TownLayoutGenerator.GenerateLayout` from Task 4
- Consumes: `GameSetupDeterministicSource` (available in `MapGenerator.Generate`, NOT in `SeedWorldFactory.CreateWorld`)
- Produces: World with `Town` records that have non-null `Layout` properties

**Architecture note (correct integration site — critical):** `SeedWorldFactory.CreateWorld` does NOT receive a `GameSetupDeterministicSource` parameter — it only has `SaltSource?` and `Guid? seedCode`. The `GameSetupDeterministicSource` is available one level up in `MapGenerator.Generate(SeedWorld, GameSetupDeterministicSource, GameEntropy, SaltSource?)`, which calls `SeedWorldFactory.CreateWorld` at line 90 and returns the `World`.

**Do NOT change `SeedWorldFactory.CreateWorld`'s signature.** Instead, post-process the returned `World` in `MapGenerator.Generate`.

**Critical type note:** `World` is a `sealed class`, NOT a record — it does NOT support `with` expressions. However, `World` has a public constructor `World(IEnumerable<Town> towns, IEnumerable<Trail> trails)`, and `Town` IS a `sealed record` (supports `with` expressions). So the post-process constructs a new `World` with the modified towns and the existing trails:

```csharp
// In MapGenerator.Generate, after the existing CreateWorld call:
var world = SeedWorldFactory.CreateWorld(...);

// Town is a record — with-expressions produce new Town instances with layouts.
// World is a sealed class — construct a new World with modified towns + existing trails.
var townsWithLayouts = world.Towns.Select((town, index) =>
    town with { Layout = TownLayoutGenerator.GenerateLayout(
        town.Services,
        town.Id,
        index,
        world.Towns.Count,
        source,
        saltSource) }).ToArray();

return new World(townsWithLayouts, world.Trails);
```

This is the cleanest integration: no signature changes, `source` is in scope, `Town` record `with` expressions produce new `Town` instances with layouts, and a new `World` is constructed with the modified towns and the existing trails.

**Architecture note (CreateCanonicalWorld does NOT need layouts):** `SeedWorldFactory.CreateCanonicalWorld()` is called by `StartingTownCatalog` and `SeedWorldMapLayout` for the start-screen map only. It does NOT go through `MapGenerator` and does NOT have a `GameSetupDeterministicSource`. Layouts are only needed for gameplay worlds (via `MapGenerator`), not the start-screen canonical world. Do NOT add layout generation to `CreateCanonicalWorld`.

- [ ] **Step 1: Write the failing test for layout integration**

Write test that verifies `MapGenerator.Generate` produces a `World` where every `Town` has a non-null `Layout` property. Use the existing `MapGeneratorTests` patterns (or `SeededNewGameFactoryTests`) for seeded world construction.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~MapGeneratorTests" -v`
Expected: FAIL with Layout being null

- [ ] **Step 3: Post-process World in MapGenerator.Generate**

After the existing `SeedWorldFactory.CreateWorld(...)` call in `MapGenerator.Generate`, iterate `world.Towns` and attach layouts using `town with { Layout = TownLayoutGenerator.GenerateLayout(...) }` (Town is a record — `with` works). Construct a new `World` with the modified towns and existing trails: `return new World(townsWithLayouts, world.Trails)` (World is a sealed class — no `with` expression). Pass the town's `Services`, `Id`, slot index, total town count, `source`, and `saltSource` to the generator.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~MapGeneratorTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/MapGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/MapGeneratorTests.cs
git commit -m "feat: integrate layout generation into MapGenerator world construction"
```

---

### Task 6: Add Town Layout DTOs, Mapping, and Extend TownDto

**Files:**

- Create: `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`
- Create: `src/WildBunch.Application/Games/Models/BuildingPlacementDto.cs`
- Create: `src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs`
- Modify: `src/WildBunch.Application/Games/Models/GameDtos.cs`
- Modify: `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`
- Test: `tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperTests.cs`
- Test: `tests/WildBunch.Application.Tests/Games/Mapping/GameSessionMapperTests.cs`

**Interfaces:**

- Consumes: `TownLayout`, `BuildingPlacement`, `BuildingKind` from Task 1
- Produces: `TownLayoutDto`, `BuildingPlacementDto`
- Produces: `TownLayoutMapper.ToDto` method
- Produces: `TownDto` with `Layout` property
- Produces: `GameSessionMapper.ToDto(DomainTown town)` that maps layout

**Architecture note (no separate endpoint):** The layout rides the existing `GameSessionDto` → `WorldDto` → `TownDto.Layout` path via the existing `GetGameSessionHandler`. No separate `GET /town-layout` endpoint is created. This follows the established CQRS read path and avoids a redundant read surface for the same data. The frontend already fetches `GameSessionDto` via `useGameSession`.

**Architecture note (minimal DTO pattern):** The existing `TownDto` is intentionally minimal: `Id, Name, Services, MapX, MapY`. It does not carry `Prosperity`, `SourceCatalog`, or `IsOutlier`. Adding `Layout` is a deliberate minimal extension — only carry what the frontend needs, consistent with the existing pattern.

- [ ] **Step 1: Write the failing test for DTO mapping**

Write test that verifies `TownLayoutMapper.ToDto` maps `TownLayout` to `TownLayoutDto` correctly, including all `BuildingKind` values.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~TownLayoutMapperTests" -v`
Expected: FAIL with "TownLayoutMapper not defined"

- [ ] **Step 3: Create DTOs and mapper**

Create `TownLayoutDto`, `BuildingPlacementDto`, and `TownLayoutMapper`. Follow existing DTO patterns in `GameDtos.cs` and existing mapper patterns in `GameSessionMapper.cs`. The DTOs must carry the full `BuildingKind` enum so the frontend can route clicks to the correct place navigation.

- [ ] **Step 4: Write the failing test for TownDto with Layout**

Write test that verifies `TownDto` includes `Layout` property and `GameSessionMapper.ToDto(DomainTown town)` maps it correctly.

- [ ] **Step 5: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~GameSessionMapperTests" -v`
Expected: FAIL

- [ ] **Step 6: Add Layout to TownDto and GameSessionMapper**

Add `TownLayoutDto? Layout = null` to `TownDto` in `GameDtos.cs`. Update `GameSessionMapper.ToDto(DomainTown town)` to map `town.Layout` via `TownLayoutMapper.ToDto`.

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~TownLayoutMapperTests|FullyQualifiedName~GameSessionMapperTests" -v`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add src/WildBunch.Application/Games/Models/TownLayoutDto.cs src/WildBunch.Application/Games/Models/BuildingPlacementDto.cs src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs src/WildBunch.Application/Games/Models/GameDtos.cs src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperTests.cs tests/WildBunch.Application.Tests/Games/Mapping/GameSessionMapperTests.cs
git commit -m "feat: add town layout DTOs, mapping, and extend TownDto with layout"
```

---

### Task 7: Add Event Replay Parity Test for Layout

**Files:**

- Test: `tests/WildBunch.Domain.Tests/Game/GameSessionEventReplayTests.cs` (or the existing parity test file)

**Interfaces:**

- Consumes: `Town.Layout` from Task 2, `TownSnapshot` round-trip from Task 3, world construction from Task 5
- Produces: Parity test verifying event replay preserves layouts

**Architecture note (event sourcing parity):** Per the architecture guardrails: "Command-path state and replay-path state must converge. This is verified by parity tests. If a command mutates state directly, the corresponding Apply method must produce the same state from the event." The world is created during game setup (command path) and reconstructed from `WorldGenerated` during replay. This test verifies that layouts survive the round-trip.

- [ ] **Step 1: Write the parity test**

Write test that creates a game session with a world containing towns with layouts, then rehydrates from the event stream, and verifies the rehydrated towns have the same layouts. Note: this test should PASS if Tasks 2-3 were done correctly (the `TownSnapshot` round-trip carries the layout). It is a verification test, not a failing-first test — the gap it guards against is a future regression where someone breaks the `TownSnapshot` round-trip.

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~GameSessionEventReplayTests" -v`

If it fails, the `TownSnapshot` round-trip (Task 3) or `Apply(WorldGenerated)` has a gap — inspect `GameSessionEventReplay.cs` to ensure it restores the world from `WorldSnapshot.ToDomain()` which now carries layouts.

- [ ] **Step 3: Fix any parity gap if needed**

If the test fails, inspect `Apply(WorldGenerated)` in `GameSessionEventReplay.cs` to ensure it restores the world from `WorldSnapshot.ToDomain()` which now carries layouts.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~GameSessionEventReplayTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tests/WildBunch.Domain.Tests/Game/GameSessionEventReplayTests.cs
git commit -m "test: add event replay parity test for town layout"
```

---

### Task 8: Implement Phaser TownHubScene

**Files:**

- Create: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`
- Create: `src/WildBunch.Web/src/components/town-hub/types.ts`
- Test: `src/WildBunch.Web/src/tests/TownHubScene.test.ts`

**Interfaces:**

- Consumes: town layout data from React props (BuildingKind, position, availability)
- Produces: Phaser scene that renders buildings and handles click navigation
- Produces: TypeScript types for town layout matching the DTOs

**Test pattern note (critical):** The existing Phaser test pattern at `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx` mocks Phaser entirely via `vi.mock("phaser", ...)`. With Phaser mocked, you **cannot test visual rendering or click detection** — you can only test that the scene is constructed with the right data and that callbacks are wired correctly. Follow this pattern:

1. Mock `phaser` with stub `Game`, `Scene`, and `Scale` classes (copy the mock from `PhaserMapHost.test.tsx` or `test-utils/setup.ts`)
2. Test that `TownHubScene` is constructed with the correct layout data and navigation callback
3. Test that the navigation callback fires with the correct `BuildingKind` when `selectBuilding` is called
4. Do NOT test rendering, graphics primitives, or pointer events — those are Phaser internals that are mocked out

- [ ] **Step 1: Write the failing test for TownHubScene**

Write test following the `PhaserMapHost.test.tsx` mock pattern. Test that `TownHubScene` is constructed with layout data and that the navigation callback fires with the correct `BuildingKind` when `selectBuilding` is called. Include all baseline BuildingKind values (Store, Sheriff, Saloon, Trailhead) in the test data.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run TownHubScene`
Expected: FAIL

- [ ] **Step 3: Write minimal implementation**

Create `TownHubScene` extending Phaser.Scene following the "Concrete Specifications > TownHubScene structure" section above. Render buildings as rectangles at their layout positions with the specified colors. Render player as a gold circle at spawn position. Implement `selectBuilding(kind: BuildingKind)` that tweens the player to the building then calls the callback. Telegraph building is rendered but has no interactive hit area. Create `types.ts` with TypeScript interfaces matching the DTOs, including the full BuildingKind enum (Store, Sheriff, Saloon, Trailhead, Telegraph).

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --run TownHubScene`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/TownHubScene.ts src/WildBunch.Web/src/components/town-hub/types.ts src/WildBunch.Web/src/tests/TownHubScene.test.ts
git commit -m "feat: implement Phaser TownHubScene"
```

---

### Task 9: Implement React PhaserTownHubHost Component

**Files:**

- Create: `src/WildBunch.Web/src/components/town-hub/PhaserTownHubHost.tsx`
- Modify: `src/WildBunch.Web/src/api/types.ts` - Add `layout` to frontend `TownDto` and add `TownLayoutDto` / `BuildingPlacementDto` / `BuildingKind` types
- Test: `src/WildBunch.Web/src/tests/PhaserTownHubHost.test.tsx`

**Interfaces:**

- Consumes: `TownHubScene` from Task 8
- Consumes: town layout data from `useGameSession` (via `currentTown.layout`)
- Produces: React component that manages Phaser game instance lifecycle

**Data path note (critical):** The layout reaches the frontend via this exact path:

1. Backend: `GameSessionDto.World.Towns[].Layout` (added in Task 6)
2. Frontend: `useGameSession()` → `session.world.towns` → `currentTown` (which is `session.world.towns.find(t => t.id === session.player.currentTownId)` per `useGameSessionQueries.ts:38-42`)
3. `currentTown.layout` gives the `TownLayoutDto` for the current town

The frontend `TownDto` in `src/WildBunch.Web/src/api/types.ts` (line 310-314) currently only has `{ id, name, services }`. It must be extended with `layout?: TownLayoutDto | null`. Also add `TownLayoutDto`, `BuildingPlacementDto`, and `BuildingKind` enum types to `types.ts` matching the backend DTOs from Task 6.

- [ ] **Step 1: Write the failing test for PhaserTownHubHost**

Write test following the `PhaserMapHost.test.tsx` mock pattern. Test that `PhaserTownHubHost` creates a Phaser game instance and passes the current town's layout data to the `TownHubScene` constructor.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run PhaserTownHubHost`
Expected: FAIL

- [ ] **Step 3: Add frontend types to types.ts**

Add `BuildingKind` enum, `BuildingPlacementDto`, `TownLayoutDto` interfaces to `src/WildBunch.Web/src/api/types.ts`. Add `layout?: TownLayoutDto | null` to the existing `TownDto` interface.

- [ ] **Step 4: Write minimal implementation**

Create `PhaserTownHubHost` following the "Concrete Specifications > PhaserTownHubHost prop interface" section above. Use the exact prop interface specified there: `layout`, `availableActions`, `onBuildingSelected`. Use a ref to hold the Phaser game instance. Get `currentTown` from `useGameSession()` and pass `currentTown?.layout` to `TownHubScene`. Pass `actions` from `useGameSession().actions`. Pass the navigation callback via a ref (same pattern as `PhaserMapHost`'s `onTownSelectedRef`). Clean up the Phaser game on unmount with `game.destroy(true)`. Use the same Scale config (FIT + CENTER_BOTH) and game dimensions (800x500) as PhaserMapHost.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --run PhaserTownHubHost`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/PhaserTownHubHost.tsx src/WildBunch.Web/src/tests/PhaserTownHubHost.test.tsx
git commit -m "feat: implement React PhaserTownHubHost component"
```

---

### Task 10: Integrate Town Hub into URL Routing

**Files:**

- Modify: `src/WildBunch.Web/src/flow/TownHubSurface.tsx`
- Test: `src/WildBunch.Web/src/tests/TownHubSurface.test.tsx`

**Interfaces:**

- Consumes: `PhaserTownHubHost` from Task 9
- Consumes: `useNavigate` from `@tanstack/react-router` (BUNCH-124 replaced `GameFlowRouter` with TanStack Router)
- Produces: TownHubSurface that renders Phaser surface instead of card grid, with building clicks navigating to existing URL routes

**BUNCH-124 alignment note (critical):** BUNCH-124 deleted `GameFlowRouter.tsx` and replaced it with TanStack Router (`shell/router.tsx`). Place navigation is now URL-based via `useNavigate` from `@tanstack/react-router`, not `onPlaceChange` callbacks. The existing routes are already defined in `shell/router.tsx`:

- `/town` → `TownHubSurface`
- `/town/store` → `StorePlace`
- `/town/sheriff` → `SheriffPlace`
- `/town/saloon` → `SaloonPlace`
- `/town/trailhead` → `TravelPrepSurface`
- `/trail` → `TrailFlowSurface`

No changes to `shell/router.tsx` are needed — the routes already exist. This task only modifies `TownHubSurface.tsx` to render `PhaserTownHubHost` instead of the card grid, and passes a `navigate`-based callback to the Phaser scene for building clicks.

**Preservation requirement:** The existing place surfaces (StorePlace, SheriffPlace, SaloonPlace, TravelPrepSurface) and their routes must remain unchanged. Only the entry point changes — building clicks in Phaser replace card clicks, but navigate to the same URL routes. No new backend commands are introduced.

- [ ] **Step 1: Write the failing test for Phaser town hub integration**

Write test that verifies TownHubSurface renders PhaserTownHubHost instead of card grid, and that building clicks call `navigate` with the correct URL. Mock `@tanstack/react-router`'s `useNavigate` and `useSearch` per the existing test patterns in `AppShell.test.tsx`.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run TownHubSurface`
Expected: FAIL

- [ ] **Step 3: Replace card grid with Phaser surface**

Update TownHubSurface to render `PhaserTownHubHost` instead of the card grid. Pass a navigation callback to `PhaserTownHubHost` that calls `navigate({ to: route })` for each building kind:

- `BuildingKind.Store` → `navigate({ to: "/town/store" })`
- `BuildingKind.Sheriff` → `navigate({ to: "/town/sheriff" })`
- `BuildingKind.Saloon` → `navigate({ to: "/town/saloon" })`
- `BuildingKind.Trailhead` → `navigate({ to: "/town/trailhead" })`
- `BuildingKind.Telegraph` → no-op (rendered visually, not clickable in this slice — see Navigation routing above)

Keep the arrival notice logic (`?arrived=1` search param) and the `useSearch` / `useNavigate` imports from `@tanstack/react-router`. No changes to `shell/router.tsx` — routes already exist from BUNCH-124.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --run TownHubSurface`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/flow/TownHubSurface.tsx src/WildBunch.Web/src/tests/TownHubSurface.test.tsx
git commit -m "feat: replace town hub card grid with Phaser surface"
```

---

### Task 11: Add Visual Feedback for Available/Unavailable Buildings

**Files:**

- Modify: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`
- Test: `src/WildBunch.Web/src/tests/TownHubScene.test.ts`

**Interfaces:**

- Consumes: available actions from useGameSession (AvailableActionKind set)
- Produces: Visual differentiation for available vs unavailable buildings

- [ ] **Step 1: Write the failing test for visual feedback**

Write test that verifies available buildings render differently from unavailable buildings, using the same AvailableActionKind-driven availability as the current card grid.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run TownHubScene`
Expected: FAIL

- [ ] **Step 3: Implement visual feedback**

Pass available actions to TownHubScene. Map AvailableActionKind to building availability using the `isBuildingAvailable` function in the "Concrete Specifications" section. Render available buildings with full opacity and 2px white border highlight. Render unavailable buildings with 0.4 opacity and no border. Telegraph building rendered at 0.6 opacity with no border and no interactive hit area. Disable click on unavailable buildings (return early in `selectBuilding`).

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --run TownHubScene`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/TownHubScene.ts src/WildBunch.Web/src/tests/TownHubScene.test.ts
git commit -m "feat: add visual feedback for available/unavailable buildings"
```

---

### Task 12: Run Full Test Suite and Validate

- [ ] **Step 1: Run all .NET tests**

Run: `dotnet test`
Expected: All tests pass, including the new event replay parity test (Task 7)

- [ ] **Step 2: Run all frontend tests**

Run: `npm test -- --run`
Expected: All tests pass

- [ ] **Step 3: Run frontend typecheck and build**

Run: `npm run typecheck && npm run build`
Expected: No errors

- [ ] **Step 4: Validate backend build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 5: Commit any final fixes**

```bash
git add -A
git commit -m "test: validate full suite for town hub phaser surface"
```

---

## Self-Review

**1. Spec coverage:**
- Town layout domain models (Task 1)
- Town record extension (Task 2)
- TownSnapshot event-sourcing round-trip (Task 3)
- Layout generation algorithm (Task 4)
- World construction pipeline integration (Task 5)
- DTOs, mapping, and TownDto extension (Task 6)
- Event replay parity test (Task 7)
- Phaser scene (Task 8)
- React host component (Task 9)
- Game flow integration (Task 10)
- Visual feedback (Task 11)
- Integration testing (Task 12)

**2. Building/navigation model:** BuildingKind includes Store, Sheriff, Saloon, Trailhead (baseline navigation, always present and clickable) and Telegraph (service-driven, rendered visually but not clickable in this slice — deferred to a future issue that adds a telegraph place surface). This preserves current player-visible town hub behavior (store/sheriff/saloon/trailhead navigation) while rendering service-driven buildings visually.

**3. Type consistency:** All types match across tasks — TownLayout, BuildingPlacement, BuildingKind used consistently. Frontend types in `types.ts` mirror backend DTOs.

**4. Architecture compliance:**
- Follows existing PhaserMapHost pattern (including Phaser mock test pattern)
- Follows BUNCH-124 URL routing pattern (TanStack Router, `useNavigate`, lazy-loaded routes) — no `GameFlowRouter` or `onPlaceChange` callbacks
- Extends Town record in WorldModels.cs; TownAggregate.Definition carries the layout automatically (no TownAggregate change needed)
- TownSnapshot round-trips the layout for event-sourcing integrity (WorldGenerated event carries it)
- Layout generation uses existing seed plumbing (GameSetupDeterministicSource) — no unseeded random
- World construction integration post-processes the World in `MapGenerator.Generate` (where `source` is in scope) — `Town` record `with` expressions attach layouts, new `World` constructed with modified towns + existing trails (World is a sealed class, NOT a record — no `with` expression on World itself) — does NOT change `SeedWorldFactory.CreateWorld`'s signature
- `CreateCanonicalWorld` (start-screen map) does NOT get layouts — only gameplay worlds via `MapGenerator` need them
- No separate API endpoint — layout rides existing GameSessionDto → WorldDto → TownDto.Layout → frontend `currentTown.layout`
- Frontend `TownDto` in `types.ts` extended with `layout` field (currently only has id, name, services)
- No new backend enter-building commands — building clicks are frontend view transitions
- Telegraph building rendered but not clickable (no place surface exists — deferred to future issue)
- React-driven state management, Phaser is pure renderer
- Event replay parity test verifies command-path and replay-path convergence

**5. Greenfield project:** No migration steps included — all database changes assume greenfield status.

**6. Preservation requirements:**
- Existing GameSession command route and current player-visible behavior must remain stable
- Existing TownHubSurface card grid replaced with Phaser surface, but underlying URL routes (StorePlace, SheriffPlace, SaloonPlace, TravelPrepSurface) unchanged — BUNCH-124's TanStack Router routes remain as-is
- Phaser remains renderer/input adapter, with React/backend/domain owning truth
- No new backend commands introduced

**7. Architecture stack alignment:**
- DDD: Town record owns the layout; TownAggregate (session-owned child component) carries it via Definition; no aggregate boundary changes
- CQRS: Layout reads through existing GetGameSessionHandler/GameSessionMapper (query side); no new commands (no mutation needed — entering a place is a frontend view transition)
- Event Sourcing: TownSnapshot round-trips Layout; WorldGenerated event carries it; parity test verifies replay convergence
- Clean Architecture: Domain (TownLayout, BuildingKind) has no external refs; GameContent references Domain; Application references Domain; Web consumes DTOs

---

## Staleness Check

This plan was verified against current source on 2026-07-05 and repaired after an architecture review against the repo's DDD/CQRS/Event Sourcing/Clean Architecture stack, plus an execution confidence assessment pass that verified all file paths, class shapes, method signatures, DTO boundaries, and integration sites against current source:

- `Town` record exists in `src/WildBunch.Domain/World/WorldModels.cs` with parameters: Id, Name, Services, Prosperity, SourceCatalog, MapX, MapY, IsOutlier. It is a `sealed record` — supports `with` expressions.
- `World` is a `sealed class` (NOT a record) in `WorldModels.cs` with constructor `World(IEnumerable<Town> towns, IEnumerable<Trail> trails)`. Does NOT support `with` expressions. `Towns` returns `IReadOnlyCollection<Town>` (via `ToList()` on internal dictionary values). Post-processing requires constructing a new `World` with modified towns + existing trails: `new World(townsWithLayouts, world.Trails)`.
- `TownAggregate` EXISTS at `src/WildBunch.Domain/Game/TownAggregate.cs` — it is a session-owned child component of `GameSession` that pairs the static `Town` definition with visit-scoped `TownVisitState`. It holds `Town Definition` (the static record). Adding `Layout` to `Town` flows through `TownAggregate.Definition` automatically.
- `TownSnapshot` exists in `src/WildBunch.Domain/World/WorldSnapshot.cs` with `FromDomain`/`ToDomain` round-tripping: Id, Name, Services, Prosperity, MapX, MapY, IsOutlier. It does NOT currently round-trip Layout — Task 3 closes this gap.
- `WorldGenerated` event exists at `src/WildBunch.Domain/Events/WorldGenerated.cs` and carries `WorldSnapshot` — this is the event-sourced source of truth for the world.
- `GameSessionEventReplay` at `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` reconstructs the world from `WorldGenerated` during replay.
- `TownServices` enum exists in `WorldModels.cs` with `None = 0` and `Telegraph = 1` only
- `AvailableActionKind` enum exists in `src/WildBunch.Domain/Actions/AvailableActionKind.cs` with Travel, BuySupplies, SendTelegram, ReadWantedPosters, CheckSheriffRecords, LookAroundSaloon, GatherLocalGossip, FollowTelegraphLeads, and others
- `ActionAvailabilityResolver` in `src/WildBunch.Domain/Actions/ActionAvailabilityResolver.cs` always adds BuySupplies; adds SendTelegram when `TownServices.Telegraph` is set; adds investigation actions from `TownSourceCatalog.Default`
- `TownSourceCatalog.Default` in `src/WildBunch.Domain/World/TownSourceModels.cs` includes baseline sources (notice board, local records, local gossip, saloon look-around) with `TownServices.None` and conditional telegraph leads with `TownServices.Telegraph`
- `SeedWorldBuilder` has been deleted by BUNCH-135. `IsCanonicalSeedWorld` is now `SeedWorld.IsCanonical`. Not relevant to this plan's integration site.
- `SeedWorldFactory` at `src/WildBunch.GameContent/NewGame/SeedWorldFactory.cs` (renamed from `SeedWorldCatalog` by BUNCH-135) contains the town construction (`CreateWorld`), prosperity palettes, and services palettes. `CreateWorld` does NOT receive `GameSetupDeterministicSource` — only `SaltSource?` and `Guid? seedCode`.
- `MapGenerator` at `src/WildBunch.GameContent/NewGame/MapGenerator.cs` is the actual integration site for Task 5. Its `Generate(SeedWorld, GameSetupDeterministicSource, GameEntropy, SaltSource?)` method has `source` in scope and calls `SeedWorldFactory.CreateWorld(...)` at line 90. Task 5 post-processes the returned `World` by constructing a new `World` with `Town` records (which support `with` expressions) that have layouts attached, plus the existing trails. `World` is a `sealed class` (NOT a record) — use `new World(townsWithLayouts, world.Trails)`, not `world with { ... }`.
- `CreateCanonicalWorld()` in `SeedWorldFactory` is called by `StartingTownCatalog` and `SeedWorldMapLayout` for the start-screen map only. It does NOT go through `MapGenerator` and does NOT need layouts.
- `PhaserMapHost` pattern exists at `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`
- `GameSessionMapper` exists at `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`; `ToDto(DomainTown town)` maps Id, Name, Services, MapX, MapY — Task 6 extends this with Layout.
- `TownDto` exists at `src/WildBunch.Application/Games/Models/GameDtos.cs` with minimal fields: Id, Name, Services, MapX, MapY — Task 6 adds Layout as a deliberate minimal extension.
- `GetGameSessionHandler` exists at `src/WildBunch.Application/Games/Queries/GetGameSessionHandler.cs` and returns `GameSessionDto` via `GameSessionMapper.ToDto` — no new endpoint needed.
- `TownHubSurface.tsx` exists at `src/WildBunch.Web/src/flow/TownHubSurface.tsx` with card grid pattern driven by AvailableActionKind. It has cards for Store, Sheriff, Saloon, Trailhead — NO telegraph card. Telegraph actions are handled via action handlers, not place navigation. BUNCH-124 updated it to use `useNavigate` from `@tanstack/react-router` for place navigation (not `onPlaceChange` callbacks).
- `GameFlowRouter.tsx` has been DELETED by BUNCH-124. URL routing is now handled by TanStack Router at `src/WildBunch.Web/src/shell/router.tsx` with routes: `/town`, `/town/store`, `/town/sheriff`, `/town/saloon`, `/town/trailhead`, `/trail`. Each route is lazy-loaded. `usePhaseRouteSync` at `src/WildBunch.Web/src/shell/usePhaseRouteSync.ts` reconciles URL with backend game phase.
- BUNCH-124 test patterns: `AppShell.test.tsx` mocks TanStack Router; `routingConventions.test.tsx` verifies route structure; `usePhaseRouteSync.test.tsx` verifies phase-to-URL sync. Task 10 tests should follow these patterns.
- Frontend `TownDto` in `src/WildBunch.Web/src/api/types.ts` (line 310-314) currently has only `{ id, name, services }` — must be extended with `layout?: TownLayoutDto | null` in Task 9.
- Frontend data path: `useGameSession()` → `session.world.towns.find(t => t.id === session.player.currentTownId)` = `currentTown` (per `useGameSessionQueries.ts:38-42`). Once `TownDto` has `layout`, `currentTown.layout` gives the layout for the Phaser scene.
- `PhaserMapHost.test.tsx` at `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx` mocks Phaser via `vi.mock("phaser", ...)` — tests verify scene construction and callback wiring, NOT visual rendering. Task 8 follows this pattern.
- `GameSetupDeterministicSource` exists in `src/WildBunch.GameContent/NewGame/` and is available in `MapGenerator.Generate` (NOT in `SeedWorldFactory.CreateWorld`).
- Architecture guardrails at `.agents/docs/architecture-guardrails.md` confirm: DDD + CQRS + Event Sourcing is the established stack; GameSession is the aggregate root; event stream is source of truth; JSON snapshots are cache; seeded setup requires explicit seams.

All file paths and patterns match current source. The plan has been repaired to align with the repo's architecture stack after a full review, plus passes to close 7 execution gaps (source availability, Telegraph routing, Phaser test pattern, frontend data path, CreateCanonicalWorld scope, BUNCH-135 rename, parity test wording), plus a BUNCH-124 alignment pass (TanStack Router URL routing, `GameFlowRouter` deletion, `useNavigate` pattern), plus an execution confidence assessment pass that closed critical gaps: `World` is a sealed class not a record (fixed code sketch from `world with { ... }` to `new World(...)`), concrete domain model shapes specified, TownLayoutGenerator algorithm specified, PhaserTownHubHost prop interface specified, TownHubScene visual/interaction design specified, test mock pattern specified.
