namespace WildBunch.Domain.Game;

public sealed class PursuitState
{
    public int Heat { get; private set; }

    public void IncreaseHeat(int amount)
    {
        Heat = Math.Max(0, Heat + amount);
    }
}
