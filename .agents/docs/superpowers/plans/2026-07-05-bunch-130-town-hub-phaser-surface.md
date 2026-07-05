# Town Hub Phaser Surface Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Linear issue:** [BUNCH-130](https://linear.app/harleys-workspace/issue/BUNCH-130/town-hub-phaser-surface)

**Goal:** Implement a top-down town layout where players click on buildings to navigate, with each town having a unique building arrangement based on available services and layouts that persist across revisits.

**Architecture:** React host component manages Phaser game instance lifecycle following the existing PhaserMapHost pattern. Backend extends SeedWorldBuilder to generate seeded town layouts. Town record (in WorldModels.cs) gets layout property. React drives all state, Phaser is pure renderer.

**Component Decision:** Extend existing Town record with layout property. Do not create separate aggregate root. Layout generation is a deterministic projection from town seed and services.

**Scope:** Town layout generation, Town model extension, React/Phaser integration for building navigation. Future town features (new building types, dynamic building availability changes, richer town events) are out of scope.

**Tech Stack:** Phaser 3, React, TypeScript, C#/.NET, styled-components

## Global Constraints

- Follow existing PhaserMapHost pattern for React-Phaser integration
- Use existing GameSetupDeterministicSource for seeded layout generation
- Extend existing Town record, do not modify aggregate boundaries
- React manages all state, Phaser scenes do not maintain local state
- Use procedural assets (Phaser graphics primitives) in Phase 1
- Follow existing styled-components pattern for React styling
- Maintain existing useGameSession hook integration
- No database migration needed (greenfield project)

---

## Model Decision: Baseline vs Service Buildings

**Baseline navigation affordances:** Every town needs a trailhead (exit point) regardless of services. This is a structural requirement for gameplay.

**Service-driven buildings:** Optional buildings derive from TownServices flags:

- `TownServices.Telegraph` → Telegraph office building
- Future services will add their corresponding buildings

**BuildingKind vs TownServices:** BuildingKind is a visual representation enum for rendering, not a service flag. TownServices is a domain service flag. The layout generator maps from TownServices to BuildingKind during layout generation.

---

## File Structure

**New Domain Files:**

- `src/WildBunch.Domain/World/TownLayout.cs` - Town layout domain model
- `src/WildBunch.Domain/World/BuildingPlacement.cs` - Building placement domain model
- `src/WildBunch.Domain/World/BuildingKind.cs` - Building kind enum (visual representation)

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

- `src/WildBunch.Web/src/flow/TownHubSurface.tsx` - Replace card grid with Phaser surface

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
- Produces: `BuildingKind` enum (Trailhead, Telegraph, future building types)

- [ ] **Step 1: Write the failing test for TownLayout creation**

Write test that verifies TownLayout can be created with buildings and player spawn position.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TownLayoutTests" -v`
Expected: FAIL with "TownLayout not defined"

- [ ] **Step 3: Write minimal implementation**

Create `TownLayout`, `BuildingPlacement`, and `BuildingKind` in `src/WildBunch.Domain/World/`. Follow existing record pattern in WorldModels.cs. BuildingKind should include Trailhead (baseline) and Telegraph (current service), with room for future building types.

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

Write test that verifies Town record can hold a Layout property.

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
- Consumes: `TownServices` from Town record
- Produces: `TownLayoutGenerator.GenerateLayout` method

- [ ] **Step 1: Write the failing test for layout generation**

Write test that verifies:

- Same seed produces same layout (determinism)
- Layout always includes trailhead (baseline affordance)
- Layout includes telegraph building when TownServices.Telegraph is set
- Layout excludes telegraph building when TownServices.None

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TownLayoutGeneratorTests" -v`
Expected: FAIL with "TownLayoutGenerator not defined"

- [ ] **Step 3: Write minimal implementation**

Create `TownLayoutGenerator` in `src/WildBunch.GameContent/NewGame/`. Use existing `GameSetupDeterministicSource` for seeded random. Map from TownServices to BuildingKind: always generate Trailhead, generate Telegraph only when TownServices.Telegraph is set. Place trailhead at town edge, player spawn in center.

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

Write test that verifies CreateWorld generates Town records with non-null Layout properties.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~SeedWorldBuilderTests" -v`
Expected: FAIL

- [ ] **Step 3: Integrate TownLayoutGenerator into SeedWorldBuilder**

