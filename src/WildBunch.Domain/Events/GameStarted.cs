using WildBunch.Domain.Inventory;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a new game session was started with the given player and world configuration.
/// </summary>
public sealed record GameStarted : IDomainEvent
{
    public required string PlayerName { get; init; }
    public required TownId StartingTownId { get; init; }
    public required string StartingTownName { get; init; }
    public required int StartingHealth { get; init; }
    public required decimal StartingWallet { get; init; }
    public required IReadOnlyList<InventoryItem> StartingInventoryItems { get; init; }
    public required GameDifficulty GameDifficulty { get; init; }
    public required SaltSource SaltSource { get; init; }
    public required GameEntropy GameEntropy { get; init; }
    /// <summary>
    /// The seed code used for world generation. This is the UUID encoding of the starting world descriptor.
    /// Same seed means same starting world under the same setup envelope. Does not change during play.
    /// </summary>
    public string? SeedCode { get; init; }
}
