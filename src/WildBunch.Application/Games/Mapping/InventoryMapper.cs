using WildBunch.Application.Games.Models;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainInventoryCapabilities = WildBunch.Domain.Inventory.InventoryCapabilities;
using DomainInventoryCapabilityResolver = WildBunch.Domain.Inventory.InventoryCapabilityResolver;
using DomainPlayer = WildBunch.Domain.Game.Player;

namespace WildBunch.Application.Games.Mapping;

public static class InventoryMapper
{
    private static readonly DomainInventoryCapabilityResolver CapabilityResolver = new();

    public static InventoryDto ToDto(DomainPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return new InventoryDto(
            new WalletDto(player.Wallet.Cash),
            player.Inventory.Items.Select(ToDto).ToArray(),
            player.Inventory.GetHorseCondition(),
            ToDto(CapabilityResolver.Resolve(player.Inventory)));
    }

    private static InventoryItemDto ToDto(DomainInventoryItem item)
        => new(item.Kind, item.Quantity, item.HorseCondition);

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
