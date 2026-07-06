# Town Hub Tile-Based Layout Design

## Problem

The current implementation uses 8 named canonical layout patterns (HubAndSpoke, LinearChain, etc.) with hardcoded building positions. This overcomplicates the system and doesn't align with the actual invariants of town hub layouts. The system should use a simpler, tile-based approach that encodes road topology and placement strategy efficiently, with prosperity controlling density.

## Goal

Replace the 8 canonical layout patterns with a tile-based layout system that:
- Encodes road topology (spur count, spur positions, spur direction) and placement strategy in 4 bits
- Uses a 10x10 tile grid for layout
- Makes prosperity control building density
- Provides seams for future expansion (more spurs, wider spurs, more buildings)

## Architecture

The system uses a tile-based grid approach where:
- The palette encodes road topology and placement strategy (4 bits = 16 combinations)
- Prosperity determines building density (how many zones are filled vs empty)
- The generator deterministically assigns buildings to tile positions based on seed

Data flow: Seed → BuildingLayoutPalette → Road Topology + Placement Strategy → Prosperity → Density → Tile Assignment → BuildingPlacement + PathSegment

## Components

### Tile Grid System

**Grid Dimensions:**
- 10 tiles wide × 10 tiles tall
- Each tile maps to 10 logical units (100x100 logical units total)

**Major Road (Vertical):**
- Center column: 2 tiles wide, runs full height (rows 0-9)
- Building zones: 1 tile on each side of the road (columns 0 and 3)
- Center corridor total: 4 tiles wide = [building zone left] [road tile 1] [road tile 2] [building zone right]

**Spur Roads (Horizontal):**
- Spurs are 1 tile tall
- Spurs are at least 2 tiles wide:
  - 1 tile where a building would be in the vertical corridor (spur start, no building there)
  - 1 tile further beyond the building zone
- Spurs extend horizontally into the 3-tile space on each side of the center corridor
- Buildings on spurs are placed in the tile row above the spur road
- No buildings below spur roads

**Spur Start Behavior:**
- Spur starts in the building zone tile (column 0 or 3), not the road tiles
- This replaces any building that would be in that building zone tile
- The tile above the spur-start tile is still a building space for the main road (not for the spur)
- After the spur-start tile, the spur extends horizontally
- The first normal spur tile (1 tile beyond the building zone) has space above it for a building tile

### BuildingLayoutPalette Encoding

**Enum Structure (4 bits = 16 combinations):**

```csharp
public enum BuildingLayoutPalette
{
    // 0 spurs
    NoSpurs_SpreadEvenly = 0,
    NoSpurs_ClusterMiddle = 1,
    NoSpurs_FavorLeft = 2,
    NoSpurs_FavorRight = 3,
    
    // 1 spur (at middle row)
    OneSpurLeft_SpreadEvenly = 4,
    OneSpurLeft_ClusterMiddle = 5,
    OneSpurRight_SpreadEvenly = 6,
    OneSpurRight_ClusterMiddle = 7,
    
    // 2 spurs (at upper and lower middle rows)
    TwoSpursLeftRight_SpreadEvenly = 8,
    TwoSpursLeftRight_ClusterMiddle = 9,
    TwoSpursRightLeft_SpreadEvenly = 10,
    TwoSpursRightLeft_ClusterMiddle = 11,
    
    // Reserved for future expansion
    Reserved12 = 12,
    Reserved13 = 13,
    Reserved14 = 14,
    Reserved15 = 15
}
```

**Spur Row Positions:**
- 0 spurs: none
- 1 spur: Row 4 (middle of 10x10 grid)
- 2 spurs: Rows 3 and 6 (upper and lower middle)

**Placement Strategies:**
- Spread evenly: Distribute buildings across available road tiles using round-robin assignment (alternating left/right for major road, sequential for spurs)
- Cluster middle: Favor road tiles in the center (rows 3-6) with 70% probability, outer rows (1-2, 7-8) with 30% probability
- Favor left: Prefer tiles on the left side of the road with 70% probability, right side with 30% probability
- Favor right: Prefer tiles on the right side of the road with 70% probability, left side with 30% probability

