# BUNCH-83 Phase 2: Events + Apply + Domain Method Migration

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Define 6 typed domain events, implement 6 `Apply` methods on `GameSession`, register events in both dispatch switches, and migrate all 8 travel domain methods from direct mutation to `ProduceEvent → Apply` — leaving observable behavior identical and all Phase 1 characterization tests passing.

**Architecture:** Each domain method computes deltas as before, then calls `ProduceEvent<T>(e)` which atomically calls `Apply(e)` (state mutation) and enqueues the event. `Apply` methods use **absolute snapshots** for journey state (`_journey = TravelJourney.FromSnapshot(e.JourneySnapshot)`) and **additive deltas** for player/pursuit state (`_player.ApplyHealthDelta(e.HealthDelta)`). This separation prevents double-application. The 13 direct `AddLogEntry(GameLogEntryKind.Travel, ...)` call sites are replaced by a single `RecordTravelUpdate(string message)` helper. Clock decoupling: `Apply(TravelDayAdvanced)` calls `Clock.Set(e.Day, 0)` — `Clock.Set` already exists.

**Tech Stack:** C#/.NET 10, xUnit, existing `ProduceEvent<T>` / `ApplyProducedEvent` infrastructure

## Global Constraints

- Phase 1 characterization tests must pass at every commit
- **`TravelJourneySnapshot` already exists** at `src/WildBunch.Domain/Travel/TravelRouteModels.cs:82` — do NOT create a new snapshot type
- **`TravelJourney.ToSnapshot()` and `TravelJourney.FromSnapshot()` already exist** — use them directly
- **`Clock.Set(int day, int turn)` already exists** — used by `Apply(TownActionContextEntered)` — do NOT create it
- **Event semantics:** journey state = absolute (from snapshot), player/pursuit state = additive (from deltas). Never mix the two.
- **Snapshot timing:** each event's `JourneySnapshot` captures journey state AFTER that event's changes. During replay, `Apply` sets `_journey` absolutely from each snapshot in order.
- Hidden encounter state travels inside `TravelJourneySnapshot` (inside `PendingEncounter.HiddenState`) — never strip it, never expose it in projections
- `AddLogEntryGuardrailTests` constant changes from 19 → 7 (test uses `Assert.True(count <= N)`, upper bound)
- No scope creep into projections (Phase 3) or handler migration (Phase 3)
- TDD: write failing test, verify fail (`dotnet test` shows red), implement, verify pass
- **All line numbers and file paths are preflight notes — re-verify at execution time**

---

## Task 1: Define `TravelDayOutcome` enum

**Files:**
- Create: `src/WildBunch.Domain/Game/TravelDayOutcome.cs`

- [ ] **Step 1: Write the enum**

```csharp
// src/WildBunch.Domain/Game/TravelDayOutcome.cs
namespace WildBunch.Domain.Game;

/// <summary>
/// Outcome of a single travel day advance, carried in TravelDayAdvanced events.
/// </summary>
public enum TravelDayOutcome
{
    Ongoing,
    Interrupted,
    Completed,
}
```

- [ ] **Step 2: Build**

Run: `dotnet build src/WildBunch.Domain`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```powershell
git add src/WildBunch.Domain/Game/TravelDayOutcome.cs
git commit -m "BUNCH-83: add TravelDayOutcome enum"
```

---

## Task 2: Define 6 typed domain events

**Files:**
- Create: `src/WildBunch.Domain/Events/JourneyStarted.cs`
- Create: `src/WildBunch.Domain/Events/TravelDayAdvanced.cs`
- Create: `src/WildBunch.Domain/Events/TrailEventApplied.cs`
- Create: `src/WildBunch.Domain/Events/JourneyEncounterResolved.cs`
- Create: `src/WildBunch.Domain/Events/JourneyCompleted.cs`
- Create: `src/WildBunch.Domain/Events/JourneyArrivalAcknowledged.cs`

**Interfaces:**
- Consumes: `IDomainEvent` (existing base), existing `TravelJourneySnapshot`, `TravelDayOutcome`
- Produces: 6 sealed event record types

- [ ] **Step 1: Check existing event file structure**

Run: `Get-ChildItem src/WildBunch.Domain/Events/`
Note the namespace, base type (`IDomainEvent`), and sealed record pattern.

