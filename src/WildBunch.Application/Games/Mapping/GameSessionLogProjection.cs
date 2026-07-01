using WildBunch.Application.Projections;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Mapping;

/// <summary>
/// Projects log entries from a GameSession's full event stream (committed + uncommitted)
/// via JournalLogProjector. This is the projection-backed replacement for the legacy
/// aggregate log entries. See ADR-0028 and BUNCH-86.
/// </summary>
public static class GameSessionLogProjection
{
    private static readonly JournalLogProjector _projector = new();

    /// <summary>
    /// Projects log entries from the session's full event stream (AllEvents).
    /// </summary>
    public static IReadOnlyList<GameLogEntry> Project(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return _projector.Project(session.AllEvents);
    }
}
