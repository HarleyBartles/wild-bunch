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
