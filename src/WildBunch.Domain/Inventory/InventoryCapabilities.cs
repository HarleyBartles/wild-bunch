namespace WildBunch.Domain.Inventory;

public sealed record InventoryCapabilities(
    bool MountedTravelAvailable,
    bool HorseUpkeepRequired,
    bool NormalRouteWaterSecure,
    bool TrailUtility,
    bool CloseThreatAvailable,
    bool FirearmThreatAvailable,
    bool GunfightCapable,
    bool RevolverUsable,
    bool RifleUsable);
