using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Domain.Cases;

/// <summary>
/// Immutable snapshot of a generated caseFile for event storage and replay.
/// Carried by the CaseFileGenerated domain event.
/// </summary>
public sealed record CaseFileSnapshot(
    IReadOnlyList<SuspectSnapshot> Suspects,
    string TrueCulpritId,
    CaseOpeningLead OpeningLead,
    IReadOnlyList<ClueSnapshot> Clues,
    IReadOnlyList<ClueSnapshot> PublicClues)
{
    public static CaseFileSnapshot FromDomain(CaseFile caseFile)
        => new(
            caseFile.Suspects.Select<Suspect, SuspectSnapshot>(SuspectSnapshot.FromDomain).ToArray(),
            caseFile.TrueCulpritId.Value,
            caseFile.OpeningLead,
            caseFile.KnownClues.Select<Clue, ClueSnapshot>(ClueSnapshot.FromDomain).ToArray(),
            caseFile.PublicClues.Select<Clue, ClueSnapshot>(ClueSnapshot.FromDomain).ToArray());

    public CaseFile ToDomain()
        => new(
            accusation: null,
            suspects: Suspects.Select<SuspectSnapshot, Suspect>(s => s.ToDomain()).ToArray(),
            trueCulpritId: new SuspectId(TrueCulpritId),
            openingLead: OpeningLead,
            knownClues: Clues.Select<ClueSnapshot, Clue>(c => c.ToDomain()).ToArray(),
            publicClues: PublicClues.Select<ClueSnapshot, Clue>(c => c.ToDomain()).ToArray());
}

public sealed record SuspectSnapshot(
    string Id,
    string Name,
    SuspectProfileSnapshot Profile,
    string[] TraitsTags,
    string Status)
{
    public static SuspectSnapshot FromDomain(Suspect suspect)
        => new(
            suspect.Id.Value,
            suspect.Name,
            SuspectProfileSnapshot.FromDomain(suspect.Profile),
            suspect.Traits.Tags.Select<SuspectTraitTag, string>(tag => tag.Value).ToArray(),
            suspect.Status.ToString());

    public Suspect ToDomain()
        => new(
            new SuspectId(Id),
            Name,
            Profile.ToDomain(),
            SuspectTraits.FromTags(TraitsTags.Select<string, SuspectTraitTag>(tag => new SuspectTraitTag(tag)).ToArray()),
            Enum.Parse<SuspectStatus>(Status));
}

public sealed record SuspectProfileSnapshot(
    IReadOnlyList<SuspectAliasSnapshot> Aliases,
    IReadOnlyList<SuspectIdentityFactSnapshot> IdentifyingFacts)
{
    public static SuspectProfileSnapshot FromDomain(SuspectProfile profile)
        => new(
            profile.Aliases.Select<SuspectAlias, SuspectAliasSnapshot>(a => SuspectAliasSnapshot.FromDomain(a)).ToArray(),
            profile.IdentifyingFacts.Select<SuspectIdentityFact, SuspectIdentityFactSnapshot>(f => SuspectIdentityFactSnapshot.FromDomain(f)).ToArray());

    public SuspectProfile ToDomain()
        => new(
            Aliases.Select<SuspectAliasSnapshot, SuspectAlias>(a => a.ToDomain()).ToArray(),
            IdentifyingFacts.Select<SuspectIdentityFactSnapshot, SuspectIdentityFact>(f => f.ToDomain()).ToArray());
}

public sealed record SuspectAliasSnapshot(string Name, string AliasKind)
{
    public static SuspectAliasSnapshot FromDomain(SuspectAlias alias)
        => new(alias.Name, alias.Kind.ToString());

    public SuspectAlias ToDomain()
        => new(Name, Enum.Parse<AliasKind>(AliasKind));
}

public sealed record SuspectIdentityFactSnapshot(string Raw, string ThirdPerson, string FirstPerson, bool IsPrimary)
{
    public static SuspectIdentityFactSnapshot FromDomain(SuspectIdentityFact fact)
        => new(fact.Language.HasForm, fact.Language.WithForm, fact.Language.WhoForm, fact.IsPrimary);

    public SuspectIdentityFact ToDomain()
        => new(FeatureLanguage.Raw(Raw, ThirdPerson, FirstPerson), IsPrimary);
}

