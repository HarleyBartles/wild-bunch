using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
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
        var session = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var result = session.ResolveJourneyEncounter(command.ChoiceId);

        if (result.SessionChanged)
        {
            await _gameSessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        }

        return new GameTurnResultDto(
            result.Success,
            result.Message,
            GameSessionMapper.ToDto(session),
            result.Status,
            result.Journey is null ? null : TravelMapper.ToDto(result.Journey),
            null,
            TravelDiaryMapper.ToDto(session.TravelDiaryDays, session.TravelRules));
    }

    private async Task<WildBunch.Domain.Game.GameSession> LoadSessionAsync(
        WildBunch.Domain.Game.GameSessionId sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        return session ?? throw new GameSessionNotFoundException(sessionId);
    }
}
