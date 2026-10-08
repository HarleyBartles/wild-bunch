using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Final session-start facts after all pipeline stages are applied.
/// Composed by <see cref="GameSetupResolver"/> from:
/// seed code -> SeedWorld -> DifficultyEnvelope -> EntropyPolicy
/// -> MysteryTruthResolution -> ResolvedGameSetup.
/// The canonical start flow (StartSetup → ViewPrologue → SelectStartingTown
/// → CompleteGameStart) consumes these facts without reinterpreting the seed
/// during live play.
/// </summary>
internal sealed record ResolvedGameSetup(
    SeedWorld SeedWorld,
    GameDifficulty GameDifficulty,
    GameEntropy GameEntropy,
    World World,
    TownId StartingTownId,
    CaseFile CaseFile,
    Wallet StartingWallet,
    Inventory StartingInventory,
    int StartingHealth,
    TravelRulesProfile TravelRulesProfile,
    SaltSource SaltSource,
    string SeedCodeText);
