using WildBunch.Application.Games.Models;
using DomainTownStoreCatalog = WildBunch.Domain.Economy.TownStoreCatalog;
using DomainStoreOffer = WildBunch.Domain.Economy.StoreOffer;

namespace WildBunch.Application.Games.Mapping;

public static class StoreCatalogMapper
{
    public static TownStoreOffersDto ToDto(DomainTownStoreCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new TownStoreOffersDto(
            catalog.TownId.Value,
            catalog.TownName,
            catalog.Available,
            catalog.SourceNote,
            catalog.Offers.Select(ToDto).ToArray());
    }

    private static StoreOfferDto ToDto(DomainStoreOffer offer)
        => new(
            offer.ItemKind,
            offer.DisplayName,
            offer.Price,
            offer.VendorType,
            offer.Availability,
            offer.SourceNote);
}
