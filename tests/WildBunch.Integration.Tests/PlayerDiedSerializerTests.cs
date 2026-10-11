using WildBunch.Domain.Events;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Integration.Tests;

public sealed class PlayerDiedSerializerTests
{
    [Fact]
    public void PlayerDied_RoundTripsThroughEventSerializer()
    {
        var serializer = new GameSessionJsonSerializer();
        var death = new PlayerDied
        {
            JourneySequence = 3,
            DiaryMessage = "I died on the trail."
        };

        var json = serializer.SerializeEvent(death);
        var reloaded = Assert.IsType<PlayerDied>(serializer.DeserializeEvent(nameof(PlayerDied), json));

        Assert.Equal(death.JourneySequence, reloaded.JourneySequence);
        Assert.Equal(death.DiaryMessage, reloaded.DiaryMessage);
    }
}