- [ ] **Step 2: Write `JourneyStarted`**

```csharp
// src/WildBunch.Domain/Events/JourneyStarted.cs
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Raised when a player departs on a new trail journey.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it.
/// </summary>
public sealed record JourneyStarted(
    TravelJourneySnapshot JourneySnapshot,
    string DiaryMessage
) : IDomainEvent;
```

- [ ] **Step 3: Write `TravelDayAdvanced`**

```csharp
// src/WildBunch.Domain/Events/TravelDayAdvanced.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Raised whenever a travel day advances (always exactly once per successful AdvanceJourneyDay).
/// Day is ABSOLUTE — Apply calls Clock.Set(e.Day, 0).
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it.
/// HealthDelta is ADDITIVE — Apply adds to player health.
/// PursuitHeatDelta is ADDITIVE — Apply adds to pursuit heat.
/// </summary>
public sealed record TravelDayAdvanced(
    int Day,
    TravelJourneySnapshot JourneySnapshot,
    int HealthDelta,
    decimal PursuitHeatDelta,
    TravelDayOutcome DayOutcome,
    string DiaryMessage,
    string HorseLostMessage
) : IDomainEvent;
```

- [ ] **Step 4: Write `TrailEventApplied`**

```csharp
// src/WildBunch.Domain/Events/TrailEventApplied.cs
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Raised when a trail event (Lucky or BadLuck) fires during a travel day.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it (captures delay, horse, mode changes).
/// WalletDelta, FoodDelta, CanteenChargeDelta are ADDITIVE — Apply adds to player.
/// HeatIncrease is ADDITIVE — Apply adds to pursuit heat.
/// Horse/delay/mode fields are informational for projections (journey snapshot is the source of truth).
/// </summary>
public sealed record TrailEventApplied(
    TravelJourneySnapshot JourneySnapshot,
    JourneyTrailEventKind TrailEventKind,
    JourneyTrailEventId TrailEventId,
    decimal WalletDelta,
    int FoodDelta,
    int CanteenChargeDelta,
    int HorseHungerDelta,
    int HorseThirstDelta,
    int HorseExhaustionDelta,
    int DelayDays,
    decimal HeatIncrease,
    TravelMode? TravelModeChangedTo,
    string DiaryMessage,
    string HorseLostMessage
) : IDomainEvent;
```

Note: Verify `JourneyTrailEventKind` and `JourneyTrailEventId` exact type names. Also verify `TravelMode` namespace — add the correct `using` statement.

- [ ] **Step 5: Write `JourneyEncounterResolved`**

```csharp
// src/WildBunch.Domain/Events/JourneyEncounterResolved.cs
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Raised on every ResolveJourneyEncounter call that results in a state change.
/// Resolved=false means encounter persists (failed attempt) — hidden state changes
/// are captured in JourneySnapshot.PendingEncounter.HiddenState.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it.
/// HealthDelta, WalletDelta, AmmoSpent, StolenItem are ADDITIVE — Apply applies to player.
/// PursuitHeatDelta is ADDITIVE — Apply adds to pursuit heat.
/// </summary>
public sealed record JourneyEncounterResolved(
    string ChoiceId,
    string ChoiceLabel,
    bool Resolved,
    int HealthDelta,
    decimal WalletDelta,
    int AmmoSpent,
    ItemKind? StolenItemKind,
    int StolenItemQuantity,
    decimal PursuitHeatDelta,
    int HorseExhaustionDelta,
    bool ContinuedOnFoot,
    TravelJourneySnapshot JourneySnapshot,
    string DiaryMessage,
    bool DayCompleted,
    bool JourneyCompleted
) : IDomainEvent;
```

- [ ] **Step 6: Write `JourneyCompleted`**

```csharp
// src/WildBunch.Domain/Events/JourneyCompleted.cs
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Raised when a journey reaches its destination.
/// DestinationTownId is ABSOLUTE — Apply sets player town.
/// JourneySnapshot is ABSOLUTE — Apply sets _journey from it.
/// </summary>
public sealed record JourneyCompleted(
    TownId DestinationTownId,
    string DestinationTownName,
    TravelJourneySnapshot JourneySnapshot,
    string DiaryMessage
) : IDomainEvent;
```

- [ ] **Step 7: Write `JourneyArrivalAcknowledged`**

