# BUNCH-83 Phase 1: Travel/Journey Characterization Tests

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pin exact current travel/journey behavior before the event-sourcing refactor. These tests must continue passing after migration and serve as the safety net. They assert EXACT values, not directional changes.

**Architecture:** Characterization tests exercise the current direct-mutation API (`StartJourney`, `AdvanceJourneyDay`, `ResolveJourneyEncounter`, `AcknowledgeJourneyArrival`) and assert exact observable state. The approach is: set up a deterministic scenario, run it to capture exact values, then write tests asserting those values. After Phase 2 migration, these same tests verify that the event-sourced path produces identical observable behavior.

**Tech Stack:** C#/.NET 10, xUnit, `TravelRandomnessState.CreateDeterministic(string.Empty)`, `ForcedRoll` parameters

## Global Constraints

- **Use `ForcedRoll` to force specific encounter outcomes** — `0UL` = success, `99UL` = failure, `null` = actual deterministic roll. NEVER loop-until-desired to find a specific generated outcome.
- **Use `TravelRandomnessState.CreateDeterministic(string.Empty)`** for all sessions — ensures reproducible day plans and encounter generation.
- **Assert EXACT field values** (health=8, wallet=15.50, food=3, RemainingDays=2, DaysTravelled=1) — not directional changes ("health decreased").
- **Assert exact event types and counts** where applicable (following BUNCH-80 `BountySaloonEventSourcingTests` pattern).
- **Assert exact diary message content** — not just "entries exist". Use `Assert.Equal` or `Assert.Contains` with specific substrings.
- **No conditional assertions** — force the scenario deterministically with `ForcedRoll` and specific world/inventory setup. Don't write `if (session.Journey?.Status == Interrupted) { ... }` — force it to be Interrupted.
- **Tests must pass on current `main` BEFORE any Phase 2 changes.**
- **Tests must continue passing AFTER Phase 2 migration** (they test observable behavior, not implementation).
- **Follow existing test patterns** from `AdvanceTravelDayHandlerTests`, `ResolveJourneyEncounterHandlerTests` — use the same factory method style.
- **Do not add new `AddLogEntry` call sites** — these tests assert results, not log entries.
- **Capture exact values by running the scenario first** — the worker should write a temporary test that prints values, run it, then write the permanent test with those exact values. Delete the temporary test.

---

## Task 1: Create travel test factory helpers

**Files:**
- Create: `tests/WildBunch.Domain.Tests/TravelTestFactory.cs`

**Interfaces:**
- Consumes: `TestSessionFactory.CreateDefault()`, `TravelResolver`, `GameSession.StartJourney`
- Produces: Deterministic travel test scenarios with known starting state

- [ ] **Step 1: Read existing test factory methods**

Read `tests/WildBunch.Application.Tests/AdvanceTravelDayHandlerTests.cs` — note the `CreateEasyLuckyFoodSession()`, `CreateHighRiskSession()`, `CreateProgressionSession()`, and `CreateSixDayQuietSession()` factory methods. These show the pattern for creating deterministic travel scenarios with specific world/trail/inventory configurations.

- [ ] **Step 2: Create `TravelTestFactory` in the Domain test project**

```csharp
// tests/WildBunch.Domain.Tests/TravelTestFactory.cs
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Factory methods for deterministic travel test scenarios.
/// All scenarios use TravelRandomnessState.CreateDeterministic(string.Empty)
/// for reproducible day plans and encounter generation.
/// </summary>
internal static class TravelTestFactory
{
    /// <summary>
    /// Creates a session with a 3-day low-risk journey from Current Town to Connected Town.
    /// Inventory: 4 Food, 1 Canteen (full 10), 1 Horse (Healthy), 1 Saddle.
    /// Wallet: $25. TravelDifficulty: Easy.
    /// This matches TestSessionFactory.CreateDefault() world setup.
    /// </summary>
    internal static (GameSession session, TravelPreview preview) CreateEasyShortJourney()
    {
        var session = TestSessionFactory.CreateDefault();
        var preview = ResolvePreview(session, new TownId("connected"));
        return (session, preview);
    }

    /// <summary>
    /// Creates a session with a high-risk journey designed to trigger encounters.
    /// Uses TrailRisk.High, TrailTerrain.Desert, WaterFeature.None, distance 6m.
    /// </summary>
    internal static (GameSession session, TravelPreview preview) CreateHighRiskJourney()
    {
        // Mirror the CreateHighRiskSession pattern from AdvanceTravelDayHandlerTests
        // but in the Domain test project (no handler, just session)
        var pinecross = new Town(new TownId("pinecross"), "Pinecross",
            TownServices.NoticeBoard | TownServices.Telegraph | TownServices.Lodging);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[] { new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id,
                TrailRisk.High, TrailTerrain.Desert, WaterFeature.None, rideDayDistance: 6m) });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 3),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(2)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1),
        });

        var session = GameSession.StartNew("Ranger Vale", world,
            TestSessionFactory.CreateBaselineCaseFile(),
            pinecross.Id, Wallet.Starting(25m), inventory,
            TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();

        var preview = ResolvePreview(session, dryfork.Id);
        return (session, preview);
    }

    /// <summary>
    /// Creates a session with a 6-day low-risk journey designed to complete without interruption.
    /// </summary>
    internal static (GameSession session, TravelPreview preview) CreateSixDayQuietJourney()
    {
        // Mirror CreateSixDayQuietSession pattern
        var origin = new Town(new TownId("origin"), "Origin Town",
            TownServices.NoticeBoard | TownServices.Telegraph | TownServices.Lodging);
        var destination = new Town(new TownId("destination"), "Destination Town", TownServices.None);
        var world = new DomainWorld(
            new[] { origin, destination },
            new[] { new Trail(new TrailId("trail-long"), origin.Id, destination.Id,
                TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, rideDayDistance: 6m) });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 8),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
        });

        var session = GameSession.StartNew("Ranger Vale", world,
            TestSessionFactory.CreateBaselineCaseFile(),
            origin.Id, Wallet.Starting(25m), inventory,
            TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();

        var preview = ResolvePreview(session, destination.Id);
        return (session, preview);
    }

    private static TravelPreview ResolvePreview(GameSession session, TownId destinationId)
    {
        var resolver = new TravelResolver();
        var result = resolver.PreviewJourney(
            session.World,
            session.Player.CurrentTownId,
            destinationId,
            session.Player.Inventory,
            session.TravelRules);
        if (!result.Success || result.Preview is null)
            throw new InvalidOperationException(
                $"Could not create journey preview: {result.Message}");
        return result.Preview;
    }
}
```