Call `TownLayoutGenerator.GenerateLayout` during town construction in SeedWorldBuilder. Pass the town seed and TownServices.

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
- Test: `tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperTests.cs`

**Interfaces:**

- Consumes: `TownLayout`, `BuildingPlacement`, `BuildingKind` from Task 1
- Produces: `TownLayoutDto`, `BuildingPlacementDto`
- Produces: `TownLayoutMapper.ToDto` method

- [ ] **Step 1: Write the failing test for DTO mapping**

Write test that verifies TownLayoutMapper.ToDto maps TownLayout to TownLayoutDto correctly.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~TownLayoutMapperTests" -v`
Expected: FAIL

- [ ] **Step 3: Write minimal implementation**

Create DTOs and mapper. Follow existing DTO patterns in GameDtos.cs and existing mapper patterns in GameSessionMapper.cs.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~TownLayoutMapperTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Games/Models/TownLayoutDto.cs src/WildBunch.Application/Games/Models/BuildingPlacementDto.cs src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperTests.cs
git commit -m "feat: add town layout DTOs and mapping"
```

---

### Task 6: Extend GameSessionMapper and TownDto with Layout

**Files:**

- Modify: `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`
- Modify: `src/WildBunch.Application/Games/Models/GameDtos.cs`
- Test: `tests/WildBunch.Application.Tests/Games/Mapping/GameSessionMapperTests.cs`

**Interfaces:**

- Consumes: `TownLayoutMapper.ToDto` from Task 5
- Produces: TownDto with Layout property
- Produces: GameSessionMapper that maps town layout

- [ ] **Step 1: Write the failing test for TownDto with Layout**

Write test that verifies TownDto includes Layout property and GameSessionMapper maps it correctly.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~GameSessionMapperTests" -v`
Expected: FAIL

- [ ] **Step 3: Add Layout to TownDto and GameSessionMapper**

Add `TownLayoutDto? Layout` to TownDto in GameDtos.cs. Update GameSessionMapper to map town layout using TownLayoutMapper.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~GameSessionMapperTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs src/WildBunch.Application/Games/Models/GameDtos.cs tests/WildBunch.Application.Tests/Games/Mapping/GameSessionMapperTests.cs
git commit -m "feat: extend GameSessionMapper and TownDto with layout"
```

---

### Task 7: Add Town Layout API Endpoint

**Files:**

- Create: `src/WildBunch.Api/Games/GetTownLayoutEndpoint.cs`
- Test: `tests/WildBunch.Api.Tests/Games/GetTownLayoutEndpointTests.cs`

**Interfaces:**

- Consumes: `TownLayoutDto` from Task 5
- Produces: GET endpoint that returns town layout by session and town id

- [ ] **Step 1: Write the failing test for the endpoint**

Write test that verifies the endpoint returns town layout for a valid session and town id.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Api.Tests/WildBunch.Api.Tests.csproj --filter "FullyQualifiedName~GetTownLayoutEndpointTests" -v`
Expected: FAIL

- [ ] **Step 3: Write minimal implementation**

Create the endpoint following existing endpoint patterns in `src/WildBunch.Api/Games/`. The endpoint should accept a session id and town id, load the session, and return the town layout DTO.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Api.Tests/WildBunch.Api.Tests.csproj --filter "FullyQualifiedName~GetTownLayoutEndpointTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Api/Games/GetTownLayoutEndpoint.cs tests/WildBunch.Api.Tests/Games/GetTownLayoutEndpointTests.cs
git commit -m "feat: add town layout API endpoint"
```

---

### Task 8: Implement Phaser TownHubScene

**Files:**

- Create: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`
- Create: `src/WildBunch.Web/src/components/town-hub/types.ts`
- Test: `src/WildBunch.Web/src/tests/TownHubScene.test.ts`

**Interfaces:**

- Consumes: town layout data from React props
- Produces: Phaser scene that renders buildings and handles click navigation
- Produces: TypeScript types for town layout

- [ ] **Step 1: Write the failing test for TownHubScene**

