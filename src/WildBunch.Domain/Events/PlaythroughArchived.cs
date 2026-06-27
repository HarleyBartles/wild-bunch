using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a playthrough was archived. Archive marks a session as ended-without-deletion
/// and persists a snapshot of the player's last position so a new playthrough can start
/// cleanly while the old one remains queryable. See BUNCH-102.
/// </summary>
public sealed record PlaythroughArchived : IDomainEvent
{
    public required DateTime ArchivedAtUtc { get; init; }
    public required string ArchiveReason { get; init; }
    public required string PlayerName { get; init; }
    public required TownId? LastTownId { get; init; }
    public required string? LastTownName { get; init; }
    public required int Day { get; init; }
    public required string Turn { get; init; }
    public required GameStatus StatusBeforeArchive { get; init; }
}
