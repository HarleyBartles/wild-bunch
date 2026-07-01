using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Stateless child domain component inside the session boundary that owns store
/// purchase decision logic. Receives narrow context records, returns StoreItemPurchased
/// events for the parent aggregate to produce. Does NOT reference the parent aggregate,
/// produce events directly, enter action context, or mutate Player. See BUNCH-120.
/// </summary>
internal sealed class StoreLoop
{
    /// <summary>
    /// Purchase decision logic. Validates quantity, stackability, cash, and inventory
    /// constraints. Returns the StoreItemPurchased event and display message, or null
    /// with a failure message if validation fails.
    /// </summary>
    internal StorePurchaseOutcome Purchase(StorePurchaseContext context)
    {
        ArgumentNullException.ThrowIfNull(context.Offer);

        if (context.Quantity < 1)
        {
            return StorePurchaseOutcome.Failed("Quantity must be at least 1.");
        }

        if (context.Offer.ItemKind == ItemKind.Horse && context.Quantity != 1)
        {
            return StorePurchaseOutcome.Failed("Horse items must have a quantity of 1.");
        }

        if (context.Quantity != 1 && !IsStackableItemKind(context.Offer.ItemKind))
        {
            return StorePurchaseOutcome.Failed($"{context.Offer.ItemKind} does not stack.");
        }

        var totalPrice = context.Offer.Price * context.Quantity;
        if (!context.PlayerCanAfford(totalPrice))
        {
            return StorePurchaseOutcome.Failed("Not enough cash.");
        }

        if (!CanPurchaseInventoryItem(context.Offer, context.Quantity, context.PlayerHasItem, out var inventoryFailureMessage))
        {
            return StorePurchaseOutcome.Failed(inventoryFailureMessage);
        }

        var e = new StoreItemPurchased
        {
            TownId = context.CurrentTownId,
            ItemKind = context.Offer.ItemKind,
            DisplayName = context.Offer.DisplayName,
            Quantity = context.Quantity,
            UnitPrice = context.Offer.Price,
            TotalPrice = totalPrice,
            WalletAfter = context.PlayerCash - totalPrice
        };

        var quantityLabel = context.Quantity == 1 ? context.Offer.DisplayName : $"{context.Quantity} {context.Offer.DisplayName}";
        return StorePurchaseOutcome.Succeeded(e, $"Purchased {quantityLabel} for ${totalPrice:0.00}.");
    }

    private static bool CanPurchaseInventoryItem(StoreOffer offer, int quantity, Func<ItemKind, bool> playerHasItem, out string failureMessage)
    {
        if (quantity < 1)
        {
            failureMessage = "Quantity must be at least 1.";
            return false;
        }

        if (offer.ItemKind == ItemKind.Horse)
        {
            if (quantity != 1)
            {
                failureMessage = "Horse items must have a quantity of 1.";
                return false;
            }

            if (playerHasItem(ItemKind.Horse))
            {
                failureMessage = "Horse already exists in inventory.";
                return false;
            }

            failureMessage = string.Empty;
            return true;
        }

        if (quantity != 1 && !IsStackableItemKind(offer.ItemKind))
        {
            failureMessage = $"{offer.ItemKind} does not stack.";
            return false;
        }

        if (!IsStackableItemKind(offer.ItemKind) && playerHasItem(offer.ItemKind))
        {
            failureMessage = $"{offer.ItemKind} already exists in inventory.";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    internal static bool IsStackableItemKind(ItemKind kind)
        => kind is ItemKind.Food or ItemKind.HorseFeed or ItemKind.RevolverAmmo or ItemKind.RifleAmmo;
}

internal sealed record StorePurchaseContext(
    StoreOffer Offer,
    int Quantity,
    TownId CurrentTownId,
    decimal PlayerCash,
    Func<decimal, bool> PlayerCanAfford,
    Func<ItemKind, bool> PlayerHasItem);

internal sealed record StorePurchaseOutcome(bool Success, StoreItemPurchased? Event, string Message)
{
    internal static StorePurchaseOutcome Failed(string message) => new(false, null, message);
    internal static StorePurchaseOutcome Succeeded(StoreItemPurchased e, string message) => new(true, e, message);
}
