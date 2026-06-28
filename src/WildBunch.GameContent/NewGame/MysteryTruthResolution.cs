using WildBunch.Domain.Game;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Entropy-applied mystery-truth resolution. This is the output of
/// <see cref="MysteryTruthResolver.Resolve"/> and sits between
/// <see cref="SeedWorld"/> (seed-owned) and
/// <see cref="ResolvedGameSetup"/> (final session-start facts).
/// BUNCH-93 will vary <see cref="ResolvedCulpritIndex"/> and
/// <see cref="SaltSource"/> by entropy mode. Transitional behavior is
/// pass-through: all entropy modes use the seed world defaults.
/// </summary>
internal sealed record MysteryTruthResolution(
    int ResolvedCulpritIndex,
    int ResolvedAccusationIndex,
    int AppliedCashBonus,
    SaltSource SaltSource);
