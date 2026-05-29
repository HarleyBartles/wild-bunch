namespace WildBunch.Domain.Inventory;

public sealed class InventoryCapabilityResolver
{
    public InventoryCapabilities Resolve(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var horseCondition = inventory.GetHorseCondition();
        var hasLivingHorse = horseCondition is HorseCondition.Healthy or HorseCondition.Lame;
        var hasHealthyHorse = horseCondition is HorseCondition.Healthy;
        var hasSaddle = inventory.HasItem(ItemKind.Saddle);
        var hasCanteen = inventory.HasItem(ItemKind.Canteen);
        var hasKnife = inventory.HasItem(ItemKind.Knife);
        var revolverUsable = inventory.HasItem(ItemKind.Revolver) && inventory.GetQuantity(ItemKind.RevolverAmmo) > 0;
        var rifleUsable = inventory.HasItem(ItemKind.Rifle) && inventory.GetQuantity(ItemKind.RifleAmmo) > 0;

        return new InventoryCapabilities(
            MountedTravelAvailable: hasHealthyHorse && hasSaddle,
            HorseUpkeepRequired: hasLivingHorse,
            NormalRouteWaterSecure: hasCanteen,
            TrailUtility: hasKnife,
            CloseThreatAvailable: hasKnife,
            FirearmThreatAvailable: revolverUsable || rifleUsable,
            GunfightCapable: revolverUsable || rifleUsable,
            RevolverUsable: revolverUsable,
            RifleUsable: rifleUsable);
    }
}
