using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Domain.Game;

/// <summary>
/// Event-sourced replay support for <see cref="GameSession"/>.
/// Contains the <see cref="RehydrateFromEvents"/> factory and event dispatch helper.
/// See ADR-0028 for the event-sourcing posture.
/// </summary>
public sealed partial class GameSession
{
    /// <summary>
    /// Reconstructs a <see cref="GameSession"/> from typed domain events by replaying
    /// them through <see cref="Apply"/> in order. This is the material Event Sourcing
    /// proof: the event stream reconstructs state without the snapshot.
    /// </summary>
    /// <param name="id">The session id.</param>
    /// <param name="world">The world definition (external reference, not stored in events).</param>
    /// <param name="events">The typed domain events to replay.</param>
    /// <returns>A session whose migrated state matches what the command path would produce.</returns>
    public static GameSession RehydrateFromEvents(
        GameSessionId id,
        DomainWorld world,
        IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            throw new ArgumentException("Cannot rehydrate a session from an empty event stream.", nameof(events));
        }

        var gameStarted = events.OfType<GameStarted>().FirstOrDefault();
        var setupCompleted = events.OfType<PlayerSetupCompleted>().FirstOrDefault();
        var worldGenerated = events.OfType<WorldGenerated>().FirstOrDefault();

        if (gameStarted is null && setupCompleted is null)
        {
            throw new ArgumentException(
                "Event stream must contain a PlayerSetupCompleted or GameStarted event.", nameof(events));
        }

        if (setupCompleted is not null && worldGenerated is null)
        {
            throw new InvalidOperationException(
                "Cannot replay player setup without a WorldGenerated event.");
        }

        // Use GameStarted if available (it has the starting town), otherwise use PlayerSetupCompleted.
        // For setup-phase sessions (no GameStarted), the salt source comes from the WorldGenerated
        // event — not a runtime fallback. Apply(WorldGenerated) will also restore it during replay,
        // but the constructor needs the correct value upfront so CompleteGameStart() reads the right
        // salt before any Apply runs.
        var playerName = gameStarted?.PlayerName ?? setupCompleted!.PlayerName;
        var startingTownId = gameStarted?.StartingTownId ?? world.Towns.First().Id;
        var startingHealth = gameStarted?.StartingHealth ?? 100; // Placeholder for setup-phase sessions
        var startingWallet = gameStarted?.StartingWallet ?? 25m;
        var gameDifficulty = gameStarted?.GameDifficulty ?? setupCompleted!.GameDifficulty;
        var saltSource = gameStarted?.SaltSource
            ?? worldGenerated?.SaltSource
            ?? throw new InvalidOperationException(
                "Cannot replay a session without a recorded salt source.");
        var gameEntropy = gameStarted?.GameEntropy
            ?? worldGenerated?.GameEntropy
            ?? setupCompleted!.GameEntropy;

        var placeholderPlayer = new Player(
            playerName,
            startingTownId,
            health: startingHealth,
            Wallet.Starting(startingWallet),
            DomainInventory.Empty());

        var caseFile = new CaseFile(
            null,
            Array.Empty<Suspect>(),
            new SuspectId("placeholder"),
            CaseOpeningLead.Create("placeholder"),
            Array.Empty<Clue>());

        var session = new GameSession(
            id,
            placeholderPlayer,
            world,
            caseFile,
            new PursuitState(),
            new GameClock(),
            GameStatus.Active,
            journey: null,
            gameDifficulty,
            saltSource,
            gameEntropy,
            currentTownVisit: null,
            Array.Empty<TravelJourneySnapshot>(),
            Array.Empty<WantedSuspectPresenceEntry>());

        // Replay all events through Apply
        foreach (var e in events)
        {
            session.ApplyEvent(e);
        }

        var missingCaseFileWorldIndex = events.ToList().FindIndex(
            e => e is WorldGenerated { CaseFile: null });
        if (missingCaseFileWorldIndex >= 0
            && !events.Skip(missingCaseFileWorldIndex + 1).OfType<CaseFileGenerated>().Any())
        {
            throw new InvalidOperationException(
                "Cannot replay a legacy WorldGenerated event without a CaseFileGenerated event.");
        }

        // Loaded events are committed history, not uncommitted
        session.MarkEventsCommitted();
        return session;
    }

    /// <summary>
    /// Dispatches a typed domain event to the appropriate Apply method.
    /// Throws for unknown event types to prevent silent data loss.
    /// </summary>
    private void ApplyEvent(IDomainEvent e)
    {
        switch (e)
        {
            case GameStarted gs:
                Apply(gs);
                break;
            case PlayerSetupCompleted psc:
                Apply(psc);
                break;
            case WorldGenerated wg:
                Apply(wg);
                break;
            case CaseFileGenerated cfg:
                Apply(cfg);
                break;
            case StartingTownSelected sts:
                Apply(sts);
                break;
            case PrologueViewed pv:
                Apply(pv);
                break;
            case PlaythroughArchived pa:
                Apply(pa);
                break;
            case PlayerDied pd:
                Apply(pd);
                break;
            case StoreItemPurchased p:
                Apply(p);
                break;
            case InvestigationPerformed ip:
                Apply(ip);
                break;
            case TownActionContextEntered tc:
                Apply(tc);
                break;
            case SaloonPersonOfInterestSpotted sp:
                Apply(sp);
                break;
            case WantedSuspectConfronted wc:
                Apply(wc);
                break;
            case SheriffTurnInSettled ts:
                Apply(ts);
                break;
            case SaloonPersonOfInterestConfronted sc:
                Apply(sc);
                break;
            case JourneyStarted js:
                Apply(js);
                break;
            case TravelDayAdvanced tda:
                Apply(tda);
                break;
            case TrailEventApplied tea:
                Apply(tea);
                break;
            case JourneyEncounterResolved jer:
                Apply(jer);
                break;
            case JourneyCompleted jc:
                Apply(jc);
                break;
            case JourneyArrivalAcknowledged jaa:
                Apply(jaa);
                break;
            case DevTravelOverrideForced dtf:
                Apply(dtf);
                break;
            case DevTravelOverrideCleared dtc:
                Apply(dtc);
                break;
            case DevTravelOverrideConsumed dtc2:
                Apply(dtc2);
                break;
            case DevSaloonOverrideForced dsf:
                Apply(dsf);
                break;
            case DevSaloonOverrideCleared dsc:
                Apply(dsc);
                break;
            case DevSaloonOverrideConsumed dsc2:
                Apply(dsc2);
                break;
            case DevDifficultyForced ddf:
                Apply(ddf);
                break;
            case DevEntropyChanged dec:
                Apply(dec);
                break;
            default:
                throw new InvalidOperationException($"Unknown domain event type: {e.GetType().Name}");
        }
    }

    /// <summary>
    /// Applies committed events that occurred after the snapshot was taken.
    /// Used by the repository load path when SnapshotVersion &lt; StreamVersion.
    /// Events are applied through Apply (the single mutation path) but are
    /// not added to UncommittedEvents (they are already committed history).
    /// </summary>
    internal void ApplyCommittedEvents(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (var e in events)
        {
            ApplyEvent(e);
        }
    }
}
