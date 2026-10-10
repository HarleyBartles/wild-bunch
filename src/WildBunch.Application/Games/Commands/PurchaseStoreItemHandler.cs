using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Commands;

public sealed class PurchaseStoreItemHandler : GameSessionCommandHandler
{
    private readonly TownStoreCatalogResolver _storeCatalogResolver;
    private readonly HudProjector _hudProjector;

    public PurchaseStoreItemHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        TownStoreCatalogResolver storeCatalogResolver,
        HudProjector hudProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _storeCatalogResolver = storeCatalogResolver;
        _hudProjector = hudProjector;
    }

    public async Task<GameTurnResultDto> HandleAsync(
        PurchaseStoreItemCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        var result = await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
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
                command.ItemKind.HasValue
                && candidate.ItemKind == command.ItemKind.Value);

            if (offer is null)
            {
                return new GameTurnResultDto(
                    false,
                    "That item is not offered at this store.",
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

        var events = await GameSessionRepository.GetEventStreamAsync(sessionId, 0, cancellationToken)
            .ConfigureAwait(false);
        var hud = _hudProjector.Project(events) with { SessionId = command.GameSessionId };

        return result with
        {
            CurrentSession = result.CurrentSession with
            {
                HudProjection = hud
            }
        };
    }
}
