using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player selected their starting town from the generated world.
/// This is a distinct fact from GameStarted — the player chooses a town,
/// then the game starts from that choice.
/// </summary>
public sealed record StartingTownSelected : IDomainEvent
{
    public required TownId StartingTownId { get; init; }
    public DateTimeOffset OccurredAt => DateTimeOffset.UtcNow;
}
