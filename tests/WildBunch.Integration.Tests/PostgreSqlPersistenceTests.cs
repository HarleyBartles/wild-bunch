using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class PostgreSqlPersistenceTests
{
    private const string PostgreSqlConnectionStringEnvironmentVariable = "ConnectionStrings__WildBunchPostgresDb";
    private static readonly SaltSource DeterministicSaltSource = SaltSource.CreateFixed(string.Empty);

    [Fact]
    public void AddPersistence_UsesPostgreSqlWhenConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WildBunchPostgresDb"] = "Host=localhost;Database=wild-bunch;Username=wild-bunch;Password=wild-bunch"
            })
            .Build();

        services.AddPersistence(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    [SkippableFact]
    public async Task PostgreSqlUnitOfWorkCommitsStagedSessionChanges()
    {
        Skip.If(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PostgreSqlConnectionStringEnvironmentVariable)), $"Set {PostgreSqlConnectionStringEnvironmentVariable} to exercise the PostgreSQL persistence lane.");
        using var database = new PostgreSqlTestDatabase();

        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(database.ConnectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        var serializer = new GameSessionJsonSerializer();
        var session = CreateCompletedTravelSession();

        await using (var context = new WildBunchDbContext(options))
        {
            await context.Database.MigrateAsync();

            var repository = new EfGameSessionRepository(context, serializer);
            var unitOfWork = new EfGameSessionUnitOfWork(context);

            await repository.StoreAsync(session);

            await using var verificationBeforeCommit = new WildBunchDbContext(options);
            Assert.Equal(0, await verificationBeforeCommit.GameSessions.CountAsync());

            await unitOfWork.CommitAsync();
        }

        await using (var verificationContext = new WildBunchDbContext(options))
        {
            Assert.Equal(1, await verificationContext.GameSessions.CountAsync());
            Assert.Equal(11, await verificationContext.GameSessionComponents.CountAsync());
            Assert.Equal(session.TravelDiaryDays.Count, await verificationContext.GameSessionDiaryDays.CountAsync());
        }
    }

    [SkippableFact]
    public async Task PostgreSqlLaneRoundTripsAggregateComponentsLogsAndDiaryWhenConnectionStringIsProvided()
    {
        Skip.If(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PostgreSqlConnectionStringEnvironmentVariable)), $"Set {PostgreSqlConnectionStringEnvironmentVariable} to exercise the PostgreSQL persistence lane.");
        using var database = new PostgreSqlTestDatabase();

        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(database.ConnectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        var serializer = new GameSessionJsonSerializer();
        var session = CreateCompletedTravelSession();

        await using (var context = new WildBunchDbContext(options))
        {
            await context.Database.MigrateAsync();

            var repository = new EfGameSessionRepository(context, serializer);
            var unitOfWork = new EfGameSessionUnitOfWork(context);
            await repository.StoreAsync(session);
            await unitOfWork.CommitAsync();

            var reloaded = await repository.GetByIdAsync(session.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(session.Id, reloaded!.Id);
            Assert.Equal(session.Player.Name, reloaded.Player.Name);
            Assert.Equal(session.Player.CurrentTownId, reloaded.Player.CurrentTownId);
            Assert.Equal(session.Status, reloaded.Status);
            Assert.Equal(GameSessionLogProjection.Project(session).Count, GameSessionLogProjection.Project(reloaded).Count);
            Assert.Equal(session.TravelDiaryDays.Count, reloaded.TravelDiaryDays.Count);
            Assert.NotNull(reloaded.Journey);
            Assert.Equal(JourneyStatus.Completed, reloaded.Journey!.Status);
            Assert.Equal(session.Player.CurrentTownId, reloaded.Player.CurrentTownId);
        }

        try
        {
            await using var verificationContext = new WildBunchDbContext(options);

            var payloadColumns = await GetPayloadColumnTypesAsync(database.ConnectionString);
            Assert.Equal(
                new[]
                {
                    ("GameSessionComponents", "jsonb"),
                    ("GameSessionTravelDiaryDays", "jsonb")
                },
                payloadColumns);

            var jsonbJourneyMatches = await CountJsonbJourneyMatchesAsync(database.ConnectionString, session.Id.Value);
            Assert.Equal(1, jsonbJourneyMatches);

            var componentRows = await verificationContext.GameSessionComponents
                .AsNoTracking()
                .Where(component => component.SessionId == session.Id.Value)
                .OrderBy(component => component.ComponentName)
                .ToArrayAsync();

            Assert.Contains(componentRows, component => component.ComponentName == "player");
            Assert.Contains(componentRows, component => component.ComponentName == "world");
            Assert.Contains(componentRows, component => component.ComponentName == "caseFile");
            Assert.Contains(componentRows, component => component.ComponentName == "clock");
            Assert.Contains(componentRows, component => component.ComponentName == "pursuitState");
            Assert.Contains(componentRows, component => component.ComponentName == "setup");
            Assert.Contains(componentRows, component => component.ComponentName == "saltSource");
            Assert.Contains(componentRows, component => component.ComponentName == "journey");
            Assert.All(componentRows, component => Assert.False(string.IsNullOrWhiteSpace(component.PayloadJson)));

            var journeyComponent = Assert.Single(componentRows, component => component.ComponentName == "journey");
            Assert.Contains("\"status\": 2", journeyComponent.PayloadJson, StringComparison.Ordinal);

            // After BUNCH-86, log entries are derived from the event stream via
            // JournalLogProjector, not stored in a GameSessionLogEntries table.
            // Verify the event stream has events (the projector derives log entries from these).
            var eventCount = await verificationContext.StoredEvents
                .AsNoTracking()
                .Where(e => e.StreamId == session.Id.Value)
                .CountAsync();
            Assert.True(eventCount > 0);

            var diaryRows = await verificationContext.GameSessionDiaryDays
                .AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .ToArrayAsync();

            Assert.Equal(Enumerable.Range(0, diaryRows.Length), diaryRows.Select(day => day.Sequence));
            Assert.Equal(session.TravelDiaryDays.Select(day => day.DayNumber), diaryRows.Select(day => serializer.DeserializeTravelDiaryDay(day.PayloadJson).DayNumber));
            Assert.Equal(session.TravelDiaryDays.Count, diaryRows.Length);
        }
        finally
        {
            await CleanupSessionAsync(options, session.Id.Value);
        }
    }

    private static async Task<(string TableName, string DataType)[]> GetPayloadColumnTypesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_name, data_type
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name IN ('GameSessionComponents', 'GameSessionTravelDiaryDays')
              AND column_name = 'PayloadJson'
            ORDER BY table_name;
            """;

        var result = new List<(string TableName, string DataType)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add((reader.GetString(0), reader.GetString(1)));
        }

        return result.ToArray();
    }

    private static async Task<int> CountJsonbJourneyMatchesAsync(string connectionString, Guid sessionId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM "GameSessionComponents"
            WHERE "SessionId" = @sessionId
              AND "ComponentName" = 'journey'
              AND "PayloadJson" @> '{"status":2}'::jsonb;
            """;
        command.Parameters.AddWithValue("sessionId", sessionId);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static GameSession CreateCompletedTravelSession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.None);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.None);
        var world = new World(
            new[] { dustvale, holloway },
            new[]
            {
                new Trail(new TrailId("trail-postgres"), dustvale.Id, holloway.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 5m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1)
        });

        var session = GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            dustvale.Id,
            Wallet.Starting(25m),
            inventory,
            GameDifficulty.Easy,
            saltSource: DeterministicSaltSource);

        var preview = CreatePostgreSqlLanePreview(session.Player.CurrentTownId, holloway.Id, "Dustvale", "Holloway");
        Assert.True(preview.Success);
        session.StartJourney(preview.Preview!);

        var dayResult = session.AdvanceJourneyDay();
        Assert.True(dayResult.Success);
        Assert.Equal(JourneyStatus.Completed, dayResult.Status);
        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Completed, session.Journey!.Status);

        return session;
    }
    private static TravelPreviewResult CreatePostgreSqlLanePreview(TownId originTownId, TownId destinationTownId, string originTownName, string destinationTownName)
    {
        var preview = new TravelPreview(
            originTownId,
            destinationTownId,
            originTownName,
            destinationTownName,
            new TravelRouteProfile("trail-postgres", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 1m, 1m, 1m, Array.Empty<string>()),
            TravelMode.Mounted,
            MountedTravelAvailable: true,
            WaterSecure: true,
            RideDayDistance: 1m,
            RemainingRideDayDistance: 1m,
            BaselineRideDays: 1,
            ExpectedDays: 1,
            RemainingDays: 1,
            CanteenChargesPerDay: 0,
            RequiredCanteenCharges: 0,
            AvailableCanteenCharges: 0,
            CanteenReserveCharges: 0,
            DelayMarginDays: 0,
            DelayRisk: false,
            RequiredFood: 1,
            AvailableFood: 4,
            RequiredHorseFeed: 0,
            AvailableHorseFeed: 0,
            HorseState: HorseTravelState.Healthy,
            Warnings: Array.Empty<string>());

        return new TravelPreviewResult(true, string.Empty, preview);
    }

    private static async Task CleanupSessionAsync(DbContextOptions<WildBunchDbContext> options, Guid sessionId)
    {
        await using var cleanupContext = new WildBunchDbContext(options);
        cleanupContext.GameSessions.Remove(new GameSessionEntity { Id = sessionId });
        await cleanupContext.SaveChangesAsync();
    }
}
