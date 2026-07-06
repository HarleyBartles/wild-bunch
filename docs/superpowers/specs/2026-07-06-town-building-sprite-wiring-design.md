# Town Building Sprite Wiring Design

## Problem

Town Hub currently renders colored rectangles as building placeholders. The approved town-building sprites exist in `src/WildBunch.Assets/town-buildings/sprites/` organized by prosperity tier (boomtown/prosperous/poor/destitute) and building family (general-store/sheriff-office/saloon/telegraph-office) with five turnaround views each. The frontend needs to wire these sprites into the Town Hub Phaser surface, using the town's prosperity level to select the correct asset tier and mechanically generating town layouts with road/path connectivity.

## Goal

Make the Town Hub scene render approved town-building sprites instead of placeholders, with mechanical town layout generation (main road north-south, side spurs, deterministic building placement and orientation), and path connectivity between buildings and roads.

## Architecture

The backend already provides prosperity and view data through the domain and DTO chain. The frontend will use Vite's static asset bundling to include the approved sprites from `src/WildBunch.Assets/town-buildings/sprites/` into the web public folder. `TownHubScene` will be updated to load Phaser sprites based on prosperity tier and building view, replacing the current colored rectangle rendering. The layout generator will mechanically calculate building views based on road geometry (main road vs spur) and place buildings with proper orientation.

Data flow: Domain (TownProsperity, BuildingView) → DTOs → Frontend → Phaser Scene → Sprite Rendering. The frontend remains a presentation adapter over authoritative backend state.

## Components

### Frontend Changes

**TownHubScene.ts**
- Add sprite path mapping constants: asset path pattern is `/assets/town-buildings/{prosperity}/{building}/{view}.png`
- Add `preload()` method to load sprites based on layout data
- Update `create()` to render sprites instead of rectangles
- Add sprite mirroring logic for left-facing buildings
- Add path rendering: draw line segments for each path in `layout.paths` using Phaser graphics
- Add fallback to rectangle rendering if sprite loading fails

**Test Files**
- `TownHubScene.test.ts` - Update test fixtures to include prosperity and view fields, verify sprite loading, verify path rendering
- `TownHubSurface.test.tsx` - Update test fixtures with new DTO fields
- `PhaserTownHubHost.test.tsx` - Update test fixtures with new DTO fields

**Frontend DTO Types**
- Add `PathSegmentDto` interface to `src/WildBunch.Web/src/api/types.ts`
- Add `paths` field to `TownLayoutDto` interface

### Backend Changes

**TownLayout.cs**
- Add `IReadOnlyList<PathSegment> Paths` field to store path connectivity data

**PathSegment.cs** (new domain type)
- Simple record: `PathSegment(int StartX, int StartY, int EndX, int EndY)`
- Coordinates in logical units (0-100) matching building placement

**TownLayoutDto.cs**
- Add `paths: PathSegmentDto[]` field

**PathSegmentDto.cs** (new DTO type)
- Simple record: `PathSegmentDto(int StartX, int StartY, int EndX, int EndY)`

**TownLayoutMapper.cs**
- Map domain `PathSegment` to DTO `PathSegmentDto`

**TownLayoutGenerator.cs**
- Encode canonical building positions in the seed (each town has deterministic building placement for the same seed)
- Enhance view selection logic with mechanical angle calculation based on road attachment
- Implement main road north-south layout (x=50, y 0-100)
- Implement side spur generation (1-2 spurs branching east/west at seeded positions)
- Add building-to-road attachment detection: buildings within 15 logical units of x=50 attach to main road, others attach to spurs
- Implement view selection: vertical road (75% FrontOblique, 25% Profile), horizontal road (33% Front, 33% FrontOblique, 33% FrontOblique mirrored)
- Generate path segments from each building center to nearest road point

**Test File**
- Add brute force test in `WildBunch.GameContent.Tests` to verify layout determinism across seed/salt combinations
- Add unit test for view selection logic across road attachment scenarios

### Build Configuration

**vite.config.ts**
- Use `vite-plugin-static-copy` to bundle assets from `src/WildBunch.Assets/town-buildings/sprites/` to public folder during build
- Assets will be available at `/assets/town-buildings/` path in the running app

## Data Flow

1. **Backend Generation:** `TownLayoutGenerator.GenerateLayout()` mechanically calculates:
   - Road network (main road north-south, side spurs east/west)
   - Building positions along roads (canonical positions encoded in seed)
   - Building views based on road attachment (vertical vs horizontal road, seeded ratios)
   - Path segments connecting buildings to roads (line segments from building center to nearest road point)
   - Creates `TownLayout` with prosperity tier, calculated `BuildingView` for each placement, and path segments

