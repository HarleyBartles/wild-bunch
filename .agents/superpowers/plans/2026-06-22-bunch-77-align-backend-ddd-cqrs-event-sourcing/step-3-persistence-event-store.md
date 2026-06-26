# Step 3 — Persistence Event Store (Envelope + Optimistic Concurrency + Snapshot Cache)

> Parent plan: `../2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing.md`
> Acceptance criteria covered: **AC-002** (event sourcing seam visible in code, persistence half), **BUNCH-3 alignment** (event store + snapshot-as-cache posture).

## Goal

Add persistence-layer event storage that wraps typed domain events in an infrastructure envelope, appends with **expected stream version** (optimistic concurrency), and loads via **snapshot + replay** (snapshot as cache, events as source of history). Full replay-from-events (no snapshot) is proven in tests for the migrated slice.

This is where the envelope lives — in `WildBunch.Persistence`, not Domain. Domain events are typed facts; the envelope is infrastructure that wraps them for storage, indexing, and concurrency.

## Onion dependency direction (corrected)

The existing repo pattern is: `IGameSessionRepository` (port) lives in `WildBunch.Application.Abstractions`; `EfGameSessionRepository` (implementation) lives in `WildBunch.Persistence`. The repository stages changes on the `DbContext` without calling `SaveChangesAsync`; `EfGameSessionUnitOfWork.CommitAsync` is the single point that calls `SaveChangesAsync` inside a transaction.

This step follows the same pattern exactly:

- **No new event-store interface in Persistence.** The event append is absorbed into the existing `IGameSessionRepository.StoreAsync` (already in Application.Abstractions). The event-stream read is a new method on `IGameSessionRepository` (also in Application.Abstractions). Persistence implements both.
- **No independent `SaveChangesAsync` in the event store code.** The event append stages `StoredEventEntity` rows on the same `DbContext` the repository already uses. The UoW's `CommitAsync` is the single save + transaction commit.
- **Single persistence path from the handler.** The handler calls `repository.StoreAsync(session)` (stages snapshot + events + concurrency check) then `uow.CommitAsync()` (saves everything in one transaction). The handler does not call a separate event-store append.

## Files

- Add: `src/WildBunch.Persistence/EventStore/StoredEventEntity.cs` — the envelope EF entity.
- Add: `src/WildBunch.Persistence/EventStore/StoredEventEntityConfiguration.cs` — EF configuration.
- Add: `src/WildBunch.Persistence/EventStore/DomainEventEnvelope.cs` — internal envelope record wrapping a typed `IDomainEvent` with infrastructure metadata. Used internally by the repository for deserialization; not exposed to Application.
- Add: `src/WildBunch.Persistence/EventStore/DomainEventPayloadResolver.cs` — maps event type names to `IDomainEvent` concrete types for deserialization.
- Add: `src/WildBunch.Persistence/EventStore/ConcurrencyException.cs` — thrown when expected version does not match. Lives in Persistence (it is a persistence-layer concern; the handler catches it).
- Modify: `src/WildBunch.Application/Abstractions/IGameSessionRepository.cs` — add `GetEventStreamAsync` method for reading typed events (replay/projection rebuild).
- Modify: `src/WildBunch.Persistence/WildBunchDbContext.cs` — add `DbSet<StoredEventEntity> StoredEvents`.
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` — `StoreAsync` stages snapshot upsert + event append + concurrency check on the same DbContext (no `SaveChangesAsync`). `GetByIdAsync` loads snapshot + replays post-snapshot events. New `GetEventStreamAsync` loads all events for a session and returns typed `IDomainEvent` list.
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs` — support event-replay construction for the migrated slice proof.
- Modify: `src/WildBunch.Persistence/DependencyInjection.cs` — no new interface to register (event append is part of the existing `IGameSessionRepository` implementation).
- Add: EF Core migration — `dotnet ef migrations add AddEventStore`.
- Add: `tests/WildBunch.Persistence.Tests/EventStore/EventStoreAppendAndLoadTests.cs`
- Add: `tests/WildBunch.Persistence.Tests/EventStore/OptimisticConcurrencyTests.cs`
- Add: `tests/WildBunch.Persistence.Tests/EventStore/ReplayFromEventsTests.cs`
- Modify: `tests/WildBunch.Application.Tests/TestDoubles/InMemoryGameSessionRepository.cs` — store and reload events in-memory; implement `GetEventStreamAsync`.

