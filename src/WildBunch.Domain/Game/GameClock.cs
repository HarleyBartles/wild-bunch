namespace WildBunch.Domain.Game;

public sealed class GameClock
{
    public int Day { get; private set; } = 1;

    public int Turn { get; private set; }

    /// <summary>
    /// Names the current turn slot. Derived from <see cref="Turn"/> (0-3) — no persistence format change.
    /// See ADR-0028 and BUNCH-80 clock/turn correction.
    /// </summary>
    public TimeOfDay TimeOfDay => (TimeOfDay)Turn;

    /// <summary>
    /// Sets the clock to an exact day/turn. Used by <see cref="GameSession.Apply(TownActionContextEntered)"/>
    /// during replay to reconstruct clock state from the event. This is the only way to set the clock
    /// to an arbitrary value outside of <see cref="Advance"/>/<see cref="AdvanceTravelDay"/>.
    /// </summary>
    public void Set(int day, int turn)
    {
        Day = day;
        Turn = turn;
    }

    public void Advance()
    {
        Turn++;

        if (Turn >= 4)
        {
            Day++;
            Turn = 0;
        }
    }

    public void AdvanceTravelDay()
    {
        Day++;
        Turn = 0;
    }
}
