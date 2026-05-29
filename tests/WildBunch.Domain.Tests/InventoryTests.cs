using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using DomainHorseCondition = WildBunch.Domain.Inventory.HorseCondition;

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
    public void HorseRemainsUniqueAndCarriesOnlyItsOwnCondition()
    {
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseCondition.Healthy)
        });

        Assert.Single(inventory.Items);
        Assert.Equal(DomainHorseCondition.Healthy, inventory.GetHorseCondition());
        Assert.Throws<ArgumentNullException>(() => new DomainInventoryItem(DomainItemKind.Horse, 1));
        Assert.Throws<ArgumentException>(() => new DomainInventoryItem(DomainItemKind.Canteen, 1, DomainHorseCondition.Healthy));
        Assert.Throws<InvalidOperationException>(() => inventory.AddItem(DomainItemKind.Horse, 1, DomainHorseCondition.Healthy));
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
