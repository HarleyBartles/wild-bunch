namespace WildBunch.Domain.Cases;

public static class BountyDeclarationMatchPolicy
{
    public static bool MatchesDeclaredWantedIdentity(string? declaredWantedIdentityHandle, Warrant warrant)
    {
        ArgumentNullException.ThrowIfNull(warrant);

        if (string.IsNullOrWhiteSpace(declaredWantedIdentityHandle))
        {
            return false;
        }

        return string.Equals(declaredWantedIdentityHandle, warrant.Id.Value, StringComparison.Ordinal);
    }
}
