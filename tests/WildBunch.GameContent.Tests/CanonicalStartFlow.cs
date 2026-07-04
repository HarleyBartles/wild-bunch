using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.Abstractions;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Provides the canonical start flow (ResolveWorld → StartSetup → ViewPrologue →
/// SelectStartingTown → CompleteGameStart) for game-content tests that need a
/// fully-started game session. Replaces the legacy SeededNewGameFactory.Create
/// convenience method with the same event-sourced flow used by production handlers.
/// </summary>
internal static class CanonicalStartFlow
{
    public static GameSession StartGame(
        SeededNewGameFactory factory,
        string playerName,
        GameDifficulty gameDifficulty,
        string? setupSeedCode,
        GameEntropy gameEntropy,
        string? startingTownId = null)
    {
        var (world, caseFile, seedCodeText, saltSource) = factory.ResolveWorld(
            playerName, gameDifficulty, setupSeedCode, gameEntropy);

        var session = GameSession.StartSetup(
            playerName, world, caseFile, gameDifficulty, gameEntropy, seedCodeText, saltSource);

        session.ViewPrologue("test-prologue-descriptor");

        var townId = string.IsNullOrWhiteSpace(startingTownId)
            ? world.Towns.First().Id
            : new TownId(startingTownId);
        session.SelectStartingTown(townId);

        var (wallet, inventory) = factory.ResolveStartingResources(gameDifficulty);
        session.CompleteGameStart(wallet, inventory);

        return session;
    }
}
