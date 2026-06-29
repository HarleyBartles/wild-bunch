using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Integration.Tests;

public sealed class DevDifficultyForcedSerializerTests
{
    [Fact]
    public void DevDifficultyForced_RoundTripsThroughEventSerializer()
    {
        var serializer = new GameSessionJsonSerializer();
        var forced = new DevDifficultyForced
        {
            ForcedDifficulty = GameDifficulty.Brutal
        };

        var json = serializer.SerializeEvent(forced);
        var reloaded = serializer.DeserializeEvent(nameof(DevDifficultyForced), json);

        var roundTripped = Assert.IsType<DevDifficultyForced>(reloaded);
        Assert.Equal(GameDifficulty.Brutal, roundTripped.ForcedDifficulty);
    }

    [Fact]
    public void ResolveEventType_KnowsDevDifficultyForced()
    {
        // Proves the ResolveEventType switch maps DevDifficultyForced.
        // Without this mapping, loading a session with a DevDifficultyForced
        // event in its stream throws InvalidOperationException.
        var serializer = new GameSessionJsonSerializer();
        var forced = new DevDifficultyForced
        {
            ForcedDifficulty = GameDifficulty.Challenging
        };

        var json = serializer.SerializeEvent(forced);

        // If ResolveEventType doesn't know the type, this throws.
        var deserialized = serializer.DeserializeEvent("DevDifficultyForced", json);
        Assert.IsType<DevDifficultyForced>(deserialized);
    }
}
