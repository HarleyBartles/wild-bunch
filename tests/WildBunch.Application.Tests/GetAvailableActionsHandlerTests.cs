using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Actions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

public sealed class GetAvailableActionsHandlerTests
{
    [Fact]
    public async Task GetAvailableActionsLoadsSessionAndReturnsExpectedActions()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.Supplies | TownServices.Lodging);
        repository.Seed(session);
        var handler = new GetAvailableActionsHandler(repository, new ActionAvailabilityResolver());

        var result = await handler.HandleAsync(new GetAvailableActionsQuery(session.Id.Value));

        Assert.Contains(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewMap);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewJournal);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.BuySupplies);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.StayAtLodging);
    }

    [Fact]
    public async Task GetAvailableActionsThrowsWhenMissing()
    {
        var handler = new GetAvailableActionsHandler(new InMemoryGameSessionRepository(), new ActionAvailabilityResolver());

        var exception = await Assert.ThrowsAsync<GameSessionNotFoundException>(
            () => handler.HandleAsync(new GetAvailableActionsQuery(Guid.NewGuid())));

        Assert.Contains("was not found", exception.Message);
    }

    private static GameSession CreateSession(TownServices currentTownServices)
    {
        var currentTown = new Town(new TownId("current"), "Current Town", currentTownServices);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[]
            {
                new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }
}
