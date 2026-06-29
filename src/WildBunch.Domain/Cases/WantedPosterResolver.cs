using WildBunch.Domain.Game;

namespace WildBunch.Domain.Cases;

/// <summary>
/// Stateless domain service that selects which warrant from a
/// <see cref="CaseFile.PublicWarrants"/> pool surfaces on a wanted poster in a
/// given town on a given visit.
/// </summary>
/// <remarks>
/// Two selection modes:
/// <list type="bullet">
/// <item><b>Boring mode</b> (salt is null): <c>warrants[(townSlotIndex + visitCount) % eligibleCount]</c>.</item>
/// <item><b>Salt mode</b> (salt is non-null): a stable hash of <c>salt + townSlotIndex + visitCount</c>
///   reduced modulo the eligible count. The hash is process-stable (it does not use
///   <see cref="string.GetHashCode()"/>, which is randomized per process).</item>
/// </list>
/// Eligible warrants are all <see cref="CaseFile.PublicWarrants"/> not already in
/// <see cref="CaseFile.KnownWarrants"/>, with the culprit warrant
/// (<see cref="InvestigationTargetKind.TrueCulprit"/>) excluded unless
/// <see cref="CaseFile.KillerReleaseState"/>.<see cref="KillerReleaseState.IsReleased"/>
/// is true. Returns null when the eligible pool is exhausted.
/// </remarks>
public sealed class WantedPosterResolver
{
    public Warrant? Resolve(CaseFile caseFile, int townSlotIndex, int visitCount, SaltSource? salt)
    {
        ArgumentNullException.ThrowIfNull(caseFile);

        var eligible = caseFile.PublicWarrants
            .Where(w => !caseFile.KnownWarrants.Any(k => k.Id.Equals(w.Id)))
            .Where(w => w.Terms.TargetKind != InvestigationTargetKind.TrueCulprit
                || caseFile.KillerReleaseState.IsReleased)
            .ToArray();

        if (eligible.Length == 0)
        {
            return null;
        }

        var index = salt is null
            ? ((townSlotIndex + visitCount) % eligible.Length + eligible.Length) % eligible.Length
            : StableSaltIndex(salt.Salt, townSlotIndex, visitCount, eligible.Length);

        return eligible[index];
    }

    /// <summary>
    /// Stable manual hash over the salt + town slot + visit count, reduced modulo
    /// the eligible count. Does NOT use <see cref="string.GetHashCode()"/> (not
    /// stable across process restarts). Uses a prime multiplier over char codes,
    /// matching the established <c>StableHash</c> pattern in this domain.
    /// </summary>
    private static int StableSaltIndex(string salt, int townSlotIndex, int visitCount, int modulus)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in salt)
            {
                hash = (hash * 31) + c;
            }

            hash = (hash * 31) + townSlotIndex;
            hash = (hash * 31) + visitCount;

            var remainder = hash % modulus;
            return ((remainder + modulus) % modulus);
        }
    }
}
