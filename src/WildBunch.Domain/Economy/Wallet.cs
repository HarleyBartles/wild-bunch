namespace WildBunch.Domain.Economy;

public readonly record struct Wallet
{
    public Wallet(decimal cash)
    {
        if (cash < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cash));
        }

        Cash = cash;
    }

    public decimal Cash { get; }

    public void Deconstruct(out decimal Cash) => Cash = this.Cash;

    public static Wallet Starting(decimal cash) => new(cash);

    public bool CanAfford(decimal amount) => amount >= 0 && Cash >= amount;

    public Wallet Adjust(decimal amount)
    {
        var nextCash = Cash + amount;
        if (nextCash < 0)
        {
            throw new InvalidOperationException("Wallet cannot go negative.");
        }

        return new Wallet(nextCash);
    }

    public Wallet Spend(decimal amount)
    {
        if (!CanAfford(amount))
        {
            throw new InvalidOperationException("Not enough cash.");
        }

        return new Wallet(Cash - amount);
    }
}
