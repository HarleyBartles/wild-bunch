using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Player-selected entropy/salt policy. Owns the entropy mode and the
/// entropy-derived cash bonus cap. The seed codec does NOT encode this —
/// it is applied downstream of <see cref="SeedWorld"/> by
/// <see cref="MysteryTruthResolver"/>.
/// BUNCH-93 will expand <see cref="MysteryTruthResolver.Resolve"/> to add
/// salted culprit reroll, feature reallocation, and Adventurous/Wild variance
/// using the <see cref="SaltSourceMode"/> selected here.
/// </summary>
public sealed record EntropyPolicy(
    GameEntropy GameEntropy,
    SaltSourceMode SaltSourceMode,
    int CashBonusCap)
{
    /// <summary>
    /// Resolves the entropy policy for the requested entropy mode.
    /// BUNCH-107 ships transitional behavior: Boring uses Fixed salt with 0
    /// cash bonus cap; all other modes use Runtime salt with their current
    /// cash bonus caps. BUNCH-93 will expand <see cref="MysteryTruthResolver"/>
    /// to add salted remix logic for Classic/Adventurous/Wild.
    /// </summary>
    public static EntropyPolicy For(GameEntropy entropy)
        => entropy switch
        {
            GameEntropy.Boring => new EntropyPolicy(
                GameEntropy.Boring,
                SaltSourceMode.Fixed,
                CashBonusCap: 0),
            GameEntropy.Classic => new EntropyPolicy(
                GameEntropy.Classic,
                SaltSourceMode.Runtime,
                CashBonusCap: 2),
            GameEntropy.Adventurous => new EntropyPolicy(
                GameEntropy.Adventurous,
                SaltSourceMode.Runtime,
                CashBonusCap: 5),
            GameEntropy.Wild => new EntropyPolicy(
                GameEntropy.Wild,
                SaltSourceMode.Runtime,
                CashBonusCap: 8),
            _ => throw new ArgumentOutOfRangeException(nameof(entropy), entropy, "Unsupported game entropy.")
        };
}
