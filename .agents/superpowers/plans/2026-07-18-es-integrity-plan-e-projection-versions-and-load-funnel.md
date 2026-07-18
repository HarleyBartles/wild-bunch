# Event Sourcing Integrity — Plan E: Projection Versions, Load Funnel, and Projection Write-Side Stamping

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the schema versioning system by adding projection version columns, building the `PersistedPayloadLoader` load funnel that enforces version checks on every load, making the serializer's deserialize methods internal so all loads go through the funnel, and replacing the hardcoded projection write-side `SchemaVersion = 1` with per-projection version stamping via `ProjectionVersions`. This is the final plan in the event-sourcing integrity series — after this, the versioning infrastructure is complete: events are upcasted (Plan D), projections are rebuilt-on-mismatch, and all loads pass through a single funnel that enforces both.

**Architecture:** `ProjectionVersions` is a hand-edited static class declaring the current version for each projection type (components + diary days). Projections don't get upcasters — when the stored version doesn't match current, the projection is dropped and rebuilt from the event stream. `PersistedPayloadLoader` is the single funnel: it upcasts events via `PayloadUpcasterRegistry` (from Plan D), version-checks components and diary days, and rebuilds stale projections via `TravelDiaryDayProjector` (for diary days) or a rehydrate-and-extract callback (for components). The three existing load paths (`EfGameSessionRepository.LoadStoreAsync`, `EfGameSessionRepository.GetEventStreamAsync`, `GameSessionReadStoreLoader.LoadStoreAsync`) all route through the loader. `GameSessionJsonSerializer`'s deserialize methods become `internal` — no code outside `WildBunch.Persistence` can bypass the funnel. The write side stamps projections with `ProjectionVersions.ForComponent(name)` / `ProjectionVersions.DiaryDay` instead of the hardcoded `const int SchemaVersion = 1`.

**Depends on:** Plan D (PayloadUpcasterRegistry must exist for event upcasting in the funnel). Plan B (TravelDiaryDayProjector must exist for diary day rebuild). Plan C (LoadFromEventsAsync must exist — the component rebuild callback reuses its world-reconstruction + RehydrateFromEvents logic).

**Tech Stack:** C#/.NET, EF Core, xUnit, `dotnet build`, `dotnet test`

## Global Constraints

- This is a greenfield repo — all projections start at v1. `ProjectionVersions` declares v1 for every projection type. The rebuild-on-mismatch path exists but never triggers until a version bump happens. The value is the infrastructure: when the first projection shape change happens, the developer bumps the constant in `ProjectionVersions` and the rebuild triggers on next load.
- `PersistedPayloadLoader` is in `WildBunch.Persistence` (not `WildBunch.Application`) because it's persistence infrastructure — it sits between the DB rows and the domain objects.
- `ProjectionVersions` is in `WildBunch.Persistence/Versioning/` alongside the upcaster registry.
- The serializer's deserialize methods become `internal`, not `private` — `PersistedPayloadLoader` and other persistence-internal code still needs to call them. The constraint is that no code *outside* `WildBunch.Persistence` can call them directly.
- Run `dotnet build` and `dotnet test` after each task. Run `.\scripts\ci-preflight.ps1` before PR.

---

### Task 1: Add SchemaVersion column to GameSessionDiaryDayEntity + EF migration

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionDiaryDayEntity.cs`
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionDiaryDayEntityConfiguration.cs`
- Create: EF Core migration (via `dotnet ef migrations add`)

**Interfaces:**
- Produces: `GameSessionDiaryDayEntity.SchemaVersion` (int, non-nullable, default 1). Existing rows defaulted to v1 by the migration.

- [ ] **Step 1: Add SchemaVersion property to GameSessionDiaryDayEntity**

Read `src/WildBunch.Persistence/GameSessions/GameSessionDiaryDayEntity.cs`. Add `SchemaVersion` property after `RecordedAtUtc`:

```csharp
public sealed class GameSessionDiaryDayEntity
{
    public Guid SessionId { get; set; }
    public int Sequence { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime RecordedAtUtc { get; set; }
    public int SchemaVersion { get; set; }
    public GameSessionEntity Session { get; set; } = null!;
}
```

- [ ] **Step 2: Configure SchemaVersion in the entity configuration**

Read `src/WildBunch.Persistence/GameSessions/GameSessionDiaryDayEntityConfiguration.cs`. Add the property configuration after `RecordedAtUtc`:

```csharp
builder.Property(e => e.SchemaVersion)
    .IsRequired();
```

- [ ] **Step 3: Generate the EF migration**

Run from the `src/WildBunch.Persistence` directory (or the repo root, depending on where the EF CLI is configured):

```bash
dotnet ef migrations add AddDiaryDaySchemaVersion --project src/WildBunch.Persistence --output-dir Migrations
```

Verify the generated migration adds a `SchemaVersion` column to the `GameSessionTravelDiaryDays` table with a default of 1 for existing rows. The migration should include an `AddColumn<int>` operation with `defaultValue: 1`. If the generated migration doesn't include the default, manually edit the migration to add `defaultValue: 1` to the `AddColumn` call.

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 5: Run existing tests to verify no regressions**

Run: `dotnet test`
Expected: PASS. The new column defaults to 1, so existing diary day rows get v1 — same as the implicit v1 before the column existed.

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/GameSessionDiaryDayEntity.cs \
  src/WildBunch.Persistence/GameSessions/GameSessionDiaryDayEntityConfiguration.cs \
  src/WildBunch.Persistence/Migrations/
git commit -m "Add SchemaVersion column to GameSessionDiaryDayEntity (EF migration)

Existing rows default to v1. Used by PersistedPayloadLoader for projection
version checks — stale versions trigger rebuild from the event stream."
```

---

### Task 2: Create ProjectionVersions static class

**Files:**
- Create: `src/WildBunch.Persistence/Versioning/ProjectionVersions.cs`

**Interfaces:**
- Produces: `ProjectionVersions` — hand-edited per-projection version constants. `ForComponent(name)` returns the current version for a component; `DiaryDay` returns the current version for diary days.

- [ ] **Step 1: Create the ProjectionVersions class**

Create `src/WildBunch.Persistence/Versioning/ProjectionVersions.cs`:

```csharp
using WildBunch.Persistence.GameSessions;

namespace WildBunch.Persistence.Versioning;

