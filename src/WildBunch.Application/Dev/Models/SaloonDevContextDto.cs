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
    IReadOnlyList<SaloonSuspectDevDto> Suspects);

/// <summary>
/// Dev-only DTO exposing the active saloon person of interest from the current
/// town visit state. Maps directly from TownSourceVisitState, not recomputed
/// from suspects. See BUNCH-90 and ADR-0032.
/// </summary>
public sealed record ActiveSaloonPoiDto(
    string? SuspectId,
    string? Descriptor,
    string? PersonOfInterestKind);

/// <summary>
/// Dev-only DTO exposing hidden culprit truth. This is the first dev endpoint
/// to deliberately expose TrueCulpritId and suspect eligibility per ADR-0030 §7.
/// Guarded by DevRoleGuard and separated from player DTOs.
/// </summary>
public sealed record HiddenTruthDevDto(
    string TrueCulpritId,
    string TrueCulpritName);

public sealed record SaloonSuspectDevDto(
    string SuspectId,
    string Name,
    bool IsTrueCulprit,
    bool IsEligibleSaloonPoi,
    string? IneligibilityReason,
    bool HasKnownWarrant,
    string? PresenceState);

public sealed record DevSaloonOverrideDto(
    string ForcedKind,
    string? ForcedSuspectId);
