using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: an encounter resolution attempt was performed (run/fight/bribe).
/// Resolved=false means the encounter persists (failed attempt) — hidden state changes
/// are captured in JourneySnapshot.PendingEncounter.HiddenState.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it.
/// HealthDelta, WalletDelta, AmmoSpent, StolenItem are ADDITIVE — Apply applies to player.
/// PursuitHeatDelta is ADDITIVE — Apply adds to pursuit heat.
/// </summary>
public sealed record JourneyEncounterResolved : IDomainEvent
{
    public required string ChoiceId { get; init; }
    public required string ChoiceLabel { get; init; }
    public required bool Resolved { get; init; }
    public required int HealthDelta { get; init; }
    public required decimal WalletDelta { get; init; }
    public required int AmmoSpent { get; init; }
    public required ItemKind? StolenItemKind { get; init; }
    public required int StolenItemQuantity { get; init; }
    public required decimal PursuitHeatDelta { get; init; }
    public required int HorseExhaustionDelta { get; init; }
    public required bool ContinuedOnFoot { get; init; }
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required string DiaryMessage { get; init; }
    public required bool DayCompleted { get; init; }
    public required bool JourneyCompleted { get; init; }
}