## Port: `IGameSessionRepository` extension (in Application.Abstractions)

```csharp
using WildBunch.Domain.Events;

namespace WildBunch.Application.Abstractions;

public interface IGameSessionRepository
{
    Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default);
    Task StoreAsync(GameSession session, Guid correlationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IDomainEvent>> GetEventStreamAsync(GameSessionId id, CancellationToken cancellationToken = default);
}
```

Changes to the existing interface:
- `StoreAsync` gains a `correlationId` parameter (used to stamp appended events). The handler passes the command's correlation id.
- `GetEventStreamAsync` is new — returns typed `IDomainEvent` list for a session. Used by the full-replay proof test and future projection rebuild. The envelope is internal to Persistence; Application only sees typed events.

**No `IGameSessionEventStore` interface.** The event append is part of `StoreAsync`; the event read is `GetEventStreamAsync`. Both are on the existing repository port in Application.Abstractions. Persistence implements them. This keeps the dependency direction inward and avoids a separate event-store port that handlers would depend on.

## Envelope shape (internal to Persistence)

```csharp
namespace WildBunch.Persistence.EventStore;

/// <summary>
/// Infrastructure envelope wrapping a typed domain event for storage.
/// Internal to Persistence — not exposed to Application or Domain. See ADR-0028.
/// </summary>
internal sealed record DomainEventEnvelope
{
    public required Guid EventId { get; init; }
    public required Guid StreamId { get; init; }
    public required long Sequence { get; init; }
    public required DateTime OccurredAtUtc { get; init; }
    public required string EventType { get; init; }
    public required int SchemaVersion { get; init; }
    public required Guid CorrelationId { get; init; }
    public required Guid? CausationId { get; init; }
    public required IDomainEvent Payload { get; init; }
}
```

The envelope is `internal` to Persistence. Application and Domain never see it. On load, the repository deserializes `PayloadJson` to the concrete `IDomainEvent` type via `DomainEventPayloadResolver`, constructs the envelope internally, extracts `Payload`, and returns typed `IDomainEvent` to Application.

## `StoredEventEntity` (EF entity)

```csharp
public sealed class StoredEventEntity
{
    public Guid EventId { get; set; }              // PK
    public Guid StreamId { get; set; }             // indexed
    public long Sequence { get; set; }             // per-stream monotonic
    public DateTime OccurredAtUtc { get; set; }
    public string EventType { get; set; }          // concrete type name
    public int SchemaVersion { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    public string PayloadJson { get; set; }        // serialized typed domain event
    public DateTime RecordedAtUtc { get; set; }    // when appended

    public GameSessionEntity Session { get; set; } = null!;
}
```

## EF configuration

- `ToTable("GameSessionEvents")`
- `HasKey(e => e.EventId)`
- Unique index on `(StreamId, Sequence)` — enforces per-stream ordering, prevents duplicate sequence, and is the DB-level concurrency backstop.
- Index on `StreamId` for load-by-session.
- `EventType` required, max length 128.
- `PayloadJson` required, `jsonb`.
- `RecordedAtUtc` required.

## Store path: single-stage snapshot + event append (no independent save)

`EfGameSessionRepository.StoreAsync` stages everything on the same `DbContext` without calling `SaveChangesAsync`. The UoW's `CommitAsync` is the single save + transaction commit.

