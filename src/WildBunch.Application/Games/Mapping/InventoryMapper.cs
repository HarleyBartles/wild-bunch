using WildBunch.Application.Games.Models;
using DomainTravelRulesProfile = WildBunch.Domain.Travel.TravelRulesProfile;
using DomainCanteenState = WildBunch.Domain.Inventory.CanteenState;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainInventoryCapabilities = WildBunch.Domain.Inventory.InventoryCapabilities;
using DomainInventoryCapabilityResolver = WildBunch.Domain.Inventory.InventoryCapabilityResolver;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;
using DomainPlayer = WildBunch.Domain.Game.Player;

namespace WildBunch.Application.Games.Mapping;

public static class InventoryMapper
{
    private static readonly DomainInventoryCapabilityResolver CapabilityResolver = new();

    public static InventoryDto ToDto(DomainPlayer player, DomainTravelRulesProfile? travelRulesProfile = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        travelRulesProfile ??= DomainTravelRulesProfile.Default;

        return new InventoryDto(
            new WalletDto(player.Wallet.Cash),
            player.Inventory.Items.Select(item => ToDto(item, travelRulesProfile)).ToArray(),
            ToDto(player.Inventory.GetHorseState(), travelRulesProfile),
            ToDto(player.Inventory.GetCanteenState()),
            ToDto(CapabilityResolver.Resolve(player.Inventory, travelRulesProfile)));
    }

    private static InventoryItemDto ToDto(DomainInventoryItem item, DomainTravelRulesProfile travelRulesProfile)
        => new(item.Kind, item.Quantity, ToDto(item.HorseState, travelRulesProfile), ToDto(item.CanteenState));

    private static HorseTravelStateDto? ToDto(DomainHorseTravelState? horseState, DomainTravelRulesProfile travelRulesProfile)
        => horseState is null
            ? null
            : new HorseTravelStateDto(
                horseState.Hunger,
                horseState.Thirst,
                horseState.Exhaustion,
                horseState.IsLameFor(travelRulesProfile),
                horseState.IsDeadFor(travelRulesProfile),
                horseState.CanProvideMountedTravelFor(travelRulesProfile));

    private static CanteenStateDto? ToDto(DomainCanteenState? canteenState)
        => canteenState is null
            ? null
            : new CanteenStateDto(
                canteenState.Charges,
                canteenState.Capacity,
                canteenState.HasWater);

    private static InventoryCapabilitiesDto ToDto(DomainInventoryCapabilities capabilities)
        => new(
            capabilities.MountedTravelAvailable,
            capabilities.HorseUpkeepRequired,
            capabilities.NormalRouteWaterSecure,
            capabilities.TrailUtility,
            capabilities.CloseThreatAvailable,
            capabilities.FirearmThreatAvailable,
            capabilities.GunfightCapable,
            capabilities.RevolverUsable,
            capabilities.RifleUsable);
}
