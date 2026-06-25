# BUNCH-89: Event-Sourced Travel Dev Controls Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the first contextual dev overlay module — travel/journey controls that let Harley force the next travel result or foe profile through backend dev commands and event-sourced aggregate state, then proceed through normal gameplay.

**Architecture:** Dev commands flow through `/api/dev/` endpoints (gated by `DevRoleGuard`) into Application handlers that load `GameSession` via the repository, invoke new aggregate command methods on `GameSession`, and store typed domain events. The aggregate holds a pending dev override as session-owned state. Normal `AdvanceJourneyDay` consumes the override once (replacing `TravelDayPlanGenerator.Generate` output) and emits the normal `TravelDayAdvanced` event. Dev events (`DevTravelOverrideForced`, `DevTravelOverrideCleared`) are immutable facts in the event stream. The frontend adds a `TravelDevPanel` to the existing `DevPanelRegistry` that fetches dev query data and dispatches dev commands via `useMutation`.

**Tech Stack:** C#/.NET 10, ASP.NET Core Minimal APIs, EF Core, xUnit, React 18, TanStack Query, styled-components, Vitest.

## Global Constraints

- `GameSession` is the live-play aggregate root; all gameplay mutation flows through it.
- Typed domain events are plain sealed records implementing `IDomainEvent`; `Apply` is the single mutation path.
- Dev endpoints live under `/api/dev/` and are gated by `DevRoleGuard.EnsureDevAccess()`.
- Dev DTOs are separate types from player DTOs.
- Normal player APIs must remain clean of travel internals and hidden/dev state.
- Travel advances one trail day at a time; do not reintroduce instant multi-day travel.
- Hidden encounter state, rolls, and generator internals must stay out of public DTO/API/read responses.
- The culprit is always a gang member; this issue does not touch culprit/seed logic.
- Do not force bribe/run/fight success or failure; the player still resolves normally.
- Do not bypass normal travel or encounter resolution mechanics.
- Do not fold BUNCH-87 deterministic test seed profiles into this issue.
- Worker environment uses PowerShell; do not use `&&` for command chaining.
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent validation.

---

## Preflight Answers (source-grounded)

### Q1: Where does travel-day generation happen and when is the next travel thing generated?

`GameSession.PrepareTravelDayAdvance()` at `src/WildBunch.Domain/Game/GameSession.cs:1100` calls `TravelDayPlanGenerator.Generate(generationContext)` at line 1173. Generation is **eager** — the entire day plan (all encounters) is generated during `AdvanceJourneyDay()`, which is invoked by `AdvanceTravelDayHandler.HandleAsync()` at `src/WildBunch.Application/Games/Commands/AdvanceTravelDayHandler.cs:26`. The generator lives at `src/WildBunch.Domain/Travel/TravelDayPlanGenerator.Context.cs:20`.

### Q2: Is there already pending next-day or pending encounter state?

Yes. `TravelJourney.PendingEncounter` (`src/WildBunch.Domain/Travel/TravelJourney.cs:40`) holds the current blocking encounter. `TravelJourney.CurrentDayPlan` (line 42) holds the full generated day plan. When an encounter requires choice, `Journey.MarkInterrupted(pendingEncounter)` (line 157) sets `Status = Interrupted` and stores the encounter. After resolution, `Journey.ResumeFromEncounter()` (line 190) clears it. There is **no pre-generated next-day state** — generation happens only inside `AdvanceJourneyDay()`.

### Q3: What current event sequence represents travel-day advancement, encounter generation/pending state, and encounter resolution?

- `JourneyStarted` — `src/WildBunch.Domain/Events/JourneyStarted.cs`, emitted by `GameSession.StartJourney()` (line 817), applied at line 483.
- `TravelDayAdvanced` — `src/WildBunch.Domain/Events/TravelDayAdvanced.cs`, emitted by `HandleInterruptedTravelDay` (line 1196), `HandleCompletedTravelDay` (line 1245), `HandleOngoingTravelDay` (line 1295). Applied at line 500. Carries ABSOLUTE `JourneySnapshot`, `Day`, `PursuitHeat`, `DayOutcome`, `DiaryMessage`, `AdditionalDiaryMessages`.
- `TrailEventApplied` — `src/WildBunch.Domain/Events/TrailEventApplied.cs`, emitted by `ApplyTrailEvent()`, applied at line 558. Carries ABSOLUTE `JourneySnapshot`, `WalletCash`, `PursuitHeat`.
- `JourneyEncounterResolved` — `src/WildBunch.Domain/Events/JourneyEncounterResolved.cs`, emitted by `ResolveJourneyEncounterDeterministic()` (lines 1774, 1854, 1948, 2012), applied at line 577. Carries ABSOLUTE `JourneySnapshot`, `PlayerHealth`, `WalletCash`, `PursuitHeat`; ADDITIVE `AmmoSpent`, `StolenItem`.
- `JourneyCompleted` — applied at line 599.
- `JourneyArrivalAcknowledged` — emitted by `AcknowledgeJourneyArrival()` (line 1423).

### Q4: What current foe profile fields affect bribe, run, and fight decisions?

`JourneyFoeProfile` at `src/WildBunch.Domain/Travel/JourneyEncounterModels.cs:35`:
- `int Speed` — used in `JourneyEncounterResolutionEngine.ResolveRun()` (line 125): escape chance `42 + (escapeBand - foeProfile.Speed) * 11`, health loss `EncounterRunFootHealthLoss + foeProfile.Speed - escapeBand`.
- `int FightStrength` — used in `ResolveFight()` (line 195): fight chance `30 + (fightBand - foeProfile.FightStrength) * 8`, health loss calculations.
- `decimal MinimumBribe` — used in `ResolveBribe()` (line 270): success check `cumulativeBribePaid >= foeProfile.MinimumBribe`, insult threshold `foeProfile.MinimumBribe * 0.35m`, theft retaliation `foeProfile.FightStrength * 2`, wallet theft `foeProfile.MinimumBribe / 2m`.

Foe profiles are created by `JourneyEncounterResolutionEngine.CreateFoeProfile()` at `src/WildBunch.Domain/Travel/JourneyEncounterResolutionEngine.cs:35`.

### Q5: Where should pending dev travel override state live?

On `GameSession` as a new private field `_pendingDevTravelOverride` of type `DevTravelOverride?` (a new domain record). This is session-owned aggregate state, persisted via the snapshot and reconstructed on load. It is NOT on `TravelJourney` — the override applies to the *next* `AdvanceJourneyDay()` call, which may happen when no day plan exists yet. It is consumed inside `PrepareTravelDayAdvance()` at line 1173, replacing the generated day plan when present.

### Q6: What immutable dev events are needed for force, clear, and consume?

Three typed dev events, each with an explicit `Apply` path so replay reconstructs the exact same aggregate state as the command path:

- `DevTravelOverrideForced` — records that a dev command set a pending override (category, optional foe profile fields, optional message). `Apply` sets `_pendingDevTravelOverride`.
- `DevTravelOverrideCleared` — records that a dev command cleared the pending override. `Apply` sets `_pendingDevTravelOverride = null`.
- `DevTravelOverrideConsumed` — records that the pending override was consumed by normal travel advancement. `Apply` sets `_pendingDevTravelOverride = null`. This is emitted by `PrepareTravelDayAdvance()` right before the `TravelDayAdvanced` event, in the same command execution.

The consumption event is necessary for replay safety. Without it, replaying `DevTravelOverrideForced → TravelDayAdvanced` would set the override on the `Forced` event and never clear it, leaving a stale pending override in the rehydrated session. With the explicit `DevTravelOverrideConsumed` event, replay of `Forced → Consumed → TravelDayAdvanced` reconstructs the correct final state: override set, then cleared, then the normal travel day applied. The `TravelDayAdvanced` event remains the normal gameplay outcome event and is unchanged — it does not know or care whether the day plan was dev-forced or generator-produced.

### Q7: How will normal travel events record that a generated thing came from dev-forced state without treating dev command intent as the gameplay outcome?

The `TravelDayAdvanced` event carries the ABSOLUTE `JourneySnapshot` (which includes `CurrentDayPlan`) and `DayOutcome` — these are the gameplay facts. The dev override only controls *what* `TravelDayPlanGenerator` would have produced; the actual day plan, encounter, and resolution are still normal gameplay events. The normal events do not carry a "wasForced" flag — the event stream speaks for itself: `DevTravelOverrideForced → DevTravelOverrideConsumed → TravelDayAdvanced` with the forced shape is the audit trail. The `DevTravelOverrideConsumed` event is dev-only and does not affect gameplay state beyond clearing the pending override; it is not a gameplay outcome.

### Q8: Where should dev-only travel query/command endpoints live?

Under `/api/dev/` in `src/WildBunch.Api/Dev/DevEndpoints.cs`, following the BUNCH-88 pattern. New endpoints:
- `GET /api/dev/sessions/{id}/travel-context` — dev query returning journey internals, pending encounter, foe profile, and current dev override state.
- `POST /api/dev/sessions/{id}/travel/force-override` — dev command to force the next travel result/profile.
- `POST /api/dev/sessions/{id}/travel/clear-override` — dev command to clear the pending override.

Application handlers live in `src/WildBunch.Application/Dev/` (Queries and Commands subdirectories), mirroring the existing `GetSessionAuditHandler` pattern.

### Q9: Which normal player APIs must remain clean of travel internals and hidden/dev state?

All endpoints under `/api/games/` — `GameSessionEndpoints`, `TravelEndpoints`, `ProjectionEndpoints`, `InvestigationEndpoints`, `ActionEndpoints`. The `GameSessionDto`, `GameTurnResultDto`, `TravelPreviewDto`, and projection DTOs must not gain dev override fields. The existing `GameApiHiddenTruthTests` guard the player boundary. Dev override state is internal to `GameSession` and only exposed through `/api/dev/` DTOs.

