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
            TownServices.Telegraph, townId, 0, sourceA, salt);
        var b = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, townId, 0, sourceB, salt);

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
            TownServices.None, NewTownId("town-1"), 0, NewSource(), null);

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
            TownServices.Telegraph, NewTownId("town-1"), 0, NewSource(), null);

        var kinds = layout.Buildings.Select(b => b.Kind).ToHashSet();
        Assert.Contains(BuildingKind.Telegraph, kinds);
    }

    [Fact]
    public void GenerateLayout_ExcludesTelegraphWhenNoServices()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.None, NewTownId("town-1"), 0, NewSource(), null);

        var kinds = layout.Buildings.Select(b => b.Kind).ToHashSet();
        Assert.DoesNotContain(BuildingKind.Telegraph, kinds);
    }

    [Fact]
    public void GenerateLayout_PlacesTrailheadAtRightEdgeAndSpawnInCenter()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.None, NewTownId("town-1"), 0, NewSource(), null);

        Assert.Equal(50, layout.PlayerSpawnX);
        Assert.Equal(50, layout.PlayerSpawnY);

        var trailhead = layout.Buildings.Single(b => b.Kind == BuildingKind.Trailhead);
        // Trailhead base position is (90, 50) at the right edge; jitter is +/-2.
        Assert.InRange(trailhead.X, 88, 92);
        Assert.InRange(trailhead.Y, 48, 52);
    }

    [Fact]
    public void GenerateLayout_BaselineBuildingsUseStandardFootprint()
    {
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, NewTownId("town-1"), 0, NewSource(), null);

        Assert.All(layout.Buildings, b =>
        {
            Assert.Equal(8, b.Width);
            Assert.Equal(10, b.Height);
        });
    }

    [Fact]
    public void GenerateLayout_JitterStaysWithinTwoUnitsOfBasePosition()
    {
        var townId = NewTownId("town-1");
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph, townId, 0, NewSource(), null);

        var expected = new Dictionary<BuildingKind, (int X, int Y)>
        {
            [BuildingKind.Store] = (12, 15),
            [BuildingKind.Sheriff] = (46, 15),
            [BuildingKind.Saloon] = (80, 15),
            [BuildingKind.Trailhead] = (90, 50),
            [BuildingKind.Telegraph] = (46, 70),
        };

        foreach (var building in layout.Buildings)
        {
            var (baseX, baseY) = expected[building.Kind];
            Assert.InRange(building.X, baseX - 2, baseX + 2);
            Assert.InRange(building.Y, baseY - 2, baseY + 2);
        }
    }
}
