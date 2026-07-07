# Task 3 Fix Report

## What I Fixed

Updated BuildingLayoutCatalog.cs to align with the updated brief for Task 3. The brief was updated to reflect that TownLayoutGenerator.cs still uses GetLayout, so we cannot delete the supporting types yet.

### Changes Made

1. **Updated TODO references from Task 4 to Task 7**
   - GetLayout method TODO: Changed "will be removed in Task 4" to "will be removed in Task 7"
   - FallbackLayout field TODO: Changed "will be removed in Task 4" to "will be removed in Task 7"
   - BuildingLayoutPattern/BuildingPlacementSpec TODO: Changed "will be removed in Task 4" to "will be removed in Task 7"
   - GetLayout comment: Changed "until Task 4" to "until Task 7"

2. **Verified existing structure matches requirements**
   - No private layout pattern fields (HubAndSpokeLayout, LinearChainLayout, etc.) existed to delete - these were already removed in previous work
   - BuildingLayoutPattern record kept (needed by TownLayoutGenerator.cs which still uses GetLayout)
   - BuildingPlacementSpec record kept (needed by TownLayoutGenerator.cs)
   - SpurDirection enum kept (used by other code)
   - GetLayout method returns a stub layout with TODO for Task 7 (when TownLayoutGenerator is rewritten to use PaletteSpec)

3. **GetPaletteSpec method already implemented correctly**
   - The method was already implemented in the previous work and matches the brief requirements
   - Returns PaletteSpec for each BuildingLayoutPalette value
   - Handles all 12 palette configurations (0, 1, and 2 spurs with different placement strategies)

## Files Changed

- `src/WildBunch.GameContent/NewGame/BuildingLayoutCatalog.cs`
  - Updated 3 TODO comments to reference Task 7 instead of Task 4
  - No structural changes needed (already aligned with brief)

## Test Results

Ran: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "BuildingLayoutCatalogTests"`

Result: **PASS**
- Passed: 2 tests
- Failed: 0
- Skipped: 0
- Duration: 30 ms

Tests verified:
- `GetPaletteSpec_ReturnsCorrectConfiguration` - Verifies palette spec configuration for OneSpurLeft_SpreadEvenly
- `GetPaletteSpec_AllPalettesHaveValidConfiguration` - Verifies all palettes have valid spur counts and matching array lengths

## Status

The implementation now aligns with the updated brief. The TODO comments correctly reference Task 7 as the point when TownLayoutGenerator will be rewritten to use PaletteSpec instead of GetLayout.
