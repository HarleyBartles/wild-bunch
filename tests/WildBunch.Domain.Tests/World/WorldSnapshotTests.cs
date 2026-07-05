using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests.World;

public sealed class WorldSnapshotTests
{
    [Fact]
    public void TownSnapshotRoundTripPreservesLayout()
    {
        var buildings = new List<BuildingPlacement>
        {
            new(BuildingKind.Store, 10, 20),
            new(BuildingKind.Sheriff, 30, 12),
            new(BuildingKind.Saloon, 50, 18),
            new(BuildingKind.Trailhead, 5, 40),
            new(BuildingKind.Telegraph, 22, 32)
        };
        var layout = new TownLayout(buildings, PlayerSpawnX: 50, PlayerSpawnY: 35);
        var town = new Town(
            new TownId("t1"),
            "Dodge",
            TownServices.Telegraph,
            TownProsperity.Boomtown,
            MapX: 100,
            MapY: 200,
            IsOutlier: false,
            Layout: layout);

        var snapshot = TownSnapshot.FromDomain(town);
        var restored = snapshot.ToDomain();

        Assert.NotNull(restored.Layout);
        Assert.Equal(50, restored.Layout!.PlayerSpawnX);
        Assert.Equal(35, restored.Layout.PlayerSpawnY);
        Assert.Equal(5, restored.Layout.Buildings.Count);
        Assert.Equal(BuildingKind.Store, restored.Layout.Buildings[0].Kind);
        Assert.Equal(BuildingKind.Telegraph, restored.Layout.Buildings[4].Kind);
        Assert.Equal(10, restored.Layout.Buildings[0].X);
        Assert.Equal(20, restored.Layout.Buildings[0].Y);
        Assert.Equal(8, restored.Layout.Buildings[0].Width);
        Assert.Equal(10, restored.Layout.Buildings[0].Height);
    }

    [Fact]
    public void TownSnapshotRoundTripPreservesNullLayout()
    {
        var town = new Town(
            new TownId("t2"),
            "Tombstone",
            TownServices.None,
            TownProsperity.Poor,
            MapX: 500,
            MapY: 500,
            IsOutlier: true,
            Layout: null);

        var snapshot = TownSnapshot.FromDomain(town);
        var restored = snapshot.ToDomain();

        Assert.Null(restored.Layout);
        Assert.Equal("t2", restored.Id.Value);
        Assert.Equal("Tombstone", restored.Name);
        Assert.True(restored.IsOutlier);
    }
}
