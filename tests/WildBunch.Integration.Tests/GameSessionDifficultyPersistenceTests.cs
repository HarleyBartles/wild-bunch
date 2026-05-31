using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using System.Text.Json.Nodes;

namespace WildBunch.Integration.Tests;

public sealed class GameSessionDifficultyPersistenceTests
{
    [Fact]
    public async Task TravelDifficultyRoundTripsThroughJsonPersistence()
    {
        using var fixture = new SqlitePersistenceFixture();
        var repository = new EfGameSessionRepository(fixture.CreateContext(), new GameSessionJsonSerializer());
        var session = CreateEasySession();

        await repository.SaveAsync(session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(TravelDifficulty.Easy, reloaded!.TravelDifficulty);
        Assert.Equal(10, reloaded.Player.Inventory.GetCanteenState()!.Capacity);
        Assert.Equal(10, reloaded.Player.Inventory.GetCanteenState()!.Charges);
    }

    [Fact]
    public void MissingTravelRandomnessInLegacySessionJsonFallsBackToRuntimeSalted()
    {
        var serializer = new GameSessionJsonSerializer();
        var legacySnapshot = JsonNode.Parse(serializer.Serialize(CreateEasySession()))!.AsObject();
        legacySnapshot.Remove("travelRandomness");

        var reloaded = serializer.Deserialize(legacySnapshot.ToJsonString());

        Assert.Equal(TravelRandomnessMode.RuntimeSalted, reloaded.TravelRandomness.Mode);
        Assert.False(string.IsNullOrWhiteSpace(reloaded.TravelRandomness.Salt));
    }

    private static GameSession CreateEasySession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);

        var world = new World(
            new[] { pinecross, holloway },
            new[]
            {
                new Trail(new TrailId("trail-easy"), pinecross.Id, holloway.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 5m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());

        var inventory = new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, new HorseTravelState(3, 2, 3)),
            new InventoryItem(ItemKind.Saddle, 1)
        });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            pinecross.Id,
            Wallet.Starting(25m),
            inventory,
            TravelDifficulty.Easy);
    }
}
