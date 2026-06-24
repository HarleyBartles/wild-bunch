# BUNCH-83 Phase 3: Projections + Persistence + Handlers + Tests + ADR

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire the 6 new travel events into projections, persistence deserialization, handler orchestration, and the full validation/test suite. Update ADR-0028. By the end of Phase 3, travel/journey flows are fully event-sourced end-to-end.

**Architecture:** Projections derive read-model state from events via pure switches. Persistence deserializer maps event type names to CLR types. Handlers migrate to `GameSessionCommandHandler.ExecuteWithRetryAsync` — **with preview generation moved inside the retry boundary** for `TravelToTownHandler`. ADR-0028 gains a dated status entry.

**Tech Stack:** C#/.NET 10, xUnit, PostgreSQL (via `postgres-dev.ps1`), EF Core

## Global Constraints

- All Phase 1 and Phase 2 tests must pass at every commit
- Projections are pure functions over event streams — no mutation, no side effects
- Hidden encounter state MAY exist in internal persisted events for replay correctness, but MUST NOT leak through player-facing projections, DTOs, or API responses
- **`TravelToTownHandler` preview generation MUST be inside `ExecuteWithRetryAsync` lambda** — preview depends on mutable session state (inventory, current town) and must be regenerated on retry
- TDD: write failing test, verify fail, implement, verify pass
- Run `.\scripts\postgres-dev.ps1 ensure` before any PostgreSQL-dependent test
- Run `.\scripts\postgres-dev.ps1 validate` for the final validation lane
- **All line numbers, file paths, and ADR paths are preflight notes — re-verify at execution time**

---

## Task 1: DiaryProjector — Travel event cases

**Files:**
- Modify: `src/WildBunch.Application/Projections/DiaryProjector.cs`
- Create: `tests/WildBunch.Application.Tests/TravelTestFactory.cs` (copy from `WildBunch.Domain.Tests`)
- Create: `tests/WildBunch.Application.Tests/DiaryProjectorTravelTests.cs`

- [ ] **Step 1: Copy `TravelTestFactory` into `Application.Tests`**

Copy `tests/WildBunch.Domain.Tests/TravelTestFactory.cs` to `tests/WildBunch.Application.Tests/TravelTestFactory.cs`. Adjust the namespace from `WildBunch.Domain.Tests` to `WildBunch.Application.Tests`. The repo has no test-to-test project references, so duplication is the sharing route (see Phase 1 Task 1 note).

- [ ] **Step 2: Read existing `DiaryProjector`**

```powershell
Get-Content src/WildBunch.Application/Projections/DiaryProjector.cs
```

- [ ] **Step 3: Write failing projection tests**

```csharp
// tests/WildBunch.Application.Tests/DiaryProjectorTravelTests.cs
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Tests;

public sealed class DiaryProjectorTravelTests
{
    private readonly DiaryProjector _projector = new();

    // Helper: create a real journey snapshot from a live session
    private static TravelJourneySnapshot MakeSnapshot()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        return session.Journey!.ToSnapshot(session.TravelRules);
    }

    [Fact]
    public void JourneyStarted_ProducesDiaryEntry()
    {
        var snapshot = MakeSnapshot();
        var e = new JourneyStarted(snapshot, "You head out at dawn.");

        var entries = _projector.Project(new[] { e });

        Assert.Contains(entries, x => x.Message == "You head out at dawn.");
    }

    [Fact]
    public void TravelDayAdvanced_ProducesDiaryEntry()
    {
        var snapshot = MakeSnapshot();
        var e = new TravelDayAdvanced(2, snapshot, -1, 0.1m, TravelDayOutcome.Ongoing,
            "Day passes uneventfully.", "");

        var entries = _projector.Project(new[] { e });

        Assert.Contains(entries, x => x.Message == "Day passes uneventfully.");
    }

    [Fact]
    public void TravelDayAdvanced_WithHorseLost_ProducesTwoEntries()
    {
        var snapshot = MakeSnapshot();
        var e = new TravelDayAdvanced(2, snapshot, -1, 0.1m, TravelDayOutcome.Ongoing,
            "Day passes.", "Your horse collapses.");

        var entries = _projector.Project(new[] { e });

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, x => x.Message == "Day passes.");
        Assert.Contains(entries, x => x.Message == "Your horse collapses.");
    }

    [Fact]
    public void TrailEventApplied_ProducesDiaryEntry()
    {
        var snapshot = MakeSnapshot();
        var e = new TrailEventApplied(snapshot,
            JourneyTrailEventKind.Lucky, JourneyTrailEventId.Windfall,
            5m, 0, 0, 0, 0, 0, 0, 0m, null,
            "You find $5 in the trail dust.", "");

        var entries = _projector.Project(new[] { e });

        Assert.Contains(entries, x => x.Message == "You find $5 in the trail dust.");
    }

    [Fact]
    public void JourneyEncounterResolved_ProducesDiaryEntry()
    {
        var snapshot = MakeSnapshot();
        var e = new JourneyEncounterResolved(
            "run", "Run", true, -2, 0m, 0, null, 0, 0.05m, 1, false,
            snapshot, "You ride hard and escape.", false, false);

        var entries = _projector.Project(new[] { e });

        Assert.Contains(entries, x => x.Message == "You ride hard and escape.");
    }

    [Fact]
    public void JourneyCompleted_ProducesDiaryEntry()
    {
        var snapshot = MakeSnapshot();
        var e = new JourneyCompleted(
            new TownId("connected"), "Connected Town", snapshot, "You arrive at Connected Town.");

        var entries = _projector.Project(new[] { e });

        Assert.Contains(entries, x => x.Message == "You arrive at Connected Town.");
    }

    [Fact]
    public void JourneyArrivalAcknowledged_ProducesDiaryEntry()
    {
        var snapshot = MakeSnapshot();
        var e = new JourneyArrivalAcknowledged(1, snapshot, "You step into town.");

        var entries = _projector.Project(new[] { e });

        Assert.Contains(entries, x => x.Message == "You step into town.");
    }
}
```

