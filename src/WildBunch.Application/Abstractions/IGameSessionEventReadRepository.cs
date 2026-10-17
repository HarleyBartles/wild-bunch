using WildBunch.Domain.Events;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Abstractions;

/// <summary>
/// Reads a persisted event stream for a player-facing projection.
/// A null result means the session does not exist; an empty stream means it
/// exists but has not recorded any events.
/// </summary>
public interface IGameSessionEventReadRepository
{
    Task<IReadOnlyList<IDomainEvent>?> GetEventStreamAsync(
        GameSessionId id,
        CancellationToken cancellationToken = default);
}
