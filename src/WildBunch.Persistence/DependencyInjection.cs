using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = SqliteConnectionStringResolver.Resolve(configuration.GetConnectionString("WildBunchDb"));

        services.AddSingleton<GameSessionJsonSerializer>();
        services.AddDbContext<WildBunchDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();

        return services;
    }

    public static IServiceProvider ApplyWildBunchMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        dbContext.Database.Migrate();
        return services;
    }
}
