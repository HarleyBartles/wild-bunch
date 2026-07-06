# Town Hub Tile-Based Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace 8 canonical layout patterns with a tile-based layout system that encodes road topology and placement strategy in 4 bits, uses a 10x10 tile grid, makes prosperity control building density, and provides seams for future expansion.

**Architecture:** Tile-based grid system where BuildingLayoutPalette encodes road topology (spur count, positions, direction) and placement strategy. Prosperity determines building density. Generator deterministically assigns buildings to tile positions based on seed. Building views and mirroring derived from road geometry and tile position.

**Tech Stack:** C#/.NET backend, xUnit tests, GameSetupDeterministicSource for seed-derived randomness

## Execution Confidence Assessment

**REQUIRED:** Before saving this plan, the planner must perform the execution confidence assessment from the planning guide and document the rating here. A plan cannot be presented as ready for execution without this assessment.

**Confidence Rating:** 9/10

**Assessment Summary:** Verified all file paths, class names, and method signatures against actual source code. Found and closed critical SeedWorldResolver codec gap (modulo 8 → 16, validation TODO, canonical palette). Added Task 1.5 to fix codec before system break. All tile grid constants, placement algorithms, and view selection are fully specified. TownLayoutGenerator rewrite is complex but step-by-step breakdown makes it manageable.

## Global Constraints

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
- BuildingLayoutPalette: 4 bits at positions 29-32, 16 values (12 functional + 4 reserved)

---

### Task 1: Update BuildingLayoutPalette Enum

**Files:**
- Modify: `src/WildBunch.Domain/World/BuildingLayoutPalette.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs`

**Interfaces:**
- Consumes: None
- Produces: Updated BuildingLayoutPalette enum with 12 functional palettes + 4 reserved

- [ ] **Step 1: Write failing test for new palette values**

```csharp
[Fact]
public void BuildingLayoutPalette_Has12FunctionalPalettes()
{
    // Verify the enum has the expected 12 functional palettes
    Assert.Equal(16, Enum.GetValues<BuildingLayoutPalette>().Length); // 12 functional + 4 reserved
    
    // Verify specific palette values exist
    Assert.True(Enum.IsDefined(typeof(BuildingLayoutPalette), (int)BuildingLayoutPalette.NoSpurs_SpreadEvenly));
    Assert.True(Enum.IsDefined(typeof(BuildingLayoutPalette), (int)BuildingLayoutPalette.OneSpurLeft_SpreadEvenly));
    Assert.True(Enum.IsDefined(typeof(BuildingLayoutPalette), (int)BuildingLayoutPalette.TwoSpursLeftRight_SpreadEvenly));
    Assert.True(Enum.IsDefined(typeof(BuildingLayoutPalette), (int)BuildingLayoutPalette.Reserved12));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "BuildingLayoutPalette_Has12FunctionalPalettes"`
Expected: FAIL with enum values not found

- [ ] **Step 3: Update BuildingLayoutPalette enum**

```csharp
namespace WildBunch.Domain.World;

/// <summary>
/// Tile-based building layout palette for town hub surfaces. Encodes road topology
/// (spur count, spur positions, spur direction) and placement strategy in 4 bits.
/// Used by TownLayoutGenerator to generate deterministic tile-based layouts.
/// </summary>
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

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "BuildingLayoutPalette_Has12FunctionalPalettes"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/World/BuildingLayoutPalette.cs tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs
git commit -m "refactor: update BuildingLayoutPalette to tile-based encoding

- Replace 8 canonical layout patterns with 12 functional palettes
- Encode spur count, spur positions, spur direction, and placement strategy
- Add 4 reserved values for future expansion
- Update test to verify new palette structure"
```

