namespace WildBunch.Domain.Inventory;

public sealed class Inventory
{
    private readonly List<InventoryItem> _items;

    public Inventory(IEnumerable<InventoryItem>? items = null)
    {
        _items = items?.Select(item => new InventoryItem(item.Kind, item.Quantity, item.HorseCondition)).ToList() ?? [];
    }

    public IReadOnlyList<InventoryItem> Items => _items;

    public static Inventory Empty() => new();

    public int GetQuantity(ItemKind kind)
        => _items.Where(item => item.Kind == kind).Sum(item => item.Quantity);

    public bool HasItem(ItemKind kind)
        => GetQuantity(kind) > 0;

    public HorseCondition? GetHorseCondition()
    {
        var horse = _items.FirstOrDefault(item => item.Kind == ItemKind.Horse);
        return horse?.HorseCondition;
    }
}
