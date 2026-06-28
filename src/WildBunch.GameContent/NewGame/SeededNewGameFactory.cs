using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.GameContent.NewGame;

public sealed class SeededNewGameFactory : INewGameFactory
{
    private readonly GameSetupPackageBuilder _setupPackageBuilder = new();
    private readonly ISaltSourceFactory _saltSourceFactory;

    public SeededNewGameFactory()
        : this(new RuntimeSaltSourceFactory())
    {
    }

    public SeededNewGameFactory(ISaltSourceFactory saltSourceFactory)
    {
        _saltSourceFactory = saltSourceFactory;
    }

    public GameSession Create(
        string playerName,
        GameDifficulty gameDifficulty = GameDifficulty.Standard,
        string? setupSeedCode = null,
        GameEntropy gameEntropy = GameEntropy.Classic,
        string? startingTownId = null)
    {
        var descriptor = ResolveDescriptor(gameDifficulty, setupSeedCode, gameEntropy);
        var setupPackage = _setupPackageBuilder.Build(descriptor);
        
        // Salt is determined by entropy: Boring = Fixed (deterministic), others = Runtime (variable)
        var saltSource = descriptor.GameEntropy == GameEntropy.Boring
            ? SaltSource.CreateFixed(descriptor.SeedCodeText)
            : _saltSourceFactory.Create(descriptor.SeedCodeText, setupPackage.GameDifficulty);

        // Player-chosen town overrides the seed-derived default; null falls back to the seed default.
        var resolvedStartingTownId = startingTownId is null
            ? setupPackage.StartingTownId
            : new TownId(startingTownId);

        // Always retain the seed code for debugging/reproducibility of the world
        return GameSession.StartNew(
            playerName,
            setupPackage.World,
            setupPackage.CaseFile,
            resolvedStartingTownId,
            setupPackage.StartingWallet,
            setupPackage.StartingInventory,
            setupPackage.GameDifficulty,
            saltSource,
            descriptor.GameEntropy,
            descriptor.SeedCodeText);
    }

    private static StartingWorldDescriptor ResolveDescriptor(GameDifficulty gameDifficulty, string? setupSeedCode, GameEntropy gameEntropy)
    {
        return StartingWorldDescriptorResolver.Resolve(setupSeedCode, gameDifficulty, gameEntropy);
    }
}
