using System.Text.Json;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Integration.Tests;

public sealed class TownProsperitySerializerTests
{
    [Fact]
    public void WorldGenerated_StoresEstablishedNumericProsperityValues()
    {
        var eventData = new WorldGenerated
        {
            SeedCode = "seed",
            SaltSource = SaltSource.CreateFixed("fixed"),
            GameEntropy = GameEntropy.Classic,
            World = new WorldSnapshot(
                new[]
                {
                    new TownSnapshot("boomtown", "Boomtown", TownProsperity.Boomtown, 0, 0, false),
                    new TownSnapshot("prosperous", "Prosperous", TownProsperity.Prosperous, 1, 0, false),
                    new TownSnapshot("poor", "Poor", TownProsperity.Poor, 2, 0, false),
                    new TownSnapshot("destitute", "Destitute", TownProsperity.Destitute, 3, 0, false)
                },
                Array.Empty<TrailSnapshot>())
        };

        var json = new GameSessionJsonSerializer().SerializeEvent(eventData);
        using var document = JsonDocument.Parse(json);
        var towns = document.RootElement.GetProperty("world").GetProperty("towns");

        Assert.Equal(4, towns.GetArrayLength());
        for (var index = 0; index < 4; index++)
        {
            Assert.Equal(JsonValueKind.Number, towns[index].GetProperty("prosperity").ValueKind);
            Assert.Equal(index, towns[index].GetProperty("prosperity").GetInt32());
        }
    }
}
