using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests;

public sealed class CaseInvestigationFoundationTests
{
    [Fact]
    public void CaseFileRepresentsGangCluesWarrantsAndIdempotentDiscovery()
    {
        var culpritTrailClue = new Clue(
            new ClueId("clue-culprit-trail"),
            ClueKind.CulpritTrail,
            "A pale scar across the left cheek caught the lantern light.",
            new[] { new SuspectId("suspect-true") },
            InvestigationTargetKind.TrueCulprit,
            source: "trail witness",
            context: "Opening lead");

        var aliasClue = new Clue(
            new ClueId("clue-alias"),
            ClueKind.Alias,
            "A rider answered to the nickname Grey Jay.",
            new[] { new SuspectId("suspect-gang") },
            InvestigationTargetKind.GangMember,
            source: "notice board",
            context: "Alias match");

        var whereaboutsClue = new Clue(
            new ClueId("clue-whereabouts"),
            ClueKind.Whereabouts,
            "Boot prints place the rider on the Red Mesa road after dusk.",
            new[] { new SuspectId("suspect-true") },
            InvestigationTargetKind.TrueCulprit,
            source: "waystation clerk",
            context: "Route lead");

        var gangWarrant = new Warrant(
            new WarrantId("warrant-gang"),
            "Tessa Wren",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                2500m,
                new[] { "Red Wren", "Aunt Tess" },
                new[] { "Pale scar across the left cheek", "Raven-feather pin" },
                "Dodge City Marshal",
                InvestigationTargetKind.TrueCulprit,
                isGangRelevant: true,
                advancesGangPressure: true),
            "Wanted for a Wild Bunch robbery and related killings.");

        var unrelatedWarrant = new Warrant(
            new WarrantId("warrant-unrelated"),
            "Reno Pike",
            new WarrantTerms(
                WarrantDisposition.AliveOnly,
                300m,
                new[] { "The Magpie", "R. Pike" },
                new[] { "Mismatched spurs", "Black felt hat" },
                "Silver Creek Sheriff",
                InvestigationTargetKind.UnrelatedWantedCriminal,
                isGangRelevant: false,
                advancesGangPressure: false),
            "Wanted for cattle theft and forging livery tags.");

        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-true"), "Tessa Wren", new SuspectTraits(true, true, true), SuspectStatus.AtLarge),
                new Suspect(new SuspectId("suspect-gang"), "Jonah Pike", new SuspectTraits(true, false, false), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-true"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: new[] { culpritTrailClue },
            publicClues: new[] { whereaboutsClue },
            publicWarrants: new[] { gangWarrant, unrelatedWarrant });

        Assert.Equal(ClueKind.CulpritTrail, caseFile.KnownClues[0].Kind);
        Assert.Equal(InvestigationTargetKind.TrueCulprit, caseFile.KnownClues[0].TargetKind);
        Assert.Equal("trail witness", caseFile.KnownClues[0].Source);
        Assert.Equal("Opening lead", caseFile.KnownClues[0].Context);

        Assert.Equal(ClueKind.Whereabouts, caseFile.PublicClues[0].Kind);
        Assert.Equal(InvestigationTargetKind.TrueCulprit, caseFile.PublicClues[0].TargetKind);

        Assert.Single(caseFile.PublicWarrants, warrant => warrant.Terms.IsGangRelevant);
        Assert.Single(caseFile.PublicWarrants, warrant => warrant.Terms.TargetKind == InvestigationTargetKind.UnrelatedWantedCriminal);
        Assert.Equal(2500m, caseFile.PublicWarrants[0].Terms.BountyAmount);
        Assert.Equal(WarrantDisposition.DeadOrAlive, caseFile.PublicWarrants[0].Terms.Disposition);
        Assert.Equal(WarrantDisposition.AliveOnly, caseFile.PublicWarrants[1].Terms.Disposition);

        var discovered = caseFile.DiscoverClue(aliasClue);
        var duplicate = caseFile.DiscoverClue(aliasClue);

        Assert.True(discovered);
        Assert.False(duplicate);
        Assert.Single(caseFile.KnownClues, clue => clue.Id.Equals(aliasClue.Id));
        Assert.Equal(2, caseFile.KnownClues.Count);
        Assert.Contains(caseFile.KnownClues, clue => clue.Kind == ClueKind.Alias);
    }
}
