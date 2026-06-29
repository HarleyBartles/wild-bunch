using WildBunch.Application.Games.Commands;
using WildBunch.Application.Projections;
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
    private static readonly SaltSource DeterministicSaltSource = SaltSource.CreateFixed(string.Empty);

    [Fact]
    public async Task TravelToConnectedTownSucceedsSavesAndReturnsUpdatedState()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new TravelToTownHandler(repository, repository, new TravelResolver(),
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new TravelToTownCommand(session.Id.Value, "silvercreek"));

        Assert.True(result.Success);
        Assert.Contains("set out", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal("dustvale", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(1, result.CurrentSession.Clock.Day);
        Assert.Equal(0, result.CurrentSession.Clock.Turn);
        Assert.NotNull(result.CurrentSession.Journey);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Active, result.JourneyStatus);
        Assert.NotNull(result.Journey);
        Assert.Equal("dustvale", result.Journey!.OriginTownId);
        Assert.Equal("silvercreek", result.Journey.DestinationTownId);
        Assert.Equal(result.CurrentSession.Journey!.TravelMode, result.Journey.TravelMode);
        Assert.Equal(result.CurrentSession.Journey.RouteProfile.TrailId, result.Journey.RouteProfile.TrailId);
        Assert.Equal(2, result.CurrentSession.Journey.RemainingDays);
        Assert.Equal(0, result.CurrentSession.Journey.DaysTravelled);
    }

    [Fact]
    public async Task TravelToUnconnectedTownFailsAndDoesNotSave()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new TravelToTownHandler(repository, repository, new TravelResolver(),
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new TravelToTownCommand(session.Id.Value, "dryridge"));

        Assert.False(result.Success);
        Assert.Equal("No trail connects those towns.", result.Message);
        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
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
        var handler = new TravelToTownHandler(repository, repository, new TravelResolver(),
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new TravelToTownCommand(session.Id.Value, "silvercreek"));

        Assert.True(result.Success);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Active, result.JourneyStatus);
        Assert.Equal("dustvale", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(1, result.CurrentSession.Clock.Day);
        Assert.Equal(0, result.CurrentSession.Clock.Turn);
        Assert.NotNull(result.CurrentSession.Journey);
    }

    private static GameSession CreateSession(bool emptyInventory = false)
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.None);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.None);
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge", TownServices.None);

        var world = new World(
            new[] { dustvale, silvercreek, dryridge },
            new[]
            {
                new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = emptyInventory
            ? DomainInventory.Empty()
            : new DomainInventory(new[]
            {
                new DomainInventoryItem(DomainItemKind.Food, 1),
                new DomainInventoryItem(DomainItemKind.Canteen, 1)
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, dustvale.Id, Wallet.Starting(25m), inventory, saltSource: DeterministicSaltSource);
    }
}




