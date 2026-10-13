using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests.EventSourcing;

/// <summary>
/// Tests for the BUNCH-80 clock/turn correction: event-sourced action-context-based
/// turn advancement and TimeOfDay naming layer.
/// See ADR-0028.
/// </summary>
public sealed class ClockTurnCorrectionTests
{
    [Fact]
    public void EnterActionContext_DifferentContext_ProducesEventAndAdvancesTurn()
    {
        var session = TestSessionFactory.CreateDefault();
        Assert.Equal(TownActionContext.None, session.CurrentActionContext);
        var turnBefore = session.Clock.Turn;

        session.EnterActionContext(TownActionContext.Saloon);

        Assert.Equal(TownActionContext.Saloon, session.CurrentActionContext);
        Assert.Equal(turnBefore + 1, session.Clock.Turn);
        var contextEvent = Assert.Single(session.UncommittedEvents.OfType<TownActionContextEntered>());
        Assert.Equal(TownActionContext.Saloon, contextEvent.Context);
        Assert.Equal(turnBefore + 1, contextEvent.Turn);
        Assert.Equal(session.Clock.Day, contextEvent.Day);
    }

    [Fact]
    public void EnterActionContext_SameContext_DoesNotProduceEventOrAdvanceTurn()
    {
        var session = TestSessionFactory.CreateDefault();
        session.EnterActionContext(TownActionContext.Saloon);
        session.MarkEventsCommitted();
        var turnAfterFirstEntry = session.Clock.Turn;

        session.EnterActionContext(TownActionContext.Saloon);

        Assert.Equal(turnAfterFirstEntry, session.Clock.Turn);
        Assert.Empty(session.UncommittedEvents);
    }

    [Fact]
    public void EnterActionContext_None_DoesNotProduceEventOrAdvanceTurn()
    {
        var session = TestSessionFactory.CreateDefault();
        var turnBefore = session.Clock.Turn;

        session.EnterActionContext(TownActionContext.None);

        Assert.Equal(turnBefore, session.Clock.Turn);
        Assert.Empty(session.UncommittedEvents);
    }

    [Fact]
    public void EnterActionContext_WrapsTurnToNextDayAtNight()
    {
        var session = TestSessionFactory.CreateDefault();
        // Advance to Night (turn 3) by entering four distinct contexts.
        session.EnterActionContext(TownActionContext.Saloon);       // turn 1 (Afternoon)
        session.EnterActionContext(TownActionContext.SheriffOffice); // turn 2 (Evening)
        session.EnterActionContext(TownActionContext.Store);         // turn 3 (Night)
        session.MarkEventsCommitted();
        Assert.Equal(3, session.Clock.Turn);
        Assert.Equal(TimeOfDay.Night, session.Clock.TimeOfDay);
        var dayBefore = session.Clock.Day;

        session.EnterActionContext(TownActionContext.Stable);        // wraps to turn 0, day 2

        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(dayBefore + 1, session.Clock.Day);
        Assert.Equal(TimeOfDay.Morning, session.Clock.TimeOfDay);
    }

    [Fact]
    public void Replay_TownActionContextEntered_ReconstructsContextAndClock()
    {
        // Build a session and capture the setup events before it is marked committed.
        var session = CreateDefaultSessionWithUncommittedGameStarted(out var setupEvents);
        session.MarkEventsCommitted();

        session.EnterActionContext(TownActionContext.Saloon);
        session.EnterActionContext(TownActionContext.SheriffOffice);
        var contextEvents = session.UncommittedEvents.ToList();
        var events = setupEvents.Concat(contextEvents).ToList();
        var contextAfterCommands = session.CurrentActionContext;
        var dayAfterCommands = session.Clock.Day;
        var turnAfterCommands = session.Clock.Turn;

        var replayed = GameSession.RehydrateFromEvents(
            session.Id, session.World,
            events);

        Assert.Equal(contextAfterCommands, replayed.CurrentActionContext);
        Assert.Equal(dayAfterCommands, replayed.Clock.Day);
        Assert.Equal(turnAfterCommands, replayed.Clock.Turn);
    }

