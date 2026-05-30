using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

public static class SeedInventoryBuilder
{
    public static Inventory CreateCanonicalLoadout(TravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= TravelRulesProfile.Default;

        return CreateStartingLoadout(travelRulesProfile, GameSetupOptionsV1.Default);
    }

    internal static Inventory CreateStartingLoadout(TravelRulesProfile travelRulesProfile, GameSetupOptionsV1 options)
    {
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        ArgumentNullException.ThrowIfNull(options);

        var items = new List<InventoryItem>
        {
            new(ItemKind.Food, options.LoadoutProfile switch
            {
                StartingLoadoutProfile.Light => 3,
                StartingLoadoutProfile.Stocked => 6,
                _ => 4
            }),
            new(ItemKind.HorseFeed, options.LoadoutProfile switch
            {
                StartingLoadoutProfile.Light => 2,
                StartingLoadoutProfile.Stocked => 4,
                _ => 3
            }),
            new(ItemKind.Canteen, 1, canteenState: CanteenState.Full(travelRulesProfile.CanteenCapacity))
        };

        if (options.StartWithHorse)
        {
            items.Add(new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy));
            items.Add(new InventoryItem(ItemKind.Saddle, 1));
        }

        items.Add(new InventoryItem(ItemKind.Knife, 1));
        items.Add(new InventoryItem(ItemKind.Revolver, 1));
        items.Add(new InventoryItem(ItemKind.RevolverAmmo, options.LoadoutProfile switch
        {
            StartingLoadoutProfile.Light => 4,
            StartingLoadoutProfile.Stocked => 8,
            _ => 6
        }));

        return new Inventory(items);
    }

    internal static Inventory CreateStartingLoadout(GameSetupGenerationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.IsCanonical)
        {
            return CreateCanonicalLoadout(plan.TravelRulesProfile);
        }

        return CreateStartingLoadout(plan.TravelRulesProfile, plan.Seed.Options);
    }

    internal static Wallet CreateStartingWallet(GameSetupGenerationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.IsCanonical)
        {
            return Wallet.Starting(25m);
        }

        var baseCash = plan.Seed.Difficulty switch
        {
            TravelDifficulty.Easy => 30m,
            TravelDifficulty.Hard => 20m,
            _ => 25m
        };

        var profileBonus = plan.Seed.Options.LoadoutProfile switch
        {
            StartingLoadoutProfile.Light => -5m,
            StartingLoadoutProfile.Stocked => 5m,
            _ => 0m
        };

        var entropyBonus = plan.Source.PickIndex(GameSetupDeterministicLabels.PlayerWalletStarting, 6);
        var horseBonus = plan.Seed.Options.StartWithHorse ? 2m : 0m;
        return Wallet.Starting(Math.Max(10m, baseCash + profileBonus + horseBonus + entropyBonus));
    }
}
