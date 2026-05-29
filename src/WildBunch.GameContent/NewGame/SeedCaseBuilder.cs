using WildBunch.Domain.Cases;

namespace WildBunch.GameContent.NewGame;

internal static class SeedCaseBuilder
{
    public static CaseFile CreateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Jonah Pike", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", new SuspectTraits(IsLocal: false, IsArmed: false, IsDesperate: false), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-3"), "Evan Quill", new SuspectTraits(IsLocal: true, IsArmed: true, IsDesperate: false), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-4"), "Tessa Wren", new SuspectTraits(IsLocal: false, IsArmed: true, IsDesperate: true), SuspectStatus.AtLarge)
        };

        var clues = new[]
        {
            new Clue(new ClueId("clue-1"), ClueKind.Witness, "A dust-covered rider was seen heading east at dusk."),
            new Clue(new ClueId("clue-2"), ClueKind.Record, "The telegraph ledger shows a coded message sent from Red Mesa."),
            new Clue(new ClueId("clue-3"), ClueKind.Physical, "Boot prints match a narrow-heeled trail rider's boots.")
        };

        return new CaseFile(
            accusation: new SuspectId("suspect-2"),
            suspects,
            trueCulpritId: new SuspectId("suspect-4"),
            knownClues: clues);
    }
}
