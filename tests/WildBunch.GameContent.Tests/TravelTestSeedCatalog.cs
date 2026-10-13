using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Seed-world fixtures consumed by travel setup behavior tests.
/// </summary>
internal static class TravelTestSeedCatalog
{
    internal static readonly SeedWorldEntry CanonicalBoring = new(
        SeedWorldResolver.CreateCanonicalSeedWorld(),
        GameDifficulty.Standard,
        GameEntropy.Boring);

    internal static readonly SeedWorldEntry CanonicalClassic = new(
        SeedWorldResolver.CreateCanonicalSeedWorld(),
        GameDifficulty.Standard,
        GameEntropy.Classic);

    internal static string ResolveSeedCode(SeedWorldEntry entry)
    {
        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(entry.SeedWorld);
        return SeedWorldResolver.FormatSeedCode(seedCode);
    }

    internal static GameSession CreateSession(SeedWorldEntry entry)
        => CanonicalStartFlow.StartGame(
            new SeededNewGameFactory(),
            "Ranger Vale",
            entry.GameDifficulty,
            ResolveSeedCode(entry),
            entry.GameEntropy);
}

internal sealed record SeedWorldEntry(
    SeedWorld SeedWorld,
    GameDifficulty GameDifficulty,
    GameEntropy GameEntropy);
