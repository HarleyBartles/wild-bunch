using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;
using DomainJourneyEncounter = WildBunch.Domain.Travel.JourneyEncounterState;
using DomainJourneyEncounterChoice = WildBunch.Domain.Travel.JourneyEncounterChoiceState;
using DomainJourneyTrailEvent = WildBunch.Domain.Travel.JourneyTrailEventState;
using DomainTravelJourney = WildBunch.Domain.Travel.TravelJourney;
using DomainJourneySnapshot = WildBunch.Domain.Travel.TravelJourneySnapshot;
using DomainTravelPreview = WildBunch.Domain.Travel.TravelPreview;
using DomainTravelRouteProfile = WildBunch.Domain.Travel.TravelRouteProfile;
using DomainTravelRulesProfile = WildBunch.Domain.Travel.TravelRulesProfile;

namespace WildBunch.Application.Games.Mapping;

public static class TravelMapper
{
    public static TravelPreviewDto ToDto(DomainTravelPreview preview, DomainTravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= DomainTravelRulesProfile.Default;

        return new TravelPreviewDto(
            preview.OriginTownId.Value,
            preview.OriginTownName,
            preview.DestinationTownId.Value,
            preview.DestinationTownName,
            preview.TravelMode,
            preview.MountedTravelAvailable,
            preview.WaterSecure,
            preview.RideDayDistance,
            preview.RemainingRideDayDistance,
            preview.BaselineRideDays,
            preview.ExpectedDays,
            preview.RemainingDays,
            preview.CanteenChargesPerDay,
            preview.RequiredCanteenCharges,
            preview.AvailableCanteenCharges,
            preview.CanteenReserveCharges,
            preview.DelayMarginDays,
            preview.DelayRisk,
            preview.RequiredFood,
            preview.AvailableFood,
            preview.RequiredHorseFeed,
            preview.AvailableHorseFeed,
            ToHorseDto(preview.HorseState, travelRulesProfile),
            preview.Warnings,
            ToDto(preview.RouteProfile));
    }

    public static TravelJourneyDto ToDto(DomainTravelJourney journey, DomainTravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= DomainTravelRulesProfile.Default;

        return ToDto(journey.ToSnapshot(travelRulesProfile), travelRulesProfile);
    }

    public static TravelJourneyDto ToDto(DomainJourneySnapshot snapshot, DomainTravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= DomainTravelRulesProfile.Default;

        return new TravelJourneyDto(
            snapshot.OriginTownId.Value,
            snapshot.OriginTownName,
            snapshot.DestinationTownId.Value,
            snapshot.DestinationTownName,
            snapshot.TravelMode,
            snapshot.Status,
            snapshot.MountedTravelAvailable,
            snapshot.WaterSecure,
            snapshot.RideDayDistance,
            snapshot.RemainingRideDayDistance,
            snapshot.RouteProfile.ExpectedDays(TravelMode.Mounted),
            snapshot.ExpectedDays,
            snapshot.RemainingDays,
            snapshot.CanteenChargesPerDay,
            snapshot.RequiredCanteenCharges,
            snapshot.AvailableCanteenCharges,
            snapshot.CanteenReserveCharges,
            snapshot.DelayMarginDays,
            snapshot.DelayRisk,
            snapshot.RequiredFood,
            snapshot.AvailableFood,
            snapshot.RequiredHorseFeed,
            snapshot.AvailableHorseFeed,
            ToHorseDto(snapshot.HorseState, travelRulesProfile),
            snapshot.DaysTravelled,
            snapshot.DelayDays,
            snapshot.PendingEncounter is null ? null : ToDto(snapshot.PendingEncounter),
            snapshot.Warnings,
            ToDto(snapshot.RouteProfile));
    }

    public static HorseTravelStateDto? ToHorseDto(DomainHorseTravelState? horseState, DomainTravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= DomainTravelRulesProfile.Default;

        return horseState is null
            ? null
            : new HorseTravelStateDto(
                horseState.Hunger,
                horseState.Thirst,
                horseState.Exhaustion,
                horseState.IsLameFor(travelRulesProfile),
                horseState.IsDeadFor(travelRulesProfile),
                horseState.CanProvideMountedTravelFor(travelRulesProfile));
    }

    public static TravelRouteProfileDto ToDto(DomainTravelRouteProfile routeProfile)
        => new(
            routeProfile.TrailId,
            routeProfile.Risk,
            routeProfile.Terrain,
            routeProfile.WaterFeature,
            routeProfile.RideDayDistance,
            routeProfile.MountedRideDayProgress,
            routeProfile.FootRideDayProgress,
            routeProfile.Warnings);

    public static JourneyEncounterDto ToDto(DomainJourneyEncounter encounter)
        => new(encounter.Kind, encounter.Message, encounter.Choices.Select(ToDto).ToArray());

    public static JourneyEncounterChoiceDto ToDto(DomainJourneyEncounterChoice choice)
        => new(choice.Id, choice.Label);

    public static JourneyTrailEventDto ToDto(DomainJourneyTrailEvent trailEvent)
        => new(
            trailEvent.Id,
            trailEvent.Kind,
            trailEvent.Title,
            trailEvent.Message,
            trailEvent.WalletDelta,
            trailEvent.FoodDelta,
            trailEvent.CanteenChargeDelta,
            trailEvent.HorseHungerDelta,
            trailEvent.HorseThirstDelta,
            trailEvent.HorseExhaustionDelta,
            trailEvent.DelayDays,
            trailEvent.HeatIncrease);
}
