using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryCapabilities = WildBunch.Domain.Inventory.InventoryCapabilities;
using DomainInventoryCapabilityResolver = WildBunch.Domain.Inventory.InventoryCapabilityResolver;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Domain.Game;

public sealed class GameSession
{
    private readonly List<GameLogEntry> _logEntries = [];

    private GameSession(
        GameSessionId id,
        Player player,
        DomainWorld world,
        CaseFile caseFile,
        PursuitState pursuitState,
        GameClock clock,
        GameStatus status)
    {
        Id = id;
        Player = player;
        World = world;
        CaseFile = caseFile;
        PursuitState = pursuitState;
        Clock = clock;
        Status = status;
    }

    public GameSessionId Id { get; }

    public GameStatus Status { get; private set; }

    public Player Player { get; private set; }

    public DomainWorld World { get; }

    public CaseFile CaseFile { get; }

    public PursuitState PursuitState { get; }

    public GameClock Clock { get; }

    public IReadOnlyList<GameLogEntry> LogEntries => _logEntries;

    public static GameSession StartNew(string playerName, DomainWorld world, CaseFile caseFile, TownId? startingTownId = null)
        => StartNew(playerName, world, caseFile, startingTownId, wallet: null, inventory: null, supplies: null);

    public static GameSession StartNew(
        string playerName,
        DomainWorld world,
        CaseFile caseFile,
        TownId? startingTownId,
        Wallet? wallet,
        DomainInventory? inventory,
        Supplies? supplies)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(caseFile);

        var resolvedTownId = startingTownId ?? world.Towns.First().Id;
        var startingTown = world.GetTown(resolvedTownId);
        var player = new Player(
            playerName,
            startingTown.Id,
            health: 100,
            wallet ?? Wallet.Starting(25m),
            inventory ?? DomainInventory.Empty(),
            supplies ?? Supplies.Starting());
        var session = new GameSession(
            GameSessionId.New(),
            player,
            world,
            caseFile,
            new PursuitState(),
            new GameClock(),
            GameStatus.Active);

        session.AddLogEntry(GameLogEntryKind.Opening, $"The hunt begins in {startingTown.Name}.");
        return session;
    }

    public void ApplyTravel(TownId destinationTownId, int supplyCost, int heatIncrease, string message)
    {
        Player.TravelTo(destinationTownId, supplyCost);
        Clock.Advance();
        PursuitState.IncreaseHeat(heatIncrease);
        AddLogEntry(GameLogEntryKind.Travel, message);
    }

    public void ApplyCaseUpdate(string message, bool advanceClock = true)
    {
        if (advanceClock)
        {
            Clock.Advance();
        }

        AddLogEntry(GameLogEntryKind.CaseUpdate, message);
    }

    public void CompleteCase(string message)
    {
        Status = GameStatus.Completed;
        AddLogEntry(GameLogEntryKind.CaseUpdate, message);
    }

    private void AddLogEntry(GameLogEntryKind kind, string message)
    {
        _logEntries.Add(new GameLogEntry(kind, message, Clock.Day, Clock.Turn));
    }
}

public readonly record struct GameSessionId(Guid Value)
{
    public static GameSessionId New() => new(Guid.NewGuid());
}

public enum GameStatus
{
    Active = 0,
    Completed = 1,
    Failed = 2
}

public sealed class Player
{
    public Player(string name, World.TownId currentTownId, int health, Wallet wallet, DomainInventory inventory, Supplies supplies)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        CurrentTownId = currentTownId;
        Health = health;
        Wallet = wallet;
        Inventory = inventory;
        Supplies = supplies;
    }

    public string Name { get; }

    public World.TownId CurrentTownId { get; private set; }

    public int Health { get; private set; }

    public Wallet Wallet { get; private set; }

    public Supplies Supplies { get; private set; }

    public decimal Money => Wallet.Cash;

    public DomainInventory Inventory { get; private set; }

    public DomainInventoryCapabilities Capabilities => new DomainInventoryCapabilityResolver().Resolve(Inventory);

    public void TravelTo(World.TownId destinationTownId, int supplyCost)
    {
        SpendSupplies(supplyCost);
        CurrentTownId = destinationTownId;
    }

    public void SpendSupplies(int amount)
    {
        Supplies = Supplies.Subtract(amount);
    }

    public void AdjustHealth(int amount)
    {
        Health += amount;
    }

    public void AdjustMoney(decimal amount)
    {
        Wallet = Wallet.Adjust(amount);
    }
}

public readonly record struct Supplies(int Units)
{
    public static Supplies Starting() => new(12);

    public bool CanAfford(int amount) => amount >= 0 && Units >= amount;

    public Supplies Subtract(int amount)
    {
        if (!CanAfford(amount))
        {
            throw new InvalidOperationException("Not enough supplies.");
        }

        return new Supplies(Units - amount);
    }
}

public sealed class PursuitState
{
    public int Heat { get; private set; }

    public void IncreaseHeat(int amount)
    {
        Heat = Math.Max(0, Heat + amount);
    }
}

public sealed class GameClock
{
    public int Day { get; private set; } = 1;

    public int Turn { get; private set; }

    public void Advance()
    {
        Turn++;

        if (Turn >= 4)
        {
            Day++;
            Turn = 0;
        }
    }
}

public sealed record GameLogEntry(GameLogEntryKind Kind, string Message, int Day, int Turn);

public enum GameLogEntryKind
{
    Opening = 0,
    Travel = 1,
    CaseUpdate = 2
}
