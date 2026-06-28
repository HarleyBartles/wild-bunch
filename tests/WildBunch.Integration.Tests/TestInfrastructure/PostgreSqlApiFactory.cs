using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WildBunch.Api;
using WildBunch.Application.Abstractions;
using WildBunch.GameContent.Abstractions;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;

namespace WildBunch.Integration.Tests.TestInfrastructure;

public sealed class PostgreSqlApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly PostgreSqlTestDatabase _database;
    private bool _disposed;

    public PostgreSqlApiFactory()
    {
        _database = new PostgreSqlTestDatabase();

        using var context = new WildBunchDbContext(new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(_database.ConnectionString)
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
            services.RemoveAll<ISaltSourceFactory>();

            services.AddSingleton(_database);
            services.AddDbContext<WildBunchDbContext>((_, options) => options.UseNpgsql(_database.ConnectionString));
            services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();
            services.AddSingleton<ISaltSourceFactory, DeterministicSaltSourceFactory>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && !_disposed)
        {
            _disposed = true;
            _database.Dispose();
        }
    }
}
