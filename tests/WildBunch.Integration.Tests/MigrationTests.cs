using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Projections;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using WildBunch.Persistence.Versioning;
using Npgsql;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task PreAlphaPlaythroughsAreDiscardedByMigrationAndNewSessionCanBeStored()
    {
        const string priorMigration = "20260719061600_AddDiaryDaySchemaVersion";
        const string discardMigration = "20261008180000_InvalidatePreAlphaPlaythroughs";
        using var database = new PostgreSqlTestDatabase();

        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options;

        await using (var context = new WildBunchDbContext(options))
        {
            await context.GetService<IMigrator>().MigrateAsync(priorMigration);
        }

        var preAlphaSession = CreateSession();
        await using (var context = new WildBunchDbContext(options))
        {
            var serializer = new GameSessionJsonSerializer();
            var projector = new TravelDiaryDayProjector();
            var upcasters = new PayloadUpcasterRegistry([]);
            var payloadLoader = new PersistedPayloadLoader(
                upcasters,
                serializer,
                projector,
                rebuildSessionFromEvents: events => SessionRebuilder.RebuildFromEvents(events, serializer));
            var repository = new EfGameSessionRepository(context, serializer, projector, upcasters, payloadLoader);

            await repository.StoreAsync(preAlphaSession);
            await new EfGameSessionUnitOfWork(context).CommitAsync();

            context.GameSessionDiaryDays.Add(new GameSessionDiaryDayEntity
            {
                SessionId = preAlphaSession.Id.Value,
                Sequence = 1,
                PayloadJson = "{}",
                RecordedAtUtc = DateTime.UtcNow,
                SchemaVersion = 1
            });
            await context.SaveChangesAsync();

            Assert.Equal(1, await context.GameSessions.CountAsync());
            Assert.NotEmpty(await context.GameSessionComponents.ToListAsync());
            Assert.NotEmpty(await context.StoredEvents.ToListAsync());
            Assert.Equal(1, await context.GameSessionDiaryDays.CountAsync());
        }

        await using (var context = new WildBunchDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        var newSession = CreateSession();
        await using (var context = new WildBunchDbContext(options))
        {
            Assert.Equal(0, await context.GameSessions.CountAsync());
            Assert.Equal(0, await context.GameSessionComponents.CountAsync());
            Assert.Equal(0, await context.StoredEvents.CountAsync());
            Assert.Equal(0, await context.GameSessionDiaryDays.CountAsync());

            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(priorMigration, appliedMigrations);
            Assert.Contains(discardMigration, appliedMigrations);

            var serializer = new GameSessionJsonSerializer();
            var projector = new TravelDiaryDayProjector();
            var upcasters = new PayloadUpcasterRegistry([]);
            var payloadLoader = new PersistedPayloadLoader(
                upcasters,
                serializer,
                projector,
                rebuildSessionFromEvents: events => SessionRebuilder.RebuildFromEvents(events, serializer));
            var repository = new EfGameSessionRepository(context, serializer, projector, upcasters, payloadLoader);

            await repository.StoreAsync(newSession);
            await new EfGameSessionUnitOfWork(context).CommitAsync();
            var reloaded = await repository.GetByIdAsync(newSession.Id);

            Assert.NotNull(reloaded);
            Assert.Equal(newSession.Player.Name, reloaded!.Player.Name);
            Assert.Equal(1, await context.GameSessions.CountAsync());

            await Assert.ThrowsAsync<NotSupportedException>(
                () => context.GetService<IMigrator>().MigrateAsync(priorMigration));
            Assert.Equal(1, await context.GameSessions.CountAsync());
            Assert.Contains(discardMigration, await context.Database.GetAppliedMigrationsAsync());
        }
    }

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
            Assert.Equal(0, await context.GameSessionDiaryDays.CountAsync());
        }

        await using var commandContext = new WildBunchDbContext(options);
        var serializer = new GameSessionJsonSerializer();
        var payloadLoader = new PersistedPayloadLoader(
            new PayloadUpcasterRegistry([]),
            serializer,
            new TravelDiaryDayProjector(),
            rebuildSessionFromEvents: events => SessionRebuilder.RebuildFromEvents(events, serializer));
        var repository = new EfGameSessionRepository(commandContext, serializer, new TravelDiaryDayProjector(), new PayloadUpcasterRegistry([]), payloadLoader);
        var unitOfWork = new EfGameSessionUnitOfWork(commandContext);
        var session = CreateSession();

        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(session.Player.CurrentTownId, reloaded!.Player.CurrentTownId);
        Assert.Equal(session.Player.Name, reloaded.Player.Name);
        Assert.Equal(GameSessionLogProjection.Project(session).Count, GameSessionLogProjection.Project(reloaded).Count);

        using (var verificationContext = new WildBunchDbContext(options))
        {
            Assert.Equal(1, await verificationContext.GameSessions.CountAsync());
            Assert.Equal(10, await verificationContext.GameSessionComponents.CountAsync());
            Assert.Equal(
                new[] { "caseFile", "clock", "currentActionContext", "player", "pursuitState", "saltSource", "setup", "townVisitState", "unrelatedCriminalLedger", "world" },
                await verificationContext.GameSessionComponents
                    .Where(component => component.SessionId == session.Id.Value)
                    .OrderBy(component => component.ComponentName)
                    .Select(component => component.ComponentName)
                    .ToArrayAsync());
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
        Assert.Contains("GameDifficulty", columns);
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
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.None);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.None);

        var world = new WildBunch.Domain.World.World(
            new[] { dustvale, silvercreek },
            new[] { new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(dustvale.Id);
        session.CompleteGameStart();
        return session;
    }
}
