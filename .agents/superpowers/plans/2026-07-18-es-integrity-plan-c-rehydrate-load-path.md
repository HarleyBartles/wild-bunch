# Event Sourcing Integrity — Plan C: RehydrateFromEvents Production Load Path Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire `RehydrateFromEvents` as a production load path in `EfGameSessionRepository`, making the snapshot a true shortcut cache rather than a requirement. A full replay equality test proves that loading from events produces the same session as loading from the snapshot, including `TravelDiaryDays` (rebuilt via the projector from Plan B).

**Architecture:** `EfGameSessionRepository` gains a `LoadFromEventsAsync` method that fetches all stored events, reconstructs the world from the `WorldGenerated` event, calls `RehydrateFromEvents`, and then rebuilds diary days via `TravelDiaryDayProjector`. The existing snapshot load path (`LoadStoreAsync` → `ToAggregate`) remains as the fast path. Path selection is simple: if the snapshot is present and all component versions are current, use the fast path; otherwise use full replay. The write path (`StoreAsync`) is unchanged.

**Depends on:** Plan B (TravelDiaryDayProjector must exist and pass its parity test).

**Tech Stack:** C#/.NET, EF Core, PostgreSQL (Testcontainers), xUnit, `dotnet build`, `dotnet test`

## Global Constraints

- This is a greenfield repo — no old saves to break. No version bumps or upcasters needed (Plan D adds versioning).
- `RehydrateFromEvents` already exists in `src/WildBunch.Domain/Game/GameSessionEventReplay.cs:29` and is tested for aggregate state parity.
- `TravelDiaryDayProjector` exists from Plan B in `src/WildBunch.Application/Projections/TravelDiaryDayProjector.cs`.
- `WorldGenerated` event carries `WorldSnapshot` which has `ToDomain()` — the world can be reconstructed from events.
- `GameSession.ReplaceTravelDiaryDays(IReadOnlyList<TravelDiaryDayState>)` exists to set diary days after rehydration.
- Integration tests use `PostgreSqlPersistenceFixture` and `PostgreSqlTestDatabase` (Testcontainers).
- Run `dotnet build` and `dotnet test` after each task. Run `.\scripts\ci-preflight.ps1` before PR.

---

### Task 1: Add LoadFromEventsAsync to EfGameSessionRepository

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`

**Interfaces:**
- Consumes: `GameSession.RehydrateFromEvents` (existing), `WorldGenerated.World.ToDomain()` (existing), `TravelDiaryDayProjector` (from Plan B), `GameSession.ReplaceTravelDiaryDays` (existing)
- Produces: `LoadFromEventsAsync` private method — the full replay load path.

- [ ] **Step 1: Add TravelDiaryDayProjector dependency to EfGameSessionRepository**

Read `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` lines 12-23. The constructor currently takes `WildBunchDbContext` and `GameSessionJsonSerializer`. Add `TravelDiaryDayProjector` as a constructor parameter:

```csharp
public sealed class EfGameSessionRepository : IGameSessionRepository
{
    private const int SchemaVersion = 1;

    private readonly WildBunchDbContext _dbContext;
    private readonly GameSessionJsonSerializer _serializer;
    private readonly TravelDiaryDayProjector _travelDiaryDayProjector;