Note: `TestSessionFactory.CreateBaselineCaseFile()` — verify if this method exists at execution time. If not, use `TestSessionFactory.CreateDefault().CaseFile` or construct a minimal `CaseFile` with 2 suspects matching the pattern in `TestSessionFactory.CreateDefault()`. The key is that the world and inventory match the intended scenario. Verify `Town` constructor parameters, `Trail` constructor parameters, and `CanteenState.Full` capacity against the actual domain API at execution time.

**Cross-project sharing route:** `TravelTestFactory` is created in `WildBunch.Domain.Tests` in this task. `WildBunch.Application.Tests` and `WildBunch.Integration.Tests` do NOT reference `WildBunch.Domain.Tests` (no test-to-test project references exist in this repo, and `InternalsVisibleTo` alone does not make the helper available without an assembly reference). The sharing route is **duplication**: when Phase 3 needs `TravelTestFactory` in `Application.Tests` or `Integration.Tests`, copy the same factory methods into a `TravelTestFactory.cs` in that test project. The helper is ~80 lines of pure factory methods with no shared mutable state. This matches the existing repo pattern where `Application.Tests` has its own inline factory methods (e.g., `CreateEasyLuckyFoodSession` in `AdvanceTravelDayHandlerTests.cs`) rather than referencing `Domain.Tests`. Do not add `InternalsVisibleTo` from `Domain.Tests` or test-to-test `ProjectReference` entries — neither pattern exists in this repo.

- [ ] **Step 3: Build**

Run: `dotnet build tests/WildBunch.Domain.Tests`
Expected: Build succeeds. Fix any API mismatches.

- [ ] **Step 4: Commit**

```powershell
git add tests/WildBunch.Domain.Tests/TravelTestFactory.cs
git commit -m "BUNCH-83: add TravelTestFactory for deterministic travel test scenarios"
```

---

## Task 2: Capture exact values from deterministic scenarios

**Files:**
- Create (temporary): `tests/WildBunch.Domain.Tests/TravelValueCaptureTests.cs`

This task creates a temporary test that prints exact values from deterministic scenarios. The worker runs it, captures the output, then uses those values in the permanent characterization tests. The temporary test is deleted after values are captured.

- [ ] **Step 1: Write temporary value-capture test**