```csharp
// src/WildBunch.Domain/Events/JourneyArrivalAcknowledged.cs
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Raised when the player acknowledges arrival.
/// Apply archives the journey and clears _journey.
/// </summary>
public sealed record JourneyArrivalAcknowledged(
    int JourneySequence,
    TravelJourneySnapshot JourneySnapshot,
    string DiaryMessage
) : IDomainEvent;
```

- [ ] **Step 8: Build**

Run: `dotnet build src/WildBunch.Domain`
Expected: Build succeeds.

- [ ] **Step 9: Commit**

```powershell
git add src/WildBunch.Domain/Events/JourneyStarted.cs
git add src/WildBunch.Domain/Events/TravelDayAdvanced.cs
git add src/WildBunch.Domain/Events/TrailEventApplied.cs
git add src/WildBunch.Domain/Events/JourneyEncounterResolved.cs
git add src/WildBunch.Domain/Events/JourneyCompleted.cs
git add src/WildBunch.Domain/Events/JourneyArrivalAcknowledged.cs
git commit -m "BUNCH-83: define 6 typed travel domain events"
```

---

## Task 3: Write failing Apply + replay-equality tests (TDD — red first)

**Files:**
- Create: `tests/WildBunch.Domain.Tests/TravelEventApplyTests.cs`
- Create: `tests/WildBunch.Domain.Tests/TravelReplayEqualityTests.cs`

- [ ] **Step 1: Write failing Apply tests**

```csharp
// tests/WildBunch.Domain.Tests/TravelEventApplyTests.cs
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

public sealed class TravelEventApplyTests
{
    [Fact]
    public void Apply_JourneyStarted_SetsJourneyFromSnapshot()
    {
        var session = TestSessionFactory.CreateDefault();
        var (s, preview) = TravelTestFactory.CreateEasyShortJourney();
        s.StartJourney(preview);
        var snapshot = s.Journey!.ToSnapshot(s.TravelRules);

        session.Apply(new JourneyStarted(snapshot, "You head out at dawn."));

        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Equal(1, session.Journey.JourneySequence);
        Assert.Equal(snapshot.RemainingDays, session.Journey.RemainingDays);
        Assert.Equal(snapshot.FoodRemaining, session.Journey.FoodRemaining);
    }

    [Fact]
    public void Apply_TravelDayAdvanced_SetsClockAndJourneyFromSnapshot()
    {
        // Create a session with a journey already started, then capture a snapshot
        // from a separate session that has advanced one day.
        var (setupSession, preview) = TravelTestFactory.CreateEasyShortJourney();
        setupSession.StartJourney(preview);
        var startSnapshot = setupSession.Journey!.ToSnapshot(setupSession.TravelRules);

        // Create the test session and apply JourneyStarted to initialize journey state
        var session = TestSessionFactory.CreateDefault();
        session.Apply(new JourneyStarted(startSnapshot, "You head out at dawn."));

        // Now capture a snapshot from a session that has advanced one day
        setupSession.AdvanceJourneyDay();
        var advancedSnapshot = setupSession.Journey!.ToSnapshot(setupSession.TravelRules);

        session.Apply(new TravelDayAdvanced(
            Day: 2, JourneySnapshot: advancedSnapshot, HealthDelta: -1,
            PursuitHeatDelta: 0.1m, DayOutcome: TravelDayOutcome.Ongoing,
            DiaryMessage: "Day passes.", HorseLostMessage: ""));

        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(advancedSnapshot.DaysTravelled, session.Journey!.DaysTravelled);
        Assert.Equal(advancedSnapshot.FoodRemaining, session.Journey.FoodRemaining);
    }

    // ... additional Apply tests for each event type
}
```

Note: The test setup needs to create a journey on the session first (via `Apply(JourneyStarted)`) before testing `Apply(TravelDayAdvanced)`. Use the actual `TravelJourney.ToSnapshot()` method to create snapshots from live journeys. The exact approach may need adjustment based on the actual `Apply` method access modifier (check if `internal` with `[InternalsVisibleTo]`).

- [ ] **Step 2: Write failing replay-equality tests**

