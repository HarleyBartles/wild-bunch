namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player viewed the prologue and the starting clue (suspect identifier) was revealed.
/// This marks the transition from "setup complete" to "ready to select starting town".
/// </summary>
public sealed record PrologueViewed : IDomainEvent
{
    public required string RevealedSuspectIdentifier { get; init; }
}