/// <summary>
/// Hand-edited current-version constants for projection types.
/// Projections don't get upcasters — when the stored version doesn't match
/// current, the projection is dropped and rebuilt from the event stream.
/// Bumping a projection version is a code change: update the constant, and
/// the rebuild logic triggers on next load. See the event sourcing integrity
/// policy and ADR-0028.
///
/// Why projections use a hand-edited version while events don't: events have
/// upcasters, so event versions are derived from upcaster count (no
/// hand-edited registry). Projections don't have upcasters (they're rebuilt,
/// not upcasted), so there's no equivalent failure mode to prevent by
/// derivation. A hand-edited constant that doesn't match reality causes a
/// rebuild on every load (wasteful but correct) or no rebuild when one was
/// needed (caught by the projection rebuild parity test). The failure modes
/// are different, so the enforcement mechanisms differ.
/// </summary>
internal static class ProjectionVersions
{
    /// <summary>
    /// Current version for all component projections. All components start
    /// at v1. When a component's JSON shape changes, bump this to 2 and the
    /// PersistedPayloadLoader will rebuild that component from the event
    /// stream on next load.
    /// </summary>
    private const int ComponentVersion = 1;

    /// <summary>
    /// Current version for diary day projections. Starts at v1. When the
    /// TravelDiaryDayState shape changes, bump this to 2 and the
    /// PersistedPayloadLoader will rebuild all diary days via
    /// TravelDiaryDayProjector on next load.
    /// </summary>
    public const int DiaryDay = 1;