    public EfGameSessionRepository(
        WildBunchDbContext dbContext,
        GameSessionJsonSerializer serializer,
        TravelDiaryDayProjector travelDiaryDayProjector)
    {
        _dbContext = dbContext;
        _serializer = serializer;
        _travelDiaryDayProjector = travelDiaryDayProjector;
    }
```

Add the necessary `using` at the top of the file:

```csharp
using WildBunch.Application.Projections;
```

- [ ] **Step 2: Add LoadFromEventsAsync method**

Add the following method to `EfGameSessionRepository` (after the `LoadStoreAsync` method, around line 265):

```csharp
/// <summary>
/// Loads a session by replaying all events from the stream through
/// RehydrateFromEvents. This is the full replay path that proves the
/// snapshot is not required. The world is reconstructed from the
/// WorldGenerated event. Diary days are rebuilt via TravelDiaryDayProjector.
/// See ADR-0028 and the event sourcing integrity policy.
/// </summary>
private async Task<GameSession?> LoadFromEventsAsync(GameSessionId id, CancellationToken cancellationToken)
{
    var storedEvents = await _dbContext.StoredEvents.AsNoTracking()
        .Where(e => e.StreamId == id.Value)
        .OrderBy(e => e.Sequence)
        .ToArrayAsync(cancellationToken)
        .ConfigureAwait(false);

    if (storedEvents.Length == 0)
    {
        return null;
    }

    var events = new IDomainEvent[storedEvents.Length];
    for (var i = 0; i < storedEvents.Length; i++)
    {
        events[i] = _serializer.DeserializeEvent(storedEvents[i].EventType, storedEvents[i].PayloadJson);
    }

    // Reconstruct the world from the WorldGenerated event.
    var worldGenerated = events.OfType<WorldGenerated>().FirstOrDefault();
    if (worldGenerated is null)
    {
        throw new InvalidOperationException(
            $"Cannot load session {id} from events: no WorldGenerated event in the stream.");
    }
    var world = worldGenerated.World.ToDomain();

    // Rehydrate the aggregate from the full event stream.
    var session = GameSession.RehydrateFromEvents(id, world, events);

    // Rebuild diary days via the projector.
    var diaryProjection = _travelDiaryDayProjector.Project(events);
    session.ReplaceTravelDiaryDays(diaryProjection.Days);

    // Set committed events for projection-backed read paths (JournalLogProjector etc.).
    session.SetCommittedEvents(events);

    return session;
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build`
Expected: FAIL — the DI registration doesn't include `TravelDiaryDayProjector` yet, and `GetByIdAsync` hasn't been updated to use the new path. But the method itself should compile. If the build fails on the method itself, fix the compilation errors.

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs
git commit -m "Add LoadFromEventsAsync to EfGameSessionRepository

Full replay load path that reconstructs the session from the event stream alone.
Reconstructs the world from WorldGenerated, rehydrates the aggregate via
RehydrateFromEvents, and rebuilds diary days via TravelDiaryDayProjector."
```

---

### Task 2: Wire path selection in GetByIdAsync

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` (the `GetByIdAsync` method)

**Interfaces:**
- Consumes: `LoadFromEventsAsync` (from Task 1), `LoadStoreAsync` (existing)
- Produces: `GetByIdAsync` selects fast path (snapshot current) or full replay (snapshot stale/missing).

- [ ] **Step 1: Update GetByIdAsync to select between fast and full replay paths**

Read `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` lines 25-29. The current method is:

```csharp
public async Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default)
{
    var store = await LoadStoreAsync(id, cancellationToken).ConfigureAwait(false);
    return store is null ? null : ToAggregate(store);
}
```

Replace with path selection logic:

```csharp
public async Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default)
{
    // Check if the session exists and whether the snapshot is current.
    var envelope = await _dbContext.GameSessions.AsNoTracking()
        .SingleOrDefaultAsync(session => session.Id == id.Value, cancellationToken)
        .ConfigureAwait(false);

    if (envelope is null)
    {
        return null;
    }

    // Full replay path: if the snapshot version doesn't match the stream version,
    // the snapshot is stale — use full replay. This is the event-sourcing-true path.
    if (envelope.SnapshotVersion != envelope.StreamVersion)
    {
        return await LoadFromEventsAsync(id, cancellationToken).ConfigureAwait(false);
    }

    // Fast path: snapshot is current. Load from snapshot + replay post-snapshot events.
    var store = await LoadStoreAsync(id, cancellationToken).ConfigureAwait(false);
    return store is null ? null : ToAggregate(store);
}
```

**Note:** The `LoadStoreAsync` method already loads the envelope internally. This change adds one extra query (the envelope check) before `LoadStoreAsync`. This is acceptable because:
1. The envelope check is a lightweight scalar query.
2. In the normal case (snapshot current), `LoadStoreAsync` is still called and does its own envelope load. The extra query is one additional round-trip.
3. In the stale snapshot case, `LoadFromEventsAsync` is called instead of `LoadStoreAsync`, avoiding the component and diary-day queries.

Alternatively, you can refactor `LoadStoreAsync` to accept the envelope as a parameter to avoid the double load. If you do this, keep the refactoring minimal — don't change `LoadStoreAsync`'s behavior, just allow passing the envelope.

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS (the method compiles, but DI registration isn't updated yet).

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs
git commit -m "Wire path selection in GetByIdAsync: snapshot fast path vs full replay

If the snapshot version doesn't match the stream version, the snapshot is stale
and the full replay path (LoadFromEventsAsync) is used. Otherwise, the existing
snapshot fast path (LoadStoreAsync + ToAggregate) is used."
```