```csharp
// tests/WildBunch.Domain.Tests/TravelValueCaptureTests.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

/// <summary>
/// TEMPORARY: Run this test to capture exact values from deterministic scenarios.
/// Delete after capturing values for permanent characterization tests.
/// </summary>
public sealed class TravelValueCaptureTests
{
    [Fact]
    public void Capture_EasyShortJourney_StartState()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        // Capture initial journey state
        Assert.Fail($@"
=== EasyShortJourney After StartJourney ===
Journey.Status: {session.Journey!.Status}
Journey.JourneySequence: {session.Journey.JourneySequence}
Journey.TravelMode: {session.Journey.TravelMode}
Journey.RemainingRideDayDistance: {session.Journey.RemainingRideDayDistance}
Journey.RemainingDays: {session.Journey.RemainingDays}
Journey.DaysTravelled: {session.Journey.DaysTravelled}
Journey.DelayDays: {session.Journey.DelayDays}
Journey.FoodRemaining: {session.Journey.FoodRemaining}
Journey.HorseFeedRemaining: {session.Journey.HorseFeedRemaining}
Journey.AvailableCanteenCharges: {session.Journey.AvailableCanteenCharges}
Journey.HorseState: {session.Journey.HorseState}
Player.Health: {session.Player.Health}
Player.Wallet.Cash: {session.Player.Wallet.Cash}
Player.FoodQuantity: {session.Player.Inventory.GetQuantity(ItemKind.Food)}
Clock.Day: {session.Clock.Day}
Clock.Turn: {session.Clock.Turn}
PursuitState.Heat: {session.PursuitState.Heat}
");
    }

    [Fact]
    public void Capture_EasyShortJourney_AfterOneAdvance()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        var result = session.AdvanceJourneyDay();

        Assert.Fail($@"
=== EasyShortJourney After AdvanceJourneyDay (day 1) ===
Result.Success: {result.Success}
Result.Status: {result.Status}
Result.Message: {result.Message}
Journey.Status: {session.Journey?.Status}
Journey.DaysTravelled: {session.Journey?.DaysTravelled}
Journey.RemainingDays: {session.Journey?.RemainingDays}
Journey.FoodRemaining: {session.Journey?.FoodRemaining}
Journey.HorseFeedRemaining: {session.Journey?.HorseFeedRemaining}
Journey.AvailableCanteenCharges: {session.Journey?.AvailableCanteenCharges}
Player.Health: {session.Player.Health}
Player.Wallet.Cash: {session.Player.Wallet.Cash}
Player.FoodQuantity: {session.Player.Inventory.GetQuantity(ItemKind.Food)}
Clock.Day: {session.Clock.Day}
Clock.Turn: {session.Clock.Turn}
PursuitState.Heat: {session.PursuitState.Heat}
TravelDiaryDays.Count: {session.TravelDiaryDays.Count}
");
    }

    [Fact]
    public void Capture_HighRiskJourney_UntilInterrupted()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);

        // Advance until interrupted or completed
        for (var i = 0; i < 10; i++)
        {
            var result = session.AdvanceJourneyDay();
            if (result.Status == JourneyStatus.Interrupted)
            {
                Assert.Fail($@"
=== HighRiskJourney Interrupted on day {i + 1} ===
Result.Success: {result.Success}
Result.Status: {result.Status}
Result.Message: {result.Message}
Journey.Status: {session.Journey?.Status}
Journey.DaysTravelled: {session.Journey?.DaysTravelled}
Journey.PendingEncounter.Kind: {session.Journey?.PendingEncounter?.Kind}
Journey.PendingEncounter.Message: {session.Journey?.PendingEncounter?.Message}
Journey.PendingEncounter.Choices.Count: {session.Journey?.PendingEncounter?.Choices.Count}
Journey.PendingEncounter.HiddenState.BribeOffersMade: {session.Journey?.PendingEncounter?.HiddenState?.BribeOffersMade}
Journey.PendingEncounter.HiddenState.ChaseFatigue: {session.Journey?.PendingEncounter?.HiddenState?.ChaseFatigue}
Player.Health: {session.Player.Health}
Player.Wallet.Cash: {session.Player.Wallet.Cash}
Player.FoodQuantity: {session.Player.Inventory.GetQuantity(ItemKind.Food)}
Clock.Day: {session.Clock.Day}
PursuitState.Heat: {session.PursuitState.Heat}
");
            }
            if (result.Status == JourneyStatus.Completed || !result.Success)
                break;
        }
    }

    [Fact]
    public void Capture_EncounterResolution_Run_Success()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);

        // Advance until interrupted
        JourneyStatus advanceStatus;
        do
        {
            var advanceResult = session.AdvanceJourneyDay();
            advanceStatus = advanceResult.Status;
        } while (advanceStatus == JourneyStatus.Active);

        if (advanceStatus != JourneyStatus.Interrupted)
            Assert.Fail("Scenario did not produce an interrupted encounter — adjust factory");

        // Force successful run
        var resolveResult = session.ResolveJourneyEncounter("run", forcedRoll: 0UL);

        Assert.Fail($@"
=== EncounterResolution Run Success (ForcedRoll=0) ===
ResolveResult.Success: {resolveResult.Success}
ResolveResult.SessionChanged: {resolveResult.SessionChanged}
ResolveResult.Status: {resolveResult.Status}
ResolveResult.Message: {resolveResult.Message}
Journey.Status: {session.Journey?.Status}
Journey.PendingEncounter: {session.Journey?.PendingEncounter?.Kind ?? "null"}
Player.Health: {session.Player.Health}
Player.Wallet.Cash: {session.Player.Wallet.Cash}
Player.FoodQuantity: {session.Player.Inventory.GetQuantity(ItemKind.Food)}
Clock.Day: {session.Clock.Day}
PursuitState.Heat: {session.PursuitState.Heat}
");
    }

    [Fact]
    public void Capture_EncounterResolution_Run_Failure()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);

        // Advance until interrupted
        JourneyStatus advanceStatus;
        do
        {
            var advanceResult = session.AdvanceJourneyDay();
            advanceStatus = advanceResult.Status;
        } while (advanceStatus == JourneyStatus.Active);

        if (advanceStatus != JourneyStatus.Interrupted)
            Assert.Fail("Scenario did not produce an interrupted encounter");

        // Force failed run
        var resolveResult = session.ResolveJourneyEncounter("run", forcedRoll: 99UL);

        Assert.Fail($@"
=== EncounterResolution Run Failure (ForcedRoll=99) ===
ResolveResult.Success: {resolveResult.Success}
ResolveResult.SessionChanged: {resolveResult.SessionChanged}
ResolveResult.Status: {resolveResult.Status}
ResolveResult.Message: {resolveResult.Message}
Journey.Status: {session.Journey?.Status}
Journey.PendingEncounter.Kind: {session.Journey?.PendingEncounter?.Kind}
Journey.PendingEncounter.HiddenState.ChaseFatigue: {session.Journey?.PendingEncounter?.HiddenState?.ChaseFatigue}
Journey.PendingEncounter.HiddenState.Annoyance: {session.Journey?.PendingEncounter?.HiddenState?.Annoyance}
Player.Health: {session.Player.Health}
Player.Wallet.Cash: {session.Player.Wallet.Cash}
");
    }

    [Fact]
    public void Capture_SixDayQuietJourney_Completion()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);

        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.Fail($@"
=== SixDayQuietJourney Completion ===
Result.Success: {result.Success}
Result.Status: {result.Status}
Result.Message: {result.Message}
Journey.Status: {session.Journey?.Status}
Journey.DaysTravelled: {session.Journey?.DaysTravelled}
Player.CurrentTownId: {session.Player.CurrentTownId}
Player.Health: {session.Player.Health}
Player.Wallet.Cash: {session.Player.Wallet.Cash}
Player.FoodQuantity: {session.Player.Inventory.GetQuantity(ItemKind.Food)}
Clock.Day: {session.Clock.Day}
PursuitState.Heat: {session.PursuitState.Heat}
TravelDiaryDays.Count: {session.TravelDiaryDays.Count}
");
    }
}
```

