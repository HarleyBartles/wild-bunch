using WildBunch.Application.Games.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests;

public sealed class TravelToTownHandlerTests
{
    [Fact]
    public async Task TravelToConnectedTownSucceedsSavesAndReturnsUpdatedState()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new TravelToTownHandler(repository, new TravelResolver());

        var result = await handler.HandleAsync(new TravelToTownCommand(session.Id.Value, "silvercreek"));

        Assert.True(result.Success);
        Assert.Equal("You set out from Dustvale toward Silver Creek.", result.Message);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal("dustvale", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(0, result.CurrentSession.Clock.Turn);
        Assert.NotNull(result.CurrentSession.Journey);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Active, result.JourneyStatus);
    }

    [Fact]
    public async Task TravelToUnconnectedTownFailsAndDoesNotSave()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new TravelToTownHandler(repository, new TravelResolver());

        var result = await handler.HandleAsync(new TravelToTownCommand(session.Id.Value, "dryridge"));

        Assert.False(result.Success);
        Assert.Equal("No trail connects those towns.", result.Message);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Equal("dustvale", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(0, result.CurrentSession.Clock.Turn);
        Assert.Equal(0, result.CurrentSession.PursuitState.Heat);
    }

    [Fact]
    public async Task TravelSucceedsWithEmptyInventoryAndDoesNotChangeWallet()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(emptyInventory: true);
        repository.Seed(session);
        var handler = new TravelToTownHandler(repository, new TravelResolver());

        var result = await handler.HandleAsync(new TravelToTownCommand(session.Id.Value, "silvercreek"));

        Assert.True(result.Success);
        Assert.Equal("You set out from Dustvale toward Silver Creek.", result.Message);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal("dustvale", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(0, result.CurrentSession.Clock.Turn);
        Assert.NotNull(result.CurrentSession.Journey);
    }

    private static GameSession CreateSession(bool emptyInventory = false)
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge", TownServices.None);

        var world = new World(
            new[] { dustvale, silvercreek, dryridge },
            new[]
            {
                new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = emptyInventory
            ? DomainInventory.Empty()
            : new DomainInventory(new[]
            {
                new DomainInventoryItem(DomainItemKind.Food, 1),
                new DomainInventoryItem(DomainItemKind.Canteen, 1)
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, dustvale.Id, Wallet.Starting(25m), inventory);
    }
}
