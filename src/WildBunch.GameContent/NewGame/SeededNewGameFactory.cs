using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.Abstractions;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using SaltSource = WildBunch.Domain.Game.SaltSource;

namespace WildBunch.GameContent.NewGame;

public sealed class SeededNewGameFactory : INewGameFactory
{
    private readonly GameSetupResolver _setupResolver;
    private readonly ISaltSourceFactory _saltSourceFactory;

    public SeededNewGameFactory()
        : this(new RuntimeSaltSourceFactory())
    {
    }

    public SeededNewGameFactory(ISaltSourceFactory saltSourceFactory)
    {
        _saltSourceFactory = saltSourceFactory;
        _setupResolver = new GameSetupResolver(saltSourceFactory);
    }

    public (World World, CaseFile CaseFile, string SeedCodeText, SaltSource SaltSource) ResolveWorld(
        string playerName,
        GameDifficulty gameDifficulty,
        string? setupSeedCode,
        GameEntropy gameEntropy)
    {
        var seed = ParseOrGenerateSeed(setupSeedCode);
        var seedWorld = SeedWorldResolver.Resolve(seed);
        var difficulty = DifficultyEnvelope.For(gameDifficulty);
        var entropy = EntropyPolicy.For(gameEntropy);
        var resolvedSetup = _setupResolver.Resolve(
            seedWorld,
            difficulty,
            entropy,
            playerChosenStartingTownId: null);

        return (resolvedSetup.World, resolvedSetup.CaseFile, resolvedSetup.SeedCodeText, resolvedSetup.SaltSource);
    }

    public (World World, CaseFile CaseFile, string SeedCodeText, SaltSource SaltSource) ResolveWorld(
        string playerName,
        GameDifficulty gameDifficulty,
        string? setupSeedCode,
        GameEntropy gameEntropy,
        LayoutSalts? devLayoutSalts)
    {
        var seed = ParseOrGenerateSeed(setupSeedCode);
        var seedWorld = SeedWorldResolver.Resolve(seed);
        var difficulty = DifficultyEnvelope.For(gameDifficulty);
        var entropy = EntropyPolicy.For(gameEntropy);
        var resolvedSetup = _setupResolver.Resolve(
            seedWorld,
            difficulty,
            entropy,
            devLayoutSalts,
            playerChosenStartingTownId: null);

        return (resolvedSetup.World, resolvedSetup.CaseFile, resolvedSetup.SeedCodeText, resolvedSetup.SaltSource);
    }

    public (Wallet Wallet, DomainInventory Inventory) ResolveStartingResources(GameDifficulty gameDifficulty)
    {
        var difficulty = DifficultyEnvelope.For(gameDifficulty);
        var inventory = SeedInventoryBuilder.CreateStartingLoadout(
            difficulty.TravelRules,
            difficulty);
        var wallet = SeedInventoryBuilder.CreateStartingWallet(difficulty.StartingCash);
        return (wallet, inventory);
    }

    private static Guid ParseOrGenerateSeed(string? setupSeedCode)
        => string.IsNullOrWhiteSpace(setupSeedCode)
            ? SeedWorldResolver.CreateCanonicalSeedCode()
            : SeedWorldResolver.TryParseSeedCode(setupSeedCode, out var parsed)
                ? parsed
                : throw new ArgumentException("Seed code must be a UUID-shaped string.", nameof(setupSeedCode));
}
