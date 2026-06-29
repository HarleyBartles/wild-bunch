using System.Text.Json;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

public sealed class TurnInToSheriffHandlerTests
{
    [Fact]
    public async Task HandleAsyncSettlesBountyOnceAndPersistsTheSettlement()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new TurnInToSheriffHandler(repository, repository);

        var firstResult = await handler.HandleAsync(new TurnInToSheriffCommand(session.Id.Value, "suspect-1", true));
        var secondResult = await handler.HandleAsync(new TurnInToSheriffCommand(session.Id.Value, "suspect-1", true));

        Assert.True(firstResult.Success);
        Assert.Equal(SheriffTurnInOutcome.AcceptedAlive, firstResult.Outcome);
        Assert.Equal(session.Id.Value, firstResult.CurrentSession.Id);
        Assert.Equal("Mira Cline", firstResult.TargetName);
        Assert.Equal(WarrantDisposition.DeadOrAlive, firstResult.Disposition);
        Assert.Equal(2500m, firstResult.BountyAmount);
        Assert.Equal(2525m, firstResult.CurrentSession.Inventory.Wallet.Cash);
        Assert.Single(repository.Sessions.Single().CaseFile.SheriffTurnInSettlements);
        Assert.True(repository.Sessions.Single().CaseFile.SheriffTurnInSettlements[0].IsAlive);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);

        Assert.False(secondResult.Success);
        Assert.Equal(SheriffTurnInOutcome.Rejected, secondResult.Outcome);
        Assert.Equal(2525m, secondResult.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Contains("already been paid", secondResult.Message, StringComparison.OrdinalIgnoreCase);

        var payload = JsonSerializer.Serialize(firstResult);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);
        return session;
    }
}
