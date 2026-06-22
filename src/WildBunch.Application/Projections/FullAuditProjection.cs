using WildBunch.Domain.Events;

namespace WildBunch.Application.Projections;

/// <summary>
/// Full audit projection: the complete event log derived from domain events.
/// This is a read-only projection — it does not mutate aggregate state.
/// See ADR-0028.
/// </summary>
public sealed record FullAuditProjection(
    Guid SessionId,
    IReadOnlyList<AuditEntry> Entries) : IProjectionResult;

public sealed record AuditEntry(
    int Sequence,
    string EventType,
    string Summary,
    DateTime OccurredAtUtc);
