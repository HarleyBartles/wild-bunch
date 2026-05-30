using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;
using Trail = WildBunch.Domain.World.Trail;
using TrailRisk = WildBunch.Domain.World.TrailRisk;
using WaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Domain.Travel;

public sealed class TravelResolver
{
    private static readonly InventoryCapabilityResolver CapabilityResolver = new();

    public TravelPreviewResult PreviewJourney(
        DomainWorld world,
        TownId currentTownId,
        TownId destinationTownId,
        DomainInventory inventory,
        TravelRulesProfile? travelRulesProfile = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(inventory);
        travelRulesProfile ??= TravelRulesProfile.Default;

        if (!world.TryGetTown(currentTownId, out var originTown))
        {
            return TravelPreviewResult.Failed("Current town could not be found.");
        }

        if (!world.TryGetTown(destinationTownId, out var destinationTown))
        {
            return TravelPreviewResult.Failed("Destination town could not be found.");
        }

        var trail = world.FindConnectedTrail(currentTownId, destinationTownId);
        if (trail is null)
        {
            return TravelPreviewResult.Failed("No trail connects those towns.");
        }

        var capabilities = CapabilityResolver.Resolve(inventory, travelRulesProfile);
        var mountedTravelAvailable = capabilities.MountedTravelAvailable;
        var travelMode = mountedTravelAvailable ? TravelMode.Mounted : TravelMode.Foot;
        var horseState = inventory.GetHorseState();
        var canteenState = inventory.GetCanteenState();
        var routeProfile = BuildRouteProfile(trail, travelRulesProfile);
        var rideDayDistance = routeProfile.RideDayDistance;
        var baselineRideDays = routeProfile.ExpectedDays(TravelMode.Mounted);
        var expectedDays = routeProfile.ExpectedDays(travelMode);
        var availableFood = inventory.GetQuantity(ItemKind.Food);
        var availableHorseFeed = inventory.GetQuantity(ItemKind.HorseFeed);
        var grazingAvailable = JourneyUpkeepRules.HasGrazing(routeProfile.Terrain);
        var routeWaterSecure = JourneyUpkeepRules.HasRouteWater(routeProfile.WaterFeature);
        var livingHorse = horseState is not null && !horseState.IsDeadFor(travelRulesProfile);
        var requiredFood = expectedDays;
        var requiredHorseFeed = livingHorse && !grazingAvailable ? expectedDays : 0;
        var canteenChargesPerDay = routeWaterSecure ? 0 : JourneyUpkeepRules.WaterChargesRequiredPerDay(horseState, travelRulesProfile);
        var requiredCanteenCharges = expectedDays * canteenChargesPerDay;
        var availableCanteenCharges = canteenState?.Charges ?? 0;
        var canteenReserveCharges = availableCanteenCharges - requiredCanteenCharges;
        var delayMarginDays = canteenChargesPerDay == 0 ? 0 : Math.Max(0, canteenReserveCharges / canteenChargesPerDay);
        var delayRisk = canteenChargesPerDay > 0 && canteenReserveCharges <= 0;
        var waterSecure = routeWaterSecure || availableCanteenCharges >= requiredCanteenCharges;
        var warnings = new List<string>(routeProfile.Warnings);

        if (!mountedTravelAvailable)
        {
            warnings.Add("Mounted travel is unavailable, so the route will continue on foot.");
        }

        if (livingHorse && !grazingAvailable)
        {
            warnings.Add("Poor grazing means the horse will rely on feed on this trail.");
        }

        if (availableFood < requiredFood)
        {
            warnings.Add("You do not have enough food to cover the full trail.");
        }

        if (availableHorseFeed < requiredHorseFeed)
        {
            warnings.Add("You do not have enough horse feed to keep the horse fed on this trail.");
        }

        if (!routeWaterSecure && livingHorse)
        {
            warnings.Add("This dry route needs two canteen charges per day to water both horse and rider.");
        }
        else if (!routeWaterSecure)
        {
            warnings.Add("This dry route needs one canteen charge per day for the rider.");
        }

        if (availableCanteenCharges < requiredCanteenCharges)
        {
            warnings.Add($"You are short by {Math.Abs(canteenReserveCharges)} canteen charge(s) for the base trail.");
        }
        else if (!routeWaterSecure && canteenReserveCharges == 0)
        {
            warnings.Add("Your canteen exactly covers the base trail, so any delay will need more water.");
        }
        else if (!routeWaterSecure)
        {
            warnings.Add($"Your canteen has {canteenReserveCharges} spare charge(s) and can absorb {delayMarginDays} delay day(s).");
        }

        warnings = new List<string>(TravelWarningFilter.Filter(warnings, mountedTravelAvailable));

        var preview = new TravelPreview(
            currentTownId,
            destinationTownId,
            originTown!.Name,
            destinationTown!.Name,
            routeProfile,
            travelMode,
            mountedTravelAvailable,
            waterSecure,
            rideDayDistance,
            rideDayDistance,
            baselineRideDays,
            expectedDays,
            expectedDays,
            canteenChargesPerDay,
            requiredCanteenCharges,
            availableCanteenCharges,
            canteenReserveCharges,
            delayMarginDays,
            delayRisk,
            requiredFood,
            availableFood,
            requiredHorseFeed,
            availableHorseFeed,
            horseState,
            warnings);

        return new TravelPreviewResult(
            true,
            $"Previewed {travelMode.ToString().ToLowerInvariant()} travel from {originTown.Name} to {destinationTown.Name}: {baselineRideDays} day ride estimate, {expectedDays} day(s) on the trail; {DescribeCanteenCoverage(routeProfile.WaterFeature, canteenChargesPerDay, canteenReserveCharges, delayMarginDays)}",
            preview);
    }

    private static string DescribeCanteenCoverage(
        WaterFeature waterFeature,
        int canteenChargesPerDay,
        int canteenReserveCharges,
        int delayMarginDays)
    {
        if (JourneyUpkeepRules.HasRouteWater(waterFeature))
        {
            return "Route water is secure, so no canteen reserve is required";
        }

        if (canteenChargesPerDay <= 0)
        {
            return "No canteen water is required on this trail";
        }

        if (canteenReserveCharges == 0)
        {
            return "The canteen exactly covers the base trail and has no reserve for delays";
        }

        if (canteenReserveCharges > 0)
        {
            return $"The canteen has {canteenReserveCharges} spare charge(s) and can absorb {delayMarginDays} delay day(s)";
        }

        return $"The canteen is short by {Math.Abs(canteenReserveCharges)} charge(s) for the base trail";
    }

    private static TravelRouteProfile BuildRouteProfile(Trail trail, TravelRulesProfile travelRulesProfile)
    {
        var warnings = new List<string>();

        if (trail.Risk >= TrailRisk.Moderate)
        {
            warnings.Add("Rough trail conditions may stress the horse.");
        }

        if (trail.WaterFeature == WaterFeature.None)
        {
            warnings.Add("Water is sparse along this trail.");
        }

        return new TravelRouteProfile(
            trail.Id.Value,
            trail.Risk,
            trail.Terrain,
            trail.WaterFeature,
            trail.RideDayDistance,
            travelRulesProfile.MountedRideDayProgress,
            travelRulesProfile.FootRideDayProgress,
            warnings);
    }
}
