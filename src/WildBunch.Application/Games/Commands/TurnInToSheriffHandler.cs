using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Commands;

public sealed class TurnInToSheriffHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public TurnInToSheriffHandler(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<SheriffTurnInResultDto> HandleAsync(
        TurnInToSheriffCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await _gameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var result = session.AssessSheriffTurnIn(new SuspectId(command.TargetSuspectId), command.IsAlive);

        return new SheriffTurnInResultDto(
            result.Success,
            result.Message,
            result.Outcome,
            GameSessionMapper.ToDto(session),
            result.TargetName,
            result.Disposition,
            result.BountyAmount);
    }
}
