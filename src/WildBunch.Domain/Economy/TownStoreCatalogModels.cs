using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Economy;

public sealed record StoreOffer(
    ItemKind ItemKind,
    string DisplayName,
    decimal Price);

public sealed record TownStoreCatalog(
    TownId TownId,
    string TownName,
    IReadOnlyList<StoreOffer> Offers);

public sealed class TownStoreCatalogResolver
{
    public TownStoreCatalog Resolve(Town town)
    {
        ArgumentNullException.ThrowIfNull(town);

        var offers = town.Prosperity switch
        {
            TownProsperity.Boomtown => new[]
            {
                new StoreOffer(ItemKind.Food, "Food", 2m),
                new StoreOffer(ItemKind.HorseFeed, "Horse feed", 1m),
                new StoreOffer(ItemKind.Canteen, "Canteen", 5m),
                new StoreOffer(ItemKind.Knife, "Knife", 8m),
                new StoreOffer(ItemKind.Horse, "Horse", 60m),
                new StoreOffer(ItemKind.Saddle, "Saddle", 20m),
                new StoreOffer(ItemKind.Revolver, "Revolver", 32m),
                new StoreOffer(ItemKind.RevolverAmmo, "Revolver ammo", 4m),
                new StoreOffer(ItemKind.RifleAmmo, "Rifle ammo", 6m)
            },
            TownProsperity.Prosperous => new[]
            {
                new StoreOffer(ItemKind.Food, "Food", 2m),
                new StoreOffer(ItemKind.HorseFeed, "Horse feed", 1m),
                new StoreOffer(ItemKind.Canteen, "Canteen", 5m),
                new StoreOffer(ItemKind.Knife, "Knife", 8m),
                new StoreOffer(ItemKind.Horse, "Horse", 60m),
                new StoreOffer(ItemKind.Saddle, "Saddle", 20m),
                new StoreOffer(ItemKind.Revolver, "Revolver", 35m),
                new StoreOffer(ItemKind.RevolverAmmo, "Revolver ammo", 4m),
                new StoreOffer(ItemKind.RifleAmmo, "Rifle ammo", 6m)
            },
            TownProsperity.Poor => new[]
            {
                new StoreOffer(ItemKind.Food, "Food", 2.5m),
                new StoreOffer(ItemKind.HorseFeed, "Horse feed", 1.25m),
                new StoreOffer(ItemKind.Canteen, "Canteen", 6m),
                new StoreOffer(ItemKind.Horse, "Horse", 75m),
                new StoreOffer(ItemKind.Saddle, "Saddle", 25m)
            },
            TownProsperity.Destitute => new[]
            {
                new StoreOffer(ItemKind.Food, "Food", 3m),
                new StoreOffer(ItemKind.HorseFeed, "Horse feed", 1.5m)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(town.Prosperity), town.Prosperity, "Unsupported prosperity tier.")
        };

        var orderedOffers = offers
            .OrderBy(offer => offer.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new TownStoreCatalog(town.Id, town.Name, orderedOffers);
    }
}
