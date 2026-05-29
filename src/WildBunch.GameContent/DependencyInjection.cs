using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent;

public static class DependencyInjection
{
    public static IServiceCollection AddGameContent(this IServiceCollection services)
    {
        services.AddSingleton<INewGameFactory, SeededNewGameFactory>();
        return services;
    }
}
