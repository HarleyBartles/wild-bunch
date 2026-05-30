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

    internal static Inventory CreateStartingLoadout(string seedCode, TravelRulesProfile travelRulesProfile, GameSetupOptionsV1 options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);
        return CreateStartingLoadout(travelRulesProfile, options);
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
