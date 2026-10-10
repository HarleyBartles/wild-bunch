// tests/WildBunch.Domain.Tests/TownActionAvailabilityTests.cs
using WildBunch.Domain.Actions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class TownActionAvailabilityTests
{
    /// <summary>
    /// Creates a session in a generated-shape town with the shared core surface.
    /// </summary>
    private static GameSession CreateSessionInTown()
    {
        var town = new Town(new TownId("town"), "Town");
        var connected = new Town(new TownId("connected"), "Connected Town");
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint",
                SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike",
                SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null, suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var session = TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: null, GameDifficulty.Easy,
            SaltSource.CreateFixed(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }

    [Fact]
    public void ActionAvailabilityResolverIncludesWantedPostersInTown()
    {
        var session = CreateSessionInTown();

        var resolver = new ActionAvailabilityResolver();
        var actions = resolver.Resolve(new ActionAvailabilityContext(
            session.StartFlowPhase,
            session.World,
            session.Player.CurrentTownId,
            session.Journey?.ToSnapshot(session.TravelRules)));

        Assert.Contains(actions, a => a.Kind == AvailableActionKind.ReadWantedPosters);
    }

    [Fact]
    public void ActionAvailabilityResolverIncludesSaloonLookAroundInTown()
    {
        var session = CreateSessionInTown();

        var resolver = new ActionAvailabilityResolver();
        var actions = resolver.Resolve(new ActionAvailabilityContext(
            session.StartFlowPhase,
            session.World,
            session.Player.CurrentTownId,
            session.Journey?.ToSnapshot(session.TravelRules)));

        Assert.Contains(actions, a => a.Kind == AvailableActionKind.LookAroundSaloon);
    }

    [Fact]
    public void ReadWantedPostersSucceedsInTown()
    {
        var session = CreateSessionInTown();

        var result = session.ReadWantedPosters();

        Assert.True(result.Success);
    }

    [Fact]
    public void LookAroundSaloonSucceedsInTown()
    {
        var session = CreateSessionInTown();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
    }
}
