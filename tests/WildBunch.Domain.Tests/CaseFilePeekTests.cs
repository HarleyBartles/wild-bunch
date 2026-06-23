using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests;

public sealed class CaseFilePeekTests
{
    private static CaseFile CreateCaseFileWithPublicClues(params Clue[] publicClues)
        => new(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>(),
            publicClues: publicClues);

    private static CaseFile CreateCaseFileWithPublicCluesAndKnownClues(
        IEnumerable<Clue> knownClues, IEnumerable<Clue> publicClues)
        => new(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), knownClues,
            publicClues: publicClues);

    private static CaseFile CreateCaseFileWithPublicWarrants(params Warrant[] publicWarrants)
        => new(
            accusation: null,
            suspects: Array.Empty<Suspect>(),
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            publicWarrants: publicWarrants);

    private static CaseFile CreateCaseFileWithPublicWarrantsAndKnownWarrants(
        IEnumerable<Warrant> knownWarrants, IEnumerable<Warrant> publicWarrants)
        => new(
            accusation: null,
            suspects: Array.Empty<Suspect>(),
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: knownWarrants,
            publicWarrants: publicWarrants);

    private static Warrant CreateWarrant(string id, InvestigationSourceKind sourceKind = InvestigationSourceKind.SheriffWarrants)
        => new(
            new WarrantId(id),
            "Bill the Outlaw",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                500m,
                Array.Empty<string>(),
                Array.Empty<string>(),
                "Dodge City Marshal",
                InvestigationTargetKind.Suspected,
                Array.Empty<OutlawGangId>(),
                null,
                sourceKind: sourceKind));

    [Fact]
    public void PeekNextPublicClueReturnsMatchingClueWithoutRemovingIt()
    {
        var clue = new Clue(new ClueId("clue-1"), ClueKind.Record, "A dusty boot print.");
        var caseFile = CreateCaseFileWithPublicClues(clue);

        var peekedAny = caseFile.PeekNextPublicClue(_ => true);
        Assert.NotNull(peekedAny);
        Assert.Equal(clue.Id, peekedAny!.Id);

        // Verify it was NOT removed
        Assert.Single(caseFile.PublicClues);
    }

    [Fact]
    public void PeekNextPublicClueSkipsAlreadyKnownClues()
    {
        var clue = new Clue(new ClueId("clue-1"), ClueKind.Record, "A dusty boot print.");
        var caseFile = CreateCaseFileWithPublicCluesAndKnownClues(
            knownClues: new[] { clue },
            publicClues: new[] { clue });

        var peeked = caseFile.PeekNextPublicClue(_ => true);
        Assert.Null(peeked);
    }

    [Fact]
    public void RevealClueRemovesFromPublicPoolAndDiscoversIt()
    {
        var clue = new Clue(new ClueId("clue-1"), ClueKind.Record, "A dusty boot print.");
        var caseFile = CreateCaseFileWithPublicClues(clue);

        caseFile.RevealClue(clue);

        Assert.Contains(clue, caseFile.KnownClues);
        Assert.DoesNotContain(clue, caseFile.PublicClues);
    }

    [Fact]
    public void RevealClueAlsoCleansStaleKnownEntriesFromPublicPool()
    {
        var clue1 = new Clue(new ClueId("clue-1"), ClueKind.Record, "Boot print.");
        var clue2 = new Clue(new ClueId("clue-2"), ClueKind.Witness, "A stranger asked about the sheriff.");
        var caseFile = CreateCaseFileWithPublicCluesAndKnownClues(
            knownClues: new[] { clue1 },
            publicClues: new[] { clue1, clue2 });

        caseFile.RevealClue(clue2);

        Assert.Contains(clue2, caseFile.KnownClues);
        Assert.DoesNotContain(clue2, caseFile.PublicClues);
        Assert.DoesNotContain(clue1, caseFile.PublicClues);
    }

    [Fact]
    public void RevealWarrantRemovesFromPublicPoolAndDiscoversIt()
    {
        var warrant = new Warrant(
            new WarrantId("w-1"),
            "Bill the Outlaw",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                500m,
                Array.Empty<string>(),
                Array.Empty<string>(),
                "Dodge City Marshal",
                InvestigationTargetKind.Suspected,
                Array.Empty<OutlawGangId>(),
                null,
                sourceKind: InvestigationSourceKind.SheriffWarrants));
        var caseFile = CreateCaseFileWithPublicWarrants(warrant);

        caseFile.RevealWarrant(warrant);

        Assert.Contains(warrant, caseFile.KnownWarrants);
        Assert.DoesNotContain(warrant, caseFile.PublicWarrants);
    }

    [Fact]
    public void PeekNextPublicWarrantReturnsMatchingWarrantWithoutRemovingIt()
    {
        var warrant = CreateWarrant("w-1");
        var caseFile = CreateCaseFileWithPublicWarrants(warrant);

        var peeked = caseFile.PeekNextPublicWarrant();

        Assert.NotNull(peeked);
        Assert.Equal(warrant.Id, peeked!.Id);

        // Verify it was NOT removed from the public pool.
        Assert.Single(caseFile.PublicWarrants);
        Assert.DoesNotContain(warrant, caseFile.KnownWarrants);
    }

    [Fact]
    public void PeekNextPublicWarrantSkipsAlreadyKnownWarrants()
    {
        var known = CreateWarrant("w-1");
        var next = CreateWarrant("w-2");
        var caseFile = CreateCaseFileWithPublicWarrantsAndKnownWarrants(
            knownWarrants: new[] { known },
            publicWarrants: new[] { known, next });

        var peeked = caseFile.PeekNextPublicWarrant();

        Assert.NotNull(peeked);
        Assert.Equal(next.Id, peeked!.Id);

        // The already-known warrant stays in the public pool (peek is non-mutating).
        Assert.Contains(known, caseFile.PublicWarrants);
        Assert.Contains(next, caseFile.PublicWarrants);
    }
}
