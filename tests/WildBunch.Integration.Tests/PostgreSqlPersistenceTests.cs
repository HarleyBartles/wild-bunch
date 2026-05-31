using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Integration.Tests;

public sealed class PostgreSqlPersistenceTests
{
    private const string PostgreSqlConnectionStringEnvironmentVariable = "ConnectionStrings__WildBunchPostgresDb";
    private static readonly TravelRandomnessState DeterministicTravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty);

    [Fact]
    public void AddPersistence_UsesSqliteByDefault()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddPersistence(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
    }

    [Fact]
    public void AddPersistence_UsesPostgreSqlWhenConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{PersistenceOptions.SectionName}:Provider"] = PersistenceProvider.PostgreSql.ToString(),
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
    public async Task PostgreSqlLaneRoundTripsAggregateComponentsLogsAndDiaryWhenConnectionStringIsProvided()
    {
        var connectionString = Environment.GetEnvironmentVariable(PostgreSqlConnectionStringEnvironmentVariable);
        Skip.If(string.IsNullOrWhiteSpace(connectionString), $"Set {PostgreSqlConnectionStringEnvironmentVariable} to exercise the PostgreSQL persistence lane.");

        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var serializer = new GameSessionJsonSerializer();
        var session = CreateTwoDayTravelSession();

        await using (var context = new WildBunchDbContext(options))
        {
            await context.Database.MigrateAsync();

            var repository = new EfGameSessionRepository(context, serializer);
            await repository.SaveAsync(session);

            var reloaded = await repository.GetByIdAsync(session.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(session.Id, reloaded!.Id);
            Assert.Equal(session.Player.Name, reloaded.Player.Name);
            Assert.Equal(session.Player.CurrentTownId, reloaded.Player.CurrentTownId);
            Assert.Equal(session.Status, reloaded.Status);
            Assert.Equal(session.LogEntries.Count, reloaded.LogEntries.Count);
            Assert.Equal(session.TravelDiaryDays.Count, reloaded.TravelDiaryDays.Count);
            Assert.NotNull(reloaded.Journey);
            Assert.Equal(JourneyStatus.Completed, reloaded.Journey!.Status);
            Assert.Equal(session.Player.CurrentTownId, reloaded.Player.CurrentTownId);
        }

        try
        {
            await using var verificationContext = new WildBunchDbContext(options);

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
            Assert.Contains(componentRows, component => component.ComponentName == "travelRandomness");
            Assert.Contains(componentRows, component => component.ComponentName == "journey");
            Assert.All(componentRows, component => Assert.False(string.IsNullOrWhiteSpace(component.PayloadJson)));

            var journeyComponent = Assert.Single(componentRows, component => component.ComponentName == "journey");
            Assert.Contains("\"JourneySequence\":1", journeyComponent.PayloadJson, StringComparison.Ordinal);
            Assert.Contains("openpass", journeyComponent.PayloadJson, StringComparison.OrdinalIgnoreCase);

            var logRows = await verificationContext.GameSessionLogEntries
                .AsNoTracking()
                .Where(entry => entry.SessionId == session.Id.Value)
                .OrderBy(entry => entry.Sequence)
                .ToArrayAsync();

            Assert.Equal(Enumerable.Range(0, logRows.Length), logRows.Select(entry => entry.Sequence));
            Assert.Equal(session.LogEntries.Select(entry => entry.Message), logRows.Select(entry => entry.Message));

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

    private static GameSession CreateTwoDayTravelSession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);
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
            TravelDifficulty.Easy,
            travelRandomness: DeterministicTravelRandomness);

        var preview = new TravelResolver().PreviewJourney(session.World, session.Player.CurrentTownId, holloway.Id, session.Player.Inventory, session.TravelRules);
        Assert.True(preview.Success);
        session.StartJourney(preview.Preview!);

        var firstDay = session.AdvanceJourneyDay();
        Assert.True(firstDay.Success);
        Assert.Equal(JourneyStatus.Active, firstDay.Status);

        var secondDay = session.AdvanceJourneyDay();
        Assert.True(secondDay.Success);
        Assert.Equal(JourneyStatus.Completed, secondDay.Status);
        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Completed, session.Journey!.Status);

        return session;
    }

    private static async Task CleanupSessionAsync(DbContextOptions<WildBunchDbContext> options, Guid sessionId)
    {
        await using var cleanupContext = new WildBunchDbContext(options);
        cleanupContext.GameSessions.Remove(new GameSessionEntity { Id = sessionId });
        await cleanupContext.SaveChangesAsync();
    }
}
