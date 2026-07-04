using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Domain.Tests;

public sealed class PurchaseBeatCostTests
{
    [Fact]
    public void Purchase_EntersStoreContextAndAdvancesBeat()
    {
        var session = CreateSession();
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(candidate => candidate.VendorType == StoreVendorType.GeneralStore && candidate.ItemKind == DomainItemKind.Food);

        var turnBefore = session.Clock.Turn;
        var contextBefore = session.CurrentActionContext;

        var result = session.Purchase(offer, 1);
        Assert.True(result.Success);
        Assert.Equal(TownActionContext.Store, session.CurrentActionContext);
        Assert.Equal(turnBefore + 1, session.Clock.Turn);
    }

    [Fact]
    public void Purchase_SameStoreContext_DoesNotAdvanceBeatAgain()
    {
        var session = CreateSession();
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(candidate => candidate.VendorType == StoreVendorType.GeneralStore && candidate.ItemKind == DomainItemKind.Food);

        // First purchase enters Store context
        session.Purchase(offer, 1);
        var turnAfterFirst = session.Clock.Turn;

        // Second purchase in same Store context should NOT advance beat
        var result = session.Purchase(offer, 1);
        Assert.True(result.Success);
        Assert.Equal(turnAfterFirst, session.Clock.Turn);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
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