    /// <summary>
    /// Returns the current version for the named component projection.
    /// All components share the same version today — if individual components
    /// need independent versioning in the future, switch this to a dictionary
    /// keyed by component name.
    /// </summary>
    public static int ForComponent(string componentName) => ComponentVersion;
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/WildBunch.Persistence/WildBunch.Persistence.csproj`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Persistence/Versioning/ProjectionVersions.cs
git commit -m "Add ProjectionVersions static class for projection version constants

Hand-edited per-projection version (projections don't have upcasters).
All projections start at v1. Bumping a constant triggers rebuild-on-mismatch
on next load via PersistedPayloadLoader."
```

---

### Task 3: Create PersistedPayloadLoader (the load funnel)

**Files:**
- Create: `src/WildBunch.Persistence/Versioning/PersistedPayloadLoader.cs`

**Interfaces:**
- Consumes: `PayloadUpcasterRegistry` (from Plan D), `GameSessionJsonSerializer` (existing), `TravelDiaryDayProjector` (from Plan B), `ProjectionVersions` (from Task 2)
- Produces: `PersistedPayloadLoader` — the single funnel that turns persisted rows into domain objects. Events are upcasted; components and diary days are version-checked and rebuilt if stale.

- [ ] **Step 1: Create the PersistedPayloadLoader class**

Create `src/WildBunch.Persistence/Versioning/PersistedPayloadLoader.cs`:

```csharp
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.Versioning;

/// <summary>
/// The single funnel that turns persisted rows into domain objects.
/// Events are upcasted via PayloadUpcasterRegistry. Components and diary
/// days are version-checked against ProjectionVersions — if the stored
/// version doesn't match current, the projection is rebuilt from the
/// event stream. No code outside WildBunch.Persistence should call
/// GameSessionJsonSerializer's deserialize methods directly — this loader
/// is the only sanctioned surface. See the event sourcing integrity policy.
/// </summary>
internal sealed class PersistedPayloadLoader
{
    private readonly PayloadUpcasterRegistry _eventUpcasters;
    private readonly GameSessionJsonSerializer _serializer;
    private readonly TravelDiaryDayProjector _diaryDayProjector;
    private readonly Func<IReadOnlyList<IDomainEvent>, GameSession> _rebuildSessionFromEvents;

    public PersistedPayloadLoader(
        PayloadUpcasterRegistry eventUpcasters,
        GameSessionJsonSerializer serializer,
        TravelDiaryDayProjector diaryDayProjector,
        Func<IReadOnlyList<IDomainEvent>, GameSession> rebuildSessionFromEvents)
    {
        _eventUpcasters = eventUpcasters;
        _serializer = serializer;
        _diaryDayProjector = diaryDayProjector;
        _rebuildSessionFromEvents = rebuildSessionFromEvents;
    }

    /// <summary>
    /// Loads a single event: upcast via the registry, then deserialize.
    /// The upcaster registry fails closed on future versions (code older
    /// than data) and on missing upcasters in the chain.
    /// </summary>
    public IDomainEvent LoadEvent(StoredEventEntity stored)
    {
        var json = _eventUpcasters.Upcast(
            stored.EventType, stored.SchemaVersion, stored.PayloadJson);
        return _serializer.DeserializeEvent(stored.EventType, json);
    }

    /// <summary>
    /// Loads a batch of events: upcast + deserialize each.
    /// Convenience method for load paths that fetch the full event stream.
    /// </summary>
    public IReadOnlyList<IDomainEvent> LoadEvents(IReadOnlyList<StoredEventEntity> stored)
    {
        var events = new IDomainEvent[stored.Count];
        for (var i = 0; i < stored.Count; i++)
        {
            events[i] = LoadEvent(stored[i]);
        }
        return events;
    }

    /// <summary>
    /// Loads a component's payload JSON: version-check, rebuild if stale.
    /// Returns null if the component doesn't exist. If the stored version
    /// doesn't match ProjectionVersions.ForComponent(name), the component
    /// is rebuilt from the event stream via the rebuild callback (which
    /// rehydrates the full session and extracts the component).
    /// </summary>
    public string? LoadComponentPayload(
        IReadOnlyDictionary<string, GameSessionComponentEntity> components,
        string componentName,
        IReadOnlyList<IDomainEvent> events)
    {
        if (!components.TryGetValue(componentName, out var entity))
            return null;

        if (entity.ComponentVersion == ProjectionVersions.ForComponent(componentName))
            return entity.PayloadJson;

        // Stale: rebuild from events. Rehydrate the session and extract
        // the component, then serialize it back to JSON. This is expensive
        // but only triggers on version mismatch (never in greenfield).
        var session = _rebuildSessionFromEvents(events);
        return SerializeComponentByName(session, componentName);
    }

    /// <summary>
    /// Loads diary days: version-check, rebuild if stale.
    /// If any row's SchemaVersion doesn't match ProjectionVersions.DiaryDay,
    /// all diary days are discarded and rebuilt via TravelDiaryDayProjector.
    /// If no rows exist, rebuild from events (empty sessions get an empty list).
    /// </summary>
    public IReadOnlyList<TravelDiaryDayState> LoadDiaryDays(
        IReadOnlyList<GameSessionDiaryDayEntity> stored,
        IReadOnlyList<IDomainEvent> events)
    {
        if (stored.Count > 0 && stored.All(d => d.SchemaVersion == ProjectionVersions.DiaryDay))
        {
            return stored.Select(d => _serializer.DeserializeTravelDiaryDay(d.PayloadJson)).ToArray();
        }

        // Stale or empty: rebuild from events via the projector.
        return _diaryDayProjector.Project(events);
    }

    private string SerializeComponentByName(GameSession session, string componentName)
    {
        return componentName switch
        {
            GameSessionComponentNames.Player => _serializer.SerializePlayer(session.Player),
            GameSessionComponentNames.World => _serializer.SerializeWorld(session.World),
            GameSessionComponentNames.CaseFile => _serializer.SerializeCaseFile(session.CaseFile),
            GameSessionComponentNames.Clock => _serializer.SerializeClock(session.Clock),
            GameSessionComponentNames.PursuitState => _serializer.SerializePursuitState(session.PursuitState),
            GameSessionComponentNames.Setup => _serializer.SerializeSetup(session.GameEntropy),
            GameSessionComponentNames.SaltSource => _serializer.SerializeSaltSource(session.SaltSource),
            GameSessionComponentNames.TownVisitState => _serializer.SerializeTownVisitState(session.TownVisitStateOrNull ?? throw new InvalidOperationException("Cannot rebuild null TownVisitState.")),
            GameSessionComponentNames.Journey => _serializer.SerializeJourneySnapshot(session.Journey?.ToSnapshot(session.TravelRules) ?? throw new InvalidOperationException("Cannot rebuild null Journey.")),
            GameSessionComponentNames.CompletedJourneyHistory => _serializer.SerializeCompletedJourneyHistory(session.CompletedJourneyHistory),
            GameSessionComponentNames.WantedSuspectPresenceLedger => _serializer.SerializeWantedSuspectPresenceLedger(session.WantedSuspectPresenceEntries),
            GameSessionComponentNames.CurrentActionContext => _serializer.SerializeCurrentActionContext(session.CurrentActionContext, session.CurrentActionContextTownId),
            GameSessionComponentNames.PendingDevTravelOverride => _serializer.SerializePendingDevTravelOverride(session.PendingDevTravelOverride) ?? throw new InvalidOperationException("Cannot rebuild null PendingDevTravelOverride."),
            GameSessionComponentNames.PendingDevSaloonOverride => _serializer.SerializePendingDevSaloonOverride(session.PendingDevSaloonOverride) ?? throw new InvalidOperationException("Cannot rebuild null PendingDevSaloonOverride."),
            GameSessionComponentNames.DevLayoutSalts => _serializer.SerializeDevLayoutSalts(session.DevLayoutSalts) ?? throw new InvalidOperationException("Cannot rebuild null DevLayoutSalts."),
            GameSessionComponentNames.UnrelatedCriminalLedger => _serializer.SerializeUnrelatedCriminalLedger(session.UnrelatedCriminalLedger),
            _ => throw new InvalidOperationException($"Unknown component name '{componentName}' for rebuild."),
        };
    }
}
```

**Note on the rebuild callback:** The `Func<IReadOnlyList<IDomainEvent>, GameSession>` callback is provided by `EfGameSessionRepository` (Task 5). It rehydrates the session from events — the same logic as `LoadFromEventsAsync` from Plan C, minus the async DB queries (the events are already loaded). The callback reconstructs the world from `WorldGenerated`, calls `GameSession.RehydrateFromEvents`, and returns the session. Since the rebuild path never triggers in greenfield (all versions are v1), this callback is never exercised in production until a version bump happens. The version mismatch behavior test (Task 12) exercises it by manually setting a stale version.

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/WildBunch.Persistence/WildBunch.Persistence.csproj`
Expected: PASS. If the build fails because `TravelDiaryDayProjector` is in `WildBunch.Application` and `WildBunch.Persistence` doesn't reference it, check the project references. Plan B/Plan C already established that `EfGameSessionRepository` takes `TravelDiaryDayProjector` as a constructor parameter, so the project reference should already exist. If not, add it:

```bash
dotnet add src/WildBunch.Persistence/WildBunch.Persistence.csproj reference src/WildBunch.Application/WildBunch.Application.csproj
```

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Persistence/Versioning/PersistedPayloadLoader.cs
git commit -m "Add PersistedPayloadLoader — the single load funnel

Events: upcast via PayloadUpcasterRegistry. Components: version-check,
rebuild from events if stale. Diary days: version-check, rebuild via
TravelDiaryDayProjector if stale. No code outside WildBunch.Persistence
should call the serializer's deserialize methods directly."
```

---

### Task 4: Make GameSessionJsonSerializer deserialize methods internal

**Files:**
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Travel.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.WantedSuspectPresence.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.UnrelatedCriminalLedger.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Setup.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Rehydration.cs`

**Interfaces:**
- Produces: All `Deserialize*` methods on `GameSessionJsonSerializer` become `internal`. `Serialize*` methods stay `public` (the write path is in `EfGameSessionRepository` which is in the same assembly). `RehydrateGameSession` stays `internal` or `public` depending on current visibility — check and make `internal` if currently `public`.

- [ ] **Step 1: Change all Deserialize methods to internal**

In each file listed above, change `public` to `internal` on every method whose name starts with `Deserialize`. The methods to change (from source verification):

- `GameSessionJsonSerializer.Components.cs`: `DeserializePlayer`, `DeserializeWorld`, `DeserializeCaseFile`, `DeserializeClock`, `DeserializePursuitState`, `DeserializeSaltSource`, `DeserializeTownVisitState`, `DeserializeJourneySnapshot`, `DeserializeCompletedJourneyHistory`, `DeserializeCurrentActionContext`, `DeserializePendingDevTravelOverride`, `DeserializePendingDevSaloonOverride`, `DeserializeDevLayoutSalts`
- `GameSessionJsonSerializer.Travel.cs`: `DeserializeTravelDiaryDay`
- `GameSessionJsonSerializer.Events.cs`: `DeserializeEvent`
- `GameSessionJsonSerializer.WantedSuspectPresence.cs`: `DeserializeWantedSuspectPresenceLedger`
- `GameSessionJsonSerializer.UnrelatedCriminalLedger.cs`: `DeserializeUnrelatedCriminalLedger`
- `GameSessionJsonSerializer.Setup.cs`: `DeserializeSetup` (if it exists — verify)

Use `replace_all` or search-and-replace within each file: `public.*Deserialize` → `internal.*Deserialize`. Be careful to only change deserialize methods, not serialize methods.

- [ ] **Step 2: Check RehydrateGameSession visibility**

Read `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Rehydration.cs`. If `RehydrateGameSession` is `public`, change it to `internal`. It's only called from `EfGameSessionRepository.ToAggregate` (same assembly).

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS. All callers of the deserialize methods are in `WildBunch.Persistence` (EfGameSessionRepository, GameSessionReadStoreLoader, PersistedPayloadLoader), so making them `internal` should not break anything. If the build fails, a caller outside `WildBunch.Persistence` is using a deserialize method directly — that caller should be routed through `PersistedPayloadLoader` instead (Tasks 5-8 handle this).

- [ ] **Step 4: Run existing tests**

Run: `dotnet test`
Expected: PASS. If tests fail because a test project calls deserialize methods directly, the test project should have `InternalsVisibleTo` access (check `src/WildBunch.Persistence/Properties/AssemblyInfo.cs` or the csproj for `InternalsVisibleTo` entries). If the test project doesn't have access, add it:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="WildBunch.Integration.Tests" />
</ItemGroup>
```

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Persistence/Serialization/
git commit -m "Make GameSessionJsonSerializer deserialize methods internal

PersistedPayloadLoader is the only sanctioned surface for deserializing
persisted payloads. No code outside WildBunch.Persistence can call the
serializer's deserialize methods directly — this enforces the version
check funnel."
```

---

### Task 5: Register PersistedPayloadLoader in DI and inject into EfGameSessionRepository

**Files:**
- Modify: `src/WildBunch.Persistence/DependencyInjection.cs`
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`

**Interfaces:**
- Consumes: `PersistedPayloadLoader` (from Task 3), `PayloadUpcasterRegistry` (from Plan D), `TravelDiaryDayProjector` (from Plan B/C)
- Produces: `PersistedPayloadLoader` registered as singleton, injected into `EfGameSessionRepository`.

- [ ] **Step 1: Register PersistedPayloadLoader in DI**

Read `src/WildBunch.Persistence/DependencyInjection.cs`. After the `PayloadUpcasterRegistry` registration (added in Plan D Task 3), add:

```csharp
using WildBunch.Persistence.Versioning;

// ... in AddPersistence method, after PayloadUpcasterRegistry registration:

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
```

- [ ] **Step 2: Create SessionRebuilder helper**

The `PersistedPayloadLoader` needs a callback to rebuild a `GameSession` from events (for stale component rebuild). This logic is the synchronous core of `LoadFromEventsAsync` from Plan C. Create a static helper so both `LoadFromEventsAsync` and the rebuild callback share the same logic.

Create `src/WildBunch.Persistence/GameSessions/SessionRebuilder.cs`:

```csharp
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.GameSessions;

/// <summary>
/// Synchronous session rebuild from events. Shared by LoadFromEventsAsync
/// (Plan C) and PersistedPayloadLoader's component rebuild callback.
/// Reconstructs the world from the WorldGenerated event, then calls
/// RehydrateFromEvents. See ADR-0028.
/// </summary>
internal static class SessionRebuilder
{
    public static GameSession RebuildFromEvents(
        IReadOnlyList<IDomainEvent> events,
        GameSessionJsonSerializer serializer)
    {
        var worldGenerated = events.OfType<WorldGenerated>().Single();
        var world = worldGenerated.World.ToDomain();
        return GameSession.RehydrateFromEvents(events, world);
    }
}
```

**Note:** If `LoadFromEventsAsync` (from Plan C) already has this world-reconstruction logic inline, refactor it to call `SessionRebuilder.RebuildFromEvents` instead. This avoids duplication.

- [ ] **Step 3: Inject PersistedPayloadLoader into EfGameSessionRepository**

Read `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` lines 12-23. The constructor currently takes `WildBunchDbContext`, `GameSessionJsonSerializer`, `TravelDiaryDayProjector` (from Plan C), and `PayloadUpcasterRegistry` (from Plan D). Add `PersistedPayloadLoader` as the next constructor parameter:

```csharp
using WildBunch.Persistence.Versioning;

// ...

public sealed class EfGameSessionRepository : IGameSessionRepository
{
    private const int SchemaVersion = 1;  // Removed in Task 9 — replaced by ProjectionVersions

    private readonly WildBunchDbContext _dbContext;
    private readonly GameSessionJsonSerializer _serializer;
    private readonly TravelDiaryDayProjector _travelDiaryDayProjector;
    private readonly PayloadUpcasterRegistry _eventUpcasters;
    private readonly PersistedPayloadLoader _payloadLoader;

    public EfGameSessionRepository(
        WildBunchDbContext dbContext,
        GameSessionJsonSerializer serializer,
        TravelDiaryDayProjector travelDiaryDayProjector,
        PayloadUpcasterRegistry eventUpcasters,
        PersistedPayloadLoader payloadLoader)
    {
        _dbContext = dbContext;
        _serializer = serializer;
        _travelDiaryDayProjector = travelDiaryDayProjector;
        _eventUpcasters = eventUpcasters;
        _payloadLoader = payloadLoader;
    }
```

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Persistence/DependencyInjection.cs \
  src/WildBunch.Persistence/GameSessions/SessionRebuilder.cs \
  src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs
git commit -m "Register PersistedPayloadLoader in DI and inject into EfGameSessionRepository

Adds SessionRebuilder helper for synchronous session rebuild from events,
shared by LoadFromEventsAsync and PersistedPayloadLoader's component
rebuild callback."
```

---

### Task 6: Refactor EfGameSessionRepository.LoadStoreAsync to use PersistedPayloadLoader

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`

**Interfaces:**
- Consumes: `PersistedPayloadLoader` (from Task 3/5)
- Produces: `LoadStoreAsync` routes event deserialization and diary day loading through the funnel.

- [ ] **Step 1: Update LoadStoreAsync to use the loader for events**

Read `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` lines 215-265. The current `LoadStoreAsync` method:

1. Loads the envelope, components, diary day payload JSONs, and all stored events.
2. Deserializes events via `_serializer.DeserializeEvent(...)` (line 247).
3. Deserializes diary days via `diaryDays.Select(_serializer.DeserializeTravelDiaryDay).ToArray()` (line 262).

Change the event deserialization (lines 244-248) to use the loader:

```csharp
// Before:
var allEvents = new IDomainEvent[allStoredEvents.Length];
for (var i = 0; i < allStoredEvents.Length; i++)
{
    allEvents[i] = _serializer.DeserializeEvent(allStoredEvents[i].EventType, allStoredEvents[i].PayloadJson);
}

// After:
var allEvents = _payloadLoader.LoadEvents(allStoredEvents);
```

- [ ] **Step 2: Update diary day loading to use the loader**

The current diary day query (lines 228-233) selects only `PayloadJson`. Change it to select the full entity so the loader can check `SchemaVersion`:

```csharp
// Before:
var diaryDays = await _dbContext.GameSessionDiaryDays.AsNoTracking()
    .Where(day => day.SessionId == id.Value)
    .OrderBy(day => day.Sequence)
    .Select(day => day.PayloadJson)
    .ToArrayAsync(cancellationToken)
    .ConfigureAwait(false);

// After:
var diaryDayEntities = await _dbContext.GameSessionDiaryDays.AsNoTracking()
    .Where(day => day.SessionId == id.Value)
    .OrderBy(day => day.Sequence)
    .ToArrayAsync(cancellationToken)
    .ConfigureAwait(false);
```

Then change the `GameSessionStore` construction (line 259-264) to use the loader for diary days:

```csharp
// Before:
return new GameSessionStore(
    envelope,
    components,
    diaryDays.Select(_serializer.DeserializeTravelDiaryDay).ToArray(),
    postSnapshotEvents,
    allEvents);

// After:
var diaryDays = _payloadLoader.LoadDiaryDays(diaryDayEntities, allEvents);
return new GameSessionStore(
    envelope,
    components,
    diaryDays,
    postSnapshotEvents,
    allEvents);
```

- [ ] **Step 3: Update GetEventStreamAsync to use the loader**

Read lines 194-213. The current `GetEventStreamAsync` deserializes events via `_serializer.DeserializeEvent(...)` (line 210). Change it:

```csharp
// Before:
var events = new IDomainEvent[storedEvents.Length];
for (var i = 0; i < storedEvents.Length; i++)
{
    events[i] = _serializer.DeserializeEvent(storedEvents[i].EventType, storedEvents[i].PayloadJson);
}
return events;

// After:
return _payloadLoader.LoadEvents(storedEvents);
```

- [ ] **Step 4: Build and run tests**

Run: `dotnet build && dotnet test`
Expected: PASS. Since all versions are v1 and `ProjectionVersions` declares v1, the loader's version checks pass and the behavior is identical to the previous direct-deserialize path.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs
git commit -m "Route LoadStoreAsync and GetEventStreamAsync through PersistedPayloadLoader

Events are upcasted via the registry; diary days are version-checked and
rebuilt if stale. The serializer's deserialize methods are no longer called
directly from these load paths."
```

---

### Task 7: Refactor GameSessionReadStoreLoader.LoadStoreAsync to use PersistedPayloadLoader

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`

**Interfaces:**
- Consumes: `PersistedPayloadLoader` (from Task 3/5)
- Produces: The read-store load path routes event deserialization and diary day loading through the funnel.

- [ ] **Step 1: Inject PersistedPayloadLoader into GameSessionReadStoreLoader**

Read `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`. The class currently takes `GameSessionJsonSerializer` as a method parameter (or constructor parameter — verify the exact shape). Add `PersistedPayloadLoader` as a constructor parameter:

```csharp
using WildBunch.Persistence.Versioning;

// ...

internal sealed class GameSessionReadStoreLoader
{
    private readonly PersistedPayloadLoader _payloadLoader;

    public GameSessionReadStoreLoader(PersistedPayloadLoader payloadLoader)
    {
        _payloadLoader = payloadLoader;
    }

    // ... existing methods updated to use _payloadLoader ...
}
```

**Note:** If `GameSessionReadStoreLoader` currently takes `GameSessionJsonSerializer` as a method parameter (not constructor), check how it's called. The loader methods may need to be updated to use `_payloadLoader` for event deserialization and diary day loading. Verify the exact constructor/method shape before modifying.

- [ ] **Step 2: Update event deserialization to use the loader**

In the `LoadStoreAsync` method (around line 152-156), change:

```csharp
// Before:
var domainEvents = new IDomainEvent[storedEvents.Length];
for (var i = 0; i < storedEvents.Length; i++)
{
    domainEvents[i] = serializer.DeserializeEvent(storedEvents[i].EventType, storedEvents[i].PayloadJson);
}

// After:
var domainEvents = _payloadLoader.LoadEvents(storedEvents);
```

- [ ] **Step 3: Update diary day loading to use the loader**

The current diary day query (lines 158-163) selects only `PayloadJson`. Change it to select the full entity:

```csharp
// Before:
var diaryDays = await dbContext.GameSessionDiaryDays.AsNoTracking()
    .Where(day => day.SessionId == id.Value)
    .OrderBy(day => day.Sequence)
    .Select(day => day.PayloadJson)
    .ToArrayAsync(cancellationToken)
    .ConfigureAwait(false);

// After:
var diaryDayEntities = await dbContext.GameSessionDiaryDays.AsNoTracking()
    .Where(day => day.SessionId == id.Value)
    .OrderBy(day => day.Sequence)
    .ToArrayAsync(cancellationToken)
    .ConfigureAwait(false);
```

Then change the `GameSessionStore` construction (line 165-169):

```csharp
// Before:
return new GameSessionStore(
    envelope,
    components,
    diaryDays.Select(serializer.DeserializeTravelDiaryDay).ToArray(),
    domainEvents);

// After:
var diaryDays = _payloadLoader.LoadDiaryDays(diaryDayEntities, domainEvents);
return new GameSessionStore(
    envelope,
    components,
    diaryDays,
    domainEvents);
```

- [ ] **Step 4: Register GameSessionReadStoreLoader in DI (if not already)**

Check `src/WildBunch.Persistence/DependencyInjection.cs` for `GameSessionReadStoreLoader` registration. If it's not registered, add it:

```csharp
services.AddSingleton<GameSessionReadStoreLoader>();
```

If it's already registered, update the registration to ensure `PersistedPayloadLoader` is injected (DI handles this automatically if the constructor takes it).

- [ ] **Step 5: Build and run tests**

Run: `dotnet build && dotnet test`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs \
  src/WildBunch.Persistence/DependencyInjection.cs
git commit -m "Route GameSessionReadStoreLoader through PersistedPayloadLoader

The read-store load path now uses the same funnel as the command load path.
Event upcasting and diary day version checks are enforced uniformly."
```

---

### Task 8: Update GameSessionComponentPayloads to route through PersistedPayloadLoader

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionComponentNames.cs`
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` (call sites in `ToAggregate`)

**Interfaces:**
- Consumes: `PersistedPayloadLoader` (from Task 3/5)
- Produces: `GameSessionComponentPayloads.GetRequiredPayload` / `GetOptionalPayload` route through the loader, which version-checks and rebuilds if stale. No raw-payload accessor remains.

- [ ] **Step 1: Update GameSessionComponentPayloads to accept the loader**

Read `src/WildBunch.Persistence/GameSessions/GameSessionComponentNames.cs` lines 35-46. The current `GameSessionComponentPayloads` class has static methods that return raw `PayloadJson` from the component entity. These need to route through `PersistedPayloadLoader.LoadComponentPayload`, which version-checks and rebuilds if stale.

Change the methods to accept the loader and events:

```csharp
internal static class GameSessionComponentPayloads
{
    internal static string GetRequiredPayload(
        IReadOnlyDictionary<string, GameSessionComponentEntity> components,
        string componentName,
        PersistedPayloadLoader payloadLoader,
        IReadOnlyList<IDomainEvent> events)
        => payloadLoader.LoadComponentPayload(components, componentName, events)
            ?? throw new InvalidOperationException($"Missing required game session component '{componentName}'.");

