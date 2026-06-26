# BUNCH-86: Fix Purchase Journal Projection Regression and Retire Legacy Log Hangers-On

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restore purchase log entries in the projection-backed `/journal` read path by moving purchase log production into `Apply(StoreItemPurchased)`, then retire the `GameSessionLogEntries` table and dead `CompleteCase` stub as the backend architecture campaign closeout.

**Architecture:** The purchase flow has an event-recording-beside-mutation seam: `Purchase()` calls `Apply(StoreItemPurchased)` for wallet/inventory mutation, then appends a `GameLogEntryKind.Purchase` log entry via `AddLogEntry()` outside `Apply`. `JournalLogProjector` skips `StoreItemPurchased`, so the projection-backed `/journal` route misses purchase entries. The fix moves the log entry into `Apply(StoreItemPurchased)` and teaches `JournalLogProjector` to project it. After this fix, every `AddLogEntry` call site lives inside an `Apply` method (or is dead code), making the `GameSessionLogEntries` table fully event-derivable and redundant. The closeout removes the table, switches command-load to event-derived log entries via `JournalLogProjector`, and removes the dead `CompleteCase` stub.

**Tech Stack:** C#/.NET, xUnit, EF Core, PostgreSQL, event sourcing with typed domain events and projectors.

## Global Constraints

- `GameSession` is the live-play aggregate root; gameplay mutations flow through `Apply` methods.
- `AddLogEntry` is `[Obsolete]` projection-legacy per ADR-0028; no new call sites.
- `JournalLogProjector` is the projection-backed replacement for the `GameSessionLogEntries` table on read paths (BUNCH-84).
- The `/journal` endpoint derives log entries from `StoredEvents` via `JournalLogProjector`; the `GameSessionReadStoreLoader` does not query `GameSessionLogEntries`.
- The command-load path (`EfGameSessionRepository.LoadStoreAsync`) currently reads `GameSessionLogEntries` as bounded compatibility surface.
- `StoreItemPurchased` event carries: `TownId`, `ItemKind`, `DisplayName`, `Quantity`, `UnitPrice`, `TotalPrice`, `WalletAfter`.
- Purchase does not advance the clock (no `TownActionContextEntered` or `GameClock.Advance` in the purchase path); the log entry uses the current `Clock.Day` / `Clock.Turn`.
- The existing purchase log message format is: `$"Purchased {quantityLabel} for ${totalPrice:0.00}."` where `quantityLabel = quantity == 1 ? displayName : $"{quantity} {displayName}"`.
- The `GameSessionDiaryDays` table is an intentional materialized read model (`TravelDiaryDayState` is a rich per-day state object not derivable from the current `DiaryProjector`); it is NOT a hanger-on and is out of scope for removal.
- The snapshot `Serialize`/`Deserialize` methods (`GameSessionJsonSerializer.SessionSnapshot`) are test-only; no production code calls them. Their `LogEntries` field is not a production hanger-on.
- `JournalResolver` reads `session.LogEntries` for investigation command handlers. After the fix, `session.LogEntries` is fully event-derived. Whether this surface is a live player-facing aggregate-log read path that should be switched to a projection-backed route, or an internal command-path read that is lawful as-is, must be determined by the closeout audit (Task 13) with source evidence — not pre-classified here.
- Command response DTOs (`GameSessionMapper.ToDto`) read `session.LogEntries`. Same as `JournalResolver` — classification deferred to the closeout audit with source evidence.
- Validation: `dotnet build`, `dotnet test`, `dotnet ef migrations list`, `.\scripts\postgres-dev.ps1 validate`.
- Worker environment uses PowerShell; no `&&` chaining.

---

## Phase 1: Purchase Regression Fix (TDD)

### Task 1: Write Failing Projector Test for StoreItemPurchased

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs`

**Interfaces:**
- Consumes: `JournalLogProjector`, `StoreItemPurchased`, `GameStarted`
- Produces: A failing test that proves `JournalLogProjector` does not project `StoreItemPurchased`

- [ ] **Step 1: Add a failing test asserting StoreItemPurchased produces a Purchase log entry**

Add this test to `JournalLogProjectorTests`:

```csharp
[Fact]
public void StoreItemPurchased_ProducesPurchaseEntry_MatchingLegacyCommandPath()
{
    var projector = new JournalLogProjector();
    var events = new IDomainEvent[]
    {
        GameStartedEvent(),
        new StoreItemPurchased
        {
            TownId = new TownId("pinecross"),
            ItemKind = ItemKind.Food,
            DisplayName = "Trail Biscuits",
            Quantity = 2,
            UnitPrice = 2m,
            TotalPrice = 4m,
            WalletAfter = 21m
        }
    };
    var log = projector.Project(events);

    // Opening + purchase entry
    Assert.Equal(2, log.Count);
    Assert.Equal(GameLogEntryKind.Purchase, log[1].Kind);
    Assert.Equal("Purchased 2 Trail Biscuits for $4.00.", log[1].Message);
    Assert.Equal(1, log[1].Day);
    Assert.Equal(0, log[1].Turn);
}

