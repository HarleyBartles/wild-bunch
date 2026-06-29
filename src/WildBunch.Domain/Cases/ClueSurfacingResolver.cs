using WildBunch.Domain.Game;

namespace WildBunch.Domain.Cases;

/// <summary>
/// Stateless domain service that selects which clue from a <see cref="CaseFile"/>'s
/// <see cref="CaseFile.PublicClues"/> pool surfaces when the player investigates a
/// particular surface (telegraph, gossip, etc.) in a town on a given visit.
/// </summary>
public sealed class ClueSurfacingResolver
{
    /// <summary>
    /// Resolves the clue that surfaces for the given investigation surface, or null
    /// when no eligible (surface-tagged, not-yet-known) clue remains.
    /// </summary>
    /// <remarks>
    /// Selection rules:
    /// <list type="bullet">
    /// <item>Filter <see cref="CaseFile.PublicClues"/> by <see cref="Clue.SourceKind"/>
    /// matching <paramref name="surface"/> and not already present in
    /// <see cref="CaseFile.KnownClues"/>.</item>
    /// <item>Boring mode (<paramref name="salt"/> is null):
    /// <c>(townSlotIndex + visitCount) % eligibleCount</c>.</item>
    /// <item>Salt mode (<paramref name="salt"/> is not null):
    /// <c>hash(salt + townSlotIndex + visitCount) % eligibleCount</c>.</item>
    /// </list>
    /// Both modes are deterministic for the same inputs.
    /// </remarks>
    public Clue? Resolve(CaseFile caseFile, InvestigationSourceKind surface, int townSlotIndex, int visitCount, SaltSource? salt)
    {
        ArgumentNullException.ThrowIfNull(caseFile);

        var eligible = caseFile.PublicClues
            .Where(clue => clue.SourceKind == surface)
            .Where(clue => !caseFile.KnownClues.Any(known => known.Id.Equals(clue.Id)))
            .ToArray();

        if (eligible.Length == 0)
        {
            return null;
        }

        // Boring mode: simple deterministic slot/visit rotation.
        // Salt mode: deterministic hash of (salt, townSlotIndex, visitCount). The uint
        // cast of the hash avoids the Math.Abs(int.MinValue) pitfall and keeps the
        // modulo result in [0, eligible.Length) regardless of the hash's sign.
        var index = salt is null
            ? (townSlotIndex + visitCount) % eligible.Length
            : (int)((uint)CombineSalt(salt.Salt, townSlotIndex, visitCount) % (uint)eligible.Length);

        return eligible[index];
    }

    private static int CombineSalt(string salt, int townSlotIndex, int visitCount)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + salt.GetHashCode(StringComparison.Ordinal);
            hash = hash * 31 + townSlotIndex;
            hash = hash * 31 + visitCount;
            return hash;
        }
    }
}
