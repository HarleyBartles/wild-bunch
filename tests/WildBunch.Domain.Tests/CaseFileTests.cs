using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests;

public sealed class CaseFileTests
{
    [Fact]
    public void AddingSameClueTwiceOnlyStoresItOnce()
    {
        var clue = new Clue(new ClueId("clue-1"), ClueKind.Witness, "A rider was seen at dawn.");
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(true, false, true), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-1"),
            knownClues: new[] { clue });

        caseFile.AddClue(clue);
        caseFile.AddClue(clue);

        Assert.Single(caseFile.KnownClues);
        Assert.Equal(clue.Id, caseFile.KnownClues[0].Id);
    }

    [Fact]
    public void DiscoveringSuspectTracksIdAndPreventsDuplicates()
    {
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(true, false, true), SuspectStatus.AtLarge),
                new Suspect(new SuspectId("suspect-2"), "Mira Cline", new SuspectTraits(false, false, false), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-2"),
            knownClues: Array.Empty<Clue>());

        Assert.False(caseFile.IsSuspectDiscovered(new SuspectId("suspect-1")));

        var first = caseFile.DiscoverSuspect(new SuspectId("suspect-1"));
        var duplicate = caseFile.DiscoverSuspect(new SuspectId("suspect-1"));

        Assert.True(first);
        Assert.False(duplicate);
        Assert.True(caseFile.IsSuspectDiscovered(new SuspectId("suspect-1")));
        Assert.Single(caseFile.DiscoveredSuspectIds);
        Assert.Single(caseFile.GetDiscoveredSuspects());
        Assert.Equal("Ira Flint", caseFile.GetDiscoveredSuspects()[0].Name);
    }

    [Fact]
    public void RevealingLinkedPublicClueDiscoversOnlyLinkedSuspects()
    {
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(true, false, true), SuspectStatus.AtLarge),
                new Suspect(new SuspectId("suspect-2"), "Mira Cline", new SuspectTraits(false, false, false), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-2"),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-1"),
                    ClueKind.Witness,
                    "A public poster shows a rider wearing a faded blue scarf.",
                    new[] { new SuspectId("suspect-2") })
            });

        var revealed = caseFile.RevealNextPublicClue();

        Assert.NotNull(revealed);
        Assert.Equal("A public poster shows a rider wearing a faded blue scarf.", revealed!.Description);
        Assert.Single(caseFile.KnownClues);
        Assert.Single(caseFile.DiscoveredSuspectIds);
        Assert.Contains(new SuspectId("suspect-2"), caseFile.DiscoveredSuspectIds);
        Assert.Single(caseFile.GetDiscoveredSuspects());
        Assert.Equal("Mira Cline", caseFile.GetDiscoveredSuspects()[0].Name);
    }

    [Fact]
    public void RevealingPublicClueWithoutLinkedSuspectsDoesNotDiscoverAnyone()
    {
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(true, false, true), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-1"),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(new ClueId("clue-public-1"), ClueKind.Record, "A public notice about a weather report.")
            });

        var revealed = caseFile.RevealNextPublicClue();

        Assert.NotNull(revealed);
        Assert.Empty(caseFile.DiscoveredSuspectIds);
        Assert.Empty(caseFile.GetDiscoveredSuspects());
    }
}
