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

public sealed record SuspectTraits
{
    public static SuspectTraits Empty { get; } = new(Array.Empty<SuspectTraitTag>());

    public SuspectTraits(IEnumerable<SuspectTraitTag> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        Tags = tags
            .Select(tag => Normalize(tag))
            .DistinctBy(tag => tag.Value, StringComparer.Ordinal)
            .ToArray();
    }

    public SuspectTraits(bool isLocal, bool isArmed, bool isDesperate)
        : this(CreateLegacyTags(isLocal, isArmed, isDesperate))
    {
    }

    public static SuspectTraits FromTags(params SuspectTraitTag[] tags)
        => new(tags);

    public static SuspectTraits FromLegacyFlags(bool isLocal, bool isArmed, bool isDesperate)
        => new(isLocal, isArmed, isDesperate);

    public IReadOnlyList<SuspectTraitTag> Tags { get; }

    public bool IsLocal => HasTag(SuspectTraitTags.Local);

    public bool IsArmed => HasTag(SuspectTraitTags.Armed);

    public bool IsDesperate => HasTag(SuspectTraitTags.Desperate);

    public bool HasTag(SuspectTraitTag tag)
        => Tags.Any(existing => string.Equals(existing.Value, Normalize(tag).Value, StringComparison.Ordinal));

    private static IEnumerable<SuspectTraitTag> CreateLegacyTags(bool isLocal, bool isArmed, bool isDesperate)
    {
        if (isLocal)
        {
            yield return SuspectTraitTags.Local;
        }

        if (isArmed)
        {
            yield return SuspectTraitTags.Armed;
        }

        if (isDesperate)
        {
            yield return SuspectTraitTags.Desperate;
        }
    }

    private static SuspectTraitTag Normalize(SuspectTraitTag tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag.Value);
        return new SuspectTraitTag(tag.Value.Trim().ToLowerInvariant());
    }
}

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
        InvestigationSourceKind? sourceKind = null,
        string? source = null,
        string? context = null)
    {
        ArgumentNullException.ThrowIfNull(description);

        Id = id;
        Kind = kind;
        Description = description;
        TargetKind = targetKind;
        SourceKind = sourceKind;
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

    public InvestigationSourceKind? SourceKind { get; }

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
