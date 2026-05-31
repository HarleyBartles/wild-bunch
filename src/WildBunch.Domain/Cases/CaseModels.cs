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