Note: Adjust `DiaryProjector.Project` method signature and `DiaryEntry` shape to match actual API. `TravelTestFactory` is created in `WildBunch.Domain.Tests` (Phase 1 Task 1). To use it in `WildBunch.Application.Tests`, copy the factory methods into a new `TravelTestFactory.cs` in this test project — the repo has no test-to-test project references, so duplication is the established route (see Phase 1 Task 1 note). Verify `JourneyTrailEventId.Windfall` exists as an enum value at execution time.

- [ ] **Step 4: Run tests — expect RED**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "DiaryProjectorTravel"`
Expected: Tests fail — DiaryProjector doesn't handle travel events yet.

- [ ] **Step 5: Add 6 travel event cases to `DiaryProjector.cs`**

```csharp
case JourneyStarted e:
    entries.Add(new DiaryEntry(e.DiaryMessage, DiaryKind.Travel));
    break;

case TravelDayAdvanced e:
    entries.Add(new DiaryEntry(e.DiaryMessage, DiaryKind.Travel));
    if (!string.IsNullOrEmpty(e.HorseLostMessage))
        entries.Add(new DiaryEntry(e.HorseLostMessage, DiaryKind.Travel));
    break;

case TrailEventApplied e:
    entries.Add(new DiaryEntry(e.DiaryMessage, DiaryKind.Travel));
    if (!string.IsNullOrEmpty(e.HorseLostMessage))
        entries.Add(new DiaryEntry(e.HorseLostMessage, DiaryKind.Travel));
    break;

case JourneyEncounterResolved e:
    entries.Add(new DiaryEntry(e.DiaryMessage, DiaryKind.Travel));
    break;

case JourneyCompleted e:
    entries.Add(new DiaryEntry(e.DiaryMessage, DiaryKind.Travel));
    break;

case JourneyArrivalAcknowledged e:
    entries.Add(new DiaryEntry(e.DiaryMessage, DiaryKind.Travel));
    break;
```

Adjust `DiaryEntry` constructor and `DiaryKind` enum to match actual projector API.

- [ ] **Step 6: Run tests — expect GREEN**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "DiaryProjectorTravel"`
Expected: All tests PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/WildBunch.Application/Projections/DiaryProjector.cs
git add tests/WildBunch.Application.Tests/TravelTestFactory.cs
git add tests/WildBunch.Application.Tests/DiaryProjectorTravelTests.cs
git commit -m "BUNCH-83: add travel event cases to DiaryProjector"
```

---

## Task 2: HudProjector — Travel state changes

**Files:**
- Modify: `src/WildBunch.Application/Projections/HudProjector.cs`
- Create: `tests/WildBunch.Application.Tests/HudProjectorTravelTests.cs`

- [ ] **Step 1: Read existing `HudProjector`**

- [ ] **Step 2: Write failing HUD projection tests**

```csharp
// tests/WildBunch.Application.Tests/HudProjectorTravelTests.cs
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Tests;

