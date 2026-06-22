using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Commands;

public sealed class TravelToTownHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameSessionUnitOfWork _gameSessionUnitOfWork;
    private readonly TravelResolver _travelResolver;

    public TravelToTownHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        TravelResolver travelResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _gameSessionUnitOfWork = gameSessionUnitOfWork;
        _travelResolver = travelResolver;
    }

    public async Task<GameTurnResultDto> HandleAsync(TravelToTownCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await _gameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var destinationTownId = new TownId(command.DestinationTownId);
        var previewResult = _travelResolver.PreviewJourney(
            session.World,
            session.Player.CurrentTownId,
            destinationTownId,
            session.Player.Inventory,
            session.TravelRules);

        if (previewResult.Success && previewResult.Preview is not null)
        {
            var startResult = session.StartJourney(previewResult.Preview);

            await _gameSessionRepository.StoreAsync(session, cancellationToken).ConfigureAwait(false);
            await _gameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

            return GameTurnResultFactory.Create(
                startResult.Success,
                startResult.Message,
                session,
                startResult.Status,
                startResult.Journey);
        }

        return GameTurnResultFactory.Create(
            previewResult.Success,
            previewResult.Message,
            session);
    }
}