**Placement Strategy Rationale:**
- Round-robin for "spread evenly" ensures balanced distribution without clustering
- Middle clustering mimics realistic town centers where buildings cluster around the main intersection
- Side bias mimics realistic town expansion where towns grow preferentially in one direction
- All strategies use seed-derived deterministic selection for consistency

### Prosperity-Based Density

**Density Mapping:**
- Boomtown: All available building zones filled (no empty spaces)
- Prosperous: 75% of building zones filled
- Poor: 50% of building zones filled
- Destitute: 25% of building zones filled

**Building Zone Count:**
- 0 spurs: 8 building zones (4 rows × 2 sides)
- 1 spur: 7 building zones (spur replaces 1 zone, adds 1 spur building = net same)
- 2 spurs: 6 building zones (spurs replace 2 zones, add 2 spur buildings = net same)

**Empty Space Handling:**
- For this slice: Leave building zones empty (no dummy buildings yet)
- Future work: Replace empty zones with dummy building tiles
- Trailhead always occupies row 0 and row 9 (not subject to density)

**Deterministic Assignment:**
- Use seed-derived ordering to assign specific buildings (Store, Sheriff, Saloon, Telegraph) to filled zones
- Telegraph only appears if town has that service
- Same seed + same prosperity = same building assignment

### Building View and Mirroring

**Building View Selection:**

Buildings on the major road (left or right of the road):
- 75% FrontOblique (front of building faces the road)
- 25% Profile (facing the road)
- View selection is deterministic based on seed at game setup time
- Same seed = same view assignment for the playthrough

Buildings on spur roads:
- Equal weight between Front, FrontOblique, and mirrored FrontOblique
- View selection is deterministic based on seed at game setup time
- Same seed = same view assignment for the playthrough

**Building Mirroring:**

- Assets should be canonically normalized to face one direction (e.g., all FrontOblique assets face right)
- Asset normalization is in scope for this work (existing assets, not new asset generation)
- Mirror selection is derived from which side of the road the building is on
- Buildings on left side of road: mirrored if needed to face the road
- Buildings on right side of road: not mirrored (canonical orientation)
- This makes mirror selection obvious and deterministic

**Determinism Constraints:**

- Each town resolved from the seed must be identical for one playthrough
- Angle variants can differ between plays of the same seed (setup-time selection)
- Within a single playthrough, towns must remain identical between visits
- No re-rolling of building views on town re-entry

**Town Positioning and Layout Determinism:**

