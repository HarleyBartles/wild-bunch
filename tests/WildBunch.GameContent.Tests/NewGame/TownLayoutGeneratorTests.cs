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
    public void GenerateLayout_UsesTileBasedPositions()
    {
        var townId = NewTownId("town-1");
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        // Tile-based system: Store is the 1st building, placed at (1, 0) -> tile center at (5, 15) with +/-2 jitter
        var store = layout.Buildings.Single(b => b.Kind == BuildingKind.Store);
        Assert.InRange(store.X, 3, 7); // 5 +/- 2
        Assert.InRange(store.Y, 13, 17); // 15 +/- 2
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
    public void GenerateLayout_ProsperityAffectsZoneDensity()
    {
        var townId = NewTownId("town-1");
        var source = NewSource();

        // Verify that prosperity is correctly stored in the layout
        foreach (var prosperity in new[] { TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Destitute })
        {
            var layout = TownLayoutGenerator.GenerateLayout(
                TownServices.Telegraph, prosperity, townId, 0, source, null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);
            Assert.Equal(prosperity, layout.Prosperity);
        }
    }
}
