using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class JournalResolverTests
{
    [Fact]
    public void ResolverReturnsCurrentTownClockStatusCaseAndLogs()
    {
        var session = CreateSession();
        var resolver = new JournalResolver();

        var result = resolver.Resolve(session);

        Assert.Equal(session.Id.Value, result.SessionId);
        Assert.Equal(session.Status, result.Status);
        Assert.Equal(session.Clock.Day, result.Day);
        Assert.Equal(session.Clock.Turn, result.Turn);
        Assert.Equal(session.Player.CurrentTownId, result.CurrentTownId);
        Assert.Equal("Pinecross", result.CurrentTownName);
        Assert.Equal(session.CaseFile.Accusation?.Value, result.AccusationId);
        Assert.Equal(session.CaseFile.OpeningLead.Description, result.OpeningLead);
        Assert.Equal(session.CaseFile.KillerReleaseState.IsReleased, result.KillerReleaseState.IsReleased);
        Assert.Equal(session.CaseFile.KillerReleaseState.Progress, result.KillerReleaseState.Progress);
        Assert.Equal(session.CaseFile.KillerReleaseState.RequiredPublicClues, result.KillerReleaseState.RequiredPublicClues);
        Assert.Equal("Find the culprit before the law closes in.", result.CaseSummary);
        Assert.Equal(session.CaseFile.Suspects.Count, result.Suspects.Count);
        Assert.Equal(session.CaseFile.KnownClues.Count, result.KnownClues.Count);
        Assert.Equal(session.LogEntries.Count, result.LogEntries.Count);
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.TrueCulpritId);
    }

    [Fact]
    public void ResolverDoesNotMutateSession()
    {
        var session = CreateSession();
        var resolver = new JournalResolver();

        var beforeTownId = session.Player.CurrentTownId;
        var beforeDay = session.Clock.Day;
        var beforeTurn = session.Clock.Turn;
        var beforeLogCount = session.LogEntries.Count;
        var beforeSuspectCount = session.CaseFile.Suspects.Count;
        var beforeClueCount = session.CaseFile.KnownClues.Count;

        _ = resolver.Resolve(session);

        Assert.Equal(beforeTownId, session.Player.CurrentTownId);
        Assert.Equal(beforeDay, session.Clock.Day);
        Assert.Equal(beforeTurn, session.Clock.Turn);
        Assert.Equal(beforeLogCount, session.LogEntries.Count);
        Assert.Equal(beforeSuspectCount, session.CaseFile.Suspects.Count);
        Assert.Equal(beforeClueCount, session.CaseFile.KnownClues.Count);
        Assert.Equal("A rider with a pale scar across the left cheek.", session.CaseFile.OpeningLead.Description);
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.TrueCulpritId);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, SupplyCost: 2, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Jonah Pike", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", new SuspectTraits(IsLocal: false, IsArmed: false, IsDesperate: false), SuspectStatus.AtLarge)
        };

        var clues = new[]
        {
            new Clue(new ClueId("clue-1"), ClueKind.Witness, "A rider was seen leaving at dusk."),
            new Clue(new ClueId("clue-2"), ClueKind.Record, "A coded telegraph entry was logged.")
        };

        var caseFile = new CaseFile(
            accusation: new SuspectId("suspect-2"),
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A rider with a pale scar across the left cheek."),
            knownClues: clues);

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
    }
}
