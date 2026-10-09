using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Projections;
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
using WildBunch.Persistence.Versioning;
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

        Assert.Equal(6, storedEvents.Length);
        Assert.Equal("PlayerSetupCompleted", storedEvents[0].EventType);
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
        var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
        loaded.Purchase(offer, 2);

        await repo.StoreAsync(loaded);
        await uow.CommitAsync();

        var storedEvents = await dbContext.StoredEvents.AsNoTracking()
            .Where(e => e.StreamId == session.Id.Value)
            .OrderBy(e => e.Sequence)
            .ToArrayAsync();

        Assert.Equal(8, storedEvents.Length);
        Assert.Equal("PlayerSetupCompleted", storedEvents[0].EventType);
        Assert.Equal("TownActionContextEntered", storedEvents[6].EventType);
        Assert.Equal("StoreItemPurchased", storedEvents[7].EventType);
        Assert.Equal(8, storedEvents[7].Sequence);
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
        var offer = resolver.Resolve(copy1!.World.GetTown(copy1.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
        copy1.Purchase(offer, 1);
        await repo.StoreAsync(copy1);
        await uow.CommitAsync();
        copy1.MarkEventsCommitted();

        // Second copy tries to purchase — should get ConcurrencyException
        var offer2 = resolver.Resolve(copy2!.World.GetTown(copy2.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
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
        var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
        loaded.Purchase(offer, 3);
        await repo.StoreAsync(loaded);
        await uow.CommitAsync();

        var events = await repo.GetEventStreamAsync(session.Id);

        Assert.Equal(8, events.Count);
        Assert.IsType<PlayerSetupCompleted>(events[0]);
        Assert.IsType<TownActionContextEntered>(events[6]);
        Assert.IsType<StoreItemPurchased>(events[7]);
        var purchase = (StoreItemPurchased)events[7];
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
        var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
        loaded.Purchase(offer, 1);
        await repo.StoreAsync(loaded);
        await uow.CommitAsync();

        // Get events after version 6 (the start flow events)
        var events = await repo.GetEventStreamAsync(session.Id, fromVersion: 6);

        Assert.Equal(2, events.Count);
        Assert.IsType<TownActionContextEntered>(events[0]);
        Assert.IsType<StoreItemPurchased>(events[1]);
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

        // 1. Create + store + commit (start flow events)
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
        Assert.Equal(8, storedEvents.Length);
        Assert.Equal("PlayerSetupCompleted", storedEvents[0].EventType);
        Assert.Equal("TownActionContextEntered", storedEvents[6].EventType);
        Assert.Equal("InvestigationPerformed", storedEvents[7].EventType);

        // 4. GetEventStreamAsync must deserialize all events without throwing
        var events = await repo.GetEventStreamAsync(session.Id);
        Assert.Equal(8, events.Count);
        Assert.IsType<PlayerSetupCompleted>(events[0]);
        Assert.IsType<TownActionContextEntered>(events[6]);
        var investigationEvent = Assert.IsType<InvestigationPerformed>(events[7]);
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

        // 1. Create + store + commit (start flow events)
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
        Assert.Equal(8, events.Count);
        Assert.IsType<PlayerSetupCompleted>(events[0]);
        Assert.IsType<TownActionContextEntered>(events[6]);
        var spottedEvent = Assert.IsType<SaloonPersonOfInterestSpotted>(events[7]);
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

        var clockBeforeRetry = (reloaded.Clock.Day, reloaded.Clock.Turn);
        var streamVersionBeforeRetry = reloaded.Version;
        Assert.False(reloaded.EnterActionContext(TownActionContext.Saloon));
        Assert.Equal(clockBeforeRetry, (reloaded.Clock.Day, reloaded.Clock.Turn));
        Assert.Equal(streamVersionBeforeRetry, reloaded.Version);

        var offer = new TownStoreCatalogResolver()
            .Resolve(reloaded.World.GetTown(reloaded.Player.CurrentTownId!.Value))
            .Offers.Single(candidate => candidate.ItemKind == DomainItemKind.Food);
        Assert.True(reloaded.Purchase(offer, 1).Success);
        Assert.Equal(streamVersionBeforeRetry + 2, reloaded.Version);
        await repo.StoreAsync(reloaded);
        await uow.CommitAsync();

        var fresh = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fresh);
        Assert.Single(fresh!.AllEvents.OfType<StoreItemPurchased>());
        Assert.Equal(streamVersionBeforeRetry + 2, fresh.Version);
        Assert.Equal(
            new[] { nameof(TownActionContextEntered), nameof(StoreItemPurchased) },
            fresh.AllEvents.TakeLast(2).Select(domainEvent => domainEvent.GetType().Name));
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
        var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
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

            // Both load the session at version 6 (same StreamVersion in their own DbContexts).
            var copy1 = await repo1.GetByIdAsync(sessionId);
            var copy2 = await repo2.GetByIdAsync(sessionId);
            Assert.NotNull(copy1);
            Assert.NotNull(copy2);
            Assert.Equal(6, copy1!.Version);
            Assert.Equal(6, copy2!.Version);

            // Both produce a purchase event → both try to append at sequence 7.
            var resolver = new TownStoreCatalogResolver();
            var offer1 = resolver.Resolve(copy1.World.GetTown(copy1.Player.CurrentTownId!.Value))
                .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
            var offer2 = resolver.Resolve(copy2!.World.GetTown(copy2.Player.CurrentTownId!.Value))
                .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
            copy1.Purchase(offer1, 1);
            copy2.Purchase(offer2, 1);

            // Both stage BEFORE either commits. Both pass the stage-time check
            // because neither DbContext has seen the other's commit yet — the DB
            // still has StreamVersion=6 for both queries inside StoreAsync.
            await repo1.StoreAsync(copy1);
            await repo2.StoreAsync(copy2);

            // First request commits successfully.
            await uow1.CommitAsync();
            copy1.MarkEventsCommitted();

            var dbContext1 = scope1.ServiceProvider.GetRequiredService<WildBunchDbContext>();
            var winningEnvelope = await dbContext1.GameSessions.AsNoTracking().SingleAsync(entity => entity.Id == sessionId.Value);
            var winningComponents = await dbContext1.GameSessionComponents.AsNoTracking()
                .Where(component => component.SessionId == sessionId.Value)
                .OrderBy(component => component.ComponentName)
                .Select(component => new { component.ComponentName, component.ComponentVersion, component.PayloadJson })
                .ToArrayAsync();
            var winningDiaryDays = await dbContext1.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == sessionId.Value)
                .OrderBy(day => day.Sequence)
                .Select(day => new { day.Sequence, day.SchemaVersion, day.PayloadJson })
                .ToArrayAsync();
            var winningEvents = await dbContext1.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == sessionId.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new { storedEvent.Sequence, storedEvent.EventId, storedEvent.EventType, storedEvent.PayloadJson })
                .ToArrayAsync();

            // Second request's commit fails at the unique index on (StreamId, Sequence)
            // because sequence 7 already exists. The UoW must translate this
            // DbUpdateException to ConcurrencyException so the handler can retry.
            var thrown = await Assert.ThrowsAsync<ConcurrencyException>(() => uow2.CommitAsync());
            Assert.Contains("Concurrency conflict", thrown.Message, StringComparison.Ordinal);

            using (var verificationScope = services.CreateScope())
            {
                var verificationDb = verificationScope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
                var verificationRepository = verificationScope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
                var envelopeAfterConflict = await verificationDb.GameSessions.AsNoTracking().SingleAsync(entity => entity.Id == sessionId.Value);
                Assert.Equal(winningEnvelope.StreamVersion, envelopeAfterConflict.StreamVersion);
                Assert.Equal(winningEnvelope.SnapshotVersion, envelopeAfterConflict.SnapshotVersion);
                Assert.Equal(winningEnvelope.UpdatedAtUtc, envelopeAfterConflict.UpdatedAtUtc);

                var componentsAfterConflict = await verificationDb.GameSessionComponents.AsNoTracking()
                    .Where(component => component.SessionId == sessionId.Value)
                    .OrderBy(component => component.ComponentName)
                    .Select(component => new { component.ComponentName, component.ComponentVersion, component.PayloadJson })
                    .ToArrayAsync();
                Assert.Equal(winningComponents, componentsAfterConflict);

                var diaryAfterConflict = await verificationDb.GameSessionDiaryDays.AsNoTracking()
                    .Where(day => day.SessionId == sessionId.Value)
                    .OrderBy(day => day.Sequence)
                    .Select(day => new { day.Sequence, day.SchemaVersion, day.PayloadJson })
                    .ToArrayAsync();
                Assert.Equal(winningDiaryDays, diaryAfterConflict);

                var eventsAfterConflict = await verificationDb.StoredEvents.AsNoTracking()
                    .Where(storedEvent => storedEvent.StreamId == sessionId.Value)
                    .OrderBy(storedEvent => storedEvent.Sequence)
                    .Select(storedEvent => new { storedEvent.Sequence, storedEvent.EventId, storedEvent.EventType, storedEvent.PayloadJson })
                    .ToArrayAsync();
                Assert.Equal(winningEvents, eventsAfterConflict);
                var persistedPurchases = eventsAfterConflict.Where(storedEvent => storedEvent.EventType == nameof(StoreItemPurchased)).ToArray();
                Assert.Single(persistedPurchases);

                var winningState = await verificationRepository.GetByIdAsync(sessionId);
                Assert.NotNull(winningState);
                Assert.Equal(copy1.Player.GetQuantity(DomainItemKind.Food), winningState!.Player.GetQuantity(DomainItemKind.Food));
                Assert.Equal(copy1.Player.Wallet.Cash, winningState.Player.Wallet.Cash);
            }

            using (var retryScope = services.CreateScope())
            {
                var retryRepository = retryScope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
                var retryUnitOfWork = retryScope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
                var retry = await retryRepository.GetByIdAsync(sessionId);
                Assert.NotNull(retry);
                var retryOffer = resolver.Resolve(retry!.World.GetTown(retry.Player.CurrentTownId!.Value))
                    .Offers.Single(offer => offer.ItemKind == DomainItemKind.Food);
                retry.Purchase(retryOffer, 1);
                await retryRepository.StoreAsync(retry);
                await retryUnitOfWork.CommitAsync();

                var retriedState = await retryRepository.GetByIdAsync(sessionId);
                Assert.NotNull(retriedState);
                Assert.Equal(retry.Player.GetQuantity(DomainItemKind.Food), retriedState!.Player.GetQuantity(DomainItemKind.Food));
                Assert.Equal(retry.Player.Wallet.Cash, retriedState.Player.Wallet.Cash);

                var eventsAfterRetry = await retryScope.ServiceProvider.GetRequiredService<WildBunchDbContext>().StoredEvents.AsNoTracking()
                    .Where(storedEvent => storedEvent.StreamId == sessionId.Value)
                    .OrderBy(storedEvent => storedEvent.Sequence)
                    .ToArrayAsync();
                Assert.Equal(
                    Enumerable.Range(1, eventsAfterRetry.Length).Select(sequence => (long)sequence),
                    eventsAfterRetry.Select(storedEvent => storedEvent.Sequence));
                var purchasesAfterRetry = eventsAfterRetry.Where(storedEvent => storedEvent.EventType == nameof(StoreItemPurchased)).ToArray();
                Assert.Equal(2, purchasesAfterRetry.Length);
                Assert.Equal(purchasesAfterRetry.Length, purchasesAfterRetry.Select(storedEvent => storedEvent.Sequence).Distinct().Count());
            }
        }
    }

    [Fact]
    public async Task CommitAsync_PreservesEventIdUniqueViolationAsIntegrityFailure()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        var first = CreateSession();
        var second = CreateSession();

        using (var seedScope = services.CreateScope())
        {
            var repository = seedScope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            var unitOfWork = seedScope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
            await repository.StoreAsync(first);
            await repository.StoreAsync(second);
            await unitOfWork.CommitAsync();
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        var unitOfWorkForInvalidRows = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
        var duplicateEventId = Guid.NewGuid();
        dbContext.StoredEvents.AddRange(
            new StoredEventEntity
            {
                StreamId = first.Id.Value,
                Sequence = first.Version + 1,
                EventId = duplicateEventId,
                OccurredAtUtc = DateTime.UtcNow,
                EventType = "TestEvent",
                PayloadJson = "{}",
                SchemaVersion = 1
            },
            new StoredEventEntity
            {
                StreamId = second.Id.Value,
                Sequence = second.Version + 1,
                EventId = duplicateEventId,
                OccurredAtUtc = DateTime.UtcNow,
                EventType = "TestEvent",
                PayloadJson = "{}",
                SchemaVersion = 1
            });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWorkForInvalidRows.CommitAsync());
        var providerException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, providerException.SqlState);
        Assert.Equal("IX_GameSessionStoredEvents_EventId", providerException.ConstraintName);
    }

    /// <summary>
    /// Ensures stale snapshot metadata cannot corrupt current cached player state or
    /// the aggregate stream position, then verifies that the player can continue.
    /// See ADR-0028 §8 (Snapshots as cache) and §7 (Optimistic concurrency).
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithStaleSnapshotRecoversCurrentCacheAndAllowsNextCommand()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        decimal expectedCash;
        int expectedFoodQuantity;

        // Seed: create + commit (v6), then reload + purchase + commit (v8).
        // After this, SnapshotVersion == StreamVersion == 8 in the DB.
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
            var offer = resolver.Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId!.Value))
                .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
            loaded.Purchase(offer, 1);
            await seedRepo.StoreAsync(loaded);
            await seedUow.CommitAsync();
            expectedCash = loaded.Player.Wallet.Cash;
            expectedFoodQuantity = loaded.Player.Inventory.GetQuantity(DomainItemKind.Food);
        }

        // Force a lagging snapshot: set SnapshotVersion back to 6 while
        // StreamVersion stays at 8. This simulates a snapshot that was not
        // refreshed after the last event append.
        long streamVersion;
        using (var adminScope = services.CreateScope())
        {
            var adminDb = adminScope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
            var entity = await adminDb.GameSessions.SingleAsync(e => e.Id == sessionId.Value);
            entity.SnapshotVersion = 6;
            streamVersion = entity.StreamVersion;
            await adminDb.SaveChangesAsync();
        }

        // The player component already includes the committed purchase although
        // the snapshot metadata is stale. Recovery must not apply those effects twice.
        using var loadScope = services.CreateScope();
        var loadRepo = loadScope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var loadUow = loadScope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
        var loaded2 = await loadRepo.GetByIdAsync(sessionId);
        Assert.NotNull(loaded2);
        Assert.Equal((int)streamVersion, loaded2!.Version);
        Assert.Equal(expectedCash, loaded2.Player.Wallet.Cash);
        Assert.Equal(expectedFoodQuantity, loaded2.Player.Inventory.GetQuantity(DomainItemKind.Food));

        var eventCountBeforeContinuation = (await loadRepo.GetEventStreamAsync(sessionId)).Count;
        var nextResolver = new TownStoreCatalogResolver();
        var nextOffer = nextResolver.Resolve(loaded2.World.GetTown(loaded2.Player.CurrentTownId!.Value))
            .Offers.Single(candidate => candidate.ItemKind == DomainItemKind.Food);
        Assert.True(loaded2.Purchase(nextOffer, 1).Success);
        expectedCash = loaded2.Player.Wallet.Cash;
        expectedFoodQuantity = loaded2.Player.Inventory.GetQuantity(DomainItemKind.Food);
        await loadRepo.StoreAsync(loaded2);
        await loadUow.CommitAsync();

        var fresh = await loadRepo.GetByIdAsync(sessionId);
        Assert.NotNull(fresh);
        Assert.Equal(expectedCash, fresh!.Player.Wallet.Cash);
        Assert.Equal(expectedFoodQuantity, fresh.Player.Inventory.GetQuantity(DomainItemKind.Food));
        var eventsAfterContinuation = await loadRepo.GetEventStreamAsync(sessionId);
        Assert.Equal(eventCountBeforeContinuation + 1, eventsAfterContinuation.Count);
        Assert.Equal((int)eventsAfterContinuation.Count, fresh.Version);
        Assert.Equal(2, eventsAfterContinuation.OfType<StoreItemPurchased>().Count());
        Assert.IsType<StoreItemPurchased>(eventsAfterContinuation[^1]);
    }

    private static ServiceProvider CreateServices(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<WildBunchDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<GameSessionJsonSerializer>();
        services.AddSingleton<TravelDiaryDayProjector>();
        services.AddSingleton<PayloadUpcasterRegistry>(_ => new PayloadUpcasterRegistry([]));
        services.AddSingleton<PersistedPayloadLoader>(sp =>
        {
            var eventUpcasters = sp.GetRequiredService<PayloadUpcasterRegistry>();
            var serializer = sp.GetRequiredService<GameSessionJsonSerializer>();
            var diaryDayProjector = sp.GetRequiredService<TravelDiaryDayProjector>();
            return new PersistedPayloadLoader(
                eventUpcasters,
                serializer,
                diaryDayProjector,
                rebuildSessionFromEvents: events => SessionRebuilder.RebuildFromEvents(events, serializer));
        });
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
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var quartzsite = new Town(new TownId("quartzsite"), "Quartzsite");
        var world = new DomainWorld(
            new[] { pinecross, quartzsite },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, quartzsite.Id, TrailRisk.Low) });

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

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    private static GameSession CreateSessionWithWarrantedSaloonSuspect()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var quartzsite = new Town(new TownId("quartzsite"), "Quartzsite");
        var world = new DomainWorld(
            new[] { pinecross, quartzsite },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, quartzsite.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Mira Cline",
                new SuspectProfile(
                    Array.Empty<SuspectAlias>(),
                    new[] { new SuspectIdentityFact(FeatureLanguage.Raw("Has a scar on the left cheek.", "a scar on the left cheek", "has a scar on the left cheek")) }),
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

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Easy, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed(string.Empty));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory: null);
        return session;
    }
}
