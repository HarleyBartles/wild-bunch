namespace WildBunch.Application.Dev.Models;

public sealed record ForceSaloonOverrideRequestDto(
    string ForcedKind,
    string? ForcedSuspectId);
