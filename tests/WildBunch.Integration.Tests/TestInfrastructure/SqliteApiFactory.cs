using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WildBunch.Api;
using WildBunch.Application.Abstractions;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;

namespace WildBunch.Integration.Tests.TestInfrastructure;

public sealed class SqliteApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public SqliteApiFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var context = new WildBunchDbContext(new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseSqlite(_connection)
            .Options);

        context.Database.Migrate();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<WildBunchDbContext>>();
            services.RemoveAll<WildBunchDbContext>();
            services.RemoveAll<IGameSessionRepository>();

            services.AddSingleton(_connection);
            services.AddDbContext<WildBunchDbContext>((_, options) => options.UseSqlite(_connection));
            services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && !_disposed)
        {
            _disposed = true;
            _connection.Dispose();
        }
    }
}
