using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests;

public sealed class CaseProgressTests
{
    [Fact]
    public void NewCaseStartsWithOpeningLeadAndLockedRelease()
    {
        var caseFile = CreateCaseFile();

        Assert.Equal("A rider bears a pale scar across the left cheek.", caseFile.OpeningLead.Description);
        Assert.False(caseFile.KillerReleaseState.IsReleased);
        Assert.Equal(0, caseFile.KillerReleaseState.Progress);
        Assert.Equal(2, caseFile.KillerReleaseState.RequiredPublicClues);
    }

    [Fact]
    public void RevealingNewPublicCluesAdvancesReleaseProgressButDuplicateReadsDoNot()
    {
        var caseFile = CreateCaseFile();

        var first = caseFile.RevealNextPublicClue();
        var second = caseFile.RevealNextPublicClue();
        var duplicate = caseFile.RevealNextPublicClue();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Null(duplicate);
        Assert.Equal(2, caseFile.KillerReleaseState.Progress);
        Assert.True(caseFile.KillerReleaseState.IsReleased);
    }

    [Fact]
    public void SuspectProfilesExposeAliasesAndIdentityFacts()
    {
        var suspect = new Suspect(
            new SuspectId("suspect-1"),
            "Tessa Wren",
            new SuspectProfile(
                new[]
                {
                    new SuspectAlias("Red Wren", AliasKind.Nickname)
                },
                new[]
                {
                    new SuspectIdentityFact("A pale scar cuts across the left cheek.")
                }),
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed),
            SuspectStatus.AtLarge);

        Assert.Single(suspect.Profile.Aliases);
        Assert.Single(suspect.Profile.IdentifyingFacts);
        Assert.Equal("Red Wren", suspect.Profile.Aliases[0].Name);
        Assert.Equal(AliasKind.Nickname, suspect.Profile.Aliases[0].Kind);
        Assert.Equal("A pale scar cuts across the left cheek.", suspect.Profile.IdentifyingFacts[0].Description);
    }

    [Fact]
    public void SuspectTraitsExposeTagsAndDeriveLegacyFlags()
    {
        var traits = SuspectTraits.FromTags(
            SuspectTraitTags.Local,
            SuspectTraitTags.Armed,
            SuspectTraitTags.Desperate,
            SuspectTraitTags.Local);

        Assert.Equal(3, traits.Tags.Count);
        Assert.Contains(traits.Tags, tag => tag.Value == SuspectTraitTags.Local.Value);
        Assert.Contains(traits.Tags, tag => tag.Value == SuspectTraitTags.Armed.Value);
        Assert.Contains(traits.Tags, tag => tag.Value == SuspectTraitTags.Desperate.Value);
        Assert.True(traits.IsLocal);
        Assert.True(traits.IsArmed);
        Assert.True(traits.IsDesperate);
        Assert.True(traits.HasTag(SuspectTraitTags.Local));
        Assert.False(traits.HasTag(SuspectTraitTags.Lookout));
    }

    private static CaseFile CreateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Jonah Pike",
                new SuspectProfile(Array.Empty<SuspectAlias>(), Array.Empty<SuspectIdentityFact>()),
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
                SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-2"),
                "Tessa Wren",
                new SuspectProfile(
                    new[] { new SuspectAlias("Red Wren", AliasKind.Nickname) },
                    new[] { new SuspectIdentityFact("A pale scar cuts across the left cheek.") }),
                SuspectTraits.FromTags(SuspectTraitTags.Armed),
                SuspectStatus.AtLarge)
        };

        return new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A rider bears a pale scar across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-1"),
                    ClueKind.Witness,
                    "A wanted poster shows a rider with a red feather pin.",
                    new[] { new SuspectId("suspect-2") }),
                new Clue(new ClueId("clue-public-2"), ClueKind.Record, "A notice records a rider near the river crossing.")
            },
            killerReleaseThreshold: 2);
    }
}
