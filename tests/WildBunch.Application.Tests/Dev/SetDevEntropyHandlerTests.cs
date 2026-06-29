using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using Town = WildBunch.Domain.World.Town;
using TownId = WildBunch.Domain.World.TownId;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;
using World = WildBunch.Domain.World.World;

namespace WildBunch.Application.Tests.Dev;

public sealed class SetDevEntropyHandlerTests
{
    [Fact]
    public async Task HandleAsync_SetsEntropyAndPersists()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession(GameEntropy.Classic);
        repository.Seed(session);

        var handler = new SetDevEntropyHandler(repository, repository);

        await handler.HandleAsync(new SetDevEntropyCommand(session.Id.Value, GameEntropy.Wild));

        var reloaded = await repository.GetByIdAsync(new(session.Id.Value));
        Assert.NotNull(reloaded);
        Assert.Equal(GameEntropy.Wild, reloaded!.GameEntropy);
    }

    [Fact]
    public async Task HandleAsync_DoesNotChangeDifficultyOrSalt()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession(GameEntropy.Classic);
        var difficultyBefore = session.GameDifficulty;
        var saltBefore = session.SaltSource;
        repository.Seed(session);

        var handler = new SetDevEntropyHandler(repository, repository);

        await handler.HandleAsync(new SetDevEntropyCommand(session.Id.Value, GameEntropy.Adventurous));

        var reloaded = await repository.GetByIdAsync(new(session.Id.Value));
        Assert.Equal(difficultyBefore, reloaded!.GameDifficulty);
        Assert.Equal(saltBefore.Mode, reloaded.SaltSource.Mode);
    }

    private static GameSession CreateSeededSession(GameEntropy entropy)
    {
        var town = new Town(new TownId("current"), "Current Town", TownServices.None);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new World(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null, suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: null, GameDifficulty.Standard,
            SaltSource.CreateFixed(string.Empty), entropy);
        session.MarkEventsCommitted();
        return session;
    }
}