[Fact]
public void StoreItemPurchased_SingleQuantity_UsesDisplayNameWithoutQuantityPrefix()
{
    var projector = new JournalLogProjector();
    var events = new IDomainEvent[]
    {
        GameStartedEvent(),
        new StoreItemPurchased
        {
            TownId = new TownId("pinecross"),
            ItemKind = ItemKind.Canteen,
            DisplayName = "Canteen",
            Quantity = 1,
            UnitPrice = 3m,
            TotalPrice = 3m,
            WalletAfter = 22m
        }
    };
    var log = projector.Project(events);

    Assert.Equal(2, log.Count);
    Assert.Equal(GameLogEntryKind.Purchase, log[1].Kind);
    Assert.Equal("Purchased Canteen for $3.00.", log[1].Message);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~JournalLogProjectorTests.StoreItemPurchased_ProducesPurchaseEntry"`
Expected: FAIL — projector skips `StoreItemPurchased`, so `log.Count` is 1, not 2.

Note: Do NOT use `--no-build` here — the test assembly must be rebuilt to pick up the newly added test. Use `--no-build` only on subsequent runs after an explicit build.

- [ ] **Step 3: Commit the failing test**

```powershell
git add tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs
git commit -m "BUNCH-86: add failing projector test for StoreItemPurchased log entry"
```

### Task 2: Write Failing Equivalence Test for Purchase Command/Replay/Projection

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs`

**Interfaces:**
- Consumes: `JournalLogProjector`, `GameSession`, `TownStoreCatalogResolver`, `TravelTestFactory` (for world/case setup)
- Produces: A failing test proving command-path `session.LogEntries` and projected log entries disagree for purchase

- [ ] **Step 1: Add a failing equivalence test for purchase**

Add this test to `JournalLogProjectorEquivalenceTests`:

```csharp
[Fact]
public void Purchase_ProjectedLogMatchesCommandPathLogEntriesExactly()
{
    var (session, gameStarted) = TravelTestFactory.CreateSessionWithGameStarted();
    session.MarkEventsCommitted();

    var resolver = new TownStoreCatalogResolver();
    var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
        .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == ItemKind.Food);

    session.Purchase(offer, 2);

    var events = new[] { gameStarted }.Concat(session.UncommittedEvents).ToList();
    var projected = new JournalLogProjector().Project(events);

    Assert.Equal(session.LogEntries.Count, projected.Count);
    for (var i = 0; i < session.LogEntries.Count; i++)
    {
        Assert.Equal(session.LogEntries[i].Kind, projected[i].Kind);
        Assert.Equal(session.LogEntries[i].Message, projected[i].Message);
        Assert.Equal(session.LogEntries[i].Day, projected[i].Day);
        Assert.Equal(session.LogEntries[i].Turn, projected[i].Turn);
    }
}
```

Note: If `TravelTestFactory.CreateSessionWithGameStarted` does not exist, check the existing factory methods in `TravelTestFactory` and use the appropriate one that returns a `(GameSession, GameStarted)` tuple. The existing `JournalLogProjectorEquivalenceTests` already uses `TravelTestFactory.CreateSixDayQuietJourneyWithGameStarted()` which returns `(session, preview, gameStarted)`. Adapt accordingly — the key requirement is a session with a captured `GameStarted` event and access to store offers.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~JournalLogProjectorEquivalenceTests.Purchase_ProjectedLogMatchesCommandPath"`
Expected: FAIL — `session.LogEntries` has the purchase entry (from `AddLogEntry` in `Purchase()`), but the projection skips `StoreItemPurchased`, so counts differ.

Note: Do NOT use `--no-build` here — the test assembly must be rebuilt to pick up the newly added test.

- [ ] **Step 3: Commit the failing test**

```powershell
git add tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs
git commit -m "BUNCH-86: add failing equivalence test for purchase log projection"
```

