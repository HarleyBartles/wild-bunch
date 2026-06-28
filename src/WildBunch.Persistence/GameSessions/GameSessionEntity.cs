namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public int GameDifficulty { get; set; }

    public string? SeedCode { get; set; }

    public int SchemaVersion { get; set; }

    public long StreamVersion { get; set; }

    public long? SnapshotVersion { get; set; }

    public ICollection<GameSessionComponentEntity> Components { get; set; } = [];

    public ICollection<GameSessionDiaryDayEntity> TravelDiaryDays { get; set; } = [];

    public ICollection<StoredEventEntity> StoredEvents { get; set; } = [];
}
