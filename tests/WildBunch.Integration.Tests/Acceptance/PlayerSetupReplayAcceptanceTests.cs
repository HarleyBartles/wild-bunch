using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Api.Games;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;
using WildBunch.GameContent.NewGame;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence;

namespace WildBunch.Integration.Tests.Acceptance;

public sealed class PlayerSetupReplayAcceptanceTests
{
    [Fact]
    public async Task NormalPlayerSetup_PersistsGeneratedFactsThatProductionReplayRestores()
    {
        const string playerName = "Ranger Vale";
        const string recordedSalt = "player-genesis-known-salt";
        var seedCode = SeedWorldResolver.CreateCanonicalSeedCode().ToString("D");
        var saltFactory = new CountingSaltSourceFactory(recordedSalt);
        using var factory = PostgreSqlApiFactory.WithSaltSourceFactory(saltFactory);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/games/setup", new SetupGameRequest(
            playerName,
            GameDifficulty.Challenging,
            seedCode,
            GameEntropy.Classic));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var setupDto = await response.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(setupDto);
        Assert.Equal(GameDifficulty.Challenging, setupDto!.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, setupDto.GameEntropy);
        Assert.Equal(StartFlowPhase.SetupComplete, setupDto.StartFlowPhase);
        Assert.Equal(playerName, setupDto.Player.Name);
        Assert.Null(setupDto.Player.CurrentTownId);

        GameSession snapshotPath;
        IReadOnlyList<IDomainEvent> events;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            snapshotPath = (await repository.GetByIdAsync(new GameSessionId(setupDto.Id)))!;
            events = await repository.GetEventStreamAsync(new GameSessionId(setupDto.Id));
        }

        Assert.NotNull(snapshotPath);
        Assert.Collection(
            events,
            item => Assert.IsType<PlayerSetupCompleted>(item),
            item => Assert.IsType<WorldGenerated>(item),
            item => Assert.IsType<CaseFileGenerated>(item));

        var setup = Assert.IsType<PlayerSetupCompleted>(events[0]);
        var worldGenerated = Assert.IsType<WorldGenerated>(events[1]);
        var caseFileGenerated = Assert.IsType<CaseFileGenerated>(events[2]);
        Assert.Equal(playerName, setup.PlayerName);
        Assert.Equal(seedCode, setup.SeedCode);
        Assert.Equal(GameDifficulty.Challenging, setup.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, setup.GameEntropy);
        Assert.Equal(SaltSourceMode.Fixed, worldGenerated.SaltSource.Mode);
        Assert.Equal(recordedSalt, worldGenerated.SaltSource.Salt);
        Assert.Equal(GameEntropy.Classic, worldGenerated.GameEntropy);
        Assert.Equal(8, worldGenerated.World.Towns.Count);
        Assert.All(worldGenerated.World.Towns, town => Assert.NotNull(town.Layout));
        Assert.NotNull(worldGenerated.CaseFile);
        Assert.Equal(snapshotPath.CaseFile.TrueCulpritId.Value, caseFileGenerated.CaseFile.TrueCulpritId);
        Assert.Contains(caseFileGenerated.CaseFile.Suspects, suspect => suspect.Id == caseFileGenerated.CaseFile.TrueCulpritId);
        Assert.Equal(
            JsonSerializer.Serialize(worldGenerated.World),
            JsonSerializer.Serialize(WildBunch.Domain.World.WorldSnapshot.FromDomain(snapshotPath.World)));
        Assert.Equal(
            JsonSerializer.Serialize(caseFileGenerated.CaseFile),
            JsonSerializer.Serialize(WildBunch.Domain.Cases.CaseFileSnapshot.FromDomain(snapshotPath.CaseFile)));

        var sessionId = new GameSessionId(setupDto.Id);
        var eventPath = GameSession.RehydrateFromEvents(sessionId, worldGenerated.World.ToDomain(), events);
        Assert.Equal(StartFlowPhase.SetupComplete, eventPath.StartFlowPhase);
        Assert.Equal(playerName, eventPath.Player.Name);
        Assert.Equal(seedCode, eventPath.SeedCode);
        Assert.Equal(GameDifficulty.Challenging, eventPath.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, eventPath.GameEntropy);
        Assert.Equal(snapshotPath.Version, eventPath.Version);
        Assert.Equal(worldGenerated.SaltSource, eventPath.SaltSource);
        Assert.Equal(snapshotPath.CaseFile.TrueCulpritId, eventPath.CaseFile.TrueCulpritId);
        Assert.Equal(
            JsonSerializer.Serialize(WildBunch.Domain.World.WorldSnapshot.FromDomain(snapshotPath.World)),
            JsonSerializer.Serialize(WildBunch.Domain.World.WorldSnapshot.FromDomain(eventPath.World)));
        Assert.Equal(
            JsonSerializer.Serialize(WildBunch.Domain.Cases.CaseFileSnapshot.FromDomain(snapshotPath.CaseFile)),
            JsonSerializer.Serialize(WildBunch.Domain.Cases.CaseFileSnapshot.FromDomain(eventPath.CaseFile)));

        await using (var damageScope = factory.Services.CreateAsyncScope())
        {
            var dbContext = damageScope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
            var envelope = await dbContext.GameSessions.SingleAsync(session => session.Id == setupDto.Id);
            envelope.SnapshotVersion = envelope.StreamVersion - 1;
            await dbContext.SaveChangesAsync();
        }

        GameSession fullReplayPath;
        await using (var replayScope = factory.Services.CreateAsyncScope())
        {
            var repository = replayScope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            fullReplayPath = (await repository.GetByIdAsync(sessionId))!;
        }

        Assert.Equal(worldGenerated.SaltSource, fullReplayPath.SaltSource);
        Assert.Equal(snapshotPath.CaseFile.TrueCulpritId, fullReplayPath.CaseFile.TrueCulpritId);
        Assert.Equal(
            JsonSerializer.Serialize(WildBunch.Domain.World.WorldSnapshot.FromDomain(snapshotPath.World)),
            JsonSerializer.Serialize(WildBunch.Domain.World.WorldSnapshot.FromDomain(fullReplayPath.World)));
        Assert.Equal(
            JsonSerializer.Serialize(WildBunch.Domain.Cases.CaseFileSnapshot.FromDomain(snapshotPath.CaseFile)),
            JsonSerializer.Serialize(WildBunch.Domain.Cases.CaseFileSnapshot.FromDomain(fullReplayPath.CaseFile)));

        var revealedIdentifier = snapshotPath.CaseFile.TrueCulpritId.Value;
        snapshotPath.ViewPrologue(revealedIdentifier);
        eventPath.ViewPrologue(revealedIdentifier);
        fullReplayPath.ViewPrologue(revealedIdentifier);

        Assert.Equal(StartFlowPhase.PrologueViewed, snapshotPath.StartFlowPhase);
        Assert.Equal(snapshotPath.StartFlowPhase, eventPath.StartFlowPhase);
        Assert.Equal(snapshotPath.StartFlowPhase, fullReplayPath.StartFlowPhase);
        Assert.Equal(snapshotPath.UncommittedEvents, eventPath.UncommittedEvents);
        Assert.Equal(snapshotPath.UncommittedEvents, fullReplayPath.UncommittedEvents);
        Assert.Equal(revealedIdentifier, Assert.IsType<PrologueViewed>(eventPath.UncommittedEvents.Single()).RevealedSuspectIdentifier);
        Assert.Equal(1, saltFactory.Calls);
    }

    private sealed class CountingSaltSourceFactory(string salt) : ISaltSourceFactory
    {
        public int Calls { get; private set; }

        public SaltSource Create(string? setupSeedCode, GameDifficulty gameDifficulty)
        {
            Calls++;
            return SaltSource.CreateFixed(salt);
        }
    }
}