```csharp
// tests/WildBunch.Domain.Tests/TravelReplayEqualityTests.cs
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Proves that command-path state == replay-path state for travel events.
/// Following the BountySaloonEventSourcingTests pattern from BUNCH-80.
/// </summary>
public sealed class TravelReplayEqualityTests
{
    [Fact]
    public void Replay_JourneyStarted_MatchesCommandPath_ExactState()
    {
        var (commandSession, preview) = TravelTestFactory.CreateEasyShortJourney();
        commandSession.StartJourney(preview);
        var events = commandSession.UncommittedEvents.ToList();
        commandSession.MarkEventsCommitted();

        var replayed = GameSession.RehydrateFromEvents(
            commandSession.Id, commandSession.World,
            TestSessionFactory.CreateBaselineCaseFileFor(commandSession),
            events);

        Assert.Equal(commandSession.Journey!.JourneySequence, replayed.Journey!.JourneySequence);
        Assert.Equal(commandSession.Journey.Status, replayed.Journey.Status);
        Assert.Equal(commandSession.Journey.RemainingDays, replayed.Journey.RemainingDays);
        Assert.Equal(commandSession.Journey.FoodRemaining, replayed.Journey.FoodRemaining);
        Assert.Equal(commandSession.Journey.HorseFeedRemaining, replayed.Journey.HorseFeedRemaining);
        Assert.Equal(commandSession.Journey.AvailableCanteenCharges, replayed.Journey.AvailableCanteenCharges);
        Assert.Equal(commandSession.Version, replayed.Version);
    }

    [Fact]
    public void Replay_AdvanceJourneyDay_MatchesCommandPath_ExactState()
    {
        var (commandSession, preview) = TravelTestFactory.CreateEasyShortJourney();
        // StartJourney produces GameStarted (from StartNew) + JourneyStarted
        commandSession.StartJourney(preview);
        // AdvanceJourneyDay produces TravelDayAdvanced (+ possibly TrailEventApplied, JourneyCompleted)
        commandSession.AdvanceJourneyDay();

        // Collect ALL uncommitted events (GameStarted + JourneyStarted + TravelDayAdvanced + ...)
        var allEvents = commandSession.UncommittedEvents.ToList();

        var replayed = GameSession.RehydrateFromEvents(
            commandSession.Id, commandSession.World,
            TestSessionFactory.CreateBaselineCaseFileFor(commandSession),
            allEvents);

        Assert.Equal(commandSession.Player.Health, replayed.Player.Health);
        Assert.Equal(commandSession.Player.Wallet.Cash, replayed.Player.Wallet.Cash);
        Assert.Equal(commandSession.Clock.Day, replayed.Clock.Day);
        Assert.Equal(commandSession.PursuitState.Heat, replayed.PursuitState.Heat);
        Assert.Equal(commandSession.Journey!.DaysTravelled, replayed.Journey!.DaysTravelled);
        Assert.Equal(commandSession.Journey.FoodRemaining, replayed.Journey.FoodRemaining);
        Assert.Equal(commandSession.Version, replayed.Version);
    }

    [Fact]
    public void Replay_FullJourneyCycle_MatchesCommandPath_ExactState()
    {
        var (commandSession, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        commandSession.StartJourney(preview);

        TravelJourneyStepResult result;
        do
        {
            result = commandSession.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        commandSession.AcknowledgeJourneyArrival();

        // Collect ALL uncommitted events from the entire cycle
        var allEvents = commandSession.UncommittedEvents.ToList();

        var replayed = GameSession.RehydrateFromEvents(
            commandSession.Id, commandSession.World,
            TestSessionFactory.CreateBaselineCaseFileFor(commandSession),
            allEvents);

        Assert.Equal(commandSession.Player.CurrentTownId, replayed.Player.CurrentTownId);
        Assert.Equal(commandSession.Player.Health, replayed.Player.Health);
        Assert.Equal(commandSession.Player.Wallet.Cash, replayed.Player.Wallet.Cash);
        Assert.Equal(commandSession.Clock.Day, replayed.Clock.Day);
        Assert.Equal(commandSession.PursuitState.Heat, replayed.PursuitState.Heat);
        Assert.Null(replayed.Journey); // Journey archived after acknowledgement
        Assert.Equal(commandSession.Version, replayed.Version);
    }
}
```

