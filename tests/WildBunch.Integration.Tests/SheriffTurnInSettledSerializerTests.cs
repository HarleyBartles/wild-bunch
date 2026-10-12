using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Integration.Tests;

public sealed class SheriffTurnInSettledSerializerTests
{
    [Fact]
    public void DeserializeEvent_RejectsNegativeBountySettlement()
    {
        var serializer = new GameSessionJsonSerializer();
        var settlement = new SheriffTurnInSettled
        {
            TargetSuspectId = new SuspectId("suspect-1"),
            TargetName = "Mira Cline",
            Disposition = WarrantDisposition.DeadOrAlive,
            IsAlive = true,
            BountyAmount = 1m,
            Message = "The sheriff pays you $1.00.",
            Day = 1,
            Turn = 1
        };
        var json = serializer.SerializeEvent(settlement).Replace("\"bountyAmount\":1", "\"bountyAmount\":-1");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            serializer.DeserializeEvent(nameof(SheriffTurnInSettled), json));
    }
}
