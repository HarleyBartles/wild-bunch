namespace WildBunch.Domain.Game;

public readonly record struct Supplies(int Units)
{
    public static Supplies Starting() => new(12);

    public bool CanAfford(int amount) => amount >= 0 && Units >= amount;

    public Supplies Subtract(int amount)
    {
        if (!CanAfford(amount))
        {
            throw new InvalidOperationException("Not enough supplies.");
        }

        return new Supplies(Units - amount);
    }
}
