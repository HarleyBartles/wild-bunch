using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Application.Projections;

/// <summary>
/// HUD projection: the player-facing heads-up display state derived from domain events.
/// This is a read-only projection — it does not mutate aggregate state.
/// See ADR-0028.
/// </summary>
public sealed record HudProjection(
    Guid SessionId,
    GameStatus Status,
    string PlayerName,
    int Health,
    decimal WalletCash,
    TownId CurrentTownId,
    string CurrentTownName,
    IReadOnlyList<HudInventoryItem> InventoryItems) : IProjectionResult;

public sealed record HudInventoryItem(
    ItemKind ItemKind,
    int Quantity);
