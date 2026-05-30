namespace WildBunch.Domain.Game;

public sealed class GameClock
{
    public int Day { get; private set; } = 1;

    public int Turn { get; private set; }

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
