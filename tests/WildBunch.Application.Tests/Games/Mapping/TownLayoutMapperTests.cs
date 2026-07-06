using System;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Games.Mapping;

public sealed class TownLayoutMapperTests
{
    [Fact]
    public void ToDto_MapsTownLayoutToTownLayoutDto_IncludingAllBuildingKindValues()
    {
        var layout = new TownLayout(
            new[]
            {
                new BuildingPlacement(BuildingKind.Store, 10, 20),
                new BuildingPlacement(BuildingKind.Sheriff, 30, 40, 12, 8),
                new BuildingPlacement(BuildingKind.Saloon, 50, 60),
                new BuildingPlacement(BuildingKind.Trailhead, 70, 80),
                new BuildingPlacement(BuildingKind.Telegraph, 90, 100)
            },
            50,
            50,
            TownProsperity.Prosperous,
            Array.Empty<PathSegment>());

        var dto = TownLayoutMapper.ToDto(layout);

        Assert.NotNull(dto);
        Assert.Equal(50, dto.PlayerSpawnX);
        Assert.Equal(50, dto.PlayerSpawnY);
        Assert.Equal(TownProsperity.Prosperous, dto.Prosperity);
        Assert.Empty(dto.Paths);
        Assert.Equal(5, dto.Buildings.Count);

        AssertBuildingPlacement(dto.Buildings[0], BuildingKind.Store, 10, 20, 8, 10);
        AssertBuildingPlacement(dto.Buildings[1], BuildingKind.Sheriff, 30, 40, 12, 8);
        AssertBuildingPlacement(dto.Buildings[2], BuildingKind.Saloon, 50, 60, 8, 10);
        AssertBuildingPlacement(dto.Buildings[3], BuildingKind.Trailhead, 70, 80, 8, 10);
        AssertBuildingPlacement(dto.Buildings[4], BuildingKind.Telegraph, 90, 100, 8, 10);
    }

    [Fact]
    public void ToDto_MapsEmptyBuildingsList()
    {
        var layout = new TownLayout(
            Array.Empty<BuildingPlacement>(),
            50,
            50,
            TownProsperity.Prosperous,
            Array.Empty<PathSegment>());

        var dto = TownLayoutMapper.ToDto(layout);

        Assert.NotNull(dto);
        Assert.Empty(dto.Buildings);
        Assert.Equal(50, dto.PlayerSpawnX);
        Assert.Equal(50, dto.PlayerSpawnY);
        Assert.Equal(TownProsperity.Prosperous, dto.Prosperity);
        Assert.Empty(dto.Paths);
    }

    [Fact]
    public void ToDto_PreservesBuildingKindEnumValuesForFrontendRouting()
    {
        // The frontend routes clicks based on BuildingKind, so every enum member
        // must survive the domain -> DTO mapping unchanged.
        foreach (var kind in Enum.GetValues<BuildingKind>())
        {
            var layout = new TownLayout(
                new[] { new BuildingPlacement(kind, 0, 0) },
                0,
                0,
                TownProsperity.Prosperous,
                Array.Empty<PathSegment>());

            var dto = TownLayoutMapper.ToDto(layout);

            Assert.NotNull(dto);
            Assert.Equal(kind, dto.Buildings[0].Kind);
        }
    }

    private static void AssertBuildingPlacement(
        BuildingPlacementDto actual,
        BuildingKind expectedKind,
        int expectedX,
        int expectedY,
        int expectedWidth,
        int expectedHeight)
    {
        Assert.Equal(expectedKind, actual.Kind);
        Assert.Equal(expectedX, actual.X);
        Assert.Equal(expectedY, actual.Y);
        Assert.Equal(expectedWidth, actual.Width);
        Assert.Equal(expectedHeight, actual.Height);
    }
}