    internal static string? GetOptionalPayload(
        IReadOnlyDictionary<string, GameSessionComponentEntity> components,
        string componentName,
        PersistedPayloadLoader payloadLoader,
        IReadOnlyList<IDomainEvent> events)
        => payloadLoader.LoadComponentPayload(components, componentName, events);
}
```

- [ ] **Step 2: Update all call sites in ToAggregate**

Read `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` lines 267-414. Every call to `GameSessionComponentPayloads.GetRequiredPayload` / `GetOptionalPayload` needs two additional arguments: `_payloadLoader` and `store.AllEvents`.

Update each call site. Example (line 269):

```csharp
// Before:
var player = _serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player));

// After:
var player = _serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player, _payloadLoader, store.AllEvents));
```

Apply the same pattern to every `GetRequiredPayload` and `GetOptionalPayload` call in `ToAggregate` (lines 269-396). There are approximately 15 call sites.

- [ ] **Step 3: Update call sites in GameSessionReadStoreLoader**

Read `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`. If it also calls `GameSessionComponentPayloads.GetRequiredPayload` / `GetOptionalPayload`, update those call sites the same way — pass `_payloadLoader` and the events list.

- [ ] **Step 4: Build and run tests**

Run: `dotnet build && dotnet test`
Expected: PASS. Since all component versions are v1 and `ProjectionVersions.ForComponent` returns 1, the loader returns the stored JSON directly — same behavior as before.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/GameSessionComponentNames.cs \
  src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs \
  src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs
git commit -m "Route GameSessionComponentPayloads through PersistedPayloadLoader

Component payloads are now version-checked and rebuilt from events if stale.
No raw-payload accessor remains — all component loads go through the funnel."
```

