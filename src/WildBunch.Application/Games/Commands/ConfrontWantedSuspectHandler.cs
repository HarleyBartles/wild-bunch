using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

public sealed class ConfrontWantedSuspectHandler : GameSessionCommandHandler
{
    public ConfrontWantedSuspectHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task<WantedSuspectConfrontationResultDto> HandleAsync(
        ConfrontWantedSuspectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var result = session.ResolveWantedSuspectConfrontation(new SuspectId(command.TargetSuspectId), command.Choice);

            return new WantedSuspectConfrontationResultDto(
                result.Success,
                result.Message,
                result.Outcome,
                GameSessionMapper.ToDto(session),
                result.DeclaredWantedIdentityHandle,
                result.TargetName,
                result.Disposition,
                result.IsAlive,
                result.IsSecured,
                result.SessionChanged);
        }, cancellationToken).ConfigureAwait(false);
    }
}
