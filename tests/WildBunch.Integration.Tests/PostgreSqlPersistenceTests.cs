using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Integration.Tests;

public sealed class PostgreSqlPersistenceTests
{
    private const string PostgreSqlConnectionStringEnvironmentVariable = "ConnectionStrings__WildBunchPostgresDb";

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
    public async Task PostgreSqlRoundTripsSessionWhenConnectionStringIsProvided()
    {
        var connectionString = Environment.GetEnvironmentVariable(PostgreSqlConnectionStringEnvironmentVariable);
        Skip.If(string.IsNullOrWhiteSpace(connectionString), $"Set {PostgreSqlConnectionStringEnvironmentVariable} to exercise the PostgreSQL persistence lane.");

        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new WildBunchDbContext(options);
        await context.Database.MigrateAsync();

        var repository = new EfGameSessionRepository(context, new GameSessionJsonSerializer());
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
