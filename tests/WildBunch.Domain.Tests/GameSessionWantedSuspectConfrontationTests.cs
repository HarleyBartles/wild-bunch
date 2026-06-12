using System.Text.Json;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionWantedSuspectConfrontationTests
{
    [Fact]
    public void ResolveWantedSuspectConfrontationRecordsSurrenderedState()
    {
        var session = CreateSession();

        var result = session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);

        Assert.True(result.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Surrendered, result.Outcome);
        Assert.Equal("Mira Cline", result.TargetName);
        Assert.Equal(WarrantDisposition.DeadOrAlive, result.Disposition);
        Assert.True(result.IsAlive);
        Assert.True(result.IsSecured);
        Assert.True(result.SessionChanged);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Single(session.CaseFile.WantedSuspectConfrontations);
        Assert.Equal(WantedSuspectPresenceState.SecuredAlive, session.GetWantedSuspectPresenceState(new SuspectId("suspect-1")));
        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(new SuspectId("suspect-1"), out var state));
        Assert.Equal(WantedSuspectConfrontationOutcome.Surrendered, state.Outcome);
        Assert.True(state.IsAlive);
        Assert.True(state.IsSecured);

        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveWantedSuspectConfrontationRecordsKilledStateAndKeepsDeadTurnInSeparate()
    {
        var session = CreateSession();

        var result = session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Killed);

        Assert.True(result.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Killed, result.Outcome);
        Assert.False(result.IsAlive);
        Assert.True(result.IsSecured);
        Assert.True(result.SessionChanged);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(WantedSuspectPresenceState.SecuredDead, session.GetWantedSuspectPresenceState(new SuspectId("suspect-1")));
        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(new SuspectId("suspect-1"), out var state));
        Assert.Equal(WantedSuspectConfrontationOutcome.Killed, state.Outcome);
        Assert.False(state.IsAlive);
        Assert.True(state.IsSecured);

        var turnIn = session.AssessSheriffTurnIn(new SuspectId("suspect-1"), isAlive: false);

        Assert.True(turnIn.Success);
        Assert.Equal(SheriffTurnInOutcome.AcceptedDead, turnIn.Outcome);
        Assert.Equal(WarrantDisposition.DeadOrAlive, turnIn.Disposition);
    }

    [Fact]
    public void ResolveWantedSuspectConfrontationRejectsRepeatAfterKilledWithoutChangingState()
    {
        var session = CreateSession();

        var firstResult = session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Killed);
        var secondResult = session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);

        Assert.True(firstResult.Success);
        Assert.False(secondResult.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Rejected, secondResult.Outcome);
        Assert.False(secondResult.SessionChanged);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(WantedSuspectPresenceState.SecuredDead, session.GetWantedSuspectPresenceState(new SuspectId("suspect-1")));
        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(new SuspectId("suspect-1"), out var state));
        Assert.Equal(WantedSuspectConfrontationOutcome.Killed, state.Outcome);
        Assert.False(state.IsAlive);
        Assert.True(state.IsSecured);
    }

    [Fact]
    public void ResolveWantedSuspectConfrontationRejectsRepeatAfterSurrenderedWithoutChangingState()
    {
        var session = CreateSession();

        var firstResult = session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);
        var secondResult = session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Killed);

        Assert.True(firstResult.Success);
        Assert.False(secondResult.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Rejected, secondResult.Outcome);
        Assert.False(secondResult.SessionChanged);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(WantedSuspectPresenceState.SecuredAlive, session.GetWantedSuspectPresenceState(new SuspectId("suspect-1")));
        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(new SuspectId("suspect-1"), out var state));
        Assert.Equal(WantedSuspectConfrontationOutcome.Surrendered, state.Outcome);
        Assert.True(state.IsAlive);
        Assert.True(state.IsSecured);
    }

    [Fact]
    public void ResolveWantedSuspectConfrontationRecordsFledStateAndBlocksTurnIn()
    {
        var session = CreateSession();

        var result = session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Fled);

        Assert.True(result.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, result.Outcome);
        Assert.True(result.IsAlive);
        Assert.False(result.IsSecured);
        Assert.True(result.SessionChanged);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(WantedSuspectPresenceState.GoneToGround, session.GetWantedSuspectPresenceState(new SuspectId("suspect-1")));
        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(new SuspectId("suspect-1"), out var state));
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, state.Outcome);
        Assert.False(state.IsSecured);

        var turnIn = session.AssessSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        Assert.False(turnIn.Success);
        Assert.Equal(SheriffTurnInOutcome.Rejected, turnIn.Outcome);
        Assert.Contains("secured", turnIn.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveWantedSuspectConfrontationTreatsAbandonedAsNoResolution()
    {
        var session = CreateSession();

        var result = session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Abandoned);

        Assert.True(result.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Abandoned, result.Outcome);
        Assert.Null(result.IsAlive);
        Assert.Null(result.IsSecured);
        Assert.True(result.SessionChanged);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Equal(WantedSuspectPresenceState.Unavailable, session.GetWantedSuspectPresenceState(new SuspectId("suspect-1")));

        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveWantedSuspectConfrontationRejectsBlockersWithoutMutatingState()
    {
        var session = CreateSession();
        StartJourney(session);
        session.Journey!.MarkCompleted();

        var journeyBlocked = session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);
        var missingWarrant = CreateSession().ResolveWantedSuspectConfrontation(new SuspectId("suspect-2"), WantedSuspectConfrontationChoice.Fled);
        var invalidSuspect = CreateSession().ResolveWantedSuspectConfrontation(new SuspectId("suspect-unknown"), WantedSuspectConfrontationChoice.Killed);

        Assert.False(journeyBlocked.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Rejected, journeyBlocked.Outcome);
        Assert.False(journeyBlocked.SessionChanged);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);

        Assert.False(missingWarrant.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Rejected, missingWarrant.Outcome);
        Assert.False(invalidSuspect.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Rejected, invalidSuspect.Outcome);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.NoticeBoard);
        var connected = new Town(new TownId("connected"), "Connected", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, connected },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, connected.Id, TrailRisk.Low) });

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
                    "Wanted for a stage robbery.")
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
    }

    private static void StartJourney(GameSession session)
    {
        var travelResolver = new TravelResolver();
        var destinationTownId = new TownId("connected");
        var preview = travelResolver.PreviewJourney(
                session.World,
                session.Player.CurrentTownId,
                destinationTownId,
                session.Player.Inventory)
            .Preview!;

        session.StartJourney(preview);
    }
}
