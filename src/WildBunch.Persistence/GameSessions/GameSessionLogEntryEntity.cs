using WildBunch.Domain.Game;

namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionLogEntryEntity
{
    public Guid SessionId { get; set; }

    public int Sequence { get; set; }

    public GameLogEntryKind Kind { get; set; }

    public string Message { get; set; } = string.Empty;

    public int Day { get; set; }

    public int Turn { get; set; }

    public GameSessionEntity Session { get; set; } = null!;
}