---

### Task 3: Register TravelDiaryDayProjector in DI

**Files:**
- Modify: DI registration (find where `EfGameSessionRepository` is registered)

**Interfaces:**
- Consumes: `TravelDiaryDayProjector` (from Plan B)
- Produces: `TravelDiaryDayProjector` registered in DI, injected into `EfGameSessionRepository`.

- [ ] **Step 1: Find the DI registration site**

Search for where `EfGameSessionRepository` is registered as `IGameSessionRepository`. This is likely in `src/WildBunch.Api/` or `src/WildBunch.Persistence/` extension methods.

```bash
grep -rn "AddScoped<IGameSessionRepository" src/ --include="*.cs"
```

- [ ] **Step 2: Register TravelDiaryDayProjector**

At the DI registration site, add:

```csharp
services.AddSingleton<TravelDiaryDayProjector>();
```

`TravelDiaryDayProjector` is stateless (pure function over events), so singleton is correct.

Also verify that the test infrastructure registers it. Check `tests/WildBunch.Integration.Tests/TestInfrastructure/` for the test DI setup. The `EventSourcingEndToEndTests.CreateServices` method (line 30-46) manually registers services — add `TravelDiaryDayProjector` there:

```csharp
services.AddSingleton<TravelDiaryDayProjector>();
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 4: Run existing tests to verify no regressions**

Run: `dotnet test`
Expected: PASS. All existing tests should pass — the snapshot path is still used when the snapshot is current (which is the case for all existing tests since they save and immediately reload).

- [ ] **Step 5: Commit**

```bash
git add <DI registration files>
git commit -m "Register TravelDiaryDayProjector in DI for EfGameSessionRepository"
```

---

### Task 4: Write the full replay equality test

This is the completion gate test — it proves that loading from events produces the same session as loading from the snapshot, including `TravelDiaryDays`.

**Files:**
- Create: `tests/WildBunch.Integration.Tests/FullReplayEqualityTests.cs`

**Interfaces:**
- Consumes: `EfGameSessionRepository` (with `LoadFromEventsAsync`), `TravelDiaryDayProjector`, `PostgreSqlTestDatabase`, `TravelTestFactory` patterns.

- [ ] **Step 1: Write the full replay equality test**

Create `tests/WildBunch.Integration.Tests/FullReplayEqualityTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
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
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Integration.Tests;

