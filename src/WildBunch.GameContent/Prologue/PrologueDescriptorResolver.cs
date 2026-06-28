using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Prologue;

/// <summary>
/// Resolves the player-visible true-culprit descriptor for the prologue from a seed code.
/// Bridges the internal <see cref="SeedWorldResolver"/> and
/// <see cref="GameSetupResolver"/> (both internal to WildBunch.GameContent.NewGame)
/// to the Application layer, which cannot reference them directly.
/// Uses the same <see cref="SaloonPersonOfInterestDescriptor.Describe"/> path used
/// elsewhere for clues/suspects so there is one canonical formatter.
/// Does NOT expose TrueCulpritId, isTrueCulprit, or internal suspect ids.
/// </summary>
public static class PrologueDescriptorResolver
{
    /// <summary>
    /// Resolves the player-visible true-culprit descriptor for the prologue from a seed code.
    /// </summary>
    public static string ResolveTrueCulpritDescriptor(
        GameDifficulty gameDifficulty = GameDifficulty.Standard,
        string? setupSeedCode = null,
        GameEntropy entropy = GameEntropy.Classic)
    {
        var seed = string.IsNullOrWhiteSpace(setupSeedCode)
            ? SeedWorldResolver.CreateCanonicalSeedCode()
            : SeedWorldResolver.TryParseSeedCode(setupSeedCode, out var parsed)
                ? parsed
                : SeedWorldResolver.CreateCanonicalSeedCode();

        var seedWorld = SeedWorldResolver.Resolve(seed);
        var difficulty = DifficultyEnvelope.For(gameDifficulty);
        var entropyPolicy = EntropyPolicy.For(entropy);
        var resolvedSetup = new GameSetupResolver().Resolve(seedWorld, difficulty, entropyPolicy);
        var trueCulprit = resolvedSetup.CaseFile.Suspects.First(s => s.Id == resolvedSetup.CaseFile.TrueCulpritId);
        return SaloonPersonOfInterestDescriptor.Describe(trueCulprit, resolvedSetup.CaseFile);
    }
}