### Q10: What tests prove no-override behavior is unchanged?

- Existing `AdvanceTravelDayHandlerTests` continue to pass unchanged (no override active).
- New domain test: `AdvanceJourneyDay_WithNoDevOverride_UsesGeneratorOutput` — characterization test proving the day plan matches `TravelDayPlanGenerator.Generate` output.
- New domain test: `AdvanceJourneyDay_WithDevOverride_ConsumesOverrideOnce` — proves the forced plan is used, `DevTravelOverrideConsumed` is emitted, and the override is cleared after.
- New domain test: `AdvanceJourneyDay_AfterConsumedOverride_ResumesNormalGeneration` — proves the next advance uses normal generation.
- Replay test: `RehydrateFromEvents_WithDevOverrideForced_ReconstructsOverrideState` — proves the override is reconstructed from the `Forced` event alone.
- **Replay-after-consumption test: `RehydrateFromEvents_AfterConsumption_HasNoPendingOverride`** — proves that replaying `Forced → Consumed → TravelDayAdvanced` rehydrates a session with `_pendingDevTravelOverride = null`. This is the critical replay-safety proof: the consumed event clears the override on replay just as the command path clears it during execution.
- **No-override replay test: `RehydrateFromEvents_WithNoDevOverride_HasNoPendingOverride`** — proves that a normal event stream without dev events rehydrates with no pending override (does not accidentally clear unrelated future state).

### Q11: What event-stream proof will demonstrate force -> advance/consume -> normal resolution?

A domain-level test that:
1. Starts a journey (emits `JourneyStarted`).
2. Forces a foe override (emits `DevTravelOverrideForced`).
3. Advances the day (emits `DevTravelOverrideConsumed` then `TravelDayAdvanced` with the forced foe encounter in the day plan, `DayOutcome = Interrupted`).
4. Verifies the override is consumed (aggregate state `_pendingDevTravelOverride` is null).
5. Resolves the encounter normally (emits `JourneyEncounterResolved`).
6. Verifies the event stream contains: `JourneyStarted`, `DevTravelOverrideForced`, `DevTravelOverrideConsumed`, `TravelDayAdvanced`, `JourneyEncounterResolved` — proving the dev force was an event, the consume was an event, the advance consumed it, and normal resolution followed.
7. **Rehydrates a fresh session from that event stream and verifies `_pendingDevTravelOverride` is null** — proving replay produces the same final state as the command path.

---

## File Structure

### Domain layer (src/WildBunch.Domain/)

| File | Responsibility |
|------|----------------|
| `Events/DevTravelOverrideForced.cs` | New typed domain event: dev forced a pending travel override |
| `Events/DevTravelOverrideCleared.cs` | New typed domain event: dev cleared the pending travel override |
| `Events/DevTravelOverrideConsumed.cs` | New typed domain event: pending override was consumed by normal travel advancement |
| `Game/DevTravelOverride.cs` | New record: the pending dev override shape (category, foe profile fields, message) |
| `Game/GameSession.cs` (modify) | Add `_pendingDevTravelOverride` field, `ForceDevTravelOverride()` / `ClearDevTravelOverride()` command methods, `Apply(DevTravelOverrideForced)` / `Apply(DevTravelOverrideCleared)` / `Apply(DevTravelOverrideConsumed)` methods, emit `DevTravelOverrideConsumed` + consume override in `PrepareTravelDayAdvance()` |
| `Game/GameSessionEventReplay.cs` (modify) | Add dev event cases to `ApplyEvent` switch |
| `Game/GameSession.cs` `ApplyProducedEvent` (modify) | Add dev event cases to the produce-time dispatch switch |

### Application layer (src/WildBunch.Application/)

| File | Responsibility |
|------|----------------|
| `Dev/Models/TravelDevContextDto.cs` | New dev DTO: journey internals, pending encounter, foe profile, current override |
| `Dev/Models/ForceTravelOverrideDto.cs` | New dev DTO: request shape for forcing |
| `Dev/Queries/GetTravelDevContextQuery.cs` | New query record |
| `Dev/Queries/GetTravelDevContextHandler.cs` | New query handler: loads session, maps dev context |
| `Dev/Commands/ForceTravelOverrideCommand.cs` | New command record |
| `Dev/Commands/ForceTravelOverrideHandler.cs` | New command handler: load → aggregate command → store → commit |
| `Dev/Commands/ClearTravelOverrideCommand.cs` | New command record |
| `Dev/Commands/ClearTravelOverrideHandler.cs` | New command handler: load → aggregate command → store → commit |
| `Dev/Mapping/TravelDevContextMapper.cs` | New mapper: domain session → dev DTO (separate from player mappers) |

### API layer (src/WildBunch.Api/)

| File | Responsibility |
|------|----------------|
| `Dev/DevEndpoints.cs` (modify) | Add 3 new dev endpoints: travel-context query, force-override POST, clear-override POST |
| `DependencyInjection.cs` (modify) | Register new dev handlers |

### Persistence layer (src/WildBunch.Persistence/)

| File | Responsibility |
|------|----------------|
| `Serialization/GameSessionJsonSerializer.Events.cs` (modify) | Add 3 new event types to `ResolveEventType` switch |
| `Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` (modify) | Add `PendingDevTravelOverride` to snapshot record and `FromDomain`/`ToDomain` |
| `Serialization/GameSessionRehydrator.cs` (modify) | Add override field to `Create` method if needed (or set via snapshot ToDomain) |

### Frontend (src/WildBunch.Web/)

| File | Responsibility |
|------|----------------|
| `src/dev/types.ts` (modify) | Add `TravelDevContextDto`, `ForceTravelOverrideRequestDto` types |
| `src/dev/devApi.ts` (modify) | Add `getTravelDevContext`, `forceTravelOverride`, `clearTravelOverride` functions |
| `src/dev/panels/TravelDevPanel.tsx` | New panel: shows journey/encounter internals + force/clear controls |
| `src/dev/DevPanelRegistry.tsx` (modify) | Register `TravelDevPanel` |

### Tests

| File | Responsibility |
|------|----------------|
| `tests/WildBunch.Domain.Tests/DevTravelOverrideTests.cs` | Domain tests: force, clear, consume-once, no-override unchanged, replay |
| `tests/WildBunch.Application.Tests/Dev/GetTravelDevContextHandlerTests.cs` | Query handler tests |
| `tests/WildBunch.Application.Tests/Dev/ForceTravelOverrideHandlerTests.cs` | Command handler tests |
| `tests/WildBunch.Application.Tests/Dev/ClearTravelOverrideHandlerTests.cs` | Command handler tests |
| `tests/WildBunch.Integration.Tests/Dev/DevTravelEndpointTests.cs` | Integration tests: 200/403/404 for dev travel endpoints |
| `src/WildBunch.Web/src/tests/TravelDevPanel.test.tsx` | Panel render + mutation tests |

### Documentation

| File | Responsibility |
|------|----------------|
| `docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md` (modify) | Add dated status entry for BUNCH-89 travel dev controls |
| `docs/adr/INDEX.md` (modify) | Update ADR-0030 last-checked timestamp |

---

## Task 1: Domain — DevTravelOverride record + dev events

**Files:**
- Create: `src/WildBunch.Domain/Game/DevTravelOverride.cs`
- Create: `src/WildBunch.Domain/Events/DevTravelOverrideForced.cs`
- Create: `src/WildBunch.Domain/Events/DevTravelOverrideCleared.cs`
- Create: `src/WildBunch.Domain/Events/DevTravelOverrideConsumed.cs`

**Interfaces:**
- Produces: `DevTravelOverride` record, `DevTravelOverrideForced` event, `DevTravelOverrideCleared` event, `DevTravelOverrideConsumed` event — consumed by Task 2 (GameSession) and Task 5 (persistence serializer).

- [ ] **Step 1: Write the DevTravelOverride record**

```csharp
// src/WildBunch.Domain/Game/DevTravelOverride.cs
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Game;

/// <summary>
/// Pending dev override for the next travel-day generation.
/// When present, AdvanceJourneyDay uses this instead of calling TravelDayPlanGenerator.
/// Consumed once by the next advance, then cleared from aggregate state.
/// This is dev-only session state, not player-facing. See BUNCH-89.
/// </summary>
public sealed record DevTravelOverride(
    TravelDayEncounterCategory ForcedCategory,
    JourneyFoeProfile? FoeProfile,
    string? EncounterMessage)
{
    public static DevTravelOverride ForFoe(JourneyFoeProfile foeProfile, string? encounterMessage = null)
        => new(TravelDayEncounterCategory.Foe, foeProfile, encounterMessage);

    public static DevTravelOverride ForCategory(TravelDayEncounterCategory category, string? encounterMessage = null)
        => new(category, null, encounterMessage);
}
```

- [ ] **Step 2: Write the DevTravelOverrideForced event**

```csharp
// src/WildBunch.Domain/Events/DevTravelOverrideForced.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command forced a pending travel override.
/// This is a dev-only event — it records dev intent, not a gameplay outcome.
/// The override is consumed by the next TravelDayAdvanced event.
/// See BUNCH-89 and ADR-0030.
/// </summary>
public sealed record DevTravelOverrideForced : IDomainEvent
{
    public required TravelDayEncounterCategory ForcedCategory { get; init; }
    public JourneyFoeProfile? FoeProfile { get; init; }
    public string? EncounterMessage { get; init; }
}
```

- [ ] **Step 3: Write the DevTravelOverrideCleared event**

