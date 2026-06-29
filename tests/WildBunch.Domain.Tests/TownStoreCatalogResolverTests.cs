using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class TownStoreCatalogResolverTests
{
    [Fact]
    public void ProsperousTownReturnsGeneralStoreAndStableOffers()
    {
        var resolver = new TownStoreCatalogResolver();
        var town = new Town(new TownId("pinecross"), "Pinecross", TownServices.None, TownProsperity.Prosperous);

        var catalog = resolver.Resolve(town);

        Assert.True(catalog.Available);
        Assert.Equal("Pinecross", catalog.TownName);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.GeneralStore && offer.ItemKind == ItemKind.Food);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.Stable && offer.ItemKind == ItemKind.Horse);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.Gunsmith && offer.ItemKind == ItemKind.Revolver);
    }

    [Fact]
    public void BoomtownReturnsFullStockIncludingGunsmith()
    {
        var resolver = new TownStoreCatalogResolver();
        var town = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Telegraph, TownProsperity.Boomtown);

        var catalog = resolver.Resolve(town);

        Assert.True(catalog.Available);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.Gunsmith && offer.ItemKind == ItemKind.Revolver);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.Gunsmith && offer.ItemKind == ItemKind.RevolverAmmo);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.Stable && offer.ItemKind == ItemKind.Horse);
    }

    [Fact]
    public void PoorTownHasNoGunsmithButHasGeneralStore()
    {
        var resolver = new TownStoreCatalogResolver();
        var town = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None, TownProsperity.Poor);

        var catalog = resolver.Resolve(town);

        Assert.True(catalog.Available);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.GeneralStore && offer.ItemKind == ItemKind.Food);
        Assert.DoesNotContain(catalog.Offers, offer => offer.VendorType == StoreVendorType.Gunsmith);
    }

    [Fact]
    public void DestituteTownHasMinimalStock()
    {
        var resolver = new TownStoreCatalogResolver();
        var town = new Town(new TownId("hardpan"), "Hardpan", TownServices.None, TownProsperity.Destitute);

        var catalog = resolver.Resolve(town);

        Assert.True(catalog.Available);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.GeneralStore && offer.ItemKind == ItemKind.Food);
        // Destitute towns have no stable, no gunsmith.
        Assert.DoesNotContain(catalog.Offers, offer => offer.VendorType == StoreVendorType.Stable);
        Assert.DoesNotContain(catalog.Offers, offer => offer.VendorType == StoreVendorType.Gunsmith);
    }
}
