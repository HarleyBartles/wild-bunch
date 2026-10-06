using System.Net;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence;

namespace WildBunch.Integration.Tests.Deployment;

public sealed class DeploymentHealthTests
{
    [Fact]
    public async Task ProductionDoesNotApplyMigrationsAndSeparatesLivenessFromReadiness()
    {
        using var database = new PostgreSqlTestDatabase();
        using var factory = new DeploymentApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        using var liveness = await client.GetAsync("/health");
        using var readiness = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NULL", connection);
        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task ReadinessSucceedsWhenAllApplicationMigrationsAreApplied()
    {
        using var database = new PostgreSqlTestDatabase();
        ApplyMigrations(database.ConnectionString);
        using var factory = new DeploymentApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadinessAllowsLaterMigrationsToBeApplied()
    {
        using var database = new PostgreSqlTestDatabase();
        ApplyMigrations(database.ConnectionString);

        await using (var connection = new NpgsqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (@id, @version)",
                connection);
            command.Parameters.AddWithValue("id", "20990101000000_CompatibleLaterMigration");
            command.Parameters.AddWithValue("version", "10.0.2");
            await command.ExecuteNonQueryAsync();
        }

        using var factory = new DeploymentApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnavailableDatabaseFailsReadinessWithoutFailingLiveness()
    {
        var unavailable = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1",
            Port = 1,
            Database = "unavailable",
            Username = "unavailable",
            Timeout = 1,
            CommandTimeout = 1
        };
        using var factory = new DeploymentApiFactory(unavailable.ConnectionString);
        using var client = factory.CreateClient();

        using var liveness = await client.GetAsync("/health");
        using var readiness = await client.GetAsync("/health/ready");
        var body = await readiness.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        Assert.DoesNotContain("unavailable", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("127.0.0.1", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DevelopmentRetainsStartupMigrationConvenience()
    {
        using var database = new PostgreSqlTestDatabase();
        using var factory = new DeploymentApiFactory(database.ConnectionString, "Development");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static void ApplyMigrations(string connectionString)
    {
        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        using var context = new WildBunchDbContext(options);
        context.Database.Migrate();
    }
}
