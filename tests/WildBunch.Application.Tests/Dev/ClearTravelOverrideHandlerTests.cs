using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Dev;

public sealed class ClearTravelOverrideHandlerTests
{
    [Fact]
    public async Task HandleAsync_ClearsPendingOverride()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithActiveJourney();
        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe));
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new ClearTravelOverrideHandler(repository, repository);

        await handler.HandleAsync(new ClearTravelOverrideCommand(session.Id.Value));

        Assert.Equal(1, repository.StoreCalls);
        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.Null(reloaded!.PendingDevTravelOverride);
    }

    [Fact]
    public async Task HandleAsync_WithNoOverride_StillSucceeds_NoOp()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithActiveJourney();
        repository.Seed(session);

        var handler = new ClearTravelOverrideHandler(repository, repository);

        await handler.HandleAsync(new ClearTravelOverrideCommand(session.Id.Value));

        // No events produced, so no store call
        Assert.Equal(0, repository.StoreCalls);
    }

    private static GameSession CreateSessionWithActiveJourney()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross",
            TownServices.None);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new World(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, dryfork.Id, TrailRisk.Low)
            });

        var caseFile = new CaseFile(
            null,
            Array.Empty<Suspect>(),
            new SuspectId("suspect-1"),
            Array.Empty<Clue>());

        var inventory = new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1)
        });

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Easy, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed(string.Empty));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        session.MarkEventsCommitted();

        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(
            session.World, session.Player.CurrentTownId!.Value, dryfork.Id,
            session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        session.MarkEventsCommitted();
        return session;
    }
}
