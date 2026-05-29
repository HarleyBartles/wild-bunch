using WildBunch.Domain.Inventory;

namespace WildBunch.GameContent.NewGame;

public static class SeedInventoryBuilder
{
    public static Inventory CreateStartingLoadout()
        => new(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.HorseFeed, 3),
            new InventoryItem(ItemKind.Canteen, 1),
            new InventoryItem(ItemKind.Horse, 1, HorseCondition.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1),
            new InventoryItem(ItemKind.Revolver, 1),
            new InventoryItem(ItemKind.RevolverAmmo, 6)
        });
}
