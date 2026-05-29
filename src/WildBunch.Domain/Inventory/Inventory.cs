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

    public IReadOnlyList<InventoryItem> Items => _items.Select(item => new InventoryItem(item.Kind, item.Quantity, item.HorseState, item.CanteenState)).ToArray();

    public static Inventory Empty() => new();

    public int GetQuantity(ItemKind kind)
        => _items.Where(item => item.Kind == kind).Sum(item => item.Quantity);

    public bool HasItem(ItemKind kind)
        => GetQuantity(kind) > 0;

    public HorseTravelState? GetHorseState()
    {
        var horse = _items.FirstOrDefault(item => item.Kind == ItemKind.Horse);
        return horse?.HorseState;
    }

    public HorseTravelState? GetHorseStateOrNull()
        => GetHorseState();

    public void SetHorseState(HorseTravelState horseState)
    {
        var index = _items.FindIndex(item => item.Kind == ItemKind.Horse);
        if (index < 0)
        {
            throw new InvalidOperationException("Horse is not present in inventory.");
        }

        var horse = _items[index];
        _items[index] = new InventoryItem(ItemKind.Horse, horse.Quantity, horseState);
    }

    public void AddItem(InventoryItem item)
        => AddItem(item.Kind, item.Quantity, item.HorseState, item.CanteenState);

    public void AddItem(ItemKind kind, int quantity, HorseTravelState? horseState = null, CanteenState? canteenState = null)
    {
        ValidateAddItem(kind, quantity, horseState, canteenState);
        ApplyAddItem(kind, quantity, horseState, canteenState);
    }

    public bool CanAddItem(ItemKind kind, int quantity, HorseTravelState? horseState = null, CanteenState? canteenState = null)
    {
        try
        {
            ValidateAddItem(kind, quantity, horseState, canteenState);
            return true;
        }
        catch
        {
            return false;
        }
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

        _items[index] = new InventoryItem(current.Kind, remaining, current.HorseState, current.CanteenState);
    }

    public CanteenState? GetCanteenState()
    {
        var canteen = _items.FirstOrDefault(item => item.Kind == ItemKind.Canteen);
        return canteen?.CanteenState;
    }

    public void SetCanteenState(CanteenState canteenState)
    {
        var index = _items.FindIndex(item => item.Kind == ItemKind.Canteen);
        if (index < 0)
        {
            throw new InvalidOperationException("Canteen is not present in inventory.");
        }

        var canteen = _items[index];
        _items[index] = new InventoryItem(ItemKind.Canteen, canteen.Quantity, canteenState: canteenState);
    }

    public HorseTravelState AdvanceHorseState(bool horseFed)
    {
        var horseState = GetHorseState();
        if (horseState is null)
        {
            throw new InvalidOperationException("Horse is not present in inventory.");
        }

        var updatedHorseState = horseState.AdvanceTravelDay(horseFed);
        SetHorseState(updatedHorseState);
        return updatedHorseState;
    }

    private static void ValidateAddItem(ItemKind kind, int quantity, HorseTravelState? horseState, CanteenState? canteenState)
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
            if (quantity != 1)
            {
                throw new InvalidOperationException("Horse items must have a quantity of 1.");
            }

            if (canteenState is not null)
            {
                throw new ArgumentException("Horse items cannot carry canteen state.", nameof(canteenState));
            }

            return;
        }

        if (kind == ItemKind.Canteen)
        {
            if (quantity != 1)
            {
                throw new InvalidOperationException("Canteen items must have a quantity of 1.");
            }

            if (horseState is not null)
            {
                throw new ArgumentException("Canteen items cannot carry horse state.", nameof(horseState));
            }

            return;
        }

        if (horseState is not null || canteenState is not null)
        {
            throw new ArgumentException("Only horse or canteen items can carry travel state.");
        }

        if (!StackableKinds.Contains(kind) && quantity != 1)
        {
            throw new InvalidOperationException($"{kind} does not stack.");
        }
    }

    private void ApplyAddItem(ItemKind kind, int quantity, HorseTravelState? horseState, CanteenState? canteenState)
    {
        if (quantity == 0)
        {
            return;
        }

        if (kind == ItemKind.Horse)
        {
            AddHorse(quantity, horseState);
            return;
        }

        if (kind == ItemKind.Canteen)
        {
            AddCanteen(quantity, canteenState);
            return;
        }

        if (StackableKinds.Contains(kind))
        {
            AddStackable(kind, quantity);
            return;
        }

        AddNonStackable(kind);
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
        _items[index] = new InventoryItem(current.Kind, current.Quantity + quantity, current.HorseState, current.CanteenState);
    }

    private void AddNonStackable(ItemKind kind)
    {
        if (_items.Any(item => item.Kind == kind))
        {
            throw new InvalidOperationException($"{kind} already exists in inventory.");
        }

        _items.Add(new InventoryItem(kind, 1));
    }

    private void AddHorse(int quantity, HorseTravelState? horseState)
    {
        if (quantity != 1)
        {
            throw new InvalidOperationException("Horse items must have a quantity of 1.");
        }

        if (_items.Any(item => item.Kind == ItemKind.Horse))
        {
            throw new InvalidOperationException("Horse already exists in inventory.");
        }

        _items.Add(new InventoryItem(ItemKind.Horse, 1, horseState));
    }

    private void AddCanteen(int quantity, CanteenState? canteenState)
    {
        if (quantity != 1)
        {
            throw new InvalidOperationException("Canteen items must have a quantity of 1.");
        }

        if (_items.Any(item => item.Kind == ItemKind.Canteen))
        {
            throw new InvalidOperationException("Canteen already exists in inventory.");
        }

        _items.Add(new InventoryItem(ItemKind.Canteen, 1, canteenState: canteenState));
    }
}
