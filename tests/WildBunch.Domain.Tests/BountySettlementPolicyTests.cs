using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class BountySettlementPolicyTests
{
    [Fact]
    public void CalculateCappedFineCapsTheFineAtTheAvailableWallet()
    {
        Assert.Equal(4m, BountySettlementPolicy.CalculateCappedFine(4m, 10m));
        Assert.Equal(0m, BountySettlementPolicy.CalculateCappedFine(0m, 10m));
        Assert.Equal(10m, BountySettlementPolicy.CalculateCappedFine(25m, 10m));
    }

    [Fact]
    public void TryCreateSheriffTurnInSettlementStateRejectsDuplicates()
    {
        var session = CreateSession();
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Killed);

        var firstAssessment = session.AssessSheriffTurnIn(new SuspectId("suspect-1"), isAlive: false);
        var firstDecision = BountySettlementPolicy.TryCreateSheriffTurnInSettlementState(
            session.CaseFile,
            firstAssessment,
            new SuspectId("suspect-1"),
            isAlive: false,
            day: session.Clock.Day,
            turn: session.Clock.Turn,
            out var settlementState,
            out var rejectionResult);

        Assert.True(firstDecision);
        Assert.NotNull(settlementState);
        Assert.Equal("Mira Cline", settlementState.TargetName);
        Assert.Equal(2500m, settlementState.BountyAmount);

        session.CaseFile.RecordSheriffTurnInSettlementState(settlementState);

        var secondAssessment = session.AssessSheriffTurnIn(new SuspectId("suspect-1"), isAlive: false);
        var secondDecision = BountySettlementPolicy.TryCreateSheriffTurnInSettlementState(
            session.CaseFile,
            secondAssessment,
            new SuspectId("suspect-1"),
            isAlive: false,
            day: session.Clock.Day,
            turn: session.Clock.Turn,
            out var duplicateSettlementState,
            out var duplicateRejectionResult);

        Assert.False(secondDecision);
        Assert.Null(duplicateSettlementState);
        Assert.Equal(SheriffTurnInOutcome.Rejected, duplicateRejectionResult.Outcome);
        Assert.Contains("already been paid", duplicateRejectionResult.Message, StringComparison.OrdinalIgnoreCase);
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
