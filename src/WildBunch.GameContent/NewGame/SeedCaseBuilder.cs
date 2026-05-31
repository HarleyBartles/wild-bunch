using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class SeedCaseBuilder
{
    private const int NormalReleaseThreshold = 5;
    private static readonly SuspectId TrueCulpritId = new("suspect-4");

    public static CaseFile CreateCanonicalCaseFile(GameSetupGenerationPlan plan, World world)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(world);

        return BuildCase(
            plan.Source,
            world,
            CaseCharacterRoster.SelectCanonicalGangRoster(),
            CaseSuspectFeaturePool.SelectCanonicalAssignedFeatures(plan.Source),
            accusationIndex: 1,
            trueCulpritIndex: 3,
            publicWarrant1: CaseCharacterRoster.CreateCanonicalTrueCulpritWarrant(),
            publicWarrant2: CaseCharacterRoster.CreateCanonicalUnrelatedWarrant());
    }

    public static CaseFile CreateCaseFile(GameSetupGenerationPlan plan, World world)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(world);

        if (plan.IsCanonical)
        {
            return CreateCanonicalCaseFile(plan, world);
        }

        var roster = CaseCharacterRoster.SelectGangRoster(plan.Source);
        var features = CaseSuspectFeaturePool.SelectAssignedFeatures(plan.Source);
        var accusationIndex = plan.Source.PickIndex(GameSetupDeterministicLabels.CaseAccusation, roster.Count);
        return BuildCase(
            plan.Source,
            world,
            roster,
            features,
            accusationIndex,
            3,
            CaseCharacterRoster.CreateTrueCulpritWarrant(roster[3]),
            CaseCharacterRoster.SelectUnrelatedWarrant(plan.Source));
    }

    private static CaseFile BuildCase(
        GameSetupDeterministicSource source,
        World world,
        IReadOnlyList<CaseCharacterProfile> roster,
        IReadOnlyList<CaseSuspectFeatureAssignment> features,
        int accusationIndex,
        int trueCulpritIndex,
        OutlawWarrantProfile publicWarrant1,
        OutlawWarrantProfile publicWarrant2)
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
            CreateSuspect(TrueCulpritId, roster[3], features[3]),
            CreateSuspect(new SuspectId("suspect-5"), roster[4], features[4]),
            CreateSuspect(new SuspectId("suspect-6"), roster[5], features[5]),
            CreateSuspect(new SuspectId("suspect-7"), roster[6], features[6])
        };

        var culprit = suspects[trueCulpritIndex];
        var suspectTurfAssignments = SelectSuspectTurfAssignments(source, world, suspects);
        var openingLead = CaseOpeningLead.Create(CaseSuspectFeaturePool.BuildOpeningLead(features[trueCulpritIndex].PrimaryFeature));
        var knownClues = CreateKnownClues(source, features[trueCulpritIndex].PrimaryFeature);
        var publicClues = CreatePublicClues(source, world, suspects, features, suspectTurfAssignments);
        var accusationId = suspects[accusationIndex].Id;
        var publicWarrants = new[]
        {
            CreateWarrant(GameSetupDeterministicLabels.CasePublicWarrants, 1, publicWarrant1, source, "Wanted for a Wild Bunch robbery and related killings.", InvestigationSourceKind.NoticeBoard),
            CreateWarrant(GameSetupDeterministicLabels.CasePublicWarrants, 2, publicWarrant2, source, "Wanted for cattle theft and forging livery tags.", InvestigationSourceKind.SheriffRecords)
        };

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
            new SuspectProfile(profile.GameAliases, feature.AllFeatures.Select(fact => new SuspectIdentityFact(fact.Description))),
            profile.Traits,
            SuspectStatus.AtLarge);

    private static IReadOnlyList<Clue> CreateKnownClues(GameSetupDeterministicSource source, CaseSuspectFeatureProfile culpritFeature)
        => new[]
        {
            CreateClue(
                source,
                GameSetupDeterministicLabels.CaseKnownClues,
                1,
                ClueKind.CulpritTrail,
                CaseSuspectFeaturePool.BuildOpeningLead(culpritFeature),
                TrueCulpritId,
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
                    })),
            CreateClue(
                source,
                GameSetupDeterministicLabels.CaseKnownClues,
                2,
                ClueKind.IdentityFact,
                $"A witness tied the rider to {culpritFeature.Description}.",
                TrueCulpritId,
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
                GameSetupDeterministicLabels.CaseKnownClues,
                3,
                ClueKind.Whereabouts,
                "Boot prints and a waystation note place the rider on the Red Mesa road after dusk.",
                TrueCulpritId,
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

    private static IReadOnlyList<Clue> CreatePublicClues(
        GameSetupDeterministicSource source,
        World world,
        IReadOnlyList<Suspect> suspects,
        IReadOnlyList<CaseSuspectFeatureAssignment> features,
        IReadOnlyList<SuspectTurfAssignment> suspectTurfAssignments)
        => new[]
        {
            CreateClue(
                source,
                GameSetupDeterministicLabels.CasePublicClues,
                1,
                ClueKind.Alias,
                $"A poster mentions {features[0].PrimaryFeature.Description.ToLowerInvariant()}",
                suspects[0].Id,
                InvestigationTargetKind.GangMember,
                "notice board",
                "Public wanted poster",
                InvestigationSourceKind.NoticeBoard,
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
                $"A public notice describes {features[1].PrimaryFeature.Description.ToLowerInvariant()}",
                suspects[1].Id,
                InvestigationTargetKind.Suspected,
                "sheriff record",
                "Public notice",
                InvestigationSourceKind.SheriffRecords,
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
                $"A telegraph clerk filed {DescribePrimaryAlias(suspects[2])} alongside a note about {features[2].PrimaryFeature.Description.ToLowerInvariant()}.",
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
                $"Local gossip out of {world.GetTown(suspectTurfAssignments[4].TurfTownId).Name} says the rider kept to the rail spur after dark.",
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
                    }))
        };

    private static string DescribePrimaryAlias(Suspect suspect)
        => suspect.Profile.Aliases.Count > 0
            ? suspect.Profile.Aliases[0].Name
            : suspect.Name;

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
