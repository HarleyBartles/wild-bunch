using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Commands;

public sealed class ClearSaloonOverrideHandler : GameSessionCommandHandler
{
    public ClearSaloonOverrideHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ClearSaloonOverrideCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            session.ClearDevSaloonOverride();
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
