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
- Add sprite path mapping constants (prosperity tier folders, building family folders, view filenames)
- Add `preload()` method to load sprites based on layout data
- Update `create()` to render sprites instead of rectangles
- Add sprite mirroring logic for left-facing buildings
- Add fallback to rectangle rendering if sprite loading fails

**Test Files**
- `TownHubScene.test.ts` - Update test fixtures to include prosperity and view fields, verify sprite loading
- `TownHubSurface.test.tsx` - Update test fixtures with new DTO fields
- `PhaserTownHubHost.test.tsx` - Update test fixtures with new DTO fields

### Backend Changes

**TownLayoutGenerator.cs**
- Enhance view selection logic with mechanical angle calculation based on road attachment
- Implement main road north-south layout (x=50, y 0-100)
- Implement side spur generation (1-2 spurs branching east/west at seeded positions)
- Add building-to-road attachment detection
- Implement view selection: vertical road (75% FrontOblique, 25% Profile), horizontal road (50% Front, 50% FrontOblique)
- Add path drawing (line segments from buildings to roads) - for this slice, paths are rendered as lines in the frontend, tiles are future work

**Test File**
- Add brute force test in `WildBunch.GameContent.Tests` to verify layout determinism across seed/salt combinations
- Add unit test for view selection logic across road attachment scenarios

### Build Configuration

**vite.config.ts**
- Configure Vite to bundle assets from `src/WildBunch.Assets/town-buildings/sprites/` to public folder

## Data Flow

1. **Backend Generation:** `TownLayoutGenerator.GenerateLayout()` mechanically calculates:
   - Road network (main road north-south, side spurs east/west)
   - Building positions along roads
   - Building views based on road attachment (vertical vs horizontal road, seeded ratios)
   - Path segments connecting buildings to roads
   - Creates `TownLayout` with prosperity tier and calculated `BuildingView` for each placement

3. **DTO Mapping:** `TownLayoutMapper.ToDto()` maps domain `TownLayout` to `TownLayoutDto` including prosperity and pre-calculated view fields

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
- **Horizontal road (spurs):** 50% Front, 50% FrontOblique (deterministic seed), no side bias

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
- Add brute force test in `WildBunch.GameContent.Tests` to verify `TownLayoutGenerator` produces deterministic layouts across all seed/salt combinations
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
- Horizontal road: 50% Front, 50% FrontOblique, no side bias
- Path rendering: line drawing for this slice, tiles are future work
