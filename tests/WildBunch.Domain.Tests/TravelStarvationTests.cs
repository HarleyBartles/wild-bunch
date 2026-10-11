using WildBunch.Domain.Cases;
using WildBunch.Domain.Actions;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class TravelStarvationTests
{
    [Fact]
    public void AdvanceJourneyDay_ConsumesLastFoodThenChargesOneTollForNextUnfedDay()
    {
        var (session, preview, _, _) = CreateJourney(food: 1);
        session.StartJourney(preview);

        session.AdvanceJourneyDay();

        var fedDay = Assert.Single(session.UncommittedEvents.OfType<TravelDayAdvanced>());
        Assert.Equal(0, fedDay.HealthDelta);
        Assert.Equal(0, session.Player.GetQuantity(ItemKind.Food));
        Assert.Equal(1250, session.Player.Health);

        session.MarkEventsCommitted();
        session.AdvanceJourneyDay();

        var unfedDay = Assert.Single(session.UncommittedEvents.OfType<TravelDayAdvanced>());
        Assert.Equal(-25, unfedDay.HealthDelta);
        Assert.Equal(1225, session.Player.Health);
    }

    [Fact]
    public void AdvanceJourneyDay_WithoutFood_ChargesTwentyFiveHealthOnce()
    {
        var (session, preview, _, _) = CreateJourney(food: 0);
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        var day = Assert.Single(session.UncommittedEvents.OfType<TravelDayAdvanced>());
        Assert.True(result.Success);
        Assert.Equal(-25, day.HealthDelta);
        Assert.Equal(1225, session.Player.Health);
        Assert.Equal(0, session.Player.GetQuantity(ItemKind.Food));

        session.MarkEventsCommitted();
        session.AdvanceJourneyDay();

        var secondUnfedDay = Assert.Single(session.UncommittedEvents.OfType<TravelDayAdvanced>());
        Assert.Equal(-25, secondUnfedDay.HealthDelta);
        Assert.Equal(1200, session.Player.Health);
    }

    [Fact]
    public void AdvanceJourneyDay_LethalStarvation_StopsTravelAndRecordsDeath()
    {
        var (session, preview, setupEvents, world) = CreateJourney(food: 0);
        setupEvents = setupEvents
            .Select(e => e is GameStarted started ? started with { StartingHealth = 10 } : e)
            .ToArray();
        session = GameSession.RehydrateFromEvents(session.Id, world, setupEvents);
        session.StartJourney(preview);
        var distanceBefore = session.Journey!.RemainingRideDayDistance;

        session.AdvanceJourneyDay();

        var day = Assert.Single(session.UncommittedEvents.OfType<TravelDayAdvanced>());
        Assert.Equal(-10, day.HealthDelta);
        Assert.Empty(day.DiaryMessage);
        Assert.Equal(0, session.Player.Health);
        Assert.Equal(GameStatus.Failed, session.Status);
        Assert.Equal(JourneyStatus.Failed, session.Journey!.Status);
        Assert.Equal(distanceBefore, session.Journey.RemainingRideDayDistance);
        var availableActions = new ActionAvailabilityResolver().Resolve(new ActionAvailabilityContext(
            session.StartFlowPhase,
            world,
            session.Player.CurrentTownId,
            session.Journey.ToSnapshot(session.TravelRules)));
        Assert.Equal(
            new[] { AvailableActionKind.ViewMap, AvailableActionKind.ViewJournal },
            availableActions.Select(action => action.Kind));
        Assert.DoesNotContain(session.UncommittedEvents, e => e is JourneyCompleted);
        Assert.DoesNotContain(session.UncommittedEvents, e => e is TrailEventApplied or JourneyEncounterResolved);
        var fatalEvents = session.UncommittedEvents.ToArray();
        Assert.IsType<TravelDayAdvanced>(fatalEvents[^2]);
        Assert.IsType<PlayerDied>(fatalEvents[^1]);
        Assert.Single(fatalEvents.OfType<PlayerDied>());
        Assert.Equal("I died on the trail. My journey ends here.", Assert.Single(fatalEvents.OfType<PlayerDied>()).DiaryMessage);

        var replay = GameSession.RehydrateFromEvents(
            session.Id,
            world,
            setupEvents.Concat(fatalEvents).ToArray());
        Assert.Equal(0, replay.Player.Health);
        Assert.Equal(GameStatus.Failed, replay.Status);
        Assert.Equal(JourneyStatus.Failed, replay.Journey!.Status);
        Assert.Equal(session.Journey.RemainingRideDayDistance, replay.Journey.RemainingRideDayDistance);
        Assert.Throws<InvalidOperationException>(() => GameSession.RehydrateFromEvents(
            session.Id,
            world,
            setupEvents.Concat(fatalEvents).Append(new PlayerDied { JourneySequence = 1, DiaryMessage = "Duplicate death." }).ToArray()));

        var eventsAfterDeath = session.UncommittedEvents.Count;
        var rejectedAdvance = session.AdvanceJourneyDay();
        Assert.False(rejectedAdvance.Success);
        Assert.Equal(eventsAfterDeath, session.UncommittedEvents.Count);
        Assert.False(session.StartJourney(preview).Success);
        Assert.Equal(eventsAfterDeath, session.UncommittedEvents.Count);
        Assert.Throws<InvalidOperationException>(() => session.ForceDevDifficulty(GameDifficulty.Brutal));
        Assert.Equal(GameStatus.Failed, session.Status);
        Assert.Equal(eventsAfterDeath, session.UncommittedEvents.Count);

        session.ArchivePlaythrough("start-over");
        var archive = Assert.Single(session.UncommittedEvents.OfType<PlaythroughArchived>());
        Assert.Equal(GameStatus.Failed, archive.StatusBeforeArchive);
        Assert.Equal(GameStatus.Archived, session.Status);
    }

    [Theory]
    [InlineData("run", 2)]
    [InlineData("fight", 1)]
    public void ResolveJourneyEncounter_LethalDamage_EndsJourneyWithoutAdvancing(string choice, int startingHealth)
    {
        var (session, preview, setupEvents, world) = CreateJourney(food: 2);
        setupEvents = setupEvents
            .Select(e => e is GameStarted started ? started with { StartingHealth = startingHealth } : e)
            .ToArray();
        session = GameSession.RehydrateFromEvents(session.Id, world, setupEvents);
        session.StartJourney(preview);
        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(
            new JourneyFoeProfile(Speed: 3, FightStrength: 3, MinimumBribe: 5m),
            "A rider blocks the trail."));
        var interruption = session.AdvanceJourneyDay();
        Assert.Equal(JourneyStatus.Interrupted, interruption.Status);

        var result = session.ResolveJourneyEncounter(choice, bulletSpend: null, bribeAmount: null, forcedRoll: 99UL);

        Assert.Equal(JourneyStatus.Failed, result.Status);
        Assert.Equal(0, session.Player.Health);
        Assert.Equal(GameStatus.Failed, session.Status);
        Assert.Equal(JourneyStatus.Failed, session.Journey!.Status);
        var encounterResult = Assert.Single(session.UncommittedEvents.OfType<JourneyEncounterResolved>());
        Assert.Equal(0, encounterResult.PlayerHealth);
        Assert.DoesNotContain(session.UncommittedEvents, e => e is JourneyCompleted);
        Assert.IsType<PlayerDied>(session.UncommittedEvents[^1]);
        var encounterEventIndex = Array.FindIndex(session.UncommittedEvents.ToArray(), e => e is JourneyEncounterResolved);
        Assert.All(session.UncommittedEvents.Skip(encounterEventIndex + 1), e => Assert.IsType<PlayerDied>(e));

        var replay = GameSession.RehydrateFromEvents(
            session.Id,
            world,
            setupEvents.Concat(session.UncommittedEvents).ToArray());
        Assert.Equal(0, replay.Player.Health);
        Assert.Equal(GameStatus.Failed, replay.Status);
        Assert.Equal(JourneyStatus.Failed, replay.Journey!.Status);
    }

    private static (GameSession Session, TravelPreview Preview, IDomainEvent[] SetupEvents, DomainWorld World) CreateJourney(int food)
    {
        var origin = new Town(new TownId("o2"), "Pinecross");
        var destination = new Town(new TownId("d2"), "Six Mile");
        var world = new DomainWorld(
            [origin, destination],
            [new Trail(new TrailId("trail-q-2"), origin.Id, destination.Id,
                TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, rideDayDistance: 3m)]);
        var inventory = new DomainInventory(food == 0
            ? [new InventoryItem(ItemKind.Knife, 1)]
            : [new InventoryItem(ItemKind.Food, food), new InventoryItem(ItemKind.Knife, 1)]);
        var caseFile = new CaseFile(
            accusation: null,
            suspects: [],
            trueCulpritId: new SuspectId("starve-culprit"),
            knownClues: []);
        var session = TestSessionFactory.StartGameCanonical(
            "Ranger Vale",
            world,
            caseFile,
            origin.Id,
            Wallet.Starting(25m),
            inventory,
            GameDifficulty.Easy,
            SaltSource.CreateFixed(string.Empty));
        var setupEvents = session.UncommittedEvents.ToArray();
        session.MarkEventsCommitted();

        var previewResult = new TravelResolver().PreviewJourney(
            world,
            origin.Id,
            destination.Id,
            session.Player.Inventory,
            session.TravelRules);
        Assert.True(previewResult.Success);
        Assert.NotNull(previewResult.Preview);
        return (session, previewResult.Preview, setupEvents, world);
    }
}
