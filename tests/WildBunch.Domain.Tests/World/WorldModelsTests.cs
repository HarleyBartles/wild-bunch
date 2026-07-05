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
            new(BuildingKind.Store, 10, 20),
            new(BuildingKind.Saloon, 30, 12)
        };
        var layout = new TownLayout(buildings, PlayerSpawnX: 50, PlayerSpawnY: 35);

        var town = new Town(
            new TownId("t1"),
            "Dodge",
            TownServices.Telegraph,
            Layout: layout);

        Assert.NotNull(town.Layout);
        Assert.Equal(2, town.Layout!.Buildings.Count);
        Assert.Equal(50, town.Layout.PlayerSpawnX);
        Assert.Equal(35, town.Layout.PlayerSpawnY);
    }

    [Fact]
    public void TownWithExpressionPreservesLayout()
    {
        var buildings = new List<BuildingPlacement>
        {
            new(BuildingKind.Store, 10, 20)
        };
        var layout = new TownLayout(buildings, PlayerSpawnX: 50, PlayerSpawnY: 35);
        var town = new Town(new TownId("t1"), "Dodge", TownServices.Telegraph, Layout: layout);

        var renamed = town with { Name = "Tombstone" };

        Assert.Equal("Tombstone", renamed.Name);
        Assert.Same(layout, renamed.Layout);
    }
}
