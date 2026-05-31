namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public int TravelDifficulty { get; set; }

    public int SchemaVersion { get; set; }

    public ICollection<GameSessionComponentEntity> Components { get; set; } = [];

    public ICollection<GameSessionLogEntryEntity> LogEntries { get; set; } = [];

    public ICollection<GameSessionDiaryDayEntity> TravelDiaryDays { get; set; } = [];
}