Note: The tests collect ALL uncommitted events from the command path (including `GameStarted` from `StartNew`) without calling `MarkEventsCommitted()` between steps. This ensures the full event stream is available for replay. If `UncommittedEvents` does not include `GameStarted` (because `StartNew` commits it), the test setup may need to seed the session via repository first — verify at execution time. The key pattern is: collect ALL events from the command path, replay them, and assert exact field equality.

- [ ] **Step 3: Run tests — expect RED**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelEventApply|TravelReplayEquality"`
Expected: Build fails or tests fail — Apply methods don't exist yet. Correct TDD red phase.

---

## Task 4: Add `RecordTravelUpdate` helper

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`

This task adds the `RecordTravelUpdate` helper BEFORE the Apply methods in Task 5, because the Apply methods call it.

- [ ] **Step 1: Find `RecordCaseUpdate` pattern**

```powershell
Select-String -Path src/WildBunch.Domain/Game/GameSession.cs -Pattern "RecordCaseUpdate" -Context 0,6
```

- [ ] **Step 2: Add `RecordTravelUpdate`**

```csharp
private void RecordTravelUpdate(string message)
{
    if (!string.IsNullOrWhiteSpace(message))
        AddLogEntry(GameLogEntryKind.Travel, message);
}
```

- [ ] **Step 3: Build**

Run: `dotnet build src/WildBunch.Domain`
Expected: Build succeeds. The helper is not called yet, but it compiles.

- [ ] **Step 4: Commit**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-83: add RecordTravelUpdate helper"
```

---

## Task 5: Implement 6 `Apply` methods on `GameSession`

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`

This task depends on Task 4 (`RecordTravelUpdate` helper) and Task 2 (6 event types).

- [ ] **Step 1: Read existing Apply methods and dispatch for pattern**

```powershell
Select-String -Path src/WildBunch.Domain/Game/GameSession.cs -Pattern "private void Apply" -Context 0,8
Select-String -Path src/WildBunch.Domain/Game/GameSession.cs -Pattern "ApplyProducedEvent" -Context 0,40
Select-String -Path src/WildBunch.Domain/Game/GameSessionEventReplay.cs -Pattern "ApplyEvent" -Context 0,40
```

Note: existing Apply methods use `private void Apply(EventType e)`, increment `_version++`, and set fields directly. The `ApplyProducedEvent` dispatch and `ApplyEvent` dispatch mirror each other.

- [ ] **Step 2: Add 6 cases to `ApplyProducedEvent` in `GameSession.cs`**

```csharp
case JourneyStarted e:             Apply(e); break;
case TravelDayAdvanced e:          Apply(e); break;
case TrailEventApplied e:          Apply(e); break;
case JourneyEncounterResolved e:   Apply(e); break;
case JourneyCompleted e:           Apply(e); break;
case JourneyArrivalAcknowledged e: Apply(e); break;
```

- [ ] **Step 3: Implement 6 Apply methods in `GameSession.cs`**

```csharp
private void Apply(JourneyStarted e)
{
    _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
    _nextJourneySequence = e.JourneySnapshot.JourneySequence + 1;
    _travelDiaryDays.Clear();
    RecordTravelUpdate(e.DiaryMessage);
    _version++;
}

private void Apply(TravelDayAdvanced e)
{
    _clock.Set(e.Day, turn: 0);  // ABSOLUTE — clock from event
    _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);  // ABSOLUTE — journey from snapshot
    _player.ApplyHealthDelta(e.HealthDelta);  // ADDITIVE — delta to player
    _pursuitState.ApplyHeatDelta(e.PursuitHeatDelta);  // ADDITIVE — delta to pursuit
    RecordTravelUpdate(e.DiaryMessage);
    if (!string.IsNullOrEmpty(e.HorseLostMessage))
        RecordTravelUpdate(e.HorseLostMessage);
    _version++;
}

private void Apply(TrailEventApplied e)
{
    _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);  // ABSOLUTE — journey from snapshot
    _player.AdjustWallet(e.WalletDelta);  // ADDITIVE
    _player.AdjustFood(e.FoodDelta);  // ADDITIVE
    _player.AdjustCanteenCharges(e.CanteenChargeDelta);  // ADDITIVE
    _pursuitState.ApplyHeatDelta(e.HeatIncrease);  // ADDITIVE
    RecordTravelUpdate(e.DiaryMessage);
    if (!string.IsNullOrEmpty(e.HorseLostMessage))
        RecordTravelUpdate(e.HorseLostMessage);
    _version++;
}

