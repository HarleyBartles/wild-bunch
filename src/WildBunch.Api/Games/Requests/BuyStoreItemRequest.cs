using WildBunch.Domain.Inventory;

namespace WildBunch.Api.Games;

public sealed record BuyStoreItemRequest(
    ItemKind? ItemKind,
    int Quantity = 1);
