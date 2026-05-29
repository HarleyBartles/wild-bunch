using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class TownStoreCatalogResolverTests
{
    [Fact]
    public void PinecrossReturnsGeneralStoreAndStableOffers()
    {
        var resolver = new TownStoreCatalogResolver();
        var town = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);

        var catalog = resolver.Resolve(town);

        Assert.True(catalog.Available);
        Assert.Equal("Pinecross", catalog.TownName);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.GeneralStore && offer.ItemKind == ItemKind.Food);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.Stable && offer.ItemKind == ItemKind.Horse);
        Assert.DoesNotContain(catalog.Offers, offer => offer.VendorType == StoreVendorType.Gunsmith);
    }

    [Fact]
    public void RedMesaReturnsGunsmithOffers()
    {
        var resolver = new TownStoreCatalogResolver();
        var town = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);

        var catalog = resolver.Resolve(town);

        Assert.True(catalog.Available);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.Gunsmith && offer.ItemKind == ItemKind.Revolver);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == StoreVendorType.Gunsmith && offer.ItemKind == ItemKind.RevolverAmmo);
        Assert.DoesNotContain(catalog.Offers, offer => offer.VendorType == StoreVendorType.Stable);
    }

    [Fact]
    public void DryForkReturnsEmptyCatalog()
    {
        var resolver = new TownStoreCatalogResolver();
        var town = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);

        var catalog = resolver.Resolve(town);

        Assert.False(catalog.Available);
        Assert.Empty(catalog.Offers);
        Assert.Contains("No store services", catalog.SourceNote);
    }
}
