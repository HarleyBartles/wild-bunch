using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Orchestrates the full game-setup pipeline:
/// seed code -> SeedWorld -> DifficultyEnvelope -> EntropyPolicy
/// -> MysteryTruthResolution -> ResolvedGameSetup -> GameSession.
/// Calls <see cref="MysteryTruthResolver.Resolve"/> as an explicit named step.
/// Calls <see cref="StartingTownPolicy.ResolveStartingTown"/> to validate the
/// player's chosen starting town against the generated world.
/// </summary>
internal sealed class GameSetupResolver
{
    private readonly ISaltSourceFactory _saltSourceFactory;

    public GameSetupResolver()
        : this(new RuntimeSaltSourceFactory())
    {
    }

    public GameSetupResolver(ISaltSourceFactory saltSourceFactory)
    {
        ArgumentNullException.ThrowIfNull(saltSourceFactory);
        _saltSourceFactory = saltSourceFactory;
    }

    public ResolvedGameSetup Resolve(
        SeedWorld seedWorld,
        DifficultyEnvelope difficulty,
        EntropyPolicy entropy,
        TownId? playerChosenStartingTownId = null)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(difficulty);
        ArgumentNullException.ThrowIfNull(entropy);

        // 1. Resolve mystery truth — the single entropy-applied seam between
        //    seed world and resolved setup. BUNCH-93 expands this method.
        var mysteryTruth = MysteryTruthResolver.Resolve(
            seedWorld,
            entropy,
            _saltSourceFactory,
            difficulty.Difficulty);

        // 2. Build the deterministic source from the seed code.
        var seedCodeText = SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode);
        var source = new GameSetupDeterministicSource(seedCodeText, null);

        // 3. Build world from seed world. The seed owns the map; it does NOT
        //    choose the starting town. Wild entropy may trim outlier towns.
        var world = MapGenerator.Generate(seedWorld, source, entropy.GameEntropy, mysteryTruth.SaltSource);

        // 4. Resolve starting town via the setup/policy seam. The player can
        //    start in any town that exists in the generated world. If no town
        //    is supplied, a safe non-seed-authored default is used.
        //    Future seam: difficulty may constrain eligibility.
        var startingTownId = StartingTownPolicy.ResolveStartingTown(world, playerChosenStartingTownId);

        // 5. Build case file using resolved culprit/accusation indices from
        //    MysteryTruthResolution — NOT raw seed world defaults.
        var isCanonical = seedWorld.IsCanonical;
        var caseFile = isCanonical
            ? SeedCaseBuilder.CreateCanonicalCaseFile(
                source,
                world,
                mysteryTruth.ResolvedCulpritIndex,
                mysteryTruth.ResolvedAccusationIndex)
            : SeedCaseBuilder.CreateCaseFile(
                source,
                world,
                mysteryTruth.ResolvedCulpritIndex,
                mysteryTruth.ResolvedAccusationIndex);

        // 6. Compute final cash: difficulty-owned base + entropy-capped seed bonus.
        var finalCash = difficulty.StartingCash + mysteryTruth.AppliedCashBonus;

        // 7. Build inventory from difficulty envelope.
        var startingInventory = SeedInventoryBuilder.CreateStartingLoadout(
            difficulty.TravelRules,
            difficulty);

        // 8. Build wallet from final cash.
        var startingWallet = SeedInventoryBuilder.CreateStartingWallet(finalCash);

        // 9. Compute starting health from difficulty.
        var startingHealth = StartingHealthFor(difficulty.Difficulty);

        return new ResolvedGameSetup(
            seedWorld,
            difficulty.Difficulty,
            entropy.GameEntropy,
            world,
            startingTownId,
            caseFile,
            startingWallet,
            startingInventory,
            startingHealth,
            difficulty.TravelRules,
            mysteryTruth.SaltSource,
            seedCodeText);
    }

    public ResolvedGameSetup Resolve(
        SeedWorld seedWorld,
        DifficultyEnvelope difficulty,
        EntropyPolicy entropy,
        LayoutSalts? devLayoutSalts,
        TownId? playerChosenStartingTownId = null)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(difficulty);
        ArgumentNullException.ThrowIfNull(entropy);

        var mysteryTruth = MysteryTruthResolver.Resolve(
            seedWorld,
            entropy,
            _saltSourceFactory,
            difficulty.Difficulty);

        var seedCodeText = SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode);
        var source = new GameSetupDeterministicSource(seedCodeText, devLayoutSalts);

        var world = MapGenerator.Generate(
            seedWorld,
            source,
            entropy.GameEntropy,
            mysteryTruth.SaltSource,
            devLayoutSalts);

        var startingTownId = StartingTownPolicy.ResolveStartingTown(world, playerChosenStartingTownId);

        var isCanonical = seedWorld.IsCanonical;
        var caseFile = isCanonical
            ? SeedCaseBuilder.CreateCanonicalCaseFile(
                source,
                world,
                mysteryTruth.ResolvedCulpritIndex,
                mysteryTruth.ResolvedAccusationIndex)
            : SeedCaseBuilder.CreateCaseFile(
                source,
                world,
                mysteryTruth.ResolvedCulpritIndex,
                mysteryTruth.ResolvedAccusationIndex);

        var finalCash = difficulty.StartingCash + mysteryTruth.AppliedCashBonus;

        var startingInventory = SeedInventoryBuilder.CreateStartingLoadout(
            difficulty.TravelRules,
            difficulty);

        var startingWallet = SeedInventoryBuilder.CreateStartingWallet(finalCash);

        var startingHealth = StartingHealthFor(difficulty.Difficulty);

        return new ResolvedGameSetup(
            seedWorld,
            difficulty.Difficulty,
            entropy.GameEntropy,
            world,
            startingTownId,
            caseFile,
            startingWallet,
            startingInventory,
            startingHealth,
            difficulty.TravelRules,
            mysteryTruth.SaltSource,
            seedCodeText,
            devLayoutSalts);
    }

    private static int StartingHealthFor(GameDifficulty gameDifficulty)
        => gameDifficulty switch
        {
            GameDifficulty.Easy => 1250,
            GameDifficulty.Challenging => 800,
            GameDifficulty.Brutal => 600,
            _ => 1000
        };
}
