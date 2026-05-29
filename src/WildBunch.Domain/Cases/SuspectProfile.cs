namespace WildBunch.Domain.Cases;

public readonly record struct SuspectIdentityFact(string Description);

public readonly record struct SuspectAlias(string Name, AliasKind Kind);

public enum AliasKind
{
    Nickname = 0,
    FormerName = 1,
    StreetName = 2,
    KnownAs = 3,
    CoverIdentity = 4
}

public sealed record SuspectProfile
{
    public static SuspectProfile Empty { get; } = new(Array.Empty<SuspectAlias>(), Array.Empty<SuspectIdentityFact>());

    public SuspectProfile(IEnumerable<SuspectAlias> aliases, IEnumerable<SuspectIdentityFact> identifyingFacts)
    {
        ArgumentNullException.ThrowIfNull(aliases);
        ArgumentNullException.ThrowIfNull(identifyingFacts);

        Aliases = aliases.Distinct().ToArray();
        IdentifyingFacts = identifyingFacts.Distinct().ToArray();
    }

    public IReadOnlyList<SuspectAlias> Aliases { get; }

    public IReadOnlyList<SuspectIdentityFact> IdentifyingFacts { get; }
}
