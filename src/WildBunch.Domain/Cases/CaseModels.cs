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
    private readonly List<Clue> _publicClues = [];

    public CaseFile(
        SuspectId? accusation,
        IEnumerable<Suspect> suspects,
        SuspectId trueCulpritId,
        IEnumerable<Clue> knownClues,
        IEnumerable<Clue>? publicClues = null)
    {
        ArgumentNullException.ThrowIfNull(suspects);
        ArgumentNullException.ThrowIfNull(knownClues);

        Accusation = accusation;
        _suspects = suspects.ToList();
        TrueCulpritId = trueCulpritId;
        _knownClues.AddRange(knownClues.DistinctBy(clue => clue.Id));
        _publicClues.AddRange((publicClues ?? Array.Empty<Clue>()).DistinctBy(clue => clue.Id));
    }

    public SuspectId? Accusation { get; private set; }

    public IReadOnlyList<Suspect> Suspects => _suspects;

    public SuspectId TrueCulpritId { get; }

    public IReadOnlyList<Clue> KnownClues => _knownClues;

    public IReadOnlyList<Clue> PublicClues => _publicClues;

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

    public Clue? RevealNextPublicClue()
    {
        for (var i = 0; i < _publicClues.Count; i++)
        {
            var clue = _publicClues[i];

            if (_knownClues.Any(existing => existing.Id.Equals(clue.Id)))
            {
                _publicClues.RemoveAt(i);
                i--;
                continue;
            }

            _publicClues.RemoveAt(i);
            _knownClues.Add(clue);
            return clue;
        }

        return null;
    }
}
