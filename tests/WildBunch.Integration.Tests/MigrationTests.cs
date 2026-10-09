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

        var preAlphaSessionId = Guid.NewGuid();
        var preAlphaCreatedAt = DateTime.UtcNow;
        const string emptyJson = "{}";
        await using (var context = new WildBunchDbContext(options))
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "GameSessions" ("Id", "CreatedAtUtc", "UpdatedAtUtc", "Status", "GameDifficulty", "SeedCode", "SchemaVersion", "StreamVersion", "SnapshotVersion")
                VALUES ({preAlphaSessionId}, {preAlphaCreatedAt}, {preAlphaCreatedAt}, 'InProgress', 0, NULL, 1, 1, 0);
                """);
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "GameSessionComponents" ("SessionId", "ComponentName", "PayloadJson", "ComponentVersion", "UpdatedAtUtc")
                VALUES ({preAlphaSessionId}, 'player', CAST({emptyJson} AS jsonb), 1, {preAlphaCreatedAt});
                """);
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "GameSessionStoredEvents" ("StreamId", "Sequence", "EventId", "OccurredAtUtc", "EventType", "PayloadJson", "CorrelationId", "CausationId", "SchemaVersion")
                VALUES ({preAlphaSessionId}, 1, {Guid.NewGuid()}, {preAlphaCreatedAt}, 'GameStarted', CAST({emptyJson} AS jsonb), NULL, NULL, 1);
                """);
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "GameSessionTravelDiaryDays" ("SessionId", "Sequence", "PayloadJson", "RecordedAtUtc", "SchemaVersion")
                VALUES ({preAlphaSessionId}, 1, CAST({emptyJson} AS jsonb), {preAlphaCreatedAt}, 1);
                """);

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
            Assert.Contains("20261009001543_AddTravelDiaryProjectionWatermark", appliedMigrations);

            var serializer = new GameSessionJsonSerializer();
            var projector = new TravelDiaryDayProjector();
            var upcasters = new PayloadUpcasterRegistry([]);
            var payloadLoader = new PersistedPayloadLoader(
                upcasters,
                serializer,
                projector,
                rebuildSessionFromEvents: SessionRebuilder.RebuildForComponentCache);
            var repository = new EfGameSessionRepository(context, serializer, projector, upcasters, payloadLoader);

            await repository.StoreAsync(newSession);
            await new EfGameSessionUnitOfWork(context).CommitAsync();
            var reloaded = await repository.GetByIdAsync(newSession.Id);

            Assert.NotNull(reloaded);
            Assert.Equal(newSession.Player.Name, reloaded!.Player.Name);
            Assert.Equal(1, await context.GameSessions.CountAsync());
            var envelope = await context.GameSessions.AsNoTracking().SingleAsync(game => game.Id == newSession.Id.Value);
            Assert.Equal(envelope.StreamVersion, envelope.TravelDiaryProjectionStreamVersion);
            Assert.Equal(0, envelope.TravelDiaryProjectionDayCount);

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
            rebuildSessionFromEvents: SessionRebuilder.RebuildForComponentCache);
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
            Assert.Equal(9, await verificationContext.GameSessionComponents.CountAsync());
            Assert.Equal(
                new[] { "caseFile", "clock", "currentActionContext", "player", "pursuitState", "saltSource", "setup", "townVisitState", "world" },
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
        Assert.Contains("GameDifficulty", columns);
        Assert.Contains("TravelDiaryProjectionStreamVersion", columns);
        Assert.Contains("TravelDiaryProjectionDayCount", columns);
    }

    [Fact]
    public async Task RemovingUnusedSchemaArtifactsPreservesStoredSession()
    {
        const string priorMigration = "20261009001543_AddTravelDiaryProjectionWatermark";
        using var database = new PostgreSqlTestDatabase();

        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options;
        var session = CreateSession();

        await using (var context = new WildBunchDbContext(options))
        {
            await context.GetService<IMigrator>().MigrateAsync(priorMigration);

            var serializer = new GameSessionJsonSerializer();
            var projector = new TravelDiaryDayProjector();
            var upcasters = new PayloadUpcasterRegistry([]);
            var payloadLoader = new PersistedPayloadLoader(
                upcasters,
                serializer,
                projector,
                rebuildSessionFromEvents: SessionRebuilder.RebuildForComponentCache);
            var repository = new EfGameSessionRepository(context, serializer, projector, upcasters, payloadLoader);

            await repository.StoreAsync(session);
            await new EfGameSessionUnitOfWork(context).CommitAsync();
        }

        await using (var context = new WildBunchDbContext(options))
        {
            await context.Database.MigrateAsync();

            var serializer = new GameSessionJsonSerializer();
            var projector = new TravelDiaryDayProjector();
            var upcasters = new PayloadUpcasterRegistry([]);
            var payloadLoader = new PersistedPayloadLoader(
                upcasters,
                serializer,
                projector,
                rebuildSessionFromEvents: SessionRebuilder.RebuildForComponentCache);
            var repository = new EfGameSessionRepository(context, serializer, projector, upcasters, payloadLoader);
            var reloaded = await repository.GetByIdAsync(session.Id);

            Assert.NotNull(reloaded);
            Assert.Equal(session.Player.Name, reloaded!.Player.Name);
            Assert.Equal(session.Player.CurrentTownId, reloaded.Player.CurrentTownId);
            Assert.Equal(session.Version, reloaded.Version);
            Assert.Equal(
                session.AllEvents.Count,
                await context.StoredEvents.CountAsync(storedEvent => storedEvent.StreamId == session.Id.Value));
        }
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
        var dustvale = new Town(new TownId("dustvale"), "Dustvale");
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek");

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
