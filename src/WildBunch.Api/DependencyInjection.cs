using WildBunch.Api.Games;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Queries;
using WildBunch.Domain.Actions;
using WildBunch.Domain.Journal;
using WildBunch.Domain.WantedPosters;
using WildBunch.Domain.Travel;
using WildBunch.Persistence;
using WildBunch.GameContent;

namespace WildBunch.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddWildBunchServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("ViteDevClient", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        services.AddPersistence(configuration);
        services.AddGameContent();
        services.AddSingleton<TravelResolver>();
        services.AddSingleton<ActionAvailabilityResolver>();
        services.AddSingleton<JournalResolver>();
        services.AddSingleton<ReadWantedPostersResolver>();
        services.AddScoped<StartNewGameHandler>();
        services.AddScoped<GetGameSessionHandler>();
        services.AddScoped<GetAvailableActionsHandler>();
        services.AddScoped<GetJournalHandler>();
        services.AddScoped<ReadWantedPostersHandler>();
        services.AddScoped<TravelToTownHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapWildBunchApi(this IEndpointRouteBuilder app)
    {
        app.MapGameEndpoints();
        return app;
    }
}
