using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

public sealed class GetJournalHandlerTests
{
    [Fact]
    public async Task GetJournalLoadsSessionAndReturnsExpectedData()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new GetJournalHandler(repository, new JournalResolver());

        var result = await handler.HandleAsync(new GetJournalQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.Id);
        Assert.Equal(session.Status, result.Status);
        Assert.Equal(session.Clock.Day, result.Clock.Day);
        Assert.Equal(session.Clock.Turn, result.Clock.Turn);
        Assert.Equal(session.Player.CurrentTownId.Value, result.CurrentTown.Id);
        Assert.Equal("Pinecross", result.CurrentTown.Name);
        Assert.Equal(session.CaseFile.Accusation?.Value, result.CaseFile.AccusationId);
        Assert.Equal("Find the culprit before the law closes in.", result.CaseFile.CaseSummary);
        Assert.Equal(session.CaseFile.Suspects.Count, result.CaseFile.Suspects.Count);
        Assert.Equal(session.CaseFile.KnownClues.Count, result.CaseFile.KnownClues.Count);
        Assert.Equal(session.LogEntries.Count, result.LogEntries.Count);
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.TrueCulpritId);
    }

    [Fact]
    public async Task GetJournalThrowsWhenMissing()
    {
        var handler = new GetJournalHandler(new InMemoryGameSessionRepository(), new JournalResolver());

        var exception = await Assert.ThrowsAsync<GameSessionNotFoundException>(
            () => handler.HandleAsync(new GetJournalQuery(Guid.NewGuid())));

        Assert.Contains("was not found", exception.Message);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, SupplyCost: 2, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Jonah Pike", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", new SuspectTraits(IsLocal: false, IsArmed: false, IsDesperate: false), SuspectStatus.AtLarge)
        };

        var clues = new[]
        {
            new Clue(new ClueId("clue-1"), ClueKind.Witness, "A rider was seen leaving at dusk."),
            new Clue(new ClueId("clue-2"), ClueKind.Record, "A coded telegraph entry was logged.")
        };

        var caseFile = new CaseFile(
            accusation: new SuspectId("suspect-2"),
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            knownClues: clues);

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
    }
}