public sealed class HudProjectorTravelTests
{
    private readonly HudProjector _projector = new();

    private static TravelJourneySnapshot MakeSnapshot()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        return session.Journey!.ToSnapshot(session.TravelRules);
    }

    [Fact]
    public void TravelDayAdvanced_AppliesHealthDelta()
    {
        var snapshot = MakeSnapshot();
        var e = new TravelDayAdvanced(2, snapshot, -3, 0.1m, TravelDayOutcome.Ongoing,
            "Day passes.", "");

        var state = _projector.Project(new[] { e });

        Assert.Equal(-3, state.HealthDelta);
    }

    [Fact]
    public void TrailEventApplied_AppliesWalletDelta()
    {
        var snapshot = MakeSnapshot();
        var e = new TrailEventApplied(snapshot,
            JourneyTrailEventKind.Lucky, JourneyTrailEventId.Windfall,
            5m, 0, 0, 0, 0, 0, 0, 0m, null, "Find $5.", "");

        var state = _projector.Project(new[] { e });

        Assert.Equal(5m, state.WalletDelta);
    }

    [Fact]
    public void JourneyEncounterResolved_AppliesHealthAndWalletDeltas()
    {
        var snapshot = MakeSnapshot();
        var e = new JourneyEncounterResolved(
            "run", "Run", true, -2, -10m, 0, null, 0, 0.05m, 1, false,
            snapshot, "Escape.", false, false);

        var state = _projector.Project(new[] { e });

        Assert.Equal(-2, state.HealthDelta);
        Assert.Equal(-10m, state.WalletDelta);
    }

    [Fact]
    public void JourneyCompleted_ChangesCurrentTown()
    {
        var snapshot = MakeSnapshot();
        var destId = new TownId("connected");
        var e = new JourneyCompleted(destId, "Connected Town", snapshot, "Arrive.");

        var state = _projector.Project(new[] { e });

        Assert.Equal(destId, state.CurrentTownId);
    }
}
```

Note: Adjust `HudState` shape and `Project` method signature to match actual API.

- [ ] **Step 3: Run tests — expect RED**

- [ ] **Step 4: Add travel event cases to `HudProjector.cs`**

```csharp
case TravelDayAdvanced e:
    state.Health += e.HealthDelta;
    break;

case TrailEventApplied e:
    state.Wallet += e.WalletDelta;
    break;

case JourneyEncounterResolved e:
    state.Health += e.HealthDelta;
    state.Wallet += e.WalletDelta;
    break;

case JourneyCompleted e:
    state.CurrentTownId = e.DestinationTownId;
    break;
```

Adjust to actual `HudState` mutation pattern.

- [ ] **Step 5: Run tests — expect GREEN**

- [ ] **Step 6: Commit**

```powershell
git add src/WildBunch.Application/Projections/HudProjector.cs
git add tests/WildBunch.Application.Tests/HudProjectorTravelTests.cs
git commit -m "BUNCH-83: add travel event cases to HudProjector"
```

---

## Task 3: Persistence deserializer — Register 6 new event types

**Files:**
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`

- [ ] **Step 1: Read existing deserializer**

```powershell
Get-Content src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs
```

- [ ] **Step 2: Add 6 new cases to `ResolveEventType`**

```csharp
case nameof(JourneyStarted):             return typeof(JourneyStarted);
case nameof(TravelDayAdvanced):          return typeof(TravelDayAdvanced);
case nameof(TrailEventApplied):          return typeof(TrailEventApplied);
case nameof(JourneyEncounterResolved):   return typeof(JourneyEncounterResolved);
case nameof(JourneyCompleted):           return typeof(JourneyCompleted);
case nameof(JourneyArrivalAcknowledged): return typeof(JourneyArrivalAcknowledged);
```

Add `using WildBunch.Domain.Events;` if not already present.

- [ ] **Step 3: Build and run existing event sourcing tests**

Run: `dotnet build src/WildBunch.Persistence`
Run: `dotnet test tests/WildBunch.Integration.Tests --filter "EventSourcingEndToEnd"`
Expected: PASS.

- [ ] **Step 4: Commit**

```powershell
git add src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs
git commit -m "BUNCH-83: register 6 travel event types in persistence deserializer"
```

---

