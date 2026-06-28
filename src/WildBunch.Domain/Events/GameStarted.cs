using WildBunch.Domain.Inventory;
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
    public required GameDifficulty Difficulty { get; init; }
    public required TravelRandomnessState TravelRandomness { get; init; }
    public required GameEntropy Entropy { get; init; }
}
