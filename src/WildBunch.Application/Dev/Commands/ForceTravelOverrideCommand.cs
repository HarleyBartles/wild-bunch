using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Commands;

public sealed record ForceTravelOverrideCommand(
    Guid GameSessionId,
    TravelDayEncounterCategory ForcedCategory,
    int? FoeSpeed,
    int? FoeFightStrength,
    decimal? FoeMinimumBribe,
    string? EncounterMessage);
