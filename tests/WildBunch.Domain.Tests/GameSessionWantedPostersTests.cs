using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.WantedPosters;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionWantedPostersTests
{
    [Fact]
    public void ReadingWantedPostersInSupportedTownAddsPublicClueAndLogEntry()
    {
        var session = CreateSession(TownServices.NoticeBoard);

        var result = session.ReadWantedPosters();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(2, session.LogEntries.Count);
        Assert.Single(session.CaseFile.KnownClues);
        Assert.Empty(session.CaseFile.PublicClues);
        Assert.Single(session.CaseFile.DiscoveredSuspectIds);
        Assert.Contains(new SuspectId("suspect-1"), session.CaseFile.DiscoveredSuspectIds);
        Assert.Equal(1, session.CaseFile.KillerReleaseProgress);
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.TrueCulpritId);
    }

    [Fact]
    public void ReadingWantedPostersTwiceDoesNotDuplicateTheSameClue()
    {
        var session = CreateSession(TownServices.NoticeBoard);

        var first = session.ReadWantedPosters();
        var second = session.ReadWantedPosters();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(2, session.Clock.Turn);
        Assert.Equal(3, session.LogEntries.Count);
        Assert.Single(session.CaseFile.KnownClues);
        Assert.Empty(session.CaseFile.PublicClues);
        Assert.Single(session.CaseFile.DiscoveredSuspectIds);
        Assert.Equal(1, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void ReadingWantedPostersInUnsupportedTownFailsAndDoesNotMutateClues()
    {
        var session = CreateSession(TownServices.None);

        var result = session.ReadWantedPosters();

        Assert.False(result.Success);
        Assert.False(result.SessionChanged);
        Assert.Empty(session.CaseFile.KnownClues);
        Assert.Single(session.CaseFile.PublicClues);
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Single(session.LogEntries);
    }

    private static GameSession CreateSession(TownServices currentTownServices)
    {
        var currentTown = new Town(new TownId("current"), "Current Town", currentTownServices);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[]
            {
                new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", new SuspectTraits(IsLocal: false, IsArmed: false, IsDesperate: false), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-1"),
                    ClueKind.Witness,
                    "A posted notice describes a rider wearing a faded blue scarf.",
                    new[] { new SuspectId("suspect-1") })
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }
}
