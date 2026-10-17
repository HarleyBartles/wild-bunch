namespace WildBunch.Persistence.GameSessions;

public sealed class StartupOperationEntity
{
    public Guid OperationId { get; set; }

    public int Kind { get; set; }

    public string RequestFingerprint { get; set; } = string.Empty;

    public Guid SessionId { get; set; }
}