```csharp
public async Task StoreAsync(GameSession session, Guid correlationId, CancellationToken ct = default)
{
    var now = DateTime.UtcNow;
    var sessionId = session.Id.Value;

    // 1. Existing snapshot upsert (unchanged from current code)
    var entity = await _dbContext.GameSessions.SingleOrDefaultAsync(e => e.Id == sessionId, ct);
    if (entity is null)
    {
        entity = new GameSessionEntity { Id = sessionId, CreatedAtUtc = now, SchemaVersion = SchemaVersion };
        _dbContext.GameSessions.Add(entity);
    }
    entity.UpdatedAtUtc = now;
    entity.Status = session.Status.ToString();
    entity.TravelDifficulty = (int)session.TravelDifficulty;
    entity.SnapshotVersion = session.Version; // NEW: record the version this snapshot covers
    // ...existing component upserts...

    // 2. Event append with expected version (stages on DbContext, does NOT save)
    var uncommitted = session.UncommittedEvents;
    if (uncommitted.Count > 0)
    {
        var expectedVersion = session.Version - uncommitted.Count;

        // Check current stream version (staged + committed events for this stream)
        var currentVersion = await _dbContext.StoredEvents
            .Where(e => e.StreamId == sessionId)
            .Select(e => (long?)e.Sequence)
            .MaxAsync(ct) ?? 0;

        if (currentVersion != expectedVersion)
        {
            throw new ConcurrencyException(sessionId, expectedVersion, currentVersion);
        }

        // Stage event rows (no SaveChangesAsync — UoW commits)
        var sequence = expectedVersion;
        foreach (var e in uncommitted)
        {
            sequence++;
            _dbContext.StoredEvents.Add(new StoredEventEntity
            {
                EventId = Guid.NewGuid(),
                StreamId = sessionId,
                Sequence = sequence,
                OccurredAtUtc = now,
                EventType = e.GetType().Name,
                SchemaVersion = 1,
                CorrelationId = correlationId,
                CausationId = null,
                PayloadJson = JsonSerializer.Serialize(e, e.GetType(), _jsonOptions),
                RecordedAtUtc = now
            });
        }
    }

    // 3. Existing log/diary sync (unchanged)
    // ...existing SyncLogEntriesAsync, SyncDiaryDaysAsync...

    // NO SaveChangesAsync here — the UoW commits.
}
```

Key points:
- **No `SaveChangesAsync` in `StoreAsync`.** This matches the existing pattern exactly — the current `StoreAsync` already stages without saving.
- **Concurrency check is a stage-time read** (max Sequence). If another command committed between load and stage, the check throws `ConcurrencyException` early. The unique index on `(StreamId, Sequence)` is the DB-level backstop if a race occurs between the stage-time check and the UoW commit.
- **`MarkEventsCommitted` is called by the handler after `CommitAsync` succeeds**, not inside `StoreAsync`.

## Load path: snapshot + replay

`EfGameSessionRepository.GetByIdAsync` (via `LoadStoreAsync`):

