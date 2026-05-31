using WildBunch.Domain.Cases;

namespace WildBunch.GameContent.NewGame;

internal static class SeedCaseBuilder
{
    private const int NormalReleaseThreshold = 5;
    private static readonly SuspectId TrueCulpritId = new("suspect-4");

    public static CaseFile CreateCanonicalCaseFile()
    {
        var suspects = CreateSuspects(CaseCharacterRoster.SelectCanonicalGangRoster());
        var knownClues = CreateCanonicalKnownClues();
        var publicClues = CreateCanonicalPublicClues(suspects);
        var publicWarrants = new[]
        {
            CreateWarrant("warrant", 1, CaseCharacterRoster.CreateCanonicalTrueCulpritWarrant(), source: null, "Wanted for a Wild Bunch robbery and related killings."),
            CreateWarrant("warrant", 2, CaseCharacterRoster.CreateCanonicalUnrelatedWarrant(), source: null, "Wanted for cattle theft and forging livery tags.")
        };

        return new CaseFile(
            accusation: new SuspectId("suspect-2"),
            suspects,
            trueCulpritId: TrueCulpritId,
            openingLead: CaseOpeningLead.Create("The culprit has a scar on his left cheek."),
            knownClues,
            publicClues: publicClues,
            killerReleaseThreshold: NormalReleaseThreshold,
            publicWarrants: publicWarrants);
    }

    public static CaseFile CreateCaseFile(GameSetupGenerationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.IsCanonical)
        {
            return CreateCanonicalCaseFile();
        }

        var roster = CaseCharacterRoster.SelectGangRoster(plan.Source);
        var suspects = CreateSuspects(roster);
        var accusationId = suspects[plan.Source.PickIndex(GameSetupDeterministicLabels.CaseAccusation, suspects.Count)].Id;
        var culprit = suspects.Single(suspect => suspect.Id.Equals(TrueCulpritId));

        var knownClues = CreateKnownClues(plan.Source);
        var publicClues = CreatePublicClues(plan.Source, suspects);
        var publicWarrants = new[]
        {
            CreateWarrant(GameSetupDeterministicLabels.CasePublicWarrants, 1, CaseCharacterRoster.CreateTrueCulpritWarrant(roster[3]), plan.Source, "Wanted for a Wild Bunch robbery and related killings."),
            CreateWarrant(GameSetupDeterministicLabels.CasePublicWarrants, 2, CaseCharacterRoster.SelectUnrelatedWarrant(plan.Source), plan.Source, "Wanted for cattle theft and forging livery tags.")
        };

