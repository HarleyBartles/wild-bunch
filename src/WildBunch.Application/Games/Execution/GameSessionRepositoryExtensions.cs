using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Execution;

public static class GameSessionRepositoryExtensions
{
    public static async Task<GameSession> LoadRequiredAsync(
        this IGameSessionRepository gameSessionRepository,
        GameSessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameSessionRepository);

        var session = await gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        return session ?? throw new GameSessionNotFoundException(sessionId);
    }

    public static async Task SaveIfAsync(
        this IGameSessionRepository gameSessionRepository,
        GameSession session,
        bool shouldSave,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameSessionRepository);
        ArgumentNullException.ThrowIfNull(session);

        if (!shouldSave)
        {
            return;
        }

        await gameSessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
    }
}
