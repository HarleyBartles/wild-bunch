# Event Sourcing Integrity — Plan B: TravelDiaryDayProjector Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a `TravelDiaryDayProjector` that reconstructs `TravelDiaryDayState` records from the event stream, proving that diary days are derived state rebuildable from events alone. A parity test verifies the projector's output exactly matches the command path's `TravelDiaryDays` for a full journey cycle.

**Architecture:** The projector is a pure function over events in `WildBunch.Application.Projections`, following the existing `DiaryProjector`/`HudProjector` pattern. It tracks running resource state (health, wallet, ammo, heat) across all events, captures day-starting state at day boundaries, and calls `TravelDiaryDayFactory.Create` (accessible via `InternalsVisibleTo`) to build each diary day. Two event enhancements are needed:
1. `TrailEventApplied` must carry `Title` and `Message` fields so the projector can reconstruct `JourneyTrailEventState`.
2. `TravelDayAdvanced` and `JourneyEncounterResolved` must carry `DayEntries` (the full accumulated entries list for the day) so the projector can reconstruct `TravelDiaryDayState.Entries` without needing the day plan loop's internal state.

**Tech Stack:** C#/.NET, xUnit, `dotnet build`, `dotnet test`

## Global Constraints

- This is a greenfield repo — no old saves to break. Event shape changes don't require version bumps or upcasters (Plan D adds versioning).
- `InternalsVisibleTo("WildBunch.Application")` is already declared in `src/WildBunch.Domain/Properties/AssemblyInfo.cs`, so the projector can access `TravelResourceSnapshot`, `TravelDiaryBaselineState`, and `TravelDiaryDayFactory.Create` (all internal in `WildBunch.Domain.Travel`).
- `TravelTestFactory` in `tests/WildBunch.Domain.Tests/` provides deterministic journey test scenarios.
- `TravelDiaryDayState` is a public record in `WildBunch.Domain.Travel` but does NOT implement `IProjectionResult` (which is in `WildBunch.Application`). A wrapper projection result type is needed.
- Run `dotnet build` and `dotnet test` after each task. Run `.\scripts\ci-preflight.ps1` before PR.

---

### Task 1: Enhance TrailEventApplied event with Title and Message

The `TrailEventApplied` event currently carries `TrailEventKind`, `TrailEventId`, and delta fields, but NOT the `Title` and `Message` from `JourneyTrailEventState`. The projector needs these to reconstruct the trail event state. Since this is a greenfield repo, we add them directly.

**Files:**
- Modify: `src/WildBunch.Domain/Events/TrailEventApplied.cs`

**Interfaces:**
- Produces: `TrailEventApplied.Title` (string) and `TrailEventApplied.Message` (string) — consumed by the projector (Task 7) to construct `JourneyTrailEventState`.

- [ ] **Step 1: Add Title and Message fields to TrailEventApplied**

Read `src/WildBunch.Domain/Events/TrailEventApplied.cs`. Add two new `required` properties after `TrailEventId`:

```csharp
public required string Title { get; init; }
public required string Message { get; init; }
```

The full event should look like:

