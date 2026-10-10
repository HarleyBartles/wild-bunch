using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Economy;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Queries;

public sealed class GetTownStoreOffersHandler
{
    private readonly IGameSessionReadRepository _gameSessionReadRepository;
    private readonly TownStoreCatalogResolver _storeCatalogResolver;

    public GetTownStoreOffersHandler(
        IGameSessionReadRepository gameSessionReadRepository,
        TownStoreCatalogResolver storeCatalogResolver)
    {
        _gameSessionReadRepository = gameSessionReadRepository;
        _storeCatalogResolver = storeCatalogResolver;
    }

    public async Task<TownStoreOffersDto> HandleAsync(
        GetTownStoreOffersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(query.GameSessionId);
        var session = await _gameSessionReadRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new GameSessionNotFoundException(sessionId);

        var townId = new TownId(query.TownId);
        if (!session.World.TryGetTown(townId, out var town))
        {
            throw new TownNotFoundException(townId);
        }

        var catalog = _storeCatalogResolver.Resolve(town!);
        return StoreCatalogMapper.ToDto(catalog);
    }
}
