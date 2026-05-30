using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;

namespace WildBunch.Application.Games.Commands;

public sealed class AcknowledgeJourneyArrivalHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public AcknowledgeJourneyArrivalHandler(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<GameTurnResultDto> HandleAsync(AcknowledgeJourneyArrivalCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await _gameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var result = session.AcknowledgeJourneyArrival();

        await _gameSessionRepository.SaveIfAsync(session, result.Success, cancellationToken).ConfigureAwait(false);

        return GameTurnResultFactory.Create(
            result.Success,
            result.Message,
            session,
            journeyStatus: null);
    }
}
