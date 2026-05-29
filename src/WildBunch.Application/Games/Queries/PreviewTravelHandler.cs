using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Queries;

public sealed class PreviewTravelHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly TravelResolver _travelResolver;

    public PreviewTravelHandler(IGameSessionRepository gameSessionRepository, TravelResolver travelResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _travelResolver = travelResolver;
    }

    public async Task<TravelPreviewResultDto> HandleAsync(PreviewTravelQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(query.GameSessionId);
        var session = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var destinationTownId = new TownId(query.DestinationTownId);
        var previewResult = _travelResolver.PreviewJourney(session.World, session.Player.CurrentTownId, destinationTownId, session.Player.Inventory);

        return new TravelPreviewResultDto(
            previewResult.Success,
            previewResult.Message,
            previewResult.Preview is null ? null : TravelMapper.ToDto(previewResult.Preview));
    }

    private async Task<WildBunch.Domain.Game.GameSession> LoadSessionAsync(
        WildBunch.Domain.Game.GameSessionId sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        return session ?? throw new GameSessionNotFoundException(sessionId);
    }
}
