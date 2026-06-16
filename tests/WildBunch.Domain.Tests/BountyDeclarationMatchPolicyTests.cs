using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests;

public sealed class BountyDeclarationMatchPolicyTests
{
    [Fact]
    public void MatchesDeclaredWantedIdentityReturnsTrueOnlyForTheExactWarrantId()
    {
        var warrant = new Warrant(
            new WarrantId("warrant-public-1"),
            "Mira Cline",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                2500m,
                Array.Empty<string>(),
                Array.Empty<string>(),
                "Dodge City Marshal",
                InvestigationTargetKind.TrueCulprit,
                Array.Empty<OutlawGangId>(),
                null));

        Assert.True(BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity("warrant-public-1", warrant));
        Assert.False(BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity("warrant-public-99", warrant));
        Assert.False(BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity(string.Empty, warrant));
        Assert.False(BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity(null, warrant));
    }
}
