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
        var connectionString = configuration.GetConnectionString("WildBunchDb") ?? "Data Source=wildbunch.db";

        services.AddSingleton<GameSessionJsonSerializer>();
        services.AddDbContext<WildBunchDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();

        return services;
    }

    public static IServiceProvider EnsureWildBunchDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        dbContext.Database.EnsureCreated();
        return services;
    }
}
