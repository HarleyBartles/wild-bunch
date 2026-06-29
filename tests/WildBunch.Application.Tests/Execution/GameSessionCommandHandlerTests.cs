using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Application.Tests.Execution;

public sealed class GameSessionCommandHandlerTests
{
    [Fact]
    public async Task ExecuteWithRetryAsync_LoadsCommandStoresCommitsAndMarksEventsCommitted()
    {
        var repo = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repo.Seed(session);

        var handler = new TestCommandHandler(repo, repo);
        var result = await handler.ExecuteWithRetryAsync(
            session.Id,
            async (s, ct) =>
            {
                var resolver = new TownStoreCatalogResolver();
                var offer = resolver.Resolve(s.World.GetTown(s.Player.CurrentTownId))
                    .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
                s.Purchase(offer, 1);
                await Task.Yield();
                return "purchased";
            });

        Assert.Equal("purchased", result);
        Assert.Equal(1, repo.StoreCalls);
        Assert.Equal(1, repo.CommitCalls);
        var stored = repo.Sessions.Single();
        Assert.Empty(stored.UncommittedEvents);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_DoesNotStoreWhenNoEventsProduced()
    {
        var repo = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repo.Seed(session);

        var handler = new TestCommandHandler(repo, repo);
        var result = await handler.ExecuteWithRetryAsync(
            session.Id,
            async (s, ct) =>
            {
                await Task.Yield();
                return "no-op";
            });

        Assert.Equal("no-op", result);
        Assert.Equal(0, repo.StoreCalls);
        Assert.Equal(0, repo.CommitCalls);
    }

    [Fact]
    public async Task ExecuteNewSessionAsync_StoresCommitsAndMarksEventsCommitted()
    {
        var repo = new InMemoryGameSessionRepository();

        var handler = new TestCommandHandler(repo, repo);
        var result = await handler.ExecuteNewSessionAsync(async ct =>
        {
            var session = CreateSession();
            await Task.Yield();
            return (session, "created");
        });

        Assert.Equal("created", result);
        Assert.Equal(1, repo.StoreCalls);
        Assert.Equal(1, repo.CommitCalls);
        var stored = repo.Sessions.Single();
        Assert.Empty(stored.UncommittedEvents);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RetriesOnConcurrencyException()
    {
        var repo = new ConcurrencyRetryRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repo.Seed(session);

        var handler = new TestCommandHandler(repo, repo);
        var result = await handler.ExecuteWithRetryAsync(
            session.Id,
            async (s, ct) =>
            {
                // Produce an event so StoreAsync is called
                var resolver = new TownStoreCatalogResolver();
                var offer = resolver.Resolve(s.World.GetTown(s.Player.CurrentTownId))
                    .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
                s.Purchase(offer, 1);
                await Task.Yield();
                return "ok";
            });

        Assert.Equal("ok", result);
        Assert.True(repo.Attempts >= 2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ThrowsAfterMaxRetries()
    {
        var repo = new AlwaysConcurrencyRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repo.Seed(session);

        var handler = new TestCommandHandler(repo, repo);

        await Assert.ThrowsAsync<ConcurrencyException>(() => handler.ExecuteWithRetryAsync(
            session.Id,
            async (s, ct) =>
            {
                // Produce an event so StoreAsync is called
                var resolver = new TownStoreCatalogResolver();
                var offer = resolver.Resolve(s.World.GetTown(s.Player.CurrentTownId))
                    .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
                s.Purchase(offer, 1);
                await Task.Yield();
                return "ok";
            }));
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Telegraph);
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

    private sealed class TestCommandHandler : GameSessionCommandHandler
    {
        public TestCommandHandler(IGameSessionRepository repo, IGameSessionUnitOfWork uow)
            : base(repo, uow) { }
    }

    private sealed class ConcurrencyRetryRepository : IGameSessionRepository, IGameSessionUnitOfWork
    {
        private readonly Dictionary<GameSessionId, GameSession> _sessions = new();
        private readonly Dictionary<GameSessionId, GameSession> _pending = new();
        public int Attempts { get; private set; }
        public int CommitCalls { get; private set; }

        public IReadOnlyCollection<GameSession> Sessions => _sessions.Values.ToArray();

        public void Seed(GameSession session) => _sessions[session.Id] = session;

        public Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken ct = default)
        {
            _sessions.TryGetValue(id, out var s);
            return Task.FromResult(s);
        }

        public Task<IReadOnlyList<GameSession>> GetByStatusAsync(GameStatus status, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<GameSession>>(_sessions.Values.Where(s => s.Status == status).ToArray());

        public Task StoreAsync(GameSession session, Guid? correlationId = null, CancellationToken ct = default)
        {
            Attempts++;
            if (Attempts == 1)
            {
                throw new ConcurrencyException(session.Id, 0, 1);
            }
            _pending[session.Id] = session;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IDomainEvent>> GetEventStreamAsync(GameSessionId id, long fromVersion = 0, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());

        public Task CommitAsync(CancellationToken ct = default)
        {
            CommitCalls++;
            foreach (var pending in _pending)
            {
                _sessions[pending.Key] = pending.Value;
            }
            _pending.Clear();
            return Task.CompletedTask;
        }
    }

    private sealed class AlwaysConcurrencyRepository : IGameSessionRepository, IGameSessionUnitOfWork
    {
        private readonly Dictionary<GameSessionId, GameSession> _sessions = new();

        public void Seed(GameSession session) => _sessions[session.Id] = session;

        public Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken ct = default)
        {
            _sessions.TryGetValue(id, out var s);
            return Task.FromResult(s);
        }

        public Task<IReadOnlyList<GameSession>> GetByStatusAsync(GameStatus status, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<GameSession>>(_sessions.Values.Where(s => s.Status == status).ToArray());

        public Task StoreAsync(GameSession session, Guid? correlationId = null, CancellationToken ct = default)
            => throw new ConcurrencyException(session.Id, 0, 1);

        public Task<IReadOnlyList<IDomainEvent>> GetEventStreamAsync(GameSessionId id, long fromVersion = 0, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());

        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
