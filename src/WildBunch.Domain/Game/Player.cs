using WildBunch.Domain.Economy;
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

    public void TravelTo(TownId destinationTownId)
    {
        CurrentTownId = destinationTownId;
    }

    public void AdjustHealth(int amount)
    {
        Health += amount;
    }
}