```csharp
public sealed record TrailEventApplied : IDomainEvent
{
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required JourneyTrailEventKind TrailEventKind { get; init; }
    public required JourneyTrailEventId TrailEventId { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required decimal WalletDelta { get; init; }
    public required decimal WalletCash { get; init; }
    public required int FoodDelta { get; init; }
    public required int CanteenChargeDelta { get; init; }
    public required int HorseHungerDelta { get; init; }
    public required int HorseThirstDelta { get; init; }
    public required int HorseExhaustionDelta { get; init; }
    public required int DelayDays { get; init; }
    public required int HeatIncrease { get; init; }
    public required int PursuitHeat { get; init; }
    public required TravelMode? TravelModeChangedTo { get; init; }
    public required string DiaryMessage { get; init; }
    public required string HorseLostMessage { get; init; }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: Build fails because the command path (Task 2) hasn't been updated to populate the new fields yet. This is expected — proceed to Task 2.

---

### Task 2: Update command path to populate Title and Message on TrailEventApplied

**Files:**
- Modify: `src/WildBunch.Domain/Game/JourneyLoop.cs` (the `TrailEventApplied` production site around line 1008)

**Interfaces:**
- Consumes: `TrailEventApplied.Title` and `TrailEventApplied.Message` (from Task 1)
- Produces: Command path populates the new fields so events are self-contained.

- [ ] **Step 1: Update the TrailEventApplied production site**

Read `src/WildBunch.Domain/Game/JourneyLoop.cs` around line 1008. The current code produces the event like:

```csharp
runtime.AddEvent(new TrailEventApplied
{
    JourneySnapshot = postEventSnapshot,
    TrailEventKind = trailEvent.Kind,
    TrailEventId = trailEvent.Id,
    WalletDelta = trailEvent.WalletDelta,
    WalletCash = runtime.WalletCash,
    FoodDelta = trailEvent.FoodDelta,
    CanteenChargeDelta = trailEvent.CanteenChargeDelta,
    HorseHungerDelta = trailEvent.HorseHungerDelta,
    HorseThirstDelta = trailEvent.HorseThirstDelta,
    HorseExhaustionDelta = trailEvent.HorseExhaustionDelta,
    DelayDays = trailEvent.DelayDays,
    HeatIncrease = trailEvent.HeatIncrease,
    PursuitHeat = context.CurrentHeat,
    TravelModeChangedTo = travelModeChangedTo,
    DiaryMessage = fullDiaryMessage,
    HorseLostMessage = horseLossMessage
});
```

Add `Title` and `Message` from the `trailEvent` variable (which is a `JourneyTrailEventState`):

```csharp
runtime.AddEvent(new TrailEventApplied
{
    JourneySnapshot = postEventSnapshot,
    TrailEventKind = trailEvent.Kind,
    TrailEventId = trailEvent.Id,
    Title = trailEvent.Title,
    Message = trailEvent.Message,
    WalletDelta = trailEvent.WalletDelta,
    WalletCash = runtime.WalletCash,
    FoodDelta = trailEvent.FoodDelta,
    CanteenChargeDelta = trailEvent.CanteenChargeDelta,
    HorseHungerDelta = trailEvent.HorseHungerDelta,
    HorseThirstDelta = trailEvent.HorseThirstDelta,
    HorseExhaustionDelta = trailEvent.HorseExhaustionDelta,
    DelayDays = trailEvent.DelayDays,
    HeatIncrease = trailEvent.HeatIncrease,
    PursuitHeat = context.CurrentHeat,
    TravelModeChangedTo = travelModeChangedTo,
    DiaryMessage = fullDiaryMessage,
    HorseLostMessage = horseLossMessage
});
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS. The `Apply(TrailEventApplied)` method only uses `e.JourneySnapshot`, so the new fields are ignored by Apply (they're for projection, not aggregate state).

- [ ] **Step 3: Run existing tests to verify no regressions**

Run: `dotnet test`
Expected: PASS. All existing tests should pass — the new fields are additive and don't change Apply behavior.

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Domain/Events/TrailEventApplied.cs src/WildBunch.Domain/Game/JourneyLoop.cs
git commit -m "Add Title and Message to TrailEventApplied event for projector reconstruction

The projector needs the trail event's Title and Message to reconstruct
JourneyTrailEventState from events. These fields were previously only in the
in-memory JourneyTrailEventState, not on the persisted event. Greenfield repo
— no version bump needed."
```

---

### Task 3: Add DayEntries to TravelDayAdvanced and JourneyEncounterResolved

**Why this is needed:** The command path builds `TravelDiaryDayState.Entries` from the day plan loop's internal state, which interleaves trail event messages and non-trail encounter messages in a specific order. The events carry `DiaryMessage`, `HorseLostMessage`, and `AdditionalDiaryMessages` as separate fields, but these do NOT capture the full entries list in the correct interleaved order. Without `DayEntries` on the events, the projector cannot reconstruct the entries list exactly — the ordering would be wrong when trail events and non-trail encounters are interleaved in the same day.

Adding `DayEntries` to `TravelDayAdvanced` and `JourneyEncounterResolved` makes the events self-contained for projection. The projector uses `DayEntries` directly instead of trying to reconstruct entries from separate fields.

**Files:**
- Modify: `src/WildBunch.Domain/Events/TravelDayAdvanced.cs`
- Modify: `src/WildBunch.Domain/Events/JourneyEncounterResolved.cs`

**Interfaces:**
- Produces: `TravelDayAdvanced.DayEntries` and `JourneyEncounterResolved.DayEntries` — consumed by the projector (Task 7) to set `TravelDiaryDayState.Entries` exactly.

- [ ] **Step 1: Add DayEntries to TravelDayAdvanced**

Read `src/WildBunch.Domain/Events/TravelDayAdvanced.cs`. Add a new property after `AdditionalDiaryMessages`:

```csharp
public IReadOnlyList<string> DayEntries { get; init; } = [];
```

The full event should look like:

```csharp
public sealed record TravelDayAdvanced : IDomainEvent
{
    public required int Day { get; init; }
    public required TravelJourneySnapshot JourneySnapshot { get; init; }
    public required int HealthDelta { get; init; }
    public required int PursuitHeat { get; init; }
    public required TravelDayOutcome DayOutcome { get; init; }
    public required string DiaryMessage { get; init; }
    public required string HorseLostMessage { get; init; }
    public IReadOnlyList<string> AdditionalDiaryMessages { get; init; } = [];
    public IReadOnlyList<string> DayEntries { get; init; } = [];
}
```

- [ ] **Step 2: Add DayEntries to JourneyEncounterResolved**

Read `src/WildBunch.Domain/Events/JourneyEncounterResolved.cs`. Add a new property after `AdditionalDiaryMessages`:

```csharp
public IReadOnlyList<string> DayEntries { get; init; } = [];
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: PASS — the new fields have default values (`[]`) so existing code that doesn't set them will still compile.

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Domain/Events/TravelDayAdvanced.cs src/WildBunch.Domain/Events/JourneyEncounterResolved.cs
git commit -m "Add DayEntries to TravelDayAdvanced and JourneyEncounterResolved

Carries the full accumulated entries list for the diary day being created or
updated. This makes the events self-contained for TravelDiaryDayProjector
reconstruction — the projector uses DayEntries directly instead of trying to
reconstruct interleaved entry ordering from separate event fields."
```

---

### Task 4: Update command path to populate DayEntries

**Files:**
- Modify: `src/WildBunch.Domain/Game/JourneyLoop.cs` (multiple production sites for `TravelDayAdvanced` and `JourneyEncounterResolved`)

**Interfaces:**
- Consumes: `TravelDayAdvanced.DayEntries` and `JourneyEncounterResolved.DayEntries` (from Task 3)
- Produces: Command path populates `DayEntries` with the `dayEntries` list at each event production site.

- [ ] **Step 1: Populate DayEntries on TravelDayAdvanced in HandleInterruptedTravelDay**

Read `src/WildBunch.Domain/Game/JourneyLoop.cs` around line 828-838 (the `TravelDayAdvanced` production in `HandleInterruptedTravelDay`). Add `DayEntries = dayEntries` to the event initializer:

```csharp
runtime.AddEvent(new TravelDayAdvanced
{
    Day = travelDay.NewDay,
    JourneySnapshot = interruptedSnapshot,
    HealthDelta = 0,
    PursuitHeat = travelDay.PursuitHeat,
    DayOutcome = TravelDayOutcome.Interrupted,
    DiaryMessage = encounterMessage,
    HorseLostMessage = horseLostMessage,
    AdditionalDiaryMessages = narrationMessages,
    DayEntries = dayEntries
});
```

- [ ] **Step 2: Populate DayEntries on TravelDayAdvanced in HandleCompletedTravelDay**

Read around line 874-892 (the `TravelDayAdvanced` production in `HandleCompletedTravelDay`). Add `DayEntries = dayEntries`:

```csharp
runtime.AddEvent(new TravelDayAdvanced
{
    Day = travelDay.NewDay,
    JourneySnapshot = completedSnapshot,
    HealthDelta = 0,
    PursuitHeat = travelDay.PursuitHeat,
    DayOutcome = TravelDayOutcome.Completed,
    DiaryMessage = arrivalMessage,
    HorseLostMessage = horseLostMessage,
    AdditionalDiaryMessages = narrationMessages,
    DayEntries = dayEntries
});
```

- [ ] **Step 3: Populate DayEntries on TravelDayAdvanced in HandleOngoingTravelDay**

Read around line 923-933 (the `TravelDayAdvanced` production in `HandleOngoingTravelDay`). Add `DayEntries = dayEntries`:

```csharp
runtime.AddEvent(new TravelDayAdvanced
{
    Day = travelDay.NewDay,
    JourneySnapshot = journeySnapshot,
    HealthDelta = 0,
    PursuitHeat = travelDay.PursuitHeat,
    DayOutcome = TravelDayOutcome.Ongoing,
    DiaryMessage = ongoingMessage,
    HorseLostMessage = horseLostMessage,
    AdditionalDiaryMessages = narrationMessages,
    DayEntries = dayEntries
});
```

- [ ] **Step 4: Populate DayEntries on JourneyEncounterResolved at all production sites**

Search for all `new JourneyEncounterResolved` production sites in `JourneyLoop.cs`. There are multiple (around lines 438, 511, 603, and others in `ResolveJourneyEncounter` and `ContinueCurrentDayAfterEncounterResolution`). At each site, add `DayEntries = dayEntries` (or the appropriate entries variable for that scope).

The pattern at each site is: the `JourneyEncounterResolved` event is produced after `PersistLatestTravelDiaryDay` is called. The `dayEntries` list at that point contains the accumulated entries for the current day. After `PersistLatestTravelDiaryDay` updates the last diary day's entries (by concatenating `newEntries`), the full entries list is `_travelDiaryDays[^1].Entries`. Use that for `DayEntries`:

```csharp
DayEntries = _travelDiaryDays[^1].Entries
```

Or, if `PersistLatestTravelDiaryDay` was not called (e.g., the encounter was resolved on the first attempt and the day was already completed), use the `dayEntries` list directly:

```csharp
DayEntries = dayEntries
```

**Important:** Read each production site carefully to determine which variable contains the correct entries list. The `PersistLatestTravelDiaryDay` method concatenates `_travelDiaryDays[^1].Entries` with `newEntries`, so after it's called, `_travelDiaryDays[^1].Entries` is the full list. If `PersistLatestTravelDiaryDay` was NOT called, `dayEntries` is the list.

For sites where `PersistLatestTravelDiaryDay` was called, use:
```csharp
DayEntries = _travelDiaryDays.Count > 0 ? _travelDiaryDays[^1].Entries : dayEntries
```

For sites where `PersistLatestTravelDiaryDay` was NOT called, use:
```csharp
DayEntries = dayEntries
```

- [ ] **Step 5: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 6: Run existing tests to verify no regressions**

Run: `dotnet test`
Expected: PASS. The new `DayEntries` field is additive — existing tests don't assert on it, and the `Apply` methods don't use it.

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Domain/Game/JourneyLoop.cs
git commit -m "Populate DayEntries on TravelDayAdvanced and JourneyEncounterResolved

Carries the full accumulated entries list for the diary day at each event
production site. This makes the events self-contained for projector
reconstruction."
```

---

### Task 5: Create TravelDiaryDayProjection result type

**Files:**
- Create: `src/WildBunch.Application/Projections/TravelDiaryDayProjection.cs`

**Interfaces:**
- Produces: `TravelDiaryDayProjection` — a wrapper around `IReadOnlyList<TravelDiaryDayState>` that implements `IProjectionResult`. Consumed by the projector (Task 7) and the parity test (Task 6).

- [ ] **Step 1: Create the projection result type**

Create `src/WildBunch.Application/Projections/TravelDiaryDayProjection.cs`:

```csharp
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Projections;

/// <summary>
/// Projection result: travel diary days derived from the domain event stream.
/// This is a read-only projection — it does not mutate aggregate state.
/// See ADR-0028 and the event sourcing integrity policy.
/// </summary>
public sealed record TravelDiaryDayProjection(
    IReadOnlyList<TravelDiaryDayState> Days) : IProjectionResult;
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/WildBunch.Application/WildBunch.Application.csproj`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Application/Projections/TravelDiaryDayProjection.cs
git commit -m "Add TravelDiaryDayProjection result type"
```

---

### Task 6: Write failing parity test

**Files:**
- Create: `tests/WildBunch.Domain.Tests/Projections/TravelDiaryDayProjectorParityTests.cs`

**Interfaces:**
- Consumes: `TravelDiaryDayProjector` (doesn't exist yet — test will fail to compile), `TravelTestFactory` (existing, in `WildBunch.Domain.Tests`)

**Note:** `TravelTestFactory` is `internal` in `WildBunch.Domain.Tests`. The parity test goes in `tests/WildBunch.Domain.Tests/Projections/` (following the pattern of `JournalLogProjectorEquivalenceTests.cs` which is already in `WildBunch.Domain.Tests`). Verify that `WildBunch.Domain.Tests.csproj` has a `ProjectReference` to `WildBunch.Application.csproj`.

- [ ] **Step 1: Check that WildBunch.Domain.Tests can reference WildBunch.Application**

Read `tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj` and verify it has a `ProjectReference` to `src/WildBunch.Application/WildBunch.Application.csproj`. If it does not, add one:

```xml
<ProjectReference Include="..\..\src\WildBunch.Application\WildBunch.Application.csproj" />
```

- [ ] **Step 2: Write the failing parity test**

Create `tests/WildBunch.Domain.Tests/Projections/TravelDiaryDayProjectorParityTests.cs`:

```csharp
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Tests;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests.Projections;

/// <summary>
/// Proves that TravelDiaryDayProjector reconstructs the exact same
/// TravelDiaryDayState records as the command path produces.
/// This is the parity test that proves diary days are derived state
/// rebuildable from the event stream alone.
/// </summary>
public sealed class TravelDiaryDayProjectorParityTests
{
    [Fact]
    public void Projector_FullJourneyCycle_MatchesCommandPathDiaryDays()
    {
        var (commandSession, preview, setupEvents) =
            TravelTestFactory.CreateSixDayQuietJourneyWithSetupEvents();
        commandSession.StartJourney(preview);

        // Force quiet days through the dev-travel override seam so the journey
        // completes without seed-dependent encounter interruptions.
        TravelJourneyStepResult result;
        do
        {
            commandSession.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            result = commandSession.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        commandSession.AcknowledgeJourneyArrival();

        var events = setupEvents.Concat(commandSession.UncommittedEvents).ToList();
        var projector = new TravelDiaryDayProjector();
        var projection = projector.Project(events);

        var commandDiaryDays = commandSession.TravelDiaryDays;
        Assert.Equal(commandDiaryDays.Count, projection.Days.Count);

        for (var i = 0; i < commandDiaryDays.Count; i++)
        {
            var expected = commandDiaryDays[i];
            var actual = projection.Days[i];

            Assert.Equal(expected.DayNumber, actual.DayNumber);
            Assert.Equal(expected.OriginTownName, actual.OriginTownName);
            Assert.Equal(expected.DestinationTownName, actual.DestinationTownName);
            Assert.Equal(expected.StartingTravelMode, actual.StartingTravelMode);
            Assert.Equal(expected.EndingTravelMode, actual.EndingTravelMode);
            Assert.Equal(expected.Status, actual.Status);
            Assert.Equal(expected.StartingRideDayDistance, actual.StartingRideDayDistance);
            Assert.Equal(expected.RemainingRideDayDistance, actual.RemainingRideDayDistance);
            Assert.Equal(expected.StartingDaysRemaining, actual.StartingDaysRemaining);
            Assert.Equal(expected.RemainingDays, actual.RemainingDays);
            Assert.Equal(expected.HealthDelta, actual.HealthDelta);
            Assert.Equal(expected.WalletDelta, actual.WalletDelta);
            Assert.Equal(expected.FoodDelta, actual.FoodDelta);
            Assert.Equal(expected.HorseFeedDelta, actual.HorseFeedDelta);
            Assert.Equal(expected.CanteenChargeDelta, actual.CanteenChargeDelta);
            Assert.Equal(expected.AmmoSpent, actual.AmmoSpent);
            Assert.Equal(expected.DelayDays, actual.DelayDays);
            Assert.Equal(expected.HeatIncrease, actual.HeatIncrease);
            Assert.Equal(expected.CurrentHealth, actual.CurrentHealth);
            Assert.Equal(expected.CurrentWallet, actual.CurrentWallet);
            Assert.Equal(expected.CurrentFood, actual.CurrentFood);
            Assert.Equal(expected.CurrentHorseFeed, actual.CurrentHorseFeed);
            Assert.Equal(expected.CurrentCanteenCharges, actual.CurrentCanteenCharges);
            Assert.Equal(expected.CurrentAmmo, actual.CurrentAmmo);
            Assert.Equal(expected.CurrentHeat, actual.CurrentHeat);
            Assert.Equal(expected.OpeningNarration, actual.OpeningNarration);
            Assert.Equal(expected.Entries, actual.Entries);
            Assert.Equal(expected.Warnings, actual.Warnings);

            // TrailEvent comparison (may be null)
            if (expected.TrailEvent is null)
            {
                Assert.Null(actual.TrailEvent);
            }
            else
            {
                Assert.NotNull(actual.TrailEvent);
                Assert.Equal(expected.TrailEvent.Id, actual.TrailEvent.Id);
                Assert.Equal(expected.TrailEvent.Kind, actual.TrailEvent.Kind);
                Assert.Equal(expected.TrailEvent.Title, actual.TrailEvent.Title);
                Assert.Equal(expected.TrailEvent.Message, actual.TrailEvent.Message);
                Assert.Equal(expected.TrailEvent.WalletDelta, actual.TrailEvent.WalletDelta);
                Assert.Equal(expected.TrailEvent.FoodDelta, actual.TrailEvent.FoodDelta);
                Assert.Equal(expected.TrailEvent.CanteenChargeDelta, actual.TrailEvent.CanteenChargeDelta);
                Assert.Equal(expected.TrailEvent.DelayDays, actual.TrailEvent.DelayDays);
            }

            // EncounterResolution comparison (may be null)
            if (expected.EncounterResolution is null)
            {
                Assert.Null(actual.EncounterResolution);
            }
            else
            {
                Assert.NotNull(actual.EncounterResolution);
                Assert.Equal(expected.EncounterResolution.ChoiceId, actual.EncounterResolution.ChoiceId);
                Assert.Equal(expected.EncounterResolution.ChoiceLabel, actual.EncounterResolution.ChoiceLabel);
                Assert.Equal(expected.EncounterResolution.HealthDelta, actual.EncounterResolution.HealthDelta);
                Assert.Equal(expected.EncounterResolution.WalletDelta, actual.EncounterResolution.WalletDelta);
                Assert.Equal(expected.EncounterResolution.AmmoSpent, actual.EncounterResolution.AmmoSpent);
                Assert.Equal(expected.EncounterResolution.HeatIncrease, actual.EncounterResolution.HeatIncrease);
                Assert.Equal(expected.EncounterResolution.HorseExhaustionDelta, actual.EncounterResolution.HorseExhaustionDelta);
                Assert.Equal(expected.EncounterResolution.ContinuedOnFoot, actual.EncounterResolution.ContinuedOnFoot);
            }
        }
    }

    [Fact]
    public void Projector_ShortJourney_MatchesCommandPathDiaryDays()
    {
        var (commandSession, preview, setupEvents) =
            TravelTestFactory.CreateEasyShortJourneyWithSetupEvents();
        commandSession.StartJourney(preview);

        TravelJourneyStepResult result;
        do
        {
            commandSession.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            result = commandSession.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        commandSession.AcknowledgeJourneyArrival();

        var events = setupEvents.Concat(commandSession.UncommittedEvents).ToList();
        var projector = new TravelDiaryDayProjector();
        var projection = projector.Project(events);

        var commandDiaryDays = commandSession.TravelDiaryDays;
        Assert.Equal(commandDiaryDays.Count, projection.Days.Count);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~TravelDiaryDayProjectorParityTests`
Expected: FAIL — `TravelDiaryDayProjector` does not exist yet (compilation error).

- [ ] **Step 4: Commit the failing test**

```bash
git add tests/WildBunch.Domain.Tests/Projections/TravelDiaryDayProjectorParityTests.cs
git commit -m "Add failing parity test for TravelDiaryDayProjector"
```

---

### Task 7: Implement TravelDiaryDayProjector

This is the core task — the projector that reconstructs diary days from events. With `DayEntries` on the events, the projector is significantly simpler: it doesn't need to track entries at all. It uses `DayEntries` from `TravelDayAdvanced` and `JourneyEncounterResolved` directly.

**Files:**
- Create: `src/WildBunch.Application/Projections/TravelDiaryDayProjector.cs`

**Interfaces:**
- Consumes: `IDomainEvent` stream, `TravelDiaryDayFactory.Create` (internal, via InternalsVisibleTo), `TravelResourceSnapshot` (internal), `TravelDiaryBaselineState` (internal), all journey event types, pre-journey resource events, `DayEntries` from `TravelDayAdvanced` and `JourneyEncounterResolved`.
- Produces: `TravelDiaryDayProjection` containing `IReadOnlyList<TravelDiaryDayState>`.

- [ ] **Step 1: Create the projector implementation**

Create `src/WildBunch.Application/Projections/TravelDiaryDayProjector.cs` with the following exact content:

```csharp
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Projections;

/// <summary>
/// Reconstructs TravelDiaryDayState records from the domain event stream.
/// This is a pure function over events — no aggregate mutation, no runtime context.
/// See ADR-0028 and the event sourcing integrity policy.
///
/// The projector tracks running resource state (health, wallet, ammo, heat) across
/// all events, captures day-starting state at day boundaries, and calls
/// TravelDiaryDayFactory.Create to build each diary day with correct deltas.
/// Entries come from the DayEntries field on TravelDayAdvanced and
/// JourneyEncounterResolved — the command path populates this with the full
/// accumulated entries list for the day.
/// </summary>
public sealed class TravelDiaryDayProjector : IDomainEventProjector<TravelDiaryDayProjection>
{
    public TravelDiaryDayProjection Project(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        // Running resource state (tracked across all events)
        int health = 0;
        decimal wallet = 0m;
        int ammo = 0;
        int heat = 0;

        // Current journey snapshot (from latest journey event)
        TravelJourneySnapshot? currentSnapshot = null;

        // Day tracking state
        TravelDiaryBaselineState? dayStartingState = null;
        JourneyTrailEventState? pendingTrailEvent = null;
        TravelDiaryEncounterResolutionState? encounterResolution = null;
        var diaryDays = new List<TravelDiaryDayState>();

        foreach (var e in events)
        {
            switch (e)
            {
                case GameStarted gs:
                    health = gs.StartingHealth;
                    wallet = gs.StartingWallet;
                    ammo = CountAmmo(gs.StartingInventoryItems);
                    break;

                case StoreItemPurchased sp:
                    wallet = sp.WalletAfter;
                    if (sp.ItemKind is ItemKind.RevolverAmmo or ItemKind.RifleAmmo)
                        ammo += sp.Quantity;
                    break;

                case SheriffTurnInSettled sts:
                    wallet += sts.BountyAmount;
                    break;

                case SaloonPersonOfInterestConfronted spoc:
                    if (spoc.WalletAfter is { } walletAfter)
                        wallet = walletAfter;
                    break;

                case JourneyStarted js:
                    currentSnapshot = js.JourneySnapshot;
                    heat = js.PursuitHeat;
                    dayStartingState = CaptureBaseline(currentSnapshot, health, wallet, ammo, heat);
                    pendingTrailEvent = null;
                    encounterResolution = null;
                    break;

                case TrailEventApplied tea:
                    currentSnapshot = tea.JourneySnapshot;
                    wallet = tea.WalletCash;
                    heat = tea.PursuitHeat;
                    pendingTrailEvent = new JourneyTrailEventState(
                        tea.TrailEventId,
                        tea.TrailEventKind,
                        tea.Title,
                        tea.Message,
                        tea.WalletDelta,
                        tea.FoodDelta,
                        tea.CanteenChargeDelta,
                        tea.HorseHungerDelta,
                        tea.HorseThirstDelta,
                        tea.HorseExhaustionDelta,
                        tea.DelayDays,
                        tea.HeatIncrease);
                    break;

                case TravelDayAdvanced tda:
                    health += tda.HealthDelta;
                    heat = tda.PursuitHeat;
                    currentSnapshot = tda.JourneySnapshot;
                    CreateAndStoreDiaryDay(
                        currentSnapshot, dayStartingState, health, wallet, ammo, heat,
                        pendingTrailEvent, encounterResolution, tda.DayEntries, diaryDays);
                    dayStartingState = CaptureBaseline(currentSnapshot, health, wallet, ammo, heat);
                    pendingTrailEvent = null;
                    encounterResolution = null;
                    break;

                case JourneyEncounterResolved jer:
                    var healthBefore = health;
                    var walletBefore = wallet;
                    var heatBefore = heat;
                    health = jer.PlayerHealth;
                    wallet = jer.WalletCash;
                    ammo -= jer.AmmoSpent;
                    heat = jer.PursuitHeat;
                    currentSnapshot = jer.JourneySnapshot;
                    encounterResolution = new TravelDiaryEncounterResolutionState(
                        jer.ChoiceId,
                        jer.ChoiceLabel,
                        health - healthBefore,
                        wallet - walletBefore,
                        jer.AmmoSpent,
                        heat - heatBefore,
                        jer.HorseExhaustionDelta,
                        jer.ContinuedOnFoot);

                    if (jer.DayCompleted)
                    {
                        // Day completed: finalize the diary day with DayEntries from the event
                        CreateAndStoreDiaryDay(
                            currentSnapshot, dayStartingState, health, wallet, ammo, heat,
                            pendingTrailEvent, encounterResolution, jer.DayEntries, diaryDays);
                        dayStartingState = CaptureBaseline(currentSnapshot, health, wallet, ammo, heat);
                        pendingTrailEvent = null;
                        encounterResolution = null;
                    }
                    else
                    {
                        // Day not completed: update the last diary day's entries
                        // The command path calls PersistLatestTravelDiaryDay which updates
                        // the last day in-place. We do the same here.
                        if (diaryDays.Count > 0)
                        {
                            var lastIndex = diaryDays.Count - 1;
                            var updatedDay = TravelDiaryDayFactory.Create(
                                currentSnapshot,
                                dayStartingState!,
                                CaptureResources(currentSnapshot, health, wallet, ammo, heat),
                                trailEvent: pendingTrailEvent,
                                pendingEncounter: currentSnapshot.PendingEncounter,
                                encounterResolution: encounterResolution,
                                entries: jer.DayEntries);
                            diaryDays[lastIndex] = updatedDay;
                        }
                    }
                    break;

                // JourneyCompleted and JourneyArrivalAcknowledged do not create diary days.
                // The last diary day is created by TravelDayAdvanced or JourneyEncounterResolved
                // with DayCompleted=true. JourneyCompleted carries an empty DiaryMessage.
            }
        }

        return new TravelDiaryDayProjection(diaryDays);
    }

    private static int CountAmmo(IReadOnlyList<InventoryItem> items)
    {
        var total = 0;
        foreach (var item in items)
        {
            if (item.Kind is ItemKind.RevolverAmmo or ItemKind.RifleAmmo)
                total += item.Quantity;
        }
        return total;
    }

    private static TravelResourceSnapshot CaptureResources(
        TravelJourneySnapshot snapshot, int health, decimal wallet, int ammo, int heat)
        => new(
            snapshot.HorseState,
            wallet,
            snapshot.AvailableFood,
            snapshot.AvailableHorseFeed,
            snapshot.AvailableCanteenCharges,
            ammo,
            health,
            heat);

    private static TravelDiaryBaselineState CaptureBaseline(
        TravelJourneySnapshot snapshot, int health, decimal wallet, int ammo, int heat)
        => new(
            snapshot.TravelMode,
            snapshot.RemainingRideDayDistance,
            snapshot.RemainingDays,
            snapshot.DelayDays,
            CaptureResources(snapshot, health, wallet, ammo, heat));

    private static void CreateAndStoreDiaryDay(
        TravelJourneySnapshot snapshot,
        TravelDiaryBaselineState? startingState,
        int health, decimal wallet, int ammo, int heat,
        JourneyTrailEventState? trailEvent,
        TravelDiaryEncounterResolutionState? encounterResolution,
        IReadOnlyList<string> entries,
        List<TravelDiaryDayState> diaryDays)
    {
        if (startingState is null)
            return;

        var currentResources = CaptureResources(snapshot, health, wallet, ammo, heat);
        var pendingEncounter = snapshot.PendingEncounter;

        diaryDays.Add(TravelDiaryDayFactory.Create(
            snapshot,
            startingState,
            currentResources,
            trailEvent: trailEvent,
            pendingEncounter: pendingEncounter,
            encounterResolution: encounterResolution,
            entries: entries));
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build`
Expected: PASS. The projector uses `TravelResourceSnapshot`, `TravelDiaryBaselineState`, and `TravelDiaryDayFactory.Create` which are internal in `WildBunch.Domain.Travel` but accessible via `InternalsVisibleTo("WildBunch.Application")`.

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Application/Projections/TravelDiaryDayProjector.cs
git commit -m "Implement TravelDiaryDayProjector

Pure function over domain events that reconstructs TravelDiaryDayState records
from the event stream. Tracks running resource state (health, wallet, ammo, heat)
across all events, captures day-starting state at day boundaries, and calls
TravelDiaryDayFactory.Create to build each diary day with correct deltas. Entries
come from the DayEntries field on TravelDayAdvanced and JourneyEncounterResolved."
```

---

### Task 8: Run parity test and fix discrepancies

- [ ] **Step 1: Run the parity test**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~TravelDiaryDayProjectorParityTests`
Expected: May PASS or FAIL. If it fails, examine the assertion failures to identify which fields don't match.

- [ ] **Step 2: Fix any discrepancies**

Common discrepancies and their fixes:

1. **Resource delta mismatch:** If health/wallet/heat deltas are off, verify the running resource tracking is correct. Check that absolute vs. additive updates are applied correctly for each event type. `TravelDayAdvanced.HealthDelta` is additive; `JourneyEncounterResolved.PlayerHealth` is absolute; `TrailEventApplied.WalletCash` is absolute; `JourneyEncounterResolved.WalletCash` is absolute.

2. **HorseState mismatch:** The `HorseTravelState` on the journey snapshot may differ from what the command path captures. Verify the projector uses `snapshot.HorseState` (which is the same snapshot the command path uses).

3. **PendingEncounter mismatch:** The factory uses `pendingEncounter ?? journeySnapshot.PendingEncounter`. The projector passes `snapshot.PendingEncounter` directly. If the command path passes a different encounter state, investigate whether the journey snapshot's `PendingEncounter` is the correct source.

4. **OpeningNarration mismatch:** The factory sets `openingNarration` only when `startingState.StartingDaysRemaining == journeySnapshot.ExpectedDays`. Verify the baseline captures `StartingDaysRemaining` correctly at journey start.

5. **DayEntries mismatch:** If entries don't match, verify that `DayEntries` is populated correctly at all production sites in Task 4. The command path's `dayEntries` list must match what's passed to `TravelDiaryDayFactory.Create`.

If a discrepancy requires a code change, fix it in the projector and re-run the test. Do not change the command path or the factory — the projector must match the command path, not the other way around.

If a discrepancy reveals a bug in the command path or factory, document it as a finding rather than fixing it in this plan (it's out of scope — this plan builds the projector, not fixes command-path bugs).

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test`
Expected: PASS. All tests including the parity tests should pass.

- [ ] **Step 4: Commit any fixes**

```bash
git add src/WildBunch.Application/Projections/TravelDiaryDayProjector.cs
git commit -m "Fix TravelDiaryDayProjector parity discrepancies"
```

If no fixes were needed, skip this step.

---

### Task 9: Regenerate index mesh, run CI preflight, and open PR

- [ ] **Step 1: Regenerate index mesh**

Run: `python scripts/generate_index_mesh.py`
Then: `python scripts/generate_index_mesh.py --check`
Expected: exit code 0.

- [ ] **Step 2: Commit index mesh if changed**

```bash
git add .agents/INDEX.md
git commit -m "Regenerate index mesh for TravelDiaryDayProjector"
```

If no INDEX.md files changed, skip this step.

- [ ] **Step 3: Run CI preflight**

Run: `.\scripts\ci-preflight.ps1`
Expected: all checks pass (backend, frontend, index-mesh).

If backend fails, run `dotnet build` and `dotnet test` to identify the issue. If frontend fails, investigate — this plan should not affect frontend. If index-mesh fails, regenerate and re-commit.

- [ ] **Step 4: Push branch and open draft PR**

```bash
git push -u origin <branch-name>
gh pr create --title "TravelDiaryDayProjector: rebuild diary days from event stream" --draft --body "..."
```

- [ ] **Step 5: Mark PR ready for review**

After confirming CI preflight passes and the branch is current with `origin/main`, mark the PR ready for review.

---

## Self-Review

### Spec Coverage

- **Part 1b TravelDiaryDayProjector:** Task 7 creates the projector. ✓
- **Part 1b parity test:** Task 6 writes the test, Task 8 verifies it passes. ✓
- **Event enhancement (Title/Message):** Tasks 1-2 enhance `TrailEventApplied` to make it self-contained for `JourneyTrailEventState` reconstruction. ✓
- **Event enhancement (DayEntries):** Tasks 3-4 add `DayEntries` to `TravelDayAdvanced` and `JourneyEncounterResolved` to make events self-contained for entries reconstruction. ✓ (Necessary prerequisite discovered during source verification — the events didn't carry the full entries list.)
- **Projection result type:** Task 5 creates `TravelDiaryDayProjection`. ✓

### Placeholder Scan

No TBDs, TODOs, or vague shorthand. The projector code is fully specified. The parity test code is fully specified. The discrepancy-fixing task (Task 8) has a concrete troubleshooting guide with common issues and their fixes.

### Type Consistency

- `TravelDiaryDayProjection` — created in Task 5, used in Task 7 (projector return type) and Task 6 (test assertion).
- `TravelDiaryDayProjector` — created in Task 7, referenced in Task 6 (test).
- `TrailEventApplied.Title` / `.Message` — added in Task 1, populated in Task 2, used in Task 7 (projector).
- `TravelDayAdvanced.DayEntries` — added in Task 3, populated in Task 4, used in Task 7 (projector).
- `JourneyEncounterResolved.DayEntries` — added in Task 3, populated in Task 4, used in Task 7 (projector).
- `TravelResourceSnapshot` — internal in Domain, used in Task 7 via InternalsVisibleTo.
- `TravelDiaryBaselineState` — internal in Domain, used in Task 7 via InternalsVisibleTo.
- `TravelDiaryDayFactory.Create` — public in Domain, used in Task 7 via InternalsVisibleTo (parameters are internal types).

## Execution Confidence Assessment

### Direct Execution Confidence: 8/10

The projector code is fully specified and simplified by the `DayEntries` enhancement — the projector doesn't need to track entries at all. The main remaining risk is resource tracking discrepancies (health/wallet/heat deltas) which the troubleshooting guide covers. A direct implementer can iterate on the parity test with the troubleshooting guide.

### SDD Confidence: 8/10

The projector code is concrete enough for transcription. The `DayEntries` enhancement eliminates the entry-ordering gap that was the primary source of uncertainty. The troubleshooting guide provides concrete steps for common discrepancies. The two-test approach (six-day quiet journey + short journey) covers the main scenarios. The encounter-resolution "update last day" pattern is explicitly handled in the projector code.

### Gap Closure Summary

- **TrailEventApplied Title/Message gap:** Discovered during source verification that the event didn't carry `Title` and `Message` fields needed by `JourneyTrailEventState`. Closed by adding them to the event (Tasks 1-2). Greenfield repo — no version bump needed.
- **DayEntries gap:** Discovered during deep source verification that the command path's `entries` list (interleaved trail event messages and non-trail encounter messages) is NOT reconstructable from the event stream's separate fields (`DiaryMessage`, `AdditionalDiaryMessages`). Closed by adding `DayEntries` to `TravelDayAdvanced` and `JourneyEncounterResolved` (Tasks 3-4). This makes the events self-contained for projection and significantly simplifies the projector.
- **InternalsVisibleTo:** Verified that `InternalsVisibleTo("WildBunch.Application")` already exists. No new assembly attributes needed.
- **Test project reference:** The parity test goes in `WildBunch.Domain.Tests` (not `WildBunch.Application.Tests`) because `TravelTestFactory` is internal there. Verified that `JournalLogProjectorEquivalenceTests.cs` already follows this pattern.
- **Projection result type:** `TravelDiaryDayState` doesn't implement `IProjectionResult`. Closed by creating `TravelDiaryDayProjection` wrapper (Task 5).
- **Projector algorithm:** Fully specified with exact code for all event handlers, resource tracking, and diary day creation. The "update last day" pattern for unresolved encounters is explicitly handled.
- **Apply methods confirmed clean:** Source verification confirmed that no `Apply` method creates diary entries or diary days — they only update aggregate state. This confirms the spec's claim that `Apply` methods should not create projections.

### Open Questions

1. **Parity test edge cases:** The parity test uses `CreateSixDayQuietJourneyWithSetupEvents` which forces quiet days (no encounters). This tests the day-advance path but not the encounter-resolution path. A more comprehensive test would include encounter resolution, but that requires a deterministic encounter scenario. The `CreateEasyShortJourneyWithSetupEvents` test provides a second data point. If the quiet-journey parity passes, the encounter path is likely correct (the projector's encounter handling follows the same pattern), but it's not proven. This is a known gap — the full replay equality test in Plan C will provide broader coverage.

2. **DayEntries population at all JourneyEncounterResolved sites:** Task 4 Step 4 requires populating `DayEntries` at all `JourneyEncounterResolved` production sites. There are multiple sites with different surrounding context. The implementer must read each site carefully to determine whether `_travelDiaryDays[^1].Entries` or `dayEntries` is the correct source. This is documented in the task but requires careful reading of the command path.
