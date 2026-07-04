using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Factory methods for deterministic travel test scenarios.
/// All scenarios use <see cref="SaltSource.CreateDeterministic"/>
/// for reproducible day plans and encounter generation. Each factory returns
/// the session alongside the resolved <see cref="TravelPreview"/> so callers
/// can decide whether to start the journey via <see cref="GameSession.StartJourney"/>.
/// </summary>
internal static class TravelTestFactory
{
    /// <summary>
    /// Creates a session with a short low-risk journey from Current Town to Connected Town.
    /// Inventory: 4 Food, 1 Canteen (full 10), 1 Horse (Healthy), 1 Saddle.
    /// Wallet: $25. GameDifficulty: Easy.
    /// This reuses <see cref="TestSessionFactory.CreateDefault"/> world setup.
    /// </summary>
    internal static (GameSession session, TravelPreview preview) CreateEasyShortJourney()
    {
        var session = TestSessionFactory.CreateDefault();
        var preview = ResolvePreview(session, new TownId("connected"));
        return (session, preview);
    }

    /// <summary>
    /// Creates an EasyShortJourney session and captures the full setup event stream
    /// before it is committed. Used by replay-equality tests that need the full event stream.
    /// </summary>
    internal static (GameSession session, TravelPreview preview, IReadOnlyList<IDomainEvent> setupEvents)
        CreateEasyShortJourneyWithSetupEvents()
    {
        var (session, preview) = CreateEasyShortJourney();
        var setupEvents = RecaptureSetupEventsForReplay(session);
        return (session, preview, setupEvents);
    }

    /// <summary>
    /// Creates a SixDayQuietJourney session and captures the full setup event stream
    /// before it is committed. Used by replay-equality tests that need the full event stream.
    /// </summary>
    internal static (GameSession session, TravelPreview preview, IReadOnlyList<IDomainEvent> setupEvents)
        CreateSixDayQuietJourneyWithSetupEvents()
    {
        var (session, preview) = CreateSixDayQuietJourney();
        var setupEvents = RecaptureSetupEventsForReplay(session);
        return (session, preview, setupEvents);
    }

    /// <summary>
    /// Recaptures the full setup event stream for a session by re-running the canonical
    /// start flow (<see cref="TestSessionFactory.StartGameCanonical"/>) with the same
    /// world/case-file/inventory seed. The factory sessions already commit their setup
    /// events, so replay tests must prepend this stream to the journey event stream
    /// manually. Returns all 6 canonical setup events so the replayed session starts at
    /// the same version as the command session.
    /// </summary>
    internal static IReadOnlyList<IDomainEvent> RecaptureSetupEventsForReplay(GameSession session)
    {
        var seed = TestSessionFactory.StartGameCanonical(
            session.Player.Name,
            session.World,
            TestSessionFactory.CreateBaselineCaseFileFor(session),
            session.Player.CurrentTownId,
            session.Player.Wallet,
            session.Player.Inventory,
            session.GameDifficulty,
            session.SaltSource);
        return seed.UncommittedEvents;
    }

    /// <summary>
    /// Creates a session with a high-risk journey designed to trigger encounters.
    /// Uses TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, distance 6m.
    /// Inventory: 3 Food, 1 Canteen (full 2), 1 Horse (Healthy), 1 Saddle, 1 Knife.
    /// </summary>
    internal static (GameSession session, TravelPreview preview) CreateHighRiskJourney()
    {
        // Mirror the CreateHighRiskSession pattern from AdvanceTravelDayHandlerTests
        // but in the Domain test project (no handler, just session + preview).
        var pinecross = new Town(new TownId("pinecross"), "Pinecross",
            TownServices.Telegraph);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id,
                    TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, rideDayDistance: 6m)
            });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 3),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(2)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1),
        });

        var caseFile = new CaseFile(
            accusation: null,
            suspects: Array.Empty<Suspect>(),
            trueCulpritId: new SuspectId("suspect-1"),
            knownClues: Array.Empty<Clue>());

        var session = TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile,
            pinecross.Id, Wallet.Starting(25m), inventory,
            GameDifficulty.Easy,
            SaltSource.CreateFixed(string.Empty));
        session.MarkEventsCommitted();

        var preview = ResolvePreview(session, dryfork.Id);
        return (session, preview);
    }

    /// <summary>
    /// Creates a session with a low-risk journey designed to complete without interruption.
    /// Uses TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, ride-day distance 3m
    /// (foot travel). GameDifficulty.Easy. Inventory: 8 Food, 1 Canteen (6/10 charges),
    /// 1 Knife. The deterministic day-plan generator produces no choice-requiring encounters
    /// for this exact combination of trail id, town ids, terrain and difficulty, so the
    /// journey reliably reaches <see cref="JourneyStatus.Completed"/>.
    /// </summary>
    internal static (GameSession session, TravelPreview preview) CreateSixDayQuietJourney()
    {
        // The trail id, town ids, terrain and difficulty are tuned together so the
        // deterministic TravelDayPlanGenerator produces no Foe/Npc encounters across
        // the whole journey. Changing any of these values may reintroduce interruptions.
        var origin = new Town(new TownId("o2"), "Pinecross",
            TownServices.None);
        var destination = new Town(new TownId("d2"), "Six Mile", TownServices.None);
        var world = new DomainWorld(
            new[] { origin, destination },
            new[]
            {
                new Trail(new TrailId("trail-q-2"), origin.Id, destination.Id,
                    TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, rideDayDistance: 3m)
            });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 8),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: new CanteenState(6, 10)),
            new InventoryItem(ItemKind.Knife, 1),
        });

        var caseFile = new CaseFile(
            accusation: null,
            suspects: Array.Empty<Suspect>(),
            trueCulpritId: new SuspectId("suspect-1"),
            knownClues: Array.Empty<Clue>());

        var session = TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile,
            origin.Id, Wallet.Starting(25m), inventory,
            GameDifficulty.Easy,
            SaltSource.CreateFixed(string.Empty));
        session.MarkEventsCommitted();

        var preview = ResolvePreview(session, destination.Id);
        return (session, preview);
    }

    /// <summary>
    /// Resolves a deterministic <see cref="TravelPreview"/> for a journey from the session's
    /// current town to <paramref name="destinationId"/>. Throws if the preview cannot be resolved
    /// so test setup fails fast with a clear message rather than producing a null preview.
    /// </summary>
    private static TravelPreview ResolvePreview(GameSession session, TownId destinationId)
    {
        var resolver = new TravelResolver();
        var result = resolver.PreviewJourney(
            session.World,
            session.Player.CurrentTownId,
            destinationId,
            session.Player.Inventory,
            session.TravelRules);
        if (!result.Success || result.Preview is null)
        {
            throw new InvalidOperationException(
                $"Could not create journey preview: {result.Message}");
        }

        return result.Preview;
    }
}
