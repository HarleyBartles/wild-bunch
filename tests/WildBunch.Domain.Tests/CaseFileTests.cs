using WildBunch.Domain.Cases;
using TownId = WildBunch.Domain.World.TownId;

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
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
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
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
                new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
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
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
                new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
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
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
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

    [Fact]
    public void ClueAnchorsCaptureSubjectLocationTimeAndDirection()
    {
        var clue = new Clue(
            new ClueId("clue-anchored"),
            ClueKind.Whereabouts,
            "Local gossip out of Red Mesa says the rider kept to the rail spur after dark.",
            new[] { new SuspectId("suspect-1") },
            InvestigationTargetKind.Suspected,
            InvestigationSourceKind.LocalGossip,
            source: "saloon talk",
            context: "Town gossip",
            anchors: new ClueAnchors(
                subjects: new[]
                {
                    new ClueSubjectAnchor("Grey Jay", Alias: "Grey Jay", Feature: "red neckerchief")
                },
                locations: new[]
                {
                    new ClueLocationAnchor("Red Mesa", TownId: new TownId("redmesa"), Place: "Red Mesa", Route: "rail spur")
                },
                times: new[]
                {
                    new ClueTimeAnchor(ClueRecency.Recent)
                },
                directions: new[]
                {
                    new ClueDirectionAnchor("heading north", Movement: "heading north", Route: "rail spur", DestinationTownId: new TownId("redmesa"))
                }));

        Assert.True(clue.Anchors.HasAnchors);
        Assert.Single(clue.Anchors.Subjects);
        Assert.Equal("Grey Jay", clue.Anchors.Subjects[0].Label);
        Assert.Equal("red neckerchief", clue.Anchors.Subjects[0].Feature);
        Assert.Single(clue.Anchors.Locations);
        Assert.Equal(new TownId("redmesa"), clue.Anchors.Locations[0].TownId);
        Assert.Equal("rail spur", clue.Anchors.Locations[0].Route);
        Assert.Single(clue.Anchors.Times);
        Assert.Equal(ClueRecency.Recent, clue.Anchors.Times[0].Recency);
        Assert.Single(clue.Anchors.Directions);
        Assert.Contains("north", clue.Anchors.Directions[0].Movement, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RevealingPublicCluesDoesNotAdvanceReleaseProgress()
    {
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-1"),
                    ClueKind.Alias,
                    "A posted notice describes a rider wearing a faded blue scarf.",
                    new[] { new SuspectId("suspect-1") }),
                new Clue(
                    new ClueId("clue-public-2"),
                    ClueKind.Record,
                    "A sheriff note ties the rider to a rail ledger.",
                    new[] { new SuspectId("suspect-1") })
            },
            killerReleaseThreshold: 2);

        var first = caseFile.RevealNextPublicClue();
        var second = caseFile.RevealNextPublicClue();
        var duplicate = caseFile.RevealNextPublicClue();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Null(duplicate);
        Assert.Equal(0, caseFile.KillerReleaseProgress);
        Assert.False(caseFile.KillerReleaseState.IsReleased);
    }

    [Fact]
    public void RevealingPublicClueBySourceSkipsMismatchedSources()
    {
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-1"),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-1"),
                    ClueKind.Alias,
                    "A posted notice describes a rider wearing a faded blue scarf.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.GangMember,
                    InvestigationSourceKind.NoticeBoard),
                new Clue(
                    new ClueId("clue-public-2"),
                    ClueKind.Record,
                    "A sheriff note ties the rider to a rail ledger.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.LocalRecords)
            });

        var revealed = caseFile.RevealNextPublicClue(InvestigationSourceKind.LocalRecords);

        Assert.NotNull(revealed);
        Assert.Equal("A sheriff note ties the rider to a rail ledger.", revealed!.Description);
        Assert.Single(caseFile.KnownClues);
        Assert.Single(caseFile.PublicClues);
        Assert.Equal(InvestigationSourceKind.NoticeBoard, caseFile.PublicClues[0].SourceKind);
    }

    [Fact]
    public void DiscoveringSameWarrantTwiceOnlyStoresItOnce()
    {
        var warrant = new Warrant(
            new WarrantId("warrant-1"),
            "Tessa Wren",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                2500m,
                new[] { "Red Wren" },
                new[] { "Pale scar across the left cheek" },
                "Dodge City Marshal",
                InvestigationTargetKind.TrueCulprit,
                [OutlawGangIds.WildBunch],
                OutlawGangIds.WildBunch),
            "Wanted for a Wild Bunch robbery.");

        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Tessa Wren", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-1"),
            knownClues: Array.Empty<Clue>());

        var first = caseFile.DiscoverWarrant(warrant);
        var duplicate = caseFile.DiscoverWarrant(warrant);

        Assert.True(first);
        Assert.False(duplicate);
        Assert.Single(caseFile.KnownWarrants);
        Assert.Equal(warrant.Id, caseFile.KnownWarrants[0].Id);
    }

    [Fact]
    public void RevealingPublicWarrantMovesItIntoKnownWarrantsWithoutDuplication()
    {
        var warrant = new Warrant(
            new WarrantId("warrant-public-1"),
            "Reno Pike",
            new WarrantTerms(
                WarrantDisposition.AliveOnly,
                300m,
                new[] { "The Magpie" },
                new[] { "Mismatched spurs" },
                "Silver Creek Sheriff",
                InvestigationTargetKind.UnrelatedWantedCriminal,
                Array.Empty<OutlawGangId>(),
                null),
            "Wanted for cattle theft.");

        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Reno Pike", SuspectTraits.FromTags(SuspectTraitTags.Local), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            publicWarrants: new[] { warrant });

        var revealed = caseFile.RevealNextPublicWarrant();

        Assert.NotNull(revealed);
        Assert.Equal(warrant.Id, revealed!.Id);
        Assert.Single(caseFile.KnownWarrants);
        Assert.Empty(caseFile.PublicWarrants);
    }

    [Fact]
    public void RevealingPublicWarrantBySourceSkipsMismatchedSources()
    {
        var noticeBoardWarrant = new Warrant(
            new WarrantId("warrant-public-1"),
            "Reno Pike",
            new WarrantTerms(
                WarrantDisposition.AliveOnly,
                300m,
                new[] { "The Magpie" },
                new[] { "Mismatched spurs" },
                "Silver Creek Sheriff",
                InvestigationTargetKind.UnrelatedWantedCriminal,
                Array.Empty<OutlawGangId>(),
                null,
                InvestigationSourceKind.NoticeBoard));

        var sheriffWarrant = new Warrant(
            new WarrantId("warrant-public-2"),
            "Mira Cline",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                2500m,
                new[] { "Red Wren", "Aunt Tess" },
                new[] { "Pale scar across the left cheek" },
                "Dodge City Marshal",
                InvestigationTargetKind.TrueCulprit,
                [OutlawGangIds.WildBunch],
                OutlawGangIds.WildBunch,
                InvestigationSourceKind.SheriffWarrants));

        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Reno Pike", SuspectTraits.FromTags(SuspectTraitTags.Local), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            publicWarrants: new[] { noticeBoardWarrant, sheriffWarrant });

        var revealed = caseFile.RevealNextPublicWarrant(InvestigationSourceKind.SheriffWarrants);

        Assert.NotNull(revealed);
        Assert.Equal(sheriffWarrant.Id, revealed!.Id);
        Assert.Single(caseFile.KnownWarrants);
        Assert.Single(caseFile.PublicWarrants);
        Assert.Equal(InvestigationSourceKind.NoticeBoard, caseFile.PublicWarrants[0].Terms.SourceKind);
    }

    [Fact]
    public void CaseFileExposesReadOnlyCollectionViews()
    {
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Tessa Wren", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-1"),
            knownClues: new[]
            {
                new Clue(new ClueId("clue-1"), ClueKind.CulpritTrail, "A scar is mentioned in the opening lead.")
            });

        Assert.True(caseFile.Suspects is ICollection<Suspect> suspectsCollection && suspectsCollection.IsReadOnly);
        Assert.True(caseFile.KnownClues is ICollection<Clue> cluesCollection && cluesCollection.IsReadOnly);
        Assert.True(caseFile.DiscoveredSuspectIds is ICollection<SuspectId> discoveredCollection && discoveredCollection.IsReadOnly);
        Assert.True(caseFile.KnownWarrants is ICollection<Warrant> warrantsCollection && warrantsCollection.IsReadOnly);
        Assert.True(caseFile.SuspectTurfAssignments is ICollection<SuspectTurfAssignment> turfCollection && turfCollection.IsReadOnly);
    }

    [Fact]
    public void SuspectTurfAssignmentsAreTrackedBySuspectId()
    {
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Tessa Wren", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
                new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-2"),
            knownClues: Array.Empty<Clue>(),
            suspectTurfAssignments: new[]
            {
                new SuspectTurfAssignment(new SuspectId("suspect-1"), new TownId("pinecross")),
                new SuspectTurfAssignment(new SuspectId("suspect-2"), new TownId("redmesa"))
            });

        Assert.Equal(2, caseFile.SuspectTurfAssignments.Count);
        Assert.True(caseFile.TryGetSuspectTurf(new SuspectId("suspect-1"), out var firstTurf));
        Assert.Equal(new TownId("pinecross"), firstTurf);
        Assert.True(caseFile.TryGetSuspectTurf(new SuspectId("suspect-2"), out var secondTurf));
        Assert.Equal(new TownId("redmesa"), secondTurf);
        Assert.False(caseFile.TryGetSuspectTurf(new SuspectId("suspect-3"), out _));
    }
}
