namespace WildBunch.Application.Dev.Models;

public sealed record SessionAuditDto(
    Guid SessionId,
    IReadOnlyList<SessionAuditEntryDto> Entries);

public sealed record SessionAuditEntryDto(
    long Sequence,
    string EventType,
    string Summary,
    DateTime OccurredAtUtc);
