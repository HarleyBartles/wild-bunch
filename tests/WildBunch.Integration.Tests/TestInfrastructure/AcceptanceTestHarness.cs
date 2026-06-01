using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;
using WildBunch.Persistence.GameSessions;

namespace WildBunch.Integration.Tests.TestInfrastructure;

internal static class AcceptanceTestHarness
{
    public static HttpClient CreateAuthenticatedClient(this PostgreSqlApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "acceptance-user");
        return client;
    }

    public static async Task<GameSessionDto> SeedCanonicalSessionAsync(
        this PostgreSqlApiFactory factory,
        string playerName = "Ranger Vale")
    {
        var session = new SeededNewGameFactory(new DeterministicTravelRandomnessSource())
            .Create(playerName, TravelDifficulty.Normal, ScenarioSeedCatalog.CanonicalMountedNormal.SeedCode);

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();

        return GameSessionMapper.ToDto(session);
    }

    public static async Task<GameSessionDto> LoadSessionAsync(this PostgreSqlApiFactory factory, Guid sessionId)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var session = await repository.GetByIdAsync(new GameSessionId(sessionId));

        if (session is null)
        {
            throw new InvalidOperationException($"Session '{sessionId}' was not found in the acceptance test store.");
        }

        return GameSessionMapper.ToDto(session);
    }
}
