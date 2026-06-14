using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionSaloonPersonOfInterestTests
{
    [Fact]
    public void LookAroundSaloonSurfacesAnActivePersonOfInterestAndRepeatLookAroundShowsNobodyElseOfInterest()
    {
        var session = CreateSessionWithoutKnownWarrants();
        var suspectId = new SuspectId("suspect-1");

        var lookAround = session.LookAroundSaloon();
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        var confrontation = session.ConfrontSaloonPersonOfInterest();
        var repeatLookAround = session.LookAroundSaloon();

        Assert.True(lookAround.Success);
        Assert.Equal("You look around the saloon and spot Mira Cline.", lookAround.Message);

        Assert.True(confrontation.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Fled, confrontation.Outcome);
        Assert.Equal("Mira Cline", confrontation.TargetName);
        Assert.Null(confrontation.Disposition);
        Assert.True(confrontation.IsAlive);
        Assert.False(confrontation.IsSecured);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.False(session.CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out _));

        Assert.True(repeatLookAround.Success);
        Assert.Equal("You look around the saloon again, but nobody of interest is here.", repeatLookAround.Message);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestRejectsWhenNoPersonOfInterestHasBeenSpotted()
    {
        var session = CreateSession();

        var result = session.ConfrontSaloonPersonOfInterest();

        Assert.False(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Rejected, result.Outcome);
        Assert.Contains("saloon", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
    }

    [Fact]
    public void GoneToGroundWantedSuspectCanSurfaceAgainAfterReenteringTown()
    {
        var session = CreateSession();
        var suspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.GoneToGround);

        var firstVisit = session.LookAroundSaloon();

        Assert.True(firstVisit.Success);
        Assert.Equal("You look around the saloon and spot Mira Cline.", firstVisit.Message);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        session.Player.TravelTo(new TownId("connected"));
        session.CurrentTownVisit.Reset(new TownId("connected"));
        session.Player.TravelTo(new TownId("current"));
        session.CurrentTownVisit.Reset(new TownId("current"));

        var secondVisit = session.LookAroundSaloon();

        Assert.True(secondVisit.Success);
        Assert.Equal("You look around the saloon and spot Mira Cline.", secondVisit.Message);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
    }

    [Fact]
    public void LookAroundSaloonDoesNotSurfaceTheTrueCulprit()
    {
        var session = CreateSessionWithoutKnownWarrants();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.Equal("You look around the saloon and spot Mira Cline.", result.Message);
        Assert.Equal(new SuspectId("suspect-1"), session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.NotEqual(new SuspectId("suspect-2"), session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
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
