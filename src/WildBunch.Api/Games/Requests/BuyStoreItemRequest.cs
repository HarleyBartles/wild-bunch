using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;

namespace WildBunch.Api.Games;

public sealed record BuyStoreItemRequest(
    StoreVendorType? VendorType,
    ItemKind? ItemKind,
    int Quantity = 1);
