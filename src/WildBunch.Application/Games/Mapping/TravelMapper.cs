using WildBunch.Application.Games.Models;
using DomainJourneyEncounter = WildBunch.Domain.Travel.JourneyEncounterState;
using DomainJourneySnapshot = WildBunch.Domain.Travel.TravelJourneySnapshot;
using DomainTravelPreview = WildBunch.Domain.Travel.TravelPreview;
using DomainTravelRouteProfile = WildBunch.Domain.Travel.TravelRouteProfile;

namespace WildBunch.Application.Games.Mapping;

public static class TravelMapper
{
    public static TravelPreviewDto ToDto(DomainTravelPreview preview)
        => new(
            preview.OriginTownId.Value,
            preview.OriginTownName,
            preview.DestinationTownId.Value,
            preview.DestinationTownName,
            preview.TravelMode,
            preview.MountedTravelAvailable,
            preview.WaterSecure,
            preview.TotalDistance,
            preview.RemainingDistance,
            preview.ExpectedDays,
            preview.RemainingDays,
            preview.RequiredFood,
            preview.AvailableFood,
            preview.RequiredHorseFeed,
            preview.AvailableHorseFeed,
            preview.HorseCondition,
            preview.Warnings,
            ToDto(preview.RouteProfile));

    public static TravelJourneyDto ToDto(DomainJourneySnapshot snapshot)
        => new(
            snapshot.OriginTownId.Value,
            snapshot.OriginTownName,
            snapshot.DestinationTownId.Value,
            snapshot.DestinationTownName,
            snapshot.TravelMode,
            snapshot.Status,
            snapshot.MountedTravelAvailable,
            snapshot.WaterSecure,
            snapshot.TotalDistance,
            snapshot.RemainingDistance,
            snapshot.ExpectedDays,
            snapshot.RemainingDays,
            snapshot.RequiredFood,
            snapshot.AvailableFood,
            snapshot.RequiredHorseFeed,
            snapshot.AvailableHorseFeed,
            snapshot.HorseCondition,
            snapshot.DaysTravelled,
            snapshot.PendingEncounter is null ? null : ToDto(snapshot.PendingEncounter),
            snapshot.Warnings,
            ToDto(snapshot.RouteProfile));

    public static TravelRouteProfileDto ToDto(DomainTravelRouteProfile routeProfile)
        => new(
            routeProfile.TrailId,
            routeProfile.Risk,
            routeProfile.Terrain,
            routeProfile.WaterFeature,
            routeProfile.TotalDistance,
            routeProfile.MountedDailyProgress,
            routeProfile.FootDailyProgress,
            routeProfile.Warnings);

    public static JourneyEncounterDto ToDto(DomainJourneyEncounter encounter)
        => new(encounter.Kind, encounter.Message);
}
