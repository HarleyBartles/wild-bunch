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
    private const int StaleComponentVersion = 0;
    private const string RebuiltDiaryEntry = "A sagebrush wind marked the day's quiet miles.";
    private const string CurrentCacheEntry = "Persist this current-version diary line.";
    private const string StaleCacheEntry = "This stale diary line must be discarded.";
    private const string MixedCacheEntry = "Discard this cached diary line.";

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

        var session = CreateSessionWithEvents();
        var events = session.UncommittedEvents.ToList();
        var staleCachedDay = projector.Project(events).Days.Single() with
        {
            Entries = [StaleCacheEntry]
        };
        var staleDays = new[]
        {
            new GameSessionDiaryDayEntity
            {
                SessionId = Guid.NewGuid(),
                Sequence = 0,
                PayloadJson = serializer.SerializeTravelDiaryDay(staleCachedDay),
                SchemaVersion = ProjectionVersions.DiaryDay - 1
            }
        };

        var result = loader.LoadDiaryDays(staleDays, events, events.Count, events.Count, staleDays.Length);

        AssertRebuiltDiaryDay(result);
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

        var session = CreateSessionWithEvents();
        var events = session.UncommittedEvents.ToList();
        var cachedCurrentDay = projector.Project(events).Days.Single() with
        {
            Entries = [MixedCacheEntry]
        };
        var mixedDays = new[]
        {
            new GameSessionDiaryDayEntity
            {
                SessionId = Guid.NewGuid(),
                Sequence = 0,
                PayloadJson = serializer.SerializeTravelDiaryDay(cachedCurrentDay),
                SchemaVersion = ProjectionVersions.DiaryDay
            },
            new GameSessionDiaryDayEntity
            {
                SessionId = Guid.NewGuid(),
                Sequence = 1,
                PayloadJson = "GARBAGE_JSON_WOULD_THROW_IF_DESERIALIZED",
                SchemaVersion = ProjectionVersions.DiaryDay - 1
            }
        };

        var result = loader.LoadDiaryDays(mixedDays, events, events.Count, events.Count, mixedDays.Length);

        AssertRebuiltDiaryDay(result);
        Assert.DoesNotContain(MixedCacheEntry, Assert.Single(result).Entries);
    }

    [Fact]
    public void LoadDiaryDays_CurrentVersion_UsesStoredJson()
    {
        var session = CreateSessionWithEvents();
        var events = session.UncommittedEvents.ToList();
        var serializer = new GameSessionJsonSerializer();
        var projector = new TravelDiaryDayProjector();
        var registry = new PayloadUpcasterRegistry([]);
        var loader = new PersistedPayloadLoader(
            registry, serializer, projector,
            _ => throw new InvalidOperationException("Should not be called for a current diary day."));
        var cachedDay = projector.Project(events).Days.Single() with
        {
            Entries = [CurrentCacheEntry]
        };
        var currentDays = new[]
        {
            new GameSessionDiaryDayEntity
            {
                SessionId = Guid.NewGuid(),
                Sequence = 0,
                PayloadJson = serializer.SerializeTravelDiaryDay(cachedDay),
                SchemaVersion = ProjectionVersions.DiaryDay
            }
        };

        var result = loader.LoadDiaryDays(currentDays, events, events.Count, events.Count, currentDays.Length);

        var loadedDay = Assert.Single(result);
        Assert.Equal(CurrentCacheEntry, Assert.Single(loadedDay.Entries));
        Assert.DoesNotContain(RebuiltDiaryEntry, loadedDay.Entries);
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
            rebuildSessionFromEvents: SessionRebuilder.RebuildForComponentCache);

        // Create a stale Player component (current is v1).
        var staleComponents = new Dictionary<string, GameSessionComponentEntity>
        {
            [GameSessionComponentNames.Player] = new()
            {
                SessionId = Guid.NewGuid(),
                ComponentName = GameSessionComponentNames.Player,
                PayloadJson = "{}",
                ComponentVersion = StaleComponentVersion
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
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var quartzsite = new Town(new TownId("quartzsite"), "Quartzsite");
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

        var previewResult = new TravelResolver().PreviewJourney(
            world,
            pinecross.Id,
            quartzsite.Id,
            session.Player.Inventory,
            session.TravelRules);
        Assert.True(previewResult.Success, previewResult.Message);
        var preview = Assert.IsType<TravelPreview>(previewResult.Preview);
        Assert.True(session.StartJourney(preview).Success);
        session.ForceDevTravelOverride(
            DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet, RebuiltDiaryEntry));
        Assert.True(session.AdvanceJourneyDay().Success);
        var travelDay = Assert.Single(session.UncommittedEvents.OfType<TravelDayAdvanced>());
        Assert.Contains(RebuiltDiaryEntry, travelDay.DayEntries);
        return session;
    }

    private static void AssertRebuiltDiaryDay(IReadOnlyList<TravelDiaryDayState> result)
    {
        var day = Assert.Single(result);
        Assert.Equal(1, day.DayNumber);
        Assert.Equal("Pinecross", day.OriginTownName);
        Assert.Equal("Quartzsite", day.DestinationTownName);
        Assert.Equal(-1, day.FoodDelta);
        Assert.Equal(3, day.CurrentFood);
        Assert.Equal(RebuiltDiaryEntry, Assert.Single(day.Entries));
    }
}