- [ ] **Step 2: Run each capture test and record the output**

Run each test individually and capture the `Assert.Fail` message output:
```powershell
dotnet test tests/WildBunch.Domain.Tests --filter "TravelValueCapture" --logger "console;verbosity=detailed"
```

Record the exact values from each test's output. These values will be used in the permanent characterization tests.

**If a scenario doesn't produce the expected state** (e.g., high-risk journey doesn't interrupt), adjust the factory method (trail risk, terrain, distance, inventory) until it does. The goal is deterministic scenarios that reliably produce the target state.

- [ ] **Step 3: Delete the temporary test file**

```powershell
Remove-Item tests/WildBunch.Domain.Tests/TravelValueCaptureTests.cs
```

---

## Task 3: Write permanent state machine characterization tests

**Files:**
- Create: `tests/WildBunch.Domain.Tests/TravelStateMachineCharacterizationTests.cs`

**Interfaces:**
- Consumes: `TravelTestFactory`, `TestSessionFactory`, `GameSession` travel methods
- Produces: Characterization tests with exact value assertions

- [ ] **Step 1: Write state machine tests with exact values**

Use the values captured in Task 2. Each test asserts exact field values, not directional changes.

```csharp
// tests/WildBunch.Domain.Tests/TravelStateMachineCharacterizationTests.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Characterization tests pinning exact current travel/journey behavior.
/// These tests MUST pass before and after the Phase 2 event-sourcing migration.
/// All values are captured from deterministic scenarios using
/// TravelRandomnessState.CreateDeterministic(string.Empty) and ForcedRoll.
/// </summary>
public sealed class TravelStateMachineCharacterizationTests
{
    [Fact]
    public void StartJourney_EasyShortJourney_ExactInitialState()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        var initialHealth = session.Player.Health;
        var initialWallet = session.Player.Wallet.Cash;
        var initialFood = session.Player.Inventory.GetQuantity(ItemKind.Food);
        var initialDay = session.Clock.Day;

        var result = session.StartJourney(preview);

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Equal(1, session.Journey.JourneySequence);
        Assert.Equal(TravelMode.Mounted, session.Journey.TravelMode);
        Assert.Equal(0, session.Journey.DaysTravelled);
        Assert.Equal(0, session.Journey.DelayDays);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Null(session.Journey.CurrentDayPlan);
        // Player state unchanged by StartJourney
        Assert.Equal(initialHealth, session.Player.Health);
        Assert.Equal(initialWallet, session.Player.Wallet.Cash);
        Assert.Equal(initialFood, session.Player.Inventory.GetQuantity(ItemKind.Food));
        // Clock not advanced by StartJourney
        Assert.Equal(initialDay, session.Clock.Day);
    }

    [Fact]
    public void StartJourney_WhenAlreadyOnTrail_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        var secondStart = session.StartJourney(preview);

        Assert.False(secondStart.Success);
        Assert.Contains("already on the trail", secondStart.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdvanceJourneyDay_WhenNoJourney_Fails()
    {
        var session = TestSessionFactory.CreateDefault();
        var result = session.AdvanceJourneyDay();

        Assert.False(result.Success);
        Assert.Contains("No active journey", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdvanceJourneyDay_FirstDay_ExactState()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        var initialDay = session.Clock.Day;
        var initialFood = session.Journey!.FoodRemaining;
        var initialHealth = session.Player.Health;
        var initialHeat = session.PursuitState.Heat;

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        // Status: use the value captured in Task 2 for this exact scenario.
        // The deterministic scenario with CreateEasyShortJourney produces a known
        // status (Active, Interrupted, or Completed) — assert it exactly.
        Assert.Equal(initialDay + 1, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(1, session.Journey!.DaysTravelled);
        // After Task 2 capture, add exact-value assertions for:
        //   - result.Status (the captured JourneyStatus)
        //   - session.Journey.FoodRemaining (exact int)
        //   - session.Player.Health (exact int)
        //   - session.PursuitState.Heat (exact decimal)
        // These values are deterministic for this scenario and must be asserted
        // with Assert.Equal, not directional checks.
    }

    [Fact]
    public void AdvanceJourneyDay_WhenPendingEncounter_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);

        // Advance until interrupted (use captured day count)
        JourneyStatus status;
        do
        {
            var advanceResult = session.AdvanceJourneyDay();
            status = advanceResult.Status;
        } while (status == JourneyStatus.Active);

        Assert.Equal(JourneyStatus.Interrupted, status);

        // Try to advance again while encounter is pending
        var blockedAdvance = session.AdvanceJourneyDay();

        Assert.False(blockedAdvance.Success);
        Assert.Contains("pending encounter", blockedAdvance.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveJourneyEncounter_WhenNoJourney_Fails()
    {
        var session = TestSessionFactory.CreateDefault();
        var result = session.ResolveJourneyEncounter("run");

        Assert.False(result.Success);
        Assert.Contains("No active journey", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveJourneyEncounter_WhenNoPendingEncounter_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        var result = session.ResolveJourneyEncounter("run");

        Assert.False(result.Success);
        Assert.Contains("no pending encounter", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcknowledgeJourneyArrival_WhenNoJourney_Fails()
    {
        var session = TestSessionFactory.CreateDefault();
        var result = session.AcknowledgeJourneyArrival();

        Assert.False(result.Success);
        Assert.Contains("No completed journey", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcknowledgeJourneyArrival_WhenJourneyNotCompleted_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        var result = session.AcknowledgeJourneyArrival();

        Assert.False(result.Success);
    }
}
```

