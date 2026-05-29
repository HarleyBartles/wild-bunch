namespace WildBunch.Domain.Cases;

public readonly record struct SuspectId(string Value);

public readonly record struct ClueId(string Value);

public sealed record Crime(string Name, string Description);

public sealed record Suspect(
    SuspectId Id,
    string Name,
    SuspectTraits Traits,
    SuspectStatus Status);

public readonly record struct SuspectTraits(bool IsLocal, bool IsArmed, bool IsDesperate);

public enum SuspectStatus
{
    AtLarge = 0,
    Captured = 1,
    Exonerated = 2
}

public sealed record Clue(ClueId Id, ClueKind Kind, string Description);

public enum ClueKind
{
    Physical = 0,
    Witness = 1,
    Record = 2,
    Rumor = 3
}

public sealed class CaseFile
{
    private readonly List<Suspect> _suspects;
    private readonly List<Clue> _knownClues = [];

    public CaseFile(SuspectId? accusation, IEnumerable<Suspect> suspects, SuspectId trueCulpritId, IEnumerable<Clue> knownClues)
    {
        ArgumentNullException.ThrowIfNull(suspects);
        ArgumentNullException.ThrowIfNull(knownClues);

        Accusation = accusation;
        _suspects = suspects.ToList();
        TrueCulpritId = trueCulpritId;
        _knownClues.AddRange(knownClues.DistinctBy(clue => clue.Id));
    }

    public SuspectId? Accusation { get; private set; }

    public IReadOnlyList<Suspect> Suspects => _suspects;

    public SuspectId TrueCulpritId { get; }

    public IReadOnlyList<Clue> KnownClues => _knownClues;

    public void SetAccusation(SuspectId suspectId)
    {
        Accusation = suspectId;
    }

    public void AddClue(Clue clue)
    {
        if (_knownClues.Any(existing => existing.Id.Equals(clue.Id)))
        {
            return;
        }

        _knownClues.Add(clue);
    }
}
