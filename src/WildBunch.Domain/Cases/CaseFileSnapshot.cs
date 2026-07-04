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
    IReadOnlyList<ClueSnapshot> PublicClues,
    string? AccusationId,
    IReadOnlyList<string> DiscoveredSuspectIds,
    int KillerReleaseThreshold,
    int KillerReleaseProgress,
    IReadOnlyList<WarrantSnapshot> KnownWarrants,
    IReadOnlyList<WarrantSnapshot> PublicWarrants,
    IReadOnlyList<SuspectTurfAssignmentSnapshot> SuspectTurfAssignments,
    IReadOnlyList<WantedSuspectConfrontationSnapshot> WantedSuspectConfrontations,
    IReadOnlyList<SheriffTurnInSettlementSnapshot> SheriffTurnInSettlements)
{
    public static CaseFileSnapshot FromDomain(CaseFile caseFile)
        => new(
            caseFile.Suspects.Select<Suspect, SuspectSnapshot>(SuspectSnapshot.FromDomain).ToArray(),
            caseFile.TrueCulpritId.Value,
            caseFile.OpeningLead,
            caseFile.KnownClues.Select<Clue, ClueSnapshot>(ClueSnapshot.FromDomain).ToArray(),
            caseFile.PublicClues.Select<Clue, ClueSnapshot>(ClueSnapshot.FromDomain).ToArray(),
            caseFile.Accusation?.Value,
            caseFile.DiscoveredSuspectIds.Select(s => s.Value).ToArray(),
            caseFile.KillerReleaseThreshold,
            caseFile.KillerReleaseProgress,
            caseFile.KnownWarrants.Select<Warrant, WarrantSnapshot>(WarrantSnapshot.FromDomain).ToArray(),
            caseFile.PublicWarrants.Select<Warrant, WarrantSnapshot>(WarrantSnapshot.FromDomain).ToArray(),
            caseFile.SuspectTurfAssignments.Select<SuspectTurfAssignment, SuspectTurfAssignmentSnapshot>(SuspectTurfAssignmentSnapshot.FromDomain).ToArray(),
            caseFile.WantedSuspectConfrontations.Select<WantedSuspectConfrontationState, WantedSuspectConfrontationSnapshot>(WantedSuspectConfrontationSnapshot.FromDomain).ToArray(),
            caseFile.SheriffTurnInSettlements.Select<SheriffTurnInSettlementState, SheriffTurnInSettlementSnapshot>(SheriffTurnInSettlementSnapshot.FromDomain).ToArray());

    public CaseFile ToDomain()
        => new(
            accusation: AccusationId is null ? null : new SuspectId(AccusationId),
            suspects: Suspects.Select<SuspectSnapshot, Suspect>(s => s.ToDomain()).ToArray(),
            trueCulpritId: new SuspectId(TrueCulpritId),
            openingLead: OpeningLead,
            knownClues: Clues.Select<ClueSnapshot, Clue>(c => c.ToDomain()).ToArray(),
            discoveredSuspectIds: DiscoveredSuspectIds.Select(id => new SuspectId(id)),
            publicClues: PublicClues.Select<ClueSnapshot, Clue>(c => c.ToDomain()).ToArray(),
            killerReleaseThreshold: KillerReleaseThreshold,
            killerReleaseProgress: KillerReleaseProgress,
            knownWarrants: KnownWarrants.Select<WarrantSnapshot, Warrant>(w => w.ToDomain()),
            publicWarrants: PublicWarrants.Select<WarrantSnapshot, Warrant>(w => w.ToDomain()),
            suspectTurfAssignments: SuspectTurfAssignments.Select<SuspectTurfAssignmentSnapshot, SuspectTurfAssignment>(s => s.ToDomain()),
            wantedSuspectConfrontations: WantedSuspectConfrontations.Select<WantedSuspectConfrontationSnapshot, WantedSuspectConfrontationState>(w => w.ToDomain()),
            sheriffTurnInSettlements: SheriffTurnInSettlements.Select<SheriffTurnInSettlementSnapshot, SheriffTurnInSettlementState>(s => s.ToDomain()));
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

public sealed record WarrantSnapshot(
    string Id,
    string TargetName,
    WarrantTermsSnapshot Terms,
    string Summary)
{
    public static WarrantSnapshot FromDomain(Warrant warrant)
        => new(
            warrant.Id.Value,
            warrant.TargetName,
            WarrantTermsSnapshot.FromDomain(warrant.Terms),
            warrant.Summary);

    public Warrant ToDomain()
        => new(
            new WarrantId(Id),
            TargetName,
            Terms.ToDomain(),
            Summary);
}

public sealed record WarrantTermsSnapshot(
    WarrantDisposition Disposition,
    decimal BountyAmount,
    IReadOnlyList<string> KnownAliases,
    IReadOnlyList<string> KnownFeatures,
    string IssuingSource,
    InvestigationTargetKind TargetKind,
    IReadOnlyList<OutlawGangId> GangAffiliations,
    OutlawGangId? AdvancesGangPressureFor,
    InvestigationSourceKind? SourceKind)
{
    public static WarrantTermsSnapshot FromDomain(WarrantTerms terms)
        => new(
            terms.Disposition,
            terms.BountyAmount,
            terms.KnownAliases.ToArray(),
            terms.KnownFeatures.ToArray(),
            terms.IssuingSource,
            terms.TargetKind,
            terms.GangAffiliations.ToArray(),
            terms.AdvancesGangPressureFor,
            terms.SourceKind);

    public WarrantTerms ToDomain()
        => new(
            Disposition,
            BountyAmount,
            KnownAliases,
            KnownFeatures,
            IssuingSource,
            TargetKind,
            GangAffiliations,
            AdvancesGangPressureFor,
            SourceKind);
}

public sealed record SuspectTurfAssignmentSnapshot(string SuspectId, string TurfTownId)
{
    public static SuspectTurfAssignmentSnapshot FromDomain(SuspectTurfAssignment assignment)
        => new(assignment.SuspectId.Value, assignment.TurfTownId.Value);

    public SuspectTurfAssignment ToDomain()
        => new(new SuspectId(SuspectId), new TownId(TurfTownId));
}

public sealed record WantedSuspectConfrontationSnapshot(
    string SuspectId,
    string TargetName,
    WarrantDisposition Disposition,
    WantedSuspectConfrontationOutcome Outcome,
    bool IsAlive,
    bool IsSecured,
    int Day,
    int Turn)
{
    public static WantedSuspectConfrontationSnapshot FromDomain(WantedSuspectConfrontationState state)
        => new(
            state.SuspectId.Value,
            state.TargetName,
            state.Disposition,
            state.Outcome,
            state.IsAlive,
            state.IsSecured,
            state.Day,
            state.Turn);

    public WantedSuspectConfrontationState ToDomain()
        => new(
            new SuspectId(SuspectId),
            TargetName,
            Disposition,
            Outcome,
            IsAlive,
            IsSecured,
            Day,
            Turn);
}

public sealed record SheriffTurnInSettlementSnapshot(
    string SuspectId,
    string TargetName,
    WarrantDisposition Disposition,
    bool IsAlive,
    decimal BountyAmount,
    int Day,
    int Turn)
{
    public static SheriffTurnInSettlementSnapshot FromDomain(SheriffTurnInSettlementState state)
        => new(
            state.SuspectId.Value,
            state.TargetName,
            state.Disposition,
            state.IsAlive,
            state.BountyAmount,
            state.Day,
            state.Turn);

    public SheriffTurnInSettlementState ToDomain()
        => new(
            new SuspectId(SuspectId),
            TargetName,
            Disposition,
            IsAlive,
            BountyAmount,
            Day,
            Turn);
}
