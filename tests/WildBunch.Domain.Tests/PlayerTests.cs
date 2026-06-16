using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainCanteenState = WildBunch.Domain.Inventory.CanteenState;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;

namespace WildBunch.Domain.Tests;

public sealed class PlayerTests
{
    [Fact]
    public void PlayerAdjustsCashAndInventoryThroughOwnedBehavior()
    {
        var player = new Player(
            "Ranger Vale",
            new TownId("pinecross"),
            1000,
            Wallet.Starting(25m),
            new DomainInventory(new[]
            {
                new DomainInventoryItem(DomainItemKind.Food, 1),
                new DomainInventoryItem(DomainItemKind.Canteen, 1)
            }));

        player.SpendCash(5m);
        player.AdjustCash(3m);
        player.AddItem(DomainItemKind.Food, 2);
        player.RemoveQuantity(DomainItemKind.Food, 1);

        Assert.True(player.CanAfford(18m));
        Assert.Equal(23m, player.Wallet.Cash);
        Assert.Equal(2, player.GetQuantity(DomainItemKind.Food));
    }

    [Fact]
    public void PlayerExposesHorseAndCanteenStateThroughOwnedBehavior()
    {
        var player = new Player(
            "Ranger Vale",
            new TownId("pinecross"),
            1000,
            Wallet.Starting(25m),
            new DomainInventory(new[]
            {
                new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseTravelState.Healthy),
                new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: DomainCanteenState.Full(10))
            }));

        Assert.True(player.HasItem(DomainItemKind.Horse));
        Assert.Equal(DomainHorseTravelState.Healthy, player.GetHorseState());
        Assert.Equal(10, player.GetCanteenState()!.Charges);
    }

    [Fact]
    public void PlayerResolvesCapabilitiesFromInventory()
    {
        var player = new Player(
            "Ranger Vale",
            new TownId("pinecross"),
            1000,
            Wallet.Starting(25m),
            new DomainInventory(new[]
            {
                new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseTravelState.Healthy),
                new DomainInventoryItem(DomainItemKind.Saddle, 1)
            }));

        var capabilities = player.GetCapabilities(TravelRulesProfile.Default);

        Assert.True(capabilities.MountedTravelAvailable);
        Assert.True(capabilities.HorseUpkeepRequired);
    }
}
