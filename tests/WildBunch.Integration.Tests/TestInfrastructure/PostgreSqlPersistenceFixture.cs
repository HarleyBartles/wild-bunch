using Microsoft.EntityFrameworkCore;
using WildBunch.Persistence;

namespace WildBunch.Integration.Tests.TestInfrastructure;

public sealed class PostgreSqlPersistenceFixture : IDisposable
{
    public PostgreSqlPersistenceFixture()
    {
        Database = new PostgreSqlTestDatabase();
        using var context = CreateContext();
        context.Database.Migrate();
    }

    public PostgreSqlTestDatabase Database { get; }

    public DbContextOptions<WildBunchDbContext> CreateOptions()
        => new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(Database.ConnectionString)
            .Options;

    public WildBunchDbContext CreateContext()
        => new(CreateOptions());

    public void Dispose()
    {
        Database.Dispose();
    }
}
