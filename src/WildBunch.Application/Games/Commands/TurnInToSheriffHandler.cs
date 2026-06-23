using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

public sealed class TurnInToSheriffHandler : GameSessionCommandHandler
{
    public TurnInToSheriffHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task<SheriffTurnInResultDto> HandleAsync(
        TurnInToSheriffCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var result = session.SettleSheriffTurnIn(new SuspectId(command.TargetSuspectId), command.IsAlive);

            return new SheriffTurnInResultDto(
                result.Success,
                result.Message,
                result.Outcome,
                GameSessionMapper.ToDto(session),
                result.TargetName,
                result.Disposition,
                result.BountyAmount);
        }, cancellationToken).ConfigureAwait(false);
    }
}