/// <summary>
/// Proves that loading a session from the full event stream (LoadFromEventsAsync)
/// produces the same state as loading from the snapshot (LoadStoreAsync + ToAggregate).
/// This is the completion gate for making event sourcing materially true:
/// the snapshot is a shortcut cache, not a requirement.
/// See ADR-0028 and the event sourcing integrity policy.
/// </summary>
public sealed class FullReplayEqualityTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private static ServiceProvider CreateServices(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<WildBunchDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<GameSessionJsonSerializer>();
        services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();
        services.AddScoped<IGameSessionUnitOfWork, EfGameSessionUnitOfWork>();
        services.AddSingleton<HudProjector>();
        services.AddSingleton<DiaryProjector>();
        services.AddSingleton<FullAuditProjector>();
        services.AddSingleton<TravelDiaryDayProjector>();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<WildBunchDbContext>().Database.Migrate();

        return provider;
    }

    private static GameSession CreateSessionWithJourney()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var quartzsite = new Town(new TownId("quartzsite"), "Quartzsite", TownServices.Telegraph);
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

    [Fact]
    public async Task FullReplay_PurchaseFlow_MatchesSnapshotLoad()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create and store
        var session = CreateSessionWithJourney();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Purchase
        var reloaded = await repo.GetByIdAsync(session.Id);
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(reloaded!.World.GetTown(reloaded.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        reloaded.Purchase(offer, 2);
        await repo.StoreAsync(reloaded);
        await uow.CommitAsync();

        // Load from snapshot (fast path — snapshot is current)
        var fromSnapshot = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromSnapshot);

        // Load from events (full replay path — force by calling LoadFromEventsAsync indirectly)
        // We need to force the full replay path. Since the snapshot is current after save,
        // GetByIdAsync will use the fast path. To test the full replay path, we need to
        // either (a) call LoadFromEventsAsync directly (it's private), or (b) corrupt the
        // snapshot version to force the full replay path.
        //
        // Option (b): set SnapshotVersion to a stale value directly in the database.
        await using var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        var entity = await db.GameSessions.SingleAsync(e => e.Id == session.Id.Value);
        entity.SnapshotVersion = entity.StreamVersion - 1; // Make snapshot stale
        await db.SaveChangesAsync();

        var fromEvents = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromEvents);

        // State equality proof — aggregate state
        Assert.Equal(fromSnapshot!.Player.Wallet.Cash, fromEvents!.Player.Wallet.Cash);
        Assert.Equal(fromSnapshot.Player.Health, fromEvents.Player.Health);
        Assert.Equal(fromSnapshot.Player.Inventory.GetQuantity(DomainItemKind.Food),
            fromEvents.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(fromSnapshot.Player.Name, fromEvents.Player.Name);
        Assert.Equal(fromSnapshot.Player.CurrentTownId, fromEvents.Player.CurrentTownId);
        Assert.Equal(fromSnapshot.Clock.Day, fromEvents.Clock.Day);
        Assert.Equal(fromSnapshot.PursuitState.Heat, fromEvents.PursuitState.Heat);
        Assert.Equal(fromSnapshot.Status, fromEvents.Status);
        Assert.Equal(fromSnapshot.GameDifficulty, fromEvents.GameDifficulty);
        Assert.Equal(fromSnapshot.SeedCode, fromEvents.SeedCode);
        Assert.Equal(fromSnapshot.Version, fromEvents.Version);

        // State equality proof — diary days (the key proof for Plan B + C)
        Assert.Equal(fromSnapshot.TravelDiaryDays.Count, fromEvents.TravelDiaryDays.Count);
    }

    [Fact]
    public async Task FullReplay_JourneyCycle_MatchesSnapshotLoad()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create session
        var session = CreateSessionWithJourney();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Start journey and advance days
        var reloaded = await repo.GetByIdAsync(session.Id);
        var preview = reloaded!.ResolveTravelPreview(new TownId("quartzsite"));
        reloaded.StartJourney(preview);

        // Force quiet days and complete the journey
        TravelJourneyStepResult result;
        do
        {
            reloaded.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            result = reloaded.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        reloaded.AcknowledgeJourneyArrival();

        await repo.StoreAsync(reloaded);
        await uow.CommitAsync();

        // Load from snapshot
        var fromSnapshot = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromSnapshot);

        // Force full replay by making snapshot stale
        await using var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        var entity = await db.GameSessions.SingleAsync(e => e.Id == session.Id.Value);
        entity.SnapshotVersion = entity.StreamVersion - 1;
        await db.SaveChangesAsync();

        var fromEvents = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromEvents);

        // Aggregate state equality
        Assert.Equal(fromSnapshot!.Player.Health, fromEvents!.Player.Health);
        Assert.Equal(fromSnapshot.Player.Wallet.Cash, fromEvents.Player.Wallet.Cash);
        Assert.Equal(fromSnapshot.Player.CurrentTownId, fromEvents.Player.CurrentTownId);
        Assert.Equal(fromSnapshot.Clock.Day, fromEvents.Clock.Day);
        Assert.Equal(fromSnapshot.PursuitState.Heat, fromEvents.PursuitState.Heat);
        Assert.Equal(fromSnapshot.Version, fromEvents.Version);

        // Diary days equality — the key proof
        Assert.Equal(fromSnapshot.TravelDiaryDays.Count, fromEvents.TravelDiaryDays.Count);
        for (var i = 0; i < fromSnapshot.TravelDiaryDays.Count; i++)
        {
            var expected = fromSnapshot.TravelDiaryDays[i];
            var actual = fromEvents.TravelDiaryDays[i];
            Assert.Equal(expected.DayNumber, actual.DayNumber);
            Assert.Equal(expected.OriginTownName, actual.OriginTownName);
            Assert.Equal(expected.DestinationTownName, actual.DestinationTownName);
            Assert.Equal(expected.Status, actual.Status);
            Assert.Equal(expected.HealthDelta, actual.HealthDelta);
            Assert.Equal(expected.CurrentHealth, actual.CurrentHealth);
            Assert.Equal(expected.CurrentWallet, actual.CurrentWallet);
            Assert.Equal(expected.Entries, actual.Entries);
        }

        // Journey state equality
        Assert.Equal(fromSnapshot.CompletedJourneyHistory.Count, fromEvents.CompletedJourneyHistory.Count);
        Assert.Null(fromEvents.Journey); // Journey completed and acknowledged
    }

    [Fact]
    public async Task FullReplay_MissingSnapshot_LoadsFromEvents()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create and store
        var session = CreateSessionWithJourney();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Delete all component rows (simulate missing/corrupted snapshot)
        await using var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        await db.GameSessionComponents
            .Where(c => c.SessionId == session.Id.Value)
            .ExecuteDeleteAsync(cancellationToken: default);
        await db.SaveChangesAsync();

        // Load — should fall back to full replay
        var fromEvents = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromEvents);
        Assert.Equal("Ranger Vale", fromEvents!.Player.Name);
        Assert.Equal(session.Version, fromEvents.Version);
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 3: Run the tests**

