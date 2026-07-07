using System;
using System.Linq;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Generates deterministic town hub surface layouts. The same seed code, town
/// identity, and <see cref="TownServices"/> always produce the same layout — no
/// unseeded randomness is used. Buildings are placed on a fixed logical grid
/// (0-100 in both dimensions) with small deterministic +/-2 jitter derived
/// from <see cref="GameSetupDeterministicSource.PickIndex"/> so each town
/// looks slightly different while remaining reproducible. The frontend scales
/// these logical units to actual canvas pixels.
/// </summary>
internal static class TownLayoutGenerator
{
    private const int SceneWidth = 100;
    private const int SceneHeight = 100;
    private const int PlayerSpawnX = 50;
    private const int PlayerSpawnY = 50;

    private const int BuildingWidth = 8;
    private const int BuildingHeight = 10;

    // Jitter range: PickIndex(label, 5) yields 0..4, subtract 2 -> -2..+2.
    private const int JitterRange = 5;
    private const int JitterOffset = 2;

    // Tile grid constants
    private const int TileSize = 10; // Each tile is 10 logical units
    private const int GridWidth = 10; // 10 tiles wide
    private const int GridHeight = 10; // 10 tiles tall
    private const int RoadColumnStart = 4; // Road tiles start at column 4 (central)
    private const int RoadColumnEnd = 5; // Road tiles end at column 5 (central)
    private const int BuildingZoneLeft = 3; // Left building zone
    private const int BuildingZoneRight = 6; // Right building zone

    // Tile type enum for grid representation
    private enum TileType
    {
        Empty,
        Road,
        BuildingZone,
        SpurStart,
        SpurRoad
    }

    /// <summary>
    /// Generates a deterministic <see cref="TownLayout"/> for a town hub surface.
    /// Always emits the baseline navigation buildings (Store, Sheriff, Saloon,
    /// Trailhead). Emits Telegraph only when <paramref name="services"/> has the
    /// <see cref="TownServices.Telegraph"/> flag set. Uses the
    /// <paramref name="layoutPalette"/> to select the building layout pattern from
    /// <see cref="BuildingLayoutCatalog"/>.
    /// </summary>
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

        // Assign buildings to zones using seed-derived ordering
        var buildingKinds = GetBuildingKindsForTown(services);
        
        // Handle Trailhead specially - place at north and south tips of major road
        // Trailhead spans 2 tiles horizontally above and below the road (columns 3-6)
        // North Trailhead: row 0, centered at road (x=50)
        // South Trailhead: row 9, centered at road (x=50)
        if (buildingKinds.Contains(BuildingKind.Trailhead))
        {
            // North Trailhead at row 0, spanning columns 3-6
            var northTrailheadX = 50; // Center of road
            var northTrailheadY = 5; // Center of row 0 (tile center)
            buildings.Add(new BuildingPlacement(BuildingKind.Trailhead, northTrailheadX, northTrailheadY, BuildingView.Front, 20, 10));
            
            // South Trailhead at row 9, spanning columns 3-6
            var southTrailheadX = 50; // Center of road
            var southTrailheadY = 95; // Center of row 9 (tile center)
            buildings.Add(new BuildingPlacement(BuildingKind.Trailhead, southTrailheadX, southTrailheadY, BuildingView.Front, 20, 10));
            
            // Remove Trailhead from regular building list
            buildingKinds.Remove(BuildingKind.Trailhead);
        }

        // Place remaining buildings in regular zones
        if (buildingKinds.Count > 0)
        {
            // Calculate how many zones to fill based on prosperity
            var zonesToFill = GetBuildingZoneCount(prosperity, availableZones.Count);
            // Ensure we have enough zones for all required buildings (required buildings override prosperity)
            var zonesNeeded = Math.Min(buildingKinds.Count, Math.Max(zonesToFill, buildingKinds.Count));
            
            // Distribute zones evenly across vertical space instead of bunching at top
            var zonesToUse = DistributeZonesVertically(availableZones, zonesNeeded);
            for (var i = 0; i < buildingKinds.Count && i < zonesToUse.Count; i++)
            {
                var (row, col, isOnSpur) = zonesToUse[i];
                var kind = buildingKinds[i];
                var isOnLeftSide = col < RoadColumnStart;

                // Calculate base position from tile (no jitter for consistent placement)
                var (baseX, baseY) = TileToLogical(row, col);

                // Select building view
                var viewLabel = $"town-{townId.Value}-slot-{townSlotIndex}-building-{kind.ToString().ToLowerInvariant()}-view";
                var view = SelectBuildingView(isOnSpur, isOnLeftSide, source, viewLabel);

                buildings.Add(new BuildingPlacement(kind, baseX, baseY, view, BuildingWidth, BuildingHeight));
            }
        }

        // Generate path segments from buildings to roads
        paths = GeneratePathSegmentsFromGrid(grid, buildings, townId, townSlotIndex, source, saltSource);

        // Convert TileType grid to int grid for serialization
        var tileGrid = new int[GridHeight][];
        for (var row = 0; row < GridHeight; row++)
        {
            tileGrid[row] = new int[GridWidth];
            for (var col = 0; col < GridWidth; col++)
            {
                tileGrid[row][col] = (int)grid[row, col];
            }
        }

