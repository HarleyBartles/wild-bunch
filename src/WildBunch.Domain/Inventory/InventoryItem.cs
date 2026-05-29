namespace WildBunch.Domain.Inventory;

public sealed record InventoryItem
{
    public InventoryItem(ItemKind kind, int quantity, HorseCondition? horseCondition = null)
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

            if (horseCondition is null)
            {
                throw new ArgumentNullException(nameof(horseCondition), "Horse items require a condition.");
            }
        }
        else if (horseCondition is not null)
        {
            throw new ArgumentException("Only horse items can carry a horse condition.", nameof(horseCondition));
        }

        Kind = kind;
        Quantity = quantity;
        HorseCondition = horseCondition;
    }

    public ItemKind Kind { get; }

    public int Quantity { get; }

    public HorseCondition? HorseCondition { get; }
}