Run: `dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~FullReplayEqualityTests`
Expected: May PASS or FAIL. If it fails, examine the assertion failures.

**Note on the missing-snapshot test:** The `FullReplay_MissingSnapshot_LoadsFromEvents` test deletes component rows but the `GetByIdAsync` path selection only checks `SnapshotVersion != StreamVersion`. If the snapshot is current but the component rows are missing, the fast path will be selected and `LoadStoreAsync` will fail because components are missing. To handle this, the path selection in `GetByIdAsync` needs to also check if components exist. Alternatively, the `LoadStoreAsync` / `ToAggregate` path needs to fall back to `LoadFromEventsAsync` when a required component is missing.

If the missing-snapshot test fails, update `GetByIdAsync` to also fall back to full replay when the component count is zero:

```csharp
// Check if any components exist
var hasComponents = await _dbContext.GameSessionComponents.AsNoTracking()
    .AnyAsync(c => c.SessionId == id.Value, cancellationToken)
    .ConfigureAwait(false);

if (!hasComponents)
{
    return await LoadFromEventsAsync(id, cancellationToken).ConfigureAwait(false);
}
```

Or, more robustly, wrap the `ToAggregate` call in a try-catch and fall back to `LoadFromEventsAsync` on any exception (missing component, deserialization error, etc.).

