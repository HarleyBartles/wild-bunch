using WildBunch.Domain.Inventory;

namespace WildBunch.Application.Games.Commands;

public sealed record PurchaseStoreItemCommand(
    Guid GameSessionId,
    string TownId,
    ItemKind? ItemKind,
    int Quantity = 1);
