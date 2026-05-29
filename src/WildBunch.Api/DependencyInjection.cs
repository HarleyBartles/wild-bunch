using WildBunch.Api.Games;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Queries;
using WildBunch.Domain.Actions;
using WildBunch.Domain.Travel;
using WildBunch.Persistence;
using WildBunch.GameContent;

namespace WildBunch.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddWildBunchServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddGameContent();
        services.AddSingleton<TravelResolver>();
        services.AddSingleton<ActionAvailabilityResolver>();
        services.AddScoped<StartNewGameHandler>();
        services.AddScoped<GetGameSessionHandler>();
        services.AddScoped<GetAvailableActionsHandler>();
        services.AddScoped<TravelToTownHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapWildBunchApi(this IEndpointRouteBuilder app)
    {
        app.MapGameEndpoints();
        return app;
    }
}