- [ ] **Step 4: Fix any failures and re-run**

If tests fail, fix the issues in `EfGameSessionRepository` (path selection, world reconstruction, etc.) and re-run. Do not change the test expectations — the test is the proof.

- [ ] **Step 5: Commit**

```bash
git add tests/WildBunch.Integration.Tests/FullReplayEqualityTests.cs src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs
git commit -m "Add full replay equality test: snapshot load == RehydrateFromEvents + projector

Proves that loading from the full event stream produces the same session as
loading from the snapshot, including TravelDiaryDays rebuilt via
TravelDiaryDayProjector. Tests purchase flow, journey cycle, and
missing-snapshot fallback."
```

---

### Task 5: Update existing tests for TravelDiaryDayProjector DI dependency

**Files:**
- Modify: Any test that creates `EfGameSessionRepository` directly (not via DI) — these need the `TravelDiaryDayProjector` parameter.

- [ ] **Step 1: Find tests that construct EfGameSessionRepository directly**

Search for `new EfGameSessionRepository` in test files:

```bash
grep -rn "new EfGameSessionRepository" tests/ --include="*.cs"
```

- [ ] **Step 2: Update each construction site**

For each site that constructs `EfGameSessionRepository` directly, add `new TravelDiaryDayProjector()` as the third constructor argument:

```csharp
var repo = new EfGameSessionRepository(dbContext, serializer, new TravelDiaryDayProjector());
```

- [ ] **Step 3: Build and run all tests**

