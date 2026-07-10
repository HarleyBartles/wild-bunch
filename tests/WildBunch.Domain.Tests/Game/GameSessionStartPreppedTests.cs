using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using DomainWorld = WildBunch.Domain.World.World;
using Xunit;

namespace WildBunch.Domain.Tests.Game;

public sealed class GameSessionStartPreppedTests
{
    [Fact]
    public void StartPrepped_CreatesMinimalSessionWithPreppedStatus()
    {
        var session = GameSession.StartPrepped("test-seed", GameDifficulty.Standard, GameEntropy.Classic);
        
        Assert.NotNull(session);
        Assert.Equal(GameStatus.Prepped, session.Status);
        Assert.Equal("test-seed", session.SeedCode);
        Assert.Equal(GameDifficulty.Standard, session.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, session.GameEntropy);
        Assert.Null(session.World);
        Assert.Null(session.CaseFile);
    }

    [Fact]
    public void StartFromPrepped_WithPreppedSession_TransitionsToActive()
    {
        var session = GameSession.StartPrepped("test-seed", GameDifficulty.Standard, GameEntropy.Classic);
        var world = new DomainWorld(Array.Empty<WildBunch.Domain.World.Town>(), Array.Empty<WildBunch.Domain.World.Trail>());
        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("test"), CaseOpeningLead.Create("test"), Array.Empty<Clue>());
        var saltSource = SaltSource.CreateRuntime();

        session.StartFromPrepped(world, caseFile, "test-seed", saltSource);

        Assert.Equal(GameStatus.Active, session.Status);
        Assert.NotNull(session.World);
        Assert.NotNull(session.CaseFile);
        Assert.Equal("test-seed", session.SeedCode);
    }

    [Fact]
    public void StartFromPrepped_WithActiveSession_ThrowsInvalidOperationException()
    {
        var session = GameSession.StartSetup(
            "Test Player",
            new DomainWorld(Array.Empty<WildBunch.Domain.World.Town>(), Array.Empty<WildBunch.Domain.World.Trail>()),
            new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("test"), CaseOpeningLead.Create("test"), Array.Empty<Clue>()),
            GameDifficulty.Standard,
            GameEntropy.Classic,
            "test-seed",
            SaltSource.CreateRuntime());
        var world = new DomainWorld(Array.Empty<WildBunch.Domain.World.Town>(), Array.Empty<WildBunch.Domain.World.Trail>());
        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("test"), CaseOpeningLead.Create("test"), Array.Empty<Clue>());
        var saltSource = SaltSource.CreateRuntime();

        var exception = Assert.Throws<InvalidOperationException>(
            () => session.StartFromPrepped(world, caseFile, "test-seed", saltSource));
        Assert.Contains("Prepped status", exception.Message);
    }

    [Fact]
    public void StartFromPrepped_WithCompletedSession_ThrowsInvalidOperationException()
    {
        var session = GameSession.StartSetup(
            "Test Player",
            new DomainWorld(Array.Empty<WildBunch.Domain.World.Town>(), Array.Empty<WildBunch.Domain.World.Trail>()),
            new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("test"), CaseOpeningLead.Create("test"), Array.Empty<Clue>()),
            GameDifficulty.Standard,
            GameEntropy.Classic,
            "test-seed",
            SaltSource.CreateRuntime());
        session.ArchivePlaythrough("test", DateTime.UtcNow);
        var world = new DomainWorld(Array.Empty<WildBunch.Domain.World.Town>(), Array.Empty<WildBunch.Domain.World.Trail>());
        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("test"), CaseOpeningLead.Create("test"), Array.Empty<Clue>());
        var saltSource = SaltSource.CreateRuntime();

        var exception = Assert.Throws<InvalidOperationException>(
            () => session.StartFromPrepped(world, caseFile, "test-seed", saltSource));
        Assert.Contains("Prepped status", exception.Message);
    }
}