### Task 3: Write Failing Integration Test for Purchase-Then-Journal

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/GameApiJournalTests.cs`

**Interfaces:**
- Consumes: `PostgreSqlApiFactory`, `BoringScenarioBuilder`, `JournalDto`, `GameLogEntryKind`
- Produces: A failing integration test proving `/journal` misses purchase entries through the projection-backed read path

- [ ] **Step 1: Add a failing integration test**

Add this test to `GameApiJournalTests`:

```csharp
[Fact]
public async Task GetJournalAfterPurchaseIncludesPurchaseLogEntry()
{
    using var factory = new PostgreSqlApiFactory();
    using var client = factory.CreateClient();

    var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
    scenario.AssertReady();

    var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
    var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
    Assert.NotNull(createdSession);

    await scenario.Fixture.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

    var buyResponse = await client.PostAsJsonAsync(
        $"/api/games/{createdSession.Id}/towns/pinecross/store/buy",
        new BuyStoreItemRequest(WildBunch.Domain.Economy.StoreVendorType.GeneralStore, WildBunch.Domain.Inventory.ItemKind.Food, 2));
    var buyResult = await buyResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();
    Assert.NotNull(buyResult);
    Assert.True(buyResult!.Success);

    var journalResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal");
    Assert.Equal(HttpStatusCode.OK, journalResponse.StatusCode);

    var journal = await journalResponse.Content.ReadFromJsonAsync<JournalDto>();
    Assert.NotNull(journal);
    Assert.Contains(journal!.LogEntries, entry => entry.Kind == GameLogEntryKind.Purchase);
    var purchaseEntry = journal.LogEntries.Single(entry => entry.Kind == GameLogEntryKind.Purchase);
    Assert.Equal("Purchased 2 Food for $4.00.", purchaseEntry.Message);
}
```

Note: Check `GameApiPurchaseTests.cs` for the exact `BuyStoreItemRequest` constructor signature and `BoringScenarioBuilder` usage pattern — mirror it. The `BuyStoreItemRequest` takes `(StoreVendorType, ItemKind, int quantity)`.

- [ ] **Step 2: Run test to verify it fails**

Run: `.\scripts\postgres-dev.ps1 ensure` then `.\scripts\postgres-dev.ps1 test -- --filter "FullyQualifiedName~GameApiJournalTests.GetJournalAfterPurchaseIncludesPurchaseLogEntry"`
Expected: FAIL — `journal.LogEntries` has no `Purchase` entry because `JournalLogProjector` skips `StoreItemPurchased`.

Note: Do NOT use `--no-build` here — the integration test assembly must be rebuilt to pick up the newly added test.

- [ ] **Step 3: Commit the failing test**

```powershell
git add tests/WildBunch.Integration.Tests/GameApiJournalTests.cs
git commit -m "BUNCH-86: add failing integration test for purchase-then-journal"
```

### Task 4: Fix Apply(StoreItemPurchased) and Purchase()

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (lines 704-709 for `Apply(StoreItemPurchased)`, lines 2296-2312 for `Purchase()`)

**Interfaces:**
- Consumes: `StoreItemPurchased` event fields (`DisplayName`, `Quantity`, `TotalPrice`)
- Produces: `Apply(StoreItemPurchased)` appends the purchase log entry; `Purchase()` no longer calls `AddLogEntry` after Apply

- [ ] **Step 1: Move the purchase log entry into Apply(StoreItemPurchased)**

Replace the `Apply(StoreItemPurchased e)` method (currently lines 704-709):

```csharp
private void Apply(StoreItemPurchased e)
{
    Player.SpendCash(e.TotalPrice);
    Player.AddItem(e.ItemKind, e.Quantity);
    var quantityLabel = e.Quantity == 1 ? e.DisplayName : $"{e.Quantity} {e.DisplayName}";
    AddLogEntry(GameLogEntryKind.Purchase, $"Purchased {quantityLabel} for ${e.TotalPrice:0.00}.");
    _version++;
}
```

- [ ] **Step 2: Remove the AddLogEntry call from Purchase()**

In `Purchase()` (around line 2310-2312), remove the `AddLogEntry` line. Keep the `quantityLabel` computation and the `StorePurchaseResult.Succeeded` return. The method should end:

```csharp
        Apply(e);
        _uncommittedEvents.Add(e);

        var quantityLabel = quantity == 1 ? offer.DisplayName : $"{quantity} {offer.DisplayName}";
        return StorePurchaseResult.Succeeded($"Purchased {quantityLabel} for ${totalPrice:0.00}.");
```

- [ ] **Step 3: Build and run targeted tests to verify the expected intermediate state**

Run: `dotnet build` then `dotnet test tests/WildBunch.Application.Tests --no-build --filter "FullyQualifiedName~JournalLogProjectorTests.StoreItemPurchased"` and `dotnet test tests/WildBunch.Domain.Tests --no-build --filter "FullyQualifiedName~JournalLogProjectorEquivalenceTests.Purchase_ProjectedLogMatchesCommandPath"`

Expected: BOTH STILL FAIL. This is the correct intermediate state:
- The projector test (Task 1) still fails because `JournalLogProjector` still skips `StoreItemPurchased` — the projector is not updated until Task 5.
- The equivalence test (Task 2) still fails because `session.LogEntries` now has the purchase entry (from `Apply`), but the projection still skips `StoreItemPurchased`, so `session.LogEntries.Count` > `projected.Count`.

What this step proves: the source change compiles and `Apply(StoreItemPurchased)` is now the single mutation+log path for purchase (mutation authority fixed). The projection fix in Task 5 is what makes both tests pass. Do NOT expect green here — the red/green sequence is:
1. projector/integration/equivalence tests fail against current source (Tasks 1-3);
2. moving the log into `Apply(StoreItemPurchased)` fixes command/replay mutation authority but does not by itself fix projection (this step — still red);
3. updating `JournalLogProjector` makes projection and API journal behavior pass (Task 5 — green).

- [ ] **Step 4: Commit the Apply/Purchase fix**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-86: move purchase log entry into Apply(StoreItemPurchased), remove AddLogEntry from Purchase()"
```

### Task 5: Update JournalLogProjector for StoreItemPurchased

**Files:**
- Modify: `src/WildBunch.Application/Projections/JournalLogProjector.cs` (lines 39-41)

**Interfaces:**
- Consumes: `StoreItemPurchased` event fields
- Produces: `JournalLogProjector` projects `StoreItemPurchased` to a `GameLogEntryKind.Purchase` entry

- [ ] **Step 1: Replace the StoreItemPurchased skip case with projection logic**

In `JournalLogProjector.Project()`, replace:

```csharp
case StoreItemPurchased:
    // Legacy Apply adds no log entry for purchases.
    break;
```

with:

```csharp
case StoreItemPurchased p:
    var purchaseQuantityLabel = p.Quantity == 1 ? p.DisplayName : $"{p.Quantity} {p.DisplayName}";
    entries.Add(new GameLogEntry(GameLogEntryKind.Purchase, $"Purchased {purchaseQuantityLabel} for ${p.TotalPrice:0.00}.", day, turn));
    break;
```

- [ ] **Step 2: Run all projector and equivalence tests to verify they pass**

