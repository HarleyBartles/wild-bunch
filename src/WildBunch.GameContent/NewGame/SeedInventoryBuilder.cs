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

        return CreateStartingLoadout(travelRulesProfile, StartingWorldDescriptorResolver.CreateCanonicalDescriptor().Player);
    }

    internal static Inventory CreateStartingLoadout(TravelRulesProfile travelRulesProfile, StartingWorldDescriptorPlayer player)
    {
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        ArgumentNullException.ThrowIfNull(player);

        var items = new List<InventoryItem>
        {
            new(ItemKind.Food, player.Loadout.Food),
            new(ItemKind.HorseFeed, player.Loadout.HorseFeed),
            new(ItemKind.Canteen, 1, canteenState: CanteenState.Full(travelRulesProfile.CanteenCapacity))
        };

        if (player.Loadout.IncludeHorse)
        {
            items.Add(new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy));
            items.Add(new InventoryItem(ItemKind.Saddle, 1));
        }

        items.Add(new InventoryItem(ItemKind.Knife, 1));
        items.Add(new InventoryItem(ItemKind.Revolver, 1));
        items.Add(new InventoryItem(ItemKind.RevolverAmmo, player.Loadout.RevolverAmmo));

        return new Inventory(items);
    }

    internal static Inventory CreateStartingLoadout(TravelRulesProfile travelRulesProfile, StartingWorldGenerationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.IsCanonical)
        {
            return CreateCanonicalLoadout(travelRulesProfile);
        }

        return CreateStartingLoadout(travelRulesProfile, plan.Descriptor.Player);
    }

    internal static Wallet CreateStartingWallet(StartingWorldGenerationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return Wallet.Starting(plan.Descriptor.Player.StartingCash);
    }
}
