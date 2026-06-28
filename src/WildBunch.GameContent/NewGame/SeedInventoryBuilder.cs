using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

public static class SeedInventoryBuilder
{
    public static Inventory CreateCanonicalLoadout(TravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= TravelRulesProfile.Default;

        return CreateStartingLoadout(travelRulesProfile, DifficultyEnvelope.For(GameDifficulty.Standard));
    }

    internal static Inventory CreateStartingLoadout(TravelRulesProfile travelRulesProfile, DifficultyEnvelope difficulty)
    {
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        ArgumentNullException.ThrowIfNull(difficulty);

        var (food, horseFeed, revolverAmmo) = ResolveLoadoutCounts(difficulty.LoadoutProfile);

        var items = new List<InventoryItem>
        {
            new(ItemKind.Food, food),
            new(ItemKind.HorseFeed, horseFeed),
            new(ItemKind.Canteen, 1, canteenState: CanteenState.Full(travelRulesProfile.CanteenCapacity))
        };

        if (difficulty.StartWithHorse)
        {
            items.Add(new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy));
            if (difficulty.IncludeSaddle)
            {
                items.Add(new InventoryItem(ItemKind.Saddle, 1));
            }
        }

        items.Add(new InventoryItem(ItemKind.Knife, 1));
        items.Add(new InventoryItem(ItemKind.Revolver, 1));
        items.Add(new InventoryItem(ItemKind.RevolverAmmo, revolverAmmo));

        return new Inventory(items);
    }

    internal static Wallet CreateStartingWallet(decimal startingCash)
    {
        return Wallet.Starting(startingCash);
    }

    private static (int Food, int HorseFeed, int RevolverAmmo) ResolveLoadoutCounts(StartingLoadoutProfile profile)
        => profile switch
        {
            StartingLoadoutProfile.Light => (3, 2, 4),
            StartingLoadoutProfile.Stocked => (6, 4, 8),
            _ => (4, 3, 6)
        };
}
