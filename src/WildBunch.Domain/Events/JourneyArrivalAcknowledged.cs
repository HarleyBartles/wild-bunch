using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player acknowledged arrival at the destination.
/// Apply archives the journey and clears _journey.
/// </summary>
public sealed record JourneyArrivalAcknowledged : IDomainEvent
{
    public required int JourneySequence { get; init; }
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required string DiaryMessage { get; init; }
}
