using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Queries;

public sealed class PreviewTravelHandler
{
    private readonly IGameSessionReadRepository _gameSessionReadRepository;
    private readonly TravelResolver _travelResolver;

    public PreviewTravelHandler(IGameSessionReadRepository gameSessionReadRepository, TravelResolver travelResolver)
    {
        _gameSessionReadRepository = gameSessionReadRepository;
        _travelResolver = travelResolver;
    }

    public async Task<TravelPreviewResultDto> HandleAsync(PreviewTravelQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(query.GameSessionId);
        var session = await _gameSessionReadRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new GameSessionNotFoundException(sessionId);

        if (session.StartFlowPhase < StartFlowPhase.GameStarted)
        {
            return new TravelPreviewResultDto(false, "The game hasn't started yet.", null);
        }

        var destinationTownId = new TownId(query.DestinationTownId);
        var previewResult = _travelResolver.PreviewJourney(
            session.World,
            session.Player.CurrentTownId!.Value,
            destinationTownId,
            session.Player.Inventory,
            TravelRulesProfile.For(session.GameDifficulty));

        return new TravelPreviewResultDto(
            previewResult.Success,
            previewResult.Message,
            previewResult.Preview is null ? null : TravelMapper.ToDto(previewResult.Preview));
    }
}
