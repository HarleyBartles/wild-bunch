using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Application.Games.Queries;

public sealed class GetStartingTownMapHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public GetStartingTownMapHandler(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<StartingTownMapDto> HandleAsync(GetStartingTownMapQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new GameSessionId(query.SessionId);
        var session = await _gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        // The world already carries the coordinates from the map generation pipeline.
        // No layout palette lookup is needed.
        var towns = SeedWorldMapLayout.GetMapTowns(session.World)
            .Select(town => new StartingTownMapTownDto(
                town.Id,
                town.Name,
                town.Services,
                town.X,
                town.Y))
            .ToArray();

        var trails = SeedWorldMapLayout.GetMapTrails(session.World)
            .Select(trail => new StartingTownMapTrailDto(
                trail.Id,
                trail.FromTownId,
                trail.ToTownId,
                trail.RideDayDistance))
            .ToArray();

        return new StartingTownMapDto(towns, trails);
    }
}
