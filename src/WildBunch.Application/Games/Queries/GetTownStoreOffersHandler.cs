using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Economy;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Queries;

public sealed class GetTownStoreOffersHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly TownStoreCatalogResolver _storeCatalogResolver;

    public GetTownStoreOffersHandler(
        IGameSessionRepository gameSessionRepository,
        TownStoreCatalogResolver storeCatalogResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _storeCatalogResolver = storeCatalogResolver;
    }

    public async Task<TownStoreOffersDto> HandleAsync(
        GetTownStoreOffersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(query.GameSessionId);
        var session = await _gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        var townId = new TownId(query.TownId);
        if (!session.World.TryGetTown(townId, out var town))
        {
            throw new TownNotFoundException(townId);
        }

        var catalog = _storeCatalogResolver.Resolve(town!);
        return StoreCatalogMapper.ToDto(catalog);
    }
}
