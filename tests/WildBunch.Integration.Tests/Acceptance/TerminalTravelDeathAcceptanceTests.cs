using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Api.Games;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Actions;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Acceptance;

public sealed class TerminalTravelDeathAcceptanceTests
{
    [Fact]
    public async Task LethalStarvationIsPersistedThenRejectedAdvancesAndArchivesAsFailed()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        var session = CreateLethalTravelSession();
        var sessionId = session.Id.Value;
        await StoreAsync(factory, session);

        var initialEvents = await ReadEventsAsync(factory, session.Id);
        Assert.Equal(GameStatus.Active, session.Status);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Equal(10, session.Player.Health);
        Assert.Equal(0, session.Player.GetQuantity(ItemKind.Food));

        var lethalAdvance = await client.PostAsync($"/api/games/{sessionId}/travel/advance", content: null);

        Assert.True(lethalAdvance.StatusCode == HttpStatusCode.OK, await lethalAdvance.Content.ReadAsStringAsync());
        var lethalResult = await lethalAdvance.Content.ReadFromJsonAsync<GameTurnResultDto>();
        Assert.NotNull(lethalResult);
        Assert.False(lethalResult!.Success);
        Assert.Equal(GameStatus.Failed, lethalResult.CurrentSession.Status);
        Assert.Equal(JourneyStatus.Failed, lethalResult.JourneyStatus);
        Assert.Equal(0, lethalResult.CurrentSession.Player.Health);

        var persistedGame = await ReadGameAsync(client, sessionId);
        var persistedJournal = await ReadJournalAsync(client, sessionId);
        var persistedActions = await ReadActionsAsync(client, sessionId);
        var eventsAfterDeath = await ReadEventsAsync(factory, session.Id);
        Assert.Equal(initialEvents.Count + 2, eventsAfterDeath.Count);
        Assert.IsType<TravelDayAdvanced>(eventsAfterDeath[^2]);
        Assert.IsType<PlayerDied>(eventsAfterDeath[^1]);
        var lethalDay = Assert.Single(eventsAfterDeath.OfType<TravelDayAdvanced>());
        Assert.Equal(-10, lethalDay.HealthDelta);
        Assert.Single(eventsAfterDeath.OfType<PlayerDied>());
        Assert.Equal(GameStatus.Failed, persistedGame.Status);
        Assert.Equal(0, persistedGame.Player.Health);
        Assert.Equal(JourneyStatus.Failed, persistedGame.Journey!.Status);
        Assert.Equal(GameStatus.Failed, persistedJournal.Status);
        Assert.Single(persistedJournal.LogEntries, entry => entry.Message == "I died on the trail. My journey ends here.");
        Assert.Equal(new[] { AvailableActionKind.ViewMap, AvailableActionKind.ViewJournal }, persistedActions.Select(action => action.Kind));
        Assert.DoesNotContain(eventsAfterDeath, domainEvent => domainEvent is JourneyCompleted or JourneyArrivalAcknowledged);

        var repeatedAdvance = await client.PostAsync($"/api/games/{sessionId}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, repeatedAdvance.StatusCode);
        var rejectedResult = await repeatedAdvance.Content.ReadFromJsonAsync<GameTurnResultDto>();
        Assert.NotNull(rejectedResult);
        Assert.False(rejectedResult!.Success);
        Assert.Equal(GameStatus.Failed, rejectedResult.CurrentSession.Status);
        Assert.Equal(eventsAfterDeath.Count, (await ReadEventsAsync(factory, session.Id)).Count);
        Assert.Equal(GameStatus.Failed, (await ReadGameAsync(client, sessionId)).Status);
        Assert.Single((await ReadJournalAsync(client, sessionId)).LogEntries, entry => entry.Message == "I died on the trail. My journey ends here.");

        var archiveResponse = await client.PostAsync($"/api/games/{sessionId}/archive", content: null);

        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        var archiveResult = await archiveResponse.Content.ReadFromJsonAsync<ArchivePlaythroughResultDto>();
        Assert.NotNull(archiveResult);
        Assert.Equal(GameStatus.Archived, archiveResult!.Status);
        Assert.Equal(GameStatus.Archived, (await ReadGameAsync(client, sessionId)).Status);

        var finalEvents = await ReadEventsAsync(factory, session.Id);
        Assert.Equal(eventsAfterDeath.Count + 1, finalEvents.Count);
        Assert.Equal(GameStatus.Failed, Assert.Single(finalEvents.OfType<PlaythroughArchived>()).StatusBeforeArchive);
        Assert.Single(finalEvents.OfType<PlayerDied>());
    }

    private static GameSession CreateLethalTravelSession()
    {
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        var original = scenario.CreateSession("Ranger Vale");
        var setup = GameSession.StartSetup(
            original.Player.Name,
            original.World,
            original.CaseFile,
            original.GameDifficulty,
            original.GameEntropy,
            scenario.SeedCode,
            original.SaltSource);
        setup.ViewPrologue("terminal-starvation-acceptance");
        setup.SelectStartingTown(original.Player.CurrentTownId!.Value);
        setup.CompleteGameStart(original.Player.Wallet, original.Player.Inventory);

        var setupEvents = setup.UncommittedEvents
            .Select(domainEvent => domainEvent is GameStarted started
                ? started with
                {
                    StartingHealth = 10,
                    StartingInventoryItems = started.StartingInventoryItems
                        .Where(item => item.Kind != ItemKind.Food)
                        .ToArray()
                }
                : domainEvent)
            .ToArray();
        var session = GameSession.RehydrateFromEvents(setup.Id, setup.World, setupEvents);
        session.SetCommittedEvents(setupEvents);
        var destinationTownId = session.World.Trails
            .Where(trail => trail.FromTownId == session.Player.CurrentTownId || trail.ToTownId == session.Player.CurrentTownId)
            .Select(trail => trail.FromTownId == session.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
            .First();
        var preview = new TravelResolver().PreviewJourney(
            session.World,
            session.Player.CurrentTownId!.Value,
            destinationTownId,
            session.Player.Inventory,
            session.TravelRules);

        Assert.True(preview.Success);
        Assert.NotNull(preview.Preview);
        Assert.True(session.StartJourney(preview.Preview!).Success);
        return session;
    }

    private static async Task StoreAsync(PostgreSqlApiFactory factory, GameSession session)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();
    }

    private static async Task<IReadOnlyList<IDomainEvent>> ReadEventsAsync(PostgreSqlApiFactory factory, GameSessionId sessionId)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        return await repository.GetEventStreamAsync(sessionId);
    }

    private static async Task<GameSessionDto> ReadGameAsync(HttpClient client, Guid sessionId)
    {
        var response = await client.GetAsync($"/api/games/{sessionId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<GameSessionDto>())!;
    }

    private static async Task<JournalDto> ReadJournalAsync(HttpClient client, Guid sessionId)
    {
        var response = await client.GetAsync($"/api/games/{sessionId}/journal");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JournalDto>())!;
    }

    private static async Task<IReadOnlyList<AvailableActionDto>> ReadActionsAsync(HttpClient client, Guid sessionId)
    {
        var response = await client.GetAsync($"/api/games/{sessionId}/actions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AvailableActionDto[]>())!;
    }
}
