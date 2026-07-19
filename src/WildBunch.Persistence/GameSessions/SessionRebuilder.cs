using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.GameSessions;

/// <summary>
/// Synchronous session rebuild from events. Shared by LoadFromEventsAsync
/// (Plan C) and PersistedPayloadLoader's component rebuild callback.
/// Reconstructs the world from the WorldGenerated event, then calls
/// RehydrateFromEvents. See ADR-0028.
/// </summary>
internal static class SessionRebuilder
{
    /// <summary>
    /// Rebuilds a session from events with a known session id. Used by
    /// LoadFromEventsAsync, which has the id from the load request.
    /// </summary>
    public static GameSession RebuildFromEvents(
        GameSessionId id,
        IReadOnlyList<IDomainEvent> events,
        GameSessionJsonSerializer serializer)
    {
        var worldGenerated = events.OfType<WorldGenerated>().Single();
        var world = worldGenerated.World.ToDomain();
        return GameSession.RehydrateFromEvents(id, world, events);
    }

    /// <summary>
    /// Rebuilds a session from events without a known session id. Used by
    /// PersistedPayloadLoader's component rebuild callback, which only
    /// receives events. The id is not carried by domain events, so a
    /// placeholder is used — this is safe because the rebuilt session is
    /// only used to extract component JSON, and no component includes the
    /// session id in its serialized form.
    /// </summary>
    public static GameSession RebuildFromEvents(
        IReadOnlyList<IDomainEvent> events,
        GameSessionJsonSerializer serializer)
        => RebuildFromEvents(GameSessionId.New(), events, serializer);
}