Run: `dotnet build` then `dotnet test tests/WildBunch.Application.Tests --no-build --filter "FullyQualifiedName~JournalLogProjectorTests"` and `dotnet test tests/WildBunch.Domain.Tests --no-build --filter "FullyQualifiedName~JournalLogProjectorEquivalenceTests"`
Expected: PASS for all — this is the green step. `JournalLogProjector` now projects `StoreItemPurchased`, so the projector tests, equivalence test, and command/replay/projection paths all agree.

- [ ] **Step 3: Run the integration test to verify it passes**

Run: `.\scripts\postgres-dev.ps1 test -- --filter "FullyQualifiedName~GameApiJournalTests.GetJournalAfterPurchaseIncludesPurchaseLogEntry"`
Expected: PASS — `/journal` now includes the purchase entry via the projection. (No `--no-build` — the integration test assembly must be rebuilt to pick up the projector source change.)

- [ ] **Step 4: Commit the projector fix**

```powershell
git add src/WildBunch.Application/Projections/JournalLogProjector.cs
git commit -m "BUNCH-86: project StoreItemPurchased to Purchase log entry in JournalLogProjector"
```

### Task 6: Update Existing Tests and Guardrails

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs` (the old `StoreItemPurchased_ProducesNoLogEntry_MatchingLegacyApply` test)
- Modify: `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs` (update `KnownLegacyAddLogEntryCallSiteCount`)

**Interfaces:**
- Consumes: The old test that asserted `StoreItemPurchased` produces no log entry (now wrong)
- Produces: Updated tests that reflect the new behavior and reduced guardrail count

- [ ] **Step 1: Remove or replace the old StoreItemPurchased_ProducesNoLogEntry test**

The test `StoreItemPurchased_ProducesNoLogEntry_MatchingLegacyApply` (lines 39-62) is now wrong — `StoreItemPurchased` DOES produce a log entry. Remove this test entirely; it is superseded by the new `StoreItemPurchased_ProducesPurchaseEntry_MatchingLegacyCommandPath` and `StoreItemPurchased_SingleQuantity_UsesDisplayNameWithoutQuantityPrefix` tests from Task 1.

- [ ] **Step 2: Update the AddLogEntry guardrail count**

In `AddLogEntryGuardrailTests.cs`, update the constant and comment:

```csharp
// Known count of AddLogEntry references in GameSession.cs after BUNCH-86.
// This includes the method definition itself (private void AddLogEntry(...))
// plus 4 call sites: Apply(GameStarted), RecordTravelUpdate (called from
// travel Apply methods), RecordCaseUpdate (called from investigation Apply
// methods), and CompleteCase (dead stub — no production callers).
// BUNCH-86 moved the purchase AddLogEntry into Apply(StoreItemPurchased),
// dropping the count from 6 to 5. Do not increase this number without
// explicit architecture approval. AddLogEntry is [Obsolete] projection-legacy
// per ADR-0028.
private const int KnownLegacyAddLogEntryCallSiteCount = 5;
```

- [ ] **Step 3: Run guardrail and projector tests to verify they pass**

Run: `dotnet build` then `dotnet test tests/WildBunch.Application.Tests --no-build --filter "FullyQualifiedName~AddLogEntryGuardrailTests|FullyQualifiedName~JournalLogProjectorTests"`
Expected: PASS. (Build first — test files changed, so the assembly must be rebuilt before `--no-build`.)

- [ ] **Step 4: Commit the test and guardrail updates**

```powershell
git add tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs
git commit -m "BUNCH-86: update projector tests and guardrail count for purchase log entry"
```

---

## Phase 2: Closeout — Retire GameSessionLogEntries Table

After Phase 1, every `AddLogEntry` call site lives inside an `Apply` method (or is the dead `CompleteCase` stub). The event stream fully determines `LogEntries`. The `GameSessionLogEntries` table is redundant with `JournalLogProjector` projection from `StoredEvents`. This phase removes the table and switches the command-load path to event-derived log entries.

### Task 7: Switch Command-Load to Event-Derived LogEntries

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` (`LoadStoreAsync` method, around lines 155-210)

**Interfaces:**
- Consumes: `JournalLogProjector` (from `WildBunch.Application.Projections`), `StoredEvents` query
- Produces: `LoadStoreAsync` derives `LogEntries` from the event stream via `JournalLogProjector` instead of the `GameSessionLogEntries` table

- [ ] **Step 1: Add JournalLogProjector field to EfGameSessionRepository**

