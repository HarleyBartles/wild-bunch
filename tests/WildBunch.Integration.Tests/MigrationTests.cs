using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Integration.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateGameSessionsTableAndRoundTripSession()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var context = new WildBunchDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        var repository = new EfGameSessionRepository(new WildBunchDbContext(options), new GameSessionJsonSerializer());
        var session = CreateSession();

        await repository.SaveAsync(session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(session.Player.CurrentTownId, reloaded!.Player.CurrentTownId);
        Assert.Equal(session.Player.Name, reloaded.Player.Name);
        Assert.Equal(session.LogEntries.Count, reloaded.LogEntries.Count);
    }

    private static GameSession CreateSession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);

        var world = new WildBunch.Domain.World.World(
            new[] { dustvale, silvercreek },
            new[] { new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(true, false, true), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        return GameSession.StartNew("Ranger Vale", world, caseFile);
    }
}
