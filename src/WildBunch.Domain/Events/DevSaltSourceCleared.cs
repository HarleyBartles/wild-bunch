namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command cleared the fixed salt source and restored runtime RNG.
/// Dev-only event. See BUNCH-101 and ADR-0030.
/// </summary>
public sealed record DevSaltSourceCleared : IDomainEvent;
