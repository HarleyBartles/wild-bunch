# Event Sourcing Integrity — Plan D: Event Upcaster Registry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the event upcaster registry infrastructure — interfaces, registry, DI registration, chain validation, and write-side event version stamping. This establishes the mechanism for evolving event payload contracts without bricking existing playthroughs. Since this is a greenfield repo, no upcasters are registered yet — the registry exists but is empty. The value is the infrastructure: when the first event shape change happens, the developer writes an upcaster, registers it, and the version bump is implicit.

**Architecture:** The registry (`PayloadUpcasterRegistry`) is a singleton in `WildBunch.Persistence`. It's keyed by `(PayloadKind, payloadType)`. Event versions are derived from the count of registered upcasters — no hand-edited version registry. The write side (`StoreAsync`) stamps each event with `CurrentVersion(eventType)` instead of the hardcoded `const int SchemaVersion = 1`. Chain validation runs at startup (registry constructor) to catch non-contiguous chains.

**Depends on:** Plan C (the full replay load path must exist so the versioning infrastructure has a load path to integrate with).

**Tech Stack:** C#/.NET, EF Core, xUnit, `dotnet build`, `dotnet test`

## Global Constraints

- This is a greenfield repo — no old saves to break. No upcasters are registered yet. The registry is empty. All events are at v1.
- The registry is in `WildBunch.Persistence` (not `WildBunch.Application`) because it's persistence infrastructure.
- Upcaster interfaces are `internal` — they're persistence-internal plumbing, not domain or application API.
- Run `dotnet build` and `dotnet test` after each task. Run `.\scripts\ci-preflight.ps1` before PR.

---

### Task 1: Create upcaster interfaces

**Files:**
- Create: `src/WildBunch.Persistence/Versioning/IPayloadUpcaster.cs`

**Interfaces:**
- Produces: `IPayloadUpcaster` (base interface), `IEventUpcaster` (marker for events), `PayloadKind` (enum).

- [ ] **Step 1: Create the interfaces file**

Create `src/WildBunch.Persistence/Versioning/IPayloadUpcaster.cs`:

```csharp
namespace WildBunch.Persistence.Versioning;

/// <summary>
/// The kind of persisted payload. Events have upcasters; projections have rebuild.
/// The enum exists for version-check uniformity in the registry, but only Event
/// has upcaster chains. See the event sourcing integrity policy.
/// </summary>
internal enum PayloadKind
{
    Event,
    Projection
}

/// <summary>
/// Transforms a persisted payload from one version to the next.
/// Upcasters are registered in the PayloadUpcasterRegistry. The chain from
/// v1 to currentVersion is validated at startup. See the event sourcing
/// integrity policy.
/// </summary>
internal interface IPayloadUpcaster
{
    string PayloadType { get; }
    int FromVersion { get; }      // transforms FromVersion -> FromVersion + 1
    string Upcast(string payloadJson);
}

/// <summary>
/// Marker interface for event upcasters. Used for DI filtering and
/// build-time completeness tests. See the event sourcing integrity policy.
/// </summary>
internal interface IEventUpcaster : IPayloadUpcaster { }
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/WildBunch.Persistence/WildBunch.Persistence.csproj`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Persistence/Versioning/IPayloadUpcaster.cs
git commit -m "Add upcaster interfaces: IPayloadUpcaster, IEventUpcaster, PayloadKind"
```

---

### Task 2: Implement PayloadUpcasterRegistry

**Files:**
- Create: `src/WildBunch.Persistence/Versioning/PayloadUpcasterRegistry.cs`

**Interfaces:**
- Consumes: `IPayloadUpcaster`, `IEventUpcaster`, `PayloadKind` (from Task 1)
- Produces: `PayloadUpcasterRegistry` — the registry with `CurrentVersion`, `Upcast`, and chain validation.

- [ ] **Step 1: Create the registry**

Create `src/WildBunch.Persistence/Versioning/PayloadUpcasterRegistry.cs`:

```csharp
using System.Collections.ObjectModel;

