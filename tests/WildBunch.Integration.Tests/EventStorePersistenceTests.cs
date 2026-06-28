using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Integration.Tests;

public sealed class EventStorePersistenceTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private readonly PostgreSqlPersistenceFixture _fixture;

    public EventStorePersistenceTests(PostgreSqlPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StoreAsync_AppendsEventsToStoredEventsTable()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();

        var session = CreateSession();
        await repo.StoreAsync(session);
        await uow.CommitAsync();

        var storedEvents = await dbContext.StoredEvents.AsNoTracking()
            .Where(e => e.StreamId == session.Id.Value)
            .OrderBy(e => e.Sequence)
            .ToArrayAsync();

        Assert.Single(storedEvents);
        Assert.Equal("GameStarted", storedEvents[0].EventType);
        Assert.Equal(1, storedEvents[0].Sequence);
    }

    [Fact]
    public async Task StoreAsync_PurchaseAppendsStoreItemPurchasedEvent()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();

        var session = CreateSession();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Reload and purchase
        var loaded = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(loaded);
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        loaded.Purchase(offer, 2);

        await repo.StoreAsync(loaded);
        await uow.CommitAsync();

        var storedEvents = await dbContext.StoredEvents.AsNoTracking()
            .Where(e => e.StreamId == session.Id.Value)
            .OrderBy(e => e.Sequence)
            .ToArrayAsync();

        Assert.Equal(2, storedEvents.Length);
        Assert.Equal("GameStarted", storedEvents[0].EventType);
        Assert.Equal("StoreItemPurchased", storedEvents[1].EventType);
        Assert.Equal(2, storedEvents[1].Sequence);
    }

    [Fact]
    public async Task StoreAsync_ThrowsConcurrencyException_OnVersionMismatch()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        var session = CreateSession();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Load two copies (simulating concurrent access)
        var copy1 = await repo.GetByIdAsync(session.Id);
        var copy2 = await repo.GetByIdAsync(session.Id);

        // First copy purchases
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(copy1!.World.GetTown(copy1.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        copy1.Purchase(offer, 1);
        await repo.StoreAsync(copy1);
        await uow.CommitAsync();
        copy1.MarkEventsCommitted();

        // Second copy tries to purchase — should get ConcurrencyException
        var offer2 = resolver.Resolve(copy2!.World.GetTown(copy2.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        copy2.Purchase(offer2, 1);

        await Assert.ThrowsAsync<ConcurrencyException>(() => repo.StoreAsync(copy2));
    }

    [Fact]
    public async Task GetEventStreamAsync_ReturnsTypedEventsInOrder()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        var session = CreateSession();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        var loaded = await repo.GetByIdAsync(session.Id);
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        loaded.Purchase(offer, 3);
        await repo.StoreAsync(loaded);
        await uow.CommitAsync();

        var events = await repo.GetEventStreamAsync(session.Id);

        Assert.Equal(2, events.Count);
        Assert.IsType<GameStarted>(events[0]);
        Assert.IsType<StoreItemPurchased>(events[1]);
        var purchase = (StoreItemPurchased)events[1];
        Assert.Equal(3, purchase.Quantity);
    }

    [Fact]
    public async Task GetEventStreamAsync_FromVersion_ReturnsOnlyEventsAfterThatVersion()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        var session = CreateSession();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        var loaded = await repo.GetByIdAsync(session.Id);
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        loaded.Purchase(offer, 1);
        await repo.StoreAsync(loaded);
        await uow.CommitAsync();

        // Get events after version 1 (the GameStarted)
        var events = await repo.GetEventStreamAsync(session.Id, fromVersion: 1);

        Assert.Single(events);
        Assert.IsType<StoreItemPurchased>(events[0]);
    }

    /// <summary>
    /// Proves that <see cref="InvestigationPerformed"/> events are serialized, persisted,
    /// and deserialized through the DB-backed event stream. Without the
    /// <see cref="GameSessionJsonSerializer.ResolveEventType"/> mapping for
    /// <see cref="InvestigationPerformed"/>, <see cref="EfGameSessionRepository.GetEventStreamAsync"/>
    /// would throw "Unknown domain event type: InvestigationPerformed".
    /// </summary>
    [Fact]
    public async Task GetEventStreamAsync_ReturnsInvestigationPerformed_AfterPersistedInvestigation()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();

        // 1. Create + store + commit (GameStarted)
        var session = CreateSession();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // 2. Reload + perform investigation + store + commit (TownActionContextEntered + InvestigationPerformed)
        var loaded = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(loaded);
        var investigationResult = loaded!.GatherLocalGossip();
        Assert.True(investigationResult.Success);
        Assert.True(investigationResult.SessionChanged);
        // GatherLocalGossip now enters Saloon context first (TownActionContextEntered),
        // then produces InvestigationPerformed — 2 uncommitted events
        Assert.Equal(2, loaded.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(loaded.UncommittedEvents[0]);
        Assert.IsType<InvestigationPerformed>(loaded.UncommittedEvents[1]);

        await repo.StoreAsync(loaded);
        await uow.CommitAsync();

        // 3. Verify the stored event rows have the correct type names
        var storedEvents = await dbContext.StoredEvents.AsNoTracking()
            .Where(e => e.StreamId == session.Id.Value)
            .OrderBy(e => e.Sequence)
            .ToArrayAsync();
        Assert.Equal(3, storedEvents.Length);
        Assert.Equal("GameStarted", storedEvents[0].EventType);
        Assert.Equal("TownActionContextEntered", storedEvents[1].EventType);
        Assert.Equal("InvestigationPerformed", storedEvents[2].EventType);

        // 4. GetEventStreamAsync must deserialize all events without throwing
        var events = await repo.GetEventStreamAsync(session.Id);
        Assert.Equal(3, events.Count);
        Assert.IsType<GameStarted>(events[0]);
        Assert.IsType<TownActionContextEntered>(events[1]);
        var investigationEvent = Assert.IsType<InvestigationPerformed>(events[2]);
        Assert.Equal(InvestigationSourceKind.LocalGossip, investigationEvent.SourceKind);
    }

    [Fact]
    public async Task GetEventStreamAsync_ReturnsBountySaloonEvents_AfterPersisted()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // 1. Create + store + commit (GameStarted)
        var session = CreateSessionWithWarrantedSaloonSuspect();
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-1"), WantedSuspectPresenceState.AvailableInTown);
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // 2. Reload + LookAroundSaloon + store + commit
        var loaded = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(loaded);
        var lookResult = loaded!.LookAroundSaloon();
        Assert.True(lookResult.Success);
        // TownActionContextEntered + SaloonPersonOfInterestSpotted
        Assert.Equal(2, loaded.UncommittedEvents.Count);

        await repo.StoreAsync(loaded);
        await uow.CommitAsync();

        // 3. Verify the event stream deserializes correctly
        var events = await repo.GetEventStreamAsync(session.Id);
        Assert.Equal(3, events.Count);
        Assert.IsType<GameStarted>(events[0]);
        Assert.IsType<TownActionContextEntered>(events[1]);
        var spottedEvent = Assert.IsType<SaloonPersonOfInterestSpotted>(events[2]);
        Assert.Equal(InvestigationSourceKind.SaloonLookAround, spottedEvent.SourceKind);
    }

    [Fact]
    public async Task SnapshotLoad_PreservesCurrentActionContext()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // 1. Create + store + commit
        var session = CreateSessionWithWarrantedSaloonSuspect();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // 2. Reload + LookAroundSaloon (enters Saloon context) + store + commit
        var loaded = await repo.GetByIdAsync(session.Id);
        var lookResult = loaded!.LookAroundSaloon();
        Assert.True(lookResult.Success, $"LookAroundSaloon failed: {lookResult.Message}");
        await repo.StoreAsync(loaded);
        await uow.CommitAsync();

        // 3. Reload from snapshot — CurrentActionContext should be Saloon
        var reloaded = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(TownActionContext.Saloon, reloaded!.CurrentActionContext);
    }

    [Fact]
    public async Task ReplayFromEvents_ReconstructsSameStateAsSnapshotLoad()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        var session = CreateSession();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        var loaded = await repo.GetByIdAsync(session.Id);
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        loaded.Purchase(offer, 3);
        await repo.StoreAsync(loaded);
        await uow.CommitAsync();

        // Load from snapshot
        var fromSnapshot = await repo.GetByIdAsync(session.Id);

        // Load from event stream (full replay)
        var events = await repo.GetEventStreamAsync(session.Id);
        var fromEvents = GameSession.RehydrateFromEvents(
            session.Id,
            fromSnapshot!.World,
            fromSnapshot.CaseFile,
            events);

        // State equality proof
        Assert.Equal(fromSnapshot.Player.Wallet.Cash, fromEvents.Player.Wallet.Cash);
        Assert.Equal(fromSnapshot.Player.Inventory.GetQuantity(DomainItemKind.Food), fromEvents.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(fromSnapshot.Version, fromEvents.Version);
    }

    [Fact]
    public async Task StoreAsync_DoesNotCallSaveChangesAsync_Directly()
    {
        // This is a structural proof: the StoreAsync method stages on DbContext
        // but does not call SaveChangesAsync. The UoW commits.
        // We verify by checking that StoreAsync alone does not persist.
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();

        var session = CreateSession();
        await repo.StoreAsync(session);

        // Without calling CommitAsync, the data should not be in the database
        var entity = await dbContext.GameSessions.AsNoTracking().SingleOrDefaultAsync(e => e.Id == session.Id.Value);
        Assert.Null(entity);
    }

    /// <summary>
    /// Proves the real cross-DbContext race: two separate scopes (each with its own
    /// DbContext) both load the session at the same version, both produce an event
    /// (so both try to append at sequence N+1), both stage (passing the stage-time
    /// check because neither has committed yet), the first commits successfully, and
    /// the second's commit fails at the database unique index backstop. The UoW must
    /// translate that DbUpdateException to ConcurrencyException so the handler can
    /// reload and retry. See ADR-0028 §7 (Optimistic concurrency).
    /// </summary>
    [Fact]
    public async Task CommitAsync_TranslatesUniqueIndexViolation_ToConcurrencyException_OnCrossDbContextRace()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);

        // Seed: create and commit a session in a throwaway scope.
        GameSessionId sessionId;
        using (var seedScope = services.CreateScope())
        {
            var seedRepo = seedScope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            var seedUow = seedScope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
            var session = CreateSession();
            sessionId = session.Id;
            await seedRepo.StoreAsync(session);
            await seedUow.CommitAsync();
            session.MarkEventsCommitted();
        }

        // Two concurrent requests: each gets its own scope/DbContext.
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();
        {
            var repo1 = scope1.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            var uow1 = scope1.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
            var repo2 = scope2.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            var uow2 = scope2.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

            // Both load the session at version 1 (same StreamVersion in their own DbContexts).
            var copy1 = await repo1.GetByIdAsync(sessionId);
            var copy2 = await repo2.GetByIdAsync(sessionId);
            Assert.NotNull(copy1);
            Assert.NotNull(copy2);
            Assert.Equal(1, copy1!.Version);
            Assert.Equal(1, copy2!.Version);

            // Both produce a purchase event → both try to append at sequence 2.
            var resolver = new TownStoreCatalogResolver();
            var offer1 = resolver.Resolve(copy1.World.GetTown(copy1.Player.CurrentTownId))
                .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
            var offer2 = resolver.Resolve(copy2!.World.GetTown(copy2.Player.CurrentTownId))
                .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
            copy1.Purchase(offer1, 1);
            copy2.Purchase(offer2, 1);

            // Both stage BEFORE either commits. Both pass the stage-time check
            // because neither DbContext has seen the other's commit yet — the DB
            // still has StreamVersion=1 for both queries inside StoreAsync.
            await repo1.StoreAsync(copy1);
            await repo2.StoreAsync(copy2);

            // First request commits successfully.
            await uow1.CommitAsync();
            copy1.MarkEventsCommitted();

            // Second request's commit fails at the unique index on (StreamId, Sequence)
            // because sequence 2 already exists. The UoW must translate this
            // DbUpdateException to ConcurrencyException so the handler can retry.
            var thrown = await Assert.ThrowsAsync<ConcurrencyException>(() => uow2.CommitAsync());
            Assert.Contains("Concurrency conflict", thrown.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Proves the snapshot + replay load path sets the aggregate version correctly
    /// when the snapshot lags behind the stream version. The repository must set the
    /// aggregate version to SnapshotVersion before replaying post-snapshot events,
    /// so that after replay Version == StreamVersion (not StreamVersion + replayedCount).
    /// See ADR-0028 §8 (Snapshots as cache) and §7 (Optimistic concurrency).
    ///
    /// Without the fix (setting version to SnapshotVersion before replay), the loaded
    /// aggregate would have Version = StreamVersion + postSnapshotEventCount, which
    /// corrupts the next optimistic concurrency check.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithLaggingSnapshot_LoadsAggregateWithVersionEqualToStreamVersion()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);

        // Seed: create + commit (v1), then reload + purchase + commit (v2).
        // After this, SnapshotVersion == StreamVersion == 2 in the DB.
        GameSessionId sessionId;
        using (var seedScope = services.CreateScope())
        {
            var seedRepo = seedScope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            var seedUow = seedScope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
            var session = CreateSession();
            sessionId = session.Id;
            await seedRepo.StoreAsync(session);
            await seedUow.CommitAsync();
            session.MarkEventsCommitted();

            var loaded = await seedRepo.GetByIdAsync(sessionId);
            var resolver = new TownStoreCatalogResolver();
            var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId))
                .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
            loaded.Purchase(offer, 1);
            await seedRepo.StoreAsync(loaded);
            await seedUow.CommitAsync();
        }

        // Force a lagging snapshot: set SnapshotVersion back to 1 while
        // StreamVersion stays at 2. This simulates a snapshot that was not
        // refreshed after the last event append.
        using (var adminScope = services.CreateScope())
        {
            var adminDb = adminScope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
            var entity = await adminDb.GameSessions.SingleAsync(e => e.Id == sessionId.Value);
            entity.SnapshotVersion = 1;
            await adminDb.SaveChangesAsync();
        }

        // Load through the repository. The snapshot is at version 1, the stream
        // is at version 2, so one post-snapshot event (StoreItemPurchased) must be
        // replayed. The loaded aggregate's Version must equal StreamVersion (2),
        // not StreamVersion + 1 (3) which would be the bug.
        using var loadScope = services.CreateScope();
        var loadRepo = loadScope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var loaded2 = await loadRepo.GetByIdAsync(sessionId);
        Assert.NotNull(loaded2);
        Assert.Equal(2, loaded2!.Version);
    }

    /// <summary>
    /// Proves the snapshot + replay load path does not duplicate aggregate LogEntries
    /// when the snapshot lags behind the stream version. The repository must project
    /// only the snapshot-prefix events for aggregate LogEntries rehydration, then let
    /// post-snapshot replay append the rest via Apply(...). If the repository projected
    /// the full stream and then replayed post-snapshot events, the post-snapshot log
    /// entries would be duplicated. See BUNCH-86.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithLaggingSnapshot_DoesNotDuplicateAggregateLogEntries()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);

        // Seed: create + commit (v1), then reload + purchase + commit (v2).
        // After this, SnapshotVersion == StreamVersion == 2 in the DB.
        // v1 produces GameStarted (1 log entry: opening).
        // v2 produces StoreItemPurchased (1 log entry: purchase).
        // Full-stream projection = 2 log entries.
        GameSessionId sessionId;
        using (var seedScope = services.CreateScope())
        {
            var seedRepo = seedScope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            var seedUow = seedScope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
            var session = CreateSession();
            sessionId = session.Id;
            await seedRepo.StoreAsync(session);
            await seedUow.CommitAsync();
            session.MarkEventsCommitted();

            var loaded = await seedRepo.GetByIdAsync(sessionId);
            var resolver = new TownStoreCatalogResolver();
            var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId))
                .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
            loaded.Purchase(offer, 1);
            await seedRepo.StoreAsync(loaded);
            await seedUow.CommitAsync();
        }

        // Force a lagging snapshot: set SnapshotVersion back to 1 while
        // StreamVersion stays at 2. This simulates a snapshot that was not
        // refreshed after the last event append.
        using (var adminScope = services.CreateScope())
        {
            var adminDb = adminScope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
            var entity = await adminDb.GameSessions.SingleAsync(e => e.Id == sessionId.Value);
            entity.SnapshotVersion = 1;
            await adminDb.SaveChangesAsync();
        }

        // Load through the repository. The snapshot is at version 1, the stream
        // is at version 2, so one post-snapshot event (StoreItemPurchased) must be
        // replayed via ApplyCommittedEvents. The aggregate's LogEntries must
        // contain exactly 2 entries (opening + purchase), not 3 (which would
        // indicate the purchase entry was duplicated by full-stream projection
        // followed by post-snapshot replay).
        using var loadScope = services.CreateScope();
        var loadRepo = loadScope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var loaded2 = await loadRepo.GetByIdAsync(sessionId);
        Assert.NotNull(loaded2);

        // The full event stream has 2 events (GameStarted + StoreItemPurchased).
        // The projector produces 2 log entries (opening + purchase).
        // The aggregate's LogEntries must match — no duplication from
        // snapshot-prefix projection + post-snapshot replay.
        Assert.Equal(2, GameSessionLogProjection.Project(loaded2!).Count);
        Assert.Equal(GameLogEntryKind.Opening, GameSessionLogProjection.Project(loaded2)[0].Kind);
        Assert.Equal(GameLogEntryKind.Purchase, GameSessionLogProjection.Project(loaded2)[1].Kind);
    }

    private static ServiceProvider CreateServices(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<WildBunchDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<GameSessionJsonSerializer>();
        services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();
        services.AddScoped<IGameSessionUnitOfWork, EfGameSessionUnitOfWork>();
        var provider = services.BuildServiceProvider();

        // Apply migrations to the fresh test database
        using (var scope = provider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
            dbContext.Database.Migrate();
        }

        return provider;
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };
        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory);
    }

    private static GameSession CreateSessionWithWarrantedSaloonSuspect()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.NoticeBoard);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Mira Cline",
                new SuspectProfile(
                    Array.Empty<SuspectAlias>(),
                    new[] { new SuspectIdentityFact("Has a scar on the left cheek.") }),
                SuspectTraits.Empty,
                SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            null, suspects, new SuspectId("suspect-2"),
            CaseOpeningLead.Create("Follow the public leads."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-1"),
                    "Mira Cline",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive, 2500m,
                        new[] { "Red Wren" }, new[] { "Raven-feather pin" },
                        "Dodge City Marshal",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(), null),
                    "Wanted for a stage robbery.")
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id,
            Wallet.Starting(25m), inventory: null, GameDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
    }
}