```csharp
// src/WildBunch.Domain/Events/DevTravelOverrideCleared.cs
namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command cleared the pending travel override.
/// Dev-only event. See BUNCH-89 and ADR-0030.
/// </summary>
public sealed record DevTravelOverrideCleared : IDomainEvent;
```

- [ ] **Step 4: Write the DevTravelOverrideConsumed event**

```csharp
// src/WildBunch.Domain/Events/DevTravelOverrideConsumed.cs
namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the pending dev travel override was consumed by normal travel advancement.
/// Emitted by PrepareTravelDayAdvance() right before the TravelDayAdvanced event,
/// in the same command execution. Apply clears _pendingDevTravelOverride.
/// This event makes replay safe: replaying Forced -> Consumed -> TravelDayAdvanced
/// reconstructs the correct final state with no pending override.
/// Dev-only event — not a gameplay outcome. See BUNCH-89 and ADR-0030.
/// </summary>
public sealed record DevTravelOverrideConsumed : IDomainEvent;
```

- [ ] **Step 5: Build to verify compilation**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: Build succeeds (new files compile, no references yet).

- [ ] **Step 6: Commit**

```powershell
git add src/WildBunch.Domain/Game/DevTravelOverride.cs src/WildBunch.Domain/Events/DevTravelOverrideForced.cs src/WildBunch.Domain/Events/DevTravelOverrideCleared.cs src/WildBunch.Domain/Events/DevTravelOverrideConsumed.cs
git commit -m "BUNCH-89: add DevTravelOverride record and dev domain events"
```

---

## Task 2: Domain — GameSession override state, command methods, Apply, consume-once

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` — add field, properties, command methods, Apply methods, consume logic
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` — add dev event cases to `ApplyEvent`
- Test: `tests/WildBunch.Domain.Tests/DevTravelOverrideTests.cs`

**Interfaces:**
- Consumes: `DevTravelOverride`, `DevTravelOverrideForced`, `DevTravelOverrideCleared`, `DevTravelOverrideConsumed` from Task 1
- Produces: `GameSession.ForceDevTravelOverride()`, `GameSession.ClearDevTravelOverride()`, `GameSession.PendingDevTravelOverride` property, `Apply(DevTravelOverrideForced)`, `Apply(DevTravelOverrideCleared)`, `Apply(DevTravelOverrideConsumed)` — consumed by Task 3 (handlers), Task 5 (persistence), Task 7 (frontend query).

**Application-layer access strategy:** `PendingDevTravelOverride` is `internal` on `GameSession`. The repo already has `[assembly: InternalsVisibleTo("WildBunch.Application")]` in `src/WildBunch.Domain/Properties/AssemblyInfo.cs` (line 4), so the Application dev mapper can read it directly. No new `InternalsVisibleTo` attribute or public accessor is needed. This is settled — not a mid-flight discovery point.

- [ ] **Step 1: Write failing domain tests**

Create `tests/WildBunch.Domain.Tests/DevTravelOverrideTests.cs`. Use the existing `TestSessionFactory` pattern to create a session with an active journey, then test force/clear/consume.

```csharp
// tests/WildBunch.Domain.Tests/DevTravelOverrideTests.cs
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.Tests;

namespace WildBunch.Domain.Tests;

public sealed class DevTravelOverrideTests
{
    [Fact]
    public void ForceDevTravelOverride_ProducesEvent_AndSetsPendingOverride()
    {
        var session = TestSessionFactory.CreateWithActiveJourney();
        var foeProfile = new JourneyFoeProfile(Speed: 5, FightStrength: 4, MinimumBribe: 8m);

        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(foeProfile, "A hard-eyed rider blocks the trail."));

        var forcedEvent = Assert.Single(session.UncommittedEvents.OfType<DevTravelOverrideForced>());
        Assert.Equal(TravelDayEncounterCategory.Foe, forcedEvent.ForcedCategory);
        Assert.NotNull(forcedEvent.FoeProfile);
        Assert.Equal(5, forcedEvent.FoeProfile!.Speed);
        Assert.NotNull(session.PendingDevTravelOverride);
    }

    [Fact]
    public void ClearDevTravelOverride_ProducesEvent_AndClearsPendingOverride()
    {
        var session = TestSessionFactory.CreateWithActiveJourney();
        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Lucky));

        session.ClearDevTravelOverride();

        Assert.Single(session.UncommittedEvents.OfType<DevTravelOverrideCleared>());
        Assert.Null(session.PendingDevTravelOverride);
    }

    [Fact]
    public void AdvanceJourneyDay_WithDevOverride_ConsumesOverrideOnce()
    {
        var session = TestSessionFactory.CreateWithActiveJourney();
        var foeProfile = new JourneyFoeProfile(Speed: 5, FightStrength: 4, MinimumBribe: 8m);
        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(foeProfile, "A hard-eyed rider blocks the trail."));
        session.MarkEventsCommitted();

        session.AdvanceJourneyDay();

        // DevTravelOverrideConsumed event was emitted
        Assert.Single(session.UncommittedEvents.OfType<DevTravelOverrideConsumed>());
        // Override consumed after advance
        Assert.Null(session.PendingDevTravelOverride);
        // Journey interrupted by the forced foe encounter
        Assert.Equal(JourneyStatus.Interrupted, session.Journey!.Status);
        Assert.NotNull(session.Journey.PendingEncounter);
        Assert.Equal("foe", session.Journey.PendingEncounter!.Kind);
    }

    [Fact]
    public void AdvanceJourneyDay_AfterConsumedOverride_ResumesNormalGeneration()
    {
        var session = TestSessionFactory.CreateWithActiveJourney();
        var foeProfile = new JourneyFoeProfile(Speed: 5, FightStrength: 4, MinimumBribe: 8m);
        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(foeProfile));
        session.MarkEventsCommitted();
        session.AdvanceJourneyDay();
        // Resolve the encounter to continue
        session.ResolveJourneyEncounter("run", forcedRoll: ulong.MaxValue);
        session.MarkEventsCommitted();

        // Next advance should use normal generation (no override)
        var result = session.AdvanceJourneyDay();
        Assert.Null(session.PendingDevTravelOverride);
        // No new DevTravelOverrideConsumed event (override was already consumed)
        Assert.Empty(session.UncommittedEvents.OfType<DevTravelOverrideConsumed>());
        // The result should be a normal advance (not forced)
        Assert.True(result.Success || session.Journey!.Status == JourneyStatus.Interrupted);
    }

    [Fact]
    public void AdvanceJourneyDay_WithNoDevOverride_UsesGeneratorOutput()
    {
        var session = TestSessionFactory.CreateWithActiveJourney();

        session.AdvanceJourneyDay();

        Assert.Null(session.PendingDevTravelOverride);
        // No dev events in the stream
        Assert.Empty(session.UncommittedEvents.OfType<DevTravelOverrideForced>());
        Assert.Empty(session.UncommittedEvents.OfType<DevTravelOverrideConsumed>());
    }

    [Fact]
    public void RehydrateFromEvents_WithDevOverrideForced_ReconstructsOverrideState()
    {
        var session = TestSessionFactory.CreateWithActiveJourney();
        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe));
        session.MarkEventsCommitted();

        var events = session.AllEvents;
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, session.CaseFile, events);

        Assert.NotNull(rehydrated.PendingDevTravelOverride);
        Assert.Equal(TravelDayEncounterCategory.Foe, rehydrated.PendingDevTravelOverride!.ForcedCategory);
    }

    [Fact]
    public void RehydrateFromEvents_AfterConsumption_HasNoPendingOverride()
    {
        var session = TestSessionFactory.CreateWithActiveJourney();
        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(
            new JourneyFoeProfile(Speed: 5, FightStrength: 4, MinimumBribe: 8m)));
        session.MarkEventsCommitted();
        session.AdvanceJourneyDay();
        session.MarkEventsCommitted();

        // Rehydrate from the full event stream: Forced -> Consumed -> TravelDayAdvanced
        var events = session.AllEvents;
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, session.CaseFile, events);

        // Critical replay-safety proof: override is null after replay
        Assert.Null(rehydrated.PendingDevTravelOverride);
    }

    [Fact]
    public void RehydrateFromEvents_WithNoDevOverride_HasNoPendingOverride()
    {
        var session = TestSessionFactory.CreateWithActiveJourney();
        session.AdvanceJourneyDay();
        session.MarkEventsCommitted();

        var events = session.AllEvents;
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, session.CaseFile, events);

        Assert.Null(rehydrated.PendingDevTravelOverride);
    }
}
```

Note: `TestSessionFactory.CreateWithActiveJourney()` may need to be added if it does not exist. Check existing factory methods first; if a journey-started session helper exists, use it. Otherwise add a helper that calls `StartJourney` with a valid preview.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~DevTravelOverrideTests"`
Expected: FAIL — `ForceDevTravelOverride` does not exist, `PendingDevTravelOverride` does not exist.

- [ ] **Step 3: Add override field and properties to GameSession**

In `src/WildBunch.Domain/Game/GameSession.cs`, add near the other private fields (around line 39):

```csharp
private DevTravelOverride? _pendingDevTravelOverride;
```

Add a public property near `Journey` (around line 107):

```csharp
/// <summary>
/// Pending dev override for the next travel-day generation. Dev-only state.
/// Consumed once by the next AdvanceJourneyDay. See BUNCH-89.
/// </summary>
internal DevTravelOverride? PendingDevTravelOverride => _pendingDevTravelOverride;
```

Use `internal` so the Application dev handler can read it for the dev query DTO, but it is not exposed on any player DTO.

- [ ] **Step 4: Add command methods to GameSession**

Add after `AdvanceJourneyDay()` (around line 834):

