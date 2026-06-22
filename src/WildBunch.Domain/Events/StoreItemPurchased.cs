using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player purchased a quantity of a store item at the current town.
/// </summary>
public sealed record StoreItemPurchased : IDomainEvent
{
    public required TownId TownId { get; init; }
    public required ItemKind ItemKind { get; init; }
    public required string DisplayName { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal TotalPrice { get; init; }
    public required decimal WalletAfter { get; init; }
}