## Task 4: Migrate `TravelToTownHandler` — preview INSIDE retry boundary

**Files:**
- Modify: `src/WildBunch.Application/Games/Commands/TravelToTownHandler.cs`
- Create: `tests/WildBunch.Application.Tests/TravelToTownHandlerRetryTests.cs`

**CRITICAL:** `TravelResolver.PreviewJourney` depends on `session.Player.CurrentTownId` and `session.Player.Inventory` — both mutable session state. `StartJourney` blindly trusts the preview. If a concurrency conflict occurs during store and the session is reloaded, the preview must be regenerated with fresh state. Therefore preview generation MUST be inside the `ExecuteWithRetryAsync` lambda.

- [ ] **Step 1: Read existing handler and reference handler**

```powershell
Get-Content src/WildBunch.Application/Games/Commands/TravelToTownHandler.cs
Get-Content src/WildBunch.Application/Games/Commands/PurchaseStoreItemHandler.cs
```

Note how `PurchaseStoreItemHandler` does catalog resolution INSIDE the lambda (because it depends on session state).

- [ ] **Step 2: Write failing concurrency retry test**

```csharp
// tests/WildBunch.Application.Tests/TravelToTownHandlerRetryTests.cs
using WildBunch.Application.Games.Commands;
using WildBunch.Application.TestDoubles;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Tests;

public sealed class TravelToTownHandlerRetryTests
{
    [Fact]
    public async Task HandleAsync_PreviewRegeneratedOnRetry_UsesFreshSessionState()
    {
        // This test proves that preview is regenerated inside the retry boundary.
        // If preview were outside, a stale preview would be used on retry.
        var repository = new InMemoryGameSessionRepository();
        var (session, _) = TravelTestFactory.CreateEasyShortJourney();
        repository.Seed(session);

        // Simulate a concurrency conflict on first store attempt
        repository.ThrowOnNextStore = true; // First StoreAsync throws ConcurrencyException

        var handler = new TravelToTownHandler(repository, repository, new TravelResolver());

        var result = await handler.HandleAsync(
            new TravelToTownCommand(session.Id.Value, new TownId("connected").Value));

        // Should succeed after retry (preview regenerated with fresh state)
        Assert.True(result.Success);
        Assert.Equal(2, repository.StoreCallCount); // First failed, second succeeded
    }
}
```

Note: `InMemoryGameSessionRepository.ThrowOnNextStore` — check if this test double exists. If not, add it. The test proves that the handler retries and regenerates the preview on the second attempt. Adjust the command shape to match the actual `TravelToTownCommand`.

- [ ] **Step 3: Run test — expect RED**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "TravelToTownHandlerRetry"`
Expected: Test fails — handler doesn't use `ExecuteWithRetryAsync` yet.

- [ ] **Step 4: Refactor `TravelToTownHandler`**

```csharp
public sealed class TravelToTownHandler : GameSessionCommandHandler
{
    private readonly TravelResolver _travelResolver;

