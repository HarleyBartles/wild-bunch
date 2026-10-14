using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class WorldMapEndpointTests
{
    [Fact]
    public async Task GetWorldMapReturnsThePersistedWorldMap()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var session = CreateExplicitSession();
        using (var scope = factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
            await repository.StoreAsync(session);
            await unitOfWork.CommitAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            var reloaded = await repository.GetByIdAsync(session.Id);
            Assert.NotNull(reloaded);
            Assert.Equal("hidden-culprit", reloaded!.CaseFile.TrueCulpritId.Value);
        }

        var response = await client.GetAsync($"/api/games/{session.Id.Value}/world-map");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        var map = JsonSerializer.Deserialize<WorldMapDto>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(map);
        Assert.Equal(
            new[]
            {
                new WorldMapTownDto("map-town-dry-creek", "Dry Creek", 83, -11),
                new WorldMapTownDto("map-town-red-mesa", "Red Mesa", 17, 39),
                new WorldMapTownDto("map-town-riverbend", "Riverbend", -26, 104)
            },
            map!.Towns.OrderBy(town => town.Id));
        Assert.Equal(
            new[]
            {
                new WorldMapTrailDto("trail-red-mesa-dry-creek", "map-town-red-mesa", "map-town-dry-creek", 3.75m),
                new WorldMapTrailDto("trail-riverbend-red-mesa", "map-town-riverbend", "map-town-red-mesa", 4.25m)
            },
            map.Trails.OrderBy(trail => trail.Id));

        using var document = JsonDocument.Parse(payload);
        Assert.Equal(new[] { "towns", "trails" }, PropertyNames(document.RootElement));
        foreach (var town in document.RootElement.GetProperty("towns").EnumerateArray())
        {
            Assert.Equal(new[] { "id", "name", "x", "y" }, PropertyNames(town));
        }

        foreach (var trail in document.RootElement.GetProperty("trails").EnumerateArray())
        {
            Assert.Equal(new[] { "fromTownId", "id", "rideDayDistance", "toTownId" }, PropertyNames(trail));
        }
    }

    [Fact]
    public async Task GetWorldMapReturnsNotFoundForMissingSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}/world-map");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string[] PropertyNames(JsonElement element)
    {
        return element.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray();
    }

    private static GameSession CreateExplicitSession()
    {
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

        return GameSession.StartSetup(
            "Test Player",
            world,
            caseFile,
            GameDifficulty.Standard,
            GameEntropy.Boring,
            "f5505e33-f2dd-4c4e-8fd2-f64ff891a653",
            SaltSource.CreateFixed("n53-map-salt"));
    }
}
