using DomainCanteenState = WildBunch.Domain.Inventory.CanteenState;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Domain.Tests;

public sealed class InventoryTests
{
    [Fact]
    public void ConstructorCombinesStackableItems()
    {
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 2),
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.RevolverAmmo, 4),
            new DomainInventoryItem(DomainItemKind.RevolverAmmo, 1)
        });

        Assert.Equal(5, inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(5, inventory.GetQuantity(DomainItemKind.RevolverAmmo));
        Assert.Equal(2, inventory.Items.Count);
    }

    [Fact]
    public void ConstructorRejectsDuplicateNonStackableItems()
    {
        Assert.Throws<InvalidOperationException>(() => new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        }));
    }

    [Fact]
    public void HorseDefaultsToHealthyStateAndCarriesItsOwnTravelState()
    {
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Horse, 1)
        });

        Assert.Single(inventory.Items);
        Assert.Equal(DomainHorseTravelState.Healthy, inventory.GetHorseState());
        Assert.Equal(DomainHorseTravelState.Healthy, inventory.Items[0].HorseState);
    }

    [Fact]
    public void CanteenDefaultsToFullChargesAndCarriesItsOwnWaterState()
    {
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        Assert.Single(inventory.Items);
        Assert.Equal(new DomainCanteenState(10, 10), inventory.GetCanteenState());
        Assert.Equal(new DomainCanteenState(10, 10), inventory.Items[0].CanteenState);
    }

    [Fact]
    public void HorseAndCanteenStateCanBeExplicitlySeededAndMutated()
    {
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Horse, 1, new DomainHorseTravelState(1, 0, 2)),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new DomainCanteenState(1, 2))
        });

        Assert.Equal(new DomainHorseTravelState(1, 0, 2), inventory.GetHorseState());
        Assert.Equal(new DomainCanteenState(1, 2), inventory.GetCanteenState());

        var nextHorseState = inventory.AdvanceHorseState(horseFed: false);
        inventory.SetCanteenState(new DomainCanteenState(0, 2));

        Assert.Equal(new DomainHorseTravelState(2, 0, 3), nextHorseState);
        Assert.Equal(new DomainHorseTravelState(2, 0, 3), inventory.GetHorseState());
        Assert.Equal(new DomainCanteenState(0, 2), inventory.GetCanteenState());
    }

    [Fact]
    public void AddAndRemoveQuantityWorkForStackables()
    {
        var inventory = new DomainInventory();

        inventory.AddItem(DomainItemKind.Food, 2);
        inventory.AddItem(DomainItemKind.Food, 3);
        inventory.RemoveQuantity(DomainItemKind.Food, 4);

        Assert.Equal(1, inventory.GetQuantity(DomainItemKind.Food));
    }
}
