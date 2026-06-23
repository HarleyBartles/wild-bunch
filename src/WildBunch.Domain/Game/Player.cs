using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Domain.Game;

public sealed class Player
{
    public Player(string name, TownId currentTownId, int health, Wallet wallet, DomainInventory inventory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        CurrentTownId = currentTownId;
        Health = health;
        Wallet = wallet;
        Inventory = inventory;
    }

    public string Name { get; }

    public TownId CurrentTownId { get; private set; }

    public int Health { get; private set; }

    public Wallet Wallet { get; private set; }

    public DomainInventory Inventory { get; private set; }

    public bool CanAfford(decimal amount)
        => Wallet.CanAfford(amount);

    public void AdjustCash(decimal amount)
    {
        Wallet = Wallet.Adjust(amount);
    }

    /// <summary>
    /// Sets wallet cash to an absolute value. Used by event-sourced Apply methods
    /// that carry the absolute cash (e.g. TrailEventApplied) so command-path
    /// direct mutations and replay-path event applications converge.
    /// See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void SetCash(decimal value)
    {
        Wallet = new Wallet(value);
    }

    public void SpendCash(decimal amount)
    {
        Wallet = Wallet.Spend(amount);
    }

    public int GetQuantity(ItemKind kind)
        => Inventory.GetQuantity(kind);

    public bool HasItem(ItemKind kind)
        => Inventory.HasItem(kind);

    public HorseTravelState? GetHorseState()
        => Inventory.GetHorseState();

    public void SetHorseState(HorseTravelState horseState)
    {
        Inventory.SetHorseState(horseState);
    }

    public CanteenState? GetCanteenState()
        => Inventory.GetCanteenState();

    public void SetCanteenState(CanteenState canteenState)
    {
        Inventory.SetCanteenState(canteenState);
    }

    public void AddItem(ItemKind kind, int quantity, HorseTravelState? horseState = null, CanteenState? canteenState = null)
    {
        Inventory.AddItem(kind, quantity, horseState, canteenState);
    }

    public void RemoveQuantity(ItemKind kind, int quantity)
    {
        Inventory.RemoveQuantity(kind, quantity);
    }

    public InventoryCapabilities GetCapabilities(TravelRulesProfile? travelRulesProfile = null)
        => new InventoryCapabilityResolver().Resolve(Inventory, travelRulesProfile);

    public void TravelTo(TownId destinationTownId)
    {
        CurrentTownId = destinationTownId;
    }

    public void AdjustHealth(int amount)
    {
        Health += amount;
    }

    /// <summary>
    /// Sets health ABSOLUTELY. Used by Apply methods that carry absolute health
    /// snapshots so command-path direct mutations and replay-path event
    /// applications converge. See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void SetHealth(int value)
    {
        Health = value;
    }
}
