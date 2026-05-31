using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace WildBunch.Persistence;

internal static class PersistenceDbContextOptions
{
    private const string PostgreSqlConnectionStringName = "WildBunchPostgresDb";

    internal static void Configure(DbContextOptionsBuilder optionsBuilder, IConfiguration configuration)
    {
        optionsBuilder.UseNpgsql(ResolvePostgreSqlConnectionString(configuration));
    }

    private static string ResolvePostgreSqlConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(PostgreSqlConnectionStringName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            $"Connection string '{PostgreSqlConnectionStringName}' is required for Wild Bunch persistence.");
    }
}
