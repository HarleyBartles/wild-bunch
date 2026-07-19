namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionDiaryDayEntity
{
    public Guid SessionId { get; set; }

    public int Sequence { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime RecordedAtUtc { get; set; }

    public int SchemaVersion { get; set; }

    public GameSessionEntity Session { get; set; } = null!;
}
