using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a journey reached its destination.
/// DestinationTownId is ABSOLUTE — Apply sets player town.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it.
/// </summary>
public sealed record JourneyCompleted : IDomainEvent
{
    public required TownId DestinationTownId { get; init; }
    public required string DestinationTownName { get; init; }
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required string DiaryMessage { get; init; }
}
