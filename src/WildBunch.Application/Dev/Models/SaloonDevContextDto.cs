using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Models;

public sealed record SaloonDevContextDto(
    Guid SessionId,
    string? CurrentActionContext,
    string? CurrentTownId,
    string? CurrentTownName,
    bool SourceSpent,
    ActiveSaloonPoiDto? ActiveSaloonPoi,
    DevSaloonOverrideDto? PendingDevOverride,
    HiddenTruthDevDto? HiddenTruth,
    CitizenInfoDto? CitizenInfo,
    IReadOnlyList<SaloonSuspectDevDto> Suspects);

/// <summary>
/// Dev-only DTO exposing the active saloon person of interest from the current
/// town visit state. Maps directly from TownSourceVisitStates, not recomputed
/// from suspects. See BUNCH-90 and ADR-0032.
/// </summary>
public sealed record ActiveSaloonPoiDto(
    string? SuspectId,
    string? SuspectName,
    string? Descriptor,
    string? PersonOfInterestKind);

/// <summary>
/// Dev-only DTO exposing hidden case truth and saloon loop explanation.
/// Guarded by DevRoleGuard and separated from player DTOs.
/// Per ADR-0030 §7 and the dev-overlay doctrine.
/// </summary>
public sealed record HiddenTruthDevDto(
    string TrueCulpritId,
    string TrueCulpritName,
    string KillerReleaseStatus,
    bool KillerIsReleased,
    string SaloonLoopExplanation);

/// <summary>
/// Dev-only DTO describing the citizen POI shape the backend supports.
/// Citizens are currently generic ("a town clerk from {town}") with no named
/// archetypes. This DTO says that honestly rather than pretending named citizens exist.
/// </summary>
public sealed record CitizenInfoDto(
    string Descriptor,
    bool HasNamedArchetypes,
    IReadOnlyList<string> AvailableArchetypes);

public sealed record SaloonSuspectDevDto(
    string SuspectId,
    string Name,
    bool IsTrueCulprit,
    bool IsEligibleSaloonPoi,
    string? IneligibilityReason,
    bool HasKnownWarrant,
    string? PresenceState,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> IdentifyingFacts,
    IReadOnlyList<string> TraitTags,
    decimal? BountyAmount,
    string? WarrantDisposition,
    IReadOnlyList<string> WarrantKnownFeatures,
    string? WarrantSummary);

public sealed record DevSaloonOverrideDto(
    string ForcedKind,
    string? ForcedSuspectId,
    string? ForcedSuspectName);
