using WildBunch.Application.Games.Commands;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Integration.Tests;

public sealed class EfGameSessionRepositoryTests
{
    [Fact]
    public async Task SaveAndLoadNewSessionRoundTripsThroughSqlite()
    {
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var session = CreateSession();

        await repository.SaveAsync(session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(session.Id, reloaded!.Id);
        Assert.Equal(session.Player.Name, reloaded.Player.Name);
        Assert.Equal(session.Player.CurrentTownId, reloaded.Player.CurrentTownId);
        Assert.Equal(session.Status, reloaded.Status);
        Assert.Equal(session.LogEntries.Count, reloaded.LogEntries.Count);
    }

    [Fact]
    public async Task SaveAfterTravelUpdatesReloadedState()
    {
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var resolver = new TravelResolver();
        var session = CreateSession();

        await repository.SaveAsync(session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var travelResult = resolver.Travel(loaded!.World, loaded, new TownId("silvercreek"));

        Assert.True(travelResult.Success);

        await repository.SaveAsync(loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(new TownId("silvercreek"), reloaded!.Player.CurrentTownId);
        Assert.Equal(10, reloaded.Player.Supplies.Units);
        Assert.Equal(1, reloaded.Clock.Turn);
        Assert.Equal(1, reloaded.PursuitState.Heat);
        Assert.Contains(reloaded.LogEntries, entry => entry.Kind == GameLogEntryKind.Travel);
    }

    private static EfGameSessionRepository CreateRepository(SqlitePersistenceFixture fixture)
        => new(fixture.CreateContext(), new GameSessionJsonSerializer());

    private static GameSession CreateSession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge", TownServices.None);

        var world = new WildBunch.Domain.World.World(
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
        return GameSession.StartNew("Ranger Vale", world, caseFile);
    }
}