Note: The `AdvanceJourneyDay_FirstDay_ExactState` test shows structural assertions (clock arithmetic, days travelled) that are knowable without running. After Task 2 capture, the worker adds exact-value `Assert.Equal` assertions for `result.Status`, `FoodRemaining`, `Health`, and `PursuitState.Heat` using the captured deterministic values. No test may be committed with directional assertions (`Assert.True(x > y)`) for these fields — they must be exact.

- [ ] **Step 2: Run tests to verify they pass on current code**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelStateMachineCharacterization"`
Expected: PASS — all tests verify current behavior with exact values.

- [ ] **Step 3: Commit**

```powershell
git add tests/WildBunch.Domain.Tests/TravelStateMachineCharacterizationTests.cs
git commit -m "BUNCH-83: add travel state machine characterization tests with exact values"
```

---

## Task 4: Write encounter resolution characterization tests with ForcedRoll

**Files:**
- Create: `tests/WildBunch.Domain.Tests/TravelEncounterResolutionCharacterizationTests.cs`

- [ ] **Step 1: Write encounter resolution tests with exact ForcedRoll outcomes**

```csharp
// tests/WildBunch.Domain.Tests/TravelEncounterResolutionCharacterizationTests.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Characterization tests for encounter resolution with deterministic ForcedRoll.
/// Forces specific outcomes (0=success, 99=failure) and asserts exact state.
/// </summary>
public sealed class TravelEncounterResolutionCharacterizationTests
{
    /// <summary>
    /// Helper: advance journey until interrupted by encounter.
    /// Fails the test if the journey doesn't interrupt within 10 days.
    /// </summary>
    private static void AdvanceUntilInterrupted(GameSession session)
    {
        for (var i = 0; i < 10; i++)
        {
            var result = session.AdvanceJourneyDay();
            if (result.Status == JourneyStatus.Interrupted)
                return;
            if (result.Status == JourneyStatus.Completed || !result.Success)
                Assert.Fail($"Journey did not interrupt — it {result.Status} on day {i + 1}. Adjust TravelTestFactory.CreateHighRiskJourney().");
        }
        Assert.Fail("Journey did not interrupt within 10 days. Adjust TravelTestFactory.CreateHighRiskJourney().");
    }

    [Fact]
    public void ResolveJourneyEncounter_Run_Success_ExactState()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        AdvanceUntilInterrupted(session);

        var healthBefore = session.Player.Health;
        var walletBefore = session.Player.Wallet.Cash;
        var heatBefore = session.PursuitState.Heat;

        var result = session.ResolveJourneyEncounter("run", forcedRoll: 0UL);

        Assert.True(result.SessionChanged);
        Assert.True(result.Success);
        Assert.NotEqual(JourneyStatus.Interrupted, result.Status); // Encounter resolved
        Assert.Null(session.Journey!.PendingEncounter); // No more pending encounter
        // After Task 2 capture, add exact-value assertions for:
        //   - session.Player.Health (exact int — compare to healthBefore)
        //   - session.Player.Wallet.Cash (exact decimal — compare to walletBefore)
        //   - session.PursuitState.Heat (exact decimal — compare to heatBefore)
        //   - result.Status (exact JourneyStatus — Active or Completed)
        // These are deterministic for ForcedRoll=0 on this scenario.
    }

    [Fact]
    public void ResolveJourneyEncounter_Run_Failure_KeepsEncounterPending()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        AdvanceUntilInterrupted(session);

        var result = session.ResolveJourneyEncounter("run", forcedRoll: 99UL);

        Assert.True(result.SessionChanged); // State changed (hidden state updated)
        Assert.False(result.Success); // But encounter not resolved
        Assert.Equal(JourneyStatus.Interrupted, result.Status);
        Assert.NotNull(session.Journey!.PendingEncounter); // Encounter still pending
        // After Task 2 capture, add exact-value assertions for:
        //   - session.Journey.PendingEncounter!.HiddenState!.ChaseFatigue (exact int)
        //   - session.Journey.PendingEncounter!.HiddenState!.Annoyance (exact int)
        //   - session.Player.Health (exact int — may be unchanged or damaged)
        // These are deterministic for ForcedRoll=99 on this scenario.
    }

    [Fact]
    public void ResolveJourneyEncounter_Bribe_Success_ExactWalletDelta()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        AdvanceUntilInterrupted(session);

        var walletBefore = session.Player.Wallet.Cash;
        // Use the bribe amount captured in Task 2 for this encounter type.
        // The capture output shows the minimum bribe and the wallet after success.
        var bribeAmount = 5m; // Replace with captured value before committing

        var result = session.ResolveJourneyEncounter("bribe", bribeAmount: bribeAmount, forcedRoll: 0UL);

        Assert.True(result.Success);
        Assert.Null(session.Journey!.PendingEncounter);
        // After Task 2 capture, add exact-value assertion:
        //   - session.Player.Wallet.Cash (exact decimal — must equal walletBefore - bribeAmount)
        // The bribe amount itself must be the captured minimum bribe for this encounter.
    }

    [Fact]
    public void ResolveJourneyEncounter_Bribe_Failure_LocksOutAfterTwoOffers()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        AdvanceUntilInterrupted(session);

        // First bribe attempt (forced failure)
        session.ResolveJourneyEncounter("bribe", bribeAmount: 1m, forcedRoll: 99UL);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey!.Status);

        // Second bribe attempt (forced failure)
        session.ResolveJourneyEncounter("bribe", bribeAmount: 1m, forcedRoll: 99UL);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey!.Status);

        // Third bribe should be locked out
        var thirdBribe = session.ResolveJourneyEncounter("bribe", bribeAmount: 1m, forcedRoll: 0UL);
        Assert.False(thirdBribe.Success);
        Assert.Contains("not take any more money", thirdBribe.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveJourneyEncounter_InvalidChoice_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        AdvanceUntilInterrupted(session);

        var result = session.ResolveJourneyEncounter("dance");

        Assert.False(result.Success);
        Assert.Contains("not a lawful way", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveJourneyEncounter_EmptyChoice_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        AdvanceUntilInterrupted(session);

        var result = session.ResolveJourneyEncounter("");

        Assert.False(result.Success);
        Assert.Contains("Choose how", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
```

