using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a trail event (Lucky or BadLuck) fired during a travel day.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it (captures delay, horse, mode changes).
/// WalletDelta, FoodDelta, CanteenChargeDelta are ADDITIVE — Apply adds to player.
/// HeatIncrease is ADDITIVE — Apply adds to pursuit heat.
/// Horse/delay/mode fields are informational for projections (journey snapshot is the source of truth).
/// </summary>
public sealed record TrailEventApplied : IDomainEvent
{
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required JourneyTrailEventKind TrailEventKind { get; init; }
    public required JourneyTrailEventId TrailEventId { get; init; }
    public required decimal WalletDelta { get; init; }
    public required int FoodDelta { get; init; }
    public required int CanteenChargeDelta { get; init; }
    public required int HorseHungerDelta { get; init; }
    public required int HorseThirstDelta { get; init; }
    public required int HorseExhaustionDelta { get; init; }
    public required int DelayDays { get; init; }
    public required decimal HeatIncrease { get; init; }
    public required TravelMode? TravelModeChangedTo { get; init; }
    public required string DiaryMessage { get; init; }
    public required string HorseLostMessage { get; init; }
}
