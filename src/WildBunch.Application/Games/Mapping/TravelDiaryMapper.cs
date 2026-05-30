using WildBunch.Application.Games.Models;
using DomainTravelDiaryDayState = WildBunch.Domain.Travel.TravelDiaryDayState;
using DomainTravelDiaryEncounterResolutionState = WildBunch.Domain.Travel.TravelDiaryEncounterResolutionState;
using DomainTravelRulesProfile = WildBunch.Domain.Travel.TravelRulesProfile;

namespace WildBunch.Application.Games.Mapping;

public static class TravelDiaryMapper
{
    public static TravelDiaryDto? ToDto(IReadOnlyList<DomainTravelDiaryDayState> days, DomainTravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= DomainTravelRulesProfile.Default;

        if (days.Count == 0)
        {
            return null;
        }

        return new TravelDiaryDto(days.Select(day => ToDto(day, travelRulesProfile)).ToArray());
    }

    private static TravelDiaryDayDto ToDto(DomainTravelDiaryDayState day, DomainTravelRulesProfile travelRulesProfile)
        => new(
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
            day.OpeningNarration,
            TravelDiaryTextRenderer.RenderJourneyBeat(day),
            TravelDiaryTextRenderer.RenderResourceBeat(day),
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
            TravelDiaryTextRenderer.RenderEntries(day, travelRulesProfile),
            day.Warnings);

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
