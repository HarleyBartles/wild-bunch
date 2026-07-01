using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Projections;

/// <summary>
/// Reference projector for the HUD projection.
/// Derives player-facing state from typed domain events.
/// This is a pure function over the event stream — no aggregate mutation.
/// See ADR-0028.
/// </summary>
public sealed class HudProjector : IDomainEventProjector<HudProjection>
{
    public HudProjection Project(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        Guid sessionId = Guid.Empty;
        GameStatus status = GameStatus.Active;
        string playerName = string.Empty;
        int health = 0;
        decimal walletCash = 0m;
        TownId currentTownId = default;
        string currentTownName = string.Empty;
        var inventory = new Dictionary<ItemKind, int>();

        foreach (var e in events)
        {
            switch (e)
            {
                case GameStarted gs:
                    sessionId = default; // SessionId is not in the event; caller sets it
                    status = GameStatus.Active;
                    playerName = gs.PlayerName;
                    health = gs.StartingHealth;
                    walletCash = gs.StartingWallet;
                    currentTownId = gs.StartingTownId;
                    currentTownName = gs.StartingTownName;
                    inventory.Clear();
                    foreach (var item in gs.StartingInventoryItems)
                    {
                        inventory[item.Kind] = item.Quantity;
                    }
                    break;

                case StoreItemPurchased sp:
                    walletCash = sp.WalletAfter;
                    inventory[sp.ItemKind] = (inventory.TryGetValue(sp.ItemKind, out var qty) ? qty : 0) + sp.Quantity;
                    break;

                case SheriffTurnInSettled st:
                    walletCash += st.BountyAmount;
                    break;

                case SaloonPersonOfInterestConfronted sc:
                    if (sc.WalletAfter is { } walletAfter)
                    {
                        walletCash = walletAfter;
                    }
                    else if (sc.FineAmount is { } fine)
                    {
                        walletCash -= fine;
                    }
                    break;

                case TravelDayAdvanced tda:
                    health += tda.HealthDelta;
                    // Player food/canteen/horse feed are set ABSOLUTE from the journey
                    // snapshot. See ADR-0028 and BUNCH-83.
                    SyncInventoryFromJourneySnapshot(inventory, tda.JourneySnapshot);
                    break;

                case TrailEventApplied tea:
                    // WalletCash and PursuitHeat are ABSOLUTE in the event.
                    walletCash = tea.WalletCash;
                    SyncInventoryFromJourneySnapshot(inventory, tea.JourneySnapshot);
                    break;

                case JourneyEncounterResolved jer:
                    health = jer.PlayerHealth;
                    walletCash = jer.WalletCash;
                    if (jer.AmmoSpent > 0)
                    {
                        inventory[ItemKind.RevolverAmmo] = Math.Max(0,
                            (inventory.TryGetValue(ItemKind.RevolverAmmo, out var ammo) ? ammo : 0) - jer.AmmoSpent);
                    }
                    if (jer.StolenItemKind is { } kind && jer.StolenItemQuantity > 0)
                    {
                        inventory[kind] = Math.Max(0,
                            (inventory.TryGetValue(kind, out var stolen) ? stolen : 0) - jer.StolenItemQuantity);
                    }
                    break;

                case JourneyCompleted jc:
                    currentTownId = jc.DestinationTownId;
                    currentTownName = jc.DestinationTownName;
                    // Canteen is refilled on arrival — sync from snapshot.
                    SyncInventoryFromJourneySnapshot(inventory, jc.JourneySnapshot);
                    break;

                case PlaythroughArchived pa:
                    status = GameStatus.Archived;
                    if (pa.LastTownId is { } lastTownId)
                    {
                        currentTownId = lastTownId;
                        currentTownName = pa.LastTownName ?? currentTownName;
                    }
                    break;
            }
        }

        return new HudProjection(
            sessionId,
            status,
            playerName,
            health,
            walletCash,
            currentTownId,
            currentTownName,
            inventory.Select(kv => new HudInventoryItem(kv.Key, kv.Value)).ToArray());
    }

    private static void SyncInventoryFromJourneySnapshot(Dictionary<ItemKind, int> inventory, TravelJourneySnapshot snapshot)
    {
        if (snapshot.AvailableFood >= 0)
            inventory[ItemKind.Food] = snapshot.AvailableFood;

        if (snapshot.AvailableHorseFeed >= 0)
            inventory[ItemKind.HorseFeed] = snapshot.AvailableHorseFeed;

        // Canteen charges are tracked in the journey snapshot as AvailableCanteenCharges.
        // The canteen item itself is not consumed; only its charges change.
        // The HUD shows inventory quantities, not canteen charges, so we don't
        // update the canteen quantity here. Canteen charges are a derived HUD field
        // that would come from a separate projection if needed.
    }
}
