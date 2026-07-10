using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the game world was generated from the seed code, salt source, and entropy.
/// Carries the full world snapshot (towns + trails) and case file so they can be reconstructed
/// by replaying this event without re-running the generation pipeline.
/// This is the event-sourced source of truth for the world and case file — the JSON snapshot
/// is a cache of this event's payload.
/// </summary>
public sealed record WorldGenerated : IDomainEvent
{
    public required string SeedCode { get; init; }
    public required SaltSource SaltSource { get; init; }
    public required GameEntropy GameEntropy { get; init; }
    public required WorldSnapshot World { get; init; }
    public required CaseFileSnapshot CaseFile { get; init; }
    public DateTimeOffset OccurredAt => DateTimeOffset.UtcNow;
}