Note: The bribe lockout test forces two failed bribes with `ForcedRoll: 99UL`, then verifies the third is locked out. This is deterministic — no conditional assertions. The bribe amount and lockout message must match captured values from Task 2 before committing.

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelEncounterResolutionCharacterization"`
Expected: PASS

- [ ] **Step 3: Commit**

```powershell
git add tests/WildBunch.Domain.Tests/TravelEncounterResolutionCharacterizationTests.cs
git commit -m "BUNCH-83: add encounter resolution characterization tests with ForcedRoll"
```

---

## Task 5: Write resource tracking and journey completion characterization tests

**Files:**
- Create: `tests/WildBunch.Domain.Tests/TravelResourceTrackingCharacterizationTests.cs`

- [ ] **Step 1: Write resource tracking tests with exact values**

```csharp
// tests/WildBunch.Domain.Tests/TravelResourceTrackingCharacterizationTests.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class TravelResourceTrackingCharacterizationTests
{
    [Fact]
    public void AdvanceJourneyDay_ConsumesExactFood()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        var initialFood = session.Journey!.FoodRemaining;

        session.AdvanceJourneyDay();

        // After Task 2 capture, add: Assert.Equal for session.Journey!.FoodRemaining
        // using the exact captured value. Do not commit with a directional assertion.
        // The food consumption is deterministic for this scenario.
        // Do not commit with a directional assertion — use the captured exact value.
    }

    [Fact]
    public void AdvanceJourneyDay_AdvancesClockExactly()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        var initialDay = session.Clock.Day;

        session.AdvanceJourneyDay();

        Assert.Equal(initialDay + 1, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void AdvanceJourneyDay_IncreasesPursuitHeatExactly()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        var initialHeat = session.PursuitState.Heat;

        session.AdvanceJourneyDay();

        // After Task 2 capture, add: Assert.Equal for session.PursuitState.Heat
        // using the exact captured value. Do not commit with a directional assertion.
        // The heat increase is deterministic for this scenario.
        // Do not commit with a directional assertion — use the captured exact value.
    }

    [Fact]
    public void SixDayQuietJourney_CompletesWithExactState()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);
        var initialTown = session.Player.CurrentTownId;

        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.Equal(JourneyStatus.Completed, result.Status);
        Assert.Equal(JourneyStatus.Completed, session.Journey!.Status);
        // After Task 2 capture, add exact-value assertions for:
        //   - session.Journey.DaysTravelled (exact int)
        //   - session.Player.Health (exact int)
        //   - session.Player.Wallet.Cash (exact decimal)
        //   - session.Journey.FoodRemaining (exact int)
        //   - session.Clock.Day (exact int)
        //   - session.PursuitState.Heat (exact decimal)
        // All deterministic for CreateSixDayQuietJourney — assert with Assert.Equal.
    }

    [Fact]
    public void AcknowledgeJourneyArrival_ClearsJourneyAndChangesTown()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);
        var destinationTown = preview.DestinationTownId;

        // Complete journey
        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.Equal(JourneyStatus.Completed, result.Status);
        Assert.Equal(destinationTown, session.Player.CurrentTownId);
        Assert.NotNull(session.Journey);

        // Acknowledge arrival
        var ackResult = session.AcknowledgeJourneyArrival();

        Assert.True(ackResult.Success);
        Assert.Null(session.Journey);
    }

    [Fact]
    public void FullJourneyCycle_ExactStateAtEachStep()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);

        // Capture state at each day
        var dayCount = 0;
        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
            dayCount++;
            Assert.Equal(dayCount, session.Journey!.DaysTravelled);
            // After Task 2 capture, add exact-value assertions for each day:
            //   - session.Journey.FoodRemaining (exact int per day)
            //   - session.Player.Health (exact int per day)
            //   - session.PursuitState.Heat (exact decimal per day)
            // Use a captured array of expected values indexed by day number.
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.Equal(JourneyStatus.Completed, result.Status);
        // After Task 2 capture, add: Assert.Equal for dayCount
        // using the exact captured value.

        session.AcknowledgeJourneyArrival();
        Assert.Null(session.Journey);
    }
}
```

