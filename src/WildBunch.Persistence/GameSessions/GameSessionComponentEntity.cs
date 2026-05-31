namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionComponentEntity
{
    public Guid SessionId { get; set; }

    public string ComponentName { get; set; } = string.Empty;

    public int ComponentVersion { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public GameSessionEntity Session { get; set; } = null!;
}
