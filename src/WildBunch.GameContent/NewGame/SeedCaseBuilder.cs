using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class SeedCaseBuilder
{
    private const int NormalReleaseThreshold = 5;

    public static CaseFile CreateCanonicalCaseFile(
        GameSetupDeterministicSource source,
        World world,
        int resolvedCulpritIndex,
        int resolvedAccusationIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(world);

        return BuildCase(
            source,
            world,
            CaseCharacterRoster.SelectCanonicalGangRoster(),
            CaseSuspectFeaturePool.SelectCanonicalAssignedFeatures(source),
            accusationIndex: resolvedAccusationIndex,
            trueCulpritIndex: resolvedCulpritIndex,
            publicWarrant1: CaseCharacterRoster.CreateGangMemberWarrant(CaseCharacterRoster.SelectCanonicalGangRoster()[0]),
            publicWarrant2: CaseCharacterRoster.CreateCanonicalUnrelatedWarrant(),
            startingTownId: SeedWorldCatalog.PinecrossId);
    }

    public static CaseFile CreateCaseFile(
        GameSetupDeterministicSource source,
        World world,
        int resolvedCulpritIndex,
        int resolvedAccusationIndex,
        TownId? startingTownId = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(world);

        var roster = CaseCharacterRoster.SelectGangRoster(source);
        var features = CaseSuspectFeaturePool.SelectAssignedFeatures(source);
        return BuildCase(
            source,
            world,
            roster,
            features,
            resolvedAccusationIndex,
            resolvedCulpritIndex,
            CaseCharacterRoster.CreateGangMemberWarrant(roster[0]),
            CaseCharacterRoster.SelectUnrelatedWarrant(source),
            startingTownId ?? world.Towns.First().Id);
    }

    private static CaseFile BuildCase(
        GameSetupDeterministicSource source,
        World world,
        IReadOnlyList<CaseCharacterProfile> roster,
        IReadOnlyList<CaseSuspectFeatureAssignment> features,
        int accusationIndex,
        int trueCulpritIndex,
        OutlawWarrantProfile publicWarrant1,
        OutlawWarrantProfile publicWarrant2,
        TownId startingTownId)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentNullException.ThrowIfNull(features);
        if (roster.Count != 7)
        {
            throw new InvalidOperationException("Seed case rosters must contain exactly seven gang suspects.");
        }
        if (features.Count != 7)
        {
            throw new InvalidOperationException("Seed case feature assignments must contain exactly seven entries.");
        }

        var suspects = new[]
        {
            CreateSuspect(new SuspectId("suspect-1"), roster[0], features[0]),
            CreateSuspect(new SuspectId("suspect-2"), roster[1], features[1]),
            CreateSuspect(new SuspectId("suspect-3"), roster[2], features[2]),
            CreateSuspect(new SuspectId("suspect-4"), roster[3], features[3]),
            CreateSuspect(new SuspectId("suspect-5"), roster[4], features[4]),
            CreateSuspect(new SuspectId("suspect-6"), roster[5], features[5]),
            CreateSuspect(new SuspectId("suspect-7"), roster[6], features[6])
        };

        var culprit = suspects[trueCulpritIndex];
        var suspectTurfAssignments = SelectSuspectTurfAssignments(source, world, suspects);
        var openingLead = CaseOpeningLead.Create(CaseSuspectFeaturePool.BuildOpeningLead(features[trueCulpritIndex].PrimaryFeature));
        var knownClues = CreateKnownClues(source, features[trueCulpritIndex].PrimaryFeature, culprit.Id);
        var publicClues = CreatePublicClues(
            source,
            world,
            suspects,
            features,
            suspectTurfAssignments,
            features[trueCulpritIndex].PrimaryFeature,
            culprit.Id,
            startingTownId);
        var publicWarrants = CreatePublicWarrants(
            source,
            world,
            publicWarrant1,
            publicWarrant2,
            startingTownId);
        var accusationId = suspects[accusationIndex].Id;

        return new CaseFile(
            accusationId,
            suspects,
            culprit.Id,
            openingLead,
            knownClues,
            publicClues: publicClues,
            killerReleaseThreshold: NormalReleaseThreshold,
            publicWarrants: publicWarrants,
            suspectTurfAssignments: suspectTurfAssignments);
    }

    private static Suspect CreateSuspect(SuspectId id, CaseCharacterProfile profile, CaseSuspectFeatureAssignment feature)
        => new(
            id,
            profile.DisplayName,
            new SuspectProfile(profile.GameAliases, feature.AllFeatures.Select(fact => new SuspectIdentityFact(fact.Description, fact.Kind == CaseFeatureKind.PrimaryMarker))),
            profile.Traits,
            SuspectStatus.AtLarge);

    private static IReadOnlyList<Clue> CreateKnownClues(GameSetupDeterministicSource source, CaseSuspectFeatureProfile culpritFeature, SuspectId trueCulpritId)
        => new[]
        {
            CreateClue(
                source,
                GameSetupDeterministicLabels.CaseKnownClues,
                1,
                ClueKind.CulpritTrail,
                CaseSuspectFeaturePool.BuildOpeningLead(culpritFeature),
                trueCulpritId,
                InvestigationTargetKind.TrueCulprit,
                "trail witness",
                "Opening lead",
                InvestigationSourceKind.TelegraphLead,
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor(culpritFeature.Description, Feature: culpritFeature.Description, Fact: "opening lead"),
                    },
                    times: new[]
                    {
                        new ClueTimeAnchor(ClueRecency.Recent)
                    }))
        };

    private static IReadOnlyList<Clue> CreatePublicClues(
        GameSetupDeterministicSource source,
        World world,
        IReadOnlyList<Suspect> suspects,
        IReadOnlyList<CaseSuspectFeatureAssignment> features,
        IReadOnlyList<SuspectTurfAssignment> suspectTurfAssignments,
        CaseSuspectFeatureProfile culpritFeature,
        SuspectId trueCulpritId,
        TownId startingTownId)
    {
        var publicClues = new List<Clue>
        {
            CreateClue(
                source,
                GameSetupDeterministicLabels.CasePublicClues,
                1,
                ClueKind.Alias,
                $"A poster links the alias {DescribePrimaryAlias(suspects[0])} to {DescribePersonWithFeature(features[0].PrimaryFeature, "a rider")}.",
                suspects[0].Id,
                InvestigationTargetKind.GangMember,
                "notice board",
                "Public wanted poster",
                InvestigationSourceKind.SheriffWarrants,
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor(DescribePrimaryAlias(suspects[0]), Alias: DescribePrimaryAlias(suspects[0]), Feature: features[0].PrimaryFeature.Description)
                    })),
            CreateClue(
                source,
                GameSetupDeterministicLabels.CasePublicClues,
                2,
                ClueKind.Record,
                $"A public notice describes {DescribeUnnamedRider(features[1].PrimaryFeature)}.",
                suspects[1].Id,
                InvestigationTargetKind.Suspected,
                "sheriff record",
                "Public notice",
                InvestigationSourceKind.LocalRecords,
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor(features[1].PrimaryFeature.Description, Feature: features[1].PrimaryFeature.Description)
                    })),
            CreateClue(
                source,
                GameSetupDeterministicLabels.CasePublicClues,
                3,
                ClueKind.IdentityFact,
                $"A telegraph clerk filed the alias {DescribePrimaryAlias(suspects[2])} alongside a note about {DescribePersonWithFeature(features[2].PrimaryFeature, "a rider")}.",
                suspects[2].Id,
                InvestigationTargetKind.Suspected,
                "telegraph clerk",
                "Telegraph lead",
                InvestigationSourceKind.TelegraphLead,
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor(DescribePrimaryAlias(suspects[2]), Alias: DescribePrimaryAlias(suspects[2]), Fact: features[2].PrimaryFeature.Description)
                    })),
            CreateClue(
                source,
                GameSetupDeterministicLabels.CasePublicClues,
                4,
                ClueKind.Whereabouts,
                $"Local gossip out of {world.GetTown(suspectTurfAssignments[4].TurfTownId).Name} says {DescribePersonWithFeature(features[4].PrimaryFeature, "a rider")} kept to the rail spur after dark.",
                suspects[4].Id,
                InvestigationTargetKind.GangMember,
                "saloon talk",
                "Town gossip",
                InvestigationSourceKind.LocalGossip,
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor(features[4].PrimaryFeature.Description, Feature: features[4].PrimaryFeature.Description)
                    },
                    locations: new[]
                    {
                        new ClueLocationAnchor(world.GetTown(suspectTurfAssignments[4].TurfTownId).Name, TownId: suspectTurfAssignments[4].TurfTownId, Place: world.GetTown(suspectTurfAssignments[4].TurfTownId).Name, Route: "rail spur")
                    },
                    times: new[]
                    {
                        new ClueTimeAnchor(ClueRecency.Recent)
                    },
                    directions: new[]
                    {
                        new ClueDirectionAnchor("kept to the rail spur after dark", Movement: "kept to the rail spur after dark", Route: "rail spur", DestinationTownId: suspectTurfAssignments[4].TurfTownId)
                    })),
            CreateClue(
                source,
                GameSetupDeterministicLabels.CasePublicClues,
                5,
                ClueKind.IdentityFact,
                $"A witness tied the rider to {DescribePersonWithFeature(culpritFeature, "a man")}.",
                trueCulpritId,
                InvestigationTargetKind.TrueCulprit,
                "telegraph ledger",
                "Identity match",
                InvestigationSourceKind.TelegraphLead,
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor(culpritFeature.Description, Feature: culpritFeature.Description, Fact: "identity match")
                    },
                    times: new[]
                    {
                        new ClueTimeAnchor(ClueRecency.Recent)
                    })),
            CreateClue(
                source,
                GameSetupDeterministicLabels.CasePublicClues,
                6,
                ClueKind.Whereabouts,
                "Boot prints and a waystation note place the rider on the Red Mesa road after dusk.",
                trueCulpritId,
                InvestigationTargetKind.TrueCulprit,
                "waystation clerk",
                "Route lead",
                InvestigationSourceKind.LocalGossip,
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor("Red Mesa rider", Feature: culpritFeature.Description)
                    },
                    locations: new[]
                    {
                        new ClueLocationAnchor("Red Mesa road", Place: "Red Mesa road", Route: "Red Mesa road")
                    },
                    times: new[]
                    {
                        new ClueTimeAnchor(ClueRecency.Recent)
                    },
                    directions: new[]
                    {
                        new ClueDirectionAnchor("after dusk", Movement: "heading along the Red Mesa road", Route: "Red Mesa road")
                    }))
        };

        publicClues.AddRange(CreateTownSpecificPublicClues(
            source,
            world,
            suspects,
            features,
            startingTownId,
            firstExtraClueIndex: publicClues.Count + 1));

        return publicClues;
    }

    private static IReadOnlyList<Warrant> CreatePublicWarrants(
        GameSetupDeterministicSource source,
        World world,
        OutlawWarrantProfile publicWarrant1,
        OutlawWarrantProfile publicWarrant2,
        TownId startingTownId)
    {
        var publicWarrants = new List<Warrant>
        {
            CreateWarrant(GameSetupDeterministicLabels.CasePublicWarrants, 1, publicWarrant1, source, "Wanted for a Wild Bunch robbery and related killings.", InvestigationSourceKind.SheriffWarrants),
            CreateWarrant(GameSetupDeterministicLabels.CasePublicWarrants, 2, publicWarrant2, source, "Wanted for cattle theft and forging livery tags.", InvestigationSourceKind.SheriffWarrants)
        };

        publicWarrants.AddRange(CreateTownSpecificPublicWarrants(
            source,
            world,
            startingTownId,
            firstExtraWarrantIndex: publicWarrants.Count + 1));

        return publicWarrants;
    }

    private static IReadOnlyList<Clue> CreateTownSpecificPublicClues(
        GameSetupDeterministicSource source,
        World world,
        IReadOnlyList<Suspect> suspects,
        IReadOnlyList<CaseSuspectFeatureAssignment> features,
        TownId startingTownId,
        int firstExtraClueIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(suspects);
        ArgumentNullException.ThrowIfNull(features);

        var extraTownClues = new List<Clue>();
        var extraTownIndex = 0;
        var candidateSuspectIndices = Enumerable.Range(0, suspects.Count)
            .Where(index => index != 3)
            .ToArray();

        foreach (var town in world.Towns.OrderBy(town => town.Id.Value, StringComparer.OrdinalIgnoreCase))
        {
            if (town.Id.Equals(startingTownId) || (town.Services & TownServices.NoticeBoard) == 0)
            {
                continue;
            }

            var clueBaseIndex = firstExtraClueIndex + extraTownIndex * 2;
            var noticeSuspectIndex = candidateSuspectIndices[(extraTownIndex * 2) % candidateSuspectIndices.Length];
            var sheriffSuspectIndex = candidateSuspectIndices[(extraTownIndex * 2 + 1) % candidateSuspectIndices.Length];

            extraTownClues.Add(CreateClue(
                source,
                GameSetupDeterministicLabels.CasePublicClues,
                clueBaseIndex,
                ClueKind.Alias,
                $"A posted circular in {town.Name} links {DescribePrimaryAlias(suspects[noticeSuspectIndex])} to {DescribePersonWithFeature(features[noticeSuspectIndex].PrimaryFeature, "a rider")}.",
                suspects[noticeSuspectIndex].Id,
                InvestigationTargetKind.GangMember,
                "notice board",
                "Public wanted poster",
                InvestigationSourceKind.SheriffWarrants,
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor(DescribePrimaryAlias(suspects[noticeSuspectIndex]), Alias: DescribePrimaryAlias(suspects[noticeSuspectIndex]), Feature: features[noticeSuspectIndex].PrimaryFeature.Description)
                    })));

            extraTownClues.Add(CreateClue(
                source,
                GameSetupDeterministicLabels.CasePublicClues,
                clueBaseIndex + 1,
                ClueKind.Record,
                $"A sheriff ledger in {town.Name} notes {DescribePersonWithFeature(features[sheriffSuspectIndex].PrimaryFeature, "a rider")} paying cash under a clean alias.",
                suspects[sheriffSuspectIndex].Id,
                InvestigationTargetKind.Suspected,
                "sheriff record",
                "Public notice",
                InvestigationSourceKind.LocalRecords,
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor(features[sheriffSuspectIndex].PrimaryFeature.Description, Feature: features[sheriffSuspectIndex].PrimaryFeature.Description)
                    })));

            extraTownIndex++;
        }

        return extraTownClues;
    }

    private static IReadOnlyList<Warrant> CreateTownSpecificPublicWarrants(
        GameSetupDeterministicSource source,
        World world,
        TownId startingTownId,
        int firstExtraWarrantIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(world);

        var extraTownWarrants = new List<Warrant>();
        var extraTownIndex = 0;
        var candidateWarrants = CaseCharacterRoster.UnrelatedWantedCriminalPool;

        foreach (var town in world.Towns.OrderBy(town => town.Id.Value, StringComparer.OrdinalIgnoreCase))
        {
            if (town.Id.Equals(startingTownId) || (town.Services & TownServices.NoticeBoard) == 0)
            {
                continue;
            }

            var warrantIndex = firstExtraWarrantIndex + extraTownIndex;
            var profile = candidateWarrants[extraTownIndex % candidateWarrants.Count];
            extraTownWarrants.Add(CreateWarrant(
                GameSetupDeterministicLabels.CasePublicWarrants,
                warrantIndex,
                profile,
                source,
                $"Wanted for offenses reported out of {town.Name}.",
                InvestigationSourceKind.SheriffWarrants));

            extraTownIndex++;
        }

        return extraTownWarrants;
    }

    private static string DescribePrimaryAlias(Suspect suspect)
        => suspect.Profile.Aliases.Count > 0
            ? suspect.Profile.Aliases[0].Name
            : suspect.Name;

    private static string DescribeUnnamedRider(CaseSuspectFeatureProfile feature)
        => $"an unnamed rider who {DescribeFeatureClause(feature.Description)}";

    private static string DescribePersonWithFeature(CaseSuspectFeatureProfile feature, string person)
        => $"{person} who {DescribeFeatureClause(feature.Description)}";

    private static string DescribeFeatureClause(string featureDescription)
    {
        var trimmed = featureDescription.Trim().TrimEnd('.', '!', '?');

        if (trimmed.Length == 0)
        {
            return "is described by an unrecorded feature";
        }

        return char.ToLowerInvariant(trimmed[0]) + trimmed[1..];
    }

    private static IReadOnlyList<SuspectTurfAssignment> SelectSuspectTurfAssignments(
        GameSetupDeterministicSource source,
        World world,
        IReadOnlyList<Suspect> suspects)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(suspects);

        var towns = world.Towns
            .OrderBy(town => town.Id.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (towns.Length == 0)
        {
            throw new InvalidOperationException("Seed case turf requires at least one town in the seeded world.");
        }

        return suspects
            .Select(suspect =>
            {
                var townIndex = source.PickIndex($"{GameSetupDeterministicLabels.CaseSuspectTurf}.{suspect.Id.Value}", towns.Length);
                return new SuspectTurfAssignment(suspect.Id, towns[townIndex].Id);
            })
            .ToArray();
    }

    private static Warrant CreateWarrant(string label, int warrantIndex, OutlawWarrantProfile profile, GameSetupDeterministicSource? source, string summary = "", InvestigationSourceKind? sourceKind = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var warrantId = source is null
            ? new WarrantId($"{label}-{warrantIndex:00}")
            : new WarrantId($"{label}-{warrantIndex:00}-{source.PickIndex($"{label}.{warrantIndex}", 97):00}");

        return new Warrant(
            warrantId,
            profile.TargetName,
            new WarrantTerms(
                profile.Disposition,
                profile.BountyAmount,
                profile.KnownAliases,
                profile.KnownFeatures,
                profile.IssuingSource,
                profile.TargetKind,
                profile.GangAffiliations,
                profile.AdvancesGangPressureFor,
                sourceKind),
            summary);
    }

    private static Clue CreateClue(
        GameSetupDeterministicSource source,
        string label,
        int clueIndex,
        ClueKind kind,
        string description,
        SuspectId linkedSuspectId,
        InvestigationTargetKind targetKind,
        string sourceNote,
        string context,
        InvestigationSourceKind? sourceKind = null,
        ClueAnchors? anchors = null)
        => new(
            new ClueId($"{label}-{clueIndex:00}-{source.PickIndex($"{label}.{clueIndex}", 97):00}"),
            kind,
            description,
            new[] { linkedSuspectId },
            targetKind,
            sourceKind,
            sourceNote,
            context,
            anchors);
}
