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

        return new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.HorseFeed, 3),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(travelRulesProfile.CanteenCapacity)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1),
            new InventoryItem(ItemKind.Revolver, 1),
            new InventoryItem(ItemKind.RevolverAmmo, 6)
        });
    }

    internal static Inventory CreateStartingLoadout(string seedCode, TravelRulesProfile travelRulesProfile, GameSetupOptionsV1 options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        ArgumentNullException.ThrowIfNull(options);

        var horseFeed = options.LoadoutProfile switch
        {
            StartingLoadoutProfile.Light => 2,
            StartingLoadoutProfile.Stocked => 4,
            _ => 3
        };

        var food = options.LoadoutProfile switch
        {
            StartingLoadoutProfile.Light => 3,
            StartingLoadoutProfile.Stocked => 6,
            _ => 4
        };

        var ammo = options.LoadoutProfile switch
        {
            StartingLoadoutProfile.Light => 4,
            StartingLoadoutProfile.Stocked => 8,
            _ => 6
        };

        var items = new List<InventoryItem>
        {
            new(ItemKind.Food, food),
            new(ItemKind.HorseFeed, horseFeed),
            new(ItemKind.Canteen, 1, canteenState: CanteenState.Full(travelRulesProfile.CanteenCapacity)),
            new(ItemKind.Knife, 1),
            new(ItemKind.Revolver, 1),
            new(ItemKind.RevolverAmmo, ammo)
        };

        if (options.StartWithHorse)
        {
            items.Insert(3, new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy));
            items.Insert(4, new InventoryItem(ItemKind.Saddle, 1));
        }

        return new Inventory(items);
    }

    internal static Wallet CreateStartingWallet(string seedCode, TravelDifficulty difficulty, GameSetupOptionsV1 options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);
        ArgumentNullException.ThrowIfNull(options);

        var baseCash = difficulty switch
        {
            TravelDifficulty.Easy => 30m,
            TravelDifficulty.Hard => 20m,
            _ => 25m
        };

        var profileBonus = options.LoadoutProfile switch
        {
            StartingLoadoutProfile.Light => -5m,
            StartingLoadoutProfile.Stocked => 5m,
            _ => 0m
        };

        var entropyBonus = PickIndex(seedCode, "wallet-bonus", 6);
        var horseBonus = options.StartWithHorse ? 2m : 0m;
        return Wallet.Starting(Math.Max(10m, baseCash + profileBonus + horseBonus + entropyBonus));
    }

    private static int PickIndex(string seedCode, string label, int count)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seedCode}|{label}"));
        return (int)(BitConverter.ToUInt64(bytes, 0) % (ulong)count);
    }
}