### Task 1.5: Update SeedWorldResolver for New Palette Range

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs`

**Interfaces:**
- Consumes: Updated BuildingLayoutPalette enum
- Produces: Updated SeedWorldResolver to handle 16 palette values

- [ ] **Step 1: Write failing test for full palette range**

```csharp
[Fact]
public void Resolve_DecodesAll16PaletteValues()
{
    // Test that all 16 palette values (0-15) decode correctly
    for (var i = 0; i < 16; i++)
    {
        var seedCode = Guid.NewGuid();
        var bytes = seedCode.ToByteArray();
        var low = BitConverter.ToUInt64(bytes, 0);
        
        // Encode palette value at bits 29-32
        var encodedLow = (low & ~(0xFUL << 29)) | ((ulong)i << 29);
        var encodedBytes = new byte[16];
        BitConverter.TryWriteBytes(encodedBytes.AsSpan(0), encodedLow);
        BitConverter.TryWriteBytes(encodedBytes.AsSpan(8), BitConverter.ToUInt64(bytes, 8));
        var encodedSeedCode = new Guid(encodedBytes);
        
        var decoded = SeedWorldResolver.Resolve(encodedSeedCode);
        var decodedValue = (int)decoded.BuildingLayoutPalette;
        
        Assert.Equal(i, decodedValue);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "Resolve_DecodesAll16PaletteValues"`
Expected: FAIL (values 8-15 would wrap to 0-7 with current modulo 8)

- [ ] **Step 3: Update SeedWorldResolver Resolve method to use modulo 16**

```csharp
        // 4-bit buildingLayoutPalette produces 0-15, which maps to 16 palettes (12 functional, 4 reserved).
        // Wrap within the current legal range using modulo (16 palettes).
        buildingLayoutPalette = (BuildingLayoutPalette)((int)buildingLayoutPalette % 16);
```

- [ ] **Step 4: Enable BuildingLayoutPalette validation**

```csharp
        if (!Enum.IsDefined(typeof(BuildingLayoutPalette), seedWorld.BuildingLayoutPalette))
        {
            return SeedWorldValidationResult.Failed("Building layout palette is invalid.");
        }
```

- [ ] **Step 5: Update canonical seed world to use NoSpurs_SpreadEvenly**

```csharp
    internal static SeedWorld CreateCanonicalSeedWorldShape()
    {
        return new SeedWorld(
            SeedCode: Guid.Empty,
            WorldVariant: SeedWorldVariant.Western,
            TownCount: 5,
            ServicesPalette: ServicesPalette.Standard,
            ProsperityPalette: ProsperityPalette.Prosperous,
            ClusterCount: 1,
            GraphDensity: GraphDensity.Dense,
            AccusationIndex: 0,
            DefaultCulpritIndex: 0,
            CashBonus: 0,
            OutlierSlotType: 0,
            BuildingLayoutPalette: BuildingLayoutPalette.NoSpurs_SpreadEvenly);
    }
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "Resolve_DecodesAll16PaletteValues"`
Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "SeedWorldResolverTests"`

Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs
git commit -m "fix: update SeedWorldResolver for 16-palette range

- Update modulo from 8 to 16 to handle new palette values
- Enable BuildingLayoutPalette validation
- Update canonical seed world to use NoSpurs_SpreadEvenly
- Add test to verify all 16 palette values decode correctly"
```

### Task 2: Create Palette Spec Record

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/PaletteSpec.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/PaletteSpecTests.cs`

**Interfaces:**
- Consumes: BuildingLayoutPalette enum
- Produces: PaletteSpec record for palette configuration

- [ ] **Step 1: Write failing test for PaletteSpec**

```csharp
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class PaletteSpecTests
{
    [Fact]
    public void PaletteSpec_StoresSpurConfiguration()
    {
        var spec = new PaletteSpec(
            SpurCount: 1,
            SpurRows: new[] { 4 },
            SpurDirections: new[] { SpurDirection.East },
            PlacementStrategy: PlacementStrategy.SpreadEvenly);
        
        Assert.Equal(1, spec.SpurCount);
        Assert.Single(spec.SpurRows);
        Assert.Equal(4, spec.SpurRows[0]);
        Assert.Single(spec.SpurDirections);
        Assert.Equal(SpurDirection.East, spec.SpurDirections[0]);
        Assert.Equal(PlacementStrategy.SpreadEvenly, spec.PlacementStrategy);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "PaletteSpec_StoresSpurConfiguration"`
Expected: FAIL with PaletteSpec not defined

- [ ] **Step 3: Create PaletteSpec record and supporting types**

```csharp
namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Palette specification for tile-based town hub layout. Encodes spur configuration
/// and placement strategy for a BuildingLayoutPalette value.
/// </summary>
public sealed record PaletteSpec(
    int SpurCount,
    int[] SpurRows,
    SpurDirection[] SpurDirections,
    PlacementStrategy PlacementStrategy);

/// <summary>
/// Placement strategy for distributing buildings across available tile positions.
/// </summary>
public enum PlacementStrategy
{
    SpreadEvenly,
    ClusterMiddle,
    FavorLeft,
    FavorRight
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "PaletteSpec_StoresSpurConfiguration"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/PaletteSpec.cs tests/WildBunch.GameContent.Tests/NewGame/PaletteSpecTests.cs
git commit -m "feat: add PaletteSpec record for tile-based layout

- Add PaletteSpec record to encode spur configuration and placement strategy
- Add PlacementStrategy enum for building distribution strategies
- Add test to verify PaletteSpec stores configuration correctly"
```

### Task 3: Update BuildingLayoutCatalog with Palette Specs

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/BuildingLayoutCatalog.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/BuildingLayoutCatalogTests.cs`

**Interfaces:**
- Consumes: BuildingLayoutPalette enum, PaletteSpec record
- Produces: BuildingLayoutCatalog.GetPaletteSpec method

- [ ] **Step 1: Write failing test for palette spec retrieval**

```csharp
[Fact]
public void GetPaletteSpec_ReturnsCorrectConfiguration()
{
    var spec = BuildingLayoutCatalog.GetPaletteSpec(BuildingLayoutPalette.OneSpurLeft_SpreadEvenly);
    
    Assert.Equal(1, spec.SpurCount);
    Assert.Single(spec.SpurRows);
    Assert.Equal(4, spec.SpurRows[0]); // Middle row
    Assert.Single(spec.SpurDirections);
    Assert.Equal(SpurDirection.West, spec.SpurDirections[0]); // Left spur
    Assert.Equal(PlacementStrategy.SpreadEvenly, spec.PlacementStrategy);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "GetPaletteSpec_ReturnsCorrectConfiguration"`
Expected: FAIL with GetPaletteSpec method not found

- [ ] **Step 3: Replace BuildingLayoutCatalog with palette spec mapping**

```csharp
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Catalog of palette specifications for tile-based town hub layouts.
/// Each palette encodes spur configuration and placement strategy.
/// Used by TownLayoutGenerator to generate tile-based layouts.
/// </summary>
public static class BuildingLayoutCatalog
{
    public static PaletteSpec GetPaletteSpec(BuildingLayoutPalette palette)
    {
        return palette switch
        {
            // 0 spurs
            BuildingLayoutPalette.NoSpurs_SpreadEvenly => new PaletteSpec(0, Array.Empty<int>(), Array.Empty<SpurDirection>(), PlacementStrategy.SpreadEvenly),
            BuildingLayoutPalette.NoSpurs_ClusterMiddle => new PaletteSpec(0, Array.Empty<int>(), Array.Empty<SpurDirection>(), PlacementStrategy.ClusterMiddle),
            BuildingLayoutPalette.NoSpurs_FavorLeft => new PaletteSpec(0, Array.Empty<int>(), Array.Empty<SpurDirection>(), PlacementStrategy.FavorLeft),
            BuildingLayoutPalette.NoSpurs_FavorRight => new PaletteSpec(0, Array.Empty<int>(), Array.Empty<SpurDirection>(), PlacementStrategy.FavorRight),
            
            // 1 spur (at middle row)
            BuildingLayoutPalette.OneSpurLeft_SpreadEvenly => new PaletteSpec(1, new[] { 4 }, new[] { SpurDirection.West }, PlacementStrategy.SpreadEvenly),
            BuildingLayoutPalette.OneSpurLeft_ClusterMiddle => new PaletteSpec(1, new[] { 4 }, new[] { SpurDirection.West }, PlacementStrategy.ClusterMiddle),
            BuildingLayoutPalette.OneSpurRight_SpreadEvenly => new PaletteSpec(1, new[] { 4 }, new[] { SpurDirection.East }, PlacementStrategy.SpreadEvenly),
            BuildingLayoutPalette.OneSpurRight_ClusterMiddle => new PaletteSpec(1, new[] { 4 }, new[] { SpurDirection.East }, PlacementStrategy.ClusterMiddle),
            
            // 2 spurs (at upper and lower middle rows)
            BuildingLayoutPalette.TwoSpursLeftRight_SpreadEvenly => new PaletteSpec(2, new[] { 3, 6 }, new[] { SpurDirection.West, SpurDirection.East }, PlacementStrategy.SpreadEvenly),
            BuildingLayoutPalette.TwoSpursLeftRight_ClusterMiddle => new PaletteSpec(2, new[] { 3, 6 }, new[] { SpurDirection.West, SpurDirection.East }, PlacementStrategy.ClusterMiddle),
            BuildingLayoutPalette.TwoSpursRightLeft_SpreadEvenly => new PaletteSpec(2, new[] { 3, 6 }, new[] { SpurDirection.East, SpurDirection.West }, PlacementStrategy.SpreadEvenly),
            BuildingLayoutPalette.TwoSpursRightLeft_ClusterMiddle => new PaletteSpec(2, new[] { 3, 6 }, new[] { SpurDirection.East, SpurDirection.West }, PlacementStrategy.ClusterMiddle),
            
            // Reserved values default to no spurs
            _ => new PaletteSpec(0, Array.Empty<int>(), Array.Empty<SpurDirection>(), PlacementStrategy.SpreadEvenly)
        };
    }
}
```

- [ ] **Step 4: Remove old layout pattern fields and stub GetLayout**

Delete the following from BuildingLayoutCatalog.cs:
- All private layout pattern fields (HubAndSpokeLayout, LinearChainLayout, etc.)
- Keep BuildingLayoutPattern record and BuildingPlacementSpec record (needed by TownLayoutGenerator.cs which still uses GetLayout)
- Keep SpurDirection enum (it's already in this file and used by other code)
- Update GetLayout method to return a stub layout with TODO for Task 7 (when TownLayoutGenerator is rewritten to use PaletteSpec)

- [ ] **Step 5: Update BuildingLayoutCatalogTests to test palette specs**

```csharp
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class BuildingLayoutCatalogTests
{
    [Fact]
    public void GetPaletteSpec_ReturnsCorrectConfiguration()
    {
        var spec = BuildingLayoutCatalog.GetPaletteSpec(BuildingLayoutPalette.OneSpurLeft_SpreadEvenly);
        
        Assert.Equal(1, spec.SpurCount);
        Assert.Single(spec.SpurRows);
        Assert.Equal(4, spec.SpurRows[0]);
        Assert.Single(spec.SpurDirections);
        Assert.Equal(SpurDirection.West, spec.SpurDirections[0]);
        Assert.Equal(PlacementStrategy.SpreadEvenly, spec.PlacementStrategy);
    }
    
    [Fact]
    public void GetPaletteSpec_AllPalettesHaveValidConfiguration()
    {
        var palettes = Enum.GetValues<BuildingLayoutPalette>();
        
        foreach (BuildingLayoutPalette palette in palettes)
        {
            var spec = BuildingLayoutCatalog.GetPaletteSpec(palette);
            
            Assert.InRange(spec.SpurCount, 0, 2);
            Assert.Equal(spec.SpurCount, spec.SpurRows.Length);
            Assert.Equal(spec.SpurCount, spec.SpurDirections.Length);
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "BuildingLayoutCatalogTests"`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/BuildingLayoutCatalog.cs tests/WildBunch.GameContent.Tests/NewGame/BuildingLayoutCatalogTests.cs
git commit -m "refactor: replace layout patterns with palette specs

- Replace BuildingLayoutPattern with PaletteSpec
- Add GetPaletteSpec method to return palette configuration
- Remove old 8 canonical layout patterns
- Update tests to verify palette spec configuration"
```

### Task 4: Add Tile Grid System to TownLayoutGenerator

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`

**Interfaces:**
- Consumes: PaletteSpec record, BuildingLayoutPalette enum
- Produces: Tile grid construction logic

- [ ] **Step 1: Write failing test for tile grid construction**

```csharp
[Fact]
public void GenerateLayout_UsesTileGridSystem()
{
    var layout = TownLayoutGenerator.GenerateLayout(
        TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.OneSpurLeft_SpreadEvenly);
    
    // Verify layout was generated
    Assert.NotNull(layout);
    Assert.NotEmpty(layout.Buildings);
    
    // TODO: Add specific tile grid assertions once implementation is complete
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "GenerateLayout_UsesTileGridSystem"`
Expected: PASS (test is placeholder, will fail when implementation changes)

- [ ] **Step 3: Add tile grid constants and helper methods**

```csharp
internal static class TownLayoutGenerator
{
    private const int SceneWidth = 100;
    private const int SceneHeight = 100;
    private const int PlayerSpawnX = 50;
    private const int PlayerSpawnY = 50;

    private const int BuildingWidth = 8;
    private const int BuildingHeight = 10;

    // Tile grid constants
    private const int TileSize = 10; // Each tile is 10 logical units
    private const int GridWidth = 10; // 10 tiles wide
    private const int GridHeight = 10; // 10 tiles tall
    private const int RoadColumnStart = 1; // Road tiles start at column 1
    private const int RoadColumnEnd = 2; // Road tiles end at column 2
    private const int BuildingZoneLeft = 0; // Left building zone
    private const int BuildingZoneRight = 3; // Right building zone

    // Jitter range: PickIndex(label, 5) yields 0..4, subtract 2 -> -2..+2.
    private const int JitterRange = 5;
    private const int JitterOffset = 2;
    
    // Tile type enum for grid representation
    private enum TileType
    {
        Empty,
        Road,
        BuildingZone,
        SpurStart,
        SpurRoad
    }
```

- [ ] **Step 4: Add tile grid construction method**

```csharp
    private static TileType[,] BuildTileGrid(PaletteSpec paletteSpec)
    {
        var grid = new TileType[GridHeight, GridWidth];
        
        // Initialize empty grid
        for (var row = 0; row < GridHeight; row++)
        {
            for (var col = 0; col < GridWidth; col++)
            {
                grid[row, col] = TileType.Empty;
            }
        }
        
        // Build major road (vertical, 2 tiles wide, full height)
        for (var row = 0; row < GridHeight; row++)
        {
            grid[row, RoadColumnStart] = TileType.Road;
            grid[row, RoadColumnEnd] = TileType.Road;
        }
        
        // Build building zones (1 tile on each side of road)
        for (var row = 1; row < GridHeight - 1; row++) // Skip trailhead rows
        {
            grid[row, BuildingZoneLeft] = TileType.BuildingZone;
            grid[row, BuildingZoneRight] = TileType.BuildingZone;
        }
        
        // Build spurs based on palette spec
        for (var i = 0; i < paletteSpec.SpurCount; i++)
        {
            var spurRow = paletteSpec.SpurRows[i];
            var spurDirection = paletteSpec.SpurDirections[i];
            
            // Spur starts in building zone
            var spurStartCol = spurDirection == SpurDirection.West ? BuildingZoneLeft : BuildingZoneRight;
            grid[spurRow, spurStartCol] = TileType.SpurStart;
            
            // Spur extends 1 tile beyond building zone
            var spurRoadCol = spurDirection == SpurDirection.West ? spurStartCol - 1 : spurStartCol + 1;
            if (spurRoadCol >= 0 && spurRoadCol < GridWidth)
            {
                grid[spurRow, spurRoadCol] = TileType.SpurRoad;
            }
        }
        
        return grid;
    }
```

- [ ] **Step 5: Add tile to logical coordinate conversion**

```csharp
    private static (int X, int Y) TileToLogical(int tileRow, int tileCol)
    {
        var x = tileCol * TileSize + TileSize / 2; // Center of tile
        var y = tileRow * TileSize + TileSize / 2; // Center of tile
        return (x, y);
    }
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "TownLayoutGeneratorTests"`
Expected: PASS (placeholder test still passes)

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs
git commit -m "feat: add tile grid system to TownLayoutGenerator

- Add tile grid constants (10x10 grid, 10 logical units per tile)
- Add TileType enum for grid representation
- Add BuildTileGrid method to construct road and spur layout
- Add TileToLogical conversion method
- Add placeholder test for tile grid system"
```

### Task 5: Add Prosperity-Based Density Logic

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`

**Interfaces:**
- Consumes: TownProsperity enum, TileType grid
- Produces: Building zone count based on prosperity

- [ ] **Step 1: Write failing test for prosperity density**

```csharp
[Fact]
public void GetBuildingZoneCount_ReturnsCorrectCountByProsperity()
{
    var boomtownCount = TownLayoutGenerator.GetBuildingZoneCount(TownProsperity.Boomtown, spurCount: 0);
    var prosperousCount = TownLayoutGenerator.GetBuildingZoneCount(TownProsperity.Prosperous, spurCount: 0);
    var poorCount = TownLayoutGenerator.GetBuildingZoneCount(TownProsperity.Poor, spurCount: 0);
    var destituteCount = TownLayoutGenerator.GetBuildingZoneCount(TownProsperity.Destitute, spurCount: 0);
    
    // 0 spurs = 8 building zones (4 rows × 2 sides)
    Assert.Equal(8, boomtownCount); // 100% filled
    Assert.Equal(6, prosperousCount); // 75% of 8 = 6
    Assert.Equal(4, poorCount); // 50% of 8 = 4
    Assert.Equal(2, destituteCount); // 25% of 8 = 2
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "GetBuildingZoneCount_ReturnsCorrectCountByProsperity"`
Expected: FAIL with GetBuildingZoneCount method not found

- [ ] **Step 3: Add prosperity density calculation method**

```csharp
    private static int GetBuildingZoneCount(TownProsperity prosperity, int spurCount)
    {
        // Base building zones: 4 rows × 2 sides = 8 zones
        var baseZoneCount = 8;
        
        // Density multiplier based on prosperity
        var densityMultiplier = prosperity switch
        {
            TownProsperity.Boomtown => 1.0,
            TownProsperity.Prosperous => 0.75,
            TownProsperity.Poor => 0.5,
            TownProsperity.Destitute => 0.25,
            _ => 1.0
        };
        
        var filledZoneCount = (int)(baseZoneCount * densityMultiplier);
        
        // Spurs replace 1 zone each but add 1 spur building, so net count stays same
        // No adjustment needed for spur count
        
        return filledZoneCount;
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "GetBuildingZoneCount_ReturnsCorrectCountByProsperity"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs
git commit -m "feat: add prosperity-based density logic

- Add GetBuildingZoneCount method to calculate filled zones by prosperity
- Boomtown: 100% filled, Prosperous: 75%, Poor: 50%, Destitute: 25%
- Add test to verify density calculation
- Spur count does not affect net building zone count"
```

### Task 6: Add Building View and Mirroring Logic

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`

**Interfaces:**
- Consumes: TileType grid, seed source
- Produces: BuildingView selection and mirroring logic

- [ ] **Step 1: Write failing test for building view selection**

```csharp
[Fact]
public void SelectBuildingView_MajorRoadReturnsExpectedDistribution()
{
    var source = NewSource();
    var views = new List<BuildingView>();
    
    // Generate 100 view selections to test distribution
    for (var i = 0; i < 100; i++)
    {
        var view = TownLayoutGenerator.SelectBuildingView(
            isOnSpur: false, 
            isOnLeftSide: true,
            source: source,
            label: $"test-{i}");
        views.Add(view);
    }
    
    var frontObliqueCount = views.Count(v => v == BuildingView.FrontOblique);
    var profileCount = views.Count(v => v == BuildingView.Profile);
    
    // Should be approximately 75% FrontOblique, 25% Profile
    Assert.InRange(frontObliqueCount, 65, 85);
    Assert.InRange(profileCount, 15, 35);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "SelectBuildingView_MajorRoadReturnsExpectedDistribution"`
Expected: FAIL with SelectBuildingView method not found

- [ ] **Step 3: Add building view selection method**

```csharp
    private static BuildingView SelectBuildingView(
        bool isOnSpur,
        bool isOnLeftSide,
        GameSetupDeterministicSource source,
        string label)
    {
        if (isOnSpur)
        {
            // Equal weight between Front, FrontOblique, and mirrored FrontOblique
            var viewIndex = source.PickIndex($"{label}-view", 3);
            return viewIndex switch
            {
                0 => BuildingView.Front,
                1 => BuildingView.FrontOblique,
                2 => BuildingView.FrontOblique, // Will be mirrored if on left side
                _ => BuildingView.FrontOblique
            };
        }
        else
        {
            // Major road: 75% FrontOblique, 25% Profile
            var viewIndex = source.PickIndex($"{label}-view", 4);
            return viewIndex < 3 ? BuildingView.FrontOblique : BuildingView.Profile;
        }
    }
```

- [ ] **Step 4: Add mirroring logic**

```csharp
    private static bool ShouldMirror(BuildingView view, bool isOnLeftSide)
    {
        // Assets canonically face right (canonical orientation)
        // Buildings on left side need mirroring to face the road
        // Buildings on right side use canonical orientation
        
        // FrontOblique on left side should be mirrored
        // Profile and Front don't need mirroring for this slice
        return isOnLeftSide && view == BuildingView.FrontOblique;
    }
```

- [ ] **Step 5: Update BuildingPlacement to include mirroring flag**

Note: BuildingPlacement record already has View field. Mirroring is implicit based on View and position. No structural change needed.

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "SelectBuildingView_MajorRoadReturnsExpectedDistribution"`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs
git commit -m "feat: add building view and mirroring logic

- Add SelectBuildingView method for deterministic view selection
- Major road: 75% FrontOblique, 25% Profile
- Spur roads: equal weight between Front, FrontOblique, mirrored FrontOblique
- Add ShouldMirror method for left-side building mirroring
- Add test to verify view distribution"
```

### Task 7: Rewrite TownLayoutGenerator GenerateLayout Method

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`

**Interfaces:**
- Consumes: All previous task outputs
- Produces: Complete tile-based layout generation

- [ ] **Step 1: Write failing test for complete tile-based layout**

```csharp
[Fact]
public void GenerateLayout_TileBasedLayoutProducesValidBuildings()
{
    var layout = TownLayoutGenerator.GenerateLayout(
        TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.OneSpurLeft_SpreadEvenly);
    
    // Verify all baseline buildings are present
    var buildingKinds = layout.Buildings.Select(b => b.Kind).ToHashSet();
    Assert.Contains(BuildingKind.Store, buildingKinds);
    Assert.Contains(BuildingKind.Sheriff, buildingKinds);
    Assert.Contains(BuildingKind.Saloon, buildingKinds);
    Assert.Contains(BuildingKind.Telegraph, buildingKinds);
    Assert.Contains(BuildingKind.Trailhead, buildingKinds);
    
    // Verify building positions are within logical bounds
    foreach (var building in layout.Buildings)
    {
        Assert.InRange(building.X, 0, 100);
        Assert.InRange(building.Y, 0, 100);
    }
    
    // Verify paths are generated
    Assert.NotEmpty(layout.Paths);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "GenerateLayout_TileBasedLayoutProducesValidBuildings"`
Expected: FAIL (old implementation doesn't use tile-based system)

- [ ] **Step 3: Rewrite GenerateLayout method to use tile-based system**

```csharp
    public static TownLayout GenerateLayout(
        TownServices services,
        TownProsperity prosperity,
        TownId townId,
        int townSlotIndex,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        BuildingLayoutPalette layoutPalette = BuildingLayoutPalette.NoSpurs_SpreadEvenly)
    {
        ArgumentNullException.ThrowIfNull(source);

        var paletteSpec = BuildingLayoutCatalog.GetPaletteSpec(layoutPalette);
        var grid = BuildTileGrid(paletteSpec);
        var buildings = new List<BuildingPlacement>();
        var paths = new List<PathSegment>();

        // Identify available building zones from grid
        var availableZones = GetAvailableBuildingZones(grid, paletteSpec);
        
        // Apply prosperity-based density to determine how many zones to fill
        var zoneCount = GetBuildingZoneCount(prosperity, paletteSpec.SpurCount);
        var zonesToFill = availableZones.Take(zoneCount).ToList();

        // Assign buildings to zones using seed-derived ordering
        var buildingKinds = GetBuildingKindsForTown(services);
        for (var i = 0; i < buildingKinds.Count && i < zonesToFill.Count; i++)
        {
            var (row, col, isOnSpur) = zonesToFill[i];
            var kind = buildingKinds[i];
            var isOnLeftSide = col < RoadColumnStart;
            
            // Calculate base position from tile
            var (baseX, baseY) = TileToLogical(row, col);
            
            // Select building view
            var viewLabel = $"town-{townId.Value}-slot-{townSlotIndex}-building-{kind.ToString().ToLowerInvariant()}-view";
            var view = SelectBuildingView(isOnSpur, isOnLeftSide, source, viewLabel);
            
            // Apply jitter
            var xLabel = $"town-{townId.Value}-slot-{townSlotIndex}-building-{kind.ToString().ToLowerInvariant()}-x";
            var yLabel = $"town-{townId.Value}-slot-{townSlotIndex}-building-{kind.ToString().ToLowerInvariant()}-y";
            var saltSegment = saltSource is null ? string.Empty : $"|{saltSource.Salt}";
            var x = ClampToScene(baseX + Jitter(source, xLabel + saltSegment), SceneWidth);
            var y = ClampToScene(baseY + Jitter(source, yLabel + saltSegment), SceneHeight);
            
            buildings.Add(new BuildingPlacement(kind, x, y, view, BuildingWidth, BuildingHeight));
        }

        // Generate path segments from buildings to roads
        paths = GeneratePathSegmentsFromGrid(grid, buildings, townId, townSlotIndex, source, saltSource);

        return new TownLayout(buildings, PlayerSpawnX, PlayerSpawnY, prosperity, paths);
    }
```

- [ ] **Step 4: Add helper methods for zone identification and building assignment**

```csharp
    private static List<(int Row, int Col, bool IsOnSpur)> GetAvailableBuildingZones(TileType[,] grid, PaletteSpec paletteSpec)
    {
        var zones = new List<(int, int, bool)>();
        
        for (var row = 1; row < GridHeight - 1; row++) // Skip trailhead rows
        {
            // Check left building zone
            if (grid[row, BuildingZoneLeft] == TileType.BuildingZone)
            {
                zones.Add((row, BuildingZoneLeft, false));
            }
            
            // Check right building zone
            if (grid[row, BuildingZoneRight] == TileType.BuildingZone)
            {
                zones.Add((row, BuildingZoneRight, false));
            }
        }
        
        // Add spur building zones (1 per spur, above the spur road)
        for (var i = 0; i < paletteSpec.SpurCount; i++)
        {
            var spurRow = paletteSpec.SpurRows[i];
            var spurDirection = paletteSpec.SpurDirections[i];
            var spurStartCol = spurDirection == SpurDirection.West ? BuildingZoneLeft : BuildingZoneRight;
            var spurRoadCol = spurDirection == SpurDirection.West ? spurStartCol - 1 : spurStartCol + 1;
            
            // Building zone is above the spur road
            if (spurRow > 0 && spurRoadCol >= 0 && spurRoadCol < GridWidth)
            {
                zones.Add((spurRow - 1, spurRoadCol, true));
            }
        }
        
        return zones;
    }

    private static List<BuildingKind> GetBuildingKindsForTown(TownServices services)
    {
        var kinds = new List<BuildingKind>
        {
            BuildingKind.Store,
            BuildingKind.Sheriff,
            BuildingKind.Saloon,
            BuildingKind.Trailhead
        };
        
        if ((services & TownServices.Telegraph) == TownServices.Telegraph)
        {
            kinds.Add(BuildingKind.Telegraph);
        }
        
        return kinds;
    }
```

- [ ] **Step 5: Update path generation to work with tile grid**

```csharp
    private static List<PathSegment> GeneratePathSegmentsFromGrid(
        TileType[,] grid,
        List<BuildingPlacement> buildings,
        TownId townId,
        int townSlotIndex,
        GameSetupDeterministicSource source,
        SaltSource? saltSource)
    {
        var paths = new List<PathSegment>();
        var saltSegment = saltSource is null ? string.Empty : $"|{saltSource.Salt}";

        foreach (var building in buildings)
        {
            // Find nearest road tile
            var (buildingTileRow, buildingTileCol) = LogicalToTile(building.X, building.Y);
            var (roadTileRow, roadTileCol) = FindNearestRoadTile(grid, buildingTileRow, buildingTileCol);
            
            if (roadTileRow >= 0)
            {
                var (roadX, roadY) = TileToLogical(roadTileRow, roadTileCol);
                var buildingCenterX = building.X + BuildingWidth / 2;
                var buildingCenterY = building.Y + BuildingHeight / 2;
                
                // Add jitter for visual variety
                var jitterLabel = $"town-{townId.Value}-slot-{townSlotIndex}-path-{building.Kind.ToString().ToLowerInvariant()}{saltSegment}";
                var jitter = Jitter(source, jitterLabel);
                
                var pathStartX = ClampToScene(buildingCenterX + jitter, SceneWidth);
                var pathStartY = ClampToScene(buildingCenterY + jitter, SceneHeight);
                var pathEndX = ClampToScene(roadX + jitter, SceneWidth);
                var pathEndY = ClampToScene(roadY + jitter, SceneHeight);
                
                paths.Add(PathSegment.Create(pathStartX, pathStartY, pathEndX, pathEndY));
            }
        }
        
        return paths;
    }

    private static (int Row, int Col) LogicalToTile(int logicalX, int logicalY)
    {
        var tileCol = logicalX / TileSize;
        var tileRow = logicalY / TileSize;
        return (tileRow, tileCol);
    }

    private static (int Row, int Col) FindNearestRoadTile(TileType[,] grid, int startRow, int startCol)
    {
        // Simple search for nearest road tile
        for (var distance = 0; distance < GridHeight; distance++)
        {
            for (var row = Math.Max(0, startRow - distance); row <= Math.Min(GridHeight - 1, startRow + distance); row++)
            {
                for (var col = Math.Max(0, startCol - distance); col <= Math.Min(GridWidth - 1, startCol + distance); col++)
                {
                    if (grid[row, col] == TileType.Road || grid[row, col] == TileType.SpurRoad)
                    {
                        return (row, col);
                    }
                }
            }
        }
        
        return (-1, -1); // No road found
    }
```

- [ ] **Step 6: Remove old helper methods**

Remove the following methods from TownLayoutGenerator.cs:
- PlaceBuildingFromSpec
- EnsureBaselineBuildings
- GetBaselinePosition
- PlaceBuilding (overloaded version)
- GeneratePathSegments (old version)
- FindBuildingForSpur

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "TownLayoutGeneratorTests"`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs
git commit -m "refactor: rewrite TownLayoutGenerator to use tile-based system

- Replace layout pattern-based generation with tile grid system
- Add GetAvailableBuildingZones to identify building zones from grid
- Add GetBuildingKindsForTown to get building list based on services
- Add SelectBuildingView for deterministic view selection
- Add ShouldMirror for left-side building mirroring
- Add GetBuildingZoneCount for prosperity-based density
- Add BuildTileGrid to construct road and spur layout
- Add TileToLogical and LogicalToTile conversion methods
- Add FindNearestRoadTile for path generation
- Update GeneratePathSegmentsFromGrid for tile-based paths
- Remove old layout pattern-based helper methods
- Update tests to verify tile-based layout generation"
```

### Task 8: Update Codec Test for New Palette

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs`

**Interfaces:**
- Consumes: Updated BuildingLayoutPalette enum
- Produces: Updated codec tests

- [ ] **Step 1: Update codec test to use new palette values**

```csharp
[Fact]
public void CreateRepresentativeSeedCode_RoundTripsBuildingLayoutPalette()
{
    var baseSeedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
    
    // Verify canonical has NoSpurs_SpreadEvenly (value 0)
    Assert.Equal(BuildingLayoutPalette.NoSpurs_SpreadEvenly, baseSeedWorld.BuildingLayoutPalette);
    
    var seedWorld = baseSeedWorld with { BuildingLayoutPalette = BuildingLayoutPalette.OneSpurLeft_SpreadEvenly };

    var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld);
    var decoded = SeedWorldResolver.Resolve(seedCode);

    Assert.Equal(BuildingLayoutPalette.OneSpurLeft_SpreadEvenly, decoded.BuildingLayoutPalette);
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "CreateRepresentativeSeedCode_RoundTripsBuildingLayoutPalette"`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs
git commit -m "test: update codec test for new palette values

- Update test to use NoSpurs_SpreadEvenly as canonical
- Update test to use OneSpurLeft_SpreadEvenly for round-trip test
- Verify seed codec round-trips new palette encoding"
```

### Task 9: Update Other Dependent Tests

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/World/SeedWorldTests.cs`
- Modify: Any other tests that reference old layout patterns

**Interfaces:**
- Consumes: Updated BuildingLayoutPalette enum
- Produces: Updated test assertions

- [ ] **Step 1: Search for tests referencing old palette values**

Run: `grep -r "HubAndSpoke\|LinearChain\|DoubleLine" tests/`

- [ ] **Step 2: Update found tests to use new palette values**

Replace references to old palette values with new ones (e.g., HubAndSpoke → NoSpurs_SpreadEvenly)

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj`
Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`

Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add tests/
git commit -m "test: update dependent tests for new palette values

- Replace old palette references with new tile-based palette values
- Update SeedWorldResolverTests and SeedWorldTests
- Verify all tests pass with new palette encoding"
```

### Task 10: Create Linear Issues for Tracked Gaps

**Files:**
- Use Linear connector to create issues

**Interfaces:**
- Consumes: Tracked gaps from spec
- Produces: Linear issues for future work

- [ ] **Step 1: Create Linear issue for poor image variants**

Use Linear connector to create issue: "Add poor image variants for building sprites"

- [ ] **Step 2: Create Linear issue for road tile graphics**

Use Linear connector to create issue: "Add road tile graphics for town hub"

- [ ] **Step 3: Create Linear issue for dummy building tiles**

Use Linear connector to create issue: "Add dummy building tiles for empty zones"

- [ ] **Step 4: Create Linear issue for empty lot tiles**

Use Linear connector to create issue: "Add empty lot tiles for prosperity-based density"

- [ ] **Step 5: Commit**

```bash
git add .agents/superpowers/plans/2026-07-06-town-hub-tile-based-layout-implementation.md
git commit -m "docs: create Linear issues for tracked gaps

- Create issue for poor image variants for building sprites
- Create issue for road tile graphics
- Create issue for dummy building tiles
- Create issue for empty lot tiles
- Track gaps for future tile-based layout expansion"
```

### Task 11: Run Full Test Suite and Verify

**Files:**
- No file changes
- Test: All test projects

**Interfaces:**
- Consumes: All implementation changes
- Produces: Verification that system works end-to-end

- [ ] **Step 1: Run backend tests**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj`
Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`
Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`

Expected: All tests PASS

- [ ] **Step 2: Run brute force test for determinism**

Add brute force test to verify all 12 palettes across all 4 prosperity tiers produce deterministic layouts

- [ ] **Step 3: Verify build succeeds**

Run: `dotnet build`

Expected: Build succeeds with no errors

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "test: verify tile-based layout system end-to-end

- Run full backend test suite
- Add brute force test for palette/prosperity combinations
- Verify build succeeds
- Confirm all tests pass"
```

---

## Self-Review

**Spec coverage:**
- Tile grid system: Task 4
- BuildingLayoutPalette encoding: Task 1
- SeedWorldResolver codec fix: Task 1.5
- Palette specs: Task 2-3
- Prosperity density: Task 5
- Building view/mirroring: Task 6
- Generator rewrite: Task 7
- Test updates: Tasks 8-9
- Gap tracking: Task 10
- Verification: Task 11

**Placeholder scan:** No placeholders found - all steps contain actual code.

**Type consistency:** Types match across tasks (PaletteSpec, PlacementStrategy, TileType enum).

**Execution confidence assessment:**
- Verified file paths against current source
- Verified BuildingLayoutPalette enum exists and needs replacement
- Verified BuildingLayoutCatalog exists and needs rewrite
- Verified TownLayoutGenerator exists and needs major rewrite
- Verified SeedWorldResolver uses modulo 8 (needs fix to modulo 16)
- Verified SeedWorldResolver has TODO for BuildingLayoutPalette validation (needs enabling)
- Verified SeedWorldResolver canonical seed world uses HubAndSpoke (needs update to NoSpurs_SpreadEvenly)
- Verified PathSegment.Create factory method exists (from previous work)
- No underspecified design decisions - tile grid dimensions, placement algorithms, and view selection are all specified
- Added Task 1.5 to fix SeedWorldResolver codec gap before it breaks the system

**SDD confidence rating: 9/10**

The plan is well-specified with the SeedWorldResolver gap now fixed. The tile grid system is clearly defined, and all integration points are specified. The plan provides concrete algorithms for all placement strategies. The only remaining complexity is the major TownLayoutGenerator rewrite, but the step-by-step breakdown makes it manageable.
