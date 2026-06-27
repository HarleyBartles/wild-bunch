using WildBunch.Domain.Events;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Abstractions;

public interface IGameSessionRepository
{
    Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameSession>> GetByStatusAsync(GameStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages the snapshot upsert and event append on the DbContext.
    /// The UoW commits. No independent SaveChangesAsync here.
    /// Throws ConcurrencyException if the persisted stream version does not match the session's expected version.
    /// </summary>
    Task StoreAsync(GameSession session, Guid? correlationId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the typed domain event stream for a session, optionally starting from a specific version.
    /// Used for replay-from-events and for loading events after a snapshot.
    /// </summary>
    Task<IReadOnlyList<IDomainEvent>> GetEventStreamAsync(GameSessionId id, long fromVersion = 0, CancellationToken cancellationToken = default);
}
