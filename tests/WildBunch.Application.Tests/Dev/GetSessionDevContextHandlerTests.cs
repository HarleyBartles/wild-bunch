using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using Town = WildBunch.Domain.World.Town;
using TownId = WildBunch.Domain.World.TownId;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;
using World = WildBunch.Domain.World.World;

namespace WildBunch.Application.Tests.Dev;

public sealed class GetSessionDevContextHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsSessionContext_WithSetupPosture()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession();
        repository.Seed(session);

        var handler = new GetSessionDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSessionDevContextQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.SessionId);
        Assert.Equal("Active", result.Status);
        Assert.Equal(session.GameDifficulty.ToString(), result.GameDifficulty);
        Assert.Equal(session.GameEntropy.ToString(), result.GameEntropy);
        Assert.NotNull(result.SaltPosture);
        Assert.Equal(session.SaltSource.Mode.ToString(), result.SaltPosture!.Mode);
        Assert.Equal(session.SaltSource.Salt, result.SaltPosture.Salt);
        Assert.Equal(session.Clock.Day, result.Clock.Day);
        Assert.Equal(session.Clock.Turn, result.Clock.Turn);
        Assert.Equal(session.CurrentTown.TownId.Value, result.CurrentTownId);
        Assert.Equal(session.CurrentTown.TownName, result.CurrentTownName);
        Assert.Equal(session.CurrentActionContext.ToString(), result.CurrentActionContext);
        Assert.False(result.HasActiveJourney);
        // Seed code is honestly reported as not retained
        Assert.False(result.SeedCodeRetained);
        Assert.Null(result.SeedCodeText);
    }

    [Fact]
    public async Task HandleAsync_AfterForceDevSaltSource_ReflectsFixedSalt()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession();
        session.ForceDevSaltSource(SaltSource.CreateFixed("deadbeef"));
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetSessionDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSessionDevContextQuery(session.Id.Value));

        Assert.Equal("Fixed", result.SaltPosture!.Mode);
        Assert.Equal("deadbeef", result.SaltPosture.Salt);
    }

    [Fact]
    public async Task HandleAsync_AfterClearDevSaltSource_ReflectsRuntimeMode()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession();
        session.ForceDevSaltSource(SaltSource.CreateFixed("deadbeef"));
        session.MarkEventsCommitted();
        session.ClearDevSaltSource();
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetSessionDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSessionDevContextQuery(session.Id.Value));

        Assert.Equal("Runtime", result.SaltPosture!.Mode);
    }

    private static GameSession CreateSeededSession()
    {
        var town = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new World(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null, suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: null, GameDifficulty.Easy,
            SaltSource.CreateFixed(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }
}
