using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player departed on a new trail journey.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it.
/// PursuitHeat is ABSOLUTE — Apply sets pursuit heat to 0 (leaving town resets heat).
/// See ADR-0029.
/// </summary>
public sealed record JourneyStarted : IDomainEvent
{
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required string DiaryMessage { get; init; }
    public required int PursuitHeat { get; init; }
}
