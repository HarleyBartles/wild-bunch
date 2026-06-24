using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a travel day advanced (always exactly once per successful AdvanceJourneyDay).
/// Day is ABSOLUTE — Apply calls Clock.Set(e.Day, 0).
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it.
/// HealthDelta is ADDITIVE — Apply adds to player health.
/// PursuitHeat is ABSOLUTE — Apply sets pursuit heat from it.
/// </summary>
public sealed record TravelDayAdvanced : IDomainEvent
{
    public required int Day { get; init; }
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required int HealthDelta { get; init; }
    public required int PursuitHeat { get; init; }
    public required TravelDayOutcome DayOutcome { get; init; }
    public required string DiaryMessage { get; init; }
    public required string HorseLostMessage { get; init; }
}