- Town positioning on the world map: Set at game setup time, can differ between separate playthroughs of the same seed, but within a single playthrough towns must remain in the same position on re-visit (already handled by existing SeedWorld system)
- Town hub layout: Set at game setup time, can differ between separate playthroughs of the same seed, but within a single playthrough the town hub must look the same with the same layout and the same buildings in the same place each time the player visits that town (this work's concern)
- No re-rolling of town hub layout (building views, building placement, building positions) on town re-entry

**Implementation:**

- Use seed-derived deterministic selection for building views at game setup
- Store the selected view in the BuildingPlacement record
- Do not re-calculate views on each town visit
- View selection uses the same seed source as building placement for consistency
- Town hub layout is generated once at game setup and persisted

### Generator Logic

**TownLayoutGenerator Changes:**
1. Decode palette to get spur configuration (count, rows, direction) and placement strategy
2. Build tile grid with major road and spurs based on palette
3. Apply placement strategy to identify which tile positions are available for buildings
4. Apply prosperity-based density to determine how many positions to fill
5. Use seed-derived ordering to assign specific buildings to filled positions
6. Generate path segments from building tiles to nearest road tiles
7. Convert tile positions to logical coordinates (0-100) for BuildingPlacement

**Path Segment Generation:**
- Path from building tile center to nearest road tile edge
- Use deterministic jitter for visual variety
- Ensure path coordinates are within 0-100 range

### Extensibility and Future Seams

**Current Scope:**
- Spur width: 2 tiles (spur-start + 1 normal spur tile)
- Spur buildings: 1 per spur
- Building count: 4 (Store, Sheriff, Saloon, Telegraph)
- Prosperity tiers: Boomtown, Prosperous, Poor, Destitute

**Future Seams:**
- Spur width extension: Current 2-tile spur design allows extension to 3+ tiles by adding more normal spur tiles
- Spur count ↔ building count: Palette encoding has reserved values (12-15) for future expansion; can add palettes with 3+ spurs when building count grows
- Dummy buildings: Empty zones are left empty now; seam exists to add dummy building tiles later
- Empty lot tiles: Seam exists to add empty lot tiles for prosperity-based density
- Road tiles: Seam exists to add proper road tile graphics (currently using line segments)
- Poor image variants: Track as gap - need poor/prosperous/destitute variants for all building sprites

**Implementation Considerations:**
- Use tile coordinates rather than absolute positions for easier grid expansion
- Separate spur configuration from building placement logic
- Keep prosperity density as a separate concern from road topology
- Reserve palette values for future spur count/building count combinations

## Implementation Changes

### Approach to Existing Code

This is a fresh approach that replaces the previous 8 canonical layout patterns (HubAndSpoke, LinearChain, etc.). The old layout patterns were incorrect and should be removed entirely.

**Breaking Changes:**
- Remove old 8 layout patterns from BuildingLayoutCatalog
- Update BuildingLayoutPalette enum to new 12-palette encoding
- This is a breaking change to the seed codec layout (same 4 bits, different encoding)
- No backward compatibility needed - this is greenfield refactoring

**Test Updates:**
- Remove tests that assert old layout pattern behavior
- Add tests that assert the one correct tile-based approach
- All tests should validate the new tile-based system only
- No need to maintain tests for the old approach

### Backend Changes

**BuildingLayoutPalette.cs:**
- Replace 8 named layouts with 12 functional palettes (0-11) + 4 reserved (12-15)
- Update enum values to match new encoding scheme

**BuildingLayoutCatalog.cs:**
- Replace layout pattern specs with palette specs
- Each palette spec contains: spur count, spur rows, spur directions, placement strategy
- Remove hardcoded building positions

**TownLayoutGenerator.cs:**
- Rewrite to use tile-based grid system instead of absolute positions
- Add tile grid construction logic
- Add prosperity-based density logic
- Update path generation to work with tile positions
- Keep deterministic jitter for visual variety

**SeedWorldResolver.cs:**
- No changes needed (still 4 bits at positions 29-32)

**PathSegment.cs:**
- Keep as-is (already has validation)

### Frontend Changes

- No changes to frontend for this slice (still uses line segments for paths)
- Future work: Replace line segments with tile-based rendering

### Test Changes

**BuildingLayoutCatalogTests.cs:**
- Update to test palette specs instead of layout patterns
- Test that all palettes decode correctly
- Test spur configuration decoding

**TownLayoutGeneratorTests.cs:**
- Update to test tile-based layout generation
- Test prosperity-based density mapping
- Test deterministic building assignment
- Test path generation from tile positions

**New Tests:**
- Add test for tile grid construction
- Add test for spur start behavior
- Add integration test for full palette → prosperity → layout flow

### Tracking Gaps

Add Linear issues to track:
- Poor image variants for building sprites
- Road tile graphics
- Dummy building tiles
- Empty lot tiles

## Testing Strategy

**Unit Tests:**
- Palette decoding (spur count, rows, directions, placement strategy)
- Tile grid construction
- Prosperity density mapping
- Building assignment determinism
- Path segment generation

**Integration Tests:**
- Full flow: palette → tile grid → prosperity → building assignment → path generation
- Seed determinism: same seed + same prosperity = same layout
- Different palettes produce different road topologies

**Brute Force Tests:**
- Test all 12 palettes across all 4 prosperity tiers
- Verify determinism across seed/salt combinations
- Verify path segment validation (0-100 range)

## Definition of Done

- BuildingLayoutPalette enum updated with 12 functional palettes
- BuildingLayoutCatalog provides palette specs (spur config + placement strategy)
- TownLayoutGenerator uses tile-based grid system
- Prosperity controls building density
- All tests pass (unit, integration, brute force)
- Path segment generation works with tile positions
- Linear issues created for tracked gaps
- Design document committed
