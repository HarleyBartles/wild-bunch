using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command forced the session difficulty to a new value.
/// This is a dev-only event — it records dev intent to change the difficulty
/// envelope (travel rules profile) for playtesting, not a gameplay outcome.
/// The difficulty is persisted in the session snapshot, so rehydration after
/// a difficulty change requires no new persistence shape.
/// See BUNCH-94 and ADR-0030.
/// </summary>
public sealed record DevDifficultyForced : IDomainEvent
{
    public required GameDifficulty ForcedDifficulty { get; init; }
}
