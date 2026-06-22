namespace WildBunch.Persistence.GameSessions;

/// <summary>
/// Persistence envelope for a typed domain event. Infrastructure only — Domain never sees this type.
/// The envelope wraps typed domain events at the store boundary for storage, indexing, and concurrency.
/// See ADR-0028.
/// </summary>
public sealed class StoredEventEntity
{
    public Guid StreamId { get; set; }
    public long Sequence { get; set; }
    public Guid EventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    public int SchemaVersion { get; set; }

    public GameSessionEntity Session { get; set; } = null!;
}
