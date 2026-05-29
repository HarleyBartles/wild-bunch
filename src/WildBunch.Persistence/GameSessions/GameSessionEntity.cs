namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StateJson { get; set; } = string.Empty;
}
