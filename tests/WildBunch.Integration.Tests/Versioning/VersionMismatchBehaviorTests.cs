using WildBunch.Application.Projections;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using WildBunch.Persistence.Versioning;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Integration.Tests.Versioning;

/// <summary>
/// Tests version mismatch behavior: stale projections trigger rebuild,
/// future event versions throw (fail-closed). See the event sourcing
/// integrity policy and spec Part 2e test 7.
/// </summary>
public sealed class VersionMismatchBehaviorTests
{
    [Fact]
    public void LoadEvent_FutureVersion_Throws()
    {
        var registry = new PayloadUpcasterRegistry([]);
        var serializer = new GameSessionJsonSerializer();
        var loader = new PersistedPayloadLoader(
            registry, serializer, new TravelDiaryDayProjector(),
            _ => throw new InvalidOperationException("Should not be called."));

        var stored = new StoredEventEntity
        {
            EventType = "GameStarted",
            SchemaVersion = 2,  // future version — code supports up to v1
            PayloadJson = "{}"
        };

        Assert.Throws<InvalidOperationException>(() => loader.LoadEvent(stored));
    }

    [Fact]
    public void LoadDiaryDays_StaleVersion_TriggersRebuildFromEvents()
    {
        var registry = new PayloadUpcasterRegistry([]);
        var serializer = new GameSessionJsonSerializer();
        var projector = new TravelDiaryDayProjector();
        var loader = new PersistedPayloadLoader(
            registry, serializer, projector,
            _ => throw new InvalidOperationException("Should not be called for diary days."));

        // A diary day entity with a stale version (v99 — current is v1) and
        // garbage JSON that would throw if the current-version path were taken.
        var staleDays = new[]
        {
            new GameSessionDiaryDayEntity
            {
                SessionId = Guid.NewGuid(),
                Sequence = 0,
                PayloadJson = "THIS_IS_GARBAGE_NOT_VALID_JSON_FOR_DIARY_DAY",
                SchemaVersion = 99  // stale — use v99 to ensure it's stale (current is v1)
            }
        };

        // Use a real event stream so the projector has something to project.
        var session = CreateSessionWithEvents();
        var events = session.UncommittedEvents.ToList();

        // If the stale path is taken: the garbage JSON is discarded, the projector
        // runs on the events, and returns its output (empty for non-journey events).
        // If the current path were taken: deserializing the garbage JSON would throw.
        var result = loader.LoadDiaryDays(staleDays, events);

        // The result must match what the projector produces directly — proving
        // the rebuild path was taken, not the stored-JSON path.
        var expectedDays = projector.Project(events).Days;
        Assert.Equal(expectedDays.Count, result.Count);
    }

    [Fact]
    public void LoadDiaryDays_MixedStaleAndCurrent_TriggersFullRebuild()
    {
        var registry = new PayloadUpcasterRegistry([]);
        var serializer = new GameSessionJsonSerializer();
        var projector = new TravelDiaryDayProjector();
        var loader = new PersistedPayloadLoader(
            registry, serializer, projector,
            _ => throw new InvalidOperationException("Should not be called for diary days."));

        // Mix of current (v1) and stale (v99) diary days.
        // The loader uses All() — if ANY are stale, ALL are discarded and rebuilt.
        // The current day has valid JSON, but the stale day has garbage JSON.
        // If the current path were taken for the current day, it would succeed,
        // but the stale day would throw. The All() check prevents this — all
        // days are discarded and rebuilt from events.
        var mixedDays = new[]
        {
            new GameSessionDiaryDayEntity
            {
                SessionId = Guid.NewGuid(),
                Sequence = 0,
                PayloadJson = serializer.SerializeTravelDiaryDay(new TravelDiaryDayState(
                    1, "Pinecross", "Dry Fork", TravelMode.Mounted, TravelMode.Mounted,
                    JourneyStatus.Active, 3m, 3m, 4, 4, null, null, null, null, null,
                    null, null, null, Entries: Array.Empty<string>(),
                    HealthDelta: 0, WalletDelta: 0m, FoodDelta: 0,
                    HorseFeedDelta: 0, CanteenChargeDelta: 0, AmmoSpent: 0,
                    HorseHungerDelta: 0, HorseThirstDelta: 0, HorseExhaustionDelta: 0,
                    DelayDays: 0, HeatIncrease: 0, CurrentHealth: 1000, CurrentWallet: 25m,
                    CurrentFood: 3, CurrentHorseFeed: 0, CurrentCanteenCharges: 2,
                    CurrentAmmo: 0, CurrentHeat: 0, Warnings: Array.Empty<string>())
                {
                    Terrain = TrailTerrain.OpenRange, RouteWaterSecure = true, CanteenChargesPerDay = 0
                }),
                SchemaVersion = ProjectionVersions.DiaryDay  // current
            },
            new GameSessionDiaryDayEntity
            {
                SessionId = Guid.NewGuid(),
                Sequence = 1,
                PayloadJson = "GARBAGE_JSON_WOULD_THROW_IF_DESERIALIZED",
                SchemaVersion = 99  // stale
            }
        };

        var session = CreateSessionWithEvents();
        var events = session.UncommittedEvents.ToList();

        // If the All() check works: all days discarded, projector runs, returns its output.
        // If the All() check were broken (e.g., changed to Any()): the current day would
        // be deserialized from stored JSON, and the stale day would throw.
        var result = loader.LoadDiaryDays(mixedDays, events);

        // All days should be discarded and rebuilt from events.
        var expectedDays = projector.Project(events).Days;
        Assert.Equal(expectedDays.Count, result.Count);
        // The current day's stored data should NOT appear in the result (it was discarded).
        Assert.DoesNotContain(result, d => d.OriginTownName == "Pinecross" && d.DestinationTownName == "Dry Fork");
    }

