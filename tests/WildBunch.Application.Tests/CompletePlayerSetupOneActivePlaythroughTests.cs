using WildBunch.Application.Games.Commands;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

/// <summary>
/// Application-level tests for the one-active-playthrough invariant enforced by
/// <see cref="CompletePlayerSetupHandler"/>. The invariant: before creating a new
/// setup-phase session, all pre-existing Active sessions are archived in the SAME
/// correlation id and SAME unit-of-work commit as the new session create.
/// See BUNCH-102.
/// </summary>
public sealed class CompletePlayerSetupOneActivePlaythroughTests
{
    [Fact]
    public async Task ArchivesExistingActiveSessionAndCreatesNewOneInSingleCommit()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var existingSession = CreateActiveSession("Ranger Vale");
        existingSession.MarkEventsCommitted();
        repository.Seed(existingSession);
        var handler = new CompletePlayerSetupHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new CompletePlayerSetupCommand
        {
            PlayerName = "Trail Hand",
            GameDifficulty = WildBunch.Domain.Travel.GameDifficulty.Standard,
            SeedCode = "00000000-0000-0000-0000-000000000000",
            GameEntropy = WildBunch.Domain.Travel.GameEntropy.Classic,
        });

        // The new session is Active.
        Assert.Equal(GameStatus.Active, result.Status);

        // The pre-existing session is now Archived.
        var archived = repository.Sessions.Single(s => s.Id == existingSession.Id);
        Assert.Equal(GameStatus.Archived, archived.Status);

        // The new session is also in the repository and Active.
        var newSession = repository.Sessions.Single(s => s.Id.Value == result.Id);
        Assert.Equal(GameStatus.Active, newSession.Status);

        // Single commit for the entire archive-old + create-new flow.
        Assert.Equal(1, repository.CommitCalls);

        // One StoreAsync for the archive + one for the create.
        Assert.Equal(2, repository.StoreCalls);

        // The archived session's event stream contains a PlaythroughArchived event
        // with the invariant-driven reason.
        var events = await repository.GetEventStreamAsync(existingSession.Id);
        var archivedEvent = events.OfType<PlaythroughArchived>().Single();
        Assert.Equal("superseded-by-new-playthrough", archivedEvent.ArchiveReason);
        Assert.Equal(GameStatus.Active, archivedEvent.StatusBeforeArchive);
    }

    [Fact]
    public async Task ArchivesAllActiveSessionsWhenMultipleExist()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var firstExisting = CreateActiveSession("Ranger Vale");
        firstExisting.MarkEventsCommitted();
        repository.Seed(firstExisting);
        var secondExisting = CreateActiveSession("Trail Hand");
        secondExisting.MarkEventsCommitted();
        repository.Seed(secondExisting);
        var handler = new CompletePlayerSetupHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new CompletePlayerSetupCommand
        {
            PlayerName = "Newcomer",
            GameDifficulty = WildBunch.Domain.Travel.GameDifficulty.Standard,
            SeedCode = "00000000-0000-0000-0000-000000000000",
            GameEntropy = WildBunch.Domain.Travel.GameEntropy.Classic,
        });

        // The new session is Active.
        Assert.Equal(GameStatus.Active, result.Status);

        // Both pre-existing sessions are now Archived.
        Assert.All(
            repository.Sessions.Where(s => s.Id != new GameSessionId(result.Id)),
            s => Assert.Equal(GameStatus.Archived, s.Status));

        // Exactly one Active session remains (the new one).
        var activeCount = repository.Sessions.Count(s => s.Status == GameStatus.Active);
        Assert.Equal(1, activeCount);

        // Single commit for the entire flow.
        Assert.Equal(1, repository.CommitCalls);

        // Two archives + one create = three StoreAsync calls.
        Assert.Equal(3, repository.StoreCalls);

        // Both archived sessions have a PlaythroughArchived event with the invariant reason.
        var firstEvents = await repository.GetEventStreamAsync(firstExisting.Id);
        var firstArchived = firstEvents.OfType<PlaythroughArchived>().Single();
        Assert.Equal("superseded-by-new-playthrough", firstArchived.ArchiveReason);

        var secondEvents = await repository.GetEventStreamAsync(secondExisting.Id);
        var secondArchived = secondEvents.OfType<PlaythroughArchived>().Single();
        Assert.Equal("superseded-by-new-playthrough", secondArchived.ArchiveReason);
    }

    [Fact]
    public async Task CreatesNewSessionWithNoArchiveEventsWhenNoActiveSessionExists()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new CompletePlayerSetupHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new CompletePlayerSetupCommand
        {
            PlayerName = "Ranger Vale",
            GameDifficulty = WildBunch.Domain.Travel.GameDifficulty.Standard,
            SeedCode = "00000000-0000-0000-0000-000000000000",
            GameEntropy = WildBunch.Domain.Travel.GameEntropy.Classic,
        });

        // The new session is Active.
        Assert.Equal(GameStatus.Active, result.Status);

        // Exactly one session in the repository (the new one).
        var session = repository.Sessions.Single();
        Assert.Equal(GameStatus.Active, session.Status);
        Assert.Equal(result.Id, session.Id.Value);

        // Single commit.
        Assert.Equal(1, repository.CommitCalls);

        // Only the create StoreAsync — no archives.
        Assert.Equal(1, repository.StoreCalls);

        // No PlaythroughArchived events anywhere in the event stream.
        var events = await repository.GetEventStreamAsync(session.Id);
        Assert.DoesNotContain(events, e => e is PlaythroughArchived);
    }

    /// <summary>
    /// Creates a minimal Active <see cref="GameSession"/> for seeding into the test repository.
    /// Each call produces a session with a fresh random <see cref="GameSessionId"/>.
    /// </summary>
    private static GameSession CreateActiveSession(string playerName)
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

        return GameSession.StartNew(playerName, world, caseFile, pinecross.Id);
    }
}