1. Load the snapshot envelope + components (existing behavior — all state).
2. Read `SnapshotVersion` from `GameSessionEntity`.
3. Load `StoredEventEntity` rows with `Sequence > SnapshotVersion` for the session.
4. Deserialize each to typed `IDomainEvent` via `DomainEventPayloadResolver`.
5. If there are post-snapshot events, replay them through `Apply` on the snapshot-constructed session (using the same `ApplyEvent` dispatch from Step 2's `GameSessionEventReplay`).
6. Set the session's `_version` to `SnapshotVersion + postSnapshotEventCount`.
7. Call `MarkEventsCommitted` (loaded events are committed history, not uncommitted).

If there are no post-snapshot events, the snapshot is current — no replay needed.

## `GetEventStreamAsync` (full event read)

```csharp
public async Task<IReadOnlyList<IDomainEvent>> GetEventStreamAsync(GameSessionId id, CancellationToken ct = default)
{
    var sessionId = id.Value;
    var entities = await _dbContext.StoredEvents
        .Where(e => e.StreamId == sessionId)
        .OrderBy(e => e.Sequence)
        .AsNoTracking()
        .ToListAsync(ct);

    return entities
        .Select(e => DomainEventPayloadResolver.Deserialize(e.EventType, e.PayloadJson, _jsonOptions))
        .ToList();
}
```

Returns typed `IDomainEvent` list ordered by sequence. The envelope is internal; Application only sees typed events. Used by the full-replay proof test and future projection rebuild.

## Full replay proof (tests only)

```csharp
// Test-only: full replay from events, no snapshot
var events = await repository.GetEventStreamAsync(sessionId, ct);
var replayed = GameSession.RehydrateFromEvents(id, world, caseFile, events);
// Assert replayed state matches the original session
```

This proves the event stream reconstructs migrated state without the snapshot. The production load path uses snapshot + replay for performance; full replay becomes production when all flows migrate (BUNCH-3 follow-up).

## `DomainEventPayloadResolver`

```csharp
internal static class DomainEventPayloadResolver
{
    private static readonly Dictionary<string, Type> _types = new()
    {
        [nameof(GameStarted)] = typeof(GameStarted),
        [nameof(StoreItemPurchased)] = typeof(StoreItemPurchased),
    };

    public static IDomainEvent Deserialize(string eventType, string payloadJson, JsonSerializerOptions options)
    {
        var type = _types.TryGetValue(eventType, out var t)
            ? t
            : throw new InvalidOperationException($"Unknown domain event type: {eventType}");
        return (IDomainEvent)JsonSerializer.Deserialize(payloadJson, type, options)!;
    }
}
```

Lives in Persistence (per AGENTS.md: codecs belong in `WildBunch.Persistence`). Follow-up issues register new event types here.

## Snapshot version tracking

`GameSessionEntity` gains a `SnapshotVersion` column (long, default 0). On `StoreAsync`, set to `session.Version`. On `LoadStoreAsync`, determines which events to replay (`Sequence > SnapshotVersion`).

For pre-migration sessions (no events), `SnapshotVersion` is 0 and there are no events to replay. Backward-compatible.

## Migration

- `dotnet ef migrations add AddEventStore --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
- Creates `GameSessionEvents` table + adds `SnapshotVersion` column to `GameSessions`.
- No data migration: existing sessions have no events; `SnapshotVersion` defaults to 0.
- Per AGENTS.md: dev database drop/recreate is allowed when schema changes.

## Tasks

- [ ] **Task 1: Add `StoredEventEntity` and EF configuration.** Match existing entity/configuration style.
- [ ] **Task 2: Add `DomainEventEnvelope` internal record** (infrastructure envelope, internal to Persistence).
- [ ] **Task 3: Add `DomainEventPayloadResolver`** mapping the two event types from Step 1.
- [ ] **Task 4: Add `ConcurrencyException`** in Persistence.
- [ ] **Task 5: Extend `IGameSessionRepository`** in Application.Abstractions: add `correlationId` parameter to `StoreAsync`; add `GetEventStreamAsync` method returning `IReadOnlyList<IDomainEvent>`.
- [ ] **Task 6: Add `DbSet<StoredEventEntity>` to `WildBunchDbContext`.**
- [ ] **Task 7: Add `SnapshotVersion` column to `GameSessionEntity`** and its configuration.
- [ ] **Task 8: Modify `EfGameSessionRepository.StoreAsync`** to stage snapshot upsert + event append + concurrency check on the same DbContext. **No `SaveChangesAsync`** — the UoW commits.
- [ ] **Task 9: Modify `EfGameSessionRepository.GetByIdAsync`/`LoadStoreAsync`** to load snapshot + replay post-snapshot events through `Apply`.
- [ ] **Task 10: Implement `GetEventStreamAsync`** on `EfGameSessionRepository` — load all events, deserialize to typed `IDomainEvent`, return.
- [ ] **Task 11: Update `GameSessionRehydrator`** to support event-replay construction for the migrated slice proof.
- [ ] **Task 12: Generate the EF migration.**
- [ ] **Task 13: Update `InMemoryGameSessionRepository`** to store/reload events and implement `GetEventStreamAsync`.
- [ ] **Task 14: Write `EventStoreAppendAndLoadTests`.** Assert: store session (stages events) → commit via UoW → load event stream → events match (type, payload, sequence); append-only (no update/delete).
- [ ] **Task 15: Write `OptimisticConcurrencyTests`.** Assert: store with correct expected version succeeds; store with stale expected version throws `ConcurrencyException`; concurrent stores (one wins, one fails). Use the repository + UoW path, not a separate event-store interface.
- [ ] **Task 16: Write `ReplayFromEventsTests`.** Assert: store session via command path → commit → `GetEventStreamAsync` → `RehydrateFromEvents` → state matches original (wallet, inventory, status, difficulty). This is the material Event Sourcing proof.

## Validation

- [ ] **V1: `.\scripts\postgres-dev.ps1 ensure`** — shared PostgreSQL service healthy.
- [ ] **V2: `dotnet build`** passes.
- [ ] **V3: `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`** lists the new migration cleanly.
- [ ] **V4: `.\scripts\postgres-dev.ps1 test -- dotnet test`** — full suite passes against PostgreSQL, including event store tests.
- [ ] **V5: Optimistic concurrency proof.** `OptimisticConcurrencyTests` proves stale version is rejected via the repository + UoW path.
- [ ] **V6: Replay proof.** `ReplayFromEventsTests` proves `RehydrateFromEvents` reconstructs state from `GetEventStreamAsync` without snapshot.
- [ ] **V7: No independent `SaveChangesAsync` in event-store code.** Grep `EfGameSessionRepository.cs` for `SaveChangesAsync` — the only save is in `EfGameSessionUnitOfWork.CommitAsync`.
- [ ] **V8: No event-store interface in Persistence.** Grep `WildBunch.Persistence` for `interface I*EventStore` — none exists. The port is `IGameSessionRepository` in Application.Abstractions.
- [ ] **V9: No domain/handler/API changes.** `git status` shows only `WildBunch.Persistence/**`, `WildBunch.Application/Abstractions/IGameSessionRepository.cs`, `InMemoryGameSessionRepository.cs`, migration files, and test files.

## Acceptance mapping

- **AC-002 (event sourcing seam visible in code):** fully satisfied when combined with Steps 1 and 2. The seam is visible in domain (typed events + `Apply` + replay), application (projectors in Step 4, repository port in Application.Abstractions), and persistence (envelope + optimistic concurrency + snapshot cache, all internal).
- **BUNCH-3 alignment:** the event store with optimistic concurrency and snapshot-as-cache is the BUNCH-3 foundation. Replay-from-events is proven for the migrated slice in tests.

## Non-goals for this step

- No production replay-from-events load path (snapshot + replay is the production path; full replay is test-proven and becomes production when all flows migrate).
- No backfill of events for pre-migration sessions.
- No projection persistence (Step 4 projectors are in-memory).
- No handler changes (Step 5).
- No API changes (Step 6).
- No normalization of live runtime state into many tables (per AGENTS.md). Only the append-only events table + `SnapshotVersion` column.
- No SignalR transport.
- No removal of the snapshot envelope.
- No separate `IGameSessionEventStore` interface — the event append/read is part of `IGameSessionRepository`.

## Self-Review

**Spec coverage:** Step 3 covers the persistence half of AC-002 and BUNCH-3 alignment. The envelope lives in Persistence (not Domain), append uses optimistic concurrency staged on the DbContext, load uses snapshot + replay, and full replay is proven in tests.

**Onion dependency direction:** The port (`IGameSessionRepository` with `StoreAsync` + `GetEventStreamAsync`) is in `WildBunch.Application.Abstractions`. The implementation (`EfGameSessionRepository`) is in `WildBunch.Persistence`. No event-store interface in Persistence. Dependency direction is inward. ✅

**Single append ownership:** `StoreAsync` stages both snapshot and events. The handler calls `StoreAsync` then `CommitAsync`. No separate append call. No double append. ✅

**UoW atomicity:** `StoreAsync` stages on the DbContext without `SaveChangesAsync`. `CommitAsync` is the single save + transaction commit. No independent save in event-store code. ✅

**Non-goals:** All nine non-goals preserved.
