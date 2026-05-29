using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;

namespace WildBunch.Application.Games.Models;

public sealed record TownStoreOffersDto(
    string TownId,
    string TownName,
    bool Available,
    string SourceNote,
    IReadOnlyList<StoreOfferDto> Offers);

public sealed record StoreOfferDto(
    ItemKind ItemKind,
    string DisplayName,
    decimal Price,
    StoreVendorType VendorType,
    StoreOfferAvailability Availability,
    string SourceNote,
    HorseCondition? HorseCondition);
