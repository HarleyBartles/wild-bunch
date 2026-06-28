using WildBunch.Domain.Game;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command forced the session RNG salt to a fixed salt source.
/// This is a dev-only event — it records dev intent to set up reproducibility
/// posture, not a gameplay outcome. The salt source is persisted in the session
/// snapshot, so rehydration after a salt change requires no new persistence shape.
/// See BUNCH-101 and ADR-0030.
/// </summary>
public sealed record DevSaltSourceForced : IDomainEvent
{
    public required SaltSource ForcedSaltSource { get; init; }
}
