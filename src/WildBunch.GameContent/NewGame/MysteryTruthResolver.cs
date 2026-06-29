using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// The single entropy-applied mystery-truth seam between
/// <see cref="SeedWorld"/> and <see cref="ResolvedGameSetup"/>.
/// Called explicitly by <see cref="GameSetupResolver.Resolve"/> as a
/// named pipeline step.
/// Transitional implementation is pass-through: all entropy modes use
/// the seed world defaults. BUNCH-93 expands <see cref="Resolve"/> here to
/// add salted culprit reroll, feature reallocation, and Adventurous/Wild
/// variance — without touching <see cref="SeedWorld"/>,
/// <see cref="SeedWorldResolver"/>,
/// <see cref="GameSetupDeterministicLabels"/>, or the seed codec.
/// </summary>
internal static class MysteryTruthResolver
{
    public static MysteryTruthResolution Resolve(
        SeedWorld seedWorld,
        EntropyPolicy entropy,
        ISaltSourceFactory saltSourceFactory,
        GameDifficulty gameDifficulty)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(entropy);
        ArgumentNullException.ThrowIfNull(saltSourceFactory);

        // Transitional pass-through:
        // - ResolvedCulpritIndex = seedWorld.DefaultCulpritIndex (all entropy modes)
        // - ResolvedAccusationIndex = seedWorld.AccusationIndex (all entropy modes)
        // - AppliedCashBonus = min(seedWorld.CashBonus, entropy.CashBonusCap)
        // - SaltSource = Fixed(seedCodeText) for Boring, factory-produced for others
        //
        // The factory is the single source of truth for non-Boring salt creation.
        // In production, RuntimeSaltSourceFactory produces SaltSource.CreateRuntime().
        // In tests, DeterministicSaltSourceFactory produces a fixed salt for reproducibility.
        //
        // BUNCH-93 will expand this method to:
        // - Boring: preserve seed world defaults deterministically (current behavior)
        // - Classic: salted replacement of culprit index and feature allocation
        // - Adventurous: more RNG variance than Classic, still inside normal rules
        // - Wild: Adventurous variance + rule-bending while preserving coherence
        //
        // BUNCH-93 changes ONLY this method and MysteryTruthResolution.
        // It does NOT change SeedWorld, SeedWorldResolver,
        // GameSetupDeterministicLabels, or the seed codec.

        var appliedCashBonus = Math.Min(seedWorld.CashBonus, entropy.CashBonusCap);

        var saltSource = entropy.SaltSourceMode == SaltSourceMode.Fixed
            ? SaltSource.CreateFixed(seedWorld.SeedCodeText)
            : saltSourceFactory.Create(seedWorld.SeedCodeText, gameDifficulty);

        return new MysteryTruthResolution(
            ResolvedCulpritIndex: seedWorld.DefaultCulpritIndex,
            ResolvedAccusationIndex: seedWorld.AccusationIndex,
            AppliedCashBonus: appliedCashBonus,
            SaltSource: saltSource);
    }
}