    [Fact]
    public void LoadDiaryDays_CurrentVersion_UsesStoredJson()
    {
        var registry = new PayloadUpcasterRegistry([]);
        var serializer = new GameSessionJsonSerializer();
        var projector = new TravelDiaryDayProjector();
        var loader = new PersistedPayloadLoader(
            registry, serializer, projector,
            _ => throw new InvalidOperationException("Should not be called."));

        var day = new TravelDiaryDayState(
            1, "Pinecross", "Dry Fork",
            TravelMode.Mounted, TravelMode.Mounted,
            JourneyStatus.Active,
            3m, 3m, 4, 4,
            null, null, null, null, null,
            null, null, null,
            Entries: Array.Empty<string>(),
            HealthDelta: 0, WalletDelta: 0m, FoodDelta: 0,
            HorseFeedDelta: 0, CanteenChargeDelta: 0, AmmoSpent: 0,
            HorseHungerDelta: 0, HorseThirstDelta: 0, HorseExhaustionDelta: 0,
            DelayDays: 0, HeatIncrease: 0,
            CurrentHealth: 1000, CurrentWallet: 25m,
            CurrentFood: 3, CurrentHorseFeed: 0,
            CurrentCanteenCharges: 2, CurrentAmmo: 0,
            CurrentHeat: 0, Warnings: Array.Empty<string>())
        {
            Terrain = TrailTerrain.OpenRange,
            RouteWaterSecure = true,
            CanteenChargesPerDay = 0
        };

        var dayJson = serializer.SerializeTravelDiaryDay(day);
        var currentDays = new[]
        {
            new GameSessionDiaryDayEntity
            {
                SessionId = Guid.NewGuid(),
                Sequence = 0,
                PayloadJson = dayJson,
                SchemaVersion = ProjectionVersions.DiaryDay  // current
            }
        };

        var result = loader.LoadDiaryDays(currentDays, Array.Empty<IDomainEvent>());

        Assert.Single(result);
        Assert.Equal(day.DayNumber, result[0].DayNumber);
        Assert.Equal(day.OriginTownName, result[0].OriginTownName);
    }

    [Fact]
    public void LoadComponentPayload_StaleVersion_TriggersRebuildFromEvents()
    {
        // Build a valid event stream that produces a GameSession, then verify
        // that a stale component version triggers the rebuild callback and
        // returns the correct component JSON.
        var session = CreateSessionWithEvents();
        var events = session.UncommittedEvents.ToList();

        var registry = new PayloadUpcasterRegistry([]);
        var serializer = new GameSessionJsonSerializer();
        var projector = new TravelDiaryDayProjector();
        var loader = new PersistedPayloadLoader(
            registry, serializer, projector,
            rebuildSessionFromEvents: evts => SessionRebuilder.RebuildFromEvents(evts, serializer));

        // Create a stale Player component (v99 — current is v1).
        var staleComponents = new Dictionary<string, GameSessionComponentEntity>
        {
            [GameSessionComponentNames.Player] = new()
            {
                SessionId = Guid.NewGuid(),
                ComponentName = GameSessionComponentNames.Player,
                PayloadJson = "{}",
                ComponentVersion = 99  // stale
            }
        };

        var result = loader.LoadComponentPayload(staleComponents, GameSessionComponentNames.Player, events);

        // The rebuild callback should have been called, producing valid Player JSON.
        Assert.NotNull(result);
        Assert.NotEqual("{}", result);

        // Verify the rebuilt JSON deserializes to the correct player name.
        var rebuiltPlayer = serializer.DeserializePlayer(result);
        Assert.Equal("Ranger Vale", rebuiltPlayer.Name);
    }

    [Fact]
    public void LoadComponentPayload_CurrentVersion_UsesStoredJson()
    {
        var session = CreateSessionWithEvents();
        var events = session.UncommittedEvents.ToList();

        var registry = new PayloadUpcasterRegistry([]);
        var serializer = new GameSessionJsonSerializer();
        var projector = new TravelDiaryDayProjector();
        var loader = new PersistedPayloadLoader(
            registry, serializer, projector,
            rebuildSessionFromEvents: _ => throw new InvalidOperationException("Should not be called."));

        var playerJson = serializer.SerializePlayer(session.Player);
        var currentComponents = new Dictionary<string, GameSessionComponentEntity>
        {
            [GameSessionComponentNames.Player] = new()
            {
                SessionId = Guid.NewGuid(),
                ComponentName = GameSessionComponentNames.Player,
                PayloadJson = playerJson,
                ComponentVersion = ProjectionVersions.ForComponent(GameSessionComponentNames.Player)  // current
            }
        };

        var result = loader.LoadComponentPayload(currentComponents, GameSessionComponentNames.Player, events);

        Assert.Equal(playerJson, result);
    }

    [Fact]
    public void LoadComponentPayload_MissingComponent_ReturnsNull()
    {
        var registry = new PayloadUpcasterRegistry([]);
        var serializer = new GameSessionJsonSerializer();
        var projector = new TravelDiaryDayProjector();
        var loader = new PersistedPayloadLoader(
            registry, serializer, projector,
            rebuildSessionFromEvents: _ => throw new InvalidOperationException("Should not be called."));

        var emptyComponents = new Dictionary<string, GameSessionComponentEntity>();

        var result = loader.LoadComponentPayload(emptyComponents, GameSessionComponentNames.Player, Array.Empty<IDomainEvent>());

        Assert.Null(result);
    }

    private static GameSession CreateSessionWithEvents()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var quartzsite = new Town(new TownId("quartzsite"), "Quartzsite", TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, quartzsite },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, quartzsite.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint",
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };
        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 4),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Easy, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }
}
