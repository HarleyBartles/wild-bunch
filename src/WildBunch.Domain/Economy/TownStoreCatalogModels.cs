using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Economy;

public enum StoreVendorType
{
    GeneralStore = 0,
    Stable = 1,
    Gunsmith = 2
}

public enum StoreOfferAvailability
{
    Available = 0,
    Unavailable = 1
}

public sealed record StoreOffer(
    ItemKind ItemKind,
    string DisplayName,
    decimal Price,
    StoreVendorType VendorType,
    StoreOfferAvailability Availability,
    string SourceNote);

public sealed record TownStoreCatalog(
    TownId TownId,
    string TownName,
    bool Available,
    string SourceNote,
    IReadOnlyList<StoreOffer> Offers);

public sealed class TownStoreCatalogResolver
{
    private static readonly HashSet<string> GunsmithTownIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "redmesa",
        "emberfall"
    };

    public TownStoreCatalog Resolve(Town town)
    {
        ArgumentNullException.ThrowIfNull(town);

        var offers = new List<StoreOffer>();

        if ((town.Services & TownServices.Supplies) != 0)
        {
            offers.AddRange(CreateGeneralStoreOffers());
        }

        if ((town.Services & TownServices.Lodging) != 0)
        {
            offers.AddRange(CreateStableOffers());
        }

        if (GunsmithTownIds.Contains(town.Id.Value))
        {
            offers.AddRange(CreateGunsmithOffers());
        }

        offers = offers
            .OrderBy(offer => offer.VendorType)
            .ThenBy(offer => offer.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var available = offers.Count > 0;
        var sourceNote = available
            ? $"Offers sourced from {FormatTownSource(town, offers)}."
            : "No store services are available in this town.";

        return new TownStoreCatalog(
            town.Id,
            town.Name,
            available,
            sourceNote,
            offers);
    }

    private static IReadOnlyList<StoreOffer> CreateGeneralStoreOffers()
        => new[]
        {
            new StoreOffer(ItemKind.Food, "Food", 2m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
            new StoreOffer(ItemKind.HorseFeed, "Horse feed", 1m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
            new StoreOffer(ItemKind.Canteen, "Canteen", 5m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
            new StoreOffer(ItemKind.Knife, "Knife", 8m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf")
        };

    private static IReadOnlyList<StoreOffer> CreateStableOffers()
        => new[]
        {
            new StoreOffer(ItemKind.Horse, "Horse", 60m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room"),
            new StoreOffer(ItemKind.Saddle, "Saddle", 20m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room"),
            new StoreOffer(ItemKind.HorseFeed, "Horse feed", 1.25m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room")
        };

    private static IReadOnlyList<StoreOffer> CreateGunsmithOffers()
        => new[]
        {
            new StoreOffer(ItemKind.Revolver, "Revolver", 32m, StoreVendorType.Gunsmith, StoreOfferAvailability.Available, "Gunsmith counter"),
            new StoreOffer(ItemKind.RevolverAmmo, "Revolver ammo", 4m, StoreVendorType.Gunsmith, StoreOfferAvailability.Available, "Gunsmith counter"),
            new StoreOffer(ItemKind.RifleAmmo, "Rifle ammo", 6m, StoreVendorType.Gunsmith, StoreOfferAvailability.Available, "Gunsmith counter")
        };

    private static string FormatTownSource(Town town, IReadOnlyList<StoreOffer> offers)
    {
        var vendorLabels = offers
            .Select(offer => offer.VendorType)
            .Distinct()
            .Select(FormatVendorType)
            .ToArray();

        return $"{town.Name} ({string.Join(", ", vendorLabels)})";
    }

    private static string FormatVendorType(StoreVendorType vendorType)
        => vendorType switch
        {
            StoreVendorType.GeneralStore => "general store",
            StoreVendorType.Stable => "stable",
            StoreVendorType.Gunsmith => "gunsmith",
            _ => vendorType.ToString().ToLowerInvariant()
        };
}
