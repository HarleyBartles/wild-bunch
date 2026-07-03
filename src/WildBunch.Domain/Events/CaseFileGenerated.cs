using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the caseFile was generated from the seed code, salt source, and entropy.
/// Carries the full caseFile snapshot (suspects, true culprit, opening lead, clues)
/// so the caseFile can be reconstructed by replaying this event without re-running
/// the generation pipeline.
/// This is the event-sourced source of truth for the caseFile — the JSON snapshot
/// is a cache of this event's payload.
/// </summary>
public sealed record CaseFileGenerated : IDomainEvent
{
    public required CaseFileSnapshot CaseFile { get; init; }
    public DateTimeOffset OccurredAt => DateTimeOffset.UtcNow;
}