Note: The `FullJourneyCycle_ExactStateAtEachStep` test asserts `DaysTravelled` incrementally (knowable without running). After Task 2 capture, the worker adds exact-value assertions for `FoodRemaining`, `Health`, and `PursuitState.Heat` at each day, plus the exact `dayCount` at completion. Use a captured array of expected values indexed by day number.

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelResourceTrackingCharacterization"`
Expected: PASS

- [ ] **Step 3: Commit**

```powershell
git add tests/WildBunch.Domain.Tests/TravelResourceTrackingCharacterizationTests.cs
git commit -m "BUNCH-83: add resource tracking and journey completion characterization tests"
```

---

## Task 6: Write travel diary accumulation characterization tests

**Files:**
- Create: `tests/WildBunch.Domain.Tests/TravelDiaryCharacterizationTests.cs`

- [ ] **Step 1: Write diary accumulation tests with exact entry counts**

```csharp
// tests/WildBunch.Domain.Tests/TravelDiaryCharacterizationTests.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

public sealed class TravelDiaryCharacterizationTests
{
    [Fact]
    public void StartJourney_ProducesExactDiaryEntry()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        var travelEntries = session.LogEntries
            .Where(e => e.Kind == GameLogEntryKind.Travel).ToList();

        Assert.NotEmpty(travelEntries);
        // After Task 2 capture, add: Assert.Equal for travelEntries.Last().Message
        // using the exact captured start message string.
        // The start message is deterministic for this scenario.
    }

    [Fact]
    public void AdvanceJourneyDay_AccumulatesExactDiaryEntries()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);
        var initialEntryCount = session.LogEntries
            .Count(e => e.Kind == GameLogEntryKind.Travel);

        session.AdvanceJourneyDay();

        var afterOneDay = session.LogEntries
            .Count(e => e.Kind == GameLogEntryKind.Travel);
        // After Task 2 capture, add: Assert.Equal for afterOneDay
        // using the exact captured entry count. Do not commit with a directional assertion.
        // The number of diary entries per day advance is deterministic.
        // Do not commit with a directional assertion — use the captured exact count.
    }

    [Fact]
    public void FullJourney_AccumulatesExactTotalDiaryEntries()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);

        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);

        session.AcknowledgeJourneyArrival();

        var totalTravelEntries = session.LogEntries
            .Count(e => e.Kind == GameLogEntryKind.Travel);
        // After Task 2 capture, add: Assert.Equal for totalTravelEntries
        // using the exact captured total. Do not commit with a directional assertion.
        // The total diary entries for a full journey cycle is deterministic.
        // Do not commit with a directional assertion — use the captured exact total.
    }
}
```