Run: `dotnet build && dotnet test`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add <test files>
git commit -m "Update test constructions of EfGameSessionRepository for TravelDiaryDayProjector"
```

If no test files needed changes, skip this step.

---

### Task 6: Regenerate index mesh, run CI preflight, and open PR

- [ ] **Step 1: Regenerate index mesh**

Run: `python scripts/generate_index_mesh.py`
Then: `python scripts/generate_index_mesh.py --check`
Expected: exit code 0.

- [ ] **Step 2: Commit index mesh if changed**

```bash
git add .agents/INDEX.md
git commit -m "Regenerate index mesh for full replay load path"
```

If no INDEX.md files changed, skip this step.

- [ ] **Step 3: Run CI preflight**

Run: `.\scripts\ci-preflight.ps1`
Expected: all checks pass (backend, frontend, index-mesh).

If backend fails, run `dotnet build` and `dotnet test` to identify the issue. If frontend fails, investigate — this plan should not affect frontend. If index-mesh fails, regenerate and re-commit.

- [ ] **Step 4: Push branch and open draft PR**

```bash
git push -u origin <branch-name>
gh pr create --title "RehydrateFromEvents production load path: snapshot is now a shortcut cache" --draft --body "..."
```

- [ ] **Step 5: Mark PR ready for review**

After confirming CI preflight passes and the branch is current with `origin/main`, mark the PR ready for review.

---

## Self-Review

### Spec Coverage

- **Part 1a LoadFromEventsAsync:** Task 1 adds the method. ✓
- **Part 1a world reconstruction from WorldGenerated:** Task 1 reconstructs the world via `worldGenerated.World.ToDomain()`. ✓
- **Part 1a path selection (fast vs. full replay):** Task 2 wires the selection in `GetByIdAsync`. ✓
- **Part 1a diary day rebuild via projector:** Task 1 calls `TravelDiaryDayProjector.Project(events)` and `session.ReplaceTravelDiaryDays(...)`. ✓
- **Part 1a write path unchanged:** No changes to `StoreAsync`. ✓
- **Part 2e test 5 (full replay equality test):** Task 4 writes the test. ✓
- **DI registration:** Task 3 registers `TravelDiaryDayProjector`. ✓
- **Existing test updates:** Task 5 updates direct constructions. ✓

### Placeholder Scan

No TBDs, TODOs, or vague shorthand. The `LoadFromEventsAsync` method is fully specified. The path selection logic is fully specified. The test code is fully specified. The missing-snapshot fallback has a concrete troubleshooting guide.

### Type Consistency

- `TravelDiaryDayProjector` — injected into `EfGameSessionRepository` (Task 1), registered in DI (Task 3), used in tests (Task 4).
- `TravelDiaryDayProjection` — returned by `TravelDiaryDayProjector.Project()`, `.Days` accessed in `LoadFromEventsAsync`.
- `WorldGenerated.World.ToDomain()` — returns `DomainWorld`, passed to `RehydrateFromEvents`.
- `GameSession.ReplaceTravelDiaryDays` — existing method, takes `IReadOnlyList<TravelDiaryDayState>`.
- `GameSession.SetCommittedEvents` — existing method, takes `IReadOnlyList<IDomainEvent>`.

## Execution Confidence Assessment

### Direct Execution Confidence: 8/10

The `LoadFromEventsAsync` method is fully specified — it's a straightforward composition of existing pieces (`RehydrateFromEvents`, `WorldGenerated.World.ToDomain()`, `TravelDiaryDayProjector.Project()`, `ReplaceTravelDiaryDays`). The path selection logic is simple. The main risk is the missing-snapshot fallback test — the path selection may need to handle the case where components are missing but the snapshot version is current. The troubleshooting guide in Task 4 covers this.

### SDD Confidence: 7/10

The code is concrete enough for transcription, but the missing-snapshot test may require debugging the path selection logic. A subagent can't ask questions about the fallback strategy. However, the troubleshooting guide provides two concrete options (check component count, or try-catch fallback). The integration tests require PostgreSQL (Testcontainers) which may not be available in all execution environments — the subagent needs to verify Testcontainers is working before running the tests.

### Gap Closure Summary

- **World reconstruction:** Verified that `WorldGenerated` carries `WorldSnapshot` and `WorldSnapshot.ToDomain()` exists. The full replay path reconstructs the world from the event stream, not the snapshot.
- **Diary day rebuild:** `TravelDiaryDayProjector` (from Plan B) provides the diary days. `GameSession.ReplaceTravelDiaryDays` sets them on the rehydrated session.
- **Path selection:** Simple version check: `SnapshotVersion != StreamVersion` → full replay. The missing-snapshot case (components deleted) needs an additional check or try-catch fallback.
- **Committed events:** `session.SetCommittedEvents(events)` is called on the full replay path, matching the snapshot path (line 411).
- **DI registration:** `TravelDiaryDayProjector` is stateless, so singleton registration is correct.
- **Test infrastructure:** `EventSourcingEndToEndTests.CreateServices` pattern is followed for the new test's `CreateServices` method.

### Open Questions

1. **Missing-snapshot fallback strategy:** The path selection checks `SnapshotVersion != StreamVersion`, but a missing snapshot (deleted component rows) with a current `SnapshotVersion` won't trigger the full replay path. The troubleshooting guide in Task 4 provides two options: (a) check component count, or (b) try-catch fallback in `ToAggregate`. The implementer should choose the simpler option that passes the test.

2. **Integration test environment:** The full replay equality tests require PostgreSQL via Testcontainers. The subagent needs to verify that Testcontainers is working before running the tests. If Testcontainers is not available, the tests can't run — this is an environment constraint, not a plan gap.

3. **`GetByIdAsync` double envelope query:** The path selection adds one extra query (the envelope check) before `LoadStoreAsync` (which also loads the envelope). This is a minor performance cost. The implementer may refactor `LoadStoreAsync` to accept the envelope as a parameter to avoid the double load, but this is optional and should not change `LoadStoreAsync`'s behavior.
