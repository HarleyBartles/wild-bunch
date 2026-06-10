using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Tests;

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
}
