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
    public void GenerateLayout_PlacesTrailheadAtRightEdgeAndSpawnInCenter()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.None, TownProsperity.Prosperous, NewTownId("town-1"), 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        Assert.Equal(50, layout.PlayerSpawnX);
        Assert.Equal(50, layout.PlayerSpawnY);

        var trailhead = layout.Buildings.Single(b => b.Kind == BuildingKind.Trailhead);
        // HubAndSpoke pattern places trailhead at (50, 85) with +/-2 jitter
        Assert.InRange(trailhead.X, 48, 52);
        Assert.InRange(trailhead.Y, 83, 87);
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
    public void GenerateLayout_UsesLayoutPatternPositions()
    {
        var townId = NewTownId("town-1");
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, TownProsperity.Prosperous, townId, 0, NewSource(), null, BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        // HubAndSpoke pattern uses different base positions than the old baseline
        // Store is at (35, 20) in the pattern, not (12, 15)
        var store = layout.Buildings.Single(b => b.Kind == BuildingKind.Store);
        Assert.InRange(store.X, 33, 37); // 35 +/- 2
        Assert.InRange(store.Y, 18, 22); // 20 +/- 2
    }
}
