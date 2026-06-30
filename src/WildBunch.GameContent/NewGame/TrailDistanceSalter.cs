using System.Security.Cryptography;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Applies entropy-salted trail distance variance to a seed-built <see cref="World"/>.
/// The seed owns the baseline topology and distances; this salter corrupts the
/// distances downstream of the seed codec, bounded by the player-selected entropy mode.
///
/// <list type="table">
/// <item><term>Boring</term><description>±0 — seed defaults preserved (Fixed salt)</description></item>
/// <item><term>Classic</term><description>±1 — mild corruption</description></item>
/// <item><term>Adventurous</term><description>±2 — moderate corruption</description></item>
/// <item><term>Wild</term><description>±3 — heavy corruption</description></item>
/// </list>
///
/// The swing is deterministic per (salt, trailId) pair. For Boring, the salt is the
/// seed code text, so the same seed always produces the same distances (which are
/// the unsalted defaults). For Runtime salt, each new session gets a fresh random
/// salt, so the same seed with Classic+ entropy produces different distances each
/// playthrough.
///
/// Topology is never changed — only distances. The hub-and-spoke concept stays
/// intact; the spokes just have different lengths.
/// </summary>
internal static class TrailDistanceSalter
{
    /// <summary>
    /// Returns a new <see cref="World"/> with the same towns and topology but
    /// with trail distances salted by the entropy policy.
    /// </summary>
    public static World Apply(World world, EntropyPolicy entropy, SaltSource saltSource)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entropy);
        ArgumentNullException.ThrowIfNull(saltSource);

        var maxSwing = MaxSwingForEntropy(entropy.GameEntropy);
        if (maxSwing == 0)
        {
            return world;
        }

        var saltBytes = System.Text.Encoding.UTF8.GetBytes(saltSource.Salt);
        var saltedTrails = world.Trails
            .Select(trail => SaltTrail(trail, saltBytes, maxSwing))
            .ToArray();

        return new World(world.Towns, saltedTrails);
    }

    private static int MaxSwingForEntropy(GameEntropy entropy)
        => entropy switch
        {
            GameEntropy.Boring => 0,
            GameEntropy.Classic => 1,
            GameEntropy.Adventurous => 2,
            GameEntropy.Wild => 3,
            _ => 0
        };

    private static Trail SaltTrail(Trail trail, byte[] saltBytes, int maxSwing)
    {
        var trailIdBytes = System.Text.Encoding.UTF8.GetBytes(trail.Id.Value);
        var combinedBytes = new byte[saltBytes.Length + trailIdBytes.Length];
        Buffer.BlockCopy(saltBytes, 0, combinedBytes, 0, saltBytes.Length);
        Buffer.BlockCopy(trailIdBytes, 0, combinedBytes, saltBytes.Length, trailIdBytes.Length);

        var hashBytes = SHA256.HashData(combinedBytes);
        var hashValue = BitConverter.ToInt32(hashBytes, 0);

        // Map hash to a swing in [-maxSwing, +maxSwing]
        var swingRange = 2 * maxSwing + 1;
        var swing = (Math.Abs(hashValue) % swingRange) - maxSwing;

        var saltedDistance = Math.Max(1m, trail.RideDayDistance + swing);

        return new Trail(
            trail.Id,
            trail.FromTownId,
            trail.ToTownId,
            trail.Risk,
            trail.Terrain,
            trail.WaterFeature,
            saltedDistance);
    }
}