```csharp
/// <summary>
/// Dev command: forces the next travel-day generation to use the given override.
/// Produces a DevTravelOverrideForced event. The override is consumed once by
/// the next AdvanceJourneyDay. See BUNCH-89.
/// </summary>
public void ForceDevTravelOverride(DevTravelOverride overrideValue)
{
    ArgumentNullException.ThrowIfNull(overrideValue);
    if (Journey is null || Journey.Status != JourneyStatus.Active)
    {
        throw new InvalidOperationException("Cannot force a travel override without an active journey.");
    }
    if (Journey.PendingEncounter is not null)
    {
        throw new InvalidOperationException("Cannot force a travel override while an encounter is pending.");
    }

    ProduceEvent(new DevTravelOverrideForced
    {
        ForcedCategory = overrideValue.ForcedCategory,
        FoeProfile = overrideValue.FoeProfile,
        EncounterMessage = overrideValue.EncounterMessage
    });
}

/// <summary>
/// Dev command: clears any pending travel override.
/// Produces a DevTravelOverrideCleared event. See BUNCH-89.
/// </summary>
public void ClearDevTravelOverride()
{
    if (_pendingDevTravelOverride is null)
    {
        return; // No-op if nothing to clear — idempotent
    }

    ProduceEvent(new DevTravelOverrideCleared());
}
```

- [ ] **Step 5: Add Apply methods for dev events**

Add near the other Apply methods (after `Apply(JourneyArrivalAcknowledged)` or near the end of the Apply block):

```csharp
/// <summary>
/// Applies a DevTravelOverrideForced event. Sets the pending dev override.
/// Dev-only event — does not affect gameplay state directly. See BUNCH-89.
/// </summary>
internal void Apply(DevTravelOverrideForced e)
{
    _pendingDevTravelOverride = new DevTravelOverride(
        e.ForcedCategory,
        e.FoeProfile,
        e.EncounterMessage);
    _version++;
}

/// <summary>
/// Applies a DevTravelOverrideCleared event. Clears the pending dev override.
/// Dev-only event. See BUNCH-89.
/// </summary>
internal void Apply(DevTravelOverrideCleared e)
{
    _pendingDevTravelOverride = null;
    _version++;
}

/// <summary>
/// Applies a DevTravelOverrideConsumed event. Clears the pending dev override.
/// This is the replay-safe consumption path: replaying Forced -> Consumed ->
/// TravelDayAdvanced reconstructs the correct final state with no pending override.
/// Dev-only event — not a gameplay outcome. See BUNCH-89.
/// </summary>
internal void Apply(DevTravelOverrideConsumed e)
{
    _pendingDevTravelOverride = null;
    _version++;
}
```

- [ ] **Step 6: Add dev event cases to ApplyProducedEvent**

In `src/WildBunch.Domain/Game/GameSession.cs`, in the `ApplyProducedEvent` switch (around line 347), add before the `default` case:

```csharp
case DevTravelOverrideForced dtf:
    Apply(dtf);
    break;
case DevTravelOverrideCleared dtc:
    Apply(dtc);
    break;
case DevTravelOverrideConsumed dtc2:
    Apply(dtc2);
    break;
```

- [ ] **Step 7: Add dev event cases to GameSessionEventReplay.ApplyEvent**

In `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`, in the `ApplyEvent` switch (around line 130), add before the `default` case:

```csharp
case DevTravelOverrideForced dtf:
    session.Apply(dtf);
    break;
case DevTravelOverrideCleared dtc:
    session.Apply(dtc);
    break;
case DevTravelOverrideConsumed dtc2:
    session.Apply(dtc2);
    break;
```

- [ ] **Step 8: Consume the override in PrepareTravelDayAdvance with an explicit consumed event**

In `src/WildBunch.Domain/Game/GameSession.cs`, in `PrepareTravelDayAdvance()` (around line 1172-1173), replace:

```csharp
var generationContext = CreateTravelDayGenerationContext(TravelDayPlanGenerator.CurrentVersion);
Journey.SetCurrentDayPlan(TravelDayPlanGenerator.Generate(generationContext));
```

with:

```csharp
TravelDayPlanState dayPlan;
if (_pendingDevTravelOverride is not null)
{
    // Dev override is active: produce the consumed event (replay-safe clear),
    // then use the forced day plan instead of the generator.
    ProduceEvent(new DevTravelOverrideConsumed());
    dayPlan = TravelDayPlanFactory.CreateForcedDayPlan(_pendingDevTravelOverride, Journey.DaysTravelled, TravelRules);
}
else
{
    dayPlan = TravelDayPlanGenerator.Generate(CreateTravelDayGenerationContext(TravelDayPlanGenerator.CurrentVersion));
}
Journey.SetCurrentDayPlan(dayPlan);
```

Note: `ProduceEvent` calls `Apply(DevTravelOverrideConsumed)` which sets `_pendingDevTravelOverride = null` and increments `_version`. This is the single mutation path — the field is not cleared imperatively outside the event apply path. The `DevTravelOverrideConsumed` event is emitted before the `TravelDayAdvanced` event in the same command execution, so the event stream order is `... DevTravelOverrideConsumed, TravelDayAdvanced ...` and replay produces the same state.

- [ ] **Step 9: Create TravelDayPlanFactory helper**

Create `src/WildBunch.Domain/Travel/TravelDayPlanFactory.cs`:

```csharp
namespace WildBunch.Domain.Travel;

/// <summary>
/// Creates a TravelDayPlanState from a dev override, bypassing the generator.
/// The forced plan contains a single encounter matching the override category.
/// See BUNCH-89.
/// </summary>
internal static class TravelDayPlanFactory
{
    public static TravelDayPlanState CreateForcedDayPlan(
        DevTravelOverride overrideValue,
        int dayNumber,
        TravelRulesProfile travelRules)
    {
        ArgumentNullException.ThrowIfNull(overrideValue);

        var encounter = CreateForcedEncounter(overrideValue, dayNumber, travelRules);
        return new TravelDayPlanState(
            dayNumber,
            new[] { encounter },
            CurrentEncounterIndex: 0,
            IsComplete: false);
    }

    private static TravelDayEncounterState CreateForcedEncounter(
        DevTravelOverride overrideValue,
        int slotIndex,
        TravelRulesProfile travelRules)
    {
        var message = overrideValue.EncounterMessage ?? BuildDefaultMessage(overrideValue.ForcedCategory);

        return overrideValue.ForcedCategory switch
        {
            TravelDayEncounterCategory.Foe when overrideValue.FoeProfile is { } foeProfile =>
                new TravelDayEncounterState(
                    slotIndex,
                    TravelDayEncounterCategory.Foe,
                    "Hard-eyed rider",
                    message,
                    TrailEvent: null,
                    PendingEncounter: JourneyEncounterState.CreateFoe(message, foeProfile),
                    Resolution: null),
            TravelDayEncounterCategory.Foe =>
                new TravelDayEncounterState(
                    slotIndex,
                    TravelDayEncounterCategory.Foe,
                    "Hard-eyed rider",
                    message,
                    TrailEvent: null,
                    PendingEncounter: JourneyEncounterState.CreateFoe(
                        message,
                        new JourneyFoeProfile(Speed: 3, FightStrength: 3, MinimumBribe: travelRules.EncounterBribeCash)),
                    Resolution: null),
            TravelDayEncounterCategory.Quiet =>
                new TravelDayEncounterState(
                    slotIndex,
                    TravelDayEncounterCategory.Quiet,
                    "Quiet trail",
                    message,
                    TrailEvent: null,
                    PendingEncounter: null,
                    Resolution: null),
            _ =>
                new TravelDayEncounterState(
                    slotIndex,
                    overrideValue.ForcedCategory,
                    BuildDefaultTitle(overrideValue.ForcedCategory),
                    message,
                    TrailEvent: null,
                    PendingEncounter: JourneyEncounterState.CreateChoiceEncounter(
                        overrideValue.ForcedCategory.ToString().ToLowerInvariant(),
                        message),
                    Resolution: null)
        };
    }

    private static string BuildDefaultMessage(TravelDayEncounterCategory category) => category switch
    {
        TravelDayEncounterCategory.Foe => "A hard-eyed rider cuts across my path.",
        TravelDayEncounterCategory.Npc => "A weathered stranger hails me from the trail.",
        TravelDayEncounterCategory.Lucky => "I spot something glinting by the trail.",
        TravelDayEncounterCategory.Unlucky => "The trail takes a bad turn.",
        TravelDayEncounterCategory.Environmental => "The weather turns rough on the trail.",
        TravelDayEncounterCategory.Resource => "I come across a cache of supplies.",
        TravelDayEncounterCategory.HorseTrouble => "My horse is acting up on the trail.",
        _ => "The trail is quiet."
    };

    private static string BuildDefaultTitle(TravelDayEncounterCategory category) => category switch
    {
        TravelDayEncounterCategory.Foe => "Hard-eyed rider",
        TravelDayEncounterCategory.Npc => "Weathered stranger",
        TravelDayEncounterCategory.Lucky => "Lucky find",
        TravelDayEncounterCategory.Unlucky => "Bad turn",
        TravelDayEncounterCategory.Environmental => "Rough weather",
        TravelDayEncounterCategory.Resource => "Supply cache",
        TravelDayEncounterCategory.HorseTrouble => "Horse trouble",
        _ => "Quiet trail"
    };
}
```

Note: This factory needs `using WildBunch.Domain.Game;` for `DevTravelOverride`. The field `TravelRulesProfile.EncounterBribeCash` is confirmed (verified in `src/WildBunch.Domain/Travel/JourneyEncounterResolutionEngine.cs:86`).

