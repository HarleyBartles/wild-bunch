namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player reached zero health and the playthrough ended.
/// The preceding event records the health loss; this event establishes terminal status.
/// </summary>
public sealed record PlayerDied : IDomainEvent
{
    public int? JourneySequence { get; init; }
    public required string DiaryMessage { get; init; }
}
