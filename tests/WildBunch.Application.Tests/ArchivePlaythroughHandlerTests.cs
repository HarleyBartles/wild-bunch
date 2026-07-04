using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

public sealed class ArchivePlaythroughHandlerTests
{
    [Fact]
    public async Task ArchivePlaythroughArchivesStoresCommitsAndReturnsResultDto()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new ArchivePlaythroughHandler(repository, repository);

        var result = await handler.HandleAsync(
            new ArchivePlaythroughCommand(session.Id, "start-over"));

        Assert.Equal(session.Id.Value, result.SessionId);
        Assert.Equal(GameStatus.Archived, result.Status);
        Assert.Equal("Ranger Vale", result.PlayerName);
        Assert.Equal("pinecross", result.LastTownId);
        Assert.Equal("Pinecross", result.LastTownName);
        Assert.Equal(1, result.Day);
        Assert.Equal("0", result.Turn);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal(GameStatus.Archived, repository.Sessions.Single().Status);
    }

    [Fact]
    public async Task ArchivePlaythroughThrowsGameSessionNotFoundExceptionWhenSessionMissing()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new ArchivePlaythroughHandler(repository, repository);

        await Assert.ThrowsAsync<GameSessionNotFoundException>(() =>
            handler.HandleAsync(new ArchivePlaythroughCommand(new GameSessionId(Guid.NewGuid()), "start-over")));

        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart();
        return session;
    }
}
