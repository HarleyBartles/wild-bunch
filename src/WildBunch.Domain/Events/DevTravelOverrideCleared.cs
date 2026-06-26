namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command cleared the pending travel override.
/// Dev-only event. See BUNCH-89 and ADR-0030.
/// </summary>
public sealed record DevTravelOverrideCleared : IDomainEvent;
