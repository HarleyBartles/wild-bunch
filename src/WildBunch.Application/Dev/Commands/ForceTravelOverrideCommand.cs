namespace WildBunch.Application.Dev.Commands;

public sealed record ForceTravelOverrideCommand(
    Guid GameSessionId,
    string ForcedCategory,
    int? FoeSpeed,
    int? FoeFightStrength,
    decimal? FoeMinimumBribe,
    string? EncounterMessage);
