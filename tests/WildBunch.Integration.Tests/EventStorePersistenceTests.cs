using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
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
}
