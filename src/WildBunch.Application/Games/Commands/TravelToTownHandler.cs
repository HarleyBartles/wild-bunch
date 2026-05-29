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
        var travelResult = _travelResolver.Travel(session.World, session, destinationTownId);

        if (travelResult.Success)
        {
            await _gameSessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        }

        return new GameTurnResultDto(
            travelResult.Success,
            travelResult.Message,
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
