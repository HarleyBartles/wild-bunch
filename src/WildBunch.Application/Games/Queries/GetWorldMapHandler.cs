using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Application.Games.Queries;

public sealed class GetWorldMapHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public GetWorldMapHandler(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<WorldMapDto> HandleAsync(GetWorldMapQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new GameSessionId(query.SessionId);
        var session = await _gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        var towns = SeedWorldMapLayout.GetMapTowns(session.World)
            .Select(town => new WorldMapTownDto(
                town.Id,
                town.Name,
                town.X,
                town.Y))
            .ToArray();

        var trails = SeedWorldMapLayout.GetMapTrails(session.World)
            .Select(trail => new WorldMapTrailDto(
                trail.Id,
                trail.FromTownId,
                trail.ToTownId,
                trail.RideDayDistance))
            .ToArray();

        return new WorldMapDto(towns, trails);
    }
}
