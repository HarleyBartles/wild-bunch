using WildBunch.Domain.Cases;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Domain.Game;

/// <summary>
/// Mutable live play-state aggregate root.
/// Command handlers load and persist this root through <see cref="WildBunch.Application.Abstractions.IGameSessionRepository"/>.
/// </summary>
public sealed class GameSession : WildBunch.Domain.IAggregateRoot
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
        => StartNew(playerName, world, caseFile, startingTownId, wallet: null, inventory: null);

    public static GameSession StartNew(
        string playerName,
        DomainWorld world,
        CaseFile caseFile,
        TownId? startingTownId,
        WildBunch.Domain.Economy.Wallet? wallet,
        DomainInventory? inventory)
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
            wallet ?? WildBunch.Domain.Economy.Wallet.Starting(25m),
            inventory ?? DomainInventory.Empty());
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

    public void ApplyTravel(TownId destinationTownId, int heatIncrease, string message)
    {
        Player.TravelTo(destinationTownId);
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
