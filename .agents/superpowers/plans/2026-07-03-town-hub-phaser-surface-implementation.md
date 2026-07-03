# Town Hub Phaser Surface Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a top-down town layout where players click on buildings to navigate, with each town having a unique building arrangement based on available services and layouts that persist across revisits.

**Architecture:** React host component manages Phaser game instance lifecycle following the existing PhaserMapHost pattern. Backend extends SeedWorldBuilder to generate seeded town layouts. Town record (in WorldModels.cs) gets layout property. React drives all state, Phaser is pure renderer.

**Scope:** Town layout generation, Town model extension, React/Phaser integration for building navigation. Future town features (new building types, dynamic building availability changes, richer town events) are out of scope.

**Tech Stack:** Phaser 3, React, TypeScript, C#/.NET, styled-components

## Global Constraints

- Follow existing PhaserMapHost pattern for React-Phaser integration
- Use existing GameSetupDeterministicSource for seeded layout generation
- Extend existing TownAggregate, do not modify aggregate boundaries
- React manages all state, Phaser scenes do not maintain local state
- Use procedural assets (Phaser graphics primitives) in Phase 1
- Follow existing styled-components pattern for React styling
- Maintain existing useGameSession hook integration
- No database migration needed (greenfield project)

---

## File Structure

**New Domain Files:**
- `src/WildBunch.Domain/World/TownLayout.cs` - Town layout domain model
- `src/WildBunch.Domain/World/BuildingPlacement.cs` - Building placement domain model
- `src/WildBunch.Domain/World/BuildingKind.cs` - Building kind enum

**New GameContent Files:**
- `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs` - Layout generation algorithm

**New Application Files:**
- `src/WildBunch.Application/Games/Models/TownLayoutDto.cs` - Town layout DTO
- `src/WildBunch.Application/Games/Models/BuildingPlacementDto.cs` - Building placement DTO
- `src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs` - Layout mapping logic

**New API Files:**
- `src/WildBunch.Api/Games/GetTownLayoutEndpoint.cs` - API endpoint

**New Web Files:**
- `src/WildBunch.Web/src/components/town-hub/PhaserTownHubHost.tsx` - React host component
- `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts` - Phaser scene
- `src/WildBunch.Web/src/components/town-hub/types.ts` - TypeScript types

**Modified Domain Files:**
- `src/WildBunch.Domain/World/WorldModels.cs` - Add Layout property to Town record
- `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs` - Add layout generation call

**Modified Application Files:**
- `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs` - Add layout mapping
- `src/WildBunch.Application/Games/Models/GameDtos.cs` - Extend TownDto

**Modified Web Files:**
- `src/WildBunch.Web/src/flow/GameFlowRouter.tsx` - Integrate town hub surface
- `src/WildBunch.Web/src/flow/TownHubSurface.tsx` - Replace card grid with Phaser surface

---

### Task 1: Add Town Layout Domain Models

**Files:**
- Create: `src/WildBunch.Domain/World/TownLayout.cs`
- Create: `src/WildBunch.Domain/World/BuildingPlacement.cs`
- Create: `src/WildBunch.Domain/World/BuildingKind.cs`
- Test: `tests/WildBunch.Domain.Tests/World/TownLayoutTests.cs`

**Interfaces:**
- Produces: `TownLayout` class with Buildings and PlayerSpawnPosition
- Produces: `BuildingPlacement` class with BuildingId, Kind, Position, Rotation
- Produces: `BuildingKind` enum with Trailhead only (current TownServices only has Telegraph)

- [ ] **Step 1: Write the failing test for TownLayout creation**

```csharp
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests.World;

public class TownLayoutTests
{
    [Fact]
    public void CreateTownLayout_WithValidData_Succeeds()
    {
        var buildings = new List<BuildingPlacement>
        {
            new BuildingPlacement("trailhead-1", BuildingKind.Trailhead, (10, 20), 0)
        };
        
        var layout = new TownLayout(buildings, (50, 50));
        
        Assert.Single(layout.Buildings);
        Assert.Equal((50, 50), layout.PlayerSpawnPosition);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TownLayoutTests" -v`
Expected: FAIL with "TownLayout not defined"

- [ ] **Step 3: Write minimal implementation**

Create `TownLayout`, `BuildingPlacement`, and `BuildingKind` (Trailhead only for now) in `src/WildBunch.Domain/World/`. Follow existing record pattern in WorldModels.cs.

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
- Produces: Town record with Layout property

- [ ] **Step 1: Write the failing test for Town with Layout**

