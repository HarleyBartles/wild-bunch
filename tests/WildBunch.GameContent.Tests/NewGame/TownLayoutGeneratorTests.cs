using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class TownLayoutGeneratorTests
{
    private static GameSetupDeterministicSource NewSource(Guid? seedCode = null)
        => new(SeedWorldResolver.FormatSeedCode(seedCode ?? SeedWorldResolver.CreateCanonicalSeedCode()));

    private static TownId NewTownId(string value) => new(value);

    [Fact]
    public void GenerateLayout_SameSeedProducesSameLayout()
    {
        var townId = NewTownId("town-1");
        var sourceA = NewSource();
        var sourceB = NewSource();
        var salt = SaltSource.CreateFixed("deterministic-salt");

        var a = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, sourceA, salt, BuildingLayoutPalette.NoSpurs_SpreadEvenly);
        var b = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, sourceB, salt, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        Assert.Equal(a.PlayerSpawnX, b.PlayerSpawnX);
        Assert.Equal(a.PlayerSpawnY, b.PlayerSpawnY);
        Assert.Equal(a.Buildings.Count, b.Buildings.Count);
        for (var i = 0; i < a.Buildings.Count; i++)
        {
            Assert.Equal(a.Buildings[i], b.Buildings[i]);
        }
    }

    [Fact]
    public void GenerateLayout_AlwaysIncludesBaselineNavigationBuildings()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.None, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        var kinds = layout.Buildings.Select(b => b.Kind).ToHashSet();
        Assert.Contains(BuildingKind.Store, kinds);
        Assert.Contains(BuildingKind.Sheriff, kinds);
        Assert.Contains(BuildingKind.Saloon, kinds);
        Assert.Contains(BuildingKind.Trailhead, kinds);
    }

    [Fact]
    public void GenerateLayout_IncludesTelegraphWhenServiceSet()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        var kinds = layout.Buildings.Select(b => b.Kind).ToHashSet();
        Assert.Contains(BuildingKind.Telegraph, kinds);
    }

    [Fact]
    public void GenerateLayout_ExcludesTelegraphWhenNoServices()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.None, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        var kinds = layout.Buildings.Select(b => b.Kind).ToHashSet();
        Assert.DoesNotContain(BuildingKind.Telegraph, kinds);
    }

    [Fact]
    public void GenerateLayout_PlacesTrailheadInBuildingZoneAndSpawnInCenter()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.None, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        Assert.Equal(50, layout.PlayerSpawnX);
        Assert.Equal(50, layout.PlayerSpawnY);

        var trailhead = layout.Buildings.Single(b => b.Kind == BuildingKind.Trailhead);
        // Tile-based system: trailhead is placed in a building zone tile
        // Building zones are at columns 0 (left) and 3 (right), rows 1-8
        // Zones are filled in order: (1,0), (1,3), (2,0), (2,3), (3,0), (3,3), ...
        // With Prosperous prosperity (0.75 density), 6 of 8 zones are filled
        // Trailhead is the 4th building, so it's placed at (2, 3) -> tile center at (35, 25) with +/-2 jitter
        Assert.InRange(trailhead.X, 33, 37);
        Assert.InRange(trailhead.Y, 23, 27);
    }

    [Fact]
    public void GenerateLayout_BaselineBuildingsUseStandardFootprint()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        Assert.All(layout.Buildings, b =>
        {
            Assert.Equal(8, b.Width);
            Assert.Equal(10, b.Height);
        });
    }

    [Fact]
    public void GenerateLayout_UsesTileBasedPositionsWithoutJitter()
    {
        var townId = NewTownId("town-1");
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        // Tile-based system: buildings are placed at exact tile centers (no jitter)
        // Each tile is 10 logical units, so tile (row, col) centers at (col*10 + 5, row*10 + 5)
        var store = layout.Buildings.Single(b => b.Kind == BuildingKind.Store);
        // Store should be placed at a building zone tile center
        // Building zones are at columns 3 (left) and 6 (right)
        Assert.True(store.X == 35 || store.X == 65, $"Store X should be 35 or 65, got {store.X}");
        Assert.InRange(store.Y, 15, 85); // Building zones are in rows 1-8
    }

    [Fact]
    public void GenerateLayout_AlwaysPlacesRequiredBuildings()
    {
        var townId = NewTownId("town-1");
        var source = NewSource();

        // All prosperity levels should place all required buildings
        // Required buildings (Store, Sheriff, Saloon, Trailhead, Telegraph when service is set)
        // override prosperity-based density calculations
        foreach (var prosperity in new[] { TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Destitute })
        {
            var layout = TownLayoutGenerator.GenerateLayout(
                TownServices.Telegraph, prosperity, townId, 0, source, null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);
            Assert.Equal(5, layout.Buildings.Count); // Store, Sheriff, Saloon, Trailhead, Telegraph
        }
    }

    [Fact]
    public void GenerateLayout_SpursAddSpurBuildingZones()
    {
        var townId = NewTownId("town-1");
        var source = NewSource();

        // No spurs: 8 building zones
        var noSpursLayout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, source, null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);
        Assert.Equal(5, noSpursLayout.Buildings.Count);

        // One spur: 8 building zones + 1 spur zone = 9 zones
        var oneSpurLayout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, source, null, BuildingLayoutPalette.OneSpurLeft_SpreadEvenly);
        Assert.Equal(5, oneSpurLayout.Buildings.Count); // Still 5 buildings (zones available >= building count)
    }

    [Fact]
    public void GenerateLayout_GeneratesPathSegments()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        // Each building should have a path segment to the road
        Assert.Equal(layout.Buildings.Count, layout.Paths.Count);
    }

    [Fact]
    public void GenerateLayout_SpursArePrioritizedForBuildingPlacement()
    {
        var townId = NewTownId("town-1");
        var source = NewSource();

        // With a spur, at least one building should be placed on the spur
        var oneSpurLayout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, source, null, BuildingLayoutPalette.OneSpurLeft_SpreadEvenly);
        
        // Check that at least one building is on the left side (where the spur is)
        // Spur building zone is at column 2 (left of building zone at column 3)
        var leftSideBuildings = oneSpurLayout.Buildings.Where(b => b.X < 35).ToList();
        Assert.True(leftSideBuildings.Count > 0, "At least one building should be on the left side (spur)");
    }

    [Fact]
    public void GenerateLayout_TileGridHasCorrectDimensions()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        Assert.NotNull(layout.TileGrid);
        Assert.Equal(10, layout.TileGrid.Length); // 10 rows
        Assert.All(layout.TileGrid, row => Assert.Equal(10, row.Length)); // 10 columns per row
    }

    [Fact]
    public void GenerateLayout_TileGridHasMajorRoadInCenter()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        Assert.NotNull(layout.TileGrid);
        
        // Road tiles should be at columns 4 and 5 (center) for all rows
        for (var row = 0; row < 10; row++)
        {
            Assert.Equal(1, layout.TileGrid[row][4]); // Road
            Assert.Equal(1, layout.TileGrid[row][5]); // Road
        }
    }

    [Fact]
    public void GenerateLayout_TileGridHasBuildingZonesOnSides()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        Assert.NotNull(layout.TileGrid);
        
        // Building zones should be at columns 3 (left) and 6 (right) for rows 1-8 (skip trailhead rows)
        for (var row = 1; row < 9; row++)
        {
            Assert.Equal(2, layout.TileGrid[row][3]); // BuildingZone
            Assert.Equal(2, layout.TileGrid[row][6]); // BuildingZone
        }
    }

    [Fact]
    public void GenerateLayout_TileGridHasSpurTilesWhenSpursExist()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.OneSpurLeft_SpreadEvenly);

        Assert.NotNull(layout.TileGrid);
        
        // OneSpurLeft palette has a spur at row 4, direction West
        // SpurStart at column 3 (junction), SpurRoad at column 2 (extension)
        Assert.Equal(3, layout.TileGrid[4][3]); // SpurStart
        Assert.Equal(4, layout.TileGrid[4][2]); // SpurRoad
    }

    [Fact]
    public void GenerateLayout_BuildingsAreDistributedVertically()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        // Get non-trailhead buildings
        var nonTrailheadBuildings = layout.Buildings.Where(b => b.Kind != BuildingKind.Trailhead).ToList();
        
        // Buildings should be spread across different rows, not bunched at the top
        var rows = nonTrailheadBuildings.Select(b => b.Y / 10).Distinct().ToList();
        Assert.True(rows.Count > 1, "Buildings should be distributed across multiple rows");
    }

    [Fact]
    public void GenerateLayout_PathsAreDisabled()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        // Path generation is disabled until proper tile-based rules are implemented
        Assert.Empty(layout.Paths);
    }
}
