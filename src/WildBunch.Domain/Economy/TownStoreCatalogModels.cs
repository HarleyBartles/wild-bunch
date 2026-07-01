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
    public TownStoreCatalog Resolve(Town town)
    {
        ArgumentNullException.ThrowIfNull(town);

        var offers = new List<StoreOffer>();

        // Every town has a general store and stable. Stock and prices vary
        // by prosperity tier. Gunsmith is only available in Boomtown and
        // Prosperous towns.
        offers.AddRange(CreateGeneralStoreOffers(town.Prosperity));
        offers.AddRange(CreateStableOffers(town.Prosperity));

        if (town.Prosperity is TownProsperity.Boomtown or TownProsperity.Prosperous)
        {
            offers.AddRange(CreateGunsmithOffers(town.Prosperity));
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

    private static IReadOnlyList<StoreOffer> CreateGeneralStoreOffers(TownProsperity prosperity)
        => prosperity switch
        {
            TownProsperity.Boomtown => new[]
            {
                new StoreOffer(ItemKind.Food, "Food", 2m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
                new StoreOffer(ItemKind.HorseFeed, "Horse feed (General store)", 1m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
                new StoreOffer(ItemKind.Canteen, "Canteen", 5m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
                new StoreOffer(ItemKind.Knife, "Knife", 8m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf")
            },
            TownProsperity.Prosperous => new[]
            {
                new StoreOffer(ItemKind.Food, "Food", 2m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
                new StoreOffer(ItemKind.HorseFeed, "Horse feed (General store)", 1m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
                new StoreOffer(ItemKind.Canteen, "Canteen", 5m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
                new StoreOffer(ItemKind.Knife, "Knife", 8m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf")
            },
            TownProsperity.Poor => new[]
            {
                new StoreOffer(ItemKind.Food, "Food", 2.5m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
                new StoreOffer(ItemKind.HorseFeed, "Horse feed (General store)", 1.25m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
                new StoreOffer(ItemKind.Canteen, "Canteen", 6m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf")
            },
            TownProsperity.Destitute => new[]
            {
                new StoreOffer(ItemKind.Food, "Food", 3m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
                new StoreOffer(ItemKind.HorseFeed, "Horse feed (General store)", 1.5m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(prosperity), prosperity, "Unsupported prosperity tier.")
        };

    private static IReadOnlyList<StoreOffer> CreateStableOffers(TownProsperity prosperity)
        => prosperity switch
        {
            TownProsperity.Boomtown => new[]
            {
                new StoreOffer(ItemKind.Horse, "Horse", 60m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room"),
                new StoreOffer(ItemKind.Saddle, "Saddle", 20m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room"),
                new StoreOffer(ItemKind.HorseFeed, "Horse feed (Stable)", 1.25m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room")
            },
            TownProsperity.Prosperous => new[]
            {
                new StoreOffer(ItemKind.Horse, "Horse", 60m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room"),
                new StoreOffer(ItemKind.Saddle, "Saddle", 20m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room"),
                new StoreOffer(ItemKind.HorseFeed, "Horse feed (Stable)", 1.25m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room")
            },
            TownProsperity.Poor => new[]
            {
                new StoreOffer(ItemKind.Horse, "Horse", 75m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room"),
                new StoreOffer(ItemKind.Saddle, "Saddle", 25m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room")
            },
            TownProsperity.Destitute => Array.Empty<StoreOffer>(),
            _ => throw new ArgumentOutOfRangeException(nameof(prosperity), prosperity, "Unsupported prosperity tier.")
        };

    private static IReadOnlyList<StoreOffer> CreateGunsmithOffers(TownProsperity prosperity)
        => prosperity switch
        {
            TownProsperity.Boomtown => new[]
            {
                new StoreOffer(ItemKind.Revolver, "Revolver", 32m, StoreVendorType.Gunsmith, StoreOfferAvailability.Available, "Gunsmith counter"),
                new StoreOffer(ItemKind.RevolverAmmo, "Revolver ammo", 4m, StoreVendorType.Gunsmith, StoreOfferAvailability.Available, "Gunsmith counter"),
                new StoreOffer(ItemKind.RifleAmmo, "Rifle ammo", 6m, StoreVendorType.Gunsmith, StoreOfferAvailability.Available, "Gunsmith counter")
            },
            TownProsperity.Prosperous => new[]
            {
                new StoreOffer(ItemKind.Revolver, "Revolver", 35m, StoreVendorType.Gunsmith, StoreOfferAvailability.Available, "Gunsmith counter"),
                new StoreOffer(ItemKind.RevolverAmmo, "Revolver ammo", 4m, StoreVendorType.Gunsmith, StoreOfferAvailability.Available, "Gunsmith counter"),
                new StoreOffer(ItemKind.RifleAmmo, "Rifle ammo", 6m, StoreVendorType.Gunsmith, StoreOfferAvailability.Available, "Gunsmith counter")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(prosperity), prosperity, "Gunsmith only available in Boomtown and Prosperous towns.")
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
