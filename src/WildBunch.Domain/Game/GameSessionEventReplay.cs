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

        // The first event may be PlayerSetupCompleted (start flow) or GameStarted (legacy/direct start).
        // For setup-phase sessions, we use the PlayerSetupCompleted event to construct a placeholder.
        // For GameStarted sessions, we use the GameStarted event as before.
        var firstEvent = events[0];

        var gameStarted = events.OfType<GameStarted>().FirstOrDefault();
        var setupCompleted = events.OfType<PlayerSetupCompleted>().FirstOrDefault();

        if (gameStarted is null && setupCompleted is null)
        {
            throw new ArgumentException(
                "Event stream must contain a PlayerSetupCompleted or GameStarted event.", nameof(events));
        }

        // Use GameStarted if available (it has the starting town), otherwise use PlayerSetupCompleted
        var playerName = gameStarted?.PlayerName ?? setupCompleted!.PlayerName;
        var startingTownId = gameStarted?.StartingTownId ?? world.Towns.First().Id;
        var startingHealth = gameStarted?.StartingHealth ?? 100; // Placeholder for setup-phase sessions
        var startingWallet = gameStarted?.StartingWallet ?? 25m;
        var gameDifficulty = gameStarted?.GameDifficulty ?? setupCompleted!.GameDifficulty;
        var saltSource = gameStarted?.SaltSource ?? SaltSource.CreateRuntime();
        var gameEntropy = gameStarted?.GameEntropy ?? setupCompleted!.GameEntropy;

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
            ApplyEvent(session, e);
        }

        // Loaded events are committed history, not uncommitted
        session.MarkEventsCommitted();
        return session;
    }

    /// <summary>
    /// Dispatches a typed domain event to the appropriate Apply method.
    /// Throws for unknown event types to prevent silent data loss.
    /// </summary>
    private static void ApplyEvent(GameSession session, IDomainEvent e)
    {
        switch (e)
        {
            case GameStarted gs:
                session.Apply(gs);
                break;
            case PlayerSetupCompleted psc:
                session.Apply(psc);
                break;
            case WorldGenerated wg:
                session.Apply(wg);
                break;
            case CaseFileGenerated cfg:
                session.Apply(cfg);
                break;
            case StartingTownSelected sts:
                session.Apply(sts);
                break;
            case PrologueViewed pv:
                session.Apply(pv);
                break;
            case PlaythroughArchived pa:
                session.Apply(pa);
                break;
            case StoreItemPurchased p:
                session.Apply(p);
                break;
            case InvestigationPerformed ip:
                session.Apply(ip);
                break;
            case TownActionContextEntered tc:
                session.Apply(tc);
                break;
            case SaloonPersonOfInterestSpotted sp:
                session.Apply(sp);
                break;
            case WantedSuspectConfronted wc:
                session.Apply(wc);
                break;
            case SheriffTurnInSettled ts:
                session.Apply(ts);
                break;
            case UnrelatedCriminalTurnInSettled ucts:
                session.Apply(ucts);
                break;
            case SaloonPersonOfInterestConfronted sc:
                session.Apply(sc);
                break;
            case JourneyStarted js:
                session.Apply(js);
                break;
            case TravelDayAdvanced tda:
                session.Apply(tda);
                break;
            case TrailEventApplied tea:
                session.Apply(tea);
                break;
            case JourneyEncounterResolved jer:
                session.Apply(jer);
                break;
            case JourneyCompleted jc:
                session.Apply(jc);
                break;
            case JourneyArrivalAcknowledged jaa:
                session.Apply(jaa);
                break;
            case DevTravelOverrideForced dtf:
                session.Apply(dtf);
                break;
            case DevTravelOverrideCleared dtc:
                session.Apply(dtc);
                break;
            case DevTravelOverrideConsumed dtc2:
                session.Apply(dtc2);
                break;
            case DevSaloonOverrideForced dsf:
                session.Apply(dsf);
                break;
            case DevSaloonOverrideCleared dsc:
                session.Apply(dsc);
                break;
            case DevSaloonOverrideConsumed dsc2:
                session.Apply(dsc2);
                break;
            case DevSaltSourceForced dsf:
                session.Apply(dsf);
                break;
            case DevSaltSourceCleared dsc:
                session.Apply(dsc);
                break;
            case DevDifficultyForced ddf:
                session.Apply(ddf);
                break;
            case DevEntropyChanged dec:
                session.Apply(dec);
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
            ApplyEvent(this, e);
        }
    }
}