Add a `JournalLogProjector` field initialized in the constructor (or as a static singleton since it's stateless):

```csharp
private readonly JournalLogProjector _journalLogProjector = new();
```

Add the using: `using WildBunch.Application.Projections;`

- [ ] **Step 2: Replace the LogEntries table query with event-stream projection in LoadStoreAsync**

In `LoadStoreAsync`, remove the `GameSessionLogEntries` query (lines 168-173):

```csharp
// REMOVE:
var logEntries = await _dbContext.GameSessionLogEntries.AsNoTracking()
    .Where(entry => entry.SessionId == id.Value)
    .OrderBy(entry => entry.Sequence)
    .Select(entry => new GameLogEntry(entry.Kind, entry.Message, entry.Day, entry.Turn))
    .ToArrayAsync(cancellationToken)
    .ConfigureAwait(false);
```

Replace the post-snapshot events load (lines 184-202) with a full event stream load that serves both projection and post-snapshot replay:

```csharp
// Load all events for log-entry projection and post-snapshot replay.
// After BUNCH-86, LogEntries are derived from the event stream via
// JournalLogProjector, replacing the GameSessionLogEntries table.
var allStoredEvents = await _dbContext.StoredEvents.AsNoTracking()
    .Where(e => e.StreamId == id.Value)
    .OrderBy(e => e.Sequence)
    .ToArrayAsync(cancellationToken)
    .ConfigureAwait(false);

var allEvents = new IDomainEvent[allStoredEvents.Length];
for (var i = 0; i < allStoredEvents.Length; i++)
{
    allEvents[i] = _serializer.DeserializeEvent(allStoredEvents[i].EventType, allStoredEvents[i].PayloadJson);
}

var logEntries = _journalLogProjector.Project(allEvents);

// Post-snapshot events for state replay (subset of allEvents).
IReadOnlyList<IDomainEvent> postSnapshotEvents = Array.Empty<IDomainEvent>();
if (envelope.SnapshotVersion < envelope.StreamVersion)
{
    postSnapshotEvents = allEvents
        .Skip((int)envelope.SnapshotVersion)
        .ToArray();
}
```

Update the `GameSessionStore` construction to use `logEntries` from the projection.

Note: The `GameSessionStore` record already has a `LogEntries` field — pass the projected `logEntries` there. The `PostSnapshotEvents` field is still needed for state replay.

- [ ] **Step 3: Build and run domain + application tests to verify no regression**

Run: `dotnet build` then `dotnet test tests/WildBunch.Domain.Tests tests/WildBunch.Application.Tests --no-build`
Expected: PASS.

- [ ] **Step 4: Commit the command-load switch**

```powershell
git add src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs
git commit -m "BUNCH-86: derive command-load LogEntries from event stream via JournalLogProjector"
```

### Task 8: Remove Table Write, Entity, Configuration, and DbSet

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` (remove `SyncLogEntriesAsync` call and method)
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionEntity.cs` (remove `LogEntries` navigation)
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionEntityConfiguration.cs` (remove `LogEntries` relationship)
- Delete: `src/WildBunch.Persistence/GameSessions/GameSessionLogEntryEntity.cs`
- Delete: `src/WildBunch.Persistence/GameSessions/GameSessionLogEntryEntityConfiguration.cs`
- Modify: `src/WildBunch.Persistence/WildBunchDbContext.cs` (remove `GameSessionLogEntries` DbSet)

**Interfaces:**
- Produces: No more `GameSessionLogEntries` table writes, reads, entity, configuration, or DbSet

- [ ] **Step 1: Remove SyncLogEntriesAsync call and method from EfGameSessionRepository**

In `StoreAsync`, remove the line:
```csharp
await SyncLogEntriesAsync(entity.Id, session.LogEntries, cancellationToken).ConfigureAwait(false);
```

Remove the entire `SyncLogEntriesAsync` method (lines 324-364).

- [ ] **Step 2: Remove LogEntries navigation from GameSessionEntity**

Remove line 23:
```csharp
public ICollection<GameSessionLogEntryEntity> LogEntries { get; set; } = [];
```

- [ ] **Step 3: Remove LogEntries relationship from GameSessionEntityConfiguration**

Remove lines 42-45:
```csharp
builder.HasMany(e => e.LogEntries)
    .WithOne(e => e.Session)
    .HasForeignKey(e => e.SessionId)
    .OnDelete(DeleteBehavior.Cascade);
```

- [ ] **Step 4: Delete GameSessionLogEntryEntity.cs and GameSessionLogEntryEntityConfiguration.cs**

```powershell
git rm src/WildBunch.Persistence/GameSessions/GameSessionLogEntryEntity.cs
git rm src/WildBunch.Persistence/GameSessions/GameSessionLogEntryEntityConfiguration.cs
```

- [ ] **Step 5: Remove DbSet from WildBunchDbContext**

Remove line 17:
```csharp
public DbSet<GameSessionLogEntryEntity> GameSessionLogEntries => Set<GameSessionLogEntryEntity>();
```

- [ ] **Step 6: Build to verify no compilation errors**

Run: `dotnet build`
Expected: PASS — no remaining references to `GameSessionLogEntryEntity`.

- [ ] **Step 7: Commit the table removal**

```powershell
git add -A
git commit -m "BUNCH-86: remove GameSessionLogEntries table entity, configuration, and DbSet"
```

### Task 9: Add EF Migration to Drop GameSessionLogEntries Table

**Files:**
- Create: `src/WildBunch.Persistence/Migrations/<timestamp>_DropGameSessionLogEntries.cs` (generated by EF CLI)

- [ ] **Step 1: Generate the migration**

Run:
```powershell
dotnet tool restore
$env:ConnectionStrings__WildBunchPostgresDb = "Host=localhost;Port=5434;Database=wildbunch_dev;Username=postgres;Password=postgres"
dotnet ef migrations add DropGameSessionLogEntries --project src/WildBunch.Persistence --startup-project src/WildBunch.Api
```

- [ ] **Step 2: Verify the migration drops the table**

Read the generated migration file. It should contain a `DropTable("GameSessionLogEntries")` operation. If it also drops `GameSessionDiaryDays` or other tables, STOP — something is wrong with the model changes.

- [ ] **Step 3: Verify migration list**

Run:
```powershell
.\scripts\postgres-dev.ps1 ensure
$env:ConnectionStrings__WildBunchPostgresDb = "Host=localhost;Port=5434;Database=wildbunch_dev;Username=postgres;Password=postgres"
dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api
```
Expected: The new migration appears at the top of the list.

- [ ] **Step 4: Commit the migration**

```powershell
git add src/WildBunch.Persistence/Migrations/
git commit -m "BUNCH-86: add migration to drop GameSessionLogEntries table"
```

### Task 10: Update Guardrail Tests for Table Removal

**Files:**
- Modify: `tests/WildBunch.Application.Tests/ReadStoreLoaderJournalProjectionGuardrailTests.cs`

**Interfaces:**
- Produces: Guardrail tests that assert neither the read-store loader NOR the command-load repository queries `GameSessionLogEntries`

- [ ] **Step 1: Update the second guardrail test**

The test `EfGameSessionRepository_StillQueriesGameSessionLogEntriesTable_AsBoundedCompatibilitySurface` is now wrong — the repository no longer queries the table. Replace it with:

```csharp
[Fact]
public void EfGameSessionRepository_NoLongerQueriesGameSessionLogEntriesTable()
{
    var repoRoot = FindRepoRoot();
    var repoPath = Path.Combine(repoRoot, "src", "WildBunch.Persistence", "GameSessions", "EfGameSessionRepository.cs");
    Assert.True(File.Exists(repoPath), $"Could not find EfGameSessionRepository.cs at {repoPath}.");

    var source = File.ReadAllText(repoPath);

    // After BUNCH-86, the command-load path derives LogEntries from the event
    // stream via JournalLogProjector, matching the read-store loader. The
    // GameSessionLogEntries table is fully removed.
    Assert.DoesNotContain("GameSessionLogEntries", source);
    Assert.Contains("JournalLogProjector", source);
}
```

Update the class-level doc comment to reflect the BUNCH-86 closeout.

- [ ] **Step 2: Run guardrail tests to verify they pass**

Run: `dotnet build` then `dotnet test tests/WildBunch.Application.Tests --no-build --filter "FullyQualifiedName~ReadStoreLoaderJournalProjectionGuardrailTests"`
Expected: PASS. (Build first — test file changed, so the assembly must be rebuilt before `--no-build`.)

- [ ] **Step 3: Commit the guardrail update**

```powershell
git add tests/WildBunch.Application.Tests/ReadStoreLoaderJournalProjectionGuardrailTests.cs
git commit -m "BUNCH-86: update guardrail tests for GameSessionLogEntries table removal"
```

---

## Phase 3: Closeout — Remove Dead Code

### Task 11: Remove CompleteCase Dead Stub

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (lines 2722-2726)

**Interfaces:**
- Produces: `CompleteCase` method removed; no more dead non-event-sourced `AddLogEntry` call site

- [ ] **Step 1: Verify CompleteCase has no production callers**

Run: search the entire `src/` tree for `CompleteCase` references outside of `GameSession.cs`.
Expected: No production callers. Only the definition and plan/test references exist.

- [ ] **Step 2: Remove the CompleteCase method**

Remove lines 2722-2726:
```csharp
public void CompleteCase(string message)
{
    Status = GameStatus.Completed;
    AddLogEntry(GameLogEntryKind.CaseUpdate, message);
}
```

- [ ] **Step 3: Update the AddLogEntry guardrail count**

In `AddLogEntryGuardrailTests.cs`, update `KnownLegacyAddLogEntryCallSiteCount` from 5 to 4:

```csharp
// Known count of AddLogEntry references in GameSession.cs after BUNCH-86.
// This includes the method definition itself (private void AddLogEntry(...))
// plus 3 call sites: Apply(GameStarted), RecordTravelUpdate (called from
// travel Apply methods), and RecordCaseUpdate (called from investigation
// Apply methods). BUNCH-86 moved purchase into Apply and removed the dead
// CompleteCase stub. Do not increase this number.
private const int KnownLegacyAddLogEntryCallSiteCount = 4;
```

- [ ] **Step 4: Build and run tests to verify no regression**

Run: `dotnet build` then `dotnet test tests/WildBunch.Domain.Tests tests/WildBunch.Application.Tests --no-build --filter "FullyQualifiedName~AddLogEntryGuardrailTests"`
Expected: PASS.

- [ ] **Step 5: Commit the dead code removal**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs
git commit -m "BUNCH-86: remove dead CompleteCase stub, update guardrail count to 4"
```

---

## Phase 4: Full Validation and Closeout Inspection

### Task 12: Run Full Backend Validation

- [ ] **Step 1: Run the full PostgreSQL-backed validation lane**

Run:
```powershell
.\scripts\postgres-dev.ps1 ensure
.\scripts\postgres-dev.ps1 validate
```
Expected: Build passes, EF migrations list passes, all tests pass.

- [ ] **Step 2: Run targeted purchase/journal tests explicitly**

Run:
```powershell
.\scripts\postgres-dev.ps1 test -- --no-build --filter "FullyQualifiedName~GameApiJournalTests|FullyQualifiedName~GameApiPurchaseTests|FullyQualifiedName~StorePurchaseAcceptanceTests|FullyQualifiedName~JournalLogProjectorTests|FullyQualifiedName~JournalLogProjectorEquivalenceTests|FullyQualifiedName~AddLogEntryGuardrailTests|FullyQualifiedName~ReadStoreLoaderJournalProjectionGuardrailTests"
```
Expected: All PASS. (`--no-build` is safe here — Step 1's `validate` already built all assemblies.)

### Task 13: Repo-Wide Architecture Closeout Inspection

This task is a structured audit, not code changes. The worker must inspect current source after all fixes and record evidence for each checklist item. The audit results go into the PR description and the worker return report.

- [ ] **Step 1: Aggregate mutation authority audit**

Search for direct gameplay state mutation outside event-applied paths in migrated flows. Verify:
- `Purchase()` calls `Apply(StoreItemPurchased)` and `_uncommittedEvents.Add(e)` — no direct `AddLogEntry` after Apply.
- `Apply(StoreItemPurchased)` is the single mutation+log path for purchase.
- `StartNew` produces `GameStarted` and applies it.
- Investigation, saloon/bounty, travel/journey, encounter flows produce typed events and mutate through `Apply`.
- Wallet/inventory mutation happens only inside `Apply` methods for migrated flows.

Record: grep for `AddLogEntry` in `GameSession.cs` — verify all remaining call sites are inside `Apply` methods or `RecordCaseUpdate`/`RecordTravelUpdate` (which are called from `Apply` methods).

- [ ] **Step 2: Legacy log compatibility audit**

Search for all references to: `AddLogEntry`, `RecordCaseUpdate`, `RecordTravelUpdate`, `_logEntries`, `LogEntries`, `JournalResolver`, `GameSessionLogEntries`, `SyncLogEntriesAsync`, snapshot `LogEntries`.

For each reference found, the worker must inspect the actual source and classify it as one of:
- **removed** — no longer present in `src/` after this issue;
- **retained with source-backed reason** — lawful, intentional, with a concrete source-backed justification;
- **harmless/temporary with source-backed reason** — not a live player-facing journal/read-model path, or data is provably identical to the projection, with source evidence;
- **closure blocker** — a live player-facing aggregate-log read path where a projection-backed route is available but not used, requiring either a fix in this issue or an AMBER/BLOCKED return with exact follow-up evidence.

Do NOT pre-classify any surface. Inspect the source, record the file/line, and classify from evidence. In particular:
- `JournalResolver` reading `session.LogEntries` — inspect which handlers call it, whether those handlers serve player-facing journal/read-model output or internal command-path logic, and whether a projection-backed route is available. Classify from evidence.
- `GameSessionMapper.ToDto` reading `session.LogEntries` — inspect whether the DTO is a player-facing read surface, whether the data is event-derived after BUNCH-86, and whether a projection-backed route is available. Classify from evidence.
- `GameSessionDiaryDays` — inspect `TravelDiaryDayState` vs `DiaryProjector` output to determine whether the table is event-derivable or intentionally materialized. Classify from evidence.

GREEN is only allowed if every remaining surface is removed, fixed, or source-backed as lawful. If any surface is a live player-facing aggregate-log read path where a projection-backed route is available but not used, either fix it in this issue or return AMBER/BLOCKED with exact follow-up evidence.

- [ ] **Step 3: Projection-backed reads and CQRS posture audit**

Verify with source evidence:
- `/journal` endpoint derives log entries from `StoredEvents` via `JournalLogProjector` (in `GameSessionReadStoreLoader`). Record the file/line that proves this.
- Command-load path (`EfGameSessionRepository.LoadStoreAsync`) now also derives log entries from `StoredEvents` via `JournalLogProjector`. Record the file/line that proves this.
- Command handlers do not use query-side read models to mutate state. Inspect handler source for any read-model imports or queries.
- Player-facing read surfaces expose safe projections/DTOs, not raw events or hidden culprit truth. Verify `/journal` payload does not contain `trueCulpritId`, `isTrueCulprit`, `linkedSuspectIds`, `killerReleaseState`, `suspectCount`. (Existing `GameApiJournalTests` already assert this — record the test name.)
- `JournalResolver.Resolve(session)` callers — inspect each caller. If any caller serves a player-facing journal/read-model output where a projection-backed route is available, classify as closure blocker (fix in this issue or return AMBER/BLOCKED). If all callers are internal command-path logic where reading event-derived `session.LogEntries` is lawful, classify as retained with source-backed reason. Do NOT pre-classify.

- [ ] **Step 4: Event sourcing and replay compatibility audit**

Verify with source evidence:
- Typed events are registered/deserializable: inspect `GameSessionJsonSerializer.DeserializeEvent` and confirm it handles all event types used in the purchase flow.
- Command path, replay path, and projection path agree for purchase: confirm `JournalLogProjectorEquivalenceTests.Purchase_ProjectedLogMatchesCommandPath` and `GameApiJournalTests.GetJournalAfterPurchaseIncludesPurchaseLogEntry` pass and prove agreement. Record the test names.
- No event-recording-beside-mutation seams: inspect `Purchase()` source and confirm it no longer appends log entries outside `Apply`. Grep all `AddLogEntry` call sites and confirm each is inside an `Apply` method or helper called from `Apply`.
- Snapshots remain cache, not the only source: inspect `EfGameSessionRepository` load path and confirm it loads from snapshot + post-snapshot event replay, with log entries from event projection (not snapshot).

- [ ] **Step 5: Onion dependency direction audit**

Verify with source evidence:
- Domain does not depend on Application, Persistence, API, or frontend. Check `src/WildBunch.Domain/*.csproj` — no project references to Application/Persistence/Api.
- Application abstractions/handlers do not depend on Persistence implementation types. Check `src/WildBunch.Application/*.csproj` — references Domain and Abstractions, not Persistence.
- Persistence depends inward on Application/Domain. Check `src/WildBunch.Persistence/*.csproj` — references Application and Domain.
- `JournalLogProjector` is in `WildBunch.Application.Projections` — confirm the namespace/project layer is correct.

- [ ] **Step 6: Persistence/read-model hangers-on audit**

Inspect all tables in the EF model and migrations. For each table, determine whether it is:
- removed by this issue (verify no references remain in `src/`);
- an event-derivable read model that should be replaced by a projector (closure blocker if not fixed);
- an intentionally materialized read model with a source-backed reason (inspect the model vs available projectors to prove the data is not derivable).

In particular:
- `GameSessionLogEntries` — verify REMOVED by this issue. Confirm the migration drops it and no references remain in `src/`.
- `GameSessionDiaryDays` — inspect `TravelDiaryDayState` (the entity/model shape) vs `DiaryProjector` output. Determine whether the per-day state is derivable from the current projector or intentionally materialized. Classify from evidence — do NOT pre-classify as "intentionally retained."
- Check migrations for any other orphaned tables without a justified role.

- [ ] **Step 7: Dead or misleading code audit**

Verify with source evidence:
- `CompleteCase` — verify REMOVED by Task 11. Grep `src/` for any remaining references. Confirm no production callers existed before removal.
- No remaining direct `AddLogEntry` callers outside `Apply` methods. Grep `GameSession.cs` for all `AddLogEntry` call sites and verify each is inside an `Apply` method or a helper called only from `Apply` methods (`RecordCaseUpdate`, `RecordTravelUpdate`).
- ADR-0028 and related docs do not claim future projectors are missing when they now exist. Check `docs/adr/ADR-0028-*.md` for stale claims. If found, update or note as follow-up.

- [ ] **Step 8: Record closeout findings**

Compile the audit results into the PR description and worker return report as an evidence-shaped table. For each of the following surfaces, record: the searches/files inspected, the classification, and the source-backed reason.

Required surfaces to report (one row each):
- `GameSessionLogEntries` (table/entity/DbSet/configuration)
- `SyncLogEntriesAsync` (method)
- `AddLogEntry` (method + call sites)
- `RecordCaseUpdate` (method + callers)
- `RecordTravelUpdate` (method + callers)
- `_logEntries` (aggregate backing field)
- `LogEntries` (aggregate property — all readers)
- `JournalResolver` (all callers, classified per Step 2/3)
- `GameSessionDiaryDays` (table — classified per Step 2/6)
- Snapshot `LogEntries` (`GameSessionJsonSerializer.SessionSnapshot` — all callers)
- `CompleteCase` (method — removed or retained)

Classification values: `removed`, `retained with source-backed reason`, `harmless/temporary with source-backed reason`, `closure blocker`.

Final judgment: GREEN only if every surface is `removed`, `retained with source-backed reason`, or `harmless/temporary with source-backed reason`. If any surface is `closure blocker`, return AMBER or BLOCKED with exact follow-up evidence — do not claim GREEN.

---

## Self-Review

### Spec coverage

- ✅ Move purchase log production into `Apply(StoreItemPurchased)` — Task 4
- ✅ `Purchase()` no longer calls `AddLogEntry` after Apply — Task 4
- ✅ Command path and replay path produce same `session.LogEntries` for purchase — Task 4 (Apply is the single path), Task 2 (equivalence test)
- ✅ `JournalLogProjector` projects `StoreItemPurchased` to `GameLogEntryKind.Purchase` — Task 5
- ✅ Preserve exact message text and day/turn behavior — Task 4/5 (same format string)
- ✅ Preserve existing DTO shape — no DTO changes
- ✅ Integration test: purchase then `/journal` — Task 3
- ✅ Projector test: `StoreItemPurchased` yields purchase entry — Task 1
- ✅ Command/replay equivalence coverage — Task 2
- ✅ `AddLogEntryGuardrailTests` count drops — Task 6/11
- ✅ No new `AddLogEntry` call sites — net reduction
- ✅ Existing journal behavior for start, investigation, saloon/bounty, travel unchanged — no changes to those flows
- ✅ `GameApiJournalTests` proves purchase-then-journal through API — Task 3
- ✅ No DTO/frontend contract changes — none planned
- ✅ Validation passes — Task 12
- ✅ Repo-wide architecture closeout inspection — Task 13
- ✅ `GameSessionLogEntries` table removed — Tasks 7-10
- ✅ Dead `CompleteCase` stub removed — Task 11

### Placeholder scan

No placeholders found. All steps contain concrete code or commands.

### Type consistency

- `StoreItemPurchased` fields used consistently: `DisplayName`, `Quantity`, `TotalPrice` in both `Apply` and `JournalLogProjector`.
- `GameLogEntry` constructor: `(GameLogEntryKind, string, int, int)` — consistent across all usages.
- `JournalLogProjector.Project(IReadOnlyList<IDomainEvent>)` — consistent signature.
- `KnownLegacyAddLogEntryCallSiteCount`: 6 → 5 (Task 6) → 4 (Task 11). Consistent progression.

### Stop condition check

- Current main HAS the purchase `AddLogEntry` outside `Apply(StoreItemPurchased)` — verified at line 2311. ✅ Proceed.
- Purchase journal behavior IS supposed to show in `GameLogEntry` output — the issue says restore it. ✅ Proceed.
- Adding purchase entry to `Apply(StoreItemPurchased)` is byte-for-byte compatible — same message format, same day/turn (purchase doesn't advance clock). ✅ Proceed.
- Fixing purchase does NOT require changing DTO/frontend shape or unrelated gameplay systems. ✅ Proceed.
