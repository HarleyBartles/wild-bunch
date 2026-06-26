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

public sealed class ClearSaloonOverrideHandlerTests
{
    [Fact]
    public async Task HandleAsync_ClearsOverride_AndPersists()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new ClearSaloonOverrideHandler(repository, repository);

        await handler.HandleAsync(new ClearSaloonOverrideCommand(session.Id.Value));

        Assert.Equal(1, repository.StoreCalls);
        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.Null(reloaded!.PendingDevSaloonOverride);
    }

    [Fact]
    public async Task HandleAsync_WhenNoOverride_IsNoOp()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        repository.Seed(session);

        var handler = new ClearSaloonOverrideHandler(repository, repository);

        await handler.HandleAsync(new ClearSaloonOverrideCommand(session.Id.Value));

        // ClearDevSaloonOverride is a no-op when nothing is pending - no events produced,
        // but the handler still saves (the aggregate itself just doesn't produce events).
        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.Null(reloaded!.PendingDevSaloonOverride);
    }

    private static GameSession CreateSessionWithSaloonSuspect()
    {
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
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            WildBunch.Domain.Economy.Wallet.Starting(25m), inventory: null,
            WildBunch.Domain.Travel.TravelDifficulty.Easy,
            WildBunch.Domain.Travel.TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }
}
