using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using Xunit;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Beat model economy tests proving same-scene grouping, cross-location advancement,
/// and day-wrap behavior. These are characterization tests — the production code already
/// supports these behaviors via EnterActionContext's same-context suppression.
/// </summary>
public class BeatModelEconomyTests
{
    [Fact]
    public void CrossLocationAction_AdvancesBeat()
    {
        var session = TestSessionFactory.CreateDefault();
        session.GatherLocalGossip(); // enters Saloon
        var turnAfterSaloon = session.Clock.Turn;

        session.ReadWantedPosters(); // enters Sheriff Office
        Assert.Equal(turnAfterSaloon + 1, session.Clock.Turn);
    }

    [Fact]
    public void SameSceneCompatibleActions_DoNotAdvanceBeat()
    {
        var session = TestSessionFactory.CreateDefault();
        session.LookAroundSaloon(); // enters Saloon
        var turnAfterSaloon = session.Clock.Turn;

        // GatherLocalGossip is also Saloon context — same scene, no beat advance
        session.GatherLocalGossip();
        Assert.Equal(turnAfterSaloon, session.Clock.Turn);
    }

    [Fact]
    public void EnterActionContext_DifferentContext_ReturnsTrueAndAdvances()
    {
        var session = TestSessionFactory.CreateDefault();
        var turnBefore = session.Clock.Turn;

        var entered = session.EnterActionContext(TownActionContext.SheriffOffice);
        Assert.True(entered);
        Assert.Equal(turnBefore + 1, session.Clock.Turn);
    }

    [Fact]
    public void EnterActionContext_SameContext_ReturnsFalseAndDoesNotAdvance()
    {
        var session = TestSessionFactory.CreateDefault();
        session.EnterActionContext(TownActionContext.SheriffOffice);
        var turnAfterFirst = session.Clock.Turn;

        var entered = session.EnterActionContext(TownActionContext.SheriffOffice);
        Assert.False(entered);
        Assert.Equal(turnAfterFirst, session.Clock.Turn);
    }

    [Fact]
    public void FullDayPasses_WhenFourBeatsConsumed()
    {
        var session = CreateSessionWithStore();
        var dayBefore = session.Clock.Day;

        session.ReadWantedPosters(); // enters Sheriff Office
        session.GatherLocalGossip(); // enters Saloon
        var offer = new TownStoreCatalogResolver()
            .Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(item => item.ItemKind == DomainItemKind.Food);
        session.Purchase(offer, 1); // enters Store
        session.ReadWantedPosters(); // returns to Sheriff Office and wraps to next day

        Assert.Equal(dayBefore + 1, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(TimeOfDay.Morning, session.Clock.TimeOfDay);
    }

    [Fact]
    public void HeatIncreases_WhenFullDayPassesInTown()
    {
        var session = CreateSessionWithStore();
        var heatBefore = session.PursuitState.Heat;

        session.ReadWantedPosters();
        session.GatherLocalGossip();
        var offer = new TownStoreCatalogResolver()
            .Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(item => item.ItemKind == DomainItemKind.Food);
        session.Purchase(offer, 1);
        session.ReadWantedPosters(); // wraps to next day

        Assert.Equal(heatBefore + 1, session.PursuitState.Heat);
    }

    [Fact]
    public void Purchase_EntersStoreContext_AndSecondPurchaseDoesNotAdvanceBeat()
    {
        var session = CreateSessionWithStore();
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.ItemKind == DomainItemKind.Food);

        session.Purchase(offer, 1); // enters Store
        var turnAfterFirstPurchase = session.Clock.Turn;

        session.Purchase(offer, 1); // same Store context
        Assert.Equal(turnAfterFirstPurchase, session.Clock.Turn);
    }

    private static GameSession CreateSessionWithStore()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa");
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint",
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        return TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Standard);
    }
}
