using WildBunch.Application.Games.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests;

public sealed class TravelToTownHandlerTests
{
    [Fact]
    public async Task TravelToConnectedTownSucceedsSavesAndReturnsUpdatedState()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new TravelToTownHandler(repository, new TravelResolver());

        var result = await handler.HandleAsync(new TravelToTownCommand(session.Id.Value, "silvercreek"));

        Assert.True(result.Success);
        Assert.Equal("Travelled to silvercreek.", result.Message);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal("silvercreek", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(10, result.CurrentSession.Player.Supplies);
        Assert.Equal(1, result.CurrentSession.Clock.Turn);
        Assert.Equal(1, result.CurrentSession.PursuitState.Heat);
    }

    [Fact]
    public async Task TravelToUnconnectedTownFailsAndDoesNotSave()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new TravelToTownHandler(repository, new TravelResolver());

        var result = await handler.HandleAsync(new TravelToTownCommand(session.Id.Value, "dryridge"));

        Assert.False(result.Success);
        Assert.Equal("No trail connects those towns.", result.Message);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Equal("dustvale", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(12, result.CurrentSession.Player.Supplies);
        Assert.Equal(0, result.CurrentSession.Clock.Turn);
        Assert.Equal(0, result.CurrentSession.PursuitState.Heat);
    }

    [Fact]
    public async Task TravelWithInsufficientSuppliesFailsAndDoesNotSave()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(supplyUnits: 1);
        repository.Seed(session);
        var handler = new TravelToTownHandler(repository, new TravelResolver());

        var result = await handler.HandleAsync(new TravelToTownCommand(session.Id.Value, "silvercreek"));

        Assert.False(result.Success);
        Assert.Equal("Not enough supplies to travel.", result.Message);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Equal("dustvale", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(1, result.CurrentSession.Player.Supplies);
        Assert.Equal(0, result.CurrentSession.Clock.Turn);
        Assert.Equal(0, result.CurrentSession.PursuitState.Heat);
    }

    private static GameSession CreateSession(int supplyUnits = 12)
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge", TownServices.None);

        var world = new World(
            new[] { dustvale, silvercreek, dryridge },
            new[]
            {
                new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, SupplyCost: 2, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var session = GameSession.StartNew("Ranger Vale", world, caseFile);

        session.Player.SpendSupplies(12 - supplyUnits);
        return session;
    }
}
