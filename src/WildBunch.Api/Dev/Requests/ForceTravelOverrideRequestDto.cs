namespace WildBunch.Api.Dev;

public sealed record ForceTravelOverrideRequestDto(
    string ForcedCategory,
    int? FoeSpeed,
    int? FoeFightStrength,
    decimal? FoeMinimumBribe,
    string? EncounterMessage);