private void Apply(JourneyEncounterResolved e)
{
    _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);  // ABSOLUTE — journey from snapshot
    _player.ApplyHealthDelta(e.HealthDelta);  // ADDITIVE
    _player.AdjustWallet(e.WalletDelta);  // ADDITIVE
    if (e.AmmoSpent > 0) _player.SpendAmmo(e.AmmoSpent);  // ADDITIVE
    if (e.StolenItemKind is { } kind)
        _player.RemoveItem(kind, e.StolenItemQuantity);  // ADDITIVE
    _pursuitState.ApplyHeatDelta(e.PursuitHeatDelta);  // ADDITIVE
    RecordTravelUpdate(e.DiaryMessage);
    _version++;
}

private void Apply(JourneyCompleted e)
{
    _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);  // ABSOLUTE — journey from snapshot
    _currentTown = e.DestinationTownId;  // ABSOLUTE — town from event
    _player.TravelTo(e.DestinationTownId);  // ABSOLUTE — player town
    _player.RefillCanteen();  // Side effect of arrival
    RecordTravelUpdate(e.DiaryMessage);
    _version++;
}

private void Apply(JourneyArrivalAcknowledged e)
{
    if (_journey is not null)
        _completedJourneyHistory.Add(_journey);
    _journey = null;
    RecordTravelUpdate(e.DiaryMessage);
    _version++;
}
```

Note: Verify all private field names (`_journey`, `_player`, `_pursuitState`, `_clock`, `_currentTown`, `_completedJourneyHistory`, `_nextJourneySequence`, `_travelDiaryDays`, `_version`) against actual `GameSession` source. Verify method names on player/pursuit (`ApplyHealthDelta`, `AdjustWallet`, `AdjustFood`, `AdjustCanteenCharges`, `SpendAmmo`, `RemoveItem`, `ApplyHeatDelta`, `TravelTo`, `RefillCanteen`). `RecordTravelUpdate` was added in Task 4.

- [ ] **Step 4: Add 6 cases to `GameSessionEventReplay.ApplyEvent`**

```csharp
case JourneyStarted e:             session.Apply(e); break;
case TravelDayAdvanced e:          session.Apply(e); break;
case TrailEventApplied e:          session.Apply(e); break;
case JourneyEncounterResolved e:   session.Apply(e); break;
case JourneyCompleted e:           session.Apply(e); break;
case JourneyArrivalAcknowledged e: session.Apply(e); break;
```

Apply methods must be `internal` (or accessible from `GameSessionEventReplay`). Check existing access modifier pattern.

- [ ] **Step 5: Build**

Run: `dotnet build src/WildBunch.Domain`
Expected: Build succeeds (`RecordTravelUpdate` was added in Task 4).

- [ ] **Step 6: Run Apply + replay tests — expect GREEN**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelEventApply|TravelReplayEquality"`
Expected: All tests PASS.

- [ ] **Step 7: Run characterization tests — expect GREEN**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelStateMachine|TravelEncounterResolution|TravelResourceTracking|TravelDiary"`
Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs
git add src/WildBunch.Domain/Game/GameSessionEventReplay.cs
git add tests/WildBunch.Domain.Tests/TravelEventApplyTests.cs
git add tests/WildBunch.Domain.Tests/TravelReplayEqualityTests.cs
git commit -m "BUNCH-83: implement 6 Apply methods + replay dispatch + replay-equality tests"
```

---

## Task 6: Migrate `StartJourney` to ProduceEvent

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`

- [ ] **Step 1: Read `StartJourney` in full**

```powershell
Select-String -Path src/WildBunch.Domain/Game/GameSession.cs -Pattern "public.*StartJourney" -Context 0,30
```

- [ ] **Step 2: Refactor `StartJourney`**

Keep all validation and computation unchanged. Replace direct mutations + `AddLogEntry` with:

```csharp
var snapshot = newJourney.ToSnapshot(TravelRules);
ProduceEvent(new JourneyStarted(snapshot, openingNarration));
```

`Apply(JourneyStarted)` handles: `_journey = TravelJourney.FromSnapshot(...)`, `_nextJourneySequence++`, `_travelDiaryDays.Clear()`, `RecordTravelUpdate(...)`.

