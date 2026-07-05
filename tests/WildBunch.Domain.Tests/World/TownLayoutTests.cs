using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests.World;

public sealed class TownLayoutTests
{
    [Fact]
    public void TownLayoutCanBeCreatedWithBuildingsAndPlayerSpawnPosition()
    {
        var buildings = new List<BuildingPlacement>
        {
            new(BuildingKind.Store, 100, 200),
            new(BuildingKind.Sheriff, 300, 120),
            new(BuildingKind.Saloon, 500, 180),
            new(BuildingKind.Trailhead, 50, 400),
            new(BuildingKind.Telegraph, 220, 320)
        };

        var layout = new TownLayout(buildings, PlayerSpawnX: 250, PlayerSpawnY: 350);

        Assert.Equal(5, layout.Buildings.Count);
        Assert.Equal(BuildingKind.Store, layout.Buildings[0].Kind);
        Assert.Equal(BuildingKind.Telegraph, layout.Buildings[4].Kind);
        Assert.Equal(100, layout.Buildings[0].X);
        Assert.Equal(200, layout.Buildings[0].Y);
        Assert.Equal(250, layout.PlayerSpawnX);
        Assert.Equal(350, layout.PlayerSpawnY);
    }

    [Fact]
    public void BuildingPlacementUsesDefaultDimensionsWhenOmitted()
    {
        var placement = new BuildingPlacement(BuildingKind.Store, 10, 20);

        Assert.Equal(60, placement.Width);
        Assert.Equal(50, placement.Height);
    }

    [Fact]
    public void BuildingPlacementAllowsCustomDimensions()
    {
        var placement = new BuildingPlacement(BuildingKind.Saloon, 10, 20, Width: 120, Height: 80);

        Assert.Equal(120, placement.Width);
        Assert.Equal(80, placement.Height);
    }

    [Fact]
    public void BuildingKindEnumHasExpectedMembers()
    {
        Assert.True(Enum.IsDefined(typeof(BuildingKind), BuildingKind.Store));
        Assert.True(Enum.IsDefined(typeof(BuildingKind), BuildingKind.Sheriff));
        Assert.True(Enum.IsDefined(typeof(BuildingKind), BuildingKind.Saloon));
        Assert.True(Enum.IsDefined(typeof(BuildingKind), BuildingKind.Trailhead));
        Assert.True(Enum.IsDefined(typeof(BuildingKind), BuildingKind.Telegraph));
    }
}
