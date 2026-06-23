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
/// Event-sourcing tests for the BUNCH-80 bounty/saloon migration.
/// Verifies that LookAroundSaloon produces typed events (SaloonPersonOfInterestSpotted)
/// and that the clock advances via TownActionContextEntered, not via RecordCaseUpdate.
/// See ADR-0028 and docs/superpowers/plans/2026-06-23-bunch-80-phase1-events-and-apply.md.
/// </summary>
public sealed class BountySaloonEventSourcingTests
{
    [Fact]
    public void LookAroundSaloonWithSuspectProducesSpottedEvent()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // Two events: TownActionContextEntered (context change) + SaloonPersonOfInterestSpotted
        Assert.Equal(2, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        var e = Assert.IsType<SaloonPersonOfInterestSpotted>(session.UncommittedEvents[1]);
        Assert.Equal(InvestigationSourceKind.SaloonLookAround, e.SourceKind);
        Assert.NotNull(e.Descriptor);
        Assert.NotNull(e.SuspectId);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, e.PersonOfInterestKind);
    }

    [Fact]
    public void LookAroundSaloonCitizenProducesSpottedEventWithNoLog()
    {
        var session = TestSessionFactory.CreateWithNoConfrontableSaloonSuspect();
        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        // Two events: TownActionContextEntered + SaloonPersonOfInterestSpotted
        var spottedEvent = session.UncommittedEvents.OfType<SaloonPersonOfInterestSpotted>().Single();
        Assert.Null(spottedEvent.SuspectId);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen, spottedEvent.PersonOfInterestKind);
        Assert.False(spottedEvent.RecordLog);
    }

    [Fact]
    public void LookAroundSaloonAdvancesTurnViaContextEvent()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        var turnBefore = session.Clock.Turn;

        session.LookAroundSaloon();

        Assert.Equal(turnBefore + 1, session.Clock.Turn);
    }

    [Fact]
    public void LookAroundSaloonNoSaloonDoesNotAdvanceTurnOrProduceEvent()
    {
        var session = TestSessionFactory.CreateWithNoSaloon();
        var turnBefore = session.Clock.Turn;

        var result = session.LookAroundSaloon();

        Assert.False(result.Success);
        Assert.Empty(session.UncommittedEvents);
        Assert.Equal(turnBefore, session.Clock.Turn);
    }

    [Fact]
    public void LookAroundSaloonRepeatProducesSpottedEventWithNoSuspect()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.LookAroundSaloon();
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        // Context is already Saloon, so no new TownActionContextEntered — only SaloonPersonOfInterestSpotted
        var spottedEvent = session.UncommittedEvents.OfType<SaloonPersonOfInterestSpotted>().Single();
        Assert.Null(spottedEvent.SuspectId);
        Assert.Null(spottedEvent.PersonOfInterestKind);
        Assert.True(spottedEvent.RecordLog);
    }

    [Fact]
    public void Replay_SaloonPersonOfInterestSpotted_ReconstructsActivePersonOfInterest()
    {
        // Build the session inline to capture the GameStarted event before MarkEventsCommitted.
        var session = CreateConfrontableSaloonSessionWithUncommittedGameStarted(out var gameStarted);
        session.MarkEventsCommitted();

        session.LookAroundSaloon();
        var events = new[] { gameStarted }.Concat(session.UncommittedEvents).ToList();
        var activePoiIdAfterCommand = session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId;
        var contextAfterCommand = session.CurrentActionContext;
        var turnAfterCommand = session.Clock.Turn;

        var replayed = GameSession.RehydrateFromEvents(
            session.Id, session.World, TestSessionFactory.CreateBaselineCaseFileFor(session),
            events);

        Assert.Equal(activePoiIdAfterCommand, replayed.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Equal(contextAfterCommand, replayed.CurrentActionContext);
        Assert.Equal(turnAfterCommand, replayed.Clock.Turn);
    }

    private static GameSession CreateConfrontableSaloonSessionWithUncommittedGameStarted(out GameStarted gameStarted)
    {
        var town = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Mira Cline",
                new SuspectProfile(
                    Array.Empty<SuspectAlias>(),
                    new[] { new SuspectIdentityFact("Has a scar on the left cheek.") }),
                SuspectTraits.Empty,
                SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: null, TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        gameStarted = Assert.IsType<GameStarted>(session.UncommittedEvents.Single());
        return session;
    }
}
