namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the pending dev travel override was consumed by normal travel advancement.
/// Emitted by PrepareTravelDayAdvance() right before the TravelDayAdvanced event,
/// in the same command execution. Apply clears _pendingDevTravelOverride.
/// This event makes replay safe: replaying Forced -> Consumed -> TravelDayAdvanced
/// reconstructs the correct final state with no pending override.
/// Dev-only event — not a gameplay outcome. See BUNCH-89 and ADR-0030.
/// </summary>
public sealed record DevTravelOverrideConsumed : IDomainEvent;
