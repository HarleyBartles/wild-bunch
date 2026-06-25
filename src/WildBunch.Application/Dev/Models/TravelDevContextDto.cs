using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Models;

public sealed record TravelDevContextDto(
    Guid SessionId,
    bool HasActiveJourney,
    string? JourneyStatus,
    int? DaysTravelled,
    int? RemainingDays,
    string? PendingEncounterKind,
    string? PendingEncounterMessage,
    FoeProfileDevDto? PendingFoeProfile,
    DevOverrideDto? PendingDevOverride);

public sealed record FoeProfileDevDto(
    int Speed,
    int FightStrength,
    decimal MinimumBribe,
    string SpeedBand,
    string FightBand,
    string BribeBand);

public sealed record DevOverrideDto(
    string ForcedCategory,
    FoeProfileDevDto? FoeProfile,
    string? EncounterMessage);