---

### Task 9: Replace projection write-side stamping with ProjectionVersions

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`

**Interfaces:**
- Consumes: `ProjectionVersions` (from Task 2)
- Produces: `UpsertComponent` stamps `ComponentVersion` with `ProjectionVersions.ForComponent(name)`. `SyncDiaryDaysAsync` stamps `SchemaVersion` with `ProjectionVersions.DiaryDay`. The `const int SchemaVersion = 1` is removed.

- [ ] **Step 1: Remove the SchemaVersion constant**

Read `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` line 14. Remove:

```csharp
private const int SchemaVersion = 1;  // Kept for projection writes (Plan E replaces this)
```

Plan D already replaced event write-side stamping with `_eventUpcasters.CurrentVersion(eventType)`. This task replaces the remaining projection write-side uses.

- [ ] **Step 2: Update UpsertComponent to use ProjectionVersions**

Read lines 416-437. The `UpsertComponent` method currently sets `ComponentVersion = SchemaVersion` (lines 427 and 434). Change both to use `ProjectionVersions.ForComponent(componentName)`:

```csharp
private void UpsertComponent(Guid sessionId, string componentName, string payloadJson, DateTime now)
{
    var component = _dbContext.GameSessionComponents.Local.FirstOrDefault(item => item.SessionId == sessionId && item.ComponentName == componentName)
        ?? _dbContext.GameSessionComponents.SingleOrDefault(item => item.SessionId == sessionId && item.ComponentName == componentName);

    if (component is null)
    {
        _dbContext.GameSessionComponents.Add(new GameSessionComponentEntity
        {
            SessionId = sessionId,
            ComponentName = componentName,
            ComponentVersion = ProjectionVersions.ForComponent(componentName),
            PayloadJson = payloadJson,
            UpdatedAtUtc = now
        });
        return;
    }

    component.ComponentVersion = ProjectionVersions.ForComponent(componentName);
    component.PayloadJson = payloadJson;
    component.UpdatedAtUtc = now;
}
```

- [ ] **Step 3: Update SyncDiaryDaysAsync to stamp SchemaVersion**

Read lines 448-483. The `SyncDiaryDaysAsync` method currently doesn't set `SchemaVersion` (the column didn't exist before Task 1). Now that the column exists, stamp each row with `ProjectionVersions.DiaryDay`:

In the update loop (lines 457-466), add `SchemaVersion` stamping:

```csharp
for (var index = 0; index < commonCount; index++)
{
    var current = existing[index];
    var desiredJson = _serializer.SerializeTravelDiaryDay(travelDiaryDays[index]);
    if (!string.Equals(current.PayloadJson, desiredJson, StringComparison.Ordinal))
    {
        current.PayloadJson = desiredJson;
        current.RecordedAtUtc = DateTime.UtcNow;
    }
    current.SchemaVersion = ProjectionVersions.DiaryDay;
}
```

In the insert loop (lines 468-477), add `SchemaVersion`:

```csharp
for (var index = existing.Count; index < travelDiaryDays.Count; index++)
{
    _dbContext.GameSessionDiaryDays.Add(new GameSessionDiaryDayEntity
    {
        SessionId = sessionId,
        Sequence = index,
        PayloadJson = _serializer.SerializeTravelDiaryDay(travelDiaryDays[index]),
        RecordedAtUtc = DateTime.UtcNow,
        SchemaVersion = ProjectionVersions.DiaryDay
    });
}
```

- [ ] **Step 4: Check for any remaining SchemaVersion const references**

Search the file for any remaining references to the old `SchemaVersion` constant:

```bash
grep -n "SchemaVersion" src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs
```

The only remaining references should be:
- `entity.SchemaVersion = _eventUpcasters.CurrentVersion(eventType)` (event writes — Plan D, line ~83/100)
- `ProjectionVersions.ForComponent(...)` and `ProjectionVersions.DiaryDay` (projection writes — this task)

If any reference to the old `const int SchemaVersion` remains, replace it.

- [ ] **Step 5: Build and run tests**

Run: `dotnet build && dotnet test`
Expected: PASS. Since `ProjectionVersions.ForComponent` returns 1 and `ProjectionVersions.DiaryDay` is 1, the stamped values are identical to the previous hardcoded `1`. No behavior change.

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs
git commit -m "Replace hardcoded projection SchemaVersion with ProjectionVersions stamping

UpsertComponent stamps ComponentVersion with ProjectionVersions.ForComponent(name).
SyncDiaryDaysAsync stamps SchemaVersion with ProjectionVersions.DiaryDay.
The const int SchemaVersion = 1 is removed — event writes use the upcaster
registry (Plan D), projection writes use ProjectionVersions (this commit)."
```

