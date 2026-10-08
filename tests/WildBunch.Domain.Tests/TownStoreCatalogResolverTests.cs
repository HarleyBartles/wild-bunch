using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class TownStoreCatalogResolverTests
{
    [Fact]
    public void CatalogOffersOneProsperityPricedEntryForEachCurrentItem()
    {
        var resolver = new TownStoreCatalogResolver();
        var towns = new[]
        {
            (
                TownProsperity.Boomtown,
                new[]
                {
                    (ItemKind.Food, "Food", 2m),
                    (ItemKind.HorseFeed, "Horse feed", 1m),
                    (ItemKind.Canteen, "Canteen", 5m),
                    (ItemKind.Knife, "Knife", 8m),
                    (ItemKind.Horse, "Horse", 60m),
                    (ItemKind.Saddle, "Saddle", 20m),
                    (ItemKind.Revolver, "Revolver", 32m),
                    (ItemKind.RevolverAmmo, "Revolver ammo", 4m),
                    (ItemKind.RifleAmmo, "Rifle ammo", 6m)
                }),
            (
                TownProsperity.Prosperous,
                new[]
                {
                    (ItemKind.Food, "Food", 2m),
                    (ItemKind.HorseFeed, "Horse feed", 1m),
                    (ItemKind.Canteen, "Canteen", 5m),
                    (ItemKind.Knife, "Knife", 8m),
                    (ItemKind.Horse, "Horse", 60m),
                    (ItemKind.Saddle, "Saddle", 20m),
                    (ItemKind.Revolver, "Revolver", 35m),
                    (ItemKind.RevolverAmmo, "Revolver ammo", 4m),
                    (ItemKind.RifleAmmo, "Rifle ammo", 6m)
                }),
            (
                TownProsperity.Poor,
                new[]
                {
                    (ItemKind.Food, "Food", 2.5m),
                    (ItemKind.HorseFeed, "Horse feed", 1.25m),
                    (ItemKind.Canteen, "Canteen", 6m),
                    (ItemKind.Horse, "Horse", 75m),
                    (ItemKind.Saddle, "Saddle", 25m)
                }),
            (
                TownProsperity.Destitute,
                new[]
                {
                    (ItemKind.Food, "Food", 3m),
                    (ItemKind.HorseFeed, "Horse feed", 1.5m)
                })
        };

        foreach (var (prosperity, expectedOffers) in towns)
        {
            var town = new Town(
                new TownId(prosperity.ToString().ToLowerInvariant()),
                prosperity.ToString(),
                TownServices.None,
                prosperity);

            var actualOffers = resolver.Resolve(town).Offers
                .Select(offer => (offer.ItemKind, offer.DisplayName, offer.Price))
                .ToArray();

            Assert.Equal(expectedOffers.Length, actualOffers.Length);
            Assert.Equal(
                expectedOffers.OrderBy(offer => offer.Item2, StringComparer.OrdinalIgnoreCase),
                actualOffers);
            Assert.Equal(actualOffers.Length, actualOffers.Select(offer => offer.Item1).Distinct().Count());
        }
    }

}