public sealed record ClueSnapshot(
    string Id,
    string Kind,
    string Description,
    string[] LinkedSuspectIds,
    string TargetKind,
    string? SourceKind,
    string? Source,
    string? Context,
    ClueAnchorsSnapshot Anchors)
{
    public static ClueSnapshot FromDomain(Clue clue)
        => new(
            clue.Id.Value,
            clue.Kind.ToString(),
            clue.Description,
            clue.LinkedSuspectIds.Select<SuspectId, string>(id => id.Value).ToArray(),
            clue.TargetKind.ToString(),
            clue.SourceKind?.ToString(),
            clue.Source,
            clue.Context,
            ClueAnchorsSnapshot.FromDomain(clue.Anchors));

    public Clue ToDomain()
        => new(
            new ClueId(Id),
            Enum.Parse<ClueKind>(Kind),
            Description,
            LinkedSuspectIds.Select<string, SuspectId>(id => new SuspectId(id)).ToArray(),
            Enum.Parse<InvestigationTargetKind>(TargetKind),
            SourceKind is null ? null : Enum.Parse<InvestigationSourceKind>(SourceKind),
            Source,
            Context,
            Anchors.ToDomain());
}

public sealed record ClueAnchorsSnapshot(
    ClueSubjectAnchorSnapshot[] Subjects,
    ClueLocationAnchorSnapshot[] Locations,
    ClueTimeAnchorSnapshot[] Times,
    ClueDirectionAnchorSnapshot[] Directions)
{
    public static ClueAnchorsSnapshot FromDomain(ClueAnchors anchors)
        => new(
            anchors.Subjects.Select<ClueSubjectAnchor, ClueSubjectAnchorSnapshot>(s => ClueSubjectAnchorSnapshot.FromDomain(s)).ToArray(),
            anchors.Locations.Select<ClueLocationAnchor, ClueLocationAnchorSnapshot>(l => ClueLocationAnchorSnapshot.FromDomain(l)).ToArray(),
            anchors.Times.Select<ClueTimeAnchor, ClueTimeAnchorSnapshot>(t => ClueTimeAnchorSnapshot.FromDomain(t)).ToArray(),
            anchors.Directions.Select<ClueDirectionAnchor, ClueDirectionAnchorSnapshot>(d => ClueDirectionAnchorSnapshot.FromDomain(d)).ToArray());

    public ClueAnchors ToDomain()
        => new(
            Subjects.Select<ClueSubjectAnchorSnapshot, ClueSubjectAnchor>(s => s.ToDomain()).ToArray(),
            Locations.Select<ClueLocationAnchorSnapshot, ClueLocationAnchor>(l => l.ToDomain()).ToArray(),
            Times.Select<ClueTimeAnchorSnapshot, ClueTimeAnchor>(t => t.ToDomain()).ToArray(),
            Directions.Select<ClueDirectionAnchorSnapshot, ClueDirectionAnchor>(d => d.ToDomain()).ToArray());
}

public sealed record ClueSubjectAnchorSnapshot(
    string Label,
    string? SuspectId,
    string? Alias,
    string? Feature,
    string? Fact)
{
    public static ClueSubjectAnchorSnapshot FromDomain(ClueSubjectAnchor anchor)
        => new(anchor.Label, anchor.SuspectId?.Value, anchor.Alias, anchor.Feature, anchor.Fact);

    public ClueSubjectAnchor ToDomain()
        => new(Label, SuspectId is null ? null : new SuspectId(SuspectId), Alias, Feature, Fact);
}

public sealed record ClueLocationAnchorSnapshot(string Label, string? TownId, string? Place, string? Route)
{
    public static ClueLocationAnchorSnapshot FromDomain(ClueLocationAnchor anchor)
        => new(anchor.Label, anchor.TownId?.Value, anchor.Place, anchor.Route);

    public ClueLocationAnchor ToDomain()
        => new(Label, TownId is null ? null : new TownId(TownId), Place, Route);
}

public sealed record ClueTimeAnchorSnapshot(string Recency, int? Day, int? Turn)
{
    public static ClueTimeAnchorSnapshot FromDomain(ClueTimeAnchor anchor)
        => new(anchor.Recency.ToString(), anchor.Day, anchor.Turn);

    public ClueTimeAnchor ToDomain()
        => new(Enum.Parse<ClueRecency>(Recency), Day, Turn);
}

public sealed record ClueDirectionAnchorSnapshot(string Label, string? Movement, string? DestinationTownId, string? Route)
{
    public static ClueDirectionAnchorSnapshot FromDomain(ClueDirectionAnchor anchor)
        => new(anchor.Label, anchor.Movement, anchor.DestinationTownId?.Value, anchor.Route);

    public ClueDirectionAnchor ToDomain()
        => new(Label, Movement, DestinationTownId is null ? null : new TownId(DestinationTownId), Route);
}
