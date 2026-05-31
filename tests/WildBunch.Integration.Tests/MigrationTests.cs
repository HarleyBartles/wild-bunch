using Microsoft.EntityFrameworkCore;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using Npgsql;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateGameSessionsTableAndRoundTripSession()
    {
        using var database = new PostgreSqlTestDatabase();

        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options;

        using (var context = new WildBunchDbContext(options))
        {
            await context.Database.MigrateAsync();

            Assert.True(await context.Database.CanConnectAsync());
            Assert.Equal(0, await context.GameSessions.CountAsync());
            Assert.Equal(0, await context.GameSessionComponents.CountAsync());
            Assert.Equal(0, await context.GameSessionLogEntries.CountAsync());
            Assert.Equal(0, await context.GameSessionDiaryDays.CountAsync());
        }

        var repository = new EfGameSessionRepository(new WildBunchDbContext(options), new GameSessionJsonSerializer());
        var session = CreateSession();

        await repository.SaveAsync(session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(session.Player.CurrentTownId, reloaded!.Player.CurrentTownId);
        Assert.Equal(session.Player.Name, reloaded.Player.Name);
        Assert.Equal(session.LogEntries.Count, reloaded.LogEntries.Count);

        using (var verificationContext = new WildBunchDbContext(options))
        {
            Assert.Equal(1, await verificationContext.GameSessions.CountAsync());
            Assert.Equal(6, await verificationContext.GameSessionComponents.CountAsync());
            Assert.Equal(
                new[] { "caseFile", "clock", "player", "pursuitState", "travelRandomness", "world" },
                await verificationContext.GameSessionComponents
                    .Where(component => component.SessionId == session.Id.Value)
                    .OrderBy(component => component.ComponentName)
                    .Select(component => component.ComponentName)
                    .ToArrayAsync());
            Assert.Equal(session.LogEntries.Count, await verificationContext.GameSessionLogEntries.CountAsync());
            Assert.Equal(0, await verificationContext.GameSessionDiaryDays.CountAsync());
        }

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await AssertJsonbColumnTypesAsync(connection);
        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'GameSessions'
            ORDER BY ordinal_position;
            """;
        var columns = new List<string>();
        await using (var reader = await schemaCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }
        }

        Assert.DoesNotContain("StateJson", columns);
        Assert.Contains("SchemaVersion", columns);
        Assert.Contains("TravelDifficulty", columns);
    }

    private static async Task AssertJsonbColumnTypesAsync(NpgsqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_name, data_type
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND column_name = 'PayloadJson'
              AND table_name IN ('GameSessionComponents', 'GameSessionTravelDiaryDays')
            ORDER BY table_name;
            """;

        var payloadColumns = new List<(string TableName, string DataType)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            payloadColumns.Add((reader.GetString(0), reader.GetString(1)));
        }

        Assert.Equal(
            new[]
            {
                ("GameSessionComponents", "jsonb"),
                ("GameSessionTravelDiaryDays", "jsonb")
            },
            payloadColumns);
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
