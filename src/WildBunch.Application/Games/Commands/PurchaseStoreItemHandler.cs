using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Commands;

public sealed class PurchaseStoreItemHandler : GameSessionCommandHandler
{
    private readonly TownStoreCatalogResolver _storeCatalogResolver;

    public PurchaseStoreItemHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        TownStoreCatalogResolver storeCatalogResolver)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _storeCatalogResolver = storeCatalogResolver;
    }

    public async Task<GameTurnResultDto> HandleAsync(
        PurchaseStoreItemCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
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

            return GameTurnResultFactory.Create(
                purchaseResult.Success,
                purchaseResult.Message,
                session);
        }, cancellationToken).ConfigureAwait(false);
    }
}
