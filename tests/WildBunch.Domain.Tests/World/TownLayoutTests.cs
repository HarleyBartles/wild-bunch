using System;
using WildBunch.Domain.World;
using Xunit;

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

        var layout = new TownLayout(buildings, PlayerSpawnX: 50, PlayerSpawnY: 35, TownProsperity.Prosperous, Array.Empty<PathSegment>(), null);

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

    [Fact]
    public void TownLayout_WithResolverVersion_CreatesSuccessfully()
    {
        var layout = new TownLayout(
            [],
            50,
            50,
            TownProsperity.Prosperous,
            [],
            null,
            "1.0.0");

        Assert.Equal("1.0.0", layout.ResolverVersion);
    }

    [Theory]
    [InlineData(-1, 20, 8, 10)]
    [InlineData(101, 20, 8, 10)]
    [InlineData(10, -1, 8, 10)]
    [InlineData(10, 101, 8, 10)]
    [InlineData(10, 20, 0, 10)]
    [InlineData(10, 20, -1, 10)]
    [InlineData(10, 20, 8, 0)]
    [InlineData(10, 20, 8, -1)]
    public void BuildingPlacement_DirectConstructionRejectsInvalidValues(
        int x, int y, int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BuildingPlacement(BuildingKind.Store, x, y, BuildingView.Front, width, height));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void BuildingPlacement_WithUpdateRejectsInvalidValues(int property)
    {
        var placement = new BuildingPlacement(BuildingKind.Store, 10, 20, BuildingView.Front, 8, 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => property switch
        {
            0 => placement with { X = 101 },
            1 => placement with { Y = -1 },
            2 => placement with { Width = 0 },
            3 => placement with { Height = -1 },
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        });
    }

    [Fact]
    public void BuildingPlacement_AcceptsInclusiveCenterBoundariesAndPositiveDimensions()
    {
        var placement = new BuildingPlacement(
            BuildingKind.Trailhead,
            X: 0,
            Y: 100,
            BuildingView.Front,
            Width: 1,
            Height: 1);

        Assert.Equal(0, placement.X);
        Assert.Equal(100, placement.Y);
        Assert.Equal(1, placement.Width);
        Assert.Equal(1, placement.Height);
    }

    [Fact]
    public void BuildingPlacement_PreservesPositionalEqualityAndDeconstruction()
    {
        var expected = new BuildingPlacement(BuildingKind.Saloon, 10, 20, BuildingView.Profile, 12, 8);
        var actual = new BuildingPlacement(BuildingKind.Saloon, 10, 20, BuildingView.Profile, 12, 8);

        Assert.Equal(expected, actual);

        var (kind, x, y, view, width, height) = actual;
        Assert.Equal(BuildingKind.Saloon, kind);
        Assert.Equal(10, x);
        Assert.Equal(20, y);
        Assert.Equal(BuildingView.Profile, view);
        Assert.Equal(12, width);
        Assert.Equal(8, height);
    }

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(101, 50)]
    [InlineData(50, -1)]
    [InlineData(50, 101)]
    public void TownLayout_DirectConstructionRejectsInvalidSpawnCoordinates(int spawnX, int spawnY)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateLayout(spawnX, spawnY));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void TownLayout_WithUpdateRejectsInvalidSpawnCoordinates(int coordinate)
    {
        var layout = CreateLayout(50, 50);

        Assert.Throws<ArgumentOutOfRangeException>(() => coordinate switch
        {
            0 => layout with { PlayerSpawnX = 101 },
            1 => layout with { PlayerSpawnY = -1 },
            _ => throw new ArgumentOutOfRangeException(nameof(coordinate))
        });
    }

    [Fact]
    public void TownLayout_AcceptsInclusiveSpawnBoundaries()
    {
        var layout = CreateLayout(0, 100);

        Assert.Equal(0, layout.PlayerSpawnX);
        Assert.Equal(100, layout.PlayerSpawnY);
    }

    [Fact]
    public void TownLayout_PreservesPositionalEqualityAndDeconstruction()
    {
        var buildings = Array.Empty<BuildingPlacement>();
        var paths = Array.Empty<PathSegment>();
        var expected = new TownLayout(buildings, 10, 20, TownProsperity.Prosperous, paths);
        var actual = new TownLayout(buildings, 10, 20, TownProsperity.Prosperous, paths);

        Assert.Equal(expected, actual);

        var (actualBuildings, spawnX, spawnY, prosperity, actualPaths, tileGrid, resolverVersion, layoutSalts) = actual;
        Assert.Same(buildings, actualBuildings);
        Assert.Equal(10, spawnX);
        Assert.Equal(20, spawnY);
        Assert.Equal(TownProsperity.Prosperous, prosperity);
        Assert.Same(paths, actualPaths);
        Assert.Null(tileGrid);
        Assert.Equal(TownLayout.CurrentResolverVersion, resolverVersion);
        Assert.Null(layoutSalts);
    }

    private static TownLayout CreateLayout(int spawnX, int spawnY)
        => new(
            Array.Empty<BuildingPlacement>(),
            spawnX,
            spawnY,
            TownProsperity.Prosperous,
            Array.Empty<PathSegment>());
}
