namespace WildBunch.Domain.Cases;

public readonly record struct SuspectId(string Value);

public readonly record struct ClueId(string Value);

public sealed record Crime(string Name, string Description);

public sealed record Suspect(
    SuspectId Id,
    string Name,
    SuspectProfile Profile,
    SuspectTraits Traits,
    SuspectStatus Status)
{
    public Suspect(SuspectId id, string name, SuspectTraits traits, SuspectStatus status)
        : this(id, name, SuspectProfile.Empty, traits, status)
    {
    }
}

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
    private readonly List<SuspectId> _discoveredSuspectIds = [];
    private readonly List<Clue> _knownClues = [];
    private readonly List<Clue> _publicClues = [];
    private int _killerReleaseProgress;

    public CaseFile(
        SuspectId? accusation,
        IEnumerable<Suspect> suspects,
        SuspectId trueCulpritId,
        IEnumerable<Clue> knownClues,
        IEnumerable<SuspectId>? discoveredSuspectIds = null,
        IEnumerable<Clue>? publicClues = null)
        : this(
            accusation,
            suspects,
            trueCulpritId,
            CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues,
            discoveredSuspectIds,
            publicClues)
    {
    }

    public CaseFile(
        SuspectId? accusation,
        IEnumerable<Suspect> suspects,
        SuspectId trueCulpritId,
        CaseOpeningLead openingLead,
        IEnumerable<Clue> knownClues,
        IEnumerable<SuspectId>? discoveredSuspectIds = null,
        IEnumerable<Clue>? publicClues = null,
        int killerReleaseThreshold = 2,
        int killerReleaseProgress = 0)
    {
        ArgumentNullException.ThrowIfNull(suspects);
        ArgumentNullException.ThrowIfNull(knownClues);
        ArgumentNullException.ThrowIfNull(openingLead);

        Accusation = accusation;
        _suspects = suspects.ToList();
        TrueCulpritId = trueCulpritId;
        OpeningLead = openingLead;
        KillerReleaseThreshold = Math.Max(1, killerReleaseThreshold);
        _killerReleaseProgress = Math.Max(0, killerReleaseProgress);
        _discoveredSuspectIds.AddRange((discoveredSuspectIds ?? Array.Empty<SuspectId>()).DistinctBy(suspectId => suspectId.Value));
        _knownClues.AddRange(knownClues.DistinctBy(clue => clue.Id));
        _publicClues.AddRange((publicClues ?? Array.Empty<Clue>()).DistinctBy(clue => clue.Id));
    }

    public SuspectId? Accusation { get; private set; }

    public IReadOnlyList<Suspect> Suspects => _suspects;

    public IReadOnlyList<SuspectId> DiscoveredSuspectIds => _discoveredSuspectIds;

    public SuspectId TrueCulpritId { get; }

    public CaseOpeningLead OpeningLead { get; }

    public KillerReleaseState KillerReleaseState => new(_killerReleaseProgress, KillerReleaseThreshold);

    public int KillerReleaseThreshold { get; }

    public int KillerReleaseProgress => _killerReleaseProgress;

    public IReadOnlyList<Clue> KnownClues => _knownClues;

    public IReadOnlyList<Clue> PublicClues => _publicClues;

    public IReadOnlyList<Suspect> GetDiscoveredSuspects()
        => _suspects.Where(suspect => _discoveredSuspectIds.Any(discovered => discovered.Equals(suspect.Id))).ToArray();

    public bool IsSuspectDiscovered(SuspectId suspectId)
        => _discoveredSuspectIds.Any(discovered => discovered.Equals(suspectId));

    public bool DiscoverSuspect(SuspectId suspectId)
    {
        if (!_suspects.Any(suspect => suspect.Id.Equals(suspectId)))
        {
            throw new ArgumentException("The suspect does not belong to this case.", nameof(suspectId));
        }

        if (IsSuspectDiscovered(suspectId))
        {
            return false;
        }

        _discoveredSuspectIds.Add(suspectId);
        return true;
    }

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
            DiscoverSuspectsFromClue(clue);
            AdvanceKillerReleaseProgress();
            return clue;
        }

        return null;
    }

    private void AdvanceKillerReleaseProgress()
    {
        _killerReleaseProgress = Math.Min(KillerReleaseThreshold, _killerReleaseProgress + 1);
    }

    private void DiscoverSuspectsFromClue(Clue clue)
    {
        foreach (var suspect in _suspects)
        {
            if (MatchesSuspectReference(clue.Description, suspect))
            {
                DiscoverSuspect(suspect.Id);
            }
        }
    }

    private static bool MatchesSuspectReference(string clueDescription, Suspect suspect)
    {
        if (clueDescription.Contains(suspect.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return suspect.Profile.Aliases.Any(alias => clueDescription.Contains(alias.Name, StringComparison.OrdinalIgnoreCase));
    }
}
