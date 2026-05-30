using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;

namespace WildBunch.Application.Games.Commands;

public sealed class ResolveJourneyEncounterHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public ResolveJourneyEncounterHandler(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<GameTurnResultDto> HandleAsync(ResolveJourneyEncounterCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await _gameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var result = session.ResolveJourneyEncounter(command.ChoiceId);

        await _gameSessionRepository.SaveIfAsync(session, result.SessionChanged, cancellationToken).ConfigureAwait(false);

        return GameTurnResultFactory.Create(
            result.Success,
            result.Message,
            session,
            result.Status,
            result.Journey);
    }
}