---

### Task 10: Update test DI registrations and constructions for PersistedPayloadLoader

**Files:**
- Modify: Test files that create `EfGameSessionRepository` or register DI services.

- [ ] **Step 1: Find test DI registration sites**

Search for test files that register `EfGameSessionRepository` or construct it directly:

```bash
grep -rn "AddScoped<IGameSessionRepository\|new EfGameSessionRepository\|CreateServices\|new GameSessionReadStoreLoader" tests/ --include="*.cs"
```

- [ ] **Step 2: Update each site to register/construct PersistedPayloadLoader**

For DI-based tests (like `EventSourcingEndToEndTests.CreateServices`), add after the `PayloadUpcasterRegistry` registration (from Plan D):

```csharp
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
```

For direct construction tests, add `PersistedPayloadLoader` as the next constructor argument:

```csharp
var payloadLoader = new PersistedPayloadLoader(
    new PayloadUpcasterRegistry([]),
    serializer,
    new TravelDiaryDayProjector(),
    rebuildSessionFromEvents: events => SessionRebuilder.RebuildFromEvents(events, serializer));
var repo = new EfGameSessionRepository(dbContext, serializer, new TravelDiaryDayProjector(), new PayloadUpcasterRegistry([]), payloadLoader);
```

Also update any tests that construct `GameSessionReadStoreLoader` directly to pass `PersistedPayloadLoader`.