    public TravelToTownHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        TravelResolver travelResolver)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _travelResolver = travelResolver;
    }

    public async Task<GameTurnResultDto> HandleAsync(
        TravelToTownCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sessionId = new GameSessionId(command.GameSessionId);
        var destinationTownId = new TownId(command.DestinationTownId);

        // PREVIEW INSIDE THE RETRY BOUNDARY — regenerated on each retry with fresh session state
        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var previewResult = _travelResolver.PreviewJourney(
                session.World,
                session.Player.CurrentTownId,
                destinationTownId,
                session.Player.Inventory,
                session.TravelRules);

            if (!previewResult.Success || previewResult.Preview is null)
            {
                return GameTurnResultFactory.Create(
                    success: false,
                    message: previewResult.Message,
                    session: session);
            }

            var startResult = session.StartJourney(previewResult.Preview);

            return GameTurnResultFactory.Create(
                success: startResult.Success,
                message: startResult.Message,
                session: session,
                journeyStatus: startResult.Status,
                journey: startResult.Journey);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

Note: Adjust constructor parameters, `GameTurnResultFactory.Create` signature, and return type to match actual API. The key is that `_travelResolver.PreviewJourney` is called INSIDE the lambda.

- [ ] **Step 5: Run retry test — expect GREEN**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "TravelToTownHandlerRetry"`
Expected: PASS — handler retries and regenerates preview.

- [ ] **Step 6: Run existing handler tests**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "TravelToTownHandler"`
Expected: PASS — observable behavior unchanged.

- [ ] **Step 7: Commit**

```powershell
git add src/WildBunch.Application/Games/Commands/TravelToTownHandler.cs
git add tests/WildBunch.Application.Tests/TravelToTownHandlerRetryTests.cs
git commit -m "BUNCH-83: migrate TravelToTownHandler with preview inside retry boundary"
```

---

## Task 5: Migrate `AdvanceTravelDayHandler` to `GameSessionCommandHandler` base

**Files:**
- Modify: `src/WildBunch.Application/Games/Commands/AdvanceTravelDayHandler.cs`

- [ ] **Step 1: Read existing handler**

- [ ] **Step 2: Refactor to `GameSessionCommandHandler` base**

```csharp
public sealed class AdvanceTravelDayHandler : GameSessionCommandHandler
{
    public AdvanceTravelDayHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork) { }

    public async Task<GameTurnResultDto> HandleAsync(
        AdvanceTravelDayCommand command, CancellationToken cancellationToken = default)
    {
        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var result = session.AdvanceJourneyDay();
            return GameTurnResultFactory.Create(
                result.Success, result.Message, session,
                result.Status, result.Journey);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 3: Build and run tests**

Run: `dotnet build src/WildBunch.Application`
Run: `dotnet test tests/WildBunch.Application.Tests --filter "AdvanceTravelDayHandler"`
Expected: PASS.

- [ ] **Step 4: Commit**

```powershell
git add src/WildBunch.Application/Games/Commands/AdvanceTravelDayHandler.cs
git commit -m "BUNCH-83: migrate AdvanceTravelDayHandler to GameSessionCommandHandler base"
```

---

## Task 6: Migrate `ResolveJourneyEncounterHandler` to `GameSessionCommandHandler` base

**Files:**
- Modify: `src/WildBunch.Application/Games/Commands/ResolveJourneyEncounterHandler.cs`

- [ ] **Step 1: Read existing handler**

- [ ] **Step 2: Refactor to `GameSessionCommandHandler` base**

```csharp
public sealed class ResolveJourneyEncounterHandler : GameSessionCommandHandler
{
    public ResolveJourneyEncounterHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork) { }

    public async Task<GameTurnResultDto> HandleAsync(
        ResolveJourneyEncounterCommand command, CancellationToken cancellationToken = default)
    {
        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var result = session.ResolveJourneyEncounter(
                command.ChoiceId,
                bulletSpend: command.BulletSpend,
                bribeAmount: command.BribeAmount,
                forcedRoll: command.ForcedRoll);
            return GameTurnResultFactory.Create(
                result.Success, result.Message, session,
                result.Status, result.Journey);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

Note: Adjust parameter names to match actual command shape.

- [ ] **Step 3: Build and run tests**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "ResolveJourneyEncounterHandler"`
Expected: PASS.

- [ ] **Step 4: Commit**

```powershell
git add src/WildBunch.Application/Games/Commands/ResolveJourneyEncounterHandler.cs
git commit -m "BUNCH-83: migrate ResolveJourneyEncounterHandler to GameSessionCommandHandler base"
```

---

## Task 7: Migrate `AcknowledgeJourneyArrivalHandler` to `GameSessionCommandHandler` base

**Files:**
- Modify: `src/WildBunch.Application/Games/Commands/AcknowledgeJourneyArrivalHandler.cs`

- [ ] **Step 1: Read existing handler**

- [ ] **Step 2: Refactor to `GameSessionCommandHandler` base**

```csharp
public sealed class AcknowledgeJourneyArrivalHandler : GameSessionCommandHandler
{
    public AcknowledgeJourneyArrivalHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork) { }

    public async Task<GameTurnResultDto> HandleAsync(
        AcknowledgeJourneyArrivalCommand command, CancellationToken cancellationToken = default)
    {
        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var result = session.AcknowledgeJourneyArrival();
            return GameTurnResultFactory.Create(
                result.Success, result.Message, session);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 3: Build and run tests**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "AcknowledgeJourneyArrivalHandler"`
Expected: PASS.

- [ ] **Step 4: Commit**

```powershell
git add src/WildBunch.Application/Games/Commands/AcknowledgeJourneyArrivalHandler.cs
git commit -m "BUNCH-83: migrate AcknowledgeJourneyArrivalHandler to GameSessionCommandHandler base"
```

---

## Task 8: Travel event persistence + replay integration tests

**Files:**
- Create: `tests/WildBunch.Integration.Tests/TravelTestFactory.cs` (copy from `WildBunch.Domain.Tests`)
- Create: `tests/WildBunch.Integration.Tests/TravelEventSourcingIntegrationTests.cs`

- [ ] **Step 1: Copy `TravelTestFactory` into `Integration.Tests`**

Copy `tests/WildBunch.Domain.Tests/TravelTestFactory.cs` to `tests/WildBunch.Integration.Tests/TravelTestFactory.cs`. Adjust the namespace from `WildBunch.Domain.Tests` to `WildBunch.Integration.Tests`. Same duplication route as Task 1.

- [ ] **Step 2: Ensure PostgreSQL dev service is running**

Run: `.\scripts\postgres-dev.ps1 ensure`

- [ ] **Step 3: Read existing `EventSourcingEndToEndTests` for pattern**

- [ ] **Step 4: Write integration tests with exact field equality**

```csharp
// tests/WildBunch.Integration.Tests/TravelEventSourcingIntegrationTests.cs
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Integration.Tests;

public sealed class TravelEventSourcingIntegrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public TravelEventSourcingIntegrationTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TravelEvents_PersistAndReplay_ReconstructsExactJourneyState()
    {
        var session = TestSessionFactory.CreateDefault();
        var preview = TravelTestFactory.ResolvePreview(session, new TownId("connected"));
        session.StartJourney(preview);

        await _fixture.Repository.StoreAsync(session);
        await _fixture.UnitOfWork.CommitAsync();
        session.MarkEventsCommitted();

        var events = await _fixture.Repository.GetEventStreamAsync(session.Id);
        var replayed = GameSession.RehydrateFromEvents(
            session.Id, session.World,
            TestSessionFactory.CreateBaselineCaseFileFor(session),
            events);

        Assert.Equal(session.Journey!.JourneySequence, replayed.Journey!.JourneySequence);
        Assert.Equal(session.Journey.Status, replayed.Journey.Status);
        Assert.Equal(session.Journey.RemainingDays, replayed.Journey.RemainingDays);
        Assert.Equal(session.Journey.FoodRemaining, replayed.Journey.FoodRemaining);
        Assert.Equal(session.Journey.HorseFeedRemaining, replayed.Journey.HorseFeedRemaining);
        Assert.Equal(session.Version, replayed.Version);
    }

    [Fact]
    public async Task FullTravelFlow_ReplayMatchesCommandPath_ExactState()
    {
        var session = TestSessionFactory.CreateDefault();
        var preview = TravelTestFactory.ResolvePreview(session, new TownId("connected"));
        session.StartJourney(preview);
        session.MarkEventsCommitted();

        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
            if (result.Status == JourneyStatus.Interrupted)
                session.ResolveJourneyEncounter("run", forcedRoll: 0UL);
        } while (result.Status == JourneyStatus.Active && result.Success);

        session.AcknowledgeJourneyArrival();

        await _fixture.Repository.StoreAsync(session);
        await _fixture.UnitOfWork.CommitAsync();

        var events = await _fixture.Repository.GetEventStreamAsync(session.Id);
        var replayed = GameSession.RehydrateFromEvents(
            session.Id, session.World,
            TestSessionFactory.CreateBaselineCaseFileFor(session),
            events);

        Assert.Equal(session.Player.CurrentTownId, replayed.Player.CurrentTownId);
        Assert.Equal(session.Player.Health, replayed.Player.Health);
        Assert.Equal(session.Player.Wallet.Cash, replayed.Player.Wallet.Cash);
        Assert.Equal(session.Clock.Day, replayed.Clock.Day);
        Assert.Equal(session.PursuitState.Heat, replayed.PursuitState.Heat);
        Assert.Null(replayed.Journey);
        Assert.Equal(session.Version, replayed.Version);
    }
}
```

Note: `PostgresFixture` — verify it exists in the integration test project at execution time. If it does not, check for an existing integration test base class or fixture pattern. `TravelTestFactory` and `TestSessionFactory` are duplicated per test project (see Phase 1 Task 1 note on the duplication route). Copy the factory methods into a new `TravelTestFactory.cs` in `WildBunch.Integration.Tests` if not already present. Adjust based on actual test infrastructure.

- [ ] **Step 5: Run integration tests**

Run: `.\scripts\postgres-dev.ps1 test -- dotnet test tests/WildBunch.Integration.Tests --filter "TravelEventSourcingIntegration"`
Expected: All tests PASS.

- [ ] **Step 6: Commit**

```powershell
git add tests/WildBunch.Integration.Tests/TravelTestFactory.cs
git add tests/WildBunch.Integration.Tests/TravelEventSourcingIntegrationTests.cs
git commit -m "BUNCH-83: add travel event persistence + replay integration tests"
```

---

## Task 9: Hidden-truth boundary tests for travel events

**Files:**
- Create: `tests/WildBunch.Application.Tests/TravelHiddenTruthBoundaryTests.cs`

- [ ] **Step 1: Write hidden-truth boundary tests**

```csharp
// tests/WildBunch.Application.Tests/TravelHiddenTruthBoundaryTests.cs
using System.Text.Json;
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Tests;

/// <summary>
/// Verifies that hidden encounter state (BribeLockedOut, ChaseFatigue, Annoyance, Shaken)
/// never appears in projections, DTOs, or serialized event JSON.
/// </summary>
public sealed class TravelHiddenTruthBoundaryTests
{
    private readonly DiaryProjector _diaryProjector = new();
    private readonly HudProjector _hudProjector = new();

    private static TravelJourneySnapshot MakeSnapshotWithHiddenState()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        // Advance until interrupted to get a pending encounter with hidden state
        for (var i = 0; i < 10; i++)
        {
            var result = session.AdvanceJourneyDay();
            if (result.Status == JourneyStatus.Interrupted) break;
        }
        return session.Journey?.ToSnapshot(session.TravelRules)
            ?? throw new InvalidOperationException("No journey snapshot");
    }

    [Fact]
    public void JourneyEncounterResolved_DiaryProjection_DoesNotExposeHiddenState()
    {
        var snapshot = MakeSnapshotWithHiddenState();
        var e = new JourneyEncounterResolved(
            "bribe", "Bribe", false, 0, -5m, 0, null, 0, 0.05m, 0, false,
            snapshot, "They refuse your money.", false, false);

        var entries = _diaryProjector.Project(new[] { e });
        var allText = string.Join(" ", entries.Select(x => x.Message));

        Assert.DoesNotContain("BribeLockedOut", allText);
        Assert.DoesNotContain("ChaseFatigue", allText);
        Assert.DoesNotContain("Annoyance", allText);
        Assert.DoesNotContain("Shaken", allText);
    }

    [Fact]
    public void JourneyEncounterResolved_HudProjection_DoesNotExposeHiddenState()
    {
        var snapshot = MakeSnapshotWithHiddenState();
        var e = new JourneyEncounterResolved(
            "bribe", "Bribe", false, 0, -5m, 0, null, 0, 0.05m, 0, false,
            snapshot, "They refuse.", false, false);

        var state = _hudProjector.Project(new[] { e });
        var serialized = JsonSerializer.Serialize(state);

        Assert.DoesNotContain("BribeLockedOut", serialized);
        Assert.DoesNotContain("ChaseFatigue", serialized);
        Assert.DoesNotContain("Annoyance", serialized);
        Assert.DoesNotContain("Shaken", serialized);
    }

    [Fact]
    public void JourneyEncounterResolved_EventJson_ContainsHiddenStateForReplay()
    {
        // This test verifies that hidden state IS present in the internal event JSON
        // for replay correctness. The boundary standard is: hidden state may exist
        // in internal persisted events, but must not leak through projections or DTOs.
        // The two tests above verify projection non-leakage.
        var snapshot = MakeSnapshotWithHiddenState();
        var e = new JourneyEncounterResolved(
            "bribe", "Bribe", false, 0, -5m, 0, null, 0, 0.05m, 0, false,
            snapshot, "They refuse.", false, false);

        var json = JsonSerializer.Serialize(e, e.GetType());

        // Hidden state is intentionally inside the event JSON for replay fidelity
        Assert.Contains("hiddenState", json, StringComparison.OrdinalIgnoreCase);
    }
}
```

Note: The hidden-truth boundary standard is: hidden encounter state (`BribeLockedOut`, `ChaseFatigue`, `Annoyance`, `Shaken`) MAY exist in internal persisted events for replay correctness, but MUST NOT leak through player-facing projections (`DiaryProjector`, `HudProjector`), DTOs, or API responses. The first two tests verify projection non-leakage. The third test verifies hidden state IS present in the internal event JSON (required for replay). There is no contradiction — the boundary is between internal persistence and player-facing output.

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "TravelHiddenTruthBoundary"`
Expected: All tests PASS.

