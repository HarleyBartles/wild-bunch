using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence;

namespace WildBunch.Integration.Tests;

public sealed class StartupOperationEndpointTests
{
    [Fact]
    public async Task SetupRetryReturnsSameSessionAndChangedRequestConflicts()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var operationId = Guid.NewGuid();
        var request = new SetupGameRequest("Ranger Vale", SeedCode: "11111111-1111-4111-8111-111111111111");

        var firstResponse = await client.PostAsJsonWithStartupOperationAsync("/api/games/setup", request, operationId);
        var first = await firstResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(first);
        var prologueResponse = await client.PostAsync($"/api/games/{first!.Id}/prologue-viewed", content: null);
        prologueResponse.EnsureSuccessStatusCode();
        var eventCountBeforeRetry = await CountEventsAsync(factory, first.Id);
        var retryResponse = await client.PostAsJsonWithStartupOperationAsync("/api/games/setup", request, operationId);
        var changedResponse = await client.PostAsJsonWithStartupOperationAsync(
            "/api/games/setup",
            request with { PlayerName = "Different Name" },
            operationId);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, changedResponse.StatusCode);
        var retry = await retryResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(retry);
        Assert.Equal(first.Id, retry!.Id);
        Assert.Equal(StartFlowPhase.PrologueViewed, retry.StartFlowPhase);
        Assert.Equal(eventCountBeforeRetry, await CountEventsAsync(factory, first.Id));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        Assert.Equal(1, await db.GameSessions.CountAsync());
        var persistedOperation = await db.StartupOperations.AsNoTracking().SingleAsync();
        Assert.Equal(operationId, persistedOperation.OperationId);
        Assert.Equal(first.Id, persistedOperation.SessionId);
    }

    [Fact]
    public async Task ConcurrentExactSetupDeliveriesCreateOneSessionAndOneReceipt()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var operationId = Guid.NewGuid();
        var request = new SetupGameRequest("Concurrent Rider", SeedCode: "44444444-4444-4444-8444-444444444444");

        var responses = await Task.WhenAll(
            client.PostAsJsonWithStartupOperationAsync("/api/games/setup", request, operationId),
            client.PostAsJsonWithStartupOperationAsync("/api/games/setup", request, operationId));
        var responseBodies = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync()));
        var sessions = await Task.WhenAll(
            responses[0].Content.ReadFromJsonAsync<GameSessionDto>(),
            responses[1].Content.ReadFromJsonAsync<GameSessionDto>());

        Assert.All(Enumerable.Range(0, responses.Length), index =>
            Assert.True(
                responses[index].StatusCode == HttpStatusCode.Created,
                $"Expected both deliveries to resolve as Created, but got {(int)responses[index].StatusCode}: {responseBodies[index]}"));
        Assert.NotNull(sessions[0]);
        Assert.NotNull(sessions[1]);
        Assert.Equal(sessions[0]!.Id, sessions[1]!.Id);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        Assert.Equal(1, await db.GameSessions.CountAsync());
        Assert.Equal(1, await db.StartupOperations.CountAsync());
    }

    [Fact]
    public async Task NewSetupConflictsUntilTheActivePlaythroughIsArchived()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var firstResponse = await client.PostAsJsonWithStartupOperationAsync(
            "/api/games/setup",
            new SetupGameRequest("First Rider", SeedCode: "11111111-1111-4111-8111-111111111111"));
        Assert.True(firstResponse.IsSuccessStatusCode, await firstResponse.Content.ReadAsStringAsync());
        var first = await firstResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(first);

        var conflictingResponse = await client.PostAsJsonWithStartupOperationAsync(
            "/api/games/setup",
            new SetupGameRequest("Second Rider", SeedCode: "22222222-2222-4222-8222-222222222222"));
        Assert.Equal(HttpStatusCode.Conflict, conflictingResponse.StatusCode);
        Assert.Equal(1, await CountSessionsAsync(factory));
        var originalEvents = await CountEventsAsync(factory, first!.Id);

        var archiveResponse = await client.PostAsync($"/api/games/{first.Id}/archive", content: null);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        var replacementResponse = await client.PostAsJsonWithStartupOperationAsync(
            "/api/games/setup",
            new SetupGameRequest("Second Rider", SeedCode: "22222222-2222-4222-8222-222222222222"));

        Assert.Equal(HttpStatusCode.Created, replacementResponse.StatusCode);
        Assert.Equal(2, await CountSessionsAsync(factory));
        Assert.Equal(originalEvents + 1, await CountEventsAsync(factory, first.Id));
    }

    [Fact]
    public async Task FirstArrivalRetryIsExactAndNewOperationCannotRepeatArrival()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var setup = await client.PostAsJsonWithStartupOperationAsync(
            "/api/games/setup",
            new SetupGameRequest("Ranger Vale", SeedCode: "22222222-2222-4222-8222-222222222222"));
        setup.EnsureSuccessStatusCode();
        var session = await setup.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(session);
        var prologue = await client.PostAsync($"/api/games/{session!.Id}/prologue-viewed", content: null);
        prologue.EnsureSuccessStatusCode();

        var chosenTown = session.World.Towns[0].Id;
        var otherTown = session.World.Towns[1].Id;
        var arrivalOperationId = Guid.NewGuid();
        var startUri = $"/api/games/{session.Id}/start";
        var firstResponse = await client.PostAsJsonWithStartupOperationAsync(
            startUri,
            new StartGameWithTownRequest(chosenTown),
            arrivalOperationId);
        var eventCountAfterArrival = await CountEventsAsync(factory, session.Id);
        var retryResponse = await client.PostAsJsonWithStartupOperationAsync(
            startUri,
            new StartGameWithTownRequest(chosenTown),
            arrivalOperationId);
        var changedTownResponse = await client.PostAsJsonWithStartupOperationAsync(
            startUri,
            new StartGameWithTownRequest(otherTown),
            arrivalOperationId);
        var newOperationResponse = await client.PostAsJsonWithStartupOperationAsync(
            startUri,
            new StartGameWithTownRequest(chosenTown));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, changedTownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, newOperationResponse.StatusCode);
        var started = await retryResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(started);
        Assert.Equal(chosenTown, started!.Player.CurrentTownId);
        Assert.Equal(eventCountAfterArrival, await CountEventsAsync(factory, session.Id));
    }

    [Fact]
    public async Task SetupWithoutOperationIdIsRejectedBeforeCreatingSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/games/setup")
        {
            Content = JsonContent.Create(new SetupGameRequest("Ranger Vale", SeedCode: "33333333-3333-4333-8333-333333333333"))
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var malformedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/games/setup")
        {
            Content = JsonContent.Create(new SetupGameRequest("Ranger Vale", SeedCode: "33333333-3333-4333-8333-333333333333"))
        };
        malformedRequest.Headers.Add("Idempotency-Key", "not-a-guid");
        var malformedResponse = await client.SendAsync(malformedRequest);
        Assert.Equal(HttpStatusCode.BadRequest, malformedResponse.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        Assert.Equal(0, await db.GameSessions.CountAsync());
    }

    [Fact]
    public async Task FirstArrivalWithoutOperationIdIsRejectedBeforeMutatingSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var setup = await client.PostAsJsonWithStartupOperationAsync(
            "/api/games/setup",
            new SetupGameRequest("Ranger Vale", SeedCode: "11111111-1111-4111-8111-111111111111"));
        setup.EnsureSuccessStatusCode();
        var session = await setup.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(session);
        var prologue = await client.PostAsync($"/api/games/{session!.Id}/prologue-viewed", content: null);
        prologue.EnsureSuccessStatusCode();
        var eventCount = await CountEventsAsync(factory, session.Id);
        var startUri = $"/api/games/{session.Id}/start";

        using var missingKeyRequest = new HttpRequestMessage(HttpMethod.Post, startUri)
        {
            Content = JsonContent.Create(new StartGameWithTownRequest(session.World.Towns[0].Id))
        };
        var missingKeyResponse = await client.SendAsync(missingKeyRequest);

        using var malformedKeyRequest = new HttpRequestMessage(HttpMethod.Post, startUri)
        {
            Content = JsonContent.Create(new StartGameWithTownRequest(session.World.Towns[0].Id))
        };
        malformedKeyRequest.Headers.Add("Idempotency-Key", "not-a-guid");
        var malformedKeyResponse = await client.SendAsync(malformedKeyRequest);

        Assert.Equal(HttpStatusCode.BadRequest, missingKeyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malformedKeyResponse.StatusCode);
        Assert.Equal(eventCount, await CountEventsAsync(factory, session.Id));
    }

    private static async Task<int> CountEventsAsync(PostgreSqlApiFactory factory, Guid sessionId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        return await db.StoredEvents.CountAsync(storedEvent => storedEvent.StreamId == sessionId);
    }

    private static async Task<int> CountSessionsAsync(PostgreSqlApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        return await db.GameSessions.CountAsync();
    }
}
