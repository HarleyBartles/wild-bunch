using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;

namespace WildBunch.Domain.Tests;

public sealed class MoneyInvariantTests
{
    [Fact]
    public void Wallet_RejectsNegativeCash()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Wallet(-1m));
    }

    [Fact]
    public void WarrantTerms_RejectsNegativeBounty()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateWarrantTerms(-1m));
    }

    [Fact]
    public void SheriffTurnInSettlementState_RejectsNegativeBounty()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSettlementState(-1m));
    }

    [Fact]
    public void ZeroWalletAndBountyValuesRemainValid()
    {
        var wallet = new Wallet(0m);
        var warrantTerms = CreateWarrantTerms(0m);
        var settlement = CreateSettlementState(0m);

        Assert.Equal(0m, wallet.Cash);
        Assert.Equal(0m, warrantTerms.BountyAmount);
        Assert.Equal(0m, settlement.BountyAmount);
    }

    private static WarrantTerms CreateWarrantTerms(decimal bountyAmount)
        => new(
            disposition: WarrantDisposition.DeadOrAlive,
            bountyAmount: bountyAmount,
            knownAliases: Array.Empty<string>(),
            knownFeatures: Array.Empty<string>(),
            issuingSource: "Sheriff",
            targetKind: InvestigationTargetKind.GangMember,
            gangAffiliations: Array.Empty<OutlawGangId>(),
            advancesGangPressureFor: null);

    private static SheriffTurnInSettlementState CreateSettlementState(decimal bountyAmount)
        => new(
            new SuspectId("suspect-1"),
            "Mira Cline",
            WarrantDisposition.DeadOrAlive,
            IsAlive: true,
            BountyAmount: bountyAmount,
            Day: 1,
            Turn: 1);
}