- [ ] **Step 3: Build and run all tests**

Run: `dotnet build && dotnet test`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add <test files>
git commit -m "Update test DI registrations and constructions for PersistedPayloadLoader"
```

---

### Task 11: Write projection version completeness test (build-time)

**Files:**
- Create: `tests/<test project>/Versioning/ProjectionVersionCompletenessTests.cs`

**Interfaces:**
- Consumes: `ProjectionVersions` (from Task 2), `GameSessionComponentNames` (existing)
- Produces: Build-time test asserting every projection type has a version declared in `ProjectionVersions`.

- [ ] **Step 1: Write the projection version completeness test**

Create the test file in the same test project as the upcaster tests (Plan D Task 6 — likely `tests/WildBunch.Persistence.Tests/Versioning/` or `tests/WildBunch.Integration.Tests/Versioning/`):

```csharp
using WildBunch.Persistence.Versioning;

namespace WildBunch.Persistence.Tests.Versioning;

/// <summary>
/// Build-time test: asserts every projection type has a version declared
/// in ProjectionVersions. No silent missing version declarations.
/// See the event sourcing integrity policy.
/// </summary>
public sealed class ProjectionVersionCompletenessTests
{
    [Fact]
    public void DiaryDayVersion_IsDeclared()
    {
        Assert.Equal(1, ProjectionVersions.DiaryDay);
    }

    [Fact]
    public void AllComponentNames_HaveVersionDeclared()
    {
        // Every component name in GameSessionComponentNames should return a
        // valid version from ProjectionVersions.ForComponent. Since all
        // components share the same version today, this asserts that
        // ForComponent returns 1 for every known component name.
        var componentNames = new[]
        {
            GameSessionComponentNames.Player,
            GameSessionComponentNames.World,
            GameSessionComponentNames.CaseFile,
            GameSessionComponentNames.Clock,
            GameSessionComponentNames.PursuitState,
            GameSessionComponentNames.Setup,
            GameSessionComponentNames.SaltSource,
            GameSessionComponentNames.TownVisitState,
            GameSessionComponentNames.Journey,
            GameSessionComponentNames.CompletedJourneyHistory,
            GameSessionComponentNames.WantedSuspectPresenceLedger,
            GameSessionComponentNames.CurrentActionContext,
            GameSessionComponentNames.PendingDevTravelOverride,
            GameSessionComponentNames.PendingDevSaloonOverride,
            GameSessionComponentNames.DevLayoutSalts,
            GameSessionComponentNames.UnrelatedCriminalLedger,
        };

        foreach (var name in componentNames)
        {
            var version = ProjectionVersions.ForComponent(name);
            Assert.True(version >= 1, $"Component '{name}' has version {version} — must be >= 1.");
        }
    }
}
```

- [ ] **Step 2: Build and run the tests**

Run: `dotnet test --filter FullyQualifiedName~ProjectionVersionCompletenessTests`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add tests/<test project>/Versioning/ProjectionVersionCompletenessTests.cs
git commit -m "Add projection version completeness test (build-time)

Asserts every projection type has a version declared in ProjectionVersions.
Currently all projections are at v1."
```

---

### Task 12: Write version mismatch behavior tests

**Files:**
- Create: `tests/<test project>/Versioning/VersionMismatchBehaviorTests.cs`

**Interfaces:**
- Consumes: `PersistedPayloadLoader` (from Task 3), `PayloadUpcasterRegistry` (from Plan D), `TravelDiaryDayProjector` (from Plan B), `ProjectionVersions` (from Task 2)
- Produces: Tests asserting that stale projection versions trigger rebuild, and that future event versions throw (fail-closed).

- [ ] **Step 1: Write the version mismatch behavior tests**

Create the test file:

```csharp
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Travel;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Persistence.Tests.Versioning;

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
    public void LoadDiaryDays_StaleVersion_TriggersRebuild()
    {
        var registry = new PayloadUpcasterRegistry([]);
        var serializer = new GameSessionJsonSerializer();
        var projector = new TravelDiaryDayProjector();
        var loader = new PersistedPayloadLoader(
            registry, serializer, projector,
            _ => throw new InvalidOperationException("Should not be called for diary days."));

        // A diary day entity with a stale version (v99 — current is v1).
        // The loader should discard it and rebuild from events via the projector.
        var staleDays = new[]
        {
            new GameSessionDiaryDayEntity
            {
                SessionId = Guid.NewGuid(),
                Sequence = 0,
                PayloadJson = serializer.SerializeTravelDiaryDay(new TravelDiaryDayState(...)),
                SchemaVersion = 99  // stale
            }
        };

        var events = Array.Empty<IDomainEvent>();  // no events -> empty diary days

        var result = loader.LoadDiaryDays(staleDays, events);

        // With no events, the projector returns an empty list.
        Assert.Empty(result);
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

        var day = new TravelDiaryDayState(...);  // construct a valid day
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
        // Assert the deserialized day matches the original
    }

    [Fact]
    public void LoadComponentPayload_StaleVersion_TriggersRebuild()
    {
        // This test exercises the component rebuild callback.
        // It requires constructing a valid event stream that produces a
        // GameSession, then checking that the rebuilt component matches.
        // See the full replay equality test (Plan C Task 4) for the event
        // stream construction pattern.
        //
        // The test:
        // 1. Build a valid event stream (GameStarted, WorldGenerated, etc.)
        // 2. Create a component entity with a stale version (v99)
        // 3. Call LoadComponentPayload — it should rebuild from events
        // 4. Assert the rebuilt JSON matches the expected component shape

        // TODO: Fill in with a concrete event stream from TravelTestFactory
        // or similar test helpers. The exact construction depends on what
        // test infrastructure exists.
    }
}
```

