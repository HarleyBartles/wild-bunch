using System.Text.Json;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionSheriffTurnInTests
{
    [Fact]
    public void AssessSheriffTurnInReturnsAcceptedAliveForKnownDeadOrAliveWarrant()
    {
        var session = CreateSession();
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);

        var result = session.AssessSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        Assert.True(result.Success);
        Assert.Equal(SheriffTurnInOutcome.AcceptedAlive, result.Outcome);
        Assert.Equal("Mira Cline", result.TargetName);
        Assert.Equal(WarrantDisposition.DeadOrAlive, result.Disposition);
        Assert.Equal(2500m, result.BountyAmount);
        Assert.False(result.SessionChanged);

        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AssessSheriffTurnInReturnsAcceptedDeadForKnownDeadOrAliveWarrant()
    {
        var session = CreateSession();
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Killed);

        var result = session.AssessSheriffTurnIn(new SuspectId("suspect-1"), isAlive: false);

        Assert.True(result.Success);
        Assert.Equal(SheriffTurnInOutcome.AcceptedDead, result.Outcome);
        Assert.Equal("Mira Cline", result.TargetName);
        Assert.Equal(WarrantDisposition.DeadOrAlive, result.Disposition);
        Assert.Equal(2500m, result.BountyAmount);
        Assert.False(result.SessionChanged);
    }

    [Fact]
    public void AssessSheriffTurnInReturnsRejectedForDeadAliveOnlyWarrant()
    {
        var session = CreateSession();
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-2"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-2"), WantedSuspectConfrontationChoice.Killed);

        var result = session.AssessSheriffTurnIn(new SuspectId("suspect-2"), isAlive: false);

        Assert.False(result.Success);
        Assert.Equal(SheriffTurnInOutcome.Rejected, result.Outcome);
        Assert.Equal("Reno Pike", result.TargetName);
        Assert.Equal(WarrantDisposition.AliveOnly, result.Disposition);
        Assert.Equal(300m, result.BountyAmount);
        Assert.False(result.SessionChanged);
        Assert.Contains("alive", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AssessSheriffTurnInRejectsWhenTheTargetHasFled()
    {
        var session = CreateSession();
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Fled);

        var result = session.AssessSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        Assert.False(result.Success);
        Assert.Equal(SheriffTurnInOutcome.Rejected, result.Outcome);
        Assert.Equal("Mira Cline", result.TargetName);
        Assert.Equal(WarrantDisposition.DeadOrAlive, result.Disposition);
        Assert.Equal(2500m, result.BountyAmount);
        Assert.False(result.SessionChanged);
        Assert.Contains("secured", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AssessSheriffTurnInKeepsWrongPersonAliveAndDeadOutcomesDistinct()
    {
        var session = CreateSession();

        var aliveResult = session.AssessSheriffTurnIn(new SuspectId("suspect-unknown"), isAlive: true);
        var deadResult = session.AssessSheriffTurnIn(new SuspectId("suspect-unknown"), isAlive: false);

        Assert.False(aliveResult.Success);
        Assert.False(deadResult.Success);
        Assert.Equal(SheriffTurnInOutcome.WrongPersonAlive, aliveResult.Outcome);
        Assert.Equal(SheriffTurnInOutcome.WrongPersonDead, deadResult.Outcome);
        Assert.False(aliveResult.SessionChanged);
        Assert.False(deadResult.SessionChanged);
    }

    [Fact]
    public void SettleSheriffTurnInCreditsTheWalletAndRejectsRepeatPayouts()
    {
        var session = CreateSession();
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Killed);

        var firstResult = session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: false);
        var secondResult = session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: false);

        Assert.True(firstResult.Success);
        Assert.Equal(SheriffTurnInOutcome.AcceptedDead, firstResult.Outcome);
        Assert.Equal(2500m, firstResult.BountyAmount);
        Assert.True(firstResult.SessionChanged);
        Assert.Equal(2525m, session.Player.Wallet.Cash);
        Assert.Single(session.CaseFile.SheriffTurnInSettlements);
        Assert.True(session.CaseFile.TryGetSheriffTurnInSettlementState(new SuspectId("suspect-1"), out var settlementState));
        Assert.Equal("Mira Cline", settlementState.TargetName);
        Assert.False(settlementState.IsAlive);
        Assert.Equal(2500m, settlementState.BountyAmount);

        Assert.False(secondResult.Success);
        Assert.Equal(SheriffTurnInOutcome.Rejected, secondResult.Outcome);
        Assert.Equal(2525m, session.Player.Wallet.Cash);
        Assert.Single(session.CaseFile.SheriffTurnInSettlements);
        Assert.Contains("already been paid", secondResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SettleSheriffTurnInRejectedAttemptProducesContextEventAndReportsSessionChanged()
    {
        // BUNCH-80 review feedback: rejected sheriff turn-in attempts enter SheriffOffice,
        // which produces a TownActionContextEntered event and mutates clock/context even
        // when no SheriffTurnInSettled event follows. The returned result must truthfully
        // report SessionChanged when the context event was produced.
        var session = CreateSession();
        session.MarkEventsCommitted();
        // Start in Saloon context (simulates prior LookAroundSaloon)
        session.EnterActionContext(TownActionContext.Saloon);
        session.MarkEventsCommitted();
        var turnBeforeSettle = session.Clock.Turn;
        Assert.Equal(TownActionContext.Saloon, session.CurrentActionContext);

        // SettleSheriffTurnIn for a suspect with no confrontation state — will be rejected,
        // but entering SheriffOffice context produces a TownActionContextEntered event.
        var result = session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        // The turn-in itself is rejected (no confrontation state)
        Assert.False(result.Success);
        Assert.Equal(SheriffTurnInOutcome.Rejected, result.Outcome);

        // The context changed from Saloon to SheriffOffice, producing a TownActionContextEntered event
        Assert.True(result.SessionChanged, "Rejected turn-in with context change must report SessionChanged = true");
        Assert.Equal(TownActionContext.SheriffOffice, session.CurrentActionContext);
        Assert.Equal(turnBeforeSettle + 1, session.Clock.Turn);

        // The TownActionContextEntered(SheriffOffice) event must be in uncommitted events
        var contextEvent = session.UncommittedEvents.OfType<TownActionContextEntered>().Single();
        Assert.Equal(TownActionContext.SheriffOffice, contextEvent.Context);
        Assert.Equal(session.Clock.Day, contextEvent.Day);
        Assert.Equal(session.Clock.Turn, contextEvent.Turn);

        // No SheriffTurnInSettled event should be produced for a rejected turn-in
        Assert.DoesNotContain(session.UncommittedEvents, e => e is SheriffTurnInSettled);
    }

    [Fact]
    public void SettleSheriffTurnInRejectedAttemptInSameContextDoesNotReportSessionChanged()
    {
        // BUNCH-80 review feedback: when the session is already in SheriffOffice context,
        // a rejected turn-in does not produce a new TownActionContextEntered event and
        // must not report SessionChanged = true.
        var session = CreateSession();
        session.MarkEventsCommitted();
        // Already in SheriffOffice context
        session.EnterActionContext(TownActionContext.SheriffOffice);
        session.MarkEventsCommitted();
        var turnBefore = session.Clock.Turn;

        var result = session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        Assert.False(result.Success);
        Assert.False(result.SessionChanged, "Rejected turn-in in same context must not report SessionChanged");
        Assert.Equal(turnBefore, session.Clock.Turn);
        Assert.Empty(session.UncommittedEvents);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.NoticeBoard);
        var world = new DomainWorld(
            new[] { pinecross },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, pinecross.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-1"),
                    "Mira Cline",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        2500m,
                        new[] { "Red Wren" },
                        new[] { "Raven-feather pin" },
                        "Dodge City Marshal",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for a stage robbery."),
                new Warrant(
                    new WarrantId("warrant-2"),
                    "Reno Pike",
                    new WarrantTerms(
                        WarrantDisposition.AliveOnly,
                        300m,
                        new[] { "The Magpie" },
                        new[] { "Mismatched spurs" },
                        "Silver Creek Sheriff",
                        InvestigationTargetKind.UnrelatedWantedCriminal,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for cattle theft.")
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
    }
}
