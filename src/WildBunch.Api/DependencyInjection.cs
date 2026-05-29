using WildBunch.Api.Games;
using WildBunch.Api.Infrastructure;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Queries;
using WildBunch.Domain.Travel;

namespace WildBunch.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddWildBunchServices(this IServiceCollection services)
    {
        services.AddSingleton<IGameSessionRepository, InMemoryGameSessionRepository>();
        services.AddSingleton<INewGameFactory, SimpleNewGameFactory>();
        services.AddSingleton<TravelResolver>();
        services.AddScoped<StartNewGameHandler>();
        services.AddScoped<GetGameSessionHandler>();
        services.AddScoped<TravelToTownHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapWildBunchApi(this IEndpointRouteBuilder app)
    {
        app.MapGameEndpoints();
        return app;
    }
}
