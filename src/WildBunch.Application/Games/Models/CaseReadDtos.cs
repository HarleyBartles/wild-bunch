using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Models;

public sealed record CaseStateDto(
    string StatusText);

public sealed record ClueAnchorsDto(
    IReadOnlyList<ClueSubjectAnchorDto> Subjects,
    IReadOnlyList<ClueLocationAnchorDto> Locations,
    IReadOnlyList<ClueTimeAnchorDto> Times,
    IReadOnlyList<ClueDirectionAnchorDto> Directions);

public sealed record ClueSubjectAnchorDto(
    string Label,
    string? Alias,
    string? Feature,
    string? Fact);

public sealed record ClueLocationAnchorDto(
    string Label,
    string? Place,
    string? Route);

public sealed record ClueTimeAnchorDto(
    ClueRecency Recency,
    int? Day,
    int? Turn);

public sealed record ClueDirectionAnchorDto(
    string Label,
    string? Movement,
    string? Route);

public enum CaseIdentityKind
{
    KnownName = 0,
    Alias = 1,
    FeatureLed = 2,
    RouteLed = 3,
    WarrantTarget = 4
}

public enum CaseIdentityStatus
{
    Unresolved = 0,
    PossibleMatch = 1,
    Resolved = 2,
    Captured = 3
}

public sealed record CaseBoardDto(
    IReadOnlyList<CaseIdentityHandleDto> NamedRecords,
    IReadOnlyList<CaseIdentityHandleDto> LooseLeads,
    IReadOnlyList<CaseEvidenceItemDto> EvidenceItems);

public sealed record CaseIdentityHandleDto(
    string Id,
    string DisplayName,
    CaseIdentityKind Kind,
    CaseIdentityStatus Status,
    string? ResolvedToDisplayName,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> SummaryLines,
    IReadOnlyList<string> RelatedLabels,
    IReadOnlyList<string> KnownAliases,
    IReadOnlyList<string> DistinguishingFeatures,
    WarrantDisposition? WarrantDisposition,
    decimal? BountyAmount,
    string? IssuingAuthority,
    string? CrimeSummary);

public sealed record CaseEvidenceItemDto(
    string Id,
    string KindLabel,
    string SourceLabel,
    string Summary,
    bool IdentityBearing,
    ClueAnchorsDto Anchors,
    IReadOnlyList<string> HandleIds);