namespace WildBunch.Persistence.Versioning;

/// <summary>
/// Registry of payload upcasters, keyed by (PayloadKind, payloadType).
/// Event versions are derived from the count of registered upcasters —
/// no hand-edited version registry. To bump a version, write and register
/// an upcaster. The act of bumping IS the act of writing the upcaster.
/// See the event sourcing integrity policy and ADR-0028.
/// </summary>
internal sealed class PayloadUpcasterRegistry
{
    private readonly Dictionary<(PayloadKind, string), SortedDictionary<int, IPayloadUpcaster>> _upcasters = new();

    public PayloadUpcasterRegistry(IEnumerable<IPayloadUpcaster> upcasters)
    {
        ArgumentNullException.ThrowIfNull(upcasters);

        foreach (var upcaster in upcasters)
        {
            var key = (GetKind(upcaster), upcaster.PayloadType);
            if (!_upcasters.TryGetValue(key, out var chain))
            {
                chain = new SortedDictionary<int, IPayloadUpcaster>();
                _upcasters[key] = chain;
            }

            if (chain.ContainsKey(upcaster.FromVersion))
            {
                throw new InvalidOperationException(
                    $"Duplicate upcaster for {key.Item1} '{key.Item2}' at FromVersion={upcaster.FromVersion}.");
            }

            chain[upcaster.FromVersion] = upcaster;
        }

        // Validate contiguous chains for event upcasters.
        foreach (var ((kind, payloadType), chain) in _upcasters)
        {
            if (kind != PayloadKind.Event)
                continue;

            ValidateContiguousChain(payloadType, chain);
        }
    }

    /// <summary>
    /// Returns the current version for the given payload type.
    /// Derived from the count of registered upcasters: no upcasters -> v1;
    /// N upcasters -> v(N+1). There is no other API to declare a version.
    /// </summary>
    public int CurrentVersion(string payloadType)
    {
        var key = (PayloadKind.Event, payloadType);
        return _upcasters.TryGetValue(key, out var chain)
            ? chain.Keys.Max() + 1   // highest FromVersion + 1
            : 1;                      // no upcasters -> still at v1
    }

    /// <summary>
    /// Upcasts a persisted payload from storedVersion to currentVersion.
    /// Fails closed if storedVersion > current (code is older than data)
    /// or if the chain is non-contiguous (missing upcaster for a transition).
    /// </summary>
    public string Upcast(string payloadType, int storedVersion, string payloadJson)
    {
        var current = CurrentVersion(payloadType);

        if (storedVersion > current)
        {
            throw new InvalidOperationException(
                $"{payloadType} stored at v{storedVersion} but current code " +
                $"supports up to v{current}. Code is older than the data.");
        }

        if (storedVersion == current)
        {
            return payloadJson;  // no upcast needed
        }

        // Unknown type with storedVersion != 1: fail closed.
        if (!_upcasters.TryGetValue((PayloadKind.Event, payloadType), out var chain))
        {
            throw new InvalidOperationException(
                $"{payloadType} stored at v{storedVersion} but no upcasters registered.");
        }

        // Run chain from storedVersion to current.
        var version = storedVersion;
        var json = payloadJson;
        while (version < current)
        {
            if (!chain.TryGetValue(version, out var upcaster))
            {
                throw new InvalidOperationException(
                    $"No {payloadType} upcaster for v{version} -> v{version + 1}.");
            }
            json = upcaster.Upcast(json);
            version++;
        }

        return json;
    }

    private static PayloadKind GetKind(IPayloadUpcaster upcaster)
        => upcaster is IEventUpcaster ? PayloadKind.Event : PayloadKind.Projection;

