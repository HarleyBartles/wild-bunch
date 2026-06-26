namespace WildBunch.Application.Dev.Models;

public sealed record ForceTravelOverrideRequestDto(
    string ForcedCategory,
    int? FoeSpeed,
    int? FoeFightStrength,
    decimal? FoeMinimumBribe,
    string? EncounterMessage);