- [ ] **Step 10: Add test session factory helper if needed**

Check `tests/WildBunch.Domain.Tests/TestSessionFactory.cs` for an existing method that returns a session with an active journey. If none exists, add:

```csharp
public static GameSession CreateWithActiveJourney()
{
    var session = CreateDefault();
    // Start a journey to a connected town using the world's travel resolver
    // Use the same pattern as existing travel tests that call StartJourney
    // Adjust based on the actual TestSessionFactory.CreateDefault() world shape
    // ...
    return session;
}
```

Inspect existing travel test helpers in `AdvanceTravelDayHandlerTests.cs` (the `CreateEasyLuckyFoodSession` / `CreateHighRiskSession` methods) and replicate the journey-start pattern at the domain level.

- [ ] **Step 11: Run domain tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~DevTravelOverrideTests"`
Expected: PASS — all 5 tests green.

- [ ] **Step 12: Run full domain test suite to verify no regressions**

Run: `dotnet test tests/WildBunch.Domain.Tests`
Expected: All existing tests still pass.

- [ ] **Step 13: Commit**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSessionEventReplay.cs src/WildBunch.Domain/Travel/TravelDayPlanFactory.cs tests/WildBunch.Domain.Tests/DevTravelOverrideTests.cs tests/WildBunch.Domain.Tests/TestSessionFactory.cs
git commit -m "BUNCH-89: add GameSession dev override state, command methods, consume-once"
```

---

## Task 3: Application — Dev DTOs, query, command handlers

**Files:**
- Create: `src/WildBunch.Application/Dev/Models/TravelDevContextDto.cs`
- Create: `src/WildBunch.Application/Dev/Models/ForceTravelOverrideRequestDto.cs`
- Create: `src/WildBunch.Application/Dev/Queries/GetTravelDevContextQuery.cs`
- Create: `src/WildBunch.Application/Dev/Queries/GetTravelDevContextHandler.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ForceTravelOverrideCommand.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ForceTravelOverrideHandler.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ClearTravelOverrideCommand.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ClearTravelOverrideHandler.cs`
- Create: `src/WildBunch.Application/Dev/Mapping/TravelDevContextMapper.cs`
- Test: `tests/WildBunch.Application.Tests/Dev/GetTravelDevContextHandlerTests.cs`
- Test: `tests/WildBunch.Application.Tests/Dev/ForceTravelOverrideHandlerTests.cs`
- Test: `tests/WildBunch.Application.Tests/Dev/ClearTravelOverrideHandlerTests.cs`

**Interfaces:**
- Consumes: `GameSession.PendingDevTravelOverride`, `GameSession.ForceDevTravelOverride()`, `GameSession.ClearDevTravelOverride()` from Task 2
- Produces: `GetTravelDevContextHandler`, `ForceTravelOverrideHandler`, `ClearTravelOverrideHandler`, dev DTOs — consumed by Task 4 (API endpoints).

- [ ] **Step 1: Write dev DTOs**

```csharp
// src/WildBunch.Application/Dev/Models/TravelDevContextDto.cs
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Models;

public sealed record TravelDevContextDto(
    Guid SessionId,
    bool HasActiveJourney,
    string? JourneyStatus,
    int? DaysTravelled,
    int? RemainingDays,
    string? PendingEncounterKind,
    string? PendingEncounterMessage,
    FoeProfileDevDto? PendingFoeProfile,
    DevOverrideDto? PendingDevOverride);

public sealed record FoeProfileDevDto(
    int Speed,
    int FightStrength,
    decimal MinimumBribe,
    string SpeedBand,
    string FightBand,
    string BribeBand);

public sealed record DevOverrideDto(
    string ForcedCategory,
    FoeProfileDevDto? FoeProfile,
    string? EncounterMessage);
```

```csharp
// src/WildBunch.Application/Dev/Models/ForceTravelOverrideRequestDto.cs
namespace WildBunch.Application.Dev.Models;

public sealed record ForceTravelOverrideRequestDto(
    string ForcedCategory,
    int? FoeSpeed,
    int? FoeFightStrength,
    decimal? FoeMinimumBribe,
    string? EncounterMessage);
```

- [ ] **Step 2: Write the dev context mapper**

```csharp
// src/WildBunch.Application/Dev/Mapping/TravelDevContextMapper.cs
using WildBunch.Application.Dev.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Mapping;

public static class TravelDevContextMapper
{
    public static TravelDevContextDto ToDto(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var journey = session.Journey;
        var pendingEncounter = journey?.PendingEncounter;
        var foeProfile = pendingEncounter?.FoeProfile;
        var devOverride = session.PendingDevTravelOverride;

        return new TravelDevContextDto(
            session.Id.Value,
            HasActiveJourney: journey is not null,
            JourneyStatus: journey?.Status.ToString(),
            DaysTravelled: journey?.DaysTravelled,
            RemainingDays: journey?.RemainingDays,
            PendingEncounterKind: pendingEncounter?.Kind,
            PendingEncounterMessage: pendingEncounter?.Message,
            PendingFoeProfile: foeProfile is null ? null : new FoeProfileDevDto(
                foeProfile.Speed,
                foeProfile.FightStrength,
                foeProfile.MinimumBribe,
                foeProfile.DescribeSpeedBand(),
                foeProfile.DescribeFightBand(),
                foeProfile.DescribeBribeBand()),
            PendingDevOverride: devOverride is null ? null : new DevOverrideDto(
                devOverride.ForcedCategory.ToString(),
                devOverride.FoeProfile is null ? null : new FoeProfileDevDto(
                    devOverride.FoeProfile.Speed,
                    devOverride.FoeProfile.FightStrength,
                    devOverride.FoeProfile.MinimumBribe,
                    devOverride.FoeProfile.DescribeSpeedBand(),
                    devOverride.FoeProfile.DescribeFightBand(),
                    devOverride.FoeProfile.DescribeBribeBand()),
                devOverride.EncounterMessage));
    }
}
```

- [ ] **Step 3: Write the query and handler**

```csharp
// src/WildBunch.Application/Dev/Queries/GetTravelDevContextQuery.cs
namespace WildBunch.Application.Dev.Queries;

public sealed record GetTravelDevContextQuery(Guid SessionId);
```

```csharp
// src/WildBunch.Application/Dev/Queries/GetTravelDevContextHandler.cs
using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Mapping;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Queries;

public sealed class GetTravelDevContextHandler
{
    private readonly IGameSessionRepository _repository;

    public GetTravelDevContextHandler(IGameSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TravelDevContextDto> HandleAsync(GetTravelDevContextQuery query, CancellationToken cancellationToken = default)
    {
        var sessionId = new GameSessionId(query.SessionId);
        var session = await _repository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        return TravelDevContextMapper.ToDto(session);
    }
}
```

- [ ] **Step 4: Write the force command and handler**

```csharp
// src/WildBunch.Application/Dev/Commands/ForceTravelOverrideCommand.cs
using WildBunch.Application.Dev.Models;

namespace WildBunch.Application.Dev.Commands;

public sealed record ForceTravelOverrideCommand(
    Guid GameSessionId,
    string ForcedCategory,
    int? FoeSpeed,
    int? FoeFightStrength,
    decimal? FoeMinimumBribe,
    string? EncounterMessage);
```

```csharp
// src/WildBunch.Application/Dev/Commands/ForceTravelOverrideHandler.cs
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Commands;

public sealed class ForceTravelOverrideHandler : GameSessionCommandHandler
{
    public ForceTravelOverrideHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ForceTravelOverrideCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            var category = Enum.Parse<TravelDayEncounterCategory>(command.ForcedCategory, ignoreCase: true);
            JourneyFoeProfile? foeProfile = null;
            if (command.FoeSpeed is not null || command.FoeFightStrength is not null || command.FoeMinimumBribe is not null)
            {
                foeProfile = new JourneyFoeProfile(
                    Speed: command.FoeSpeed ?? 3,
                    FightStrength: command.FoeFightStrength ?? 3,
                    MinimumBribe: command.FoeMinimumBribe ?? 5m);
            }

            var overrideValue = foeProfile is not null
                ? DevTravelOverride.ForFoe(foeProfile, command.EncounterMessage)
                : DevTravelOverride.ForCategory(category, command.EncounterMessage);

            session.ForceDevTravelOverride(overrideValue);
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 5: Write the clear command and handler**

```csharp
// src/WildBunch.Application/Dev/Commands/ClearTravelOverrideCommand.cs
namespace WildBunch.Application.Dev.Commands;

public sealed record ClearTravelOverrideCommand(Guid GameSessionId);
```

```csharp
// src/WildBunch.Application/Dev/Commands/ClearTravelOverrideHandler.cs
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Commands;

public sealed class ClearTravelOverrideHandler : GameSessionCommandHandler
{
    public ClearTravelOverrideHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ClearTravelOverrideCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            session.ClearDevTravelOverride();
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 6: Write failing application tests**

Create test files following the existing `GetSessionAuditHandlerTests.cs` pattern. Use `InMemoryGameSessionRepository` and `TestSessionFactory`.

```csharp
// tests/WildBunch.Application.Tests/Dev/GetTravelDevContextHandlerTests.cs
using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Tests;

namespace WildBunch.Application.Tests.Dev;

public sealed class GetTravelDevContextHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsJourneyContext_WhenSessionHasActiveJourney()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = TestSessionFactory.CreateWithActiveJourney();
        repository.Seed(session);
        var handler = new GetTravelDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetTravelDevContextQuery(session.Id.Value));

