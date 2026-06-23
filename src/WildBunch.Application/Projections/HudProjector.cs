using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
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
}
