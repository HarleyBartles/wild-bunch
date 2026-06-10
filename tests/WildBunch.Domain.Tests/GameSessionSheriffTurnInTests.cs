using System.Text.Json;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionSheriffTurnInTests
{
    [Fact]
    public void AssessSheriffTurnInReturnsAcceptedAliveForKnownDeadOrAliveWarrant()
    {
        var session = CreateSession();

        var result = session.AssessSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        Assert.True(result.Success);
        Assert.Equal(SheriffTurnInOutcome.AcceptedAlive, result.Outcome);
        Assert.Equal("Mira Cline", result.TargetName);
        Assert.Equal(WarrantDisposition.DeadOrAlive, result.Disposition);
        Assert.Equal(2500m, result.BountyAmount);
        Assert.False(result.SessionChanged);

        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AssessSheriffTurnInReturnsAcceptedDeadForKnownDeadOrAliveWarrant()
    {
        var session = CreateSession();

        var result = session.AssessSheriffTurnIn(new SuspectId("suspect-1"), isAlive: false);

        Assert.True(result.Success);
        Assert.Equal(SheriffTurnInOutcome.AcceptedDead, result.Outcome);
        Assert.Equal("Mira Cline", result.TargetName);
        Assert.Equal(WarrantDisposition.DeadOrAlive, result.Disposition);
        Assert.Equal(2500m, result.BountyAmount);
        Assert.False(result.SessionChanged);
    }

    [Fact]
    public void AssessSheriffTurnInReturnsRejectedForDeadAliveOnlyWarrant()
    {
        var session = CreateSession();

        var result = session.AssessSheriffTurnIn(new SuspectId("suspect-2"), isAlive: false);

        Assert.False(result.Success);
        Assert.Equal(SheriffTurnInOutcome.Rejected, result.Outcome);
        Assert.Equal("Reno Pike", result.TargetName);
        Assert.Equal(WarrantDisposition.AliveOnly, result.Disposition);
        Assert.Equal(300m, result.BountyAmount);
        Assert.False(result.SessionChanged);
        Assert.Contains("alive", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AssessSheriffTurnInKeepsWrongPersonAliveAndDeadOutcomesDistinct()
    {
        var session = CreateSession();

        var aliveResult = session.AssessSheriffTurnIn(new SuspectId("suspect-unknown"), isAlive: true);
        var deadResult = session.AssessSheriffTurnIn(new SuspectId("suspect-unknown"), isAlive: false);

        Assert.False(aliveResult.Success);
        Assert.False(deadResult.Success);
        Assert.Equal(SheriffTurnInOutcome.WrongPersonAlive, aliveResult.Outcome);
        Assert.Equal(SheriffTurnInOutcome.WrongPersonDead, deadResult.Outcome);
        Assert.False(aliveResult.SessionChanged);
        Assert.False(deadResult.SessionChanged);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.NoticeBoard);
        var world = new DomainWorld(
            new[] { pinecross },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, pinecross.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-1"),
                    "Mira Cline",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        2500m,
                        new[] { "Red Wren" },
                        new[] { "Raven-feather pin" },
                        "Dodge City Marshal",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for a stage robbery."),
                new Warrant(
                    new WarrantId("warrant-2"),
                    "Reno Pike",
                    new WarrantTerms(
                        WarrantDisposition.AliveOnly,
                        300m,
                        new[] { "The Magpie" },
                        new[] { "Mismatched spurs" },
                        "Silver Creek Sheriff",
                        InvestigationTargetKind.UnrelatedWantedCriminal,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for cattle theft.")
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
    }
}