    /// <summary>
    /// Creates a default session but returns it BEFORE MarkEventsCommitted so the caller
    /// can capture the setup events for replay-stream construction.
    /// </summary>
    private static GameSession CreateDefaultSessionWithUncommittedGameStarted(out IReadOnlyList<IDomainEvent> setupEvents)
    {
        var town = new Town(new TownId("current"), "Current Town");
        var connected = new Town(new TownId("connected"), "Connected Town");
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint",
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline",
                SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: Array.Empty<Clue>());

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1)
        });

        var session = TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory, GameDifficulty.Easy,
            SaltSource.CreateFixed(string.Empty));
        setupEvents = session.UncommittedEvents.ToList();
        return session;
    }

    [Fact]
    public void TimeOfDay_MapsFromTurnCorrectly()
    {
        var clock = new GameClock();
        Assert.Equal(TimeOfDay.Morning, clock.TimeOfDay);

        clock.Advance();
        Assert.Equal(TimeOfDay.Afternoon, clock.TimeOfDay);

        clock.Advance();
        Assert.Equal(TimeOfDay.Evening, clock.TimeOfDay);

        clock.Advance();
        Assert.Equal(TimeOfDay.Night, clock.TimeOfDay);

        clock.Advance();
        Assert.Equal(TimeOfDay.Morning, clock.TimeOfDay);
        Assert.Equal(2, clock.Day);
    }

    [Fact]
    public void GameClock_Set_ReconstructsExactDayAndTurn()
    {
        var clock = new GameClock();
        clock.Set(day: 3, turn: 2);
        Assert.Equal(3, clock.Day);
        Assert.Equal(2, clock.Turn);
        Assert.Equal(TimeOfDay.Evening, clock.TimeOfDay);
    }

    // --- Task 6: Investigation method context entry ---

    [Fact]
    public void ReadWantedPosters_EntersSheriffOfficeContext_AndAdvancesTurn()
    {
        var session = TestSessionFactory.CreateDefault();
        var turnBefore = session.Clock.Turn;
        session.ReadWantedPosters();
        Assert.Equal(TownActionContext.SheriffOffice, session.CurrentActionContext);
        Assert.Equal(turnBefore + 1, session.Clock.Turn);
    }

    [Fact]
    public void GatherLocalGossip_EntersSaloonContext()
    {
        var session = TestSessionFactory.CreateDefault();
        session.GatherLocalGossip();
        Assert.Equal(TownActionContext.Saloon, session.CurrentActionContext);
    }

    [Fact]
    public void ReadWantedPosters_EntersSheriffOfficeContext()
    {
        var session = TestSessionFactory.CreateDefault();
        session.ReadWantedPosters();
        Assert.Equal(TownActionContext.SheriffOffice, session.CurrentActionContext);
    }

    [Fact]
    public void TwoSheriffActionsInSameContext_DoNotAdvanceTurnTwice()
    {
        var session = TestSessionFactory.CreateDefault();
        session.ReadWantedPosters(); // enters SheriffOffice, advances turn
        var turnAfterFirst = session.Clock.Turn;

        // ReadWantedPosters is also a SheriffOffice action — same context, no turn advance
        session.ReadWantedPosters();
        Assert.Equal(turnAfterFirst, session.Clock.Turn);
        Assert.Equal(TownActionContext.SheriffOffice, session.CurrentActionContext);
    }

    [Fact]
    public void JourneyRoundTrip_ReturningToSameTownResetsSaloonContextAndAdvancesTime()
    {
        var (session, outboundPreview) = TravelTestFactory.CreateEasyShortJourney();
        var originalTownId = session.Player.CurrentTownId!.Value;

        session.LookAroundSaloon();
        Assert.Equal(TownActionContext.Saloon, session.CurrentActionContext);

        CompleteJourneyAndAcknowledge(session, outboundPreview);
        Assert.Equal(outboundPreview.DestinationTownId, session.Player.CurrentTownId);
        Assert.Equal(TownActionContext.None, session.CurrentActionContext);

        session.LookAroundSaloon();
        Assert.Equal(TownActionContext.Saloon, session.CurrentActionContext);

        var returnPreview = TravelTestFactory.ResolvePreviewFor(session, originalTownId);
        CompleteJourneyAndAcknowledge(session, returnPreview);
        Assert.Equal(originalTownId, session.Player.CurrentTownId);
        Assert.Equal(TownActionContext.None, session.CurrentActionContext);

        var turnBeforeReenteringSaloon = session.Clock.Turn;
        session.LookAroundSaloon();

        Assert.Equal(TownActionContext.Saloon, session.CurrentActionContext);
        Assert.Equal(turnBeforeReenteringSaloon + 1, session.Clock.Turn);
    }

    private static void CompleteJourneyAndAcknowledge(GameSession session, TravelPreview preview)
    {
        var start = session.StartJourney(preview);
        Assert.True(start.Success);

        TravelJourneyStepResult result;
        do
        {
            session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            result = session.AdvanceJourneyDay();
        } while (result.Success && result.Status == JourneyStatus.Active);

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Completed, result.Status);
        Assert.True(session.AcknowledgeJourneyArrival().Success);
    }
}
