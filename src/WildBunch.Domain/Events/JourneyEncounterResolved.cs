using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: an encounter resolution attempt was performed (run/fight/bribe).
/// Resolved=false means the encounter persists (failed attempt) — hidden state changes
/// are captured in JourneySnapshot.PendingEncounter.HiddenState.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it.
/// PlayerHealth and WalletCash are ABSOLUTE — Apply sets them from the event.
/// AmmoSpent and StolenItem are ADDITIVE — Apply applies them to the player.
/// PursuitHeat is ABSOLUTE — Apply sets pursuit heat from it. Under the current
/// heat model (ADR-0029), trail encounters do not affect heat; PursuitHeat is
/// always the current heat value (unchanged by encounter resolution).
/// AdditionalDiaryMessages carries narration-only encounter messages from the
/// continued day plan after resolution. Projected via JournalLogProjector on read paths.
/// </summary>
public sealed record JourneyEncounterResolved : IDomainEvent
{
    public required string ChoiceId { get; init; }
    public required string ChoiceLabel { get; init; }
    public required bool Resolved { get; init; }
    public required int PlayerHealth { get; init; }
    public required decimal WalletCash { get; init; }
    public required int AmmoSpent { get; init; }
    public required ItemKind? StolenItemKind { get; init; }
    public required int StolenItemQuantity { get; init; }
    public required int PursuitHeat { get; init; }
    public required int HorseExhaustionDelta { get; init; }
    public required bool ContinuedOnFoot { get; init; }
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required string DiaryMessage { get; init; }
    public required bool DayCompleted { get; init; }
    public required bool JourneyCompleted { get; init; }
    public IReadOnlyList<string> AdditionalDiaryMessages { get; init; } = [];
    public IReadOnlyList<string> DayEntries { get; init; } = [];
}