```csharp
[Fact]
public void Town_WithLayout_ReturnsLayout()
{
    var layout = new TownLayout(new List<BuildingPlacement>(), (50, 50));
    var town = new Town(
        new TownId("town-1"),
        "Test Town",
        TownServices.Telegraph,
        layout: layout);
    
    Assert.NotNull(town.Layout);
    Assert.Equal(layout, town.Layout);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~WorldModelsTests" -v`
Expected: FAIL with Layout property not defined

- [ ] **Step 3: Add Layout property to Town record**

Add optional `TownLayout? Layout` property to the Town record in WorldModels.cs. Follow existing record pattern.

- [ ] **Step 4: Update Town construction calls to provide layout parameter**

Update Town construction calls in SeedWorldBuilder and test fixtures to pass layout parameter (use null for now).

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~WorldModelsTests" -v`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/World/WorldModels.cs tests/WildBunch.Domain.Tests/World/WorldModelsTests.cs
git commit -m "feat: add Layout property to Town record"
```

---

### Task 3: Implement Town Layout Generation Algorithm

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`

**Interfaces:**
- Consumes: `TownLayout`, `BuildingPlacement`, `BuildingKind` from Task 1
- Consumes: existing SeedWorldBuilder coordinate system
- Produces: `TownLayoutGenerator.GenerateLayout` method

- [ ] **Step 1: Write the failing test for layout generation**

Write test that verifies:
- Same seed produces same layout (determinism)
- Layout includes trailhead building
- Positional rules (trailhead at edge, player spawn in center)

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TownLayoutGeneratorTests" -v`
Expected: FAIL with "TownLayoutGenerator not defined"

- [ ] **Step 3: Write minimal implementation**

Create `TownLayoutGenerator` in `src/WildBunch.GameContent/NewGame/`. Use existing `GameSetupDeterministicSource` for seeded random. Place trailhead at town edge, player spawn in center. For now, only generate trailhead building (current TownServices only has Telegraph).

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TownLayoutGeneratorTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs
git commit -m "feat: implement town layout generation algorithm"
```

---

### Task 4: Integrate Layout Generation into SeedWorldBuilder

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/SeedWorldBuilderTests.cs`

**Interfaces:**
- Consumes: `TownLayoutGenerator.GenerateLayout` from Task 3
- Produces: SeedWorldBuilder that generates layouts for each town

- [ ] **Step 1: Write the failing test for layout integration**

Verify that CreateWorld generates Town records with non-null Layout properties.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~SeedWorldBuilderTests" -v`
Expected: FAIL with Layout being null

- [ ] **Step 3: Modify SeedWorldBuilder.CreateWorld to generate layouts**

Integrate `TownLayoutGenerator.GenerateLayout` call into town creation. Follow existing SeedWorldBuilder pattern for coordinate derivation.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~SeedWorldBuilderTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs tests/WildBunch.GameContent.Tests/NewGame/SeedWorldBuilderTests.cs
git commit -m "feat: integrate layout generation into SeedWorldBuilder"
```

---

### Task 5: Add Town Layout DTOs and Mapping

**Files:**
- Create: `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`
- Create: `src/WildBunch.Application/Games/Models/BuildingPlacementDto.cs`
- Create: `src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs`
- Test: `tests/WildBunch.Application.Tests/Mapping/TownLayoutMapperTests.cs`

**Interfaces:**
- Consumes: `TownLayout`, `BuildingPlacement`, `BuildingKind` from Task 1
- Produces: DTOs for API
- Produces: Mapping logic

- [ ] **Step 1: Write the failing test for DTO mapping**

Verify round-trip mapping from domain to DTO preserves layout structure.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~TownLayoutMapperTests" -v`
Expected: FAIL with "TownLayoutMapper not defined"

- [ ] **Step 3: Write minimal implementation**

Create DTOs following existing DTO pattern in GameDtos.cs. Create mapper following existing mapper pattern in GameSessionMapper.cs.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~TownLayoutMapperTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Games/Models/TownLayoutDto.cs src/WildBunch.Application/Games/Models/BuildingPlacementDto.cs src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs tests/WildBunch.Application.Tests/Mapping/TownLayoutMapperTests.cs
git commit -m "feat: add town layout DTOs and mapping"
```

---

### Task 6: Extend GameSessionMapper to Include Layout

**Files:**
- Modify: `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`
- Modify: `src/WildBunch.Application/Games/Models/GameDtos.cs`
- Test: `tests/WildBunch.Application.Tests/Mapping/GameSessionMapperTests.cs`

**Interfaces:**
- Consumes: `TownLayoutMapper.ToDto` from Task 5
- Produces: Extended TownDto with Layout property

- [ ] **Step 1: Write the failing test for layout in TownDto**

Verify that GameSessionMapper.ToDto includes TownLayoutDto in TownDto.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~GameSessionMapperTests" -v`
Expected: FAIL with Layout being null