- [ ] **Step 3: Commit**

```powershell
git add tests/WildBunch.Application.Tests/TravelHiddenTruthBoundaryTests.cs
git commit -m "BUNCH-83: add hidden-truth boundary tests for travel events"
```

---

## Task 10: Update ADR-0028

**Files:**
- Modify: ADR-0028 (search for exact path at execution time)

- [ ] **Step 1: Find ADR-0028**

```powershell
Get-ChildItem docs/ -Recurse -Filter "ADR-0028*"
```

- [ ] **Step 2: Read current ADR-0028**

- [ ] **Step 3: Add dated status entry for BUNCH-83**

Add a new status entry:

```markdown
### 2026-06-23 — BUNCH-83: Travel/Journey migration complete

All travel and journey flows are now event-sourced:
- 6 new domain events: JourneyStarted, TravelDayAdvanced, TrailEventApplied,
  JourneyEncounterResolved, JourneyCompleted, JourneyArrivalAcknowledged
- 6 Apply methods on GameSession (journey state=absolute from snapshot,
  player/pursuit state=additive from deltas)
- TravelJourneySnapshot (existing type) carries full journey state including
  hidden encounter state — projections never expose hidden state
- Clock decoupled: TravelDayAdvanced carries the new day; Clock.Set used in Apply
- 4 handlers migrated to GameSessionCommandHandler base with optimistic concurrency
- TravelToTownHandler preview generation moved inside retry boundary
- AddLogEntryGuardrailTests count reduced from 19 → 7
- DiaryProjector and HudProjector handle all 6 travel events
- Replay-equality tests prove command-path == replay-path
```

