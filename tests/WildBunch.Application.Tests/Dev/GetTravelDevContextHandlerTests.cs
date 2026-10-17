using WildBunch.Application.Dev.Models;
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
    public async Task HandleAsync_ReturnsCompleteEncounterContext_WhenJourneyIsInterrupted()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithActiveJourney();
        const string encounterMessage = "A hard-eyed rider blocks the trail.";
        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(
            new JourneyFoeProfile(Speed: 5, FightStrength: 4, MinimumBribe: 8m),
            encounterMessage));
        session.MarkEventsCommitted();
        session.AdvanceJourneyDay();
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetTravelDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetTravelDevContextQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.SessionId);
        Assert.True(result.HasActiveJourney);
        Assert.Equal("Interrupted", result.JourneyStatus);
        Assert.Equal(1, result.DaysTravelled);
        Assert.Equal(session.Journey!.RemainingDays, result.RemainingDays);
        Assert.Equal("foe", result.PendingEncounterKind);
        Assert.Equal(encounterMessage, result.PendingEncounterMessage);
        Assert.Equal(
            new FoeProfileDevDto(5, 4, 8m, "quick", "hard-handed", "pricey"),
            result.PendingFoeProfile);
        Assert.Null(result.PendingDevOverride);
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
    public async Task HandleAsync_ReturnsCustomPendingOverride_WithProfileAndMessage()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithActiveJourney();
        const string encounterMessage = "A rider waits beyond the bend.";
        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(
            new JourneyFoeProfile(Speed: 7, FightStrength: 2, MinimumBribe: 11m),
            encounterMessage));
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetTravelDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetTravelDevContextQuery(session.Id.Value));

        Assert.Equal(
            new DevOverrideDto(
                "Foe",
                new FoeProfileDevDto(7, 2, 11m, "fast", "light-handed", "greedy"),
                encounterMessage),
            result.PendingDevOverride);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyTravelContext_WhenSessionHasNoJourney()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateStartedSession();
        repository.Seed(session);

        var handler = new GetTravelDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetTravelDevContextQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.SessionId);
        Assert.False(result.HasActiveJourney);
        Assert.Null(result.JourneyStatus);
        Assert.Null(result.DaysTravelled);
        Assert.Null(result.RemainingDays);
        Assert.Null(result.PendingEncounterKind);
        Assert.Null(result.PendingEncounterMessage);
        Assert.Null(result.PendingFoeProfile);
        Assert.Null(result.PendingDevOverride);
    }

    private static GameSession CreateSessionWithActiveJourney()
    {
        var session = CreateStartedSession();
        var pinecross = session.Player.CurrentTownId!.Value;
        var dryfork = new TownId("dryfork");

        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(
            session.World, pinecross, dryfork,
            session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        session.MarkEventsCommitted();
        return session;
    }

    private static GameSession CreateStartedSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork");
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
        return session;
    }
}
