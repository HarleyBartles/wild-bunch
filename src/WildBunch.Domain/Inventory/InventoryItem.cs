namespace WildBunch.Domain.Inventory;

public sealed record InventoryItem
{
    public InventoryItem(ItemKind kind, int quantity, HorseTravelState? horseState = null, CanteenState? canteenState = null)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        }

        if (kind == ItemKind.Horse)
        {
            if (quantity != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Horse items must have a quantity of 1.");
            }

            if (canteenState is not null)
            {
                throw new ArgumentException("Horse items cannot carry canteen state.", nameof(canteenState));
            }
        }
        else if (kind == ItemKind.Canteen)
        {
            if (quantity != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Canteen items must have a quantity of 1.");
            }

            if (horseState is not null)
            {
                throw new ArgumentException("Canteen items cannot carry horse state.", nameof(horseState));
            }
        }
        else if (horseState is not null || canteenState is not null)
        {
            throw new ArgumentException("Only horse or canteen items can carry travel state.");
        }

        Kind = kind;
        Quantity = quantity;
        HorseState = kind == ItemKind.Horse ? horseState ?? HorseTravelState.Healthy : null;
        CanteenState = kind == ItemKind.Canteen ? canteenState ?? WildBunch.Domain.Inventory.CanteenState.Full() : null;
    }

    public ItemKind Kind { get; }

    public int Quantity { get; }

    public HorseTravelState? HorseState { get; }

    public CanteenState? CanteenState { get; }
}