Write test that verifies TownHubScene renders buildings from layout data and emits navigation events on building click.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run TownHubScene`
Expected: FAIL

- [ ] **Step 3: Write minimal implementation**

Create `TownHubScene` extending Phaser.Scene. Render buildings as Phaser graphics primitives (rectangles for buildings, circle for player). Implement click detection on buildings that emits a navigation event. Player auto-walks to clicked building using Phaser tweens. Create `types.ts` with TypeScript interfaces matching the DTOs.

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
- Test: `src/WildBunch.Web/src/tests/PhaserTownHubHost.test.tsx`

**Interfaces:**

- Consumes: `TownHubScene` from Task 8
- Consumes: town layout data from useGameSession
- Produces: React component that manages Phaser game instance lifecycle

- [ ] **Step 1: Write the failing test for PhaserTownHubHost**

Write test that verifies PhaserTownHubHost creates a Phaser game instance and passes town layout data to the scene.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run PhaserTownHubHost`
Expected: FAIL

- [ ] **Step 3: Write minimal implementation**

Create `PhaserTownHubHost` following the existing `PhaserMapHost` pattern. Use a ref to hold the Phaser game instance. Pass town layout data and navigation callback to TownHubScene via scene data. Clean up the Phaser game on unmount.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --run PhaserTownHubHost`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/PhaserTownHubHost.tsx src/WildBunch.Web/src/tests/PhaserTownHubHost.test.tsx
git commit -m "feat: implement React PhaserTownHubHost component"
```

---

### Task 10: Integrate Town Hub into GameFlowRouter

**Files:**

- Modify: `src/WildBunch.Web/src/flow/TownHubSurface.tsx`
- Modify: `src/WildBunch.Web/src/flow/GameFlowRouter.tsx`
- Test: `src/WildBunch.Web/src/tests/TownHubSurface.test.tsx`

**Interfaces:**

- Consumes: `PhaserTownHubHost` from Task 9
- Produces: TownHubSurface that renders Phaser surface instead of card grid
- Produces: GameFlowRouter that routes to Phaser town hub

- [ ] **Step 1: Write the failing test for Phaser town hub integration**

Write test that verifies TownHubSurface renders PhaserTownHubHost instead of card grid.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run TownHubSurface`
Expected: FAIL

- [ ] **Step 3: Replace card grid with Phaser surface**

Update TownHubSurface to render `PhaserTownHubHost` instead of the card grid. Keep the existing place navigation logic (store, sheriff, saloon, trailhead) but trigger it from Phaser building clicks instead of card clicks. Update GameFlowRouter if needed.

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --run TownHubSurface`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/flow/TownHubSurface.tsx src/WildBunch.Web/src/flow/GameFlowRouter.tsx src/WildBunch.Web/src/tests/TownHubSurface.test.tsx
git commit -m "feat: integrate Phaser town hub into GameFlowRouter"
```

---

### Task 11: Add Visual Feedback for Available/Unavailable Buildings

**Files:**

- Modify: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`
- Test: `src/WildBunch.Web/src/tests/TownHubScene.test.ts`

**Interfaces:**

- Consumes: available actions from useGameSession
- Produces: Visual differentiation for available vs unavailable buildings

- [ ] **Step 1: Write the failing test for visual feedback**

Write test that verifies available buildings render differently from unavailable buildings.

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --run TownHubScene`
Expected: FAIL

- [ ] **Step 3: Implement visual feedback**

Pass available actions to TownHubScene. Render available buildings with full opacity and a highlight. Render unavailable buildings with reduced opacity and no highlight. Disable click on unavailable buildings.

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
Expected: All tests pass

- [ ] **Step 2: Run all frontend tests**

Run: `npm test -- --run`
Expected: All tests pass

- [ ] **Step 3: Run frontend lint and typecheck**

Run: `npm run lint && npm run typecheck`
Expected: No errors

- [ ] **Step 4: Validate build**

Run: `dotnet build`
Expected: Build succeeds

- [ ] **Step 5: Commit any final fixes**

```bash
git add -A
git commit -m "test: validate full suite for town hub phaser surface"
```

---

## Staleness Check

This plan was verified against current source on 2026-07-05:

- `Town` record exists in `src/WildBunch.Domain/World/WorldModels.cs` with expected signature
- `TownServices` enum exists with `None = 0` and `Telegraph = 1`
- `PhaserMapHost` pattern exists at `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`
- `GameSessionMapper` exists at `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`
- `GameDtos.cs` exists at `src/WildBunch.Application/Games/Models/GameDtos.cs`
- `TownHubSurface.tsx` exists at `src/WildBunch.Web/src/flow/TownHubSurface.tsx` with card grid pattern
- `GameSetupDeterministicSource` exists in `src/WildBunch.GameContent/NewGame/`
- `SeedWorldBuilder` exists in `src/WildBunch.GameContent/NewGame/`

All file paths and patterns match current source.
