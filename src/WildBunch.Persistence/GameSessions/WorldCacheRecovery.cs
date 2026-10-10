using System.Text.Json.Nodes;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.GameSessions;

internal static class WorldCacheRecovery
{
    public static bool MatchesGeneratedWorld(
        IReadOnlyList<IDomainEvent> events,
        World world,
        GameSessionJsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(serializer);

        var generatedWorld = events.OfType<WorldGenerated>().Single();
        var expectedPayload = JsonNode.Parse(serializer.SerializeWorld(generatedWorld.World.ToDomain()));
        var cachedPayload = JsonNode.Parse(serializer.SerializeWorld(world));

        return JsonNode.DeepEquals(expectedPayload, cachedPayload);
    }
}
