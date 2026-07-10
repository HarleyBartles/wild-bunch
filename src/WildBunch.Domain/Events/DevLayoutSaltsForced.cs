using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Dev-only event: forces layout salts for town hub layout generation.
/// Stores dev-controlled layout salts in the session for reproducible
/// layout generation. Does not affect gameplay state directly.
/// See BUNCH-147.
/// </summary>
public sealed record DevLayoutSaltsForced : IDomainEvent
{
    public required LayoutSalts ForcedLayoutSalts { get; init; }
}
