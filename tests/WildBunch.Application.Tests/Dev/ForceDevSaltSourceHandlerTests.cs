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

public sealed class ForceDevSaltSourceHandlerTests
{
    [Fact]
    public async Task HandleAsync_LocksRngToFixedSalt_WhenSaltProvided()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession();
        repository.Seed(session);

        var handler = new ForceDevSaltSourceHandler(repository, repository);
        await handler.HandleAsync(new ForceDevSaltSourceCommand(session.Id.Value, Salt: "deadbeef"));

        Assert.Equal(1, repository.StoreCalls);
        var reloaded = await repository.GetByIdAsync(new(session.Id.Value));
        Assert.Equal(SaltSourceMode.Fixed, reloaded!.SaltSource.Mode);
        Assert.Equal("deadbeef", reloaded.SaltSource.Salt);
    }

    [Fact]
    public async Task HandleAsync_LocksRngWithGeneratedSalt_WhenSaltIsNull()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession();
        repository.Seed(session);

        var handler = new ForceDevSaltSourceHandler(repository, repository);
        await handler.HandleAsync(new ForceDevSaltSourceCommand(session.Id.Value, Salt: null));

        var reloaded = await repository.GetByIdAsync(new(session.Id.Value));
        Assert.Equal(SaltSourceMode.Fixed, reloaded!.SaltSource.Mode);
        Assert.False(string.IsNullOrEmpty(reloaded.SaltSource.Salt));
        // Generated salt is 32-char hex (16 bytes → 32 hex chars)
        Assert.Equal(32, reloaded.SaltSource.Salt.Length);
    }

    [Fact]
    public async Task HandleAsync_LocksRngWithGeneratedSalt_WhenSaltIsEmptyString()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession();
        repository.Seed(session);

        var handler = new ForceDevSaltSourceHandler(repository, repository);
        await handler.HandleAsync(new ForceDevSaltSourceCommand(session.Id.Value, Salt: ""));

        var reloaded = await repository.GetByIdAsync(new(session.Id.Value));
        Assert.Equal(SaltSourceMode.Fixed, reloaded!.SaltSource.Mode);
        Assert.False(string.IsNullOrEmpty(reloaded.SaltSource.Salt));
        Assert.Equal(32, reloaded.SaltSource.Salt.Length);
    }

    [Fact]
    public async Task HandleAsync_LocksRngWithGeneratedSalt_WhenSaltIsWhitespace()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession();
        repository.Seed(session);

        var handler = new ForceDevSaltSourceHandler(repository, repository);
        await handler.HandleAsync(new ForceDevSaltSourceCommand(session.Id.Value, Salt: "   "));

        var reloaded = await repository.GetByIdAsync(new(session.Id.Value));
        Assert.Equal(SaltSourceMode.Fixed, reloaded!.SaltSource.Mode);
        Assert.False(string.IsNullOrEmpty(reloaded.SaltSource.Salt));
        // Generated salt is hex, never whitespace
        Assert.DoesNotContain(" ", reloaded.SaltSource.Salt);
        Assert.Equal(32, reloaded.SaltSource.Salt.Length);
    }

    [Fact]
    public async Task HandleAsync_UsesExactSalt_WhenNonEmptyProvided()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession();
        repository.Seed(session);

        var handler = new ForceDevSaltSourceHandler(repository, repository);
        await handler.HandleAsync(new ForceDevSaltSourceCommand(session.Id.Value, Salt: "my-custom-salt"));

        var reloaded = await repository.GetByIdAsync(new(session.Id.Value));
        Assert.Equal(SaltSourceMode.Fixed, reloaded!.SaltSource.Mode);
        Assert.Equal("my-custom-salt", reloaded.SaltSource.Salt);
    }

    [Fact]
    public async Task HandleAsync_TrimsSurroundingWhitespace_FromNonEmptySalt()
    {
        // Salt contract: non-empty string after trimming → use the trimmed value verbatim.
        // This test proves the trim is deliberate, not accidental drift.
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession();
        repository.Seed(session);

        var handler = new ForceDevSaltSourceHandler(repository, repository);
        await handler.HandleAsync(new ForceDevSaltSourceCommand(session.Id.Value, Salt: "  deadbeef  "));

        var reloaded = await repository.GetByIdAsync(new(session.Id.Value));
        Assert.Equal(SaltSourceMode.Fixed, reloaded!.SaltSource.Mode);
        Assert.Equal("deadbeef", reloaded.SaltSource.Salt);
    }

    private static GameSession CreateSeededSession()
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

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Easy, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed(string.Empty));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(town.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory: null);
        session.MarkEventsCommitted();
        return session;
    }
}