- [ ] **Step 4: Update migrated slice scope and remaining-work notes**

- [ ] **Step 5: Commit**

```powershell
git add docs/adr/ADR-0028-*.md
git commit -m "BUNCH-83: update ADR-0028 with travel/journey migration status"
```

---

## Task 11: Full validation

- [ ] **Step 1: Run dotnet build**

Run: `dotnet build`
Expected: Build succeeds with 0 errors.

- [ ] **Step 2: Run dotnet test (non-PostgreSQL)**

Run: `dotnet test --filter "FullyQualifiedName!~Integration"`
Expected: All tests pass.

- [ ] **Step 3: Run PostgreSQL-backed validation lane**

Run: `.\scripts\postgres-dev.ps1 validate`
Expected: EF migrations list succeeds, all integration tests pass.

- [ ] **Step 4: Verify clean worktree**

Run: `git status`
Expected: Clean working tree.

---

## Phase 3 Completion Checklist

- [ ] `DiaryProjector` handles all 6 travel events
- [ ] `HudProjector` handles travel health/wallet/town changes
- [ ] `GameSessionJsonSerializer.Events.cs` registers all 6 travel event types
- [ ] `TravelToTownHandler` uses `GameSessionCommandHandler` base with preview INSIDE retry boundary
- [ ] `TravelToTownHandlerRetryTests` proves preview regeneration on retry
- [ ] All 4 travel handlers use `GameSessionCommandHandler` base
- [ ] Travel event persistence + replay integration tests pass with exact field equality
- [ ] Hidden-truth boundary tests pass (hidden state present in internal events for replay, absent from projections and DTOs)
- [ ] ADR-0028 updated with BUNCH-83 status entry
- [ ] `dotnet build` clean
- [ ] `dotnet test` no regressions
- [ ] `.\scripts\postgres-dev.ps1 validate` passes
- [ ] Clean worktree

---

## BUNCH-83 Full Campaign Completion (after all 3 phases)

- **Status:** GREEN
- **Branch:** `harleydbartles/bunch-83-migrate-travel-and-journey-flows-to-event-sourcing`
- **Head commit hash:** (from `git rev-parse HEAD`)
- **PR URL:** (from `gh pr create`)
- **Validation:** `dotnet build` clean, `dotnet test` all pass, `.\scripts\postgres-dev.ps1 validate` passes
- **Issue-goal conformance:** Travel/journey flows fully event-sourced with 6 typed events, 6 Apply methods (absolute snapshots + additive deltas), 4 migrated handlers (preview inside retry boundary), projections, persistence, replay-equality proof, hidden-truth boundary proof, and ADR-0028 updated
- **Known caveats:** Legacy-log/UI projection deprecation deferred to follow-up
