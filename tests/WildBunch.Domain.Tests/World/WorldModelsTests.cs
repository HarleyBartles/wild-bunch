using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests.World;

public sealed class WorldModelsTests
{
    [Fact]
    public void TownDefaultsLayoutToNullWhenNotProvided()
    {
        var town = new Town(new TownId("t1"), "Dodge", TownServices.Telegraph);

        Assert.Null(town.Layout);
    }

    [Fact]
    public void TownHoldsLayoutWhenProvided()
    {
        var buildings = new List<BuildingPlacement>
        {
            new(BuildingKind.Store, 100, 200),
            new(BuildingKind.Saloon, 300, 120)
        };
        var layout = new TownLayout(buildings, PlayerSpawnX: 250, PlayerSpawnY: 350);

        var town = new Town(
            new TownId("t1"),
            "Dodge",
            TownServices.Telegraph,
            Layout: layout);

        Assert.NotNull(town.Layout);
        Assert.Equal(2, town.Layout!.Buildings.Count);
        Assert.Equal(250, town.Layout.PlayerSpawnX);
        Assert.Equal(350, town.Layout.PlayerSpawnY);
    }

    [Fact]
    public void TownWithExpressionPreservesLayout()
    {
        var buildings = new List<BuildingPlacement>
        {
            new(BuildingKind.Store, 100, 200)
        };
        var layout = new TownLayout(buildings, PlayerSpawnX: 250, PlayerSpawnY: 350);
        var town = new Town(new TownId("t1"), "Dodge", TownServices.Telegraph, Layout: layout);

        var renamed = town with { Name = "Tombstone" };

        Assert.Equal("Tombstone", renamed.Name);
        Assert.Same(layout, renamed.Layout);
    }
}
