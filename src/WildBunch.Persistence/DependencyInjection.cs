using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Projections;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<GameSessionJsonSerializer>();
        services.AddSingleton<TravelDiaryDayProjector>();
        services.AddDbContext<WildBunchDbContext>((_, options) => PersistenceDbContextOptions.Configure(options, configuration));
        services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();
        services.AddScoped<IGameSessionUnitOfWork, EfGameSessionUnitOfWork>();
        services.AddScoped<IGameSessionReadRepository, EfGameSessionReadRepository>();
        services.AddScoped<IGameJournalReadRepository, EfGameJournalReadRepository>();

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
