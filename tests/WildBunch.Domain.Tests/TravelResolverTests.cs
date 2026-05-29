using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;
using TrailId = WildBunch.Domain.World.TrailId;
using TownServices = WildBunch.Domain.World.TownServices;
using TrailRisk = WildBunch.Domain.World.TrailRisk;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;

namespace WildBunch.Domain.Tests;

public sealed class TravelResolverTests
{
    [Fact]
    public void TravelToConnectedTownMovesPlayerConsumesSuppliesAdvancesClockAndIncreasesHeat()
    {
        var session = CreateSession();
        var resolver = new TravelResolver();

        var result = resolver.Travel(session.World, session, new TownId("silvercreek"));

        Assert.True(result.Success);
        Assert.Equal(new TownId("silvercreek"), session.Player.CurrentTownId);
        Assert.Equal(10, session.Player.Supplies.Units);
        Assert.Equal(1, session.Clock.Day);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(1, session.PursuitState.Heat);
        Assert.Contains(session.LogEntries, entry => entry.Kind == GameLogEntryKind.Travel);
    }

    [Fact]
    public void TravelToUnconnectedTownFailsAndDoesNotMovePlayer()
    {
        var session = CreateSession();
        var resolver = new TravelResolver();

        var result = resolver.Travel(session.World, session, new TownId("dryridge"));

        Assert.False(result.Success);
        Assert.Equal(new TownId("dustvale"), session.Player.CurrentTownId);
        Assert.Equal(12, session.Player.Supplies.Units);
        Assert.Equal(1, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void TravelWithoutEnoughSuppliesFailsAndDoesNotMovePlayer()
    {
        var session = CreateSession(supplyUnits: 1);
        var resolver = new TravelResolver();

        var result = resolver.Travel(session.World, session, new TownId("silvercreek"));

        Assert.False(result.Success);
        Assert.Equal(new TownId("dustvale"), session.Player.CurrentTownId);
        Assert.Equal(1, session.Player.Supplies.Units);
        Assert.Equal(1, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(0, session.PursuitState.Heat);
    }

    private static GameSession CreateSession(int supplyUnits = 12)
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge", TownServices.None);

        var world = new DomainWorld(
            new[] { dustvale, silvercreek, dryridge },
            new[]
            {
                new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, SupplyCost: 2, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var session = GameSession.StartNew("Ranger Vale", world, caseFile);

        session.Player.SpendSupplies(12 - supplyUnits);
        return session;
    }
}
