using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Persistence.GameSessions;

/// <summary>
/// Reconstructs a session from ordered domain events by rebuilding its world
/// and calling GameSession.RehydrateFromEvents.
/// </summary>
internal static class SessionRebuilder
{
    /// <summary>
    /// Rebuilds a session when its persisted envelope supplied the session ID.
    /// </summary>
    public static GameSession RebuildFromEvents(
        GameSessionId id,
        IReadOnlyList<IDomainEvent> events)
    {
        var worldGenerated = events.OfType<WorldGenerated>().Single();
        var world = worldGenerated.World.ToDomain();
        return GameSession.RehydrateFromEvents(id, world, events);
    }

    /// <summary>
    /// Rebuilds a session for the component-cache callback when only events are
    /// available. Events do not carry the persistence envelope's session ID.
    /// </summary>
    public static GameSession RebuildForComponentCache(IReadOnlyList<IDomainEvent> events)
        => RebuildFromEvents(GameSessionId.New(), events);
}