- [ ] **Step 3: Extend TownDto to include Layout**

Add optional TownLayoutDto property to TownDto following existing DTO pattern.

- [ ] **Step 4: Update GameSessionMapper to map layout**

Integrate TownLayoutMapper.ToDto call in town mapping logic.

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~GameSessionMapperTests" -v`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs src/WildBunch.Application/Games/Models/GameDtos.cs tests/WildBunch.Application.Tests/Mapping/GameSessionMapperTests.cs
git commit -m "feat: extend GameSessionMapper to include town layout"
```

---

### Task 7: Add Town Layout API Endpoint

**Files:**
- Create: `src/WildBunch.Api/Games/GetTownLayoutEndpoint.cs`
- Test: `tests/WildBunch.Api.Tests/Games/GetTownLayoutEndpointTests.cs`

**Interfaces:**
- Consumes: `TownLayoutDto` from Task 5
- Produces: GET endpoint for town layout

- [ ] **Step 1: Write the failing test for API endpoint**

Verify endpoint returns TownLayoutDto for current town.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Api.Tests/WildBunch.Api.Tests.csproj --filter "FullyQualifiedName~GetTownLayoutEndpointTests" -v`
Expected: FAIL with 404 Not Found

- [ ] **Step 3: Implement API endpoint**

Create endpoint following existing endpoint pattern in Games folder. Use existing GetGameSessionHandler. Return TownLayoutDto from current town.

- [ ] **Step 4: Register endpoint in Program.cs**

Register endpoint following existing endpoint registration pattern.

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Api.Tests/WildBunch.Api.Tests.csproj --filter "FullyQualifiedName~GetTownLayoutEndpointTests" -v`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Api/Games/GetTownLayoutEndpoint.cs tests/WildBunch.Api.Tests/Games/GetTownLayoutEndpointTests.cs
git commit -m "feat: add town layout API endpoint"
```

---

### Task 8: Create Phaser Town Hub Scene

**Files:**
- Create: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`
- Create: `src/WildBunch.Web/src/components/town-hub/types.ts`
- Test: `src/WildBunch.Web/src/tests/TownHubScene.test.tsx`

**Interfaces:**
- Consumes: `TownLayoutDto` from Task 5
- Produces: `TownHubScene` Phaser scene with building rendering and click handling

- [ ] **Step 1: Write the failing test for scene creation**

Verify scene can be instantiated with layout data and renders buildings.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- TownHubScene.test.tsx`
Expected: FAIL with "TownHubScene not defined"

- [ ] **Step 3: Write minimal implementation**

Create TypeScript types following existing type pattern. Create Phaser scene following PhaserMapHost pattern. Use procedural graphics (rectangles/circles) for buildings and player. Make buildings interactive with click handlers.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- TownHubScene.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/TownHubScene.ts src/WildBunch.Web/src/components/town-hub/types.ts src/WildBunch.Web/src/tests/TownHubScene.test.tsx
git commit -m "feat: create Phaser Town Hub scene"
```

---

### Task 9: Create React Host Component

**Files:**
- Create: `src/WildBunch.Web/src/components/town-hub/PhaserTownHubHost.tsx`
- Test: `src/WildBunch.Web/src/tests/PhaserTownHubHost.test.tsx`

**Interfaces:**
- Consumes: `TownHubScene` from Task 8
- Consumes: `TownLayoutDto` from API
- Produces: `PhaserTownHubHost` React component following PhaserMapHost pattern

- [ ] **Step 1: Write the failing test for host component**

Verify component renders Phaser canvas and handles building selection callbacks.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- PhaserTownHubHost.test.tsx`
Expected: FAIL with "PhaserTownHubHost not defined"

- [ ] **Step 3: Write minimal implementation**

Create React component following PhaserMapHost pattern. Use useEffect for Phaser game lifecycle. Use styled-components for canvas container. Handle building selection callbacks via ref pattern.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- PhaserTownHubHost.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/PhaserTownHubHost.tsx src/WildBunch.Web/src/tests/PhaserTownHubHost.test.tsx
git commit -m "feat: create React host component for Town Hub"
```

---

### Task 10: Integrate Town Hub into Game Flow Router

**Files:**
- Modify: `src/WildBunch.Web/src/flow/TownHubSurface.tsx`
- Test: `src/WildBunch.Web/src/tests/TownHubSurface.test.tsx`

**Interfaces:**
- Consumes: `PhaserTownHubHost` from Task 9
- Consumes: Town layout data from useGameSession
- Produces: Integrated town hub surface in game flow

- [ ] **Step 1: Write the failing test for integration**

Verify TownHubSurface renders PhaserTownHubHost with layout data.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- TownHubSurface.test.tsx`
Expected: FAIL with Phaser canvas not found

