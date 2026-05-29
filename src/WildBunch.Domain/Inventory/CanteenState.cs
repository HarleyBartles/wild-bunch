namespace WildBunch.Domain.Inventory;

public sealed record CanteenState
{
    public CanteenState(int charges, int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        }

        if (charges < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(charges), "Charges cannot be negative.");
        }

        if (charges > capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(charges), "Charges cannot exceed capacity.");
        }

        Charges = charges;
        Capacity = capacity;
    }

    public static CanteenState Full(int capacity = 2) => new(capacity, capacity);

    public int Charges { get; }

    public int Capacity { get; }

    public bool HasWater => Charges > 0;

    public CanteenState Consume(int charges)
    {
        if (charges < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(charges), "Charges cannot be negative.");
        }

        if (charges == 0)
        {
            return this;
        }

        if (charges > Charges)
        {
            throw new InvalidOperationException("Canteen does not have enough water charges.");
        }

        return new CanteenState(Charges - charges, Capacity);
    }
}
