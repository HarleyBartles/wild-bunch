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
        // Calculate how many zones to fill based on prosperity
        var zonesToFill = GetBuildingZoneCount(prosperity, availableZones.Count);
        // Ensure we have enough zones for all required buildings (required buildings override prosperity)
        var zonesNeeded = Math.Min(buildingKinds.Count, Math.Max(zonesToFill, buildingKinds.Count));
        var zonesToUse = availableZones.Take(zonesNeeded).ToList();
        for (var i = 0; i < buildingKinds.Count && i < zonesToUse.Count; i++)
        {
            var (row, col, isOnSpur) = zonesToUse[i];
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

    private static int GetBuildingZoneCount(TownProsperity prosperity, int totalZones)
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

    private static int Jitter(GameSetupDeterministicSource source, string label)
        => source.PickIndex(label, JitterRange) - JitterOffset;

    private static int ClampToScene(int value, int max)
    {
        if (value < 0) return 0;
        if (value > max) return max;
        return value;
    }
}
