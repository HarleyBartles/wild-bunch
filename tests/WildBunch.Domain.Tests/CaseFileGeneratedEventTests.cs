using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using TownId = WildBunch.Domain.World.TownId;
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

    [Fact]
    public void CaseFileSnapshot_RoundTrip_Preserves_KnownWarrants()
    {
        var warrant = new Warrant(
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
            "Wanted for a stage robbery.");

        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[] { new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge) },
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the trail."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: new[] { warrant });

        var snapshot = CaseFileSnapshot.FromDomain(caseFile);
        var restored = snapshot.ToDomain();

        Assert.Single(restored.KnownWarrants);
        Assert.Equal(warrant.Id, restored.KnownWarrants[0].Id);
        Assert.Equal(warrant.TargetName, restored.KnownWarrants[0].TargetName);
        Assert.Equal(warrant.Terms.BountyAmount, restored.KnownWarrants[0].Terms.BountyAmount);
        Assert.Equal(warrant.Terms.Disposition, restored.KnownWarrants[0].Terms.Disposition);
    }

    [Fact]
    public void CaseFileSnapshot_RoundTrip_Preserves_KillerReleaseGate()
    {
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[] { new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge) },
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the trail."),
            knownClues: Array.Empty<Clue>(),
            killerReleaseThreshold: 3,
            killerReleaseProgress: 2);

        var snapshot = CaseFileSnapshot.FromDomain(caseFile);
        var restored = snapshot.ToDomain();

        Assert.Equal(3, restored.KillerReleaseThreshold);
        Assert.Equal(2, restored.KillerReleaseProgress);
    }

    [Fact]
    public void CaseFileSnapshot_RoundTrip_Preserves_PublicWarrants()
    {
        var warrant = new Warrant(
            new WarrantId("warrant-pub-1"),
            "Mira Cline",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                2500m,
                new[] { "Red Wren" },
                new[] { "Raven-feather pin" },
                "Dodge City Marshal",
                InvestigationTargetKind.GangMember,
                new[] { OutlawGangIds.WildBunch },
                OutlawGangIds.WildBunch,
                InvestigationSourceKind.SheriffWarrants),
            "Wanted for a Wild Bunch robbery.");

        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[] { new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge) },
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the trail."),
            knownClues: Array.Empty<Clue>(),
            publicWarrants: new[] { warrant });

        var snapshot = CaseFileSnapshot.FromDomain(caseFile);
        var restored = snapshot.ToDomain();

        Assert.Single(restored.PublicWarrants);
        Assert.Equal(warrant.Id, restored.PublicWarrants[0].Id);
    }

    [Fact]
    public void CaseFileSnapshot_RoundTrip_Preserves_SuspectTurfAssignments()
    {
        var turf = new SuspectTurfAssignment(new SuspectId("suspect-1"), new TownId("pinecross"));
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[] { new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge) },
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the trail."),
            knownClues: Array.Empty<Clue>(),
            suspectTurfAssignments: new[] { turf });

        var snapshot = CaseFileSnapshot.FromDomain(caseFile);
        var restored = snapshot.ToDomain();

        Assert.Single(restored.SuspectTurfAssignments);
        Assert.Equal(turf.SuspectId, restored.SuspectTurfAssignments[0].SuspectId);
        Assert.Equal(turf.TurfTownId, restored.SuspectTurfAssignments[0].TurfTownId);
    }

    [Fact]
    public void CaseFileSnapshot_RoundTrip_Preserves_DiscoveredSuspectIds()
    {
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[] { new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge) },
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the trail."),
            knownClues: Array.Empty<Clue>(),
            discoveredSuspectIds: new[] { new SuspectId("suspect-1") });

        var snapshot = CaseFileSnapshot.FromDomain(caseFile);
        var restored = snapshot.ToDomain();

        Assert.Single(restored.DiscoveredSuspectIds);
        Assert.Equal(new SuspectId("suspect-1"), restored.DiscoveredSuspectIds[0]);
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
