using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a trail event (Lucky or BadLuck) fired during a travel day.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it (captures delay, horse, mode changes).
/// WalletCash and PursuitHeat are ABSOLUTE — Apply sets player wallet and pursuit heat directly.
/// PursuitHeat is future lawman pressure (ADR-0029), not trail danger. It is
/// always the current heat value (unchanged by trail events). HeatIncrease is
/// a dead/reserved field — always 0 today; retained for a future lawman-pressure
/// system but has no effect under the current model.
/// Food/canteen/horse are set ABSOLUTE from the journey snapshot via SyncPlayerFromJourneySnapshot.
/// Horse/delay/mode fields are informational for projections (journey snapshot is the source of truth).
/// </summary>
public sealed record TrailEventApplied : IDomainEvent
{
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required JourneyTrailEventKind TrailEventKind { get; init; }
    public required JourneyTrailEventId TrailEventId { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required decimal WalletDelta { get; init; }
    public required decimal WalletCash { get; init; }
    public required int FoodDelta { get; init; }
    public required int CanteenChargeDelta { get; init; }
    public required int HorseHungerDelta { get; init; }
    public required int HorseThirstDelta { get; init; }
    public required int HorseExhaustionDelta { get; init; }
    public required int DelayDays { get; init; }
    public required int HeatIncrease { get; init; }
    public required int PursuitHeat { get; init; }
    public required TravelMode? TravelModeChangedTo { get; init; }
    public required string DiaryMessage { get; init; }
    public required string HorseLostMessage { get; init; }
}
