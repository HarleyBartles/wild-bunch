using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
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
    public void TravelToConnectedTownMovesPlayerAdvancesClockAndIncreasesHeat()
    {
        var session = CreateSession();
        var resolver = new TravelResolver();

        var result = resolver.Travel(session.World, session, new TownId("silvercreek"));

        Assert.True(result.Success);
        Assert.Equal(new TownId("silvercreek"), session.Player.CurrentTownId);
        Assert.Equal(25m, session.Player.Wallet.Cash);
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
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(1, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void TravelDoesNotConsumeWalletOrInventory()
    {
        var session = CreateSession();
        var resolver = new TravelResolver();

        var result = resolver.Travel(session.World, session, new TownId("silvercreek"));

        Assert.True(result.Success);
        Assert.Equal(new TownId("silvercreek"), session.Player.CurrentTownId);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(1, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(1, session.Clock.Day);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(1, session.PursuitState.Heat);
    }

    private static GameSession CreateSession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge", TownServices.None);

        var world = new DomainWorld(
            new[] { dustvale, silvercreek, dryridge },
            new[]
            {
                new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, dustvale.Id, Wallet.Starting(25m), inventory);
    }
}
