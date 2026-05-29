namespace WildBunch.Domain.Inventory;

public sealed class Inventory
{
    private static readonly HashSet<ItemKind> StackableKinds =
    [
        ItemKind.Food,
        ItemKind.HorseFeed,
        ItemKind.RevolverAmmo,
        ItemKind.RifleAmmo
    ];

    private readonly List<InventoryItem> _items;

    public Inventory(IEnumerable<InventoryItem>? items = null)
    {
        _items = [];

        if (items is null)
        {
            return;
        }

        foreach (var item in items)
        {
            AddItem(item);
        }
    }

    public IReadOnlyList<InventoryItem> Items => _items.Select(item => new InventoryItem(item.Kind, item.Quantity, item.HorseCondition)).ToArray();

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

    public void AddItem(InventoryItem item)
        => AddItem(item.Kind, item.Quantity, item.HorseCondition);

    public void AddItem(ItemKind kind, int quantity, HorseCondition? horseCondition = null)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        }

        if (quantity == 0)
        {
            return;
        }

        if (kind == ItemKind.Horse)
        {
            AddHorse(quantity, horseCondition);
            return;
        }

        if (horseCondition is not null)
        {
            throw new ArgumentException("Only horse items can carry a horse condition.", nameof(horseCondition));
        }

        if (StackableKinds.Contains(kind))
        {
            AddStackable(kind, quantity);
            return;
        }

        AddNonStackable(kind, quantity);
    }

    public void RemoveQuantity(ItemKind kind, int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        }

        if (quantity == 0)
        {
            return;
        }

        var index = _items.FindIndex(item => item.Kind == kind);
        if (index < 0)
        {
            throw new InvalidOperationException($"Item {kind} is not present.");
        }

        var current = _items[index];
        if (quantity > current.Quantity)
        {
            throw new InvalidOperationException($"Not enough {kind} to remove.");
        }

        var remaining = current.Quantity - quantity;
        if (remaining == 0)
        {
            _items.RemoveAt(index);
            return;
        }

        _items[index] = new InventoryItem(current.Kind, remaining, current.HorseCondition);
    }

    private void AddStackable(ItemKind kind, int quantity)
    {
        var index = _items.FindIndex(item => item.Kind == kind);
        if (index < 0)
        {
            _items.Add(new InventoryItem(kind, quantity));
            return;
        }

        var current = _items[index];
        _items[index] = new InventoryItem(current.Kind, current.Quantity + quantity, current.HorseCondition);
    }

    private void AddNonStackable(ItemKind kind, int quantity)
    {
        if (quantity != 1)
        {
            throw new InvalidOperationException($"{kind} does not stack.");
        }

        if (_items.Any(item => item.Kind == kind))
        {
            throw new InvalidOperationException($"{kind} already exists in inventory.");
        }

        _items.Add(new InventoryItem(kind, 1));
    }

    private void AddHorse(int quantity, HorseCondition? horseCondition)
    {
        if (quantity != 1)
        {
            throw new InvalidOperationException("Horse items must have a quantity of 1.");
        }

        if (horseCondition is null)
        {
            throw new ArgumentNullException(nameof(horseCondition), "Horse items require a condition.");
        }

        if (_items.Any(item => item.Kind == ItemKind.Horse))
        {
            throw new InvalidOperationException("Horse already exists in inventory.");
        }

        _items.Add(new InventoryItem(ItemKind.Horse, 1, horseCondition));
    }
}
