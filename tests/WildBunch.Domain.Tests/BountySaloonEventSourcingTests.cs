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
/// See ADR-0028 and .agents/superpowers/plans/2026-06-23-bunch-80-phase1-events-and-apply.md.
/// </summary>
public sealed class BountySaloonEventSourcingTests
{
    [Fact]
    public void LookAroundSaloonWithSuspectProducesSpottedEvent()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session.MarkEventsCommitted();
        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // Three events: TownActionContextEntered (context change) + DevSaloonOverrideConsumed + SaloonPersonOfInterestSpotted
        Assert.Equal(3, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        Assert.IsType<DevSaloonOverrideConsumed>(session.UncommittedEvents[1]);
        var e = Assert.IsType<SaloonPersonOfInterestSpotted>(session.UncommittedEvents[2]);
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
        Assert.NotNull(spottedEvent.Descriptor);
        Assert.NotNull(spottedEvent.CitizenRole);
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
        var town = new Town(new TownId("current"), "Current Town", TownServices.None);
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
            Wallet.Starting(25m), inventory: null, GameDifficulty.Easy,
            SaltSource.CreateFixed(string.Empty));
        gameStarted = Assert.IsType<GameStarted>(session.UncommittedEvents.Single());
        return session;
    }

    // --- Task 3: WantedSuspectConfronted event + ResolveWantedSuspectConfrontation ---

    [Fact]
    public void ResolveWantedSuspectConfrontationSurrenderedProducesEvent()
    {
        var session = TestSessionFactory.CreateWithWarrantedSuspect();
        // Pre-enter Saloon context (as LookAroundSaloon would do) and set active POI
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.MarkEventsCommitted();

        var result = session.ResolveWantedSuspectConfrontation(
            new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // Single event: WantedSuspectConfronted (no context event — already in Saloon)
        Assert.Single(session.UncommittedEvents);
        var e = Assert.IsType<WantedSuspectConfronted>(session.UncommittedEvents.Single());
        Assert.Equal(new SuspectId("suspect-1"), e.TargetSuspectId);
        Assert.Equal(WantedSuspectConfrontationOutcome.Surrendered, e.Outcome);
        Assert.True(e.IsAlive);
        Assert.True(e.IsSecured);
    }

    [Fact]
    public void ResolveWantedSuspectConfrontation_DoesNotAdvanceTurn_WhenAlreadyInSaloonContext()
    {
        var session = TestSessionFactory.CreateWithWarrantedSuspect();
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.MarkEventsCommitted();
        var turnBefore = session.Clock.Turn;

        session.ResolveWantedSuspectConfrontation(
            new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);

        // Turn does NOT advance — already in Saloon context
        Assert.Equal(turnBefore, session.Clock.Turn);
    }

    [Fact]
    public void ResolveWantedSuspectConfrontationAbandonedProducesEventWithoutConfrontationState()
    {
        var session = TestSessionFactory.CreateWithWarrantedSuspect();
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.MarkEventsCommitted();

        var result = session.ResolveWantedSuspectConfrontation(
            new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Abandoned);

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Single(session.UncommittedEvents);
        var e = Assert.IsType<WantedSuspectConfronted>(session.UncommittedEvents.Single());
        Assert.Equal(WantedSuspectConfrontationOutcome.Abandoned, e.Outcome);
        // Abandoned does not record confrontation state
        Assert.False(session.CaseFile.TryGetWantedSuspectConfrontationState(new SuspectId("suspect-1"), out _));
    }

    [Fact]
    public void ResolveWantedSuspectConfrontation_RecordsClockTurnWithoutPlusOneOffset()
    {
        var session = TestSessionFactory.CreateWithWarrantedSuspect();
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.MarkEventsCommitted();
        var turnBefore = session.Clock.Turn;

        session.ResolveWantedSuspectConfrontation(
            new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);

        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(new SuspectId("suspect-1"), out var state));
        Assert.Equal(turnBefore, state.Turn);
    }

    // --- Task 4: SheriffTurnInSettled event + SettleSheriffTurnIn ---

    [Fact]
    public void SettleSheriffTurnInProducesSettledEvent()
    {
        var session = TestSessionFactory.CreateWithSecuredSuspect();
        // Pre-enter Saloon context (as the confrontation flow would do)
        session.EnterActionContext(TownActionContext.Saloon);
        session.MarkEventsCommitted();

        var result = session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // Two events: TownActionContextEntered(SheriffOffice) + SheriffTurnInSettled
        Assert.Equal(2, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        var e = Assert.IsType<SheriffTurnInSettled>(session.UncommittedEvents[1]);
        Assert.Equal(new SuspectId("suspect-1"), e.TargetSuspectId);
        Assert.True(e.BountyAmount > 0);
    }

    [Fact]
    public void SettleSheriffTurnIn_AdvancesTurn_WhenEnteringSheriffContextFromSaloon()
    {
        var session = TestSessionFactory.CreateWithSecuredSuspect();
        session.EnterActionContext(TownActionContext.Saloon);
        session.MarkEventsCommitted();
        var turnBefore = session.Clock.Turn;

        session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        // Turn advances — context change from Saloon to SheriffOffice
        Assert.Equal(turnBefore + 1, session.Clock.Turn);
        Assert.Equal(TownActionContext.SheriffOffice, session.CurrentActionContext);
    }

    [Fact]
    public void SettleSheriffTurnIn_Rejected_StillProducesContextEvent()
    {
        var session = TestSessionFactory.CreateWithWarrantedSuspect();
        session.EnterActionContext(TownActionContext.Saloon);
        session.MarkEventsCommitted();
        var turnBefore = session.Clock.Turn;

        // Try to turn in a suspect that hasn't been confronted/secured
        var result = session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        // Turn-in is rejected (suspect not secured), but the player still went to the sheriff's office
        Assert.False(result.Success);
        Assert.Equal(turnBefore + 1, session.Clock.Turn);
        Assert.Equal(TownActionContext.SheriffOffice, session.CurrentActionContext);
        // Context event is in the stream even though no settlement event follows
        Assert.Contains(session.UncommittedEvents, e => e is TownActionContextEntered);
        Assert.DoesNotContain(session.UncommittedEvents, e => e is SheriffTurnInSettled);
    }

    // --- Task 5: SaloonPersonOfInterestConfronted event + ConfrontSaloonPersonOfInterest ---

    [Fact]
    public void ConfrontCitizenWithWrongDeclarationProducesConfrontedEvent()
    {
        var session = TestSessionFactory.CreateWithActiveCitizenSaloonPerson();
        var result = session.ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle: "wrong-handle");

        Assert.True(result.Success);
        var confrontedEvent = session.UncommittedEvents.OfType<SaloonPersonOfInterestConfronted>().Single();
        Assert.True(confrontedEvent.IsCitizen);
        Assert.True(confrontedEvent.FineAmount > 0);
    }

    [Fact]
    public void ConfrontArmedCorrectDeclarationProducesConfrontedAndSettledEvents()
    {
        var session = TestSessionFactory.CreateWithArmedCorrectDeclarationSetup();
        var result = session.ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle: "warrant-public-1");

        Assert.True(result.Success);
        Assert.Contains(session.UncommittedEvents, e => e is WantedSuspectConfronted);
        Assert.Contains(session.UncommittedEvents, e => e is SheriffTurnInSettled);
        Assert.Contains(session.UncommittedEvents, e => e is SaloonPersonOfInterestConfronted);
    }

    [Fact]
    public void ConfrontSaloonPerson_DoesNotAdvanceTurn_WhenAlreadyInSaloonContext()
    {
        var session = TestSessionFactory.CreateWithActiveCitizenSaloonPerson();
        var turnAfterLookAround = session.Clock.Turn;

        session.ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle: "wrong-handle");

        Assert.Equal(turnAfterLookAround, session.Clock.Turn);
    }

    [Fact]
    public void ConfrontSaloonPerson_NoPersonOfInterest_DoesNotProduceEvent()
    {
        var session = TestSessionFactory.CreateWithNoConfrontableSaloonSuspect();
        session.EnterActionContext(TownActionContext.Saloon);
        session.MarkEventsCommitted();

        var result = session.ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle: "wrong-handle");

        Assert.False(result.Success);
        Assert.Empty(session.UncommittedEvents);
    }

    // --- Hidden-truth boundary tests ---

    [Fact]
    public void BountySaloonEvents_DoNotCarryHiddenTruthFields()
    {
        // Verify that none of the 5 new event types have properties named
        // TrueCulpritId, LinkedSuspectIds, TargetKind, or KillerReleaseState
        var eventTypes = new[]
        {
            typeof(TownActionContextEntered),
            typeof(SaloonPersonOfInterestSpotted),
            typeof(WantedSuspectConfronted),
            typeof(SheriffTurnInSettled),
            typeof(SaloonPersonOfInterestConfronted)
        };

        var forbiddenNames = new[] { "TrueCulpritId", "LinkedSuspectIds", "TargetKind", "KillerReleaseState" };

        foreach (var type in eventTypes)
        {
            foreach (var prop in type.GetProperties())
            {
                Assert.DoesNotContain(prop.Name, forbiddenNames);
            }
        }
    }

    [Fact]
    public void BountySaloonEventJson_DoesNotContainHiddenTruthFields()
    {
        var events = new IDomainEvent[]
        {
            new TownActionContextEntered { Context = TownActionContext.Saloon, TownId = new TownId("current"), Day = 1, Turn = 1, TimeOfDay = TimeOfDay.Morning, PursuitHeat = 0 },
            new SaloonPersonOfInterestSpotted { SourceKind = InvestigationSourceKind.SaloonLookAround, TownId = new TownId("current"), Message = "test", RecordLog = true },
            new WantedSuspectConfronted { TargetSuspectId = new SuspectId("s1"), TargetName = "Test", Disposition = WarrantDisposition.DeadOrAlive, Choice = WantedSuspectConfrontationChoice.Surrendered, Outcome = WantedSuspectConfrontationOutcome.Surrendered, IsAlive = true, IsSecured = true, Message = "test" },
            new SheriffTurnInSettled { TargetSuspectId = new SuspectId("s1"), TargetName = "Test", Disposition = WarrantDisposition.DeadOrAlive, IsAlive = true, BountyAmount = 50m, Message = "test", Day = 1, Turn = 1 },
            new SaloonPersonOfInterestConfronted { Message = "test", TargetName = "stranger", PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen, Outcome = SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration, IsCitizen = true }
        };

        foreach (var e in events)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(e, e.GetType());
            Assert.DoesNotContain("trueCulpritId", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("linkedSuspectIds", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("targetKind", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("killerReleaseState", json, StringComparison.OrdinalIgnoreCase);
        }
    }
}
