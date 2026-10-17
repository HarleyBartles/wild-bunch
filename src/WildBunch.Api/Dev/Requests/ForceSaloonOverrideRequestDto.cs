namespace WildBunch.Api.Dev;

public sealed record ForceSaloonOverrideRequestDto(
    string ForcedKind,
    string? ForcedSuspectId,
    string? ForcedCitizenRoleKey);
