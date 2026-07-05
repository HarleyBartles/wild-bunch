using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;

namespace WildBunch.Application.Tests.Games.Mapping;

public sealed class GameSessionMapperTests
{
    [Fact]
    public void ToDto_World_MapsTownLayoutIntoTownDtoLayout()
    {
        var layout = new TownLayout(
            new[]
            {
                new BuildingPlacement(BuildingKind.Store, 10, 20),
                new BuildingPlacement(BuildingKind.Sheriff, 30, 40, 70, 60),
                new BuildingPlacement(BuildingKind.Saloon, 50, 60),
                new BuildingPlacement(BuildingKind.Trailhead, 70, 80),
                new BuildingPlacement(BuildingKind.Telegraph, 90, 100)
            },
            400,
            250);

        var townWithLayout = new Town(
            new TownId("town-with-layout"),
            "Dodge City",
            TownServices.Telegraph,
            MapX: 100,
            MapY: 200,
            Layout: layout);

        var townWithoutLayout = new Town(
            new TownId("town-no-layout"),
            "Tombstone",
            TownServices.None,
            MapX: 50,
            MapY: 75);

        var world = new DomainWorld(
            new[] { townWithLayout, townWithoutLayout },
            Array.Empty<Trail>());

        // GameSessionMapper.ToDto(DomainWorld world) is the public path that
        // maps each town via the private ToDto(DomainTown town). We assert on
        // the resulting WorldDto to verify the layout rides the read path.
        var worldDto = MapWorld(world);

        var withLayout = Assert.Single(worldDto.Towns, t => t.Id == "town-with-layout");
        Assert.NotNull(withLayout.Layout);
        Assert.Equal(400, withLayout.Layout!.PlayerSpawnX);
        Assert.Equal(250, withLayout.Layout.PlayerSpawnY);
        Assert.Equal(5, withLayout.Layout.Buildings.Count);
        Assert.Equal(BuildingKind.Store, withLayout.Layout.Buildings[0].Kind);
        Assert.Equal(10, withLayout.Layout.Buildings[0].X);
        Assert.Equal(20, withLayout.Layout.Buildings[0].Y);
        Assert.Equal(60, withLayout.Layout.Buildings[0].Width);
        Assert.Equal(50, withLayout.Layout.Buildings[0].Height);
        Assert.Equal(BuildingKind.Sheriff, withLayout.Layout.Buildings[1].Kind);
        Assert.Equal(70, withLayout.Layout.Buildings[1].Width);
        Assert.Equal(60, withLayout.Layout.Buildings[1].Height);
        Assert.Equal(BuildingKind.Telegraph, withLayout.Layout.Buildings[4].Kind);

        var withoutLayout = Assert.Single(worldDto.Towns, t => t.Id == "town-no-layout");
        Assert.Null(withoutLayout.Layout);
    }

    [Fact]
    public void TownDto_Layout_IsOptionalAndDefaultsToNull()
    {
        var townDto = new TownDto("town-1", "Dodge City", TownServices.None, 0, 0);

        Assert.Null(townDto.Layout);
    }

    /// <summary>
    /// GameSessionMapper.ToDto(DomainWorld) is internal, exposed to tests via
    /// InternalsVisibleTo. This lets us exercise the town-level mapping without
    /// standing up a full GameSession.
    /// </summary>
    private static WorldDto MapWorld(DomainWorld world)
        => GameSessionMapper.ToDto(world);
}
