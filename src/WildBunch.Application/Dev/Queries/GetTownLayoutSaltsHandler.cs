using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Application.Dev.Queries;

/// <summary>
/// Handler for GetTownLayoutSaltsQuery. Returns the current layout salts
/// from the game session, or defaults if none are set.
/// </summary>
public sealed class GetTownLayoutSaltsHandler
{
    private readonly IGameSessionRepository _repository;

    public GetTownLayoutSaltsHandler(IGameSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TownLayoutSaltsDto> HandleAsync(GetTownLayoutSaltsQuery query, CancellationToken cancellationToken = default)
    {
        var sessionId = new GameSessionId(query.GameId);
        var session = await _repository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        var salts = session.DevLayoutSalts ?? new LayoutSalts(
            "default-buildings",
            "default-roads",
            "default-dirt",
            "default-props");

        return new TownLayoutSaltsDto(
            "1.0.0",
            salts.BuildingsSalt,
            salts.RoadsSalt,
            salts.DirtSalt,
            salts.PropsSalt);
    }
}
