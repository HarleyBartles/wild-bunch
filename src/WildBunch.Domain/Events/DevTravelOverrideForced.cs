using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command forced a pending travel override.
/// This is a dev-only event — it records dev intent, not a gameplay outcome.
/// The override is consumed by the next DevTravelOverrideConsumed + TravelDayAdvanced pair.
/// See BUNCH-89 and ADR-0030.
/// </summary>
public sealed record DevTravelOverrideForced : IDomainEvent
{
    public required TravelDayEncounterCategory ForcedCategory { get; init; }
    public JourneyFoeProfile? FoeProfile { get; init; }
    public string? EncounterMessage { get; init; }
}
