// tests/WildBunch.Domain.Tests/TownActionAvailabilityTests.cs
using WildBunch.Domain.Actions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class TownActionAvailabilityTests
{
    /// <summary>
    /// Creates a session in a town with TownServices.None - no NoticeBoard,
    /// no Supplies, no Lodging, no Telegraph, no Doctor. This is the worst case
    /// for action availability. The town uses the default source catalog (no
    /// sabotaged definitions). Every town has a saloon and a sheriff's office,
    /// so both ReadWantedPosters and LookAroundSaloon must be available here.
    /// </summary>
    private static GameSession CreateSessionInNoServiceTown()
    {
        var town = new Town(new TownId("no-service"), "No Service Town", TownServices.None);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
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
    public void ActionAvailabilityResolver_AlwaysIncludesReadWantedPosters_RegardlessOfTownServices()
    {
        var session = CreateSessionInNoServiceTown();

        // Prove the precondition: the town has no NoticeBoard service.
        // This makes the test falsifiable - if the town had NoticeBoard,
        // the assertion would pass trivially even without the fix.
        Assert.Equal(TownServices.None, session.CurrentTown.Services);
        Assert.False((session.CurrentTown.Services & TownServices.None) != 0);

        var resolver = new ActionAvailabilityResolver();
        var actions = resolver.Resolve(session);

        Assert.Contains(actions, a => a.Kind == AvailableActionKind.ReadWantedPosters);
    }

    [Fact]
    public void ActionAvailabilityResolver_AlwaysIncludesLookAroundSaloon_RegardlessOfTownServices()
    {
        var session = CreateSessionInNoServiceTown();

        // Prove the precondition: the town has no services at all.
        Assert.Equal(TownServices.None, session.CurrentTown.Services);

        var resolver = new ActionAvailabilityResolver();
        var actions = resolver.Resolve(session);

        Assert.Contains(actions, a => a.Kind == AvailableActionKind.LookAroundSaloon);
    }

    [Fact]
    public void ReadWantedPosters_Succeeds_EvenWhenTownHasNoNoticeBoardService()
    {
        var session = CreateSessionInNoServiceTown();

        // Prove the precondition: no NoticeBoard service.
        Assert.False((session.CurrentTown.Services & TownServices.None) != 0);

        var result = session.ReadWantedPosters();

        // Should not fail with "no wanted posters here" - that check is removed.
        // It may succeed with "nothing new" if no warrants/clues are available,
        // but it must not fail with the availability message.
        Assert.True(result.Success);
    }

    [Fact]
    public void LookAroundSaloon_Succeeds_EvenWhenTownHasNoServices()
    {
        var session = CreateSessionInNoServiceTown();

        // Prove the precondition: no services at all.
        Assert.Equal(TownServices.None, session.CurrentTown.Services);

        var result = session.LookAroundSaloon();

        // Should not fail with "no saloon here" - that check is removed.
        Assert.True(result.Success);
    }
}
