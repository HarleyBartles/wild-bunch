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

public sealed class ConfrontWantedSuspectHandlerTests
{
    [Fact]
    public async Task HandleAsyncRecordsTheConfrontationStateAndPersistsIt()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        repository.Seed(session);
        var handler = new ConfrontWantedSuspectHandler(repository, repository);

        var result = await handler.HandleAsync(new ConfrontWantedSuspectCommand(session.Id.Value, "suspect-1", WantedSuspectConfrontationChoice.Fled));

        Assert.True(result.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, result.Outcome);
        Assert.False(result.IsSecured);
        Assert.True(result.SessionChanged);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(new SuspectId("suspect-1"), out var state));
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, state.Outcome);
        Assert.False(state.IsSecured);

        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.NoticeBoard);
        var connected = new Town(new TownId("connected"), "Connected", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, connected },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, connected.Id, TrailRisk.Low) });

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
                    "Wanted for a stage robbery.")
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
    }
}
