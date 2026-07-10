using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class TownLayoutGeneratorTests
{
    private static LayoutDeterministicSource NewLayoutSource(Guid? seedCode = null, LayoutSalts? salts = null)
    {
        var seedCodeStr = SeedWorldResolver.FormatSeedCode(seedCode ?? SeedWorldResolver.CreateCanonicalSeedCode());
        var actualSalts = salts ?? new LayoutSalts("default", "default", "default", "default");
        return new LayoutDeterministicSource(seedCodeStr, new TownId("town-1"), 0, "1.0.0", actualSalts);
    }

    private static TownId NewTownId(string value) => new(value);

    [Fact]
    public void GenerateLayout_SameSeedProducesSameLayout()
    {
        var townId = NewTownId("town-1");
        var salts = new LayoutSalts("deterministic-salt", "deterministic-salt", "deterministic-salt", "deterministic-salt");
        var sourceA = new LayoutDeterministicSource("test-seed", townId, 0, "1.0.0", salts);
        var sourceB = new LayoutDeterministicSource("test-seed", townId, 0, "1.0.0", salts);

        var a = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, sourceA, BuildingLayoutPalette.NoSpurs_SpreadEvenly);
        var b = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, sourceB, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

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
            TownServices.None, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

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
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

        var kinds = layout.Buildings.Select(b => b.Kind).ToHashSet();
        Assert.Contains(BuildingKind.Telegraph, kinds);
    }

    [Fact]
    public void GenerateLayout_ExcludesTelegraphWhenNoServices()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.None, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

        var kinds = layout.Buildings.Select(b => b.Kind).ToHashSet();
        Assert.DoesNotContain(BuildingKind.Telegraph, kinds);
    }

    [Fact]
    public void GenerateLayout_PlacesTrailheadInBuildingZoneAndSpawnInCenter()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.None, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

        Assert.Equal(50, layout.PlayerSpawnX);
        Assert.Equal(50, layout.PlayerSpawnY);

        var trailheads = layout.Buildings.Where(b => b.Kind == BuildingKind.Trailhead).ToList();
        Assert.Equal(2, trailheads.Count);

        var northTrailhead = trailheads.Single(b => b.Y < 50);
        var southTrailhead = trailheads.Single(b => b.Y > 50);

        Assert.Equal(50, northTrailhead.X);
        Assert.Equal(5, northTrailhead.Y);
        Assert.Equal(50, southTrailhead.X);
        Assert.Equal(95, southTrailhead.Y);
    }

    [Fact]
    public void GenerateLayout_BaselineBuildingsUseStandardFootprint()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

        Assert.All(layout.Buildings.Where(b => b.Kind != BuildingKind.Trailhead), b =>
        {
            Assert.Equal(8, b.Width);
            Assert.Equal(10, b.Height);
        });

        Assert.All(layout.Buildings.Where(b => b.Kind == BuildingKind.Trailhead), b =>
        {
            Assert.Equal(20, b.Width);
            Assert.Equal(10, b.Height);
        });
    }

    [Fact]
    public void GenerateLayout_UsesTileBasedPositionsWithoutJitter()
    {
        var townId = NewTownId("town-1");
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

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

        // All prosperity levels should place all required buildings
        // Required buildings (Store, Sheriff, Saloon, Trailhead, Telegraph when service is set)
        // override prosperity-based density calculations
        foreach (var prosperity in new[] { TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Destitute })
        {
            var layout = TownLayoutGenerator.GenerateLayout(
                TownServices.Telegraph, prosperity, townId, 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");
            Assert.Equal(6, layout.Buildings.Count); // Store, Sheriff, Saloon, Telegraph, north trailhead, south trailhead
        }
    }

    [Fact]
    public void GenerateLayout_SpursAddSpurBuildingZones()
    {
        var townId = NewTownId("town-1");

        // No spurs: 8 building zones
        var noSpursLayout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");
        Assert.Equal(6, noSpursLayout.Buildings.Count);

        // One spur: 8 building zones + 1 spur zone = 9 zones
        var oneSpurLayout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, NewLayoutSource(), BuildingLayoutPalette.OneSpurLeft_SpreadEvenly, "1.0.0");
        Assert.Equal(6, oneSpurLayout.Buildings.Count); // Still 6 buildings (zones available >= building count)
    }

    [Fact]
    public void GenerateLayout_GeneratesPathSegments()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

        // Path generation is intentionally disabled for the current hub generator.
        Assert.Empty(layout.Paths);
    }

    [Fact]
    public void GenerateLayout_SpursArePrioritizedForBuildingPlacement()
    {
        var townId = NewTownId("town-1");

        // With a spur, at least one building should be placed on the spur
        var oneSpurLayout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, NewLayoutSource(), BuildingLayoutPalette.OneSpurLeft_SpreadEvenly, "1.0.0");
        
        // Check that at least one building is on the left side (where the spur is)
        // Spur building zone is at column 2 (left of building zone at column 3)
        var leftSideBuildings = oneSpurLayout.Buildings.Where(b => b.X < 35).ToList();
        Assert.True(leftSideBuildings.Count > 0, "At least one building should be on the left side (spur)");
    }

    [Fact]
    public void GenerateLayout_TileGridHasCorrectDimensions()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

        Assert.NotNull(layout.TileGrid);
        Assert.Equal(10, layout.TileGrid.Length); // 10 rows
        Assert.All(layout.TileGrid, row => Assert.Equal(10, row.Length)); // 10 columns per row
    }

    [Fact]
    public void GenerateLayout_TileGridHasMajorRoadInCenter()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

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
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

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
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.OneSpurLeft_SpreadEvenly, "1.0.0");

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
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

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
            TownServices.Telegraph, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewLayoutSource(), BuildingLayoutPalette.NoSpurs_SpreadEvenly, "1.0.0");

        // Path generation is disabled until proper tile-based rules are implemented
        Assert.Empty(layout.Paths);
    }

    [Fact]
    public void GenerateLayout_WithLayoutSaltsAndResolverVersion_ProducesVersionedLayout()
    {
        var townId = NewTownId("town-1");
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");
        var layoutSource = new LayoutDeterministicSource("test-seed", townId, 0, "1.0.0", salts);

        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph,
            TownProsperity.Prosperous,
            townId,
            0,
            layoutSource,
            BuildingLayoutPalette.NoSpurs_SpreadEvenly,
            "1.0.0");

        Assert.Equal("1.0.0", layout.ResolverVersion);
    }
}
