using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;

namespace WildBunch.Application.Games.Commands;

public sealed class AdvanceTravelDayHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public AdvanceTravelDayHandler(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<GameTurnResultDto> HandleAsync(AdvanceTravelDayCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await _gameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var result = session.AdvanceJourneyDay();

        await _gameSessionRepository.SaveIfAsync(session, result.Success || result.Journey is not null, cancellationToken).ConfigureAwait(false);

        return GameTurnResultFactory.Create(
            result.Success,
            result.Message,
            session,
            result.Status,
            result.Journey,
            result.TrailEvent);
    }
}
