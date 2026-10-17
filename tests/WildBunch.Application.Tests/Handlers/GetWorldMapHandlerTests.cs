using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Handlers;

public sealed class GetWorldMapHandlerTests
{
    [Fact]
    public async Task ProjectsEveryTownAndTrailMapFieldFromTheLoadedWorld()
    {
        var (handler, session) = CreateHandlerWithExplicitWorld();
        var result = await handler.HandleAsync(new GetWorldMapQuery(session.Id.Value));

        Assert.Equal(
            new[]
            {
                new WorldMapTownDto("map-town-red-mesa", "Red Mesa", 17, 39),
                new WorldMapTownDto("map-town-dry-creek", "Dry Creek", 83, -11),
                new WorldMapTownDto("map-town-riverbend", "Riverbend", -26, 104)
            },
            result.Towns);
        Assert.Equal(
            new[]
            {
                new WorldMapTrailDto("trail-red-mesa-dry-creek", "map-town-red-mesa", "map-town-dry-creek", 3.75m),
                new WorldMapTrailDto("trail-riverbend-red-mesa", "map-town-riverbend", "map-town-red-mesa", 4.25m)
            },
            result.Trails);
    }

    [Fact]
    public async Task ThrowsForMissingSession()
    {
        var repo = new InMemoryGameSessionRepository();
        var handler = new GetWorldMapHandler(repo);
        await Assert.ThrowsAsync<GameSessionNotFoundException>(() =>
            handler.HandleAsync(new GetWorldMapQuery(Guid.NewGuid())));
    }

    private static (GetWorldMapHandler Handler, GameSession Session) CreateHandlerWithExplicitWorld()
    {
        var repo = new InMemoryGameSessionRepository();
        var redMesa = new Town(new TownId("map-town-red-mesa"), "Red Mesa", MapX: 17, MapY: 39);
        var dryCreek = new Town(new TownId("map-town-dry-creek"), "Dry Creek", MapX: 83, MapY: -11);
        var riverbend = new Town(new TownId("map-town-riverbend"), "Riverbend", MapX: -26, MapY: 104);
        var world = new World(
            new[] { redMesa, dryCreek, riverbend },
            new[]
            {
                new Trail(new TrailId("trail-red-mesa-dry-creek"), redMesa.Id, dryCreek.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 3.75m),
                new Trail(new TrailId("trail-riverbend-red-mesa"), riverbend.Id, redMesa.Id, TrailRisk.Low, TrailTerrain.Hills, WaterFeature.Spring, 4.25m)
            });
        var suspects = new[]
        {
            new Suspect(
                new SuspectId("hidden-culprit"),
                "Hidden Culprit",
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
                SuspectStatus.AtLarge)
        };
        var caseFile = new CaseFile(null, suspects, new SuspectId("hidden-culprit"), Array.Empty<Clue>());
        var session = GameSession.StartSetup(
            "Test Player",
            world,
            caseFile,
            GameDifficulty.Standard,
            GameEntropy.Boring,
            "f5505e33-f2dd-4c4e-8fd2-f64ff891a653",
            SaltSource.CreateFixed("n53-map-salt"));
        repo.Seed(session);
        IGameSessionReadRepository readRepository = repo;
        return (new GetWorldMapHandler(readRepository), session);
    }
}
