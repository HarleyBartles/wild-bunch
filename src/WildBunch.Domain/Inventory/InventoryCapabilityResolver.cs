namespace WildBunch.Domain.Inventory;

public sealed class InventoryCapabilityResolver
{
    public InventoryCapabilities Resolve(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var horseState = inventory.GetHorseState();
        var hasLivingHorse = horseState is not null && !horseState.IsDead;
        var hasSaddle = inventory.HasItem(ItemKind.Saddle);
        var hasCanteenWater = inventory.GetCanteenState()?.HasWater == true;
        var hasKnife = inventory.HasItem(ItemKind.Knife);
        var revolverUsable = inventory.HasItem(ItemKind.Revolver) && inventory.GetQuantity(ItemKind.RevolverAmmo) > 0;
        var rifleUsable = inventory.HasItem(ItemKind.Rifle) && inventory.GetQuantity(ItemKind.RifleAmmo) > 0;

        return new InventoryCapabilities(
            MountedTravelAvailable: hasLivingHorse && !horseState!.IsLame && hasSaddle,
            HorseUpkeepRequired: hasLivingHorse,
            NormalRouteWaterSecure: hasCanteenWater,
            TrailUtility: hasKnife,
            CloseThreatAvailable: hasKnife,
            FirearmThreatAvailable: revolverUsable || rifleUsable,
            GunfightCapable: revolverUsable || rifleUsable,
            RevolverUsable: revolverUsable,
            RifleUsable: rifleUsable);
    }
}
