namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the pending dev saloon override was consumed by normal saloon look-around.
/// Emitted by LookAroundSaloon() right before the SaloonPersonOfInterestSpotted event,
/// in the same command execution. Apply clears _pendingDevSaloonOverride.
/// This event makes replay safe: replaying Forced -> Consumed -> Spotted
/// reconstructs the correct final state with no pending override.
/// Dev-only event - not a gameplay outcome. See BUNCH-90 and ADR-0030.
/// </summary>
public sealed record DevSaloonOverrideConsumed : IDomainEvent;
