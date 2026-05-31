using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace WildBunch.Persistence;

internal static class PersistenceDbContextOptions
{
    internal static void Configure(DbContextOptionsBuilder optionsBuilder, IConfiguration configuration)
    {
        var persistenceProvider = ResolveProvider(configuration);

        switch (persistenceProvider)
        {
            case PersistenceProvider.Sqlite:
                optionsBuilder.UseSqlite(SqliteConnectionStringResolver.Resolve(configuration.GetConnectionString(PersistenceConnectionStrings.Sqlite)));
                return;

            case PersistenceProvider.PostgreSql:
                optionsBuilder.UseNpgsql(ResolvePostgreSqlConnectionString(configuration));
                return;

            default:
                throw new NotSupportedException($"Unsupported persistence provider '{persistenceProvider}'.");
        }
    }

    private static PersistenceProvider ResolveProvider(IConfiguration configuration)
    {
        var providerValue = configuration[$"{PersistenceOptions.SectionName}:{nameof(PersistenceOptions.Provider)}"];
        return Enum.TryParse(providerValue, ignoreCase: true, out PersistenceProvider provider)
            ? provider
            : PersistenceProvider.Sqlite;
    }

    private static string ResolvePostgreSqlConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(PersistenceConnectionStrings.PostgreSql);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            $"Connection string '{PersistenceConnectionStrings.PostgreSql}' is required when '{PersistenceOptions.SectionName}:{nameof(PersistenceOptions.Provider)}' is set to '{PersistenceProvider.PostgreSql}'.");
    }
}
