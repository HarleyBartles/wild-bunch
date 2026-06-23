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
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Tests for the BUNCH-80 clock/turn correction: event-sourced action-context-based
/// turn advancement, TimeOfDay naming layer, and RecordCaseUpdate decoupling from the clock.
/// See ADR-0028 + docs/superpowers/plans/2026-06-23-bunch-80-overview.md.
/// </summary>
public sealed class ClockTurnCorrectionTests
{
    [Fact]
    public void RecordCaseUpdate_DoesNotAdvanceClock()
    {
        var session = TestSessionFactory.CreateDefault();
        var turnBefore = session.Clock.Turn;
        var dayBefore = session.Clock.Day;

        session.RecordCaseUpdate("test message");

        Assert.Equal(turnBefore, session.Clock.Turn);
        Assert.Equal(dayBefore, session.Clock.Day);
    }

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
        // Build a session and capture the GameStarted event before it is marked committed.
        var session = CreateDefaultSessionWithUncommittedGameStarted(out var gameStarted);
        session.MarkEventsCommitted();

        session.EnterActionContext(TownActionContext.Saloon);
        session.EnterActionContext(TownActionContext.SheriffOffice);
        var contextEvents = session.UncommittedEvents.ToList();
        var events = new[] { gameStarted }.Concat(contextEvents).ToList();
        var contextAfterCommands = session.CurrentActionContext;
        var dayAfterCommands = session.Clock.Day;
        var turnAfterCommands = session.Clock.Turn;

        var replayed = GameSession.RehydrateFromEvents(
            session.Id, session.World, TestSessionFactory.CreateBaselineCaseFileFor(session),
            events);

        Assert.Equal(contextAfterCommands, replayed.CurrentActionContext);
        Assert.Equal(dayAfterCommands, replayed.Clock.Day);
        Assert.Equal(turnAfterCommands, replayed.Clock.Turn);
    }

    /// <summary>
    /// Creates a default session but returns it BEFORE MarkEventsCommitted so the caller
    /// can capture the GameStarted event for replay-stream construction.
    /// </summary>
    private static GameSession CreateDefaultSessionWithUncommittedGameStarted(out GameStarted gameStarted)
    {
        var town = new Town(new TownId("current"), "Current Town",
            TownServices.NoticeBoard | TownServices.Telegraph | TownServices.Lodging);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory, TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        gameStarted = Assert.IsType<GameStarted>(session.UncommittedEvents.Single());
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
}