Note: `TravelJourney.ToSnapshot()` takes a `TravelRulesProfile` parameter — check the actual signature and pass the correct argument (likely `TravelRules` property on `GameSession`).

- [ ] **Step 3: Build and run tests**

Run: `dotnet build src/WildBunch.Domain`
Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelStateMachine|TravelEventApply|TravelReplayEquality"`
Run: `dotnet test tests/WildBunch.Application.Tests --filter "TravelToTownHandler"`
Expected: ALL PASS.

- [ ] **Step 4: Commit**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-83: migrate StartJourney to ProduceEvent(JourneyStarted)"
```

---

## Task 7: Migrate `AdvanceJourneyDay` path to ProduceEvent

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`

This is the largest migration. Methods: `AdvanceJourneyDayDeterministic`, `PrepareTravelDayAdvance`, `HandleInterruptedTravelDay`, `HandleCompletedTravelDay`, `HandleOngoingTravelDay`, `ApplyTrailEvent`.

- [ ] **Step 1: Read all 6 methods in full**

Read each method to understand the flow and identify all direct mutations and `AddLogEntry` call sites.

- [ ] **Step 2: Remove `Clock.AdvanceTravelDay()` from `PrepareTravelDayAdvance`**

The clock will advance via `Apply(TravelDayAdvanced)`. Compute the new day value as `_clock.Day + 1` and carry it in the `TravelDayAdvanced` event.

- [ ] **Step 3: Migrate `ApplyTrailEvent` to produce `TrailEventApplied`**

Compute all deltas. Build post-trail-event snapshot via `_journey.ToSnapshot(TravelRules)`. Replace direct mutations + `AddLogEntry` with:
```csharp
ProduceEvent(new TrailEventApplied(snapshot, kind, id, walletDelta, foodDelta, canteenDelta,
    horseHungerDelta, horseThirstDelta, horseExhaustionDelta, delayDays, heatIncrease,
    travelModeChangedTo, diaryMessage, horseLostMessage));
```

`Apply(TrailEventApplied)` handles: `_journey = FromSnapshot(e.JourneySnapshot)` (ABSOLUTE), player deltas (ADDITIVE), pursuit heat (ADDITIVE).

- [ ] **Step 4: Migrate `HandleInterruptedTravelDay`**

```csharp
var snapshot = _journey.ToSnapshot(TravelRules);
ProduceEvent(new TravelDayAdvanced(newDay, snapshot, healthDelta, heatDelta,
    TravelDayOutcome.Interrupted, diaryMessage, horseLostMessage));
```

- [ ] **Step 5: Migrate `HandleCompletedTravelDay`**

```csharp
var completedSnapshot = _journey.ToSnapshot(TravelRules);
ProduceEvent(new TravelDayAdvanced(newDay, completedSnapshot, healthDelta, heatDelta,
    TravelDayOutcome.Completed, diaryMessage, horseLostMessage));
ProduceEvent(new JourneyCompleted(destTownId, destTownName, completedSnapshot, arrivalMessage));
```

- [ ] **Step 6: Migrate `HandleOngoingTravelDay`**

```csharp
var snapshot = _journey.ToSnapshot(TravelRules);
ProduceEvent(new TravelDayAdvanced(newDay, snapshot, healthDelta, heatDelta,
    TravelDayOutcome.Ongoing, diaryMessage, ""));
```

- [ ] **Step 7: Build and run tests**

Run: `dotnet build src/WildBunch.Domain`
Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelStateMachine|TravelEventApply|TravelReplayEquality|TravelResourceTracking"`
Run: `dotnet test tests/WildBunch.Application.Tests --filter "AdvanceTravelDayHandler"`
Expected: ALL PASS.

- [ ] **Step 8: Commit**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-83: migrate AdvanceJourneyDay path to ProduceEvent events"
```

---

## Task 8: Migrate `ResolveJourneyEncounter` path to ProduceEvent

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`

Methods: `ResolveJourneyEncounterDeterministic`, `ContinueCurrentDayAfterEncounterResolution`.

- [ ] **Step 1: Read both methods in full**

- [ ] **Step 2: Migrate run/fight/bribe branches to produce `JourneyEncounterResolved`**

For each branch:
1. Compute all deltas (keep existing logic)
2. Build post-resolution snapshot via `_journey.ToSnapshot(TravelRules)`
3. `ProduceEvent(new JourneyEncounterResolved(...))`
4. Remove direct mutations and `AddLogEntry` call sites