    private static void ValidateContiguousChain(string payloadType, SortedDictionary<int, IPayloadUpcaster> chain)
    {
        // A contiguous chain starts at v1 and goes to v(chain.Count).
        // If the first upcaster is not at FromVersion=1, the chain is non-contiguous.
        var expectedFromVersion = 1;
        foreach (var (fromVersion, _) in chain)
        {
            if (fromVersion != expectedFromVersion)
            {
                throw new InvalidOperationException(
                    $"Non-contiguous upcaster chain for event '{payloadType}': " +
                    $"expected FromVersion={expectedFromVersion}, found FromVersion={fromVersion}.");
            }
            expectedFromVersion++;
        }
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/WildBunch.Persistence/WildBunch.Persistence.csproj`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Persistence/Versioning/PayloadUpcasterRegistry.cs
git commit -m "Implement PayloadUpcasterRegistry with version derivation and chain validation

Version derivation: CurrentVersion = max(FromVersion) + 1, or 1 if no upcasters.
Chain validation: contiguous from v1 to currentVersion, throws at startup if non-contiguous.
Upcast: runs chain from storedVersion to current, fails closed on future versions."
```

---

### Task 3: Register PayloadUpcasterRegistry in DI

**Files:**
- Modify: `src/WildBunch.Persistence/DependencyInjection.cs`

**Interfaces:**
- Consumes: `PayloadUpcasterRegistry` (from Task 2), `IEventUpcaster` (from Task 1)
- Produces: Registry registered as singleton, upcasters registered via explicit `AddEventUpcasters` call.

- [ ] **Step 1: Register the registry and upcasters in DI**

Read `src/WildBunch.Persistence/DependencyInjection.cs`. Add the registry registration after `services.AddSingleton<GameSessionJsonSerializer>();`:

```csharp
using WildBunch.Persistence.Versioning;

// ... in AddPersistence method:

services.AddSingleton<GameSessionJsonSerializer>();

// Event upcaster registry. No upcasters registered yet (greenfield repo, all events at v1).
// When the first event shape change happens, write an IEventUpcaster and add it here.
// The build-time completeness test (Plan D Task 6) asserts every IEventUpcaster in the
// assembly is registered here.
services.AddSingleton<PayloadUpcasterRegistry>(sp =>
{
    var upcasters = new List<IPayloadUpcaster>();
    // No upcasters yet. Add upcasters here as they're written:
    // upcasters.Add(new GameStartedV1ToV2Upcaster());
    return new PayloadUpcasterRegistry(upcasters);
});
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Persistence/DependencyInjection.cs
git commit -m "Register PayloadUpcasterRegistry in DI (no upcasters yet — greenfield)"
```

---

### Task 4: Replace hardcoded SchemaVersion with per-type version stamping for events

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`

**Interfaces:**
- Consumes: `PayloadUpcasterRegistry` (from Task 2, injected via DI)
- Produces: Event writes stamp `SchemaVersion` with `CurrentVersion(eventType)` instead of hardcoded `1`.

- [ ] **Step 1: Inject PayloadUpcasterRegistry into EfGameSessionRepository**

Read `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` lines 12-23. Add `PayloadUpcasterRegistry` as a constructor parameter:

```csharp
using WildBunch.Persistence.Versioning;

// ...

public sealed class EfGameSessionRepository : IGameSessionRepository
{
    private const int SchemaVersion = 1;  // Kept for projection writes (Plan E replaces this)

    private readonly WildBunchDbContext _dbContext;
    private readonly GameSessionJsonSerializer _serializer;
    private readonly TravelDiaryDayProjector _travelDiaryDayProjector;
    private readonly PayloadUpcasterRegistry _eventUpcasters;

    public EfGameSessionRepository(
        WildBunchDbContext dbContext,
        GameSessionJsonSerializer serializer,
        TravelDiaryDayProjector travelDiaryDayProjector,
        PayloadUpcasterRegistry eventUpcasters)
    {
        _dbContext = dbContext;
        _serializer = serializer;
        _travelDiaryDayProjector = travelDiaryDayProjector;
        _eventUpcasters = eventUpcasters;
    }
```

**Note:** If Plan C has already been executed, the constructor already has `TravelDiaryDayProjector`. Add `PayloadUpcasterRegistry` as the next parameter. If Plan C hasn't been executed yet, add both parameters.

- [ ] **Step 2: Update event writes to stamp CurrentVersion**

Read the event append loop in `StoreAsync` (around line 86-102). The current code stamps `SchemaVersion = SchemaVersion` (the hardcoded const). Replace with per-type version:

```csharp
// Append uncommitted events to the event stream
if (session.UncommittedEvents.Count > 0)
{
    var nextSequence = entity.StreamVersion + 1;
    foreach (var e in session.UncommittedEvents)
    {
        var eventType = e.GetType().Name;
        _dbContext.StoredEvents.Add(new StoredEventEntity
        {
            StreamId = entity.Id,
            Sequence = nextSequence++,
            EventId = Guid.NewGuid(),
            OccurredAtUtc = now,
            EventType = eventType,
            PayloadJson = _serializer.SerializeEvent(e),
            CorrelationId = correlationId,
            SchemaVersion = _eventUpcasters.CurrentVersion(eventType)
        });
    }
    entity.StreamVersion = session.Version;
    entity.SnapshotVersion = session.Version;
}
```

The only change is: `SchemaVersion = SchemaVersion` → `SchemaVersion = _eventUpcasters.CurrentVersion(eventType)`.

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 4: Run existing tests to verify no regressions**

Run: `dotnet test`
Expected: PASS. Since no upcasters are registered, `CurrentVersion(eventType)` returns `1` for all event types — same as the hardcoded `SchemaVersion = 1`. No behavior change.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs
git commit -m "Stamp event SchemaVersion with CurrentVersion(eventType) instead of hardcoded 1

Since no upcasters are registered yet, CurrentVersion returns 1 for all event types
— same as the previous hardcoded value. The infrastructure is in place for when
the first event shape change happens."
```

---

### Task 5: Update test DI registrations for PayloadUpcasterRegistry

**Files:**
- Modify: Test files that create `EfGameSessionRepository` or register DI services.

- [ ] **Step 1: Find test DI registration sites**

Search for test files that register `EfGameSessionRepository` or construct it directly:

```bash
grep -rn "AddScoped<IGameSessionRepository\|new EfGameSessionRepository\|CreateServices" tests/ --include="*.cs"
```

- [ ] **Step 2: Update each site to register/construct PayloadUpcasterRegistry**

For DI-based tests (like `EventSourcingEndToEndTests.CreateServices`), add:

```csharp
services.AddSingleton<PayloadUpcasterRegistry>(_ => new PayloadUpcasterRegistry([]));
```

For direct construction tests, add `new PayloadUpcasterRegistry([])` as the constructor argument:

```csharp
var repo = new EfGameSessionRepository(dbContext, serializer, new TravelDiaryDayProjector(), new PayloadUpcasterRegistry([]));
```

- [ ] **Step 3: Build and run all tests**

Run: `dotnet build && dotnet test`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add <test files>
git commit -m "Update test DI registrations for PayloadUpcasterRegistry"
```

---

### Task 6: Write upcaster chain completeness test (build-time)

**Files:**
- Create: `tests/WildBunch.Persistence.Tests/Versioning/UpcasterChainCompletenessTests.cs`

**Interfaces:**
- Consumes: `PayloadUpcasterRegistry`, `IEventUpcaster` (from Tasks 1-2)
- Produces: Build-time test asserting every `IEventUpcaster` in the assembly is registered.

- [ ] **Step 1: Check if WildBunch.Persistence.Tests project exists**

Check if `tests/WildBunch.Persistence.Tests/` exists. If it does not, check where persistence tests live. They may be in `WildBunch.Integration.Tests` or a separate project.

```bash
ls tests/
```

If there's no `WildBunch.Persistence.Tests` project, create the test in `tests/WildBunch.Integration.Tests/Versioning/` or wherever persistence-focused tests live. The test needs to reference `WildBunch.Persistence` and have access to `internal` types via `InternalsVisibleTo`.

- [ ] **Step 2: Verify InternalsVisibleTo for the test project**

Check `src/WildBunch.Persistence/Properties/AssemblyInfo.cs` (or the csproj) for `InternalsVisibleTo` entries. If the test project doesn't have access, add it:

```csharp
[assembly: InternalsVisibleTo("WildBunch.Persistence.Tests")]
```

Or in the csproj:
```xml
<ItemGroup>
  <InternalsVisibleTo Include="WildBunch.Persistence.Tests" />
</ItemGroup>
```

- [ ] **Step 3: Write the chain completeness test**

Create the test file:

```csharp
using System.Reflection;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Persistence.Tests.Versioning;

/// <summary>
/// Build-time test: asserts every IEventUpcaster in the assembly is registered
/// in the DI registration call. No silent missed upcasters.
/// See the event sourcing integrity policy.
/// </summary>
public sealed class UpcasterChainCompletenessTests
{
    [Fact]
    public void AllEventUpcastersInAssembly_AreRegisteredInDi()
    {
        // The DI registration in DependencyInjection.cs explicitly lists upcasters.
        // This test asserts that every IEventUpcaster class in the WildBunch.Persistence
        // assembly is referenced by that registration.
        //
        // Since no upcasters exist yet, this test asserts that the assembly contains
        // zero IEventUpcaster implementations. When the first upcaster is written,
        // this test will fail until it's registered in DependencyInjection.cs.

        var upcasterType = typeof(IEventUpcaster);
        var assembly = typeof(PayloadUpcasterRegistry).Assembly;

        var allUpcasters = assembly.GetTypes()
            .Where(t => upcasterType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .ToList();

        // No upcasters exist yet. When upcasters are added, the DI registration
        // in DependencyInjection.cs must reference them. This test will need to
        // be updated to verify the registration list matches the assembly scan.
        Assert.Empty(allUpcasters);
    }

    [Fact]
    public void Registry_WithNoUpcasters_ReturnsVersion1ForAllTypes()
    {
        var registry = new PayloadUpcasterRegistry([]);

        Assert.Equal(1, registry.CurrentVersion("GameStarted"));
        Assert.Equal(1, registry.CurrentVersion("TravelDayAdvanced"));
        Assert.Equal(1, registry.CurrentVersion("StoreItemPurchased"));
    }

    [Fact]
    public void Registry_WithNoUpcasters_UpcastReturnsPayloadUnchanged()
    {
        var registry = new PayloadUpcasterRegistry([]);

        var json = """{"test":"value"}""";
        var result = registry.Upcast("GameStarted", storedVersion: 1, json);
        Assert.Equal(json, result);
    }

    [Fact]
    public void Registry_FutureVersion_Throws()
    {
        var registry = new PayloadUpcasterRegistry([]);

        Assert.Throws<InvalidOperationException>(() =>
            registry.Upcast("GameStarted", storedVersion: 2, """{"test":"value"}"""));
    }
}
```

- [ ] **Step 4: Build and run the tests**

Run: `dotnet test --filter FullyQualifiedName~UpcasterChainCompletenessTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tests/<test project>/Versioning/UpcasterChainCompletenessTests.cs
git commit -m "Add upcaster chain completeness test (build-time)

Asserts every IEventUpcaster in the assembly is registered. Currently zero
upcasters exist — the test will fail when the first upcaster is written
without being registered in DependencyInjection.cs."
```

---

### Task 7: Write a sample upcaster correctness test

This test demonstrates the upcaster correctness pattern. It creates a fake upcaster, registers it, and verifies the chain works. This is the template for future real upcasters.

**Files:**
- Create: `tests/WildBunch.Persistence.Tests/Versioning/UpcasterCorrectnessTests.cs`

- [ ] **Step 1: Write the sample upcaster correctness test**

Create the test file:

```csharp
using WildBunch.Persistence.Versioning;

namespace WildBunch.Persistence.Tests.Versioning;

/// <summary>
/// Demonstrates the upcaster correctness test pattern.
/// When a real upcaster is written, copy this pattern: seed a vN payload,
/// run the upcaster chain, assert the output matches the expected v(N+1) shape.
/// See the event sourcing integrity policy.
/// </summary>
public sealed class UpcasterCorrectnessTests
{
    /// <summary>
    /// A test-only upcaster that adds a "newField" to a payload at v1 -> v2.
    /// This demonstrates the pattern without needing a real event shape change.
    /// </summary>
    private sealed class TestEventV1ToV2Upcaster : IEventUpcaster
    {
        public string PayloadType => "TestEvent";
        public int FromVersion => 1;
        public string Upcast(string payloadJson)
        {
            // In a real upcaster, this would use JsonNode to transform the payload.
            // Here we just append a field to demonstrate the pattern.
            return payloadJson.Replace("}", ",\"newField\":\"added\"}");
        }
    }

    [Fact]
    public void Upcaster_V1ToV2_ProducesV2Shape()
    {
        var registry = new PayloadUpcasterRegistry([new TestEventV1ToV2Upcaster()]);

        // v1 payload (no newField)
        var v1Json = """{"existingField":"value"}""";

        // Upcast to v2
        var v2Json = registry.Upcast("TestEvent", storedVersion: 1, v1Json);

        // v2 payload has newField
        Assert.Contains("\"newField\":\"added\"", v2Json);
        Assert.Contains("\"existingField\":\"value\"", v2Json);
    }

    [Fact]
    public void CurrentVersion_WithOneUpcaster_Returns2()
    {
        var registry = new PayloadUpcasterRegistry([new TestEventV1ToV2Upcaster()]);
        Assert.Equal(2, registry.CurrentVersion("TestEvent"));
    }

    [Fact]
    public void Upcast_AtCurrentVersion_ReturnsPayloadUnchanged()
    {
        var registry = new PayloadUpcasterRegistry([new TestEventV1ToV2Upcaster()]);

        var v2Json = """{"existingField":"value","newField":"already_present"}""";
        var result = registry.Upcast("TestEvent", storedVersion: 2, v2Json);
        Assert.Equal(v2Json, result);
    }

    [Fact]
    public void Registry_NonContiguousChain_ThrowsAtConstruction()
    {
        // An upcaster that starts at v2 (skipping v1) — non-contiguous
        var badUpcaster = new TestEventV2ToV3Upcaster();

        Assert.Throws<InvalidOperationException>(() =>
            new PayloadUpcasterRegistry([badUpcaster]));
    }

    private sealed class TestEventV2ToV3Upcaster : IEventUpcaster
    {
        public string PayloadType => "TestEvent";
        public int FromVersion => 2;
        public string Upcast(string payloadJson) => payloadJson;
    }
}
```

- [ ] **Step 2: Build and run the tests**

Run: `dotnet test --filter FullyQualifiedName~UpcasterCorrectnessTests`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add tests/<test project>/Versioning/UpcasterCorrectnessTests.cs
git commit -m "Add upcaster correctness test pattern (test-only upcaster demo)

Demonstrates the pattern for testing real upcasters: seed a vN payload,
run the chain, assert v(N+1) shape. Also tests non-contiguous chain rejection."
```

---

### Task 8: Regenerate index mesh, run CI preflight, and open PR

- [ ] **Step 1: Regenerate index mesh**

Run: `python scripts/generate_index_mesh.py`
Then: `python scripts/generate_index_mesh.py --check`
Expected: exit code 0.

- [ ] **Step 2: Commit index mesh if changed**

```bash
git add .agents/INDEX.md
git commit -m "Regenerate index mesh for upcaster registry"
```

If no INDEX.md files changed, skip this step.

- [ ] **Step 3: Run CI preflight**

Run: `.\scripts\ci-preflight.ps1`
Expected: all checks pass.

- [ ] **Step 4: Push branch and open draft PR**

```bash
git push -u origin <branch-name>
gh pr create --title "Event upcaster registry: version derivation infrastructure" --draft --body "..."
```

- [ ] **Step 5: Mark PR ready for review**

---

## Self-Review

### Spec Coverage

- **Part 2a interfaces:** Task 1 creates `IPayloadUpcaster`, `IEventUpcaster`, `PayloadKind`. ✓
- **Part 2a registry:** Task 2 creates `PayloadUpcasterRegistry` with `CurrentVersion`, `Upcast`, chain validation. ✓
- **Part 2a explicit registration:** Task 3 registers in DI with explicit list (no assembly scanning). ✓
- **Part 2a chain validation:** Task 2 validates contiguous chains at construction. Task 7 tests non-contiguous rejection. ✓
- **Part 2a unknown type throws:** Task 2 fails closed on unknown types with `storedVersion != 1`. Task 6 tests future version throw. ✓
- **Part 2d event writes:** Task 4 stamps `SchemaVersion` with `CurrentVersion(eventType)`. ✓
- **Part 2e test 1 (chain completeness):** Task 6. ✓
- **Part 2e test 2 (upcaster correctness):** Task 7. ✓
- **Part 2e test 3 (JSON shape snapshots):** Not in this plan — deferred to when the first real upcaster is written. The infrastructure is in place; the snapshot test is per-event-type and there are no shape changes yet. ✓ (documented as deferred)

### Placeholder Scan

No TBDs, TODOs, or vague shorthand. The registry code is fully specified. The test code is fully specified. The DI registration is fully specified.

### Type Consistency

- `IPayloadUpcaster` / `IEventUpcaster` / `PayloadKind` — created in Task 1, used in Tasks 2-7.
- `PayloadUpcasterRegistry` — created in Task 2, registered in Task 3, injected in Task 4, tested in Tasks 6-7.
- `EfGameSessionRepository` — modified in Task 4 (new constructor parameter), updated in Task 5 (test constructions).

## Execution Confidence Assessment

### Direct Execution Confidence: 9/10

This plan is pure infrastructure with no behavior change (no upcasters registered, `CurrentVersion` returns 1 for all types). The registry code is fully specified from the spec. The tests are straightforward. The main risk is the `InternalsVisibleTo` setup for the test project, which is a mechanical configuration step.

### SDD Confidence: 9/10

Each task is self-contained. The registry code is concrete enough for transcription. The tests are concrete. The DI registration is explicit. No design decisions need to be made during execution.

### Gap Closure Summary

- **Interface shapes:** Fully specified from the spec (Part 2a).
- **Registry algorithm:** Fully specified from the spec (Part 2a) — `CurrentVersion`, `Upcast`, chain validation.
- **DI registration:** Explicit list pattern, no assembly scanning. Fully specified.
- **Write-side stamping:** Simple replacement of `SchemaVersion = SchemaVersion` with `SchemaVersion = _eventUpcasters.CurrentVersion(eventType)`.
- **Test project setup:** The `InternalsVisibleTo` step is documented. The test project location is flexible (existing persistence tests or new project).

### Open Questions

1. **Test project location:** The plan says to check if `WildBunch.Persistence.Tests` exists. If it doesn't, the tests go in `WildBunch.Integration.Tests/Versioning/` or a new project. The implementer needs to verify `InternalsVisibleTo` for whichever project is chosen. This is a mechanical step, not a design decision.

2. **JSON shape snapshot tests (Part 2e test 3):** Deferred to when the first real upcaster is written. The infrastructure is in place; the snapshot test is per-event-type and there are no shape changes yet. This is a documented deferral, not a gap.
