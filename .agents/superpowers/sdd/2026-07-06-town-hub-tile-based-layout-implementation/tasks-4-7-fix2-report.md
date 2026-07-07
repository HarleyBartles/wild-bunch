# Tasks 4-7 Fix 2 Report

## Issue
The test `GenerateLayout_ProsperityAffectsZoneDensity` only verified that prosperity was stored in the layout, not that the density calculation worked. Since required buildings always override prosperity in this slice, the test should verify that `GetBuildingZoneCount` produces the correct zone counts for different prosperity levels.

## What I Fixed

### 1. Made GetBuildingZoneCount Internal
- **File**: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- **Change**: Changed `GetBuildingZoneCount` from `private static` to `internal static` (line 249)
- **Reason**: The method was already accessible to the test project via `InternalsVisibleTo` in `AssemblyInfo.cs`, making it internal allows direct testing without reflection

### 2. Updated Test to Verify Zone Count Calculation
- **File**: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`
- **Change**: Rewrote `GenerateLayout_ProsperityAffectsZoneDensity` test to directly call `GetBuildingZoneCount` and verify it produces correct zone counts
- **Test Coverage**: 
  - Boomtown: 8 zones (1.0 * 8 = 8)
  - Prosperous: 6 zones (0.75 * 8 = 6)
  - Poor: 4 zones (0.5 * 8 = 4)
  - Destitute: 2 zones (0.25 * 8 = 2)
- **Removed**: Unused variables (`townId`, `source`) that were only needed for the old test approach

## Files Changed
1. `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs` - Made GetBuildingZoneCount internal
2. `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs` - Updated test to verify zone count calculation

## Test Results
- **Target Test**: `GenerateLayout_ProsperityAffectsZoneDensity` - **PASSED**
- **Full Test Suite**: `TownLayoutGeneratorTests` - **11/11 PASSED**
- **Build**: Successful with only pre-existing warnings

## Verification
The test now directly verifies the density calculation logic rather than just checking that prosperity is stored in the layout. This provides meaningful coverage of the prosperity-to-zone-count mapping for all four prosperity levels.
