using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionPurchaseTests
{
    [Fact]
    public void StackablePurchaseDebitsWalletAddsInventoryAndLogsOnce()
    {
        var session = CreateSession();
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
            .Offers.Single(candidate => candidate.VendorType == StoreVendorType.GeneralStore && candidate.ItemKind == DomainItemKind.Food);

        var result = session.Purchase(offer, 3);

        Assert.True(result.Success);
        Assert.Equal("Purchased 3 Food for $6.00.", result.Message);
        Assert.Equal(19m, session.Player.Wallet.Cash);
        Assert.Equal(4, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(2, session.LogEntries.Count);
        Assert.Equal(GameLogEntryKind.Purchase, session.LogEntries.Last().Kind);
    }

    [Fact]
    public void NonStackableHorsePurchaseAddsHorseWithStableCondition()
    {
        var session = CreateSession(emptyInventory: true, wallet: Wallet.Starting(100m));
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
            .Offers.Single(candidate => candidate.VendorType == StoreVendorType.Stable && candidate.ItemKind == DomainItemKind.Horse);

        var result = session.Purchase(offer, 1);

        Assert.True(result.Success);
        Assert.Equal("Purchased Horse for $60.00.", result.Message);
        Assert.Equal(40m, session.Player.Wallet.Cash);
        Assert.Equal(1, session.Player.Inventory.GetQuantity(DomainItemKind.Horse));
        Assert.Equal(DomainHorseTravelState.Healthy, session.Player.Inventory.GetHorseState());
        Assert.Equal(2, session.LogEntries.Count);
        Assert.Equal(GameLogEntryKind.Purchase, session.LogEntries.Last().Kind);
    }

    [Fact]
    public void InsufficientCashFailsWithoutMutation()
    {
        var session = CreateSession(wallet: Wallet.Starting(4m));
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
            .Offers.Single(candidate => candidate.VendorType == StoreVendorType.GeneralStore && candidate.ItemKind == DomainItemKind.Canteen);

        var result = session.Purchase(offer, 1);

        Assert.False(result.Success);
        Assert.Equal("Not enough cash.", result.Message);
        Assert.Equal(4m, session.Player.Wallet.Cash);
        Assert.Equal(1, session.Player.Inventory.GetQuantity(DomainItemKind.Canteen));
        Assert.Single(session.LogEntries);
    }

    [Fact]
    public void DuplicateNonStackablePurchaseFailsWithoutMutation()
    {
        var session = CreateSession(inventory: new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        }));
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
            .Offers.Single(candidate => candidate.VendorType == StoreVendorType.GeneralStore && candidate.ItemKind == DomainItemKind.Canteen);

        var result = session.Purchase(offer, 1);

        Assert.False(result.Success);
        Assert.Equal("Canteen already exists in inventory.", result.Message);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(1, session.Player.Inventory.GetQuantity(DomainItemKind.Canteen));
        Assert.Single(session.LogEntries);
    }

    [Fact]
    public void InvalidQuantityFailsWithoutMutation()
    {
        var session = CreateSession();
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
            .Offers.Single(candidate => candidate.VendorType == StoreVendorType.GeneralStore && candidate.ItemKind == DomainItemKind.Food);

        var result = session.Purchase(offer, 0);

        Assert.False(result.Success);
        Assert.Equal("Quantity must be at least 1.", result.Message);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(1, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Single(session.LogEntries);
    }

    [Fact]
    public void HorseQuantityAboveOneFailsWithoutMutation()
    {
        var session = CreateSession(emptyInventory: true);
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
            .Offers.Single(candidate => candidate.VendorType == StoreVendorType.Stable && candidate.ItemKind == DomainItemKind.Horse);

        var result = session.Purchase(offer, 2);

        Assert.False(result.Success);
        Assert.Equal("Horse items must have a quantity of 1.", result.Message);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(0, session.Player.Inventory.GetQuantity(DomainItemKind.Horse));
        Assert.Single(session.LogEntries);
    }

    private static GameSession CreateSession(
        bool emptyInventory = false,
        Wallet? wallet = null,
        DomainInventory? inventory = null)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var resolvedInventory = inventory
            ?? (emptyInventory
                ? DomainInventory.Empty()
                : new DomainInventory(new[]
                {
                    new DomainInventoryItem(DomainItemKind.Food, 1),
                    new DomainInventoryItem(DomainItemKind.Canteen, 1)
                }));

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, wallet ?? Wallet.Starting(25m), resolvedInventory);
    }
}