        Assert.True(result.HasActiveJourney);
        Assert.NotNull(result.JourneyStatus);
    }

    [Fact]
    public async Task HandleAsync_ThrowsWhenSessionDoesNotExist()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new GetTravelDevContextHandler(repository);

        await Assert.ThrowsAsync<GameSessionNotFoundException>(() =>
            handler.HandleAsync(new GetTravelDevContextQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_ReturnsDevOverride_WhenOverrideIsPending()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = TestSessionFactory.CreateWithActiveJourney();
        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(
            WildBunch.Domain.Travel.TravelDayEncounterCategory.Foe));
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new GetTravelDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetTravelDevContextQuery(session.Id.Value));

        Assert.NotNull(result.PendingDevOverride);
        Assert.Equal("Foe", result.PendingDevOverride.ForcedCategory);
    }
}
```

Write similar test files for `ForceTravelOverrideHandlerTests` and `ClearTravelOverrideHandlerTests` that verify the handlers produce events and mutate state through the repository.

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~Dev.*Travel"`
Expected: PASS

- [ ] **Step 8: Commit**

```powershell
git add src/WildBunch.Application/Dev/ tests/WildBunch.Application.Tests/Dev/
git commit -m "BUNCH-89: add dev travel context query and force/clear command handlers"
```

---

## Task 4: API — Dev travel endpoints + DI registration

**Files:**
- Modify: `src/WildBunch.Api/Dev/DevEndpoints.cs` — add 3 endpoints
- Modify: `src/WildBunch.Api/DependencyInjection.cs` — register 3 handlers
- Test: `tests/WildBunch.Integration.Tests/Dev/DevTravelEndpointTests.cs`

**Interfaces:**
- Consumes: handlers from Task 3
- Produces: `GET /api/dev/sessions/{id}/travel-context`, `POST /api/dev/sessions/{id}/travel/force-override`, `POST /api/dev/sessions/{id}/travel/clear-override`

- [ ] **Step 1: Add endpoints to DevEndpoints.cs**

Add to the `MapDevEndpoints` method, after the existing audit endpoint:

```csharp
dev.MapGet("/sessions/{id:guid}/travel-context", GetTravelDevContextAsync)
    .WithName("GetTravelDevContext")
    .Produces<TravelDevContextDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound);

dev.MapPost("/sessions/{id:guid}/travel/force-override", ForceTravelOverrideAsync)
    .WithName("ForceTravelOverride")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status400BadRequest);

dev.MapPost("/sessions/{id:guid}/travel/clear-override", ClearTravelOverrideAsync)
    .WithName("ClearTravelOverride")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound);
```

Add the endpoint handler methods following the existing `GetSessionAuditAsync` pattern (guard → handler → catch DevAccessDeniedException/GameSessionNotFoundException):

```csharp
private static async Task<IResult> GetTravelDevContextAsync(
    Guid id,
    DevRoleGuard guard,
    GetTravelDevContextHandler handler,
    CancellationToken cancellationToken)
{
    try
    {
        guard.EnsureDevAccess();
        var result = await handler.HandleAsync(new GetTravelDevContextQuery(id), cancellationToken);
        return Results.Ok(result);
    }
    catch (DevAccessDeniedException)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
    catch (GameSessionNotFoundException)
    {
        return Results.NotFound();
    }
}

private static async Task<IResult> ForceTravelOverrideAsync(
    Guid id,
    DevRoleGuard guard,
    ForceTravelOverrideHandler handler,
    ForceTravelOverrideRequestDto request,
    CancellationToken cancellationToken)
{
    try
    {
        guard.EnsureDevAccess();
        if (string.IsNullOrWhiteSpace(request.ForcedCategory))
        {
            return Results.BadRequest("ForcedCategory is required.");
        }
        await handler.HandleAsync(new ForceTravelOverrideCommand(
            id, request.ForcedCategory, request.FoeSpeed,
            request.FoeFightStrength, request.FoeMinimumBribe, request.EncounterMessage),
            cancellationToken);
        return Results.NoContent();
    }
    catch (DevAccessDeniedException)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
    catch (GameSessionNotFoundException)
    {
        return Results.NotFound();
    }
    catch (ArgumentException)
    {
        return Results.BadRequest("Invalid ForcedCategory value.");
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
}

private static async Task<IResult> ClearTravelOverrideAsync(
    Guid id,
    DevRoleGuard guard,
    ClearTravelOverrideHandler handler,
    CancellationToken cancellationToken)
{
    try
    {
        guard.EnsureDevAccess();
        await handler.HandleAsync(new ClearTravelOverrideCommand(id), cancellationToken);
        return Results.NoContent();
    }
    catch (DevAccessDeniedException)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
    catch (GameSessionNotFoundException)
    {
        return Results.NotFound();
    }
}
```

Add the necessary `using` directives for the new Application.Dev types.

- [ ] **Step 2: Register handlers in DependencyInjection.cs**

Add to the dev services section (around line 66):

```csharp
services.AddScoped<GetTravelDevContextHandler>();
services.AddScoped<ForceTravelOverrideHandler>();
services.AddScoped<ClearTravelOverrideHandler>();
```

Add the `using` directives for `WildBunch.Application.Dev.Queries` and `WildBunch.Application.Dev.Commands`.

- [ ] **Step 3: Write integration tests**

Create `tests/WildBunch.Integration.Tests/Dev/DevTravelEndpointTests.cs` following the existing `DevEndpointTests.cs` pattern. Test:
- `GetTravelDevContext_Returns200_InDevEnvironment` (with a seeded session that has an active journey)
- `GetTravelDevContext_Returns403_InNonDevEnvironment` (using `NonDevApiFactory`)
- `GetTravelDevContext_Returns404_WhenSessionDoesNotExist`
- `ForceTravelOverride_Returns204_AndForcesOverride`
- `ForceTravelOverride_Returns403_InNonDevEnvironment`
- `ClearTravelOverride_Returns204_AndClearsOverride`
- `PlayerFacingTravelContextPath_Returns404` (guard: `/api/games/{id}/travel-context` does not exist)

- [ ] **Step 4: Run integration tests**

Run: `.\scripts\postgres-dev.ps1 ensure; dotnet test tests/WildBunch.Integration.Tests --filter "FullyQualifiedName~DevTravelEndpoint"`
Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add src/WildBunch.Api/Dev/DevEndpoints.cs src/WildBunch.Api/DependencyInjection.cs tests/WildBunch.Integration.Tests/Dev/DevTravelEndpointTests.cs
git commit -m "BUNCH-89: add dev travel context, force-override, and clear-override endpoints"
```

---

## Task 5: Persistence — Event serializer + snapshot codec for dev override

**Files:**
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs` — add 2 event types to `ResolveEventType`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` — add `PendingDevTravelOverride` to snapshot
- Test: verify existing event sourcing tests still pass + new replay test

**Interfaces:**
- Consumes: `DevTravelOverrideForced`, `DevTravelOverrideCleared`, `DevTravelOverride` from Tasks 1-2
- Produces: persistence round-trip for dev override state.

- [ ] **Step 1: Add event types to ResolveEventType**

In `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`, add to the `ResolveEventType` switch (around line 49):

```csharp
nameof(DevTravelOverrideForced) => typeof(DevTravelOverrideForced),
nameof(DevTravelOverrideCleared) => typeof(DevTravelOverrideCleared),
nameof(DevTravelOverrideConsumed) => typeof(DevTravelOverrideConsumed),
```

Add `using WildBunch.Domain.Events;` if not already present (it is, since other events are referenced).

- [ ] **Step 2: Add PendingDevTravelOverride to snapshot**

In `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`, add a field to the `GameSessionSnapshot` record:

```csharp
DevTravelOverride? PendingDevTravelOverride,
```

Add to `FromDomain`:

```csharp
PendingDevTravelOverride = session.PendingDevTravelOverride,
```

Add to `ToDomain` (after journey construction, before `GameSessionRehydrator.Create` or after — set via a rehydrator method if the constructor does not accept it):

If `GameSessionRehydrator.Create` does not accept the override, add a `SetPendingDevTravelOverride` method to `GameSessionRehydrator` and call it in `ToDomain`:

```csharp
GameSessionRehydrator.SetPendingDevTravelOverride(session, PendingDevTravelOverride);
```

In `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs`, add:

```csharp
public static void SetPendingDevTravelOverride(GameSession session, DevTravelOverride? overrideValue)
{
    // Use reflection to set the private field, matching the existing pattern
    // in SetVersion / SetCurrentActionContext
    var field = typeof(GameSession).GetField("_pendingDevTravelOverride",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    field?.SetValue(session, overrideValue);
}
```

Add `using WildBunch.Domain.Game;` if not already present.

- [ ] **Step 3: Run event sourcing tests to verify no regressions**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~EventSourcing"`
Expected: PASS — existing replay tests still green.

- [ ] **Step 4: Verify replay tests are already in place**

The replay tests (`RehydrateFromEvents_WithDevOverrideForced_ReconstructsOverrideState`, `RehydrateFromEvents_AfterConsumption_HasNoPendingOverride`, `RehydrateFromEvents_WithNoDevOverride_HasNoPendingOverride`) were added in Task 2 Step 1. After adding the persistence serializer entries in Steps 1-2, these tests now exercise the full persistence round-trip path (event serialization + deserialization + replay). Verify they still pass:

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~DevTravelOverrideTests"`
Expected: PASS — all 8 tests green, including the three replay tests.

- [ ] **Step 5: Run full domain test suite**

Run: `dotnet test tests/WildBunch.Domain.Tests`
Expected: PASS — no regressions from the serializer changes.

- [ ] **Step 6: Commit**

```powershell
git add src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs tests/WildBunch.Domain.Tests/DevTravelOverrideTests.cs
git commit -m "BUNCH-89: persist dev travel override in event stream and snapshot"
```

---

## Task 6: Frontend — TravelDevPanel + dev API client + registry

**Files:**
- Modify: `src/WildBunch.Web/src/dev/types.ts` — add travel dev DTOs
- Modify: `src/WildBunch.Web/src/dev/devApi.ts` — add travel dev API functions
- Create: `src/WildBunch.Web/src/dev/panels/TravelDevPanel.tsx` — new panel component
- Modify: `src/WildBunch.Web/src/dev/DevPanelRegistry.tsx` — register the panel
- Test: `src/WildBunch.Web/src/tests/TravelDevPanel.test.tsx`

**Interfaces:**
- Consumes: `/api/dev/sessions/{id}/travel-context`, `/api/dev/sessions/{id}/travel/force-override`, `/api/dev/sessions/{id}/travel/clear-override` from Task 4
- Produces: `TravelDevPanel` registered in the dev overlay sidebar.

- [ ] **Step 1: Add TypeScript DTOs to types.ts**

```typescript
// Append to src/WildBunch.Web/src/dev/types.ts

export interface FoeProfileDevDto {
  speed: number;
  fightStrength: number;
  minimumBribe: number;
  speedBand: string;
  fightBand: string;
  bribeBand: string;
}

export interface DevOverrideDto {
  forcedCategory: string;
  foeProfile: FoeProfileDevDto | null;
  encounterMessage: string | null;
}

export interface TravelDevContextDto {
  sessionId: string;
  hasActiveJourney: boolean;
  journeyStatus: string | null;
  daysTravelled: number | null;
  remainingDays: number | null;
  pendingEncounterKind: string | null;
  pendingEncounterMessage: string | null;
  pendingFoeProfile: FoeProfileDevDto | null;
  pendingDevOverride: DevOverrideDto | null;
}

export interface ForceTravelOverrideRequestDto {
  forcedCategory: string;
  foeSpeed?: number;
  foeFightStrength?: number;
  foeMinimumBribe?: number;
  encounterMessage?: string;
}
```

- [ ] **Step 2: Add API functions to devApi.ts**

```typescript
// Append to src/WildBunch.Web/src/dev/devApi.ts
import type { TravelDevContextDto, ForceTravelOverrideRequestDto } from "./types";

export function getTravelDevContext(gameId: string) {
  return requestJson<TravelDevContextDto>(`/api/dev/sessions/${gameId}/travel-context`);
}

export function forceTravelOverride(gameId: string, request: ForceTravelOverrideRequestDto) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/travel/force-override`, {
    method: "POST",
    body: JSON.stringify(request),
    headers: { "Content-Type": "application/json" },
  });
}

