using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests.Mappers;

public sealed class CaseBoardMapperTests
{
    [Fact]
    public void WantedPosterResolvesLooseKnownNameLeadIntoNamedRecord()
    {
        var board = CaseBoardMapper.ToDto(
            new[]
            {
                new Clue(
                    new ClueId("clue-alias"),
                    ClueKind.Alias,
                    "A poster links the alias Grey Jay to a rider in the county line files.",
                    Array.Empty<SuspectId>(),
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.SheriffWarrants,
                    source: "wanted poster",
                    context: "Public notice",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("Grey Jay", Alias: "Grey Jay")
                        }))
            },
            new[]
            {
                new Warrant(
                    new WarrantId("warrant-butch"),
                    "Butch Cassidy",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        2500m,
                        new[] { "Grey Jay" },
                        new[] { "red hat" },
                        "County marshal",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(),
                        null,
                        InvestigationSourceKind.SheriffWarrants),
                    "Wanted for a string of robberies near the county line.")
            });

        var namedRecord = Assert.Single(board.NamedRecords, record => record.DisplayName == "Butch Cassidy");
        Assert.Equal(CaseIdentityKind.WarrantTarget, namedRecord.Kind);
        Assert.Equal(CaseIdentityStatus.Resolved, namedRecord.Status);
        Assert.Contains("Grey Jay", namedRecord.KnownAliases);
        Assert.Contains("red hat", namedRecord.DistinguishingFeatures);
        Assert.Equal(WarrantDisposition.DeadOrAlive, namedRecord.WarrantDisposition);
        Assert.Equal(2500m, namedRecord.BountyAmount);
        Assert.Equal("County marshal", namedRecord.IssuingAuthority);
        Assert.Contains("Wanted for a string of robberies near the county line.", namedRecord.CrimeSummary);
        Assert.Contains(namedRecord.SummaryLines, line => line.Contains("Dead or alive warrant", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Grey Jay", namedRecord.RelatedLabels);
        Assert.Contains("red hat", namedRecord.RelatedLabels);
        Assert.Contains(namedRecord.SummaryLines, line => line.Contains("County marshal", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(board.LooseLeads);
    }

    [Fact]
    public void KnownNameLeadWithoutNamedEvidenceStaysLoose()
    {
        var board = CaseBoardMapper.ToDto(
            new[]
            {
                new Clue(
                    new ClueId("clue-alias"),
                    ClueKind.Alias,
                    "A poster links the alias Grey Jay to a rider in the county line files.",
                    Array.Empty<SuspectId>(),
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.SheriffWarrants,
                    source: "wanted poster",
                    context: "Public notice",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("Grey Jay", Alias: "Grey Jay")
                        }))
            },
            Array.Empty<Warrant>());

        var looseLead = Assert.Single(board.LooseLeads, record => record.DisplayName == "Grey Jay");
        Assert.Equal(CaseIdentityKind.Alias, looseLead.Kind);
        Assert.Equal(CaseIdentityStatus.Unresolved, looseLead.Status);
        Assert.Contains(looseLead.SummaryLines, line => line.Contains("Grey Jay", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(board.NamedRecords);
    }

    [Fact]
    public void RouteOnlyObservationCreatesRouteLead()
    {
        var board = CaseBoardMapper.ToDto(
            new[]
            {
                new Clue(
                    new ClueId("clue-color"),
                    ClueKind.Whereabouts,
                    "A rider turned north at dusk.",
                    Array.Empty<SuspectId>(),
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.LocalGossip,
                    source: "saloon talk",
                    context: "Town gossip",
                    anchors: new ClueAnchors(
                        locations: new[]
                        {
                            new ClueLocationAnchor("North road", Place: "North road", Route: "North road")
                        },
                        times: new[]
                        {
                            new ClueTimeAnchor(ClueRecency.Yesterday, Day: 2)
                        },
                        directions: new[]
                        {
                            new ClueDirectionAnchor("north", Movement: "turned north", Route: "North road")
                        }))
            },
            Array.Empty<Warrant>());

        Assert.Empty(board.NamedRecords);
        var looseLead = Assert.Single(board.LooseLeads);
        Assert.Equal("Rider on North road", looseLead.DisplayName);
        Assert.Equal(CaseIdentityKind.RouteLed, looseLead.Kind);
        Assert.Single(board.EvidenceItems);
        Assert.True(board.EvidenceItems[0].IdentityBearing);
        Assert.Single(board.EvidenceItems[0].HandleIds);
        Assert.Equal("North road", board.EvidenceItems[0].Anchors.Locations[0].Route);
    }

    [Fact]
    public void FeatureAndRouteClueYieldsOneFeatureLeadWithRouteEvidence()
    {
        var board = CaseBoardMapper.ToDto(
            new[]
            {
                new Clue(
                    new ClueId("clue-feature-route"),
                    ClueKind.Whereabouts,
                    "Local gossip says the rider with no eyebrows kept to the rail spur after dark.",
                    Array.Empty<SuspectId>(),
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.LocalGossip,
                    source: "saloon talk",
                    context: "Town gossip",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("Has no eyebrows", Feature: "Has no eyebrows", Fact: "opening lead")
                        },
                        locations: new[]
                        {
                            new ClueLocationAnchor("Red Mesa road", Place: "Red Mesa road", Route: "rail spur")
                        },
                        directions: new[]
                        {
                            new ClueDirectionAnchor("kept to the rail spur after dark", Movement: "kept to the rail spur after dark", Route: "rail spur")
                        }))
            },
            Array.Empty<Warrant>());

        var looseLead = Assert.Single(board.LooseLeads);
        Assert.Equal("Rider with no eyebrows", looseLead.DisplayName);
        Assert.Equal(CaseIdentityKind.FeatureLed, looseLead.Kind);
        Assert.DoesNotContain(board.LooseLeads, lead => lead.DisplayName == "Rider on rail spur");
        Assert.DoesNotContain(board.LooseLeads, lead => lead.DisplayName.Contains("opening lead", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(board.LooseLeads, lead => lead.DisplayName.Contains("identity match", StringComparison.OrdinalIgnoreCase));
        Assert.Single(board.EvidenceItems);
        Assert.True(board.EvidenceItems[0].IdentityBearing);
        Assert.Single(board.EvidenceItems[0].HandleIds);
        Assert.Equal("rail spur", board.EvidenceItems[0].Anchors.Locations[0].Route);
    }

    [Fact]
    public void NameAndFeatureClueYieldsOneKnownNameLeadWithFeatureEvidence()
    {
        var board = CaseBoardMapper.ToDto(
            new[]
            {
                new Clue(
                    new ClueId("clue-name-feature"),
                    ClueKind.Alias,
                    "A poster links Grey Jay to a rider who has no eyebrows.",
                    Array.Empty<SuspectId>(),
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.SheriffWarrants,
                    source: "wanted poster",
                    context: "Public notice",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("Grey Jay", Alias: "Grey Jay", Feature: "Has no eyebrows")
                        },
                        locations: new[]
                        {
                            new ClueLocationAnchor("Red Mesa road", Place: "Red Mesa road", Route: "rail spur")
                        }))
            },
            Array.Empty<Warrant>());

        var looseLead = Assert.Single(board.LooseLeads);
        Assert.Equal("Grey Jay", looseLead.DisplayName);
        Assert.Equal(CaseIdentityKind.Alias, looseLead.Kind);
        Assert.DoesNotContain(board.LooseLeads, lead => lead.DisplayName == "Rider with no eyebrows");
        Assert.Single(board.EvidenceItems);
        Assert.True(board.EvidenceItems[0].IdentityBearing);
        Assert.Single(board.EvidenceItems[0].HandleIds);
        Assert.Equal("Has no eyebrows", board.EvidenceItems[0].Anchors.Subjects[0].Feature);
    }

    [Fact]
    public void CapturedWantedTurnInMarksTheWantedRecordAndCollapsesItsIdentityEvidence()
    {
        var session = CreateArmedWantedSessionWithIdentityEvidence();
        var capturedSuspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(capturedSuspectId, WantedSuspectPresenceState.AvailableInTown);

        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(capturedSuspectId));
        session.MarkEventsCommitted();

        var lookAround = session.LookAroundSaloon();
        var turnIn = session.ConfrontSaloonPersonOfInterest("warrant-mira");
        var caseFile = GameSessionMapper.ToDto(session).CaseFile;

        Assert.True(lookAround.Success);
        Assert.True(turnIn.Success);
        Assert.Single(session.CaseFile.SheriffTurnInSettlements);

        var capturedRecord = Assert.Single(caseFile.CaseBoard.NamedRecords, record => record.DisplayName == "Mira Cline");
        Assert.Equal(CaseIdentityStatus.Captured, capturedRecord.Status);
        Assert.Contains(capturedRecord.SummaryLines, line => line.Contains("captured", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(WarrantDisposition.DeadOrAlive, capturedRecord.WarrantDisposition);
        Assert.Equal(2500m, capturedRecord.BountyAmount);
        Assert.Contains("Red Wren", capturedRecord.KnownAliases);
        Assert.Contains("Raven-feather pin", capturedRecord.DistinguishingFeatures);

        Assert.Contains(caseFile.KnownClues, clue => clue.Id == "clue-mira-alias");
        Assert.Contains(caseFile.KnownClues, clue => clue.Id == "clue-mira-feature");
        Assert.Contains(session.CaseFile.KnownWarrants, warrant => warrant.Id.Value == "warrant-mira");
        Assert.DoesNotContain(caseFile.CaseBoard.EvidenceItems, evidence => evidence.Id == "clue-mira-alias");
        Assert.DoesNotContain(caseFile.CaseBoard.EvidenceItems, evidence => evidence.Id == "clue-mira-feature");

        var activeRecord = Assert.Single(caseFile.CaseBoard.NamedRecords, record => record.DisplayName == "Reno Pike");
        Assert.Equal(CaseIdentityStatus.Resolved, activeRecord.Status);
        Assert.Contains(caseFile.CaseBoard.EvidenceItems, evidence => evidence.Id == "clue-reno-feature");
    }

    private static GameSession CreateArmedWantedSessionWithIdentityEvidence()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.None);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var knownClues = new[]
        {
            new Clue(
                new ClueId("clue-mira-alias"),
                ClueKind.Alias,
                "A wanted poster links Red Wren to a rider in the marshal files.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.SheriffWarrants,
                source: "wanted poster",
                context: "Public notice",
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor("Red Wren", Alias: "Red Wren")
                    })),
            new Clue(
                new ClueId("clue-mira-feature"),
                ClueKind.IdentityFact,
                "Saloon gossip says the wanted rider wears a Raven-feather pin.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.LocalGossip,
                source: "saloon talk",
                context: "Identity rumor",
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor("Raven-feather pin", Feature: "Raven-feather pin")
                    })),
            new Clue(
                new ClueId("clue-reno-feature"),
                ClueKind.IdentityFact,
                "A deputy remembers a wanted rider with mismatched spurs.",
                new[] { new SuspectId("suspect-2") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.LocalRecords,
                source: "sheriff record",
                context: "Open warrant",
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor("Mismatched spurs", Feature: "Mismatched spurs")
                    }))
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: knownClues,
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-mira"),
                    "Mira Cline",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        2500m,
                        new[] { "Red Wren" },
                        new[] { "Raven-feather pin" },
                        "Dodge City Marshal",
                        InvestigationTargetKind.UnrelatedWantedCriminal,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for a stage robbery."),
                new Warrant(
                    new WarrantId("warrant-reno"),
                    "Reno Pike",
                    new WarrantTerms(
                        WarrantDisposition.AliveOnly,
                        300m,
                        new[] { "The Magpie" },
                        new[] { "Mismatched spurs" },
                        "Silver Creek Sheriff",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for cattle theft.")
            });

        var inventory = new DomainInventory(
            new[]
            {
                new InventoryItem(ItemKind.Revolver, 1),
                new InventoryItem(ItemKind.RevolverAmmo, 2)
            });

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(currentTown.Id);
        session.CompleteGameStart(wallet: null, inventory: inventory);
        return session;
    }
}
