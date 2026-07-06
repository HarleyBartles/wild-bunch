# Tasks 4-7 Combined: Tile-Based TownLayoutGenerator Rewrite

## Task Description

Combine Tasks 4-7 to rewrite TownLayoutGenerator with the tile-based system in one coherent change. This adds tile grid constants, prosperity density logic, building view/mirroring logic, and rewrites the GenerateLayout method to use the new PaletteSpec-based tile grid system.

## Files
- Modify: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`

## Interfaces
- Consumes: PaletteSpec record (from Task 2), BuildingLayoutPalette enum (from Task 1)
- Produces: Complete tile-based layout generation

## Combined Steps

### Step 1: Add tile grid constants and TileType enum

Add to TownLayoutGenerator.cs:

```csharp
// Tile grid constants
private const int TileSize = 10; // Each tile is 10 logical units
private const int GridWidth = 10; // 10 tiles wide
private const int GridHeight = 10; // 10 tiles tall
private const int RoadColumnStart = 1; // Road tiles start at column 1
private const int RoadColumnEnd = 2; // Road tiles end at column 2
private const int BuildingZoneLeft = 0; // Left building zone
private const int BuildingZoneRight = 3; // Right building zone

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

### Step 2: Add BuildTileGrid method

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

### Step 3: Add TileToLogical conversion method

```csharp
private static (int X, int Y) TileToLogical(int tileRow, int tileCol)
{
    var x = tileCol * TileSize + TileSize / 2; // Center of tile
    var y = tileRow * TileSize + TileSize / 2; // Center of tile
    return (x, y);
}
```

### Step 4: Add GetBuildingZoneCount method

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

### Step 5: Add SelectBuildingView method

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

### Step 6: Add ShouldMirror method

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

### Step 7: Add GetAvailableBuildingZones method

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
```

### Step 8: Add GetBuildingKindsForTown method

```csharp
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

### Step 9: Add LogicalToTile method

```csharp
private static (int Row, int Col) LogicalToTile(int logicalX, int logicalY)
{
    var tileCol = logicalX / TileSize;
    var tileRow = logicalY / TileSize;
    return (tileRow, tileCol);
}
```

### Step 10: Add FindNearestRoadTile method

```csharp
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

### Step 11: Add GeneratePathSegmentsFromGrid method

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
```

### Step 12: Rewrite GenerateLayout method

Replace the existing GenerateLayout method with:

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

### Step 13: Remove old helper methods

Delete the following methods from TownLayoutGenerator.cs:
- PlaceBuildingFromSpec
- EnsureBaselineBuildings
- GetBaselinePosition
- PlaceBuilding (overloaded version)
- GeneratePathSegments (old version)
- FindBuildingForSpur

### Step 14: Update tests

Update TownLayoutGeneratorTests.cs to use the new tile-based system. Update test fixtures to use new palette values and verify tile-based behavior.

### Step 15: Run tests

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "TownLayoutGeneratorTests"`
Expected: PASS

### Step 16: Commit

```bash
git add src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs
git commit -m "refactor: rewrite TownLayoutGenerator to use tile-based system

- Add tile grid constants and TileType enum
- Add BuildTileGrid to construct road and spur layout
- Add GetBuildingZoneCount for prosperity-based density
- Add SelectBuildingView for deterministic view selection
- Add ShouldMirror for left-side building mirroring
- Add GetAvailableBuildingZones to identify building zones
- Add GetBuildingKindsForTown to get building list
- Add TileToLogical and LogicalToTile conversion methods
- Add FindNearestRoadTile for path generation
- Add GeneratePathSegmentsFromGrid for tile-based paths
- Rewrite GenerateLayout to use tile grid system
- Remove old layout pattern-based helper methods
- Update tests to verify tile-based layout generation"
```
