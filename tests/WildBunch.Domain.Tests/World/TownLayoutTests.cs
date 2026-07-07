using System;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests.World;

public sealed class TownLayoutTests
{
    [Fact]
    public void TownLayoutCanBeCreatedWithBuildingsAndPlayerSpawnPosition()
    {
        var buildings = new List<BuildingPlacement>
        {
            new(BuildingKind.Store, 12, 20),
            new(BuildingKind.Sheriff, 30, 12),
            new(BuildingKind.Saloon, 50, 18),
            new(BuildingKind.Trailhead, 5, 40),
            new(BuildingKind.Telegraph, 22, 32)
        };

        var layout = new TownLayout(buildings, PlayerSpawnX: 50, PlayerSpawnY: 35, TownProsperity.Prosperous, Array.Empty<PathSegment>(), new int[10, 10]);

        Assert.Equal(5, layout.Buildings.Count);
        Assert.Equal(BuildingKind.Store, layout.Buildings[0].Kind);
        Assert.Equal(BuildingKind.Telegraph, layout.Buildings[4].Kind);
        Assert.Equal(12, layout.Buildings[0].X);
        Assert.Equal(20, layout.Buildings[0].Y);
        Assert.Equal(BuildingView.FrontOblique, layout.Buildings[0].View);
        Assert.Equal(50, layout.PlayerSpawnX);
        Assert.Equal(35, layout.PlayerSpawnY);
    }

    [Fact]
    public void BuildingPlacementUsesDefaultDimensionsWhenOmitted()
    {
        var placement = new BuildingPlacement(BuildingKind.Store, 10, 20, BuildingView.FrontOblique);

        Assert.Equal(8, placement.Width);
        Assert.Equal(10, placement.Height);
    }

    [Fact]
    public void BuildingPlacementAllowsCustomDimensions()
    {
        var placement = new BuildingPlacement(BuildingKind.Saloon, 10, 20, BuildingView.Profile, Width: 12, Height: 8);

        Assert.Equal(12, placement.Width);
        Assert.Equal(8, placement.Height);
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

    [Fact]
    public void BuildingViewEnumHasExpectedMembers()
    {
        Assert.True(Enum.IsDefined(typeof(BuildingView), BuildingView.Front));
        Assert.True(Enum.IsDefined(typeof(BuildingView), BuildingView.Profile));
        Assert.True(Enum.IsDefined(typeof(BuildingView), BuildingView.Rear));
        Assert.True(Enum.IsDefined(typeof(BuildingView), BuildingView.FrontOblique));
        Assert.True(Enum.IsDefined(typeof(BuildingView), BuildingView.RearOblique));
    }
}
