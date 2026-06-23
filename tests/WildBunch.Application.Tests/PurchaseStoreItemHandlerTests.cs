using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using System.Linq;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Application.Tests;

public sealed class PurchaseStoreItemHandlerTests
{
    [Fact]
    public async Task PurchaseCurrentTownOfferSucceedsSavesOnceAndReturnsUpdatedState()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new PurchaseStoreItemHandler(repository, repository, new TownStoreCatalogResolver(),
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new PurchaseStoreItemCommand(
            session.Id.Value,
            "pinecross",
            StoreVendorType.GeneralStore,
            DomainItemKind.Food,
            2));

        Assert.True(result.Success);
        Assert.Equal("Purchased 2 Food for $4.00.", result.Message);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal(21m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(2, result.CurrentSession.Inventory.Items.Count);
        Assert.Equal(3, result.CurrentSession.Inventory.Items.Single(item => item.Kind == DomainItemKind.Food).Quantity);
        Assert.Equal(2, result.CurrentSession.LogEntries.Count);
    }

    [Fact]
    public async Task PurchaseTownMismatchFailsWithoutSaveOrMutation()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new PurchaseStoreItemHandler(repository, repository, new TownStoreCatalogResolver(),
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new PurchaseStoreItemCommand(
            session.Id.Value,
            "redmesa",
            StoreVendorType.GeneralStore,
            DomainItemKind.Food,
            1));

        Assert.False(result.Success);
        Assert.Equal("You must be in that town to buy there.", result.Message);
        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
        Assert.Equal("pinecross", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Single(result.CurrentSession.LogEntries);
    }

    [Fact]
    public async Task PurchaseUnknownOfferFailsWithoutSaveOrMutation()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new PurchaseStoreItemHandler(repository, repository, new TownStoreCatalogResolver(),
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new PurchaseStoreItemCommand(
            session.Id.Value,
            "pinecross",
            StoreVendorType.Gunsmith,
            DomainItemKind.RifleAmmo,
            1));

        Assert.False(result.Success);
        Assert.Equal("That store offer is not available in this town.", result.Message);
        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Single(result.CurrentSession.LogEntries);
    }

    [Fact]
    public async Task PurchaseUnknownTownThrowsWithoutMutation()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new PurchaseStoreItemHandler(repository, repository, new TownStoreCatalogResolver(),
            new HudProjector(), new DiaryProjector());

        await Assert.ThrowsAsync<TownNotFoundException>(() => handler.HandleAsync(new PurchaseStoreItemCommand(
            session.Id.Value,
            "missing-town",
            StoreVendorType.GeneralStore,
            DomainItemKind.Food,
            1)));

        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Single(session.LogEntries);
    }

    [Fact]
    public async Task PurchaseWhileJourneyIsActiveReturnsFailureWithoutSaving()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        StartJourney(session);
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new PurchaseStoreItemHandler(repository, repository, new TownStoreCatalogResolver(),
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new PurchaseStoreItemCommand(
            session.Id.Value,
            "pinecross",
            StoreVendorType.GeneralStore,
            DomainItemKind.Food,
            1));

        Assert.False(result.Success);
        Assert.Equal("Finish the current journey before taking that action.", result.Message);
        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.NotNull(result.CurrentSession.Journey);
        Assert.Equal(JourneyStatus.Active, result.CurrentSession.Journey!.Status);
        Assert.Equal(2, result.CurrentSession.LogEntries.Count);
    }

    [Fact]
    public async Task PurchaseReturnsDtoWithHudAndDiaryProjections()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new PurchaseStoreItemHandler(repository, repository, new TownStoreCatalogResolver(),
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new PurchaseStoreItemCommand(
            session.Id.Value,
            "pinecross",
            StoreVendorType.GeneralStore,
            DomainItemKind.Food,
            2));

        Assert.True(result.Success);
        Assert.NotNull(result.CurrentSession.HudProjection);
        Assert.Equal(21m, result.CurrentSession.HudProjection!.WalletCash);
        Assert.NotNull(result.CurrentSession.DiaryProjection);
        Assert.NotEmpty(result.CurrentSession.DiaryProjection!.Entries);
        Assert.Equal(session.Id.Value, result.CurrentSession.HudProjection.SessionId);
        Assert.Equal(session.Id.Value, result.CurrentSession.DiaryProjection.SessionId);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        var world = new World(
            new[] { pinecross, redmesa },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        }));
    }

    private static void StartJourney(GameSession session)
    {
        var travelResolver = new TravelResolver();
        var preview = travelResolver.PreviewJourney(
                session.World,
                session.Player.CurrentTownId,
                new TownId("redmesa"),
                session.Player.Inventory)
            .Preview!;

        session.StartJourney(preview);
    }
}
