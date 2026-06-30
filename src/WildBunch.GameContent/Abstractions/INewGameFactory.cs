using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;

namespace WildBunch.GameContent.Abstractions;

public interface INewGameFactory
{
    GameSession Create(
        string playerName,
        GameDifficulty gameDifficulty = GameDifficulty.Standard,
        string? setupSeedCode = null,
        GameEntropy gameEntropy = GameEntropy.Classic,
        string? startingTownId = null);

    /// <summary>
    /// Resolves the world and case file from the seed code without creating a game session.
    /// Used by the start flow to create a setup-phase session that knows the world
    /// before the player selects a starting town.
    /// </summary>
    (World World, CaseFile CaseFile, string SeedCodeText) ResolveWorld(
        string playerName,
        GameDifficulty gameDifficulty,
        string? setupSeedCode,
        GameEntropy gameEntropy);

    /// <summary>
    /// Resolves the starting wallet and inventory for a given difficulty.
    /// Used by the complete-game-start flow to provide difficulty-owned starting resources.
    /// </summary>
    (Wallet Wallet, DomainInventory Inventory) ResolveStartingResources(GameDifficulty gameDifficulty);
}
