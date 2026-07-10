using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Application.Dev.Queries;

/// <summary>
/// Handler for GetTownLayoutSaltsQuery. Returns the current dev layout salts
/// from the game session, or null values if no dev salts are set.
/// </summary>
public sealed class GetTownLayoutSaltsHandler
{
    private const string ResolverVersion = "1.0.0";
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

        var devSalts = session.DevLayoutSalts;
        
        // Return null values when no dev salts are set, to distinguish from actual dev salts
        return new TownLayoutSaltsDto(
            ResolverVersion,
            devSalts?.BuildingsSalt,
            devSalts?.RoadsSalt,
            devSalts?.DirtSalt,
            devSalts?.PropsSalt);
    }
}
