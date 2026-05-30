using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Cases;

namespace WildBunch.GameContent.NewGame;

internal static class SeedCaseBuilder
{
    public static CaseFile CreateCanonicalCaseFile()
    {
        var suspects = CreateSuspects();
        var clues = new[]
        {
            new Clue(new ClueId("clue-1"), ClueKind.Witness, "A dust-covered rider was seen heading east at dusk."),
            new Clue(new ClueId("clue-2"), ClueKind.Record, "The telegraph ledger shows a coded message sent from Red Mesa."),
            new Clue(new ClueId("clue-3"), ClueKind.Physical, "Boot prints match a narrow-heeled trail rider's boots.")
        };

        var publicClues = new[]
        {
            new Clue(
                new ClueId("clue-public-1"),
                ClueKind.Witness,
                "A poster shows a rider marked by a faded blue scarf and a grey jay emblem.",
                new[] { new SuspectId("suspect-1") }),
            new Clue(
                new ClueId("clue-public-2"),
                ClueKind.Physical,
                "A public notice describes a tin badge clipped to a saddle strap.",
                new[] { new SuspectId("suspect-2") })
        };

        return new CaseFile(
            accusation: new SuspectId("suspect-2"),
            suspects,
            trueCulpritId: new SuspectId("suspect-4"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: clues,
            publicClues: publicClues,
            killerReleaseThreshold: 2);
    }

    public static CaseFile CreateCaseFile(string seedCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);

        var suspects = CreateSuspects();
        var culpritIndex = PickIndex(seedCode, "culprit", suspects.Count);
        var culpritId = suspects[culpritIndex].Id;
        var accusationId = suspects[PickIndex(seedCode, "accusation", suspects.Count)].Id;

        var knownClues = new[]
        {
            CreateClue(seedCode, "known-1", ClueKind.Witness, "A rider with a split-finger glove was seen crossing the red ridge at dusk.", culpritId, suspects[(culpritIndex + 1) % suspects.Count].Id),
            CreateClue(seedCode, "known-2", ClueKind.Record, "The telegraph ledger shows a coded payment routed through Sagewell.", suspects[(culpritIndex + 2) % suspects.Count].Id),
            CreateClue(seedCode, "known-3", ClueKind.Physical, "Boot prints match a narrow-heeled trail rider's boots.", culpritId)
        };

        var publicClues = new[]
        {
            CreateClue(seedCode, "public-1", ClueKind.Witness, "A wanted poster mentions a faded blue sash and a cracked leather gauntlet.", suspects[0].Id),
            CreateClue(seedCode, "public-2", ClueKind.Physical, "A notice board sketch shows a sand-colored hat with a stitched brim.", suspects[1].Id)
        };

        var openingLead = CaseOpeningLead.Create(culpritIndex switch
        {
            0 => "A cracked leather gauntlet turned up in the dust at dusk.",
            1 => "A tin badge was found clipped to a saddle strap.",
            2 => "A black-stained cuff was seen by the tracks.",
            _ => "A pale scar cuts across the left cheek."
        });

        return new CaseFile(
            accusationId,
            suspects,
            culpritId,
            openingLead,
            knownClues,
            publicClues: publicClues,
            killerReleaseThreshold: 2);
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
                SuspectStatus.AtLarge)
        };

    private static Clue CreateClue(string seedCode, string clueKey, ClueKind kind, string description, params SuspectId[] linkedSuspectIds)
        => new(
            new ClueId($"{clueKey}-{PickIndex(seedCode, clueKey, 97):00}"),
            kind,
            description,
            linkedSuspectIds);

    private static int PickIndex(string seedCode, string label, int count)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seedCode}|{label}"));
        return (int)(BitConverter.ToUInt64(bytes, 0) % (ulong)count);
    }
}
