using WildBunch.Domain.Economy;
using DomainCanteenState = WildBunch.Domain.Inventory.CanteenState;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryCapabilityResolver = WildBunch.Domain.Inventory.InventoryCapabilityResolver;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

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
        Assert.DoesNotContain(inventory.Items, item => item.Kind == DomainItemKind.Food && item.HorseState is not null);
    }

    [Theory]
    [InlineData(0, 0, 0, true)]
    [InlineData(1, 0, 0, true)]
    [InlineData(0, 1, 0, true)]
    [InlineData(0, 0, 2, true)]
    [InlineData(0, 2, 0, false)]
    [InlineData(0, 0, 3, false)]
    [InlineData(3, 0, 0, false)]
    public void MountedTravelRequiresLivingNonLameHorseAndSaddle(int hunger, int thirst, int exhaustion, bool expected)
    {
        var resolver = new DomainInventoryCapabilityResolver();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Horse, 1, new DomainHorseTravelState(hunger, thirst, exhaustion)),
            new DomainInventoryItem(DomainItemKind.Saddle, 1)
        });

        Assert.Equal(expected, resolver.Resolve(inventory).MountedTravelAvailable);
    }

    [Fact]
    public void HorseUpkeepRequiresLivingHorseEvenWithoutSaddle()
    {
        var resolver = new DomainInventoryCapabilityResolver();
        var healthyHorse = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseTravelState.Healthy) });
        var hungryHorse = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Horse, 1, new DomainHorseTravelState(1, 0, 0)) });
        var exhaustedHorse = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Horse, 1, new DomainHorseTravelState(0, 1, 0)) });
        var lameHorse = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Horse, 1, new DomainHorseTravelState(0, 0, 3)) });
        var deadHorse = new DomainInventory(new[] { new DomainInventoryItem(DomainItemKind.Horse, 1, new DomainHorseTravelState(3, 0, 0)) });

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
    public void EmptyCanteenDoesNotSecureNormalRouteWater()
    {
        var resolver = new DomainInventoryCapabilityResolver();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new DomainCanteenState(0, 2))
        });

        Assert.False(resolver.Resolve(inventory).NormalRouteWaterSecure);
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
