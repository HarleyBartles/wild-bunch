using System.Text.Json;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.WantedPosters;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

public sealed class ReadWantedPostersHandlerTests
{
    [Fact]
    public async Task ReadWantedPostersLoadsSessionSavesSuccessfulMutationAndReturnsExpectedResult()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.NoticeBoard);
        repository.Seed(session);
        var handler = new ReadWantedPostersHandler(repository, new ReadWantedPostersResolver(), new JournalResolver());

        var result = await handler.HandleAsync(new ReadWantedPostersCommand(session.Id.Value));

        Assert.True(result.Success);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(1, result.CurrentJournal.Clock.Turn);
        Assert.Equal(2, result.CurrentJournal.LogEntries.Count);
        Assert.Single(result.CurrentJournal.CaseFile.KnownClues);
        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadWantedPostersReturnsFailureWithoutSavingWhenActionUnavailable()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.None);
        repository.Seed(session);
        var handler = new ReadWantedPostersHandler(repository, new ReadWantedPostersResolver(), new JournalResolver());

        var result = await handler.HandleAsync(new ReadWantedPostersCommand(session.Id.Value));

        Assert.False(result.Success);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Empty(result.CurrentJournal.CaseFile.KnownClues);
        Assert.Single(result.CurrentJournal.LogEntries);
        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadWantedPostersThrowsWhenMissing()
    {
        var handler = new ReadWantedPostersHandler(
            new InMemoryGameSessionRepository(),
            new ReadWantedPostersResolver(),
            new JournalResolver());

        await Assert.ThrowsAsync<WildBunch.Application.Games.Exceptions.GameSessionNotFoundException>(
            () => handler.HandleAsync(new ReadWantedPostersCommand(Guid.NewGuid())));
    }

    private static GameSession CreateSession(TownServices currentTownServices)
    {
        var currentTown = new Town(new TownId("current"), "Current Town", currentTownServices);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[]
            {
                new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, SupplyCost: 2, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", new SuspectTraits(IsLocal: false, IsArmed: false, IsDesperate: false), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(new ClueId("clue-public-1"), ClueKind.Witness, "A posted notice describes a rider wearing a faded blue scarf.")
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }
}