        return new TownLayout(buildings, PlayerSpawnX, PlayerSpawnY, prosperity, paths, tileGrid);
    }

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

            // Spur is 2 tiles wide: junction tile + extension tile
            // West spurs: junction at column 3 (building zone), extension at column 2
            // East spurs: junction at column 6 (building zone), extension at column 7
            var spurJunctionCol = spurDirection == SpurDirection.West ? BuildingZoneLeft : BuildingZoneRight;
            var spurExtensionCol = spurDirection == SpurDirection.West ? BuildingZoneLeft - 1 : BuildingZoneRight + 1;

            // Mark junction tile (where spur meets the road)
            grid[spurRow, spurJunctionCol] = TileType.SpurStart;
            
            // Mark extension tile (extends further outward)
            if (spurExtensionCol >= 0 && spurExtensionCol < GridWidth)
            {
                grid[spurRow, spurExtensionCol] = TileType.SpurRoad;
            }
        }

        return grid;
    }

    private static (int X, int Y) TileToLogical(int tileRow, int tileCol)
    {
        var x = tileCol * TileSize + TileSize / 2; // Center of tile
        var y = tileRow * TileSize + TileSize / 2; // Center of tile
        return (x, y);
    }

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

        // Add spur building zones (1 per spur, above the spur extension tile)
        for (var i = 0; i < paletteSpec.SpurCount; i++)
        {
            var spurRow = paletteSpec.SpurRows[i];
            var spurDirection = paletteSpec.SpurDirections[i];

            // Spur building zone is above the spur extension tile (not the junction)
            // West spurs: extension at column 2, building zone at column 2 (row - 1)
            // East spurs: extension at column 7, building zone at column 7 (row - 1)
            var spurJunctionCol = spurDirection == SpurDirection.West ? BuildingZoneLeft : BuildingZoneRight;
            var spurExtensionCol = spurDirection == SpurDirection.West ? BuildingZoneLeft - 1 : BuildingZoneRight + 1;

            if (spurRow > 0 && spurExtensionCol >= 0 && spurExtensionCol < GridWidth)
            {
                zones.Add((spurRow - 1, spurExtensionCol, true));
            }
        }

        return zones;
    }

    private static List<(int Row, int Col, bool IsOnSpur)> DistributeZonesVertically(
        List<(int Row, int Col, bool IsOnSpur)> availableZones,
        int zonesNeeded)
    {
        if (zonesNeeded >= availableZones.Count)
        {
            return availableZones.ToList();
        }

        // Sort zones by row to get vertical distribution
        var sortedZones = availableZones.OrderBy(z => z.Row).ToList();
        
        // Select zones at regular intervals to distribute vertically
        var step = (double)(sortedZones.Count - 1) / Math.Max(1, zonesNeeded - 1);
        var selectedZones = new List<(int Row, int Col, bool IsOnSpur)>();
        
        for (var i = 0; i < zonesNeeded; i++)
        {
            var index = (int)Math.Round(i * step);
            index = Math.Clamp(index, 0, sortedZones.Count - 1);
            selectedZones.Add(sortedZones[index]);
        }

        return selectedZones;
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

    internal static int GetBuildingZoneCount(TownProsperity prosperity, int totalZones)
    {
        // Calculate density based on prosperity level
        var density = prosperity switch
        {
            TownProsperity.Boomtown => 1.0,
            TownProsperity.Prosperous => 0.75,
            TownProsperity.Poor => 0.5,
            TownProsperity.Destitute => 0.25,
            _ => 0.75
        };

        return (int)Math.Ceiling(totalZones * density);
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
                
                // Calculate front door position (edge of building facing the road)
                double frontDoorX, frontDoorY;
                
                if (building.Kind == BuildingKind.Trailhead)
                {
                    // Trailhead at north/south tips of road - front door faces road vertically
                    // North Trailhead (y=5): front door at bottom edge
                    // South Trailhead (y=95): front door at top edge
                    if (building.Y < 50)
                    {
                        // North Trailhead - front door at bottom
                        frontDoorX = building.X + BuildingWidth / 2;
                        frontDoorY = building.Y + BuildingHeight;
                    }
                    else
                    {
                        // South Trailhead - front door at top
                        frontDoorX = building.X + BuildingWidth / 2;
                        frontDoorY = building.Y;
                    }
                }
                else
                {
                    // Regular buildings - front door faces road horizontally
                    // Road is centered at x=50, so buildings with x < 50 are on the left side
                    var isOnLeftSide = building.X < 50;
                    frontDoorX = isOnLeftSide ? building.X + BuildingWidth : building.X;
                    frontDoorY = building.Y + BuildingHeight / 2;
                }

                // Add jitter for visual variety
                var jitterLabel = $"town-{townId.Value}-slot-{townSlotIndex}-path-{building.Kind.ToString().ToLowerInvariant()}{saltSegment}";
                var jitter = Jitter(source, jitterLabel);

                var pathStartX = ClampToScene((int)(frontDoorX + jitter), SceneWidth);
                var pathStartY = ClampToScene((int)(frontDoorY + jitter), SceneHeight);
                var pathEndX = ClampToScene((int)(roadX + jitter), SceneWidth);
                var pathEndY = ClampToScene((int)(roadY + jitter), SceneHeight);

                paths.Add(PathSegment.Create(pathStartX, pathStartY, pathEndX, pathEndY));
            }
        }

        return paths;
    }

    private static int Jitter(GameSetupDeterministicSource source, string label)
        => source.PickIndex(label, JitterRange) - JitterOffset;

    private static int ClampToScene(int value, int max)
    {
        if (value < 0) return 0;
        if (value > max) return max;
        return value;
    }
}
