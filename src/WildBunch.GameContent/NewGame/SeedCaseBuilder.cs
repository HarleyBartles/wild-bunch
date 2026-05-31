using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Cases;

namespace WildBunch.GameContent.NewGame;

internal static class SeedCaseBuilder
{
    private const int NormalReleaseThreshold = 5;
    private static readonly SuspectId TrueCulpritId = new("suspect-4");

    public static CaseFile CreateCanonicalCaseFile()
    {
        var suspects = CreateSuspects();
        var clues = new[]
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

        var publicClues = new[]
        {
                new Clue(
                    new ClueId("clue-public-1"),
                    ClueKind.Alias,
                    "A poster shows a rider marked by a faded blue scarf and the nickname Grey Jay.",
                    new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.GangMember,
                source: "notice board",
                context: "Public wanted poster"),
            new Clue(
                new ClueId("clue-public-2"),
                ClueKind.Record,
                "A public notice describes a tin badge clipped to a saddle strap.",
                new[] { new SuspectId("suspect-2") },
                InvestigationTargetKind.Suspected,
                source: "sheriff record",
                context: "Public notice")
        };

        var publicWarrants = new[]
        {
            new Warrant(
                new WarrantId("warrant-1"),
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
                "Wanted for a Wild Bunch robbery and related killings."),
            new Warrant(
                new WarrantId("warrant-2"),
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
                "Wanted for cattle theft and forging livery tags.")
        };

        return new CaseFile(
            accusation: new SuspectId("suspect-2"),
            suspects,
            trueCulpritId: TrueCulpritId,
            openingLead: CaseOpeningLead.Create("The culprit has a scar on his left cheek."),
            knownClues: clues,
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

        var suspects = CreateSuspects();
        var culpritId = TrueCulpritId;
        var accusationId = suspects[plan.Source.PickIndex(GameSetupDeterministicLabels.CaseAccusation, suspects.Count)].Id;

        var knownClues = new[]
        {
            CreateClue(plan.Source, GameSetupDeterministicLabels.CaseKnownClues, 1, ClueKind.CulpritTrail, "The culprit has a scar on his left cheek.", culpritId, InvestigationTargetKind.TrueCulprit, "trail witness", "Opening lead"),
            CreateClue(plan.Source, GameSetupDeterministicLabels.CaseKnownClues, 2, ClueKind.IdentityFact, "A rider answered to the name Red Wren and wore a raven-feather pin.", culpritId, InvestigationTargetKind.TrueCulprit, "telegraph ledger", "Identity match"),
            CreateClue(plan.Source, GameSetupDeterministicLabels.CaseKnownClues, 3, ClueKind.Whereabouts, "Boot prints and a waystation note place the rider on the Red Mesa road after dusk.", culpritId, InvestigationTargetKind.TrueCulprit, "waystation clerk", "Route lead")
        };

        var publicClues = new[]
        {
            CreateClue(plan.Source, GameSetupDeterministicLabels.CasePublicClues, 1, ClueKind.Alias, "A wanted poster mentions a faded blue sash and the nickname Grey Jay.", suspects[0].Id, InvestigationTargetKind.GangMember, "notice board", "Public wanted poster"),
            CreateClue(plan.Source, GameSetupDeterministicLabels.CasePublicClues, 2, ClueKind.Record, "A notice board sketch shows a sand-colored hat with a stitched brim.", suspects[1].Id, InvestigationTargetKind.Suspected, "sheriff record", "Public notice")
        };

        var publicWarrants = new[]
        {
            CreateWarrant(plan.Source, GameSetupDeterministicLabels.CasePublicClues, 1, "Tessa Wren", WarrantDisposition.DeadOrAlive, 2500m, new[] { "Red Wren", "Aunt Tess" }, new[] { "Pale scar across the left cheek", "Raven-feather pin" }, "Dodge City Marshal", InvestigationTargetKind.TrueCulprit, true, true, "Wanted for a Wild Bunch robbery and related killings."),
            CreateWarrant(plan.Source, GameSetupDeterministicLabels.CasePublicClues, 2, "Reno Pike", WarrantDisposition.AliveOnly, 300m, new[] { "The Magpie", "R. Pike" }, new[] { "Mismatched spurs", "Black felt hat" }, "Silver Creek Sheriff", InvestigationTargetKind.UnrelatedWantedCriminal, false, false, "Wanted for cattle theft and forging livery tags.")
        };

        var openingLead = CaseOpeningLead.Create("The culprit has a scar on his left cheek.");

        return new CaseFile(
            accusationId,
            suspects,
            culpritId,
            openingLead,
            knownClues,
            publicClues: publicClues,
            killerReleaseThreshold: NormalReleaseThreshold,
            publicWarrants: publicWarrants);
    }

    private static IReadOnlyList<Suspect> CreateSuspects()
        => new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Jonah Pike",
                new SuspectProfile(
                    new[]
                    {
                        new SuspectAlias("Grey Jay", AliasKind.Nickname),
                        new SuspectAlias("J. Pike", AliasKind.FormerName)
                    },
                    new[]
                    {
                        new SuspectIdentityFact("Wears a cracked leather gauntlet on the right hand."),
                        new SuspectIdentityFact("Keeps a brass spur tied to a faded blue sash.")
                    }),
                new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true),
                SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-2"),
                "Mira Cline",
                new SuspectProfile(
                    new[]
                    {
                        new SuspectAlias("M.K. Rook", AliasKind.KnownAs)
                    },
                    new[]
                    {
                        new SuspectIdentityFact("Carries a tin badge clipped to a saddle strap."),
                        new SuspectIdentityFact("Prefers a sand-colored hat with the brim stitched flat.")
                    }),
                new SuspectTraits(IsLocal: false, IsArmed: false, IsDesperate: false),
                SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-3"),
                "Evan Quill",
                new SuspectProfile(
                    new[]
                    {
                        new SuspectAlias("Inkshot", AliasKind.Nickname),
                        new SuspectAlias("E. Quill", AliasKind.FormerName)
                    },
                    new[]
                    {
                        new SuspectIdentityFact("Has a black-stained cuff on the left sleeve."),
                        new SuspectIdentityFact("Keeps a split-finger glove tucked into a coat pocket.")
                    }),
                new SuspectTraits(IsLocal: true, IsArmed: true, IsDesperate: false),
                SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-4"),
                "Tessa Wren",
                new SuspectProfile(
                    new[]
                    {
                        new SuspectAlias("Red Wren", AliasKind.Nickname),
                        new SuspectAlias("Aunt Tess", AliasKind.KnownAs)
                    },
                    new[]
                    {
                        new SuspectIdentityFact("A pale scar cuts across the left cheek."),
                        new SuspectIdentityFact("Wears a raven-feather pin on a dark coat.")
                    }),
                new SuspectTraits(IsLocal: false, IsArmed: true, IsDesperate: true),
                SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-5"),
                "Silas Boone",
                new SuspectProfile(
                    new[]
                    {
                        new SuspectAlias("Hollow Boone", AliasKind.Nickname)
                    },
                    new[]
                    {
                        new SuspectIdentityFact("Keeps iron-rim spectacles tucked into a coat pocket."),
                        new SuspectIdentityFact("Wears a long dust-colored duster with a frayed hem.")
                    }),
                new SuspectTraits(IsLocal: true, IsArmed: true, IsDesperate: false),
                SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-6"),
                "Clara Vale",
                new SuspectProfile(
                    new[]
                    {
                        new SuspectAlias("Cedar Vale", AliasKind.Nickname)
                    },
                    new[]
                    {
                        new SuspectIdentityFact("Keeps a copper ribbon tied in her hair."),
                        new SuspectIdentityFact("Leaves tobacco-stained glove prints on ledgers and rail notices.")
                    }),
                new SuspectTraits(IsLocal: false, IsArmed: false, IsDesperate: true),
                SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-7"),
                "Orin Nash",
                new SuspectProfile(
                    new[]
                    {
                        new SuspectAlias("O. Nash", AliasKind.FormerName)
                    },
                    new[]
                    {
                        new SuspectIdentityFact("Has a silver tooth that catches the light when he smiles."),
                        new SuspectIdentityFact("Carries a rope-burn scar on the left wrist.")
                    }),
                new SuspectTraits(IsLocal: true, IsArmed: true, IsDesperate: true),
                SuspectStatus.AtLarge)
        };

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

    private static Warrant CreateWarrant(
        GameSetupDeterministicSource source,
        string label,
        int warrantIndex,
        string targetName,
        WarrantDisposition disposition,
        decimal bountyAmount,
        IReadOnlyList<string> knownAliases,
        IReadOnlyList<string> knownFeatures,
        string issuingSource,
        InvestigationTargetKind targetKind,
        bool isGangRelevant,
        bool advancesGangPressure,
        string summary)
        => new(
            new WarrantId($"{label}-{warrantIndex:00}-{source.PickIndex($"{label}.{warrantIndex}", 97):00}"),
            targetName,
            new WarrantTerms(
                disposition,
                bountyAmount,
                knownAliases,
                knownFeatures,
                issuingSource,
                targetKind,
                isGangRelevant,
                advancesGangPressure),
            summary);
}