        return new CaseFile(
            accusationId,
            suspects,
            culprit.Id,
            CaseOpeningLead.Create("The culprit has a scar on his left cheek."),
            knownClues,
            publicClues: publicClues,
            killerReleaseThreshold: NormalReleaseThreshold,
            publicWarrants: publicWarrants);
    }

    private static IReadOnlyList<Suspect> CreateSuspects(IReadOnlyList<CaseCharacterProfile> roster)
    {
        ArgumentNullException.ThrowIfNull(roster);
        if (roster.Count != 7)
        {
            throw new InvalidOperationException("Seed case rosters must contain exactly seven gang suspects.");
        }

        return new[]
        {
            CreateSuspect(new SuspectId("suspect-1"), roster[0]),
            CreateSuspect(new SuspectId("suspect-2"), roster[1]),
            CreateSuspect(new SuspectId("suspect-3"), roster[2]),
            CreateSuspect(TrueCulpritId, roster[3]),
            CreateSuspect(new SuspectId("suspect-5"), roster[4]),
            CreateSuspect(new SuspectId("suspect-6"), roster[5]),
            CreateSuspect(new SuspectId("suspect-7"), roster[6])
        };
    }

    private static Suspect CreateSuspect(SuspectId id, CaseCharacterProfile profile)
        => new(
            id,
            profile.DisplayName,
            new SuspectProfile(profile.GameAliases, profile.IdentifyingFacts.Select(fact => new SuspectIdentityFact(fact))),
            profile.Traits,
            SuspectStatus.AtLarge);

    private static IReadOnlyList<Clue> CreateCanonicalKnownClues()
        => new[]
        {
            new Clue(
                new ClueId("clue-1"),
                ClueKind.CulpritTrail,
                "The culprit has a scar on his left cheek.",
                new[] { TrueCulpritId },
                InvestigationTargetKind.TrueCulprit,
                source: "trail witness",
                context: "Opening lead"),
            new Clue(
                new ClueId("clue-2"),
                ClueKind.IdentityFact,
                "A rider answered to the name Red Wren and wore a raven-feather pin.",
                new[] { TrueCulpritId },
                InvestigationTargetKind.TrueCulprit,
                source: "telegraph ledger",
                context: "Identity match"),
            new Clue(
                new ClueId("clue-3"),
                ClueKind.Whereabouts,
                "Boot prints and a waystation note place the rider on the Red Mesa road after dusk.",
                new[] { TrueCulpritId },
                InvestigationTargetKind.TrueCulprit,
                source: "waystation clerk",
                context: "Route lead")
        };

    private static IReadOnlyList<Clue> CreateKnownClues(GameSetupDeterministicSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new[]
        {
            CreateClue(source, GameSetupDeterministicLabels.CaseKnownClues, 1, ClueKind.CulpritTrail, "The culprit has a scar on his left cheek.", TrueCulpritId, InvestigationTargetKind.TrueCulprit, "trail witness", "Opening lead"),
            CreateClue(source, GameSetupDeterministicLabels.CaseKnownClues, 2, ClueKind.IdentityFact, "A rider answered to the name Red Wren and wore a raven-feather pin.", TrueCulpritId, InvestigationTargetKind.TrueCulprit, "telegraph ledger", "Identity match"),
            CreateClue(source, GameSetupDeterministicLabels.CaseKnownClues, 3, ClueKind.Whereabouts, "Boot prints and a waystation note place the rider on the Red Mesa road after dusk.", TrueCulpritId, InvestigationTargetKind.TrueCulprit, "waystation clerk", "Route lead")
        };
    }

    private static IReadOnlyList<Clue> CreateCanonicalPublicClues(IReadOnlyList<Suspect> suspects)
        => new[]
        {
            new Clue(
                new ClueId("clue-public-1"),
                ClueKind.Alias,
                "A poster shows a rider marked by a faded blue scarf and the nickname Grey Jay.",
                new[] { suspects[0].Id },
                InvestigationTargetKind.GangMember,
                source: "notice board",
                context: "Public wanted poster"),
            new Clue(
                new ClueId("clue-public-2"),
                ClueKind.Record,
                "A public notice describes a tin badge clipped to a saddle strap.",
                new[] { suspects[1].Id },
                InvestigationTargetKind.Suspected,
                source: "sheriff record",
                context: "Public notice")
        };

    private static IReadOnlyList<Clue> CreatePublicClues(GameSetupDeterministicSource source, IReadOnlyList<Suspect> suspects)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(suspects);

        return new[]
        {
            CreateClue(source, GameSetupDeterministicLabels.CasePublicClues, 1, ClueKind.Alias, "A wanted poster mentions a faded blue sash and the nickname Grey Jay.", suspects[0].Id, InvestigationTargetKind.GangMember, "notice board", "Public wanted poster"),
            CreateClue(source, GameSetupDeterministicLabels.CasePublicClues, 2, ClueKind.Record, "A notice board sketch shows a sand-colored hat with a stitched brim.", suspects[1].Id, InvestigationTargetKind.Suspected, "sheriff record", "Public notice")
        };
    }

    private static Warrant CreateWarrant(string label, int warrantIndex, OutlawWarrantProfile profile, GameSetupDeterministicSource? source, string summary = "")
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
                profile.IsGangRelevant,
                profile.AdvancesGangPressure),
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
        string context)
        => new(
            new ClueId($"{label}-{clueIndex:00}-{source.PickIndex($"{label}.{clueIndex}", 97):00}"),
            kind,
            description,
            new[] { linkedSuspectId },
            targetKind,
            sourceNote,
            context);
}
