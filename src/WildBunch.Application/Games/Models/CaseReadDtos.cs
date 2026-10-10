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
    int? Turn,
    string? TimeOfDayLabel = null);

public sealed record ClueDirectionAnchorDto(
    string Label,
    string? Movement,
    string? Route);

public sealed record CaseBoardDto(
    IReadOnlyList<CaseWarrantRecordDto> Warrants,
    IReadOnlyList<CaseClueDto> Clues);

public sealed record CaseWarrantRecordDto(
    string Id,
    string TargetName,
    WarrantDisposition Disposition,
    decimal BountyAmount,
    IReadOnlyList<string> KnownAliases,
    IReadOnlyList<string> KnownFeatures,
    string IssuingSource,
    string Summary,
    SheriffTurnInSettlementDto? Settlement);

public sealed record SheriffTurnInSettlementDto(
    bool IsAlive,
    decimal BountyAmount,
    int Day,
    int Turn);

public sealed record CaseClueDto(
    string Id,
    ClueKind Kind,
    string Description,
    InvestigationSourceKind? SourceKind,
    string? Source,
    string? Context,
    ClueAnchorsDto Anchors);