- [ ] **Step 3: Modify TownHubSurface to use PhaserTownHubHost**

Replace existing TownHubGrid card grid with PhaserTownHubHost. Convert town layout data from backend format to TypeScript format. Map building clicks to existing place navigation logic. Preserve existing place surface rendering (StorePlace, SheriffPlace, etc.) when active.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- TownHubSurface.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/flow/TownHubSurface.tsx src/WildBunch.Web/src/tests/TownHubSurface.test.tsx
git commit -m "feat: integrate Town Hub Phaser surface into game flow"
```
  if (place) {
    onPlaceChange(place);
  }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- GameFlowRouter.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/flow/GameFlowRouter.tsx src/WildBunch.Web/src/flow/TownHubSurface.tsx src/WildBunch.Web/src/tests/GameFlowRouter.test.tsx
git commit -m "feat: integrate Town Hub Phaser surface into game flow"
```

---

### Task 11: Add Character Movement and Visual Feedback

**Files:**
- Modify: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`
- Test: `src/WildBunch.Web/src/tests/TownHubScene.test.tsx`

**Interfaces:**
- Consumes: Existing scene from Task 8
- Produces: Character movement animation and building availability visual feedback

- [ ] **Step 1: Write the failing test for character movement**

Verify character moves to clicked building position with animation.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- TownHubScene.test.tsx`
Expected: FAIL with movement not implemented

- [ ] **Step 3: Add character movement logic**

Add player sprite tracking. Implement Phaser tween for character movement to clicked building. Add visual feedback (scale, color) for building hover states. Gray out unavailable buildings based on selectableBuildingIds.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- TownHubScene.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/TownHubScene.ts src/WildBunch.Web/src/tests/TownHubScene.test.tsx
git commit -m "feat: add character movement and visual feedback to Town Hub"
```

---

### Task 12: Final Integration Testing and Validation

**Files:**
- Test: `src/WildBunch.Web/src/tests/TownHubIntegration.test.tsx`
- Test: `tests/WildBunch.Integration.Tests/TownHubIntegrationTests.cs`

**Interfaces:**
- Consumes: All previous tasks
- Produces: End-to-end validation of town hub surface

- [ ] **Step 1: Write integration test for full flow**

Verify town hub renders with buildings from backend, navigation works end-to-end.

- [ ] **Step 2: Run integration test**

Run: `npm test -- TownHubIntegration.test.tsx`
Expected: PASS

- [ ] **Step 3: Run backend integration test**

Verify API endpoint returns valid layout with trailhead building.

- [ ] **Step 4: Run backend integration test**

Run: `dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~TownHubIntegrationTests" -v`
Expected: PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test` and `npm test`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Web/src/tests/TownHubIntegration.test.tsx tests/WildBunch.Integration.Tests/TownHubIntegrationTests.cs
git commit -m "test: add Town Hub integration tests"
```

---

## Self-Review

**1. Spec coverage:**
- ✅ Town layout domain models (Task 1)
- ✅ Town record extension (Task 2)
- ✅ Layout generation algorithm (Task 3)
- ✅ SeedWorldBuilder integration (Task 4)
- ✅ DTOs and mapping (Task 5)
- ✅ GameSessionMapper extension (Task 6)
- ✅ API endpoint (Task 7)
- ✅ Phaser scene (Task 8)
- ✅ React host component (Task 9)
- ✅ Game flow integration (Task 10)
- ✅ Character movement and visual feedback (Task 11)
- ✅ Integration testing (Task 12)

**2. Placeholder scan:** No placeholders found - all steps contain concrete instructions.

**3. Type consistency:** All types match across tasks - TownLayout, BuildingPlacement, BuildingKind used consistently.

**4. Architecture compliance:** Follows existing PhaserMapHost pattern, extends Town record (no boundary changes), React-driven state management.

**5. Greenfield project:** No migration steps included - all database changes assume greenfield status.

**6. Live-main reconciliation:** Updated file paths to match current source (WorldModels.cs instead of Town.cs), adjusted BuildingKind to match current TownServices (Trailhead only), simplified route-level instructions.

**7. Preservation requirements:**
- Existing GameSession command route and current player-visible behavior must remain stable
- Existing TownHubSurface card grid replaced with Phaser surface, but underlying navigation logic unchanged
- Phaser remains renderer/input adapter, with React/backend/domain owning truth