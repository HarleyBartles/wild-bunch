using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionSaloonWantedSuspectLoopTests
{
    [Fact]
    public void LookAroundSaloonTracksAnActiveSuspectAndConfrontingItMakesTheSuspectFlee()
    {
        var session = CreateSession();
        var suspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);

        var lookAround = session.LookAroundSaloon();
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonWantedSuspectId);

        var confrontation = session.ConfrontSaloonWantedSuspect();
        var repeatConfrontation = session.ConfrontSaloonWantedSuspect();

        Assert.True(lookAround.Success);
        Assert.Equal("You look around the saloon and spot Mira Cline.", lookAround.Message);

        Assert.True(confrontation.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, confrontation.Outcome);
        Assert.Equal("Mira Cline", confrontation.TargetName);
        Assert.True(confrontation.IsAlive);
        Assert.False(confrontation.IsSecured);
        Assert.Equal(WantedSuspectPresenceState.GoneToGround, session.GetWantedSuspectPresenceState(suspectId));
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonWantedSuspectId);
        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out var confrontationState));
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, confrontationState.Outcome);

        Assert.False(repeatConfrontation.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Rejected, repeatConfrontation.Outcome);
        Assert.Equal(WantedSuspectPresenceState.GoneToGround, session.GetWantedSuspectPresenceState(suspectId));
        Assert.Single(session.CaseFile.WantedSuspectConfrontations);

        session.Player.TravelTo(new TownId("connected"));
        session.CurrentTownVisit.Reset(new TownId("connected"));
        session.Player.TravelTo(new TownId("current"));
        session.CurrentTownVisit.Reset(new TownId("current"));

        var afterReturn = session.LookAroundSaloon();

        Assert.True(afterReturn.Success);
        Assert.Equal("You look around the saloon and spot Mira Cline.", afterReturn.Message);
    }

    [Fact]
    public void ConfrontSaloonWantedSuspectRejectsWhenNoSuspectHasBeenSpotted()
    {
        var session = CreateSession();

        var result = session.ConfrontSaloonWantedSuspect();

        Assert.False(result.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Rejected, result.Outcome);
        Assert.Contains("saloon", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonWantedSuspectId);
    }

    [Fact]
    public void ConfrontSaloonWantedSuspectKeepsAGoneToGroundPersonOfInterestEligibleForFutureSelection()
    {
        var session = CreateSession();
        var suspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(suspectId);
        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.GoneToGround);

        var result = session.ConfrontSaloonWantedSuspect();

        Assert.True(result.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, result.Outcome);
        Assert.True(result.SessionChanged);
        Assert.Contains("Mira Cline", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonWantedSuspectId);
        Assert.Equal(WantedSuspectPresenceState.GoneToGround, session.GetWantedSuspectPresenceState(suspectId));
        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out var confrontationState));
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, confrontationState.Outcome);
    }

    [Fact]
    public void ConfrontSaloonWantedSuspectClearsAStaleActiveSuspectThatNoLongerHasAKnownWarrant()
    {
        var session = CreateSessionWithoutKnownWarrants();
        var suspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(suspectId);

        var result = session.ConfrontSaloonWantedSuspect();

        Assert.False(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Contains("wanted notice", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonWantedSuspectId);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Equal(WantedSuspectPresenceState.AvailableInTown, session.GetWantedSuspectPresenceState(suspectId));
    }

    [Fact]
    public void LookAroundSaloonDoesNotSurfaceAWantedSuspectWithoutAKnownWarrant()
    {
        var session = CreateSessionWithoutKnownWarrants();
        var suspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.Equal("You look around the saloon, but nobody of interest is here.", result.Message);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonWantedSuspectId);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
    }

    private static GameSession CreateSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }

    private static GameSession CreateSessionWithoutKnownWarrants()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }
}
