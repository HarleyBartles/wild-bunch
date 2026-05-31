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

public sealed record Clue
{
    public Clue(ClueId id, ClueKind kind, string description)
        : this(id, kind, description, Array.Empty<SuspectId>())
    {
    }

    public Clue(ClueId id, ClueKind kind, string description, IEnumerable<SuspectId>? linkedSuspectIds)
        : this(id, kind, description, linkedSuspectIds, InvestigationTargetKind.Unknown)
    {
    }

    public Clue(
        ClueId id,
        ClueKind kind,
        string description,
        IEnumerable<SuspectId>? linkedSuspectIds,
        InvestigationTargetKind targetKind,
        string? source = null,
        string? context = null)
    {
        ArgumentNullException.ThrowIfNull(description);

        Id = id;
        Kind = kind;
        Description = description;
        TargetKind = targetKind;
        Source = source;
        Context = context;
        LinkedSuspectIds = (linkedSuspectIds ?? Array.Empty<SuspectId>())
            .DistinctBy(suspectId => suspectId.Value)
            .ToArray();
    }

    public ClueId Id { get; }

    public ClueKind Kind { get; }

    public string Description { get; }

    public InvestigationTargetKind TargetKind { get; }

    public string? Source { get; }

    public string? Context { get; }

    public IReadOnlyList<SuspectId> LinkedSuspectIds { get; }
}

public enum ClueKind
{
    Physical = 0,
    Witness = 1,
    Record = 2,
    Rumor = 3,
    CulpritTrail = 4,
    IdentityFact = 5,
    Alias = 6,
    Whereabouts = 7,
    Warrant = 8,
    Contradiction = 9,
    Context = 10
}

public sealed class CaseFile
{
    private readonly List<Suspect> _suspects;
    private readonly List<SuspectId> _discoveredSuspectIds = [];
    private readonly List<Clue> _knownClues = [];
    private readonly List<Clue> _publicClues = [];
    private readonly List<Warrant> _knownWarrants = [];
    private readonly List<Warrant> _publicWarrants = [];
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
        int killerReleaseProgress = 0,
        IEnumerable<Warrant>? knownWarrants = null,
        IEnumerable<Warrant>? publicWarrants = null)
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
        _knownWarrants.AddRange((knownWarrants ?? Array.Empty<Warrant>()).DistinctBy(warrant => warrant.Id));
        _publicWarrants.AddRange((publicWarrants ?? Array.Empty<Warrant>()).DistinctBy(warrant => warrant.Id));
    }

    public SuspectId? Accusation { get; private set; }

    public IReadOnlyList<Suspect> Suspects => _suspects;

    public IReadOnlyList<Suspect> GangRoster => _suspects;

    public IReadOnlyList<SuspectId> DiscoveredSuspectIds => _discoveredSuspectIds;

    public SuspectId TrueCulpritId { get; }

    public CaseOpeningLead OpeningLead { get; }

    public KillerReleaseState KillerReleaseState => new(_killerReleaseProgress, KillerReleaseThreshold);

    public int KillerReleaseThreshold { get; }

    public int KillerReleaseProgress => _killerReleaseProgress;

    public IReadOnlyList<Clue> KnownClues => _knownClues;

    public IReadOnlyList<Clue> PublicClues => _publicClues;

    public IReadOnlyList<Warrant> KnownWarrants => _knownWarrants;

    public IReadOnlyList<Warrant> PublicWarrants => _publicWarrants;

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
        => DiscoverClue(clue);

    public bool DiscoverClue(Clue clue, bool advanceKillerReleaseProgress = false)
    {
        ArgumentNullException.ThrowIfNull(clue);

        if (_knownClues.Any(existing => existing.Id.Equals(clue.Id)))
        {
            return false;
        }

        _knownClues.Add(clue);
        DiscoverSuspectsFromClue(clue);

        if (advanceKillerReleaseProgress)
        {
            AdvanceKillerReleaseProgress();
        }

        return true;
    }

    public void AddWarrant(Warrant warrant)
        => DiscoverWarrant(warrant);

    public bool DiscoverWarrant(Warrant warrant)
    {
        ArgumentNullException.ThrowIfNull(warrant);

        if (_knownWarrants.Any(existing => existing.Id.Equals(warrant.Id)))
        {
            return false;
        }

        _knownWarrants.Add(warrant);
        return true;
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
            if (DiscoverClue(clue, advanceKillerReleaseProgress: true))
            {
                return clue;
            }
        }

        return null;
    }

    public Warrant? RevealNextPublicWarrant()
    {
        for (var i = 0; i < _publicWarrants.Count; i++)
        {
            var warrant = _publicWarrants[i];

            if (_knownWarrants.Any(existing => existing.Id.Equals(warrant.Id)))
            {
                _publicWarrants.RemoveAt(i);
                i--;
                continue;
            }

            _publicWarrants.RemoveAt(i);
            if (DiscoverWarrant(warrant))
            {
                return warrant;
            }
        }

        return null;
    }

    private void AdvanceKillerReleaseProgress()
    {
        _killerReleaseProgress = Math.Min(KillerReleaseThreshold, _killerReleaseProgress + 1);
    }

    private void DiscoverSuspectsFromClue(Clue clue)
    {
        foreach (var suspectId in clue.LinkedSuspectIds)
        {
            if (_suspects.Any(suspect => suspect.Id.Equals(suspectId)))
            {
                DiscoverSuspect(suspectId);
            }
        }
    }
}
