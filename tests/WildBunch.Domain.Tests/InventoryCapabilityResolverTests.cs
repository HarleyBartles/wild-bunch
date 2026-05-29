using WildBunch.Domain.Economy;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryCapabilityResolver = WildBunch.Domain.Inventory.InventoryCapabilityResolver;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using DomainHorseCondition = WildBunch.Domain.Inventory.HorseCondition;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;

namespace WildBunch.Domain.Tests;

public sealed class InventoryCapabilityResolverTests
{
    [Fact]
    public void WalletIsSeparateFromInventoryItems()
    {
        var wallet = Wallet.Starting(25m);
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 2),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        Assert.Equal(25m, wallet.Cash);
        Assert.Equal(2, inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(1, inventory.GetQuantity(DomainItemKind.Canteen));
        Assert.DoesNotContain(inventory.Items, item => item.Kind == DomainItemKind.Food && item.HorseCondition is not null);
    }

    [Theory]
    [InlineData(DomainHorseCondition.Healthy, true)]
    [InlineData(DomainHorseCondition.Hungry, false)]
    [InlineData(DomainHorseCondition.Exhausted, false)]
    [InlineData(DomainHorseCondition.Lame, false)]
    [InlineData(DomainHorseCondition.Dead, false)]
    public void MountedTravelRequiresHealthyHorseAndSaddle(DomainHorseCondition horseCondition, bool expected)
    {
        var resolver = new DomainInventoryCapabilityResolver();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Horse, 1, horseCondition),
            new DomainInventoryItem(DomainItemKind.Saddle, 1)
        });

        Assert.Equal(expected, resolver.Resolve(inventory).MountedTravelAvailable);
    }

    [Fact]
    public void HorseUpkeepRequiresLivingHorseEvenWithoutSaddle()
    {
        var resolver = new DomainInventoryCapabilityResolver();
        var healthyHorse = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseCondition.Healthy) });
        var hungryHorse = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseCondition.Hungry) });
        var exhaustedHorse = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseCondition.Exhausted) });
        var lameHorse = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseCondition.Lame) });
        var deadHorse = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseCondition.Dead) });

        Assert.True(resolver.Resolve(healthyHorse).HorseUpkeepRequired);
        Assert.True(resolver.Resolve(hungryHorse).HorseUpkeepRequired);
        Assert.True(resolver.Resolve(exhaustedHorse).HorseUpkeepRequired);
        Assert.True(resolver.Resolve(lameHorse).HorseUpkeepRequired);
        Assert.False(resolver.Resolve(deadHorse).HorseUpkeepRequired);
    }

    [Fact]
    public void KnifeEnablesTrailUtilityAndCloseThreat()
    {
        var resolver = new DomainInventoryCapabilityResolver();
        var inventory = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Knife, 1) });

        var capabilities = resolver.Resolve(inventory);

        Assert.True(capabilities.TrailUtility);
        Assert.True(capabilities.CloseThreatAvailable);
    }

    [Fact]
    public void CanteenSecureNormalRouteWater()
    {
        var resolver = new DomainInventoryCapabilityResolver();
        var withCanteen = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Canteen, 1) });
        var withoutCanteen = DomainInventory.Empty();

        Assert.True(resolver.Resolve(withCanteen).NormalRouteWaterSecure);
        Assert.False(resolver.Resolve(withoutCanteen).NormalRouteWaterSecure);
    }

    [Fact]
    public void RevolverRequiresMatchingAmmoAndWrongAmmoDoesNotWork()
    {
        var resolver = new DomainInventoryCapabilityResolver();
        var matched = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Revolver, 1),
            new DomainInventoryItem(DomainItemKind.RevolverAmmo, 1)
        });
        var mismatched = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Revolver, 1),
            new DomainInventoryItem(DomainItemKind.RifleAmmo, 1)
        });

        Assert.True(resolver.Resolve(matched).RevolverUsable);
        Assert.True(resolver.Resolve(matched).GunfightCapable);
        Assert.False(resolver.Resolve(mismatched).RevolverUsable);
        Assert.False(resolver.Resolve(mismatched).GunfightCapable);
    }

    [Fact]
    public void RifleRequiresMatchingAmmoAndCountsAsFirearmThreat()
    {
        var resolver = new DomainInventoryCapabilityResolver();
        var matched = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Rifle, 1),
            new DomainInventoryItem(DomainItemKind.RifleAmmo, 2)
        });

        var capabilities = resolver.Resolve(matched);

        Assert.True(capabilities.RifleUsable);
        Assert.True(capabilities.FirearmThreatAvailable);
        Assert.True(capabilities.GunfightCapable);
    }
}
