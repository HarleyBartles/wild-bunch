using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WildBunch.Persistence;

namespace WildBunch.Integration.Tests.TestInfrastructure;

public sealed class SqlitePersistenceFixture : IDisposable
{
    public SqlitePersistenceFixture()
    {
        Connection = new SqliteConnection("Data Source=:memory:");
        Connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public SqliteConnection Connection { get; }

    public DbContextOptions<WildBunchDbContext> CreateOptions()
        => new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseSqlite(Connection)
            .Options;

    public WildBunchDbContext CreateContext()
        => new(CreateOptions());

    public void Dispose()
    {
        Connection.Dispose();
    }
}