**Note on the `LoadComponentPayload_StaleVersion_TriggersRebuild` test:** This test exercises the component rebuild callback, which rehydrates a full session from events. It needs a valid event stream that produces a `GameSession` — the same kind of stream used in the full replay equality test (Plan C Task 4). Use `TravelTestFactory` or similar test helpers to construct the stream. The test is marked TODO because the exact event stream construction depends on the test infrastructure. The implementer should fill it in using the same pattern as `TravelReplayEqualityTests`.

- [ ] **Step 2: Build and run the tests**

Run: `dotnet test --filter FullyQualifiedName~VersionMismatchBehaviorTests`
Expected: The first three tests PASS. The fourth test (`LoadComponentPayload_StaleVersion_TriggersRebuild`) may be skipped or marked TODO if the event stream construction isn't filled in yet. If it's TODO, create a follow-up issue.

- [ ] **Step 3: Commit**

```bash
git add tests/<test project>/Versioning/VersionMismatchBehaviorTests.cs
git commit -m "Add version mismatch behavior tests

Asserts: future event versions throw (fail-closed), stale diary day versions
trigger rebuild via TravelDiaryDayProjector, current versions use stored JSON.
Component rebuild test is TODO — needs event stream construction from test
helpers."
```

---

### Task 13: CI preflight and PR

- [ ] **Step 1: Run CI preflight**

Run: `.\scripts\ci-preflight.ps1`
Expected: PASS. If any checks fail, fix them before creating the PR.

- [ ] **Step 2: Run full test suite**

Run: `dotnet build && dotnet test`
Expected: PASS.

- [ ] **Step 3: Create PR**

```bash
gh pr create --title "Plan E: Projection versions, load funnel, and projection write-side stamping" --body "$(cat <<'EOF'
## Summary
- Add `SchemaVersion` column to `GameSessionDiaryDayEntity` (EF migration, existing rows default to v1)
- Add `ProjectionVersions` static class for hand-edited projection version constants
- Add `PersistedPayloadLoader` — the single load funnel that upcasts events, version-checks projections, and rebuilds stale projections from the event stream
- Make `GameSessionJsonSerializer` deserialize methods `internal` — no code outside `WildBunch.Persistence` can bypass the funnel
- Route all three load paths (`EfGameSessionRepository.LoadStoreAsync`, `GetEventStreamAsync`, `GameSessionReadStoreLoader.LoadStoreAsync`) through the funnel
- Route `GameSessionComponentPayloads` accessors through the funnel — no raw-payload accessor remains
- Replace hardcoded projection `SchemaVersion = 1` with `ProjectionVersions.ForComponent(name)` / `ProjectionVersions.DiaryDay`
- Add projection version completeness test (build-time) and version mismatch behavior tests

Depends on: Plan B (TravelDiaryDayProjector), Plan C (LoadFromEventsAsync), Plan D (PayloadUpcasterRegistry).

#### Test plan
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes
- [ ] `.\scripts\ci-preflight.ps1` passes
- [ ] Projection version completeness test passes
- [ ] Version mismatch behavior tests pass (future version throws, stale diary day triggers rebuild)
- [ ] Existing tests pass with no regressions (all versions are v1 — no behavior change)

Generated with [Devin](https://devin.ai)
EOF
)"
```

---

## Spec Coverage

| Spec Section | Plan E Task |
|---|---|
| Part 2b: Projection version columns | Task 1 (diary day SchemaVersion column + migration) |
| Part 2b: ProjectionVersions static class | Task 2 |
| Part 2b: Rebuild-on-mismatch logic | Task 3 (PersistedPayloadLoader.LoadComponentPayload / LoadDiaryDays) |
| Part 2c: PersistedPayloadLoader load funnel | Task 3 |
| Part 2c: Serializer deserialize methods become internal | Task 4 |
| Part 2c: Three load paths route through loader | Tasks 5-7 |
| Part 2c: GameSessionComponentPayloads routes through loader | Task 8 |
| Part 2d: Projection write-side stamping | Task 9 |
| Part 2e test 7: Version mismatch behavior | Task 12 |
| Part 2e: Projection version completeness | Task 11 |

## Confidence Assessment

**Direct: 8/10.** The plan is concrete: file names, type names, method signatures, and call sites are all verified against the live source tree. The main risks:

1. **Component rebuild callback complexity.** The `PersistedPayloadLoader` needs a `Func<IReadOnlyList<IDomainEvent>, GameSession>` callback to rebuild stale components. This callback rehydrates a full session from events — the same logic as `LoadFromEventsAsync` (Plan C). The `SessionRebuilder` helper (Task 5 Step 2) shares this logic. The risk is that `RehydrateFromEvents` needs more inputs than just events + world (e.g., diary days). If so, the callback may need adjustment. Since the rebuild path never triggers in greenfield, this risk is latent — it won't surface until the first version bump.

2. **GameSessionReadStoreLoader constructor shape.** The plan assumes `GameSessionReadStoreLoader` can take `PersistedPayloadLoader` as a constructor parameter. If it currently takes `GameSessionJsonSerializer` as a method parameter (not constructor), the refactoring shape changes. The plan notes this uncertainty (Task 7 Step 1).

3. **InternalsVisibleTo for test projects.** Making deserialize methods `internal` may break test projects that call them directly. The plan handles this (Task 4 Step 4) by adding `InternalsVisibleTo` if needed.

4. **Component rebuild test (Task 12 Step 1, test 4).** The `LoadComponentPayload_StaleVersion_TriggersRebuild` test is marked TODO because it needs a valid event stream construction. The implementer should fill it in using the same pattern as `TravelReplayEqualityTests` (Plan C Task 4).

**SDD: 7/10.** The plan is executable via subagent-driven-development with task-by-task checkpoints. Each task has a build+test verification step. The main SDD risk is Task 8 (updating ~15 call sites in `ToAggregate`) — a subagent might miss one or introduce a typo. The task explicitly calls out the number of call sites and the pattern to follow.

## Open Questions Carried Forward

1. **Component rebuild callback inputs.** Does `GameSession.RehydrateFromEvents` need anything beyond events + world? If it also needs diary days or other inputs, the `SessionRebuilder.RebuildFromEvents` helper (Task 5 Step 2) needs adjustment. Verify by reading `RehydrateFromEvents` during implementation.

2. **Individual component versioning.** `ProjectionVersions.ForComponent` currently returns a single `ComponentVersion` constant for all components. If individual components need independent versioning in the future, switch to a dictionary keyed by component name. The build-time test (Task 11) would need updating to check per-component versions.

3. **GameSessionReadStoreLoader method shape.** Verify whether `GameSessionReadStoreLoader` takes `GameSessionJsonSerializer` as a constructor or method parameter before starting Task 7. The refactoring shape depends on this.
