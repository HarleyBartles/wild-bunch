using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Domain.Tests;

public sealed class ClueSurfacingResolverTests
{
    [Fact]
    public void BoringMode_ReturnsClueMatchingSurfaceTag()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();

        var clue = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, townSlotIndex: 0, visitCount: 0, salt: null);

        Assert.NotNull(clue);
        Assert.Equal(InvestigationSourceKind.TelegraphLead, clue!.SourceKind);
    }

    [Fact]
    public void BoringMode_AlreadyKnownCluesAreSkipped()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();

        var first = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, 0, 0, salt: null);
        Assert.NotNull(first);
        caseFile.RevealClue(first!);

        var next = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, 0, 0, salt: null);
        // May be null if no more telegraph clues, or a different telegraph clue
        if (next is not null)
        {
            Assert.NotEqual(first!.Id, next.Id);
        }
    }

    [Fact]
    public void BoringMode_SameInputs_ReturnsSameClue()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();

        var first = resolver.Resolve(caseFile, InvestigationSourceKind.LocalGossip, 2, 1, salt: null);
        var second = resolver.Resolve(caseFile, InvestigationSourceKind.LocalGossip, 2, 1, salt: null);

        Assert.Equal(first?.Id, second?.Id);
    }

    [Fact]
    public void ReturnsNullWhenNoCluesMatchSurface()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();

        var clue = resolver.Resolve(caseFile, InvestigationSourceKind.SheriffWarrants, 0, 0, salt: null);

        // No SheriffWarrants-tagged clues exist in the base 6, so this returns null.
        Assert.Null(clue);
    }

    [Fact]
    public void SaltMode_IsDeterministicForSameInputs()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();
        var salt = SaltSource.CreateFixed("deadbeef");

        var first = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, 1, 2, salt);
        var second = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, 1, 2, salt);

        Assert.NotNull(first);
        Assert.Equal(first!.Id, second?.Id);
    }

    [Fact]
    public void SaltMode_ReturnsClueMatchingSurfaceTag()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();
        var salt = SaltSource.CreateFixed("cafe");

        var clue = resolver.Resolve(caseFile, InvestigationSourceKind.LocalGossip, 0, 0, salt);

        Assert.NotNull(clue);
        Assert.Equal(InvestigationSourceKind.LocalGossip, clue!.SourceKind);
    }

    [Fact]
    public void SaltMode_DifferentSaltCanSelectDifferentClue()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();

        // Two distinct fixed salts over the two telegraph clues should be able to
        // land on different indices for at least one (townSlotIndex, visitCount) pair.
        var saltA = SaltSource.CreateFixed("salt-a");
        var saltB = SaltSource.CreateFixed("salt-b");

        var cluesA = new HashSet<string>();
        var cluesB = new HashSet<string>();
        for (var town = 0; town < 5; town++)
        {
            for (var visit = 0; visit < 5; visit++)
            {
                var a = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, town, visit, saltA);
                var b = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, town, visit, saltB);
                if (a is not null) cluesA.Add(a.Id.Value);
                if (b is not null) cluesB.Add(b.Id.Value);
            }
        }

        // The two salts should not select the exact same single clue across all input pairs;
        // both telegraph clues should be reachable under at least one salt.
        Assert.True(cluesA.Count > 1 || cluesB.Count > 1);
    }

    [Fact]
    public void BoringMode_IndexFollowsTownSlotPlusVisitModulo()
    {
        // With two telegraph clues, (townSlotIndex + visitCount) % 2 selects the index.
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();

        var atZero = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, 0, 0, salt: null);
        var atOne = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, 1, 0, salt: null);

        Assert.NotNull(atZero);
        Assert.NotNull(atOne);
        Assert.NotEqual(atZero!.Id, atOne!.Id);
    }

    private static CaseFile BuildTestCaseFile()
    {
        var suspect = new Suspect(
            new SuspectId("suspect-1"),
            "Ira Flint",
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
            SuspectStatus.AtLarge);

        var publicClues = new[]
        {
            new Clue(
                new ClueId("clue-tel-1"),
                ClueKind.Record,
                "A telegraph wire names a rider heading for the rail spur.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.TelegraphLead),
            new Clue(
                new ClueId("clue-tel-2"),
                ClueKind.Witness,
                "A telegraph clerk recalls a tall stranger paying cash for a wire.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.TelegraphLead),
            new Clue(
                new ClueId("clue-gossip-1"),
                ClueKind.Rumor,
                "Saloon talk says the rider wore a faded blue scarf.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.LocalGossip),
            new Clue(
                new ClueId("clue-gossip-2"),
                ClueKind.Rumor,
                "A barkeep mentions the stranger asked after the northern pass.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.LocalGossip),
            new Clue(
                new ClueId("clue-records-1"),
                ClueKind.Record,
                "A county ledger lists a horse sold to a cash-paying stranger.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.LocalRecords),
            new Clue(
                new ClueId("clue-notice-1"),
                ClueKind.Alias,
                "A posted notice describes a rider wearing a faded blue scarf.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.GangMember,
                InvestigationSourceKind.NoticeBoard)
        };

        return new CaseFile(
            accusation: null,
            suspects: new[] { suspect },
            trueCulpritId: new SuspectId("suspect-1"),
            knownClues: Array.Empty<Clue>(),
            publicClues: publicClues);
    }
}
