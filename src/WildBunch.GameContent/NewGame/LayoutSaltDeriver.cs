using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Derives layout salts for town hub layout generation from seed + entropy policy.
/// Salts are deterministic: same seed + same entropy policy + same townId + same townSlotIndex = same salts.
/// If devLayoutSalts is provided (from GameSession.DevLayoutSalts), uses those instead of deriving.
/// </summary>
internal static class LayoutSaltDeriver
{
    public static LayoutSalts DeriveLayoutSalts(
        SeedWorld seedWorld,
        EntropyPolicy entropyPolicy,
        TownId townId,
        int townSlotIndex,
        LayoutSalts? devLayoutSalts)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(entropyPolicy);
        
        // If dev salts are set, use them directly (dev control overrides derivation)
        if (devLayoutSalts is not null)
        {
            return devLayoutSalts;
        }
        
        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld);
        
        // Derive each salt from seed + town context + entropy policy
        // Fixed mode still preserves deterministic partitioning by seed, town, slot, and concern
        var buildingsSalt = DeriveSalt(seedCode.ToString(), townId.Value, townSlotIndex, "buildings", entropyPolicy.SaltSourceMode);
        var roadsSalt = DeriveSalt(seedCode.ToString(), townId.Value, townSlotIndex, "roads", entropyPolicy.SaltSourceMode);
        var dirtSalt = DeriveSalt(seedCode.ToString(), townId.Value, townSlotIndex, "dirt", entropyPolicy.SaltSourceMode);
        var propsSalt = DeriveSalt(seedCode.ToString(), townId.Value, townSlotIndex, "props", entropyPolicy.SaltSourceMode);
        
        return new LayoutSalts(buildingsSalt, roadsSalt, dirtSalt, propsSalt);
    }
    
    private static string DeriveSalt(string seedCode, string townId, int townSlotIndex, string concern, SaltSourceMode mode)
    {
        var input = $"{seedCode}|{townId}|{townSlotIndex}|{concern}|{mode}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
