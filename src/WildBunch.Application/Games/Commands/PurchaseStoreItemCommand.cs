using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;

namespace WildBunch.Application.Games.Commands;

public sealed record PurchaseStoreItemCommand(
    Guid GameSessionId,
    string TownId,
    StoreVendorType? VendorType,
    ItemKind? ItemKind,
    int Quantity = 1);
