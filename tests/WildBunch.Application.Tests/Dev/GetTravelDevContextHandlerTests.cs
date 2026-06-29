using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Dev;

public sealed class GetTravelDevContextHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsJourneyContext_WhenSessionHasActiveJourney()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithActiveJourney();
        repository.Seed(session);

        var handler = new GetTravelDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetTravelDevContextQuery(session.Id.Value));

        Assert.True(result.HasActiveJourney);
        Assert.NotNull(result.JourneyStatus);
    }

    [Fact]
    public async Task HandleAsync_ThrowsWhenSessionDoesNotExist()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new GetTravelDevContextHandler(repository);

        await Assert.ThrowsAsync<GameSessionNotFoundException>(() =>
            handler.HandleAsync(new GetTravelDevContextQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_ReturnsDevOverride_WhenOverrideIsPending()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithActiveJourney();
        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(
            TravelDayEncounterCategory.Foe));
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetTravelDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetTravelDevContextQuery(session.Id.Value));

        Assert.NotNull(result.PendingDevOverride);
        Assert.Equal("Foe", result.PendingDevOverride.ForcedCategory);
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