For failed attempts (resolved=false), still produce the event — hidden state changes must be carried in the snapshot for replay correctness.

- [ ] **Step 3: Migrate `ContinueCurrentDayAfterEncounterResolution`**

- Trail event during continuation: `ApplyTrailEvent` already migrated in Task 7
- Journey completes during continuation: produce `JourneyCompleted`
- Remove direct `AddLogEntry` call sites

- [ ] **Step 4: Build and run tests**

Run: `dotnet build src/WildBunch.Domain`
Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelStateMachine|TravelEncounterResolution|TravelEventApply|TravelReplayEquality"`
Run: `dotnet test tests/WildBunch.Application.Tests --filter "ResolveJourneyEncounterHandler"`
Expected: ALL PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-83: migrate ResolveJourneyEncounter path to ProduceEvent events"
```

---

## Task 9: Migrate `AcknowledgeJourneyArrival` + update guardrail count + verify GREEN

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs`

- [ ] **Step 1: Read `AcknowledgeJourneyArrival` in full**

- [ ] **Step 2: Migrate to `ProduceEvent(JourneyArrivalAcknowledged)`**

```csharp
var snapshot = _journey.ToSnapshot(TravelRules);
ProduceEvent(new JourneyArrivalAcknowledged(_journey.JourneySequence, snapshot, diaryMessage));
```

`Apply(JourneyArrivalAcknowledged)` handles archival + clearing `_journey`.

- [ ] **Step 3: Verify 0 direct `AddLogEntry(Travel, ...)` call sites remain**

```powershell
Select-String -Path src/WildBunch.Domain/Game/GameSession.cs -Pattern "AddLogEntry.*Travel"
```

Expected: Only the `RecordTravelUpdate` definition (1 line). All 13 direct call sites gone.

- [ ] **Step 4: Update guardrail test constant**

Read the guardrail test:
```powershell
Get-Content tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs
```

The test uses `Assert.True(matches.Count <= KnownLegacyAddLogEntryCallSiteCount)` — upper bound. The constant `KnownLegacyAddLogEntryCallSiteCount = 19` includes the method definition itself.

New count: 19 - 13 (removed travel calls) + 1 (RecordTravelUpdate's call to AddLogEntry) = 7.

Change `KnownLegacyAddLogEntryCallSiteCount` from `19` to `7`.

- [ ] **Step 5: Run all travel tests and guardrail test**

Run: `dotnet build`
Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelStateMachine|TravelEncounterResolution|TravelResourceTracking|TravelDiary|TravelEventApply|TravelReplayEquality"`
Run: `dotnet test tests/WildBunch.Application.Tests --filter "Travel|Journey|AddLogEntryGuardrail"`
Expected: ALL PASS including `AddLogEntryGuardrailTests` (GREEN at count=7).

- [ ] **Step 6: Run full test suite**

Run: `dotnet test`
Expected: No regressions.

- [ ] **Step 7: Commit**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs
git add tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs
git commit -m "BUNCH-83: migrate AcknowledgeJourneyArrival to ProduceEvent; update guardrail count to 7"
```

---

## Phase 2 Completion Checklist

- [ ] `TravelDayOutcome` enum exists
- [ ] 6 event type files exist in `src/WildBunch.Domain/Events/`
- [ ] **No new `TravelJourneySnapshot` created** — existing type reused
- [ ] **No new `FromSnapshot`/`ToSnapshot` created** — existing methods reused
- [ ] **No new `Clock.Set` created** — existing method reused
- [ ] 6 `Apply` methods on `GameSession` (journey=absolute, player/pursuit=additive)
- [ ] 6 cases in `ApplyProducedEvent` dispatch
- [ ] 6 cases in `GameSessionEventReplay.ApplyEvent` dispatch
- [ ] `RecordTravelUpdate` helper present; 0 direct `AddLogEntry(Travel)` in travel methods
- [ ] `AddLogEntryGuardrailTests` GREEN at count=7
- [ ] All Phase 1 characterization tests GREEN
- [ ] Replay-equality tests GREEN (command-path == replay-path for exact fields)
- [ ] All existing handler tests GREEN
- [ ] `dotnet build` clean
- [ ] `dotnet test` no regressions
