namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command cleared the pending saloon override.
/// Dev-only event. See BUNCH-90 and ADR-0030.
/// </summary>
public sealed record DevSaloonOverrideCleared : IDomainEvent;
