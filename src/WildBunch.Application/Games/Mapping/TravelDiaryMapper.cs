using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using DomainTravelDiaryDayState = WildBunch.Domain.Travel.TravelDiaryDayState;
using DomainTravelDiaryEncounterResolutionState = WildBunch.Domain.Travel.TravelDiaryEncounterResolutionState;
using DomainTravelRulesProfile = WildBunch.Domain.Travel.TravelRulesProfile;

namespace WildBunch.Application.Games.Mapping;

public static class TravelDiaryMapper
{
    public static TravelDiaryDto? ToDto(IReadOnlyList<DomainTravelDiaryDayState> days, DomainTravelRulesProfile? travelRulesProfile = null)
        => ToDto(days, Array.Empty<GameLogEntry>(), travelRulesProfile);

    public static TravelDiaryDto? ToDto(
        IReadOnlyList<DomainTravelDiaryDayState> days,
        IReadOnlyList<GameLogEntry> logEntries,
        DomainTravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= DomainTravelRulesProfile.Default;

        var latestJourneySequence = logEntries
            .Where(entry => entry.Kind == GameLogEntryKind.Travel && entry.JourneySequence.HasValue)
            .Select(entry => entry.JourneySequence)
            .Max();
        var journeyEntries = latestJourneySequence.HasValue
            ? logEntries
                .Where(entry => entry.Kind == GameLogEntryKind.Travel && entry.JourneySequence == latestJourneySequence)
                .Select(JournalMapper.ToEntryDto)
                .ToArray()
            : Array.Empty<GameLogEntryDto>();

        if (days.Count == 0 && journeyEntries.Length == 0)
        {
            return null;
        }

        return new TravelDiaryDto(journeyEntries, days.Select(day => ToDto(day, travelRulesProfile)).ToArray());
    }

    private static TravelDiaryDayDto ToDto(
        DomainTravelDiaryDayState day,
        DomainTravelRulesProfile travelRulesProfile)
    {
        var beatSlots = TrailBeatSlotProjection.FromDayState(day);

        return new TravelDiaryDayDto(
            day.DayNumber,
            day.OriginTownName,
            day.DestinationTownName,
            day.StartingTravelMode,
            day.EndingTravelMode,
            day.Status,
            day.StartingRideDayDistance,
            day.RemainingRideDayDistance,
            day.StartingDaysRemaining,
            day.RemainingDays,
            TravelMapper.ToHorseDto(day.HorseStateBefore, travelRulesProfile),
            TravelMapper.ToHorseDto(day.HorseStateAfter, travelRulesProfile),
            day.TrailEvent is null ? null : TravelMapper.ToDto(day.TrailEvent),
            day.PendingEncounter is null ? null : TravelMapper.ToDto(day.PendingEncounter),
            day.EncounterResolution is null ? null : ToDto(day.EncounterResolution),
            day.HealthDelta,
            day.WalletDelta,
            day.FoodDelta,
            day.HorseFeedDelta,
            day.CanteenChargeDelta,
            day.AmmoSpent,
            day.HorseHungerDelta,
            day.HorseThirstDelta,
            day.HorseExhaustionDelta,
            day.DelayDays,
            day.HeatIncrease,
            day.CurrentHealth,
            day.CurrentWallet,
            day.CurrentFood,
            day.CurrentHorseFeed,
            day.CurrentCanteenCharges,
            day.CurrentAmmo,
            day.CurrentHeat,
            day.Warnings,
            beatSlots);
    }

    private static TravelDiaryEncounterResolutionDto ToDto(DomainTravelDiaryEncounterResolutionState resolution)
        => new(
            resolution.ChoiceId,
            resolution.ChoiceLabel,
            resolution.HealthDelta,
            resolution.WalletDelta,
            resolution.AmmoSpent,
            resolution.HeatIncrease,
            resolution.HorseExhaustionDelta,
            resolution.ContinuedOnFoot);
}
