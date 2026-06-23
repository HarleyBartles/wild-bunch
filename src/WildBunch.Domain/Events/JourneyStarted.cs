using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player departed on a new trail journey.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it.
/// </summary>
public sealed record JourneyStarted : IDomainEvent
{
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required string DiaryMessage { get; init; }
}
