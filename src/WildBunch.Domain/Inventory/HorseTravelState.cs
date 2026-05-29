namespace WildBunch.Domain.Inventory;

public sealed record HorseTravelState
{
    public HorseTravelState(int hunger = 0, int thirst = 0, int exhaustion = 0)
    {
        if (hunger < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hunger), "Hunger cannot be negative.");
        }

        if (thirst < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thirst), "Thirst cannot be negative.");
        }

        if (exhaustion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exhaustion), "Exhaustion cannot be negative.");
        }

        Hunger = hunger;
        Thirst = thirst;
        Exhaustion = exhaustion;
    }

    public static HorseTravelState Healthy { get; } = new();

    public int Hunger { get; }

    public int Thirst { get; }

    public int Exhaustion { get; }

    public bool IsDead => Hunger >= 3 || Thirst >= 2 || Exhaustion >= 5;

    public bool IsLame => !IsDead && Exhaustion >= 3;

    public bool CanProvideMountedTravel => !IsDead && !IsLame;

    public HorseTravelState AdvanceTravelDay(bool horseFed)
        => horseFed
            ? new HorseTravelState(Hunger, Thirst, Exhaustion + 1)
            : new HorseTravelState(Hunger + 1, Thirst, Exhaustion + 1);

    public HorseTravelState IncreaseHunger(int amount = 1)
        => Increase(Hunger, amount, hunger => new HorseTravelState(hunger, Thirst, Exhaustion));

    public HorseTravelState RecoverHunger(int amount = 1)
        => Decrease(Hunger, amount, hunger => new HorseTravelState(hunger, Thirst, Exhaustion));

    public HorseTravelState IncreaseThirst(int amount = 1)
        => Increase(Thirst, amount, thirst => new HorseTravelState(Hunger, thirst, Exhaustion));

    public HorseTravelState RecoverThirst(int amount = 1)
        => Decrease(Thirst, amount, thirst => new HorseTravelState(Hunger, thirst, Exhaustion));

    public HorseTravelState IncreaseExhaustion(int amount = 1)
        => Increase(Exhaustion, amount, exhaustion => new HorseTravelState(Hunger, Thirst, exhaustion));

    private static HorseTravelState Increase(int currentValue, int amount, Func<int, HorseTravelState> projector)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        return amount == 0 ? projector(currentValue) : projector(currentValue + amount);
    }

    private static HorseTravelState Decrease(int currentValue, int amount, Func<int, HorseTravelState> projector)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        if (amount == 0)
        {
            return projector(currentValue);
        }

        return projector(Math.Max(0, currentValue - amount));
    }
}