3. **DTO Mapping:** `TownLayoutMapper.ToDto()` maps domain `TownLayout` to `TownLayoutDto` including prosperity, pre-calculated view fields, and path segments

4. **Session Mapping:** `GameSessionMapper.ToDto()` includes town prosperity in `TownDto` when building `WorldDto`

5. **Frontend Consumption:** `TownHubScene` receives `TownLayoutDto` with prosperity and pre-calculated building view data

6. **Sprite Loading:** `TownHubScene.preload()` constructs asset paths using the provided view enum values and prosperity tier

7. **Sprite Rendering:** `TownHubScene.create()` renders sprites using the provided view, mirrors sprites based on road direction (left side of main road, spur direction)

## Mechanical Building Direction Formula

**Road Attachment Detection:**
- For each building, determine which road segment it attaches to by checking proximity to road segments
- Main road: vertical segment at x=50 (y from 0-100)
- Side spurs: horizontal segments branching east/west at specific y coordinates

**View Selection:**
- **Vertical road (main road):** 75% FrontOblique (hero view), 25% Profile (deterministic seed)
- **Horizontal road (spurs):** 33% Front, 33% FrontOblique, 33% FrontOblique mirrored (deterministic seed), no side bias

**Mirroring:**
- Main road: left side (x < 50) → mirror, right side (x > 50) → no mirror
- Spurs: mirror based on spur direction (east spur → mirror, west spur → no mirror)

**Path Connectivity:**
- Draw short line segments from each building to its attached road segment to prove connectivity (line drawing for now, tiles in future work)

## Error Handling

- **Asset Loading Failures:** If a sprite fails to load in Phaser, gracefully fall back to colored rectangle placeholder (existing behavior) to prevent scene break
- **Missing View Enum Values:** If a building placement has invalid `BuildingView`, default to `BuildingView.FrontOblique` and log warning
- **Missing Prosperity Tier:** If a town has invalid `TownProsperity`, default to `TownProsperity.Prosperous` and log warning

## Testing

### Frontend Tests

**Unit Tests:**
- Update `TownHubScene.test.ts` to verify sprite loading with different prosperity tiers and view angles
- Add test case for fallback to rectangle rendering if sprite loading fails
- Verify sprite mirroring logic for left-facing buildings

**Integration Tests:**
- Update `TownHubSurface.test.tsx` and `PhaserTownHubHost.test.tsx` test fixtures to include prosperity and view fields
- Verify end-to-end flow from DTO to sprite rendering

### Backend Tests

**Unit Tests:**
- Add unit test for view selection logic across road attachment scenarios (main road vs spur, different positions)
- Test path connectivity line drawing

**Brute Force Test:**
- Add brute force test in `WildBunch.GameContent.Tests` following the pattern of `MapGeneratorBruteForceAnalysisTests`
- Iterate over combination matrix: 100 representative seed codes, all town slot indices (0-9), all prosperity levels (4), all service flags (reasonable subset)
- Collect data in single pass: view distribution by road type, spur count distribution, building position ranges
- Assert statistical expectations: view ratios match intended (75/25 for vertical, 33/33/33 for horizontal), spur count distribution is reasonable
- Assert anti-pattern detection: no duplicate building placements, all buildings have valid views, no buildings outside scene bounds
- Ensures mechanical angle calculation is consistent and no hidden randomness

**Test Kind:**
- Frontend tests: Unit tests for scene logic, integration tests for React component integration
- Backend tests: Unit tests for layout logic, brute-force tests for determinism (GameContent test kind)

## Constraints

- Do NOT move asset custody to web public tree manually - use Vite bundling
- Use approved prosperity tiers: boomtown, prosperous, poor, destitute
- Use approved building views: front, profile, rear, front-oblique, rear-oblique
- Follow existing frontend standards (styled-components, no plain CSS classes)
- Follow TDD discipline - write failing tests first
- Keep changes scoped to the approved issue scope
- Do not expand into new town gameplay systems, combat, or unrelated HUD work
- Main road runs north-south, side spurs branch east/west
- Buildings always have their front to the road
- Vertical road: 75% FrontOblique, 25% Profile bias
- Horizontal road: 33% Front, 33% FrontOblique, 33% FrontOblique mirrored, no side bias
- Path rendering: line drawing for this slice, tiles are future work
