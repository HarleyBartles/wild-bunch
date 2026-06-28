using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Dev;

public sealed class ForceTravelOverrideHandlerTests
{
    [Fact]
    public async Task HandleAsync_ForcesFoeOverride_WithFoeProfile()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithActiveJourney();
        repository.Seed(session);

        var handler = new ForceTravelOverrideHandler(repository, repository);

        await handler.HandleAsync(new ForceTravelOverrideCommand(
            session.Id.Value,
            ForcedCategory: "Foe",
            FoeSpeed: 5,
            FoeFightStrength: 4,
            FoeMinimumBribe: 8m,
            EncounterMessage: "A hard-eyed rider blocks the trail."));

        Assert.Equal(1, repository.StoreCalls);
        // Verify the override was persisted by reloading
        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded!.PendingDevTravelOverride);
        Assert.Equal(TravelDayEncounterCategory.Foe, reloaded.PendingDevTravelOverride!.ForcedCategory);
        Assert.Equal(5, reloaded.PendingDevTravelOverride.FoeProfile!.Speed);
    }

    [Fact]
    public async Task HandleAsync_ForcesCategoryOnly_WhenNoFoeProfileProvided()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithActiveJourney();
        repository.Seed(session);

        var handler = new ForceTravelOverrideHandler(repository, repository);

        await handler.HandleAsync(new ForceTravelOverrideCommand(
            session.Id.Value,
            ForcedCategory: "Lucky",
            FoeSpeed: null,
            FoeFightStrength: null,
            FoeMinimumBribe: null,
            EncounterMessage: null));

        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded!.PendingDevTravelOverride);
        Assert.Equal(TravelDayEncounterCategory.Lucky, reloaded.PendingDevTravelOverride!.ForcedCategory);
        Assert.Null(reloaded.PendingDevTravelOverride.FoeProfile);
    }

    private static GameSession CreateSessionWithActiveJourney()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross",
            TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id,
            Wallet.Starting(25m), inventory, GameDifficulty.Easy,
            SaltSource.CreateFixed(string.Empty));
        session.MarkEventsCommitted();

        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(
            session.World, session.Player.CurrentTownId, dryfork.Id,
            session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        session.MarkEventsCommitted();
        return session;
    }
}
