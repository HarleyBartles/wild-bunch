using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command changed the session entropy posture.
/// This is a dev-only event — it records dev intent to set variance posture
/// (lucky/unlucky/rare frequency), not a gameplay outcome. The entropy is
/// persisted in the session snapshot, so rehydration after an entropy change
/// requires no new persistence shape. See BUNCH-93 and ADR-0030.
/// </summary>
public sealed record DevEntropyChanged : IDomainEvent
{
    public required GameEntropy NewEntropy { get; init; }
}
