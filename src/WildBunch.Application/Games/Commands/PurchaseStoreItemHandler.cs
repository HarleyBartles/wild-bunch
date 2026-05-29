using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Economy;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Commands;

public sealed class PurchaseStoreItemHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly TownStoreCatalogResolver _storeCatalogResolver;

    public PurchaseStoreItemHandler(
        IGameSessionRepository gameSessionRepository,
        TownStoreCatalogResolver storeCatalogResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _storeCatalogResolver = storeCatalogResolver;
    }

    public async Task<GameTurnResultDto> HandleAsync(
        PurchaseStoreItemCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);

        var townId = new TownId(command.TownId);
        if (!session.World.TryGetTown(townId, out var town))
        {
            throw new TownNotFoundException(townId);
        }

        if (session.Player.CurrentTownId != townId)
        {
            return new GameTurnResultDto(
                false,
                "You must be in that town to buy there.",
                GameSessionMapper.ToDto(session));
        }

        var catalog = _storeCatalogResolver.Resolve(town!);
        var offer = catalog.Offers.FirstOrDefault(candidate =>
            command.VendorType.HasValue
            && command.ItemKind.HasValue
            && candidate.VendorType == command.VendorType.Value
            && candidate.ItemKind == command.ItemKind.Value);

        if (offer is null || offer.Availability != StoreOfferAvailability.Available)
        {
            return new GameTurnResultDto(
                false,
                "That store offer is not available in this town.",
                GameSessionMapper.ToDto(session));
        }

        if (command.Quantity < 1)
        {
            return new GameTurnResultDto(
                false,
                "Quantity must be at least 1.",
                GameSessionMapper.ToDto(session));
        }

        var purchaseResult = session.Purchase(offer, command.Quantity);
        if (purchaseResult.Success)
        {
            await _gameSessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        }

        return new GameTurnResultDto(
            purchaseResult.Success,
            purchaseResult.Message,
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