export function clearTravelOverride(gameId: string) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/travel/clear-override`, {
    method: "POST",
  });
}
```

Note: Verify the `requestJson` signature in `src/api/httpClient.ts` accepts a second options argument with `method`, `body`, and `headers`. If the existing API uses a different pattern (e.g., separate functions for POST), adjust to match the established convention.

- [ ] **Step 3: Write the TravelDevPanel component**

Create `src/WildBunch.Web/src/dev/panels/TravelDevPanel.tsx`. The panel:
- Fetches `getTravelDevContext` via `useQuery`.
- Shows journey status, days travelled, remaining days.
- Shows pending encounter kind/message and foe profile (if any).
- Shows current dev override (if any).
- Provides a form to force a foe override: category select, foe speed/fight/bribe inputs, optional message.
- Provides a "Clear override" button.
- Uses `useMutation` for force/clear, invalidates the travel-context query on success.
- Uses styled-components matching the `SessionAuditDevPanel` style.

Keep the component under 200 lines. If it grows, extract the force form into a sub-component.

```tsx
// src/WildBunch.Web/src/dev/panels/TravelDevPanel.tsx
import styled from "styled-components";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useGameSession } from "../../state/useGameSession";
import { getTravelDevContext, forceTravelOverride, clearTravelOverride } from "../devApi";

export function TravelDevPanel() {
  const { gameId } = useGameSession();
  const queryClient = useQueryClient();
  const [category, setCategory] = useState("Foe");
  const [foeSpeed, setFoeSpeed] = useState("5");
  const [foeFight, setFoeFight] = useState("4");
  const [foeBribe, setFoeBribe] = useState("8");
  const [message, setMessage] = useState("");

  const { data, isLoading, error } = useQuery({
    queryKey: ["dev-travel-context", gameId],
    queryFn: () => getTravelDevContext(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  const forceMutation = useMutation({
    mutationFn: () => forceTravelOverride(gameId as string, {
      forcedCategory: category,
      foeSpeed: category === "Foe" ? Number(foeSpeed) : undefined,
      foeFightStrength: category === "Foe" ? Number(foeFight) : undefined,
      foeMinimumBribe: category === "Foe" ? Number(foeBribe) : undefined,
      encounterMessage: message || undefined,
    }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["dev-travel-context", gameId] }),
  });

  const clearMutation = useMutation({
    mutationFn: () => clearTravelOverride(gameId as string),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["dev-travel-context", gameId] }),
  });

  if (!gameId) return <MutedText>No active session.</MutedText>;
  if (isLoading) return <MutedText>Loading travel context...</MutedText>;
  if (error) return <ErrorText>{error instanceof Error ? error.message : "Failed to load."}</ErrorText>;
  if (!data) return <MutedText>No travel context.</MutedText>;

  return (
    <PanelContainer>
      <Section>
        <SectionTitle>Journey</SectionTitle>
        <FieldRow>
          <span>Status: <strong>{data.journeyStatus ?? "none"}</strong></span>
          <span>Day: {data.daysTravelled ?? 0}</span>
          <span>Remaining: {data.remainingDays ?? 0}</span>
        </FieldRow>
      </Section>

      {data.pendingEncounterKind && (
        <Section>
          <SectionTitle>Pending encounter</SectionTitle>
          <div>Kind: <strong>{data.pendingEncounterKind}</strong></div>
          <div>{data.pendingEncounterMessage}</div>
          {data.pendingFoeProfile && (
            <FoeProfileGrid>
              <span>Speed: {data.pendingFoeProfile.speed} ({data.pendingFoeProfile.speedBand})</span>
              <span>Fight: {data.pendingFoeProfile.fightStrength} ({data.pendingFoeProfile.fightBand})</span>
              <span>Bribe: ${data.pendingFoeProfile.minimumBribe} ({data.pendingFoeProfile.bribeBand})</span>
            </FoeProfileGrid>
          )}
        </Section>
      )}

      {data.pendingDevOverride && (
        <Section>
          <SectionTitle>Active dev override</SectionTitle>
          <div>Category: <strong>{data.pendingDevOverride.forcedCategory}</strong></div>
          {data.pendingDevOverride.encounterMessage && <div>{data.pendingDevOverride.encounterMessage}</div>}
        </Section>
      )}

      <Section>
        <SectionTitle>Force next travel override</SectionTitle>
        <FormRow>
          <label>Category:
            <select value={category} onChange={(e) => setCategory(e.target.value)}>
              <option value="Foe">Foe</option>
              <option value="Npc">Npc</option>
              <option value="Lucky">Lucky</option>
              <option value="Unlucky">Unlucky</option>
              <option value="Environmental">Environmental</option>
              <option value="Resource">Resource</option>
              <option value="HorseTrouble">HorseTrouble</option>
              <option value="Quiet">Quiet</option>
            </select>
          </label>
        </FormRow>
        {category === "Foe" && (
          <>
            <FormRow>
              <label>Speed: <input type="number" value={foeSpeed} onChange={(e) => setFoeSpeed(e.target.value)} min={1} max={10} /></label>
              <label>Fight: <input type="number" value={foeFight} onChange={(e) => setFoeFight(e.target.value)} min={1} max={10} /></label>
              <label>Bribe: <input type="number" value={foeBribe} onChange={(e) => setFoeBribe(e.target.value)} min={1} step={0.5} /></label>
            </FormRow>
          </>
        )}
        <FormRow>
          <label>Message (optional): <input type="text" value={message} onChange={(e) => setMessage(e.target.value)} placeholder="Custom encounter message" /></label>
        </FormRow>
        <ButtonRow>
          <ActionButton onClick={() => forceMutation.mutate()} disabled={forceMutation.isPending}>
            {forceMutation.isPending ? "Forcing..." : "Force override"}
          </ActionButton>
          <ActionButton onClick={() => clearMutation.mutate()} disabled={clearMutation.isPending || !data.pendingDevOverride}>
            {clearMutation.isPending ? "Clearing..." : "Clear override"}
          </ActionButton>
        </ButtonRow>
        {forceMutation.isError && <ErrorText>{(forceMutation.error as Error).message}</ErrorText>}
        {clearMutation.isError && <ErrorText>{(clearMutation.error as Error).message}</ErrorText>}
      </Section>
    </PanelContainer>
  );
}

const PanelContainer = styled.div`
  display: grid;
  gap: 12px;
`;
const Section = styled.div`
  padding: 8px 12px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.025);
  border: 1px solid var(--border);
  font-size: 0.8rem;
`;
const SectionTitle = styled.div`
  color: var(--accent);
  font-weight: 600;
  margin-bottom: 6px;
`;
const FieldRow = styled.div`
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
`;
const FoeProfileGrid = styled.div`
  display: grid;
  gap: 4px;
  margin-top: 6px;
  padding-top: 6px;
  border-top: 1px solid var(--border);
`;
const FormRow = styled.div`
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  align-items: center;
  margin-bottom: 6px;
  label { display: flex; gap: 4px; align-items: center; }
  input, select { background: var(--surface); border: 1px solid var(--border); color: var(--text); border-radius: 4px; padding: 2px 6px; }
