using WildBunch.Domain.Game;

namespace WildBunch.Application.Abstractions;

public interface IGameSessionRepository
{
    Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default);

    Task StoreAsync(GameSession session, CancellationToken cancellationToken = default);

    Task SaveAsync(GameSession session, CancellationToken cancellationToken = default);
}
