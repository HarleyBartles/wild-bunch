using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Commands;

public sealed class TravelToTownHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly TravelResolver _travelResolver;

    public TravelToTownHandler(IGameSessionRepository gameSessionRepository, TravelResolver travelResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _travelResolver = travelResolver;
    }

    public async Task<GameTurnResultDto> HandleAsync(TravelToTownCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var destinationTownId = new TownId(command.DestinationTownId);
        var previewResult = _travelResolver.PreviewJourney(
            session.World,
            session.Player.CurrentTownId,
            destinationTownId,
            session.Player.Inventory,
            session.TravelRules);

        if (previewResult.Success && previewResult.Preview is not null)
        {
            var travelResult = session.StartJourney(previewResult.Preview);
            await _gameSessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);

            return new GameTurnResultDto(
                travelResult.Success,
                travelResult.Message,
                GameSessionMapper.ToDto(session),
                travelResult.Status,
                travelResult.Journey is null ? null : TravelMapper.ToDto(travelResult.Journey),
                null,
                TravelDiaryMapper.ToDto(session.TravelDiaryDays, session.TravelRules));
        }

        return new GameTurnResultDto(
            previewResult.Success,
            previewResult.Message,
            GameSessionMapper.ToDto(session));
    }

    private async Task<WildBunch.Domain.Game.GameSession> LoadSessionAsync(
        WildBunch.Domain.Game.GameSessionId sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        return session ?? throw new GameSessionNotFoundException(sessionId);
    }
}
