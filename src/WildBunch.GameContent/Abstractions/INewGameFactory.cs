using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using SaltSource = WildBunch.Domain.Game.SaltSource;

namespace WildBunch.GameContent.Abstractions;

public interface INewGameFactory
{
    /// <summary>
    /// Resolves the world and case file from the seed code without creating a game session.
    /// Used by the start flow to create a setup-phase session that knows the world
    /// before the player selects a starting town.
    /// </summary>
    (World World, CaseFile CaseFile, string SeedCodeText, SaltSource SaltSource) ResolveWorld(
        string playerName,
        GameDifficulty gameDifficulty,
        string? setupSeedCode,
        GameEntropy gameEntropy);

    /// <summary>
    /// Resolves the world and case file from the seed code with optional dev layout salts.
    /// Used by the dev flow to create a setup-phase session with deterministic layout overrides.
    /// </summary>
    (World World, CaseFile CaseFile, string SeedCodeText, SaltSource SaltSource) ResolveWorld(
        string playerName,
        GameDifficulty gameDifficulty,
        string? setupSeedCode,
        GameEntropy gameEntropy,
        LayoutSalts? devLayoutSalts);

    /// <summary>
    /// Resolves the starting wallet and inventory for a given difficulty.
    /// Used by the complete-game-start flow to provide difficulty-owned starting resources.
    /// </summary>
    (Wallet Wallet, DomainInventory Inventory) ResolveStartingResources(GameDifficulty gameDifficulty);
}