`;
const ButtonRow = styled.div`
  display: flex;
  gap: 8px;
`;
const ActionButton = styled.button`
  padding: 4px 12px;
  border-radius: 6px;
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--text);
  cursor: pointer;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
`;
const MutedText = styled.p`
  color: var(--muted);
  margin: 0;
`;
const ErrorText = styled.p`
  color: var(--danger);
  margin: 0;
`;
```

- [ ] **Step 4: Register TravelDevPanel in DevPanelRegistry**

```tsx
// src/WildBunch.Web/src/dev/DevPanelRegistry.tsx
import type { ReactNode } from "react";
import { SessionAuditDevPanel } from "./panels/SessionAuditDevPanel";
import { TravelDevPanel } from "./panels/TravelDevPanel";

export interface DevPanelDefinition {
  id: string;
  label: string;
  render: () => ReactNode;
}

export const devPanels: DevPanelDefinition[] = [
  {
    id: "session-audit",
    label: "Session audit",
    render: () => <SessionAuditDevPanel />,
  },
  {
    id: "travel",
    label: "Travel",
    render: () => <TravelDevPanel />,
  },
];
```

- [ ] **Step 5: Write panel tests**

Create `src/WildBunch.Web/src/tests/TravelDevPanel.test.tsx` following the existing `DevOverlay.test.tsx` pattern. Test:
- Renders journey context when data is loaded.
- Shows "No active session" when no gameId.
- Force button calls the mutation.
- Clear button is disabled when no override is active.

- [ ] **Step 6: Run frontend tests**

Run: `cd src/WildBunch.Web; npm test -- --run TravelDevPanel`
Expected: PASS

- [ ] **Step 7: Commit**

```powershell
git add src/WildBunch.Web/src/dev/ src/WildBunch.Web/src/tests/TravelDevPanel.test.tsx
git commit -m "BUNCH-89: add TravelDevPanel with force/clear controls to dev overlay"
```

---

## Task 7: ADR update + hidden-truth guard test

**Files:**
- Modify: `docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md` — add dated status entry
- Modify: `docs/adr/INDEX.md` — update ADR-0030 timestamp
- Test: verify `GameApiHiddenTruthTests` still pass (no new hidden truth leaks)

- [ ] **Step 1: Update ADR-0030**

Add a new dated status entry to the Dated Status History:

```markdown
- 2026-06-25 - live (BUNCH-89): First contextual dev module added. TravelDevPanel in the dev overlay with dev endpoints for travel-context query, force-override, and clear-override. New typed domain events DevTravelOverrideForced and DevTravelOverrideCleared. Dev override is session-owned aggregate state consumed once by the next AdvanceJourneyDay. Normal travel generation unchanged when no override is active. Dev DTOs separate from player DTOs. Player-facing APIs remain clean of dev override state.
```

- [ ] **Step 2: Update ADR INDEX**

Update the ADR-0030 row in `docs/adr/INDEX.md`:

```markdown
| ADR-0030 | 2026-06-25 | Updated — BUNCH-89 travel dev controls |
```

- [ ] **Step 3: Run hidden truth guard tests**

Run: `dotnet test --filter "FullyQualifiedName~HiddenTruth"`
Expected: PASS — no new hidden truth leaks through player APIs.

- [ ] **Step 4: Commit**

```powershell
git add docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md docs/adr/INDEX.md
git commit -m "BUNCH-89: update ADR-0030 for travel dev controls module"
```

---

## Task 8: Full validation + event-stream proof + screenshots

- [ ] **Step 1: Run full build**

Run: `dotnet build`
Expected: 0 errors, 0 warnings (or only pre-existing warnings).

- [ ] **Step 2: Run full domain test suite**

Run: `dotnet test tests/WildBunch.Domain.Tests`
Expected: All tests pass including new `DevTravelOverrideTests`.

- [ ] **Step 3: Run full application test suite**

Run: `dotnet test tests/WildBunch.Application.Tests`
Expected: All tests pass including new dev handler tests.

- [ ] **Step 4: Run PostgreSQL-backed integration tests**

Run: `.\scripts\postgres-dev.ps1 ensure; dotnet test tests/WildBunch.Integration.Tests`
Expected: All tests pass including new `DevTravelEndpointTests`.

- [ ] **Step 5: Run EF migrations check**

Run: `dotnet tool restore; dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
Expected: No new migration needed (dev override is in the snapshot JSON, no schema change). If a migration is needed, add it.

- [ ] **Step 6: Run frontend tests**

Run: `cd src/WildBunch.Web; npm test -- --run`
Expected: All tests pass.

- [ ] **Step 7: Run frontend build**

Run: `cd src/WildBunch.Web; npm run build`
Expected: Build succeeds.

- [ ] **Step 8: Event-stream proof (manual or automated)**

Start the API and Vite dev server. Using the dev overlay:
1. Start a new game and begin travel to a town.
2. Open the dev overlay, select the "Travel" panel.
3. Verify the journey context shows (status: Active, day: 0, remaining days).
4. Force a foe override with Speed=5, Fight=4, Bribe=8.
5. Verify the "Active dev override" section appears.
6. Advance the travel day (normal player action).
7. Verify the journey is interrupted by a foe encounter matching the forced profile.
8. Verify the dev override is cleared (no longer shown in the panel).
9. Resolve the encounter normally (run/fight/bribe).
10. Verify the session audit panel shows the event sequence: JourneyStarted → DevTravelOverrideForced → TravelDayAdvanced → JourneyEncounterResolved.

Take screenshots of steps 4, 6, 7, 8, and 10 as evidence.

- [ ] **Step 9: Cleanup worker-owned processes**

Stop any API servers, Vite dev servers, and browser sessions started for validation. Report process IDs and ports used.

- [ ] **Step 10: Final commit if any remaining changes**

```powershell
git add -A
git commit -m "BUNCH-89: validation complete"
```

---

## Self-Review

### Spec coverage

- ✅ Inspect current travel generation, pending encounter, foe profile, command/API, and event-sourcing seams — covered in Preflight Answers.
- ✅ Add dev-only travel command endpoints for forcing and clearing — Task 4.
- ✅ Route dev commands through application/domain handling and GameSession — Tasks 2, 3, 4.
- ✅ Record explicit immutable dev events for force, clear, and consumption — Tasks 1, 2 (three events: `DevTravelOverrideForced`, `DevTravelOverrideCleared`, `DevTravelOverrideConsumed`; consumption is an explicit event with its own `Apply` path for replay safety).
- ✅ Support forcing next travel category/profile, including foe encounters — Tasks 1, 2, 3.
- ✅ For foe forcing, allow bribe demand, speed, fight strength — Task 3 (DTO), Task 6 (frontend form).
- ✅ Display current journey/encounter internals through dev-only query — Task 3 (query), Task 6 (panel).
- ✅ Ensure next normal travel advance consumes the dev override once — Task 2 (consume-once in PrepareTravelDayAdvance).
- ✅ Normal travel generation unchanged when no dev override — Task 2 (conditional branch), Task 8 (test).
- ✅ Forced travel state consumed once by normal travel advancement — Task 2, Task 8 (proof).
- ✅ Player still resolves run/fight/bribe normally — no change to resolution engine.
- ✅ Validation includes domain/application/API/frontend tests and event-stream proof — Task 8.
- ✅ Return evidence includes screenshots and sequence — Task 8, Step 8.

### Non-goals check

- ✅ Does not force bribe/run/fight success or failure — override only controls the encounter shape, not resolution outcome.
- ✅ Does not bypass normal travel or encounter resolution mechanics — resolution engine unchanged.
- ✅ Does not expose travel internals through normal player APIs — dev DTOs are separate types under `/api/dev/`.
- ✅ Does not create a separate event store, process manager, or generic command framework — uses existing event-sourcing infrastructure.
- ✅ Does not fold BUNCH-87 deterministic test seed profiles — not referenced.

### Placeholder scan

No TBD, TODO, or "implement later" placeholders. All code blocks contain complete implementations. The `TestSessionFactory.CreateWithActiveJourney()` helper is flagged for verification against existing patterns — this is a real investigation step, not a placeholder.

### Type consistency

- `DevTravelOverride` — consistent across Task 1 (definition), Task 2 (GameSession field), Task 3 (mapper), Task 5 (snapshot).
- `DevTravelOverrideForced` / `DevTravelOverrideCleared` / `DevTravelOverrideConsumed` — consistent across Task 1 (definition), Task 2 (Apply + dispatch + consume logic), Task 5 (serializer). The consumed event is emitted by `ProduceEvent(new DevTravelOverrideConsumed())` in `PrepareTravelDayAdvance()`, applied via `Apply(DevTravelOverrideConsumed)` which clears `_pendingDevTravelOverride`, and dispatched in both `ApplyProducedEvent` and `GameSessionEventReplay.ApplyEvent`.
- `ForceTravelOverrideCommand` — consistent across Task 3 (definition), Task 4 (endpoint).
- `TravelDevContextDto` — consistent across Task 3 (definition), Task 4 (endpoint), Task 6 (frontend type).
- `PendingDevTravelOverride` — `internal` on GameSession, accessed by Application via the existing `[assembly: InternalsVisibleTo("WildBunch.Application")]` in `src/WildBunch.Domain/Properties/AssemblyInfo.cs` (line 4). Settled — no new attribute or public accessor needed.

---

## Split conditions

This plan does not require splitting. The work is a single coherent slice: one domain concept (dev override), one consume-once seam, one set of dev endpoints, one frontend panel. If the `TravelDayPlanFactory` forced-plan generation proves more complex than expected (e.g., non-foe categories need trail events that require generator internals), split the factory work into a follow-up and limit the first slice to foe + quiet categories only.
