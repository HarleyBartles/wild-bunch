using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using Xunit;

namespace WildBunch.Domain.Tests;

public sealed class CaseFileGeneratedEventTests
{
    [Fact]
    public void CaseFileGenerated_CarriesCaseFileSnapshotThatReconstructsToIdenticalCaseFile()
    {
        var caseFile = CreateBaselineCaseFile();

        var evt = new CaseFileGenerated
        {
            CaseFile = CaseFileSnapshot.FromDomain(caseFile)
        };

        var reconstructed = evt.CaseFile.ToDomain();

        Assert.Equal(caseFile.Suspects.Count, reconstructed.Suspects.Count);
        for (var i = 0; i < caseFile.Suspects.Count; i++)
        {
            var expected = caseFile.Suspects[i];
            var actual = reconstructed.Suspects[i];
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Status, actual.Status);
            Assert.Equal(expected.Profile.Aliases.Count, actual.Profile.Aliases.Count);
            Assert.Equal(expected.Profile.IdentifyingFacts.Count, actual.Profile.IdentifyingFacts.Count);
            Assert.Equal(expected.Traits.Tags.Count, actual.Traits.Tags.Count);
        }

        Assert.Equal(caseFile.KnownClues.Count, reconstructed.KnownClues.Count);
        for (var i = 0; i < caseFile.KnownClues.Count; i++)
        {
            var expected = caseFile.KnownClues[i];
            var actual = reconstructed.KnownClues[i];
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Description, actual.Description);
            Assert.Equal(expected.TargetKind, actual.TargetKind);
            Assert.Equal(expected.SourceKind, actual.SourceKind);
            Assert.Equal(expected.Source, actual.Source);
            Assert.Equal(expected.Context, actual.Context);
            Assert.Equal(expected.LinkedSuspectIds.Count, actual.LinkedSuspectIds.Count);
        }
    }

    [Fact]
    public void CaseFileGenerated_PreservesTrueCulpritId()
    {
        var caseFile = CreateBaselineCaseFile();

        var evt = new CaseFileGenerated
        {
            CaseFile = CaseFileSnapshot.FromDomain(caseFile)
        };

        var reconstructed = evt.CaseFile.ToDomain();

        Assert.Equal(caseFile.TrueCulpritId, reconstructed.TrueCulpritId);
        Assert.Equal("suspect-2", reconstructed.TrueCulpritId.Value);
    }

    [Fact]
    public void CaseFileGenerated_PreservesOpeningLead()
    {
        var caseFile = CreateBaselineCaseFile();

        var evt = new CaseFileGenerated
        {
            CaseFile = CaseFileSnapshot.FromDomain(caseFile)
        };

        var reconstructed = evt.CaseFile.ToDomain();

        Assert.Equal(caseFile.OpeningLead, reconstructed.OpeningLead);
        Assert.Equal("A pale scar cuts across the left cheek.", reconstructed.OpeningLead.Description);
    }

    [Fact]
    public void CaseFileSnapshot_RoundTrip_Preserves_PublicClues()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint",
                SuspectTraits.FromTags(SuspectTraitTags.Local), SuspectStatus.AtLarge)
        };
        var publicClue = new Clue(
            new ClueId("clue-public-1"),
            ClueKind.Alias,
            "A dusty boot print.",
            new[] { new SuspectId("suspect-1") },
            InvestigationTargetKind.Suspected,
            InvestigationSourceKind.LocalGossip,
            source: "test source",
            context: "test context");

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the trail."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[] { publicClue });

        var snapshot = CaseFileSnapshot.FromDomain(caseFile);
        var restored = snapshot.ToDomain();

        Assert.Single(restored.PublicClues);
        Assert.Equal(publicClue.Id, restored.PublicClues[0].Id);
        Assert.Equal(publicClue.Description, restored.PublicClues[0].Description);
        Assert.Empty(restored.KnownClues);
    }

    /// <summary>
    /// Builds a baseline caseFile with suspects (profiles, aliases, identity facts),
    /// a true culprit, an opening lead, and known clues — exercising the full snapshot
    /// surface carried by <see cref="CaseFileGenerated"/>.
    /// </summary>
    private static CaseFile CreateBaselineCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Ira Flint",
                new SuspectProfile(
                    new[] { new SuspectAlias("Red Ira", AliasKind.Nickname) },
                    new[] { new SuspectIdentityFact(FeatureLanguage.Raw("Has a scar on the left cheek.", "a scar on the left cheek", "has a scar on the left cheek")) }),
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
                SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-2"),
                "Mira Cline",
                SuspectTraits.Empty,
                SuspectStatus.AtLarge)
        };

        var knownClues = new[]
        {
            new Clue(
                new ClueId("clue-1"),
                ClueKind.Alias,
                "Goes by Red Ira in these parts.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                sourceKind: InvestigationSourceKind.LocalGossip,
                source: "the saloon keeper",
                context: "overheard at the bar")
        };

        return new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues);
    }
}
