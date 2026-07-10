using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using DomainWorld = WildBunch.Domain.World.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.Application.Tests.Dev;

public sealed class SetTownLayoutSaltsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithPreppedSession_SetsDevLayoutSalts()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new SetTownLayoutSaltsHandler(repository, repository);

        // Create a prepped session
        var session = GameSession.StartPrepped(
            SeedWorldResolver.CreateCanonicalSeedCode().ToString(),
            GameDifficulty.Standard,
            GameEntropy.Classic);
        await repository.StoreAsync(session, Guid.NewGuid(), CancellationToken.None);
        await repository.CommitAsync(CancellationToken.None);
        session.MarkEventsCommitted();

        // Set dev layout salts
        var command = new SetTownLayoutSaltsCommand(
            session.Id.Value,
            "dev-buildings",
            "dev-roads",
            "dev-dirt",
            "dev-props");
        await handler.HandleAsync(command, CancellationToken.None);

        // Verify the salts were set
        var updated = await repository.GetByIdAsync(session.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.NotNull(updated.DevLayoutSalts);
        Assert.Equal("dev-buildings", updated.DevLayoutSalts.BuildingsSalt);
        Assert.Equal("dev-roads", updated.DevLayoutSalts.RoadsSalt);
        Assert.Equal("dev-dirt", updated.DevLayoutSalts.DirtSalt);
        Assert.Equal("dev-props", updated.DevLayoutSalts.PropsSalt);
    }

    [Fact]
    public async Task HandleAsync_WithActiveSession_ThrowsInvalidOperationException()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new SetTownLayoutSaltsHandler(repository, repository);

        // Create an active session
        var session = GameSession.StartSetup(
            "Test Player",
            new DomainWorld(Array.Empty<WildBunch.Domain.World.Town>(), Array.Empty<WildBunch.Domain.World.Trail>()),
            new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("test"), CaseOpeningLead.Create("test"), Array.Empty<Clue>()),
            GameDifficulty.Standard,
            GameEntropy.Classic,
            SeedWorldResolver.CreateCanonicalSeedCode().ToString(),
            SaltSource.CreateRuntime());
        await repository.StoreAsync(session, Guid.NewGuid(), CancellationToken.None);
        await repository.CommitAsync(CancellationToken.None);
        session.MarkEventsCommitted();

        // Try to set dev layout salts on active session
        var command = new SetTownLayoutSaltsCommand(
            session.Id.Value,
            "dev-buildings",
            "dev-roads",
            "dev-dirt",
            "dev-props");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command, CancellationToken.None));
        Assert.Contains("Prepped status", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_WithCompletedSession_ThrowsInvalidOperationException()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new SetTownLayoutSaltsHandler(repository, repository);

        // Create a completed session
        var session = GameSession.StartSetup(
            "Test Player",
            new DomainWorld(Array.Empty<WildBunch.Domain.World.Town>(), Array.Empty<WildBunch.Domain.World.Trail>()),
            new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("test"), CaseOpeningLead.Create("test"), Array.Empty<Clue>()),
            GameDifficulty.Standard,
            GameEntropy.Classic,
            SeedWorldResolver.CreateCanonicalSeedCode().ToString(),
            SaltSource.CreateRuntime());
        session.ArchivePlaythrough("test", DateTime.UtcNow);
        await repository.StoreAsync(session, Guid.NewGuid(), CancellationToken.None);
        await repository.CommitAsync(CancellationToken.None);
        session.MarkEventsCommitted();

        // Try to set dev layout salts on completed session
        var command = new SetTownLayoutSaltsCommand(
            session.Id.Value,
            "dev-buildings",
            "dev-roads",
            "dev-dirt",
            "dev-props");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command, CancellationToken.None));
        Assert.Contains("Prepped status", exception.Message);
    }
}
