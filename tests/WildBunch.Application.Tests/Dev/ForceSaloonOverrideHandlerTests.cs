using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using Town = WildBunch.Domain.World.Town;
using TownId = WildBunch.Domain.World.TownId;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;
using World = WildBunch.Domain.World.World;

namespace WildBunch.Application.Tests.Dev;

public sealed class ForceSaloonOverrideHandlerTests
{
    [Fact]
    public async Task HandleAsync_ForcesSuspectOverride_AndPersists()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        repository.Seed(session);

        var handler = new ForceSaloonOverrideHandler(repository, repository);

        await handler.HandleAsync(new ForceSaloonOverrideCommand(
            session.Id.Value,
            ForcedKind: "Suspect",
            ForcedSuspectId: "suspect-1"));

        Assert.Equal(1, repository.StoreCalls);
        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded!.PendingDevSaloonOverride);
        Assert.Equal(DevSaloonPoiKind.Suspect, reloaded.PendingDevSaloonOverride!.ForcedKind);
        Assert.Equal(new SuspectId("suspect-1"), reloaded.PendingDevSaloonOverride.ForcedSuspectId);
    }

    [Fact]
    public async Task HandleAsync_ForcesCitizenOverride_AndPersists()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        repository.Seed(session);

        var handler = new ForceSaloonOverrideHandler(repository, repository);

        await handler.HandleAsync(new ForceSaloonOverrideCommand(
            session.Id.Value,
            ForcedKind: "Citizen",
            ForcedSuspectId: null));

        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded!.PendingDevSaloonOverride);
        Assert.Equal(DevSaloonPoiKind.Citizen, reloaded.PendingDevSaloonOverride!.ForcedKind);
        Assert.Null(reloaded.PendingDevSaloonOverride.ForcedSuspectId);
    }

    [Fact]
    public async Task HandleAsync_RejectsTrueCulprit_WhenKillerReleaseGateIsLocked()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        repository.Seed(session);

        // Verify gate is locked
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);

        var handler = new ForceSaloonOverrideHandler(repository, repository);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new ForceSaloonOverrideCommand(
                session.Id.Value,
                ForcedKind: "Suspect",
                ForcedSuspectId: "suspect-2")));

        // Gate-aware rejection, not "must never appear"
        Assert.Contains("killer trail is locked", ex.Message.ToLowerInvariant());
        Assert.DoesNotContain("must never appear", ex.Message.ToLowerInvariant());
        Assert.Equal(0, repository.StoreCalls);
    }

    [Fact]
    public async Task HandleAsync_AcceptsTrueCulprit_WhenKillerReleaseGateIsOpen()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect(killerReleaseProgress: 2);
        repository.Seed(session);

        // Verify gate is open
        Assert.True(session.CaseFile.KillerReleaseState.IsReleased);

        var handler = new ForceSaloonOverrideHandler(repository, repository);

        await handler.HandleAsync(new ForceSaloonOverrideCommand(
            session.Id.Value,
            ForcedKind: "Suspect",
            ForcedSuspectId: "suspect-2"));

        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded!.PendingDevSaloonOverride);
        Assert.Equal(DevSaloonPoiKind.Suspect, reloaded.PendingDevSaloonOverride!.ForcedKind);
        Assert.Equal("suspect-2", reloaded.PendingDevSaloonOverride.ForcedSuspectId?.Value);
    }

    private static GameSession CreateSessionWithSaloonSuspect(int killerReleaseProgress = 0)
    {
        // Use the domain test factory which creates a session with a confrontable saloon suspect.
        // The Application tests project references WildBunch.Domain.Tests? No - we need to create
        // the session inline here.
        var town = new WildBunch.Domain.World.Town(
            new TownId("current"), "Current Town", WildBunch.Domain.World.TownServices.NoticeBoard);
        var connected = new WildBunch.Domain.World.Town(
            new TownId("connected"), "Connected Town", WildBunch.Domain.World.TownServices.None);
        var world = new WildBunch.Domain.World.World(
            new[] { town, connected },
            new[] { new WildBunch.Domain.World.Trail(
                new WildBunch.Domain.World.TrailId("trail-1"), town.Id, connected.Id,
                WildBunch.Domain.World.TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Mira Cline",
                new SuspectProfile(
                    Array.Empty<SuspectAlias>(),
                    new[] { new SuspectIdentityFact("Has a scar on the left cheek.") }),
                SuspectTraits.Empty,
                SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null, suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads."),
            knownClues: Array.Empty<Clue>(),
            killerReleaseThreshold: 2,
            killerReleaseProgress: killerReleaseProgress,
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            WildBunch.Domain.Economy.Wallet.Starting(25m), inventory: null,
            WildBunch.Domain.Travel.GameDifficulty.Easy,
            WildBunch.Domain.Travel.TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }
}