Note: After Task 2 capture, replace the instruction comments with exact `Assert.Equal` assertions using the captured deterministic values. No test may be committed with directional assertions (`Assert.True(x > y)`) for fields that have deterministic values. The diary entry assertions should verify exact message content where the message is deterministic.

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelDiaryCharacterization"`
Expected: PASS

- [ ] **Step 3: Commit**

```powershell
git add tests/WildBunch.Domain.Tests/TravelDiaryCharacterizationTests.cs
git commit -m "BUNCH-83: add travel diary accumulation characterization tests"
```

---

## Task 7: Verify all characterization tests pass together

- [ ] **Step 1: Run all new characterization tests**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "TravelStateMachine|TravelEncounterResolution|TravelResourceTracking|TravelDiary"`
Expected: ALL PASS

- [ ] **Step 2: Run all existing travel tests to verify no regressions**

Run: `dotnet test --filter "Travel|Journey"`
Expected: ALL PASS — existing tests continue to pass alongside new characterization tests

- [ ] **Step 3: Run full test suite**

Run: `dotnet test`
Expected: No regressions from new test additions

- [ ] **Step 4: Commit (if any adjustments were needed)**

If any factory methods needed adjustment to produce deterministic scenarios, commit those changes.

---

## Phase 1 Completion Gate

**No Phase 1 commit may contain any of the following:**

1. **The temporary value-capture file** (`TravelValueCaptureTests.cs`) — this file is created in Task 2, run to capture values, then deleted before any permanent test is written. It must not appear in any commit.

2. **Placeholder strings in permanent test files** — the following strings are forbidden in any committed characterization test:
   - `After Task 2 capture`
   - `EXPECTED_`
   - `Replace with captured`
   - `Do not commit with`
   - `CAPTURED_`
   - `// After Task 2 capture, add:`
   - Any comment that instructs the worker to add assertions later

3. **Directional assertions for deterministic fields** — no `Assert.True(x > y)`, `Assert.True(x > 0)`, or similar directional checks for fields that have deterministic values in the scenario. Every such field must use `Assert.Equal` with the concrete captured value.

4. **Commented-out assertion lines** — no `// Assert.Equal(...)` lines in committed tests. If an assertion is needed, it must be active with a concrete value.

**Before Phase 1 is considered complete, verify:**

```powershell
# Verify no temporary capture file exists
Test-Path tests/WildBunch.Domain.Tests/TravelValueCaptureTests.cs
# Expected: False

# Verify no placeholder strings in permanent tests
Select-String -Path tests/WildBunch.Domain.Tests/TravelStateMachineCharacterizationTests.cs, tests/WildBunch.Domain.Tests/TravelEncounterResolutionCharacterizationTests.cs, tests/WildBunch.Domain.Tests/TravelResourceTrackingCharacterizationTests.cs, tests/WildBunch.Domain.Tests/TravelDiaryCharacterizationTests.cs -Pattern "After Task 2|EXPECTED_|Replace with captured|Do not commit with|CAPTURED_|// After Task 2"
# Expected: no matches

# Verify no directional assertions for deterministic fields
Select-String -Path tests/WildBunch.Domain.Tests/TravelStateMachineCharacterizationTests.cs, tests/WildBunch.Domain.Tests/TravelEncounterResolutionCharacterizationTests.cs, tests/WildBunch.Domain.Tests/TravelResourceTrackingCharacterizationTests.cs, tests/WildBunch.Domain.Tests/TravelDiaryCharacterizationTests.cs -Pattern "Assert\.True\(.*>.*initial|Assert\.True\(.*>.*0\)"
# Expected: no matches

# Verify no commented-out assertions
Select-String -Path tests/WildBunch.Domain.Tests/TravelStateMachineCharacterizationTests.cs, tests/WildBunch.Domain.Tests/TravelEncounterResolutionCharacterizationTests.cs, tests/WildBunch.Domain.Tests/TravelResourceTrackingCharacterizationTests.cs, tests/WildBunch.Domain.Tests/TravelDiaryCharacterizationTests.cs -Pattern "^\s*//.*Assert\.Equal"
# Expected: no matches
```

Every permanent characterization test must contain concrete `Assert.Equal` assertions using values captured from the deterministic scenarios. If any of the above checks fail, Phase 1 is not complete.
