# Saloon POI Dev Controls Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the second contextual dev overlay module — saloon/POI controls that let Harley inspect hidden/internal saloon POI state and force the next saloon encounter shape through backend dev commands and event-sourced aggregate state, then proceed through normal gameplay.

**Architecture:** Dev commands flow through `/api/dev/` endpoints (gated by `DevRoleGuard`) into Application handlers that load `GameSession` via the repository, invoke new aggregate command methods on `GameSession`, and store typed domain events. The aggregate holds a pending dev saloon override as session-owned state. Normal `LookAroundSaloon()` consumes the override once (replacing the candidate-selection logic) and emits the normal `SaloonPersonOfInterestSpotted` event. Dev events (`DevSaloonOverrideForced`, `DevSaloonOverrideCleared`, `DevSaloonOverrideConsumed`) are immutable facts in the event stream. The frontend adds a `SaloonDevPanel` to the existing `DevPanelRegistry` that fetches dev query data (including hidden culprit/case truth) and dispatches dev commands.

**Tech Stack:** C#/.NET 10, ASP.NET Core Minimal APIs, EF Core, xUnit, React 18, TanStack Query, styled-components, Vitest.

## Global Constraints

- `GameSession` is the live-play aggregate root; all gameplay mutation flows through it.
- Typed domain events are plain sealed records implementing `IDomainEvent`; `Apply` is the single mutation path.
- Dev endpoints live under `/api/dev/` and are gated by `DevRoleGuard.EnsureDevAccess()`.
- Dev DTOs are separate types from player DTOs.
- Normal player APIs must remain clean of saloon internals, hidden/dev state, and hidden culprit truth.
- Hidden culprit truth (`CaseFile.TrueCulpritId`) remains internal to normal player APIs; dev-only endpoints MAY expose it for debugging/playtesting per ADR-0030 §7.
- Do not redesign saloon gameplay; the look/confront/declare/take-in mechanics stay unchanged.
- Do not force final success/failure outcomes; the player still resolves confrontations normally.
- Do not bypass normal saloon or confrontation resolution mechanics.
- The culprit is always a gang member; this issue does not touch culprit/seed logic.
- Clue, journal, wanted-poster, culprit truth, and bounty flows stay stable beyond dev-control plumbing.
- Worker environment uses PowerShell; do not use `&&` for command chaining.
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent validation.
- The dev override is consumed once by the next `LookAroundSaloon()` call, then cleared from aggregate state.

---

## File Structure

### Domain layer (src/WildBunch.Domain/)

| File | Responsibility |
|------|----------------|
| `Game/DevSaloonOverride.cs` | New record: the pending dev override shape (forced POI kind: Suspect/Citizen/FalseLead, optional suspect ID) |
| `Events/DevSaloonOverrideForced.cs` | New typed domain event: dev forced a pending saloon override |
| `Events/DevSaloonOverrideCleared.cs` | New typed domain event: dev cleared the pending saloon override |
| `Events/DevSaloonOverrideConsumed.cs` | New typed domain event: pending override was consumed by normal saloon look-around |
| `Game/GameSession.cs` (modify) | Add `_pendingDevSaloonOverride` field, `ForceDevSaloonOverride()` / `ClearDevSaloonOverride()` command methods, `Apply(DevSaloonOverrideForced)` / `Apply(DevSaloonOverrideCleared)` / `Apply(DevSaloonOverrideConsumed)` methods, consume override in `LookAroundSaloon()` |
| `Game/GameSessionEventReplay.cs` (modify) | Add dev event cases to `ApplyEvent` switch |
| `Game/GameSession.cs` `ApplyProducedEvent` (modify) | Add dev event cases to the produce-time dispatch switch |

### Application layer (src/WildBunch.Application/)

| File | Responsibility |
|------|----------------|
| `Dev/Models/SaloonDevContextDto.cs` | New dev DTO: saloon POI internals, active POI, eligible suspects, hidden culprit truth, current override |
| `Dev/Models/ForceSaloonOverrideRequestDto.cs` | New dev DTO: request shape for forcing |
| `Dev/Queries/GetSaloonDevContextQuery.cs` | New query record |
| `Dev/Queries/GetSaloonDevContextHandler.cs` | New query handler: loads session, maps dev context |
| `Dev/Commands/ForceSaloonOverrideCommand.cs` | New command record |
| `Dev/Commands/ForceSaloonOverrideHandler.cs` | New command handler: load → aggregate command → store → commit |
| `Dev/Commands/ClearSaloonOverrideCommand.cs` | New command record |
| `Dev/Commands/ClearSaloonOverrideHandler.cs` | New command handler: load → aggregate command → store → commit |
| `Dev/Mapping/SaloonDevContextMapper.cs` | New mapper: domain session → dev DTO (separate from player mappers) |

### API layer (src/WildBunch.Api/)

| File | Responsibility |
|------|----------------|
| `Dev/DevEndpoints.cs` (modify) | Add 3 new dev endpoints: saloon-context query, force-override POST, clear-override POST |
| `DependencyInjection.cs` (modify) | Register new dev handlers |

### Persistence layer (src/WildBunch.Persistence/)

| File | Responsibility |
|------|----------------|
| `Serialization/GameSessionJsonSerializer.Events.cs` (modify) | Add 3 new event types to `ResolveEventType` switch |
| `Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` (modify) | Add `PendingDevSaloonOverride` to snapshot record and `FromDomain`/`ToDomain` |
| `Serialization/GameSessionJsonSerializer.Components.cs` (modify) | Add serialize/deserialize methods for `PendingDevSaloonOverride` |
| `GameSessions/GameSessionComponentNames.cs` (modify) | Add `PendingDevSaloonOverride` component name constant |
| `GameSessions/EfGameSessionRepository.cs` (modify) | Persist/load `PendingDevSaloonOverride` component |

### Frontend (src/WildBunch.Web/)

| File | Responsibility |
|------|----------------|
| `src/dev/types.ts` (modify) | Add `SaloonDevContextDto`, `ForceSaloonOverrideRequestDto` types |
| `src/dev/devApi.ts` (modify) | Add `getSaloonDevContext`, `forceSaloonOverride`, `clearSaloonOverride` functions |
| `src/dev/panels/SaloonDevPanel.tsx` | New panel: shows saloon POI internals + hidden truth + force/clear controls |
| `src/dev/DevPanelRegistry.tsx` (modify) | Register `SaloonDevPanel` |

### Tests

| File | Responsibility |
|------|----------------|
| `tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs` | Domain tests: force, clear, consume-once, no-override unchanged, replay |
| `tests/WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.cs` | Query handler tests |
| `tests/WildBunch.Application.Tests/Dev/ForceSaloonOverrideHandlerTests.cs` | Command handler tests |
| `tests/WildBunch.Application.Tests/Dev/ClearSaloonOverrideHandlerTests.cs` | Command handler tests |
| `tests/WildBunch.Integration.Tests/DevSaloonEndpointTests.cs` | Integration tests: 200/403/404 for dev saloon endpoints |
| `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs` (modify) | Add dev saloon-context hidden-truth boundary test |
| `src/WildBunch.Web/src/tests/SaloonDevPanel.test.tsx` | Panel render + mutation tests |

### Documentation

| File | Responsibility |
|------|----------------|
| `docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md` (modify) | Add dated status entry for BUNCH-90 saloon dev controls |
| `docs/adr/INDEX.md` (modify) | Update ADR-0030 last-checked timestamp |

---

## Source Seams Inspected

All file paths and line numbers verified against current `main` (worktree `bunch-90`).

### Q1: Where is current saloon POI state represented in GameSession and related domain types?

Saloon POI state lives in `TownVisitTownState` (`src/WildBunch.Domain/Game/TownSourceVisitState.cs:68`), which is accessed via `GameSession.CurrentTownVisit.CurrentTownState` (`src/WildBunch.Domain/Game/GameSession.cs:118`). The state fields are:

- `ActiveSaloonPersonOfInterestId` (`TownSourceVisitState.cs:117`) — `SuspectId?`, the active spotted suspect.
- `ActiveSaloonPersonOfInterestDescriptor` (`TownSourceVisitState.cs:119`) — `string?`, the public descriptor text.
- `ActiveSaloonPersonOfInterestKind` (`TownSourceVisitState.cs:121`) — `SaloonPersonOfInterestKind?` (enum: `Citizen = 0`, `WantedSuspect = 1`, defined at `src/WildBunch.Domain/Cases/SaloonPersonOfInterestConfrontation.cs:3`).

These are set by `SetActiveSaloonPersonOfInterest()` (`TownSourceVisitState.cs:143,150`), `SetActiveSaloonCitizenPersonOfInterest()` (`:159`), and cleared by `ClearActiveSaloonPersonOfInterest()` (`:166`). They are cleared on town visit advance (`AdvanceVisit()` at `:220`).

The `SaloonPersonOfInterestSpotted` event (`src/WildBunch.Domain/Events/SaloonPersonOfInterestSpotted.cs:11`) carries `SuspectId?`, `Descriptor`, `PersonOfInterestKind`, and `RecordLog`. It is applied at `GameSession.cs:393` via `Apply(SaloonPersonOfInterestSpotted)` which calls `SetActiveSaloonPersonOfInterest` or `SetActiveSaloonCitizenPersonOfInterest`.

### Q2: What current saloon flows exist for look, confront, declaration, take-in, citizen, wanted suspect, and false-lead cases?

- **Look:** `GameSession.LookAroundSaloon()` (`GameSession.cs:2539`). Enters `TownActionContext.Saloon` via `EnterActionContext` (`:2554`). If the source is spent, emits a repeat "nobody of interest" event (`:2558-2567`). Otherwise calls `TryGetConfrontableSaloonPersonOfInterestCandidateInTown()` (`:2570`, defined at `:3002`) to find an eligible suspect. If found, emits `SaloonPersonOfInterestSpotted` with `WantedSuspect` kind (`:2574-2585`). If not found, emits a citizen POI event (`:2588-2600`).
- **Confront:** `GameSession.ConfrontSaloonPersonOfInterest()` (`GameSession.cs:2603`) delegates to `_bountyLoopCoordinator.ConfrontSaloonPersonOfInterest()` (`GameSession.BountyLoopCoordinator.cs:18`). Checks for active POI, resolves wanted suspect vs citizen, handles warrant declaration, wrong declaration fines, and produces `SaloonPersonOfInterestConfronted` / `WantedSuspectConfronted` events.
- **Declaration:** The confront flow accepts a `declaredWantedIdentityHandle` parameter (`GameSession.cs:2603`). Wrong declarations produce a fine (`SaloonPersonOfInterestConfrontationResult.WrongWantedDeclaration` at `SaloonPersonOfInterestConfrontation.cs:63`).
- **Take-in:** `GameSession.SettleSheriffTurnIn()` (`GameSession.cs:2634`) delegates to `_bountyLoopCoordinator.SettleSheriffTurnIn()`. Produces `SheriffTurnInSettled` events.
- **Citizen case:** When no confrontable suspect is found, `DescribeTownCitizen()` (`GameSession.cs:2985`) returns `"a town clerk from {townName}"` and a citizen POI is spotted (`:2588-2600`).
- **Wanted suspect case:** `IsEligibleSaloonPersonOfInterestCandidate()` (`GameSession.cs:3019`) checks that the suspect is not the true culprit, and either has no known warrant or has a warrant with presence state `AvailableInTown` or `GoneToGround`.
- **False-lead case:** A citizen POI that is confronted with a wrong wanted identity declaration results in a fine (`SaloonPersonOfInterestConfrontationResult.WrongWantedDeclaration` with `isCitizen: true`).

### Q3: Where is hidden culprit/case truth stored, and how is it currently kept out of normal player APIs?

Hidden culprit truth is in `CaseFile.TrueCulpritId` (`src/WildBunch.Domain/Cases/CaseFile.cs:137`) — a `SuspectId` property that is public on the domain model but never mapped into player-facing DTOs. The `GameApiHiddenTruthTests` (`tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs:11`) verify that public API responses do not contain `"trueCulpritId"`, `"isTrueCulprit"`, `"linkedSuspectIds"`, `"killerReleaseState"`, or gang member names (Butch Cassidy, Sundance Kid, etc.).

The `IsEligibleSaloonPersonOfInterestCandidate()` method (`GameSession.cs:3019`) uses `CaseFile.TrueCulpritId` to prevent the true culprit from ever appearing as a saloon POI (`:3023-3026`), but this check is internal — the true culprit ID is never returned to the player.

The existing dev travel context endpoint test (`GameApiHiddenTruthTests.cs:75`) verifies that `/api/dev/sessions/{id}/travel-context` does not leak hidden truth. The saloon dev context endpoint will be the first dev endpoint to **deliberately** expose hidden truth (culprit ID, suspect eligibility) per ADR-0030 §7, which permits dev-only endpoints to expose hidden truth when deliberately scoped, guarded, and separated from player DTOs.

### Q4: What current events represent saloon POI setup, action, and resolution?

- `SaloonPersonOfInterestSpotted` (`src/WildBunch.Domain/Events/SaloonPersonOfInterestSpotted.cs:11`) — emitted by `LookAroundSaloon()` (`GameSession.cs:2566,2584,2599`). Applied at `GameSession.cs:393`. Marks the source spent, optionally records a case-update log, and sets the active saloon POI.
- `SaloonPersonOfInterestConfronted` (`src/WildBunch.Domain/Events/SaloonPersonOfInterestConfronted.cs`) — emitted by the bounty loop coordinator during confrontation. Applied at `GameSession.cs:463`. Clears the active saloon POI and optionally fines the player.
- `WantedSuspectConfronted` (`src/WildBunch.Domain/Events/WantedSuspectConfronted.cs`) — emitted during wanted-suspect confrontation resolution. Applied at `GameSession.cs:422`. Records confrontation state and updates presence ledger.
- `SheriffTurnInSettled` — emitted during sheriff turn-in. Applied at `GameSession.cs:451`. Adjusts wallet and records settlement state.
- `TownActionContextEntered` — emitted by `EnterActionContext()` when entering the saloon context. Applied at `GameSession.cs:377`. Sets context and advances clock.

### Q5: Where should pending dev saloon/POI override state live in aggregate state?

On `GameSession` as a new private field `_pendingDevSaloonOverride` of type `DevSaloonOverride?` (a new domain record). This mirrors the `_pendingDevTravelOverride` pattern (`GameSession.cs:42`). It is session-owned aggregate state, persisted via the snapshot and reconstructed on load. It is consumed inside `LookAroundSaloon()` at `GameSession.cs:2570`, replacing the `TryGetConfrontableSaloonPersonOfInterestCandidateInTown` candidate-selection logic when present. It is NOT on `TownVisitTownState` — the override applies to the *next* `LookAroundSaloon()` call regardless of town visit state.

### Q6: What immutable dev events are needed for force, clear, and consume?

Three typed dev events, each with an explicit `Apply` path so replay reconstructs the exact same aggregate state as the command path:

- `DevSaloonOverrideForced` — records that a dev command set a pending saloon override (forced POI kind, optional suspect ID). `Apply` sets `_pendingDevSaloonOverride`.
- `DevSaloonOverrideCleared` — records that a dev command cleared the pending override. `Apply` sets `_pendingDevSaloonOverride = null`.
- `DevSaloonOverrideConsumed` — records that the pending override was consumed by normal saloon look-around. `Apply` sets `_pendingDevSaloonOverride = null`. Emitted by `LookAroundSaloon()` right before the `SaloonPersonOfInterestSpotted` event, in the same command execution.

The consumption event is necessary for replay safety. Without it, replaying `DevSaloonOverrideForced → SaloonPersonOfInterestSpotted` would set the override on the `Forced` event and never clear it, leaving a stale pending override in the rehydrated session. With the explicit `DevSaloonOverrideConsumed` event, replay of `Forced → Consumed → SaloonPersonOfInterestSpotted` reconstructs the correct final state.

### Q7: What dev-only query data is useful and safe to show only through dev endpoints?

The `SaloonDevContextDto` will include:
- **Current saloon context:** `CurrentActionContext` (is it `Saloon`?), `CurrentTownId`, `CurrentTownName`, `SaloonAvailable` (whether the town has a saloon source).
- **Active POI state:** `ActiveSaloonPersonOfInterestId`, `ActiveSaloonPersonOfInterestDescriptor`, `ActiveSaloonPersonOfInterestKind`, `SaloonSourceSpent` (whether `SaloonLookAround` is spent this visit).
- **Eligible suspects (dev-only internal truth):** List of all suspects with their ID, name, `IsTrueCulprit` flag, `IsEligibleSaloonCandidate` flag, and reason for ineligibility if applicable. This is hidden truth that helps Harley understand why certain suspects do or don't appear.
- **Hidden culprit truth (dev-only):** `TrueCulpritId` and `TrueCulpritName` — the actual culprit identity. This is deliberately exposed through the dev endpoint per ADR-0030 §7.
- **Pending dev override:** `PendingDevOverride` with forced kind, suspect ID, and suspect name if applicable.

### Q8: Where should dev-only saloon command/query endpoints live under the foundation conventions?

Under `/api/dev/` in `src/WildBunch.Api/Dev/DevEndpoints.cs`, following the BUNCH-89 pattern. New endpoints:
- `GET /api/dev/sessions/{id}/saloon-context` — dev query returning saloon POI internals, eligible suspects, hidden culprit truth, and current dev override state.
- `POST /api/dev/sessions/{id}/saloon/force-override` — dev command to force the next saloon POI shape.
- `POST /api/dev/sessions/{id}/saloon/clear-override` — dev command to clear the pending override.

Application handlers live in `src/WildBunch.Application/Dev/` (Queries and Commands subdirectories), mirroring the existing `GetTravelDevContextHandler` pattern.

### Q9: Which normal APIs/read models must remain clean of hidden truth?

All endpoints under `/api/games/` — `GameSessionEndpoints`, `TravelEndpoints`, `ProjectionEndpoints`, `InvestigationEndpoints`, `ActionEndpoints`. The `GameSessionDto`, `GameTurnResultDto`, and projection DTOs must not gain dev override fields or hidden culprit fields. The existing `GameApiHiddenTruthTests` guard the player boundary. Dev override state is internal to `GameSession` and only exposed through `/api/dev/` DTOs. The `SaloonPersonOfInterestSpotted` event and its player-facing DTOs remain unchanged — they carry only public descriptor text and POI kind, never the true culprit ID or eligibility reasoning.

### Q10: What tests prove no-override saloon behavior is unchanged?

- Existing `GameSessionSaloonPersonOfInterestTests` (`tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs`) continue to pass unchanged (no override active).
- New domain test: `LookAroundSaloon_WithNoDevOverride_UsesNormalCandidateSelection` — characterization test proving the normal suspect-or-citizen flow is unchanged.
- New domain test: `LookAroundSaloon_WithDevOverride_ConsumesOverrideOnce` — proves the forced POI is used, `DevSaloonOverrideConsumed` is emitted, and the override is cleared after.
- New domain test: `LookAroundSaloon_AfterConsumedOverride_ResumesNormalSelection` — proves the next look-around uses normal selection.
- Replay test: `RehydrateFromEvents_WithDevSaloonOverrideForced_ReconstructsOverrideState` — proves the override is reconstructed from the `Forced` event alone.
- **Replay-after-consumption test: `RehydrateFromEvents_AfterSaloonConsumption_HasNoPendingOverride`** — proves that replaying `Forced → Consumed → SaloonPersonOfInterestSpotted` rehydrates a session with `_pendingDevSaloonOverride = null`.
- **No-override replay test: `RehydrateFromEvents_WithNoDevSaloonOverride_HasNoPendingOverride`** — proves that a normal event stream without dev events rehydrates with no pending override.

### Q11: What event-stream proof will demonstrate force -> saloon action/consume -> normal outcome?

A domain-level test that:
1. Creates a session with a confrontable saloon suspect (`TestSessionFactory.CreateWithConfrontableSaloonSuspect()`).
2. Forces a suspect override (emits `DevSaloonOverrideForced`).
3. Looks around the saloon (emits `DevSaloonOverrideConsumed` then `SaloonPersonOfInterestSpotted` with the forced suspect).
4. Verifies the override is consumed (aggregate state `_pendingDevSaloonOverride` is null).
5. Confronts the POI normally (emits `SaloonPersonOfInterestConfronted` / `WantedSuspectConfronted`).
6. Verifies the event stream contains: `DevSaloonOverrideForced`, `DevSaloonOverrideConsumed`, `SaloonPersonOfInterestSpotted`, `SaloonPersonOfInterestConfronted` — proving the dev force was an event, the consume was an event, the look-around consumed it, and normal confrontation followed.
7. **Rehydrates a fresh session from that event stream and verifies `_pendingDevSaloonOverride` is null** — proving replay produces the same final state as the command path.

---

## Task 1: Domain — DevSaloonOverride record + dev events

**Files:**
- Create: `src/WildBunch.Domain/Game/DevSaloonOverride.cs`
- Create: `src/WildBunch.Domain/Events/DevSaloonOverrideForced.cs`
- Create: `src/WildBunch.Domain/Events/DevSaloonOverrideCleared.cs`
- Create: `src/WildBunch.Domain/Events/DevSaloonOverrideConsumed.cs`

**Interfaces:**
- Produces: `DevSaloonOverride` record, `DevSaloonOverrideForced` event, `DevSaloonOverrideCleared` event, `DevSaloonOverrideConsumed` event — consumed by Task 2 (GameSession) and Task 5 (persistence serializer).

**Dev override shape:** The `DevSaloonOverride` record captures the forced POI kind (`Suspect`, `Citizen`, `FalseLead`) and an optional suspect ID. When `ForcedKind = Suspect` and `ForcedSuspectId` is set, `LookAroundSaloon()` will spot that specific suspect. When `ForcedKind = Suspect` and `ForcedSuspectId` is null, it spots the first eligible suspect (same as normal, but guaranteed). When `ForcedKind = Citizen`, it spots a citizen. When `ForcedKind = FalseLead`, it spots a citizen (the false-lead outcome is produced by the normal confrontation flow when the player declares a wrong wanted identity on a citizen POI).

- [ ] **Step 1: Write the DevSaloonOverride record**

```csharp
// src/WildBunch.Domain/Game/DevSaloonOverride.cs
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Game;

/// <summary>
/// Pending dev override for the next saloon look-around.
/// When present, LookAroundSaloon uses this instead of calling
/// TryGetConfrontableSaloonPersonOfInterestCandidateInTown.
/// Consumed once by the next look-around, then cleared from aggregate state.
/// This is dev-only session state, not player-facing. See BUNCH-90.
/// </summary>
public sealed record DevSaloonOverride(
    DevSaloonPoiKind ForcedKind,
    SuspectId? ForcedSuspectId)
{
    /// <summary>
    /// Force the next look-around to spot a specific suspect by ID.
    /// The suspect must exist in the case file. Normal eligibility checks
    /// (not the true culprit, warrant/presence state) are bypassed by the
    /// dev override — the dev is explicitly choosing who appears.
    /// </summary>
    public static DevSaloonOverride ForSuspect(SuspectId suspectId)
        => new(DevSaloonPoiKind.Suspect, suspectId);

    /// <summary>
    /// Force the next look-around to spot the first eligible suspect
    /// (same as normal selection, but guaranteed to run even if the
    /// source would otherwise be spent or no candidate is found).
    /// </summary>
    public static DevSaloonOverride ForAnySuspect()
        => new(DevSaloonPoiKind.Suspect, null);

    /// <summary>
    /// Force the next look-around to spot a citizen (non-suspect) POI.
    /// </summary>
    public static DevSaloonOverride ForCitizen()
        => new(DevSaloonPoiKind.Citizen, null);

    /// <summary>
    /// Force the next look-around to spot a citizen POI that will produce
    /// a false-lead outcome when confronted with a wrong wanted identity.
    /// This is semantically a citizen POI — the false-lead outcome comes
    /// from the normal confrontation flow, not from the override itself.
    /// </summary>
    public static DevSaloonOverride ForFalseLead()
        => new(DevSaloonPoiKind.FalseLead, null);
}

/// <summary>
/// The kind of POI the dev override forces for the next saloon look-around.
/// </summary>
public enum DevSaloonPoiKind
{
    /// <summary>Force a specific or any eligible suspect to appear.</summary>
    Suspect = 0,

    /// <summary>Force a citizen (non-suspect) to appear.</summary>
    Citizen = 1,

    /// <summary>
    /// Force a citizen to appear that will produce a false-lead outcome
    /// when confronted with a wrong wanted identity declaration.
    /// Semantically a citizen POI — the false-lead outcome is a normal
    /// confrontation flow result.
    /// </summary>
    FalseLead = 2
}
```

- [ ] **Step 2: Write the DevSaloonOverrideForced event**

```csharp
// src/WildBunch.Domain/Events/DevSaloonOverrideForced.cs
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command forced a pending saloon override.
/// This is a dev-only event — it records dev intent, not a gameplay outcome.
/// The override is consumed by the next DevSaloonOverrideConsumed + SaloonPersonOfInterestSpotted pair.
/// See BUNCH-90 and ADR-0030.
/// </summary>
public sealed record DevSaloonOverrideForced : IDomainEvent
{
    public required DevSaloonPoiKind ForcedKind { get; init; }
    public SuspectId? ForcedSuspectId { get; init; }
}
```

- [ ] **Step 3: Write the DevSaloonOverrideCleared event**

```csharp
// src/WildBunch.Domain/Events/DevSaloonOverrideCleared.cs
namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command cleared the pending saloon override.
/// Dev-only event. See BUNCH-90 and ADR-0030.
/// </summary>
public sealed record DevSaloonOverrideCleared : IDomainEvent;
```

- [ ] **Step 4: Write the DevSaloonOverrideConsumed event**

```csharp
// src/WildBunch.Domain/Events/DevSaloonOverrideConsumed.cs
namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the pending dev saloon override was consumed by normal saloon look-around.
/// Emitted by LookAroundSaloon() right before the SaloonPersonOfInterestSpotted event,
/// in the same command execution. Apply clears _pendingDevSaloonOverride.
/// This event makes replay safe: replaying Forced -> Consumed -> SaloonPersonOfInterestSpotted
/// reconstructs the correct final state with no pending override.
/// Dev-only event — not a gameplay outcome. See BUNCH-90 and ADR-0030.
/// </summary>
public sealed record DevSaloonOverrideConsumed : IDomainEvent;
```

- [ ] **Step 5: Build to verify compilation**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: Build succeeds (new files compile, no references yet).

- [ ] **Step 6: Commit**

```powershell
git add src/WildBunch.Domain/Game/DevSaloonOverride.cs src/WildBunch.Domain/Events/DevSaloonOverrideForced.cs src/WildBunch.Domain/Events/DevSaloonOverrideCleared.cs src/WildBunch.Domain/Events/DevSaloonOverrideConsumed.cs
git commit -m "BUNCH-90: add DevSaloonOverride record and dev domain events"
```

---

## Task 2: Domain — GameSession override state, command methods, Apply, consume-once

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` — add field, properties, command methods, Apply methods, consume logic in `LookAroundSaloon()`
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` — add dev event cases to `ApplyEvent`
- Test: `tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs`

**Interfaces:**
- Consumes: `DevSaloonOverride`, `DevSaloonOverrideForced`, `DevSaloonOverrideCleared`, `DevSaloonOverrideConsumed` from Task 1
- Produces: `GameSession.ForceDevSaloonOverride()`, `GameSession.ClearDevSaloonOverride()`, `GameSession.PendingDevSaloonOverride` property, `Apply(DevSaloonOverrideForced)`, `Apply(DevSaloonOverrideCleared)`, `Apply(DevSaloonOverrideConsumed)` — consumed by Task 3 (handlers), Task 5 (persistence), Task 6 (frontend query).

**Application-layer access strategy:** `PendingDevSaloonOverride` is `internal` on `GameSession`. The repo already has `[assembly: InternalsVisibleTo("WildBunch.Application")]` in `src/WildBunch.Domain/Properties/AssemblyInfo.cs` (line 4), so the Application dev mapper can read it directly. No new `InternalsVisibleTo` attribute or public accessor is needed. This is settled — not a mid-flight discovery point.

**Consume-once strategy:** `LookAroundSaloon()` at `GameSession.cs:2539` currently calls `TryGetConfrontableSaloonPersonOfInterestCandidateInTown()` at line 2570. The override is consumed **after** the availability check and context entry, but **before** the spent-source check. This means:
- If the saloon is not available, the override is NOT consumed (the look-around fails first).
- If the saloon is available, the override IS consumed — it replaces both the spent-source repeat path and the normal candidate-selection path.
- The `DevSaloonOverrideConsumed` event is emitted before the `SaloonPersonOfInterestSpotted` event, in the same command execution. The forced POI shape is built from the captured override value before the consumed event clears the field (same capture-before-consume pattern as travel at `GameSession.cs:1267`).

- [ ] **Step 1: Write failing domain tests**

Create `tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs`. Use the existing `TestSessionFactory` pattern to create sessions with confrontable saloon suspects, then test force/clear/consume.

```csharp
// tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Tests for BUNCH-90 event-sourced saloon dev controls.
/// Proves force, clear, consume-once, replay safety, and no-override unchanged behavior.
/// </summary>
public sealed class DevSaloonOverrideTests
{
    [Fact]
    public void ForceDevSaloonOverride_ProducesEvent_AndSetsPendingOverride()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        var suspectId = new SuspectId("suspect-1");

        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));

        var forcedEvent = Assert.Single(session.UncommittedEvents.OfType<DevSaloonOverrideForced>());
        Assert.Equal(DevSaloonPoiKind.Suspect, forcedEvent.ForcedKind);
        Assert.Equal(suspectId, forcedEvent.ForcedSuspectId);
        Assert.NotNull(session.PendingDevSaloonOverride);
    }

    [Fact]
    public void ForceDevSaloonOverride_ForCitizen_ProducesEventWithCitizenKind()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();

        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());

        var forcedEvent = Assert.Single(session.UncommittedEvents.OfType<DevSaloonOverrideForced>());
        Assert.Equal(DevSaloonPoiKind.Citizen, forcedEvent.ForcedKind);
        Assert.Null(forcedEvent.ForcedSuspectId);
    }

    [Fact]
    public void ClearDevSaloonOverride_ProducesEvent_AndClearsPendingOverride()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();

        session.ClearDevSaloonOverride();

        Assert.Single(session.UncommittedEvents.OfType<DevSaloonOverrideCleared>());
        Assert.Null(session.PendingDevSaloonOverride);
    }

    [Fact]
    public void ClearDevSaloonOverride_WithNoOverride_IsNoOp()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();

        session.ClearDevSaloonOverride();

        Assert.Empty(session.UncommittedEvents);
        Assert.Null(session.PendingDevSaloonOverride);
    }

    [Fact]
    public void LookAroundSaloon_WithDevOverride_ForcesSpecificSuspect()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        var suspectId = new SuspectId("suspect-1");
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        // DevSaloonOverrideConsumed event was emitted
        Assert.Single(session.UncommittedEvents.OfType<DevSaloonOverrideConsumed>());
        // Override consumed after look-around
        Assert.Null(session.PendingDevSaloonOverride);
        // The forced suspect was spotted
        Assert.True(result.Success);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect,
            session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
    }

    [Fact]
    public void LookAroundSaloon_WithDevOverride_ForcesCitizen()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.Single(session.UncommittedEvents.OfType<DevSaloonOverrideConsumed>());
        Assert.Null(session.PendingDevSaloonOverride);
        Assert.True(result.Success);
        // Citizen POI: no suspect ID, but a descriptor is set
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.NotNull(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen,
            session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
    }

    [Fact]
    public void LookAroundSaloon_WithDevOverride_BypassesSpentSourceCheck()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        var suspectId = new SuspectId("suspect-1");
        // First look-around spends the source
        session.LookAroundSaloon();
        session.MarkEventsCommitted();
        // Force an override — it should bypass the spent-source repeat path
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.Single(session.UncommittedEvents.OfType<DevSaloonOverrideConsumed>());
        Assert.Null(session.PendingDevSaloonOverride);
        Assert.True(result.Success);
        // The forced suspect was spotted, not the "nobody of interest" repeat message
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
    }

    [Fact]
    public void LookAroundSaloon_AfterConsumedOverride_ResumesNormalSelection()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();
        session.LookAroundSaloon();
        session.MarkEventsCommitted();

        // Next look-around should use normal selection (no override)
        // Source is now spent, so normal path produces "nobody of interest"
        var result = session.LookAroundSaloon();

        Assert.Null(session.PendingDevSaloonOverride);
        // No new DevSaloonOverrideConsumed event (override was already consumed)
        Assert.Empty(session.UncommittedEvents.OfType<DevSaloonOverrideConsumed>());
    }

    [Fact]
    public void LookAroundSaloon_WithNoDevOverride_UsesNormalCandidateSelection()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();

        session.LookAroundSaloon();

        Assert.Null(session.PendingDevSaloonOverride);
        // No dev events in the stream
        Assert.Empty(session.UncommittedEvents.OfType<DevSaloonOverrideForced>());
        Assert.Empty(session.UncommittedEvents.OfType<DevSaloonOverrideConsumed>());
    }

    [Fact]
    public void LookAroundSaloon_WithNoSaloon_DoesNotConsumeOverride()
    {
        var session = TestSessionFactory.CreateWithNoSaloon();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        // Override is NOT consumed — the look-around failed before reaching the consume point
        Assert.False(result.Success);
        Assert.NotNull(session.PendingDevSaloonOverride);
        Assert.Empty(session.UncommittedEvents.OfType<DevSaloonOverrideConsumed>());
    }

    [Fact]
    public void RehydrateFromEvents_WithDevSaloonOverrideForced_ReconstructsOverrideState()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session.MarkEventsCommitted();

        var gameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);
        var events = new[] { gameStarted }.Concat(session.CommittedEvents.OfType<IDomainEvent>()).ToList();
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, session.CaseFile, events);

        Assert.NotNull(rehydrated.PendingDevSaloonOverride);
        Assert.Equal(DevSaloonPoiKind.Suspect, rehydrated.PendingDevSaloonOverride!.ForcedKind);
    }

    [Fact]
    public void RehydrateFromEvents_AfterSaloonConsumption_HasNoPendingOverride()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();
        session.LookAroundSaloon();
        session.MarkEventsCommitted();

        // Rehydrate from the full event stream: Forced -> Consumed -> SaloonPersonOfInterestSpotted
        var gameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);
        var events = new[] { gameStarted }.Concat(session.CommittedEvents.OfType<IDomainEvent>()).ToList();
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, session.CaseFile, events);

        // Critical replay-safety proof: override is null after replay
        Assert.Null(rehydrated.PendingDevSaloonOverride);
    }

    [Fact]
    public void RehydrateFromEvents_WithNoDevSaloonOverride_HasNoPendingOverride()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.LookAroundSaloon();
        session.MarkEventsCommitted();

        var gameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);
        var events = new[] { gameStarted }.Concat(session.CommittedEvents.OfType<IDomainEvent>()).ToList();
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, session.CaseFile, events);

        Assert.Null(rehydrated.PendingDevSaloonOverride);
    }
}
```

Note: `TravelTestFactory.RecaptureGameStartedForReplay` is the existing helper used by `DevTravelOverrideTests` (`tests/WildBunch.Domain.Tests/DevTravelOverrideTests.cs:135`). It reconstructs the `GameStarted` event from a session for replay testing. The saloon tests use the same pattern.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~DevSaloonOverrideTests"`
Expected: FAIL — `ForceDevSaloonOverride` does not exist, `PendingDevSaloonOverride` does not exist.

- [ ] **Step 3: Add override field and property to GameSession**

In `src/WildBunch.Domain/Game/GameSession.cs`, add near the other private fields (after line 42, the `_pendingDevTravelOverride` field):

```csharp
private DevSaloonOverride? _pendingDevSaloonOverride;
```

Add a public property near `PendingDevTravelOverride` (after line 126):

```csharp
/// <summary>
/// Pending dev override for the next saloon look-around. Dev-only state.
/// Consumed once by the next LookAroundSaloon. See BUNCH-90.
/// </summary>
internal DevSaloonOverride? PendingDevSaloonOverride => _pendingDevSaloonOverride;
```

Use `internal` so the Application dev handler can read it for the dev query DTO, but it is not exposed on any player DTO.

- [ ] **Step 4: Add command methods to GameSession**

Add after `ClearDevTravelOverride()` (after line 924):

```csharp
/// <summary>
/// Dev command: forces the next saloon look-around to use the given override.
/// Produces a DevSaloonOverrideForced event. The override is consumed once by
/// the next LookAroundSaloon. See BUNCH-90.
/// </summary>
public void ForceDevSaloonOverride(DevSaloonOverride overrideValue)
{
    ArgumentNullException.ThrowIfNull(overrideValue);
    if (IsJourneyModal())
    {
        throw new InvalidOperationException("Cannot force a saloon override while a journey is active.");
    }

    ProduceEvent(new DevSaloonOverrideForced
    {
        ForcedKind = overrideValue.ForcedKind,
        ForcedSuspectId = overrideValue.ForcedSuspectId
    });
}

/// <summary>
/// Dev command: clears any pending saloon override.
/// Produces a DevSaloonOverrideCleared event. See BUNCH-90.
/// </summary>
public void ClearDevSaloonOverride()
{
    if (_pendingDevSaloonOverride is null)
    {
        return; // No-op if nothing to clear — idempotent
    }

    ProduceEvent(new DevSaloonOverrideCleared());
}
```

- [ ] **Step 5: Add Apply methods for dev events**

Add near the other Apply methods (after `Apply(DevTravelOverrideConsumed)` at line 671):

```csharp
/// <summary>
/// Applies a DevSaloonOverrideForced event. Sets the pending dev saloon override.
/// Dev-only event — does not affect gameplay state directly. See BUNCH-90.
/// </summary>
internal void Apply(DevSaloonOverrideForced e)
{
    _pendingDevSaloonOverride = new DevSaloonOverride(
        e.ForcedKind,
        e.ForcedSuspectId);
    _version++;
}

/// <summary>
/// Applies a DevSaloonOverrideCleared event. Clears the pending dev saloon override.
/// Dev-only event. See BUNCH-90.
/// </summary>
internal void Apply(DevSaloonOverrideCleared e)
{
    _pendingDevSaloonOverride = null;
    _version++;
}

/// <summary>
/// Applies a DevSaloonOverrideConsumed event. Clears the pending dev saloon override.
/// This is the replay-safe consumption path: replaying Forced -> Consumed ->
/// SaloonPersonOfInterestSpotted reconstructs the correct final state with no pending override.
/// Dev-only event — not a gameplay outcome. See BUNCH-90.
/// </summary>
internal void Apply(DevSaloonOverrideConsumed e)
{
    _pendingDevSaloonOverride = null;
    _version++;
}
```

- [ ] **Step 6: Add dev event cases to ApplyProducedEvent**

In `src/WildBunch.Domain/Game/GameSession.cs`, in the `ApplyProducedEvent` switch (around line 363, after the `DevTravelOverrideConsumed` case), add before the `default` case:

```csharp
case DevSaloonOverrideForced dsf:
    Apply(dsf);
    break;
case DevSaloonOverrideCleared dsc:
    Apply(dsc);
    break;
case DevSaloonOverrideConsumed dsc2:
    Apply(dsc2);
    break;
```

- [ ] **Step 7: Add dev event cases to GameSessionEventReplay.ApplyEvent**

In `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`, in the `ApplyEvent` switch (around line 139, after the `DevTravelOverrideConsumed` case), add before the `default` case:

```csharp
case DevSaloonOverrideForced dsf:
    session.Apply(dsf);
    break;
case DevSaloonOverrideCleared dsc:
    session.Apply(dsc);
    break;
case DevSaloonOverrideConsumed dsc2:
    session.Apply(dsc2);
    break;
```

- [ ] **Step 8: Consume the override in LookAroundSaloon**

In `src/WildBunch.Domain/Game/GameSession.cs`, in `LookAroundSaloon()` (starting at line 2539), the current flow is:

1. Journey modal check (`:2541`)
2. Saloon availability check (`:2546`)
3. Enter saloon context (`:2554`)
4. Spent-source repeat check (`:2556-2568`)
5. Try confrontable suspect candidate (`:2570-2586`)
6. Default citizen POI (`:2588-2600`)

The override is consumed **after** step 2 (availability check) and step 3 (context entry), but **before** step 4 (spent-source check). This means the override bypasses both the spent-source repeat path and the normal candidate selection. Replace steps 4-6 with override-aware logic:

Replace the block from line 2556 (`if (CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround))`) through line 2600 (the end of the citizen POI block) with:

```csharp
// Capture the pending override before producing the consumed event.
// ProduceEvent(new DevSaloonOverrideConsumed()) calls Apply() which clears
// _pendingDevSaloonOverride, so we must build the forced POI from the captured
// value before emitting the event. See BUNCH-90.
var pendingOverride = _pendingDevSaloonOverride;

if (pendingOverride is not null)
{
    ProduceEvent(new DevSaloonOverrideConsumed());

    // Build the forced POI from the captured override value.
    // The consumed event has already cleared _pendingDevSaloonOverride.
    if (pendingOverride.ForcedKind is DevSaloonPoiKind.Suspect)
    {
        Suspect? forcedSuspect = null;
        if (pendingOverride.ForcedSuspectId is not null)
        {
            forcedSuspect = CaseFile.Suspects.FirstOrDefault(s => s.Id.Equals(pendingOverride.ForcedSuspectId.Value));
        }
        else
        {
            // ForAnySuspect: use normal candidate selection
            TryGetConfrontableSaloonPersonOfInterestCandidateInTown(out var candidate);
            forcedSuspect = candidate;
        }

        if (forcedSuspect is not null)
        {
            var descriptor = SaloonPersonOfInterestDescriptor.Describe(forcedSuspect, CaseFile);
            var spotMessage = $"You look around the saloon and spot {descriptor}.";
            var spotEvent = new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = CurrentTown.TownId,
                Message = spotMessage,
                SuspectId = forcedSuspect.Id,
                Descriptor = descriptor,
                PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
                RecordLog = true
            };
            ProduceEvent(spotEvent);
            return CaseInvestigationResult.Succeeded(spotMessage, sessionChanged: true);
        }
        // If no suspect found (ForAnySuspect with no eligible candidates), fall through to citizen
    }

    // Citizen and FalseLead both produce a citizen POI.
    // FalseLead is semantically a citizen — the false-lead outcome comes from
    // the normal confrontation flow when the player declares a wrong wanted identity.
    var citizenDescriptor = DescribeTownCitizen(CurrentTown);
    var citizenMessage = $"You look around the saloon and spot {citizenDescriptor}.";
    var citizenEvent = new SaloonPersonOfInterestSpotted
    {
        SourceKind = InvestigationSourceKind.SaloonLookAround,
        TownId = CurrentTown.TownId,
        Message = citizenMessage,
        Descriptor = citizenDescriptor,
        PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
        RecordLog = false
    };
    ProduceEvent(citizenEvent);
    return CaseInvestigationResult.Succeeded(citizenMessage, sessionChanged: true);
}

// Normal path (no dev override)
if (CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround))
{
    var repeatMessage = "You look around the saloon again, but nobody of interest is here.";
    var repeatEvent = new SaloonPersonOfInterestSpotted
    {
        SourceKind = InvestigationSourceKind.SaloonLookAround,
        TownId = CurrentTown.TownId,
        Message = repeatMessage,
        RecordLog = true
    };
    ProduceEvent(repeatEvent);
    return CaseInvestigationResult.Succeeded(repeatMessage, sessionChanged: true);
}

if (TryGetConfrontableSaloonPersonOfInterestCandidateInTown(out var suspect))
{
    var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, CaseFile);
    var spotMessage = $"You look around the saloon and spot {descriptor}.";
    var spotEvent = new SaloonPersonOfInterestSpotted
    {
        SourceKind = InvestigationSourceKind.SaloonLookAround,
        TownId = CurrentTown.TownId,
        Message = spotMessage,
        SuspectId = suspect.Id,
        Descriptor = descriptor,
        PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
        RecordLog = true
    };
    ProduceEvent(spotEvent);
    return CaseInvestigationResult.Succeeded(spotMessage, sessionChanged: true);
}

var defaultCitizenDescriptor = DescribeTownCitizen(CurrentTown);
var defaultCitizenMessage = $"You look around the saloon and spot {defaultCitizenDescriptor}.";
var defaultCitizenEvent = new SaloonPersonOfInterestSpotted
{
    SourceKind = InvestigationSourceKind.SaloonLookAround,
    TownId = CurrentTown.TownId,
    Message = defaultCitizenMessage,
    Descriptor = defaultCitizenDescriptor,
    PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
    RecordLog = false
};
ProduceEvent(defaultCitizenEvent);
return CaseInvestigationResult.Succeeded(defaultCitizenMessage, sessionChanged: true);
```

This preserves the exact same normal-path behavior (steps 4-6 are identical to the current code) while adding the override path before them. The `Suspect? forcedSuspect = null;` variable uses a nullable reference type — add `#nullable enable` at the top of the file if not already enabled, or use `suspect = null!;` pattern matching the existing `TryGetConfrontableSaloonPersonOfInterestCandidateInTown` style at line 3015.

- [ ] **Step 9: Run domain tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~DevSaloonOverrideTests"`
Expected: PASS — all 12 tests green.

- [ ] **Step 10: Run full domain test suite to verify no regressions**

Run: `dotnet test tests/WildBunch.Domain.Tests`
Expected: All existing tests still pass, including `GameSessionSaloonPersonOfInterestTests`.

- [ ] **Step 11: Commit**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSessionEventReplay.cs tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs
git commit -m "BUNCH-90: add GameSession dev saloon override state, command methods, consume-once"
```

---

## Task 3: Application — Dev DTOs, query, command handlers

**Files:**
- Create: `src/WildBunch.Application/Dev/Models/SaloonDevContextDto.cs`
- Create: `src/WildBunch.Application/Dev/Models/ForceSaloonOverrideRequestDto.cs`
- Create: `src/WildBunch.Application/Dev/Queries/GetSaloonDevContextQuery.cs`
- Create: `src/WildBunch.Application/Dev/Queries/GetSaloonDevContextHandler.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ForceSaloonOverrideCommand.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ForceSaloonOverrideHandler.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ClearSaloonOverrideCommand.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ClearSaloonOverrideHandler.cs`
- Create: `src/WildBunch.Application/Dev/Mapping/SaloonDevContextMapper.cs`
- Test: `tests/WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.cs`
- Test: `tests/WildBunch.Application.Tests/Dev/ForceSaloonOverrideHandlerTests.cs`
- Test: `tests/WildBunch.Application.Tests/Dev/ClearSaloonOverrideHandlerTests.cs`

**Interfaces:**
- Consumes: `GameSession.PendingDevSaloonOverride`, `GameSession.ForceDevSaloonOverride()`, `GameSession.ClearDevSaloonOverride()` from Task 2
- Produces: `GetSaloonDevContextHandler`, `ForceSaloonOverrideHandler`, `ClearSaloonOverrideHandler`, dev DTOs — consumed by Task 4 (API endpoints).

- [ ] **Step 1: Write dev DTOs**

```csharp
// src/WildBunch.Application/Dev/Models/SaloonDevContextDto.cs
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Dev.Models;

public sealed record SaloonDevContextDto(
    Guid SessionId,
    bool InSaloonContext,
    string? CurrentTownId,
    string? CurrentTownName,
    bool SaloonAvailable,
    bool SaloonSourceSpent,
    string? ActiveSaloonPersonOfInterestId,
    string? ActiveSaloonPersonOfInterestDescriptor,
    string? ActiveSaloonPersonOfInterestKind,
    string? TrueCulpritId,
    string? TrueCulpritName,
    IReadOnlyList<SuspectDevDto> Suspects,
    DevSaloonOverrideDto? PendingDevOverride);

public sealed record SuspectDevDto(
    string SuspectId,
    string Name,
    bool IsTrueCulprit,
    bool IsEligibleSaloonCandidate,
    string? IneligibilityReason);

public sealed record DevSaloonOverrideDto(
    string ForcedKind,
    string? ForcedSuspectId,
    string? ForcedSuspectName);
```

```csharp
// src/WildBunch.Application/Dev/Models/ForceSaloonOverrideRequestDto.cs
namespace WildBunch.Application.Dev.Models;

public sealed record ForceSaloonOverrideRequestDto(
    string ForcedKind,
    string? ForcedSuspectId);
```

- [ ] **Step 2: Write the query and handler**

```csharp
// src/WildBunch.Application/Dev/Queries/GetSaloonDevContextQuery.cs
namespace WildBunch.Application.Dev.Queries;

public sealed record GetSaloonDevContextQuery(Guid SessionId);
```

```csharp
// src/WildBunch.Application/Dev/Queries/GetSaloonDevContextHandler.cs
using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Mapping;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Queries;

public sealed class GetSaloonDevContextHandler
{
    private readonly IGameSessionRepository _repository;

    public GetSaloonDevContextHandler(IGameSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<SaloonDevContextDto> HandleAsync(GetSaloonDevContextQuery query, CancellationToken cancellationToken = default)
    {
        var sessionId = new GameSessionId(query.SessionId);
        var session = await _repository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        return SaloonDevContextMapper.ToDto(session);
    }
}
```

- [ ] **Step 3: Write the mapper**

```csharp
// src/WildBunch.Application/Dev/Mapping/SaloonDevContextMapper.cs
using WildBunch.Application.Dev.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Mapping;

public static class SaloonDevContextMapper
{
    public static SaloonDevContextDto ToDto(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var townState = session.CurrentTownVisit.CurrentTownState;
        var trueCulprit = session.CaseFile.Suspects.FirstOrDefault(s => s.Id.Equals(session.CaseFile.TrueCulpritId));
        var devOverride = session.PendingDevSaloonOverride;

        var suspects = session.CaseFile.Suspects.Select(s => new SuspectDevDto(
            s.Id.Value,
            s.Name,
            IsTrueCulprit: s.Id.Equals(session.CaseFile.TrueCulpritId),
            IsEligibleSaloonCandidate: IsEligibleCandidate(session, s),
            IneligibilityReason: GetIneligibilityReason(session, s))).ToArray();

        return new SaloonDevContextDto(
            session.Id.Value,
            InSaloonContext: session.CurrentActionContext == TownActionContext.Saloon,
            CurrentTownId: session.Player.CurrentTownId.Value,
            CurrentTownName: session.CurrentTown.TownName,
            SaloonAvailable: session.CurrentTown.IsAvailable(InvestigationSourceKind.SaloonLookAround),
            SaloonSourceSpent: townState.IsSpent(InvestigationSourceKind.SaloonLookAround),
            ActiveSaloonPersonOfInterestId: townState.ActiveSaloonPersonOfInterestId?.Value,
            ActiveSaloonPersonOfInterestDescriptor: townState.ActiveSaloonPersonOfInterestDescriptor,
            ActiveSaloonPersonOfInterestKind: townState.ResolveActiveSaloonPersonOfInterestKind()?.ToString(),
            TrueCulpritId: session.CaseFile.TrueCulpritId.Value,
            TrueCulpritName: trueCulprit?.Name,
            Suspects: suspects,
            PendingDevOverride: devOverride is null ? null : MapOverride(devOverride, session.CaseFile));
    }

    private static bool IsEligibleCandidate(GameSession session, Suspect suspect)
    {
        if (suspect.Id.Equals(session.CaseFile.TrueCulpritId))
            return false;

        if (!session.TryGetKnownWarrantForSuspect(suspect.Id, out _))
            return true;

        if (!session.TryGetWantedSuspectPresenceState(suspect.Id, out var presenceState))
            return false;

        return presenceState is WantedSuspectPresenceState.AvailableInTown or WantedSuspectPresenceState.GoneToGround;
    }

    private static string? GetIneligibilityReason(GameSession session, Suspect suspect)
    {
        if (suspect.Id.Equals(session.CaseFile.TrueCulpritId))
            return "Is the true culprit (never appears as saloon POI).";

        if (session.TryGetKnownWarrantForSuspect(suspect.Id, out _))
        {
            if (!session.TryGetWantedSuspectPresenceState(suspect.Id, out var presenceState))
                return "Has a known warrant but no presence state recorded.";

            if (presenceState is not (WantedSuspectPresenceState.AvailableInTown or WantedSuspectPresenceState.GoneToGround))
                return $"Has a known warrant but presence state is {presenceState}.";
        }

        return null;
    }

    private static DevSaloonOverrideDto MapOverride(DevSaloonOverride overrideValue, CaseFile caseFile)
    {
        var suspectName = overrideValue.ForcedSuspectId is null
            ? null
            : caseFile.Suspects.FirstOrDefault(s => s.Id.Equals(overrideValue.ForcedSuspectId!.Value))?.Name;

        return new DevSaloonOverrideDto(
            overrideValue.ForcedKind.ToString(),
            overrideValue.ForcedSuspectId?.Value,
            suspectName);
    }
}
```

Note: `TryGetKnownWarrantForSuspect` (`GameSession.cs:849`, `public`) and `TryGetWantedSuspectPresenceState` (`GameSession.cs:849`, `public`) are public methods on `GameSession`. The mapper calls them directly. `PendingDevSaloonOverride` is `internal` and accessible via `InternalsVisibleTo("WildBunch.Application")` (`src/WildBunch.Domain/Properties/AssemblyInfo.cs:4`). The eligibility logic mirrors `IsEligibleSaloonPersonOfInterestCandidate()` at `GameSession.cs:3019`.

- [ ] **Step 4: Write the force command and handler**

```csharp
// src/WildBunch.Application/Dev/Commands/ForceSaloonOverrideCommand.cs
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Dev.Commands;

public sealed record ForceSaloonOverrideCommand(
    Guid GameSessionId,
    DevSaloonPoiKind ForcedKind,
    SuspectId? ForcedSuspectId);
```

```csharp
// src/WildBunch.Application/Dev/Commands/ForceSaloonOverrideHandler.cs
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Commands;

public sealed class ForceSaloonOverrideHandler : GameSessionCommandHandler
{
    public ForceSaloonOverrideHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ForceSaloonOverrideCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            var overrideValue = command.ForcedKind switch
            {
                DevSaloonPoiKind.Suspect when command.ForcedSuspectId is not null
                    => DevSaloonOverride.ForSuspect(command.ForcedSuspectId.Value),
                DevSaloonPoiKind.Suspect
                    => DevSaloonOverride.ForAnySuspect(),
                DevSaloonPoiKind.Citizen
                    => DevSaloonOverride.ForCitizen(),
                DevSaloonPoiKind.FalseLead
                    => DevSaloonOverride.ForFalseLead(),
                _ => throw new ArgumentException($"Invalid forced kind: {command.ForcedKind}")
            };

            session.ForceDevSaloonOverride(overrideValue);
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 5: Write the clear command and handler**

```csharp
// src/WildBunch.Application/Dev/Commands/ClearSaloonOverrideCommand.cs
namespace WildBunch.Application.Dev.Commands;

public sealed record ClearSaloonOverrideCommand(Guid GameSessionId);
```

```csharp
// src/WildBunch.Application/Dev/Commands/ClearSaloonOverrideHandler.cs
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Commands;

public sealed class ClearSaloonOverrideHandler : GameSessionCommandHandler
{
    public ClearSaloonOverrideHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ClearSaloonOverrideCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            session.ClearDevSaloonOverride();
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 6: Write failing application tests**

Create `tests/WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.cs`:

```csharp
// tests/WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.cs
using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Tests.Dev;

public sealed class GetSaloonDevContextHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsSaloonContext_WhenSessionExists()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        repository.Seed(session);

        var handler = new GetSaloonDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.SessionId);
        Assert.False(result.InSaloonContext);
        Assert.True(result.SaloonAvailable);
        Assert.False(result.SaloonSourceSpent);
        Assert.NotEmpty(result.Suspects);
    }

    [Fact]
    public async Task HandleAsync_ReturnsTrueCulpritId_InDevContext()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        repository.Seed(session);

        var handler = new GetSaloonDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(session.Id.Value));

        // Dev endpoint deliberately exposes hidden truth per ADR-0030 §7
        Assert.Equal("suspect-2", result.TrueCulpritId);
        Assert.NotNull(result.TrueCulpritName);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEligibilityFlags_ForSuspects()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        repository.Seed(session);

        var handler = new GetSaloonDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(session.Id.Value));

        var trueCulprit = result.Suspects.First(s => s.IsTrueCulprit);
        Assert.False(trueCulprit.IsEligibleSaloonCandidate);
        Assert.Contains("true culprit", trueCulprit.IneligibilityReason, StringComparison.OrdinalIgnoreCase);

        var nonCulprit = result.Suspects.First(s => !s.IsTrueCulprit);
        Assert.True(nonCulprit.IsEligibleSaloonCandidate);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDevOverride_WhenOverrideIsPending()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetSaloonDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(session.Id.Value));

        Assert.NotNull(result.PendingDevOverride);
        Assert.Equal("Suspect", result.PendingDevOverride.ForcedKind);
        Assert.Equal("suspect-1", result.PendingDevOverride.ForcedSuspectId);
    }

    [Fact]
    public async Task HandleAsync_ThrowsWhenSessionDoesNotExist()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new GetSaloonDevContextHandler(repository);

        await Assert.ThrowsAsync<GameSessionNotFoundException>(() =>
            handler.HandleAsync(new GetSaloonDevContextQuery(Guid.NewGuid())));
    }
}
```

Create `tests/WildBunch.Application.Tests/Dev/ForceSaloonOverrideHandlerTests.cs`:

```csharp
// tests/WildBunch.Application.Tests/Dev/ForceSaloonOverrideHandlerTests.cs
using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Tests.Dev;

public sealed class ForceSaloonOverrideHandlerTests
{
    [Fact]
    public async Task HandleAsync_ForcesSuspectOverride_WithSuspectId()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        repository.Seed(session);

        var handler = new ForceSaloonOverrideHandler(repository, repository);

        await handler.HandleAsync(new ForceSaloonOverrideCommand(
            session.Id.Value,
            DevSaloonPoiKind.Suspect,
            new SuspectId("suspect-1")));

        Assert.Equal(1, repository.StoreCalls);
        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded!.PendingDevSaloonOverride);
        Assert.Equal(DevSaloonPoiKind.Suspect, reloaded.PendingDevSaloonOverride!.ForcedKind);
        Assert.Equal(new SuspectId("suspect-1"), reloaded.PendingDevSaloonOverride.ForcedSuspectId);
    }

    [Fact]
    public async Task HandleAsync_ForcesCitizenOverride_WithoutSuspectId()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        repository.Seed(session);

        var handler = new ForceSaloonOverrideHandler(repository, repository);

        await handler.HandleAsync(new ForceSaloonOverrideCommand(
            session.Id.Value,
            DevSaloonPoiKind.Citizen,
            ForcedSuspectId: null));

        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded!.PendingDevSaloonOverride);
        Assert.Equal(DevSaloonPoiKind.Citizen, reloaded.PendingDevSaloonOverride!.ForcedKind);
        Assert.Null(reloaded.PendingDevSaloonOverride.ForcedSuspectId);
    }
}
```

Create `tests/WildBunch.Application.Tests/Dev/ClearSaloonOverrideHandlerTests.cs`:

```csharp
// tests/WildBunch.Application.Tests/Dev/ClearSaloonOverrideHandlerTests.cs
using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Tests.Dev;

public sealed class ClearSaloonOverrideHandlerTests
{
    [Fact]
    public async Task HandleAsync_ClearsPendingOverride()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new ClearSaloonOverrideHandler(repository, repository);

        await handler.HandleAsync(new ClearSaloonOverrideCommand(session.Id.Value));

        Assert.Equal(1, repository.StoreCalls);
        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.Null(reloaded!.PendingDevSaloonOverride);
    }

    [Fact]
    public async Task HandleAsync_WithNoOverride_StillSucceeds_NoOp()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        repository.Seed(session);

        var handler = new ClearSaloonOverrideHandler(repository, repository);

        await handler.HandleAsync(new ClearSaloonOverrideCommand(session.Id.Value));

        // No events produced, so no store call
        Assert.Equal(0, repository.StoreCalls);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~Dev.*Saloon"`
Expected: PASS

- [ ] **Step 8: Commit**

```powershell
git add src/WildBunch.Application/Dev/ tests/WildBunch.Application.Tests/Dev/
git commit -m "BUNCH-90: add dev saloon context query and force/clear command handlers"
```

---

## Task 4: API — Dev saloon endpoints + DI registration

**Files:**
- Modify: `src/WildBunch.Api/Dev/DevEndpoints.cs` — add 3 endpoints
- Modify: `src/WildBunch.Api/DependencyInjection.cs` — register 3 handlers
- Test: `tests/WildBunch.Integration.Tests/DevSaloonEndpointTests.cs`

**Interfaces:**
- Consumes: handlers from Task 3
- Produces: `GET /api/dev/sessions/{id}/saloon-context`, `POST /api/dev/sessions/{id}/saloon/force-override`, `POST /api/dev/sessions/{id}/saloon/clear-override`

- [ ] **Step 1: Add endpoints to DevEndpoints.cs**

In `src/WildBunch.Api/Dev/DevEndpoints.cs`, add to the `MapDevEndpoints` method, after the existing travel clear-override endpoint (after line 37):

```csharp
dev.MapGet("/sessions/{id:guid}/saloon-context", GetSaloonDevContextAsync)
    .WithName("GetSaloonDevContext")
    .Produces<SaloonDevContextDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound);

dev.MapPost("/sessions/{id:guid}/saloon/force-override", ForceSaloonOverrideAsync)
    .WithName("ForceSaloonOverride")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status400BadRequest);

dev.MapPost("/sessions/{id:guid}/saloon/clear-override", ClearSaloonOverrideAsync)
    .WithName("ClearSaloonOverride")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound);
```

Add the endpoint handler methods following the existing `GetTravelDevContextAsync` pattern (guard → handler → catch `DevAccessDeniedException`/`GameSessionNotFoundException`):

```csharp
private static async Task<IResult> GetSaloonDevContextAsync(
    Guid id,
    DevRoleGuard guard,
    GetSaloonDevContextHandler handler,
    CancellationToken cancellationToken)
{
    try
    {
        guard.EnsureDevAccess();
        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(id), cancellationToken);
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

private static async Task<IResult> ForceSaloonOverrideAsync(
    Guid id,
    DevRoleGuard guard,
    ForceSaloonOverrideHandler handler,
    ForceSaloonOverrideRequestDto request,
    CancellationToken cancellationToken)
{
    try
    {
        guard.EnsureDevAccess();
        if (string.IsNullOrWhiteSpace(request.ForcedKind))
        {
            return Results.BadRequest("ForcedKind is required.");
        }
        if (!Enum.TryParse<DevSaloonPoiKind>(request.ForcedKind, ignoreCase: true, out var forcedKind))
        {
            return Results.BadRequest("Invalid ForcedKind value.");
        }

        WildBunch.Domain.Cases.SuspectId? forcedSuspectId = null;
        if (!string.IsNullOrWhiteSpace(request.ForcedSuspectId))
        {
            forcedSuspectId = new WildBunch.Domain.Cases.SuspectId(request.ForcedSuspectId);
        }

        await handler.HandleAsync(new ForceSaloonOverrideCommand(
            id, forcedKind, forcedSuspectId), cancellationToken);
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
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
}

private static async Task<IResult> ClearSaloonOverrideAsync(
    Guid id,
    DevRoleGuard guard,
    ClearSaloonOverrideHandler handler,
    CancellationToken cancellationToken)
{
    try
    {
        guard.EnsureDevAccess();
        await handler.HandleAsync(new ClearSaloonOverrideCommand(id), cancellationToken);
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

Add the `using` directives for `WildBunch.Application.Dev.Queries`, `WildBunch.Application.Dev.Commands`, `WildBunch.Application.Dev.Models`, and `WildBunch.Domain.Game` (for `DevSaloonPoiKind`) at the top of the file.

- [ ] **Step 2: Register handlers in DependencyInjection.cs**

In `src/WildBunch.Api/DependencyInjection.cs`, add to the dev services section (after line 70):

```csharp
services.AddScoped<GetSaloonDevContextHandler>();
services.AddScoped<ForceSaloonOverrideHandler>();
services.AddScoped<ClearSaloonOverrideHandler>();
```

Add the `using` directives for `WildBunch.Application.Dev.Queries` and `WildBunch.Application.Dev.Commands`.

- [ ] **Step 3: Write integration tests**

Create `tests/WildBunch.Integration.Tests/DevSaloonEndpointTests.cs` following the existing `DevTravelEndpointTests.cs` pattern. Use `BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady()` to create a session (the Pinecross town has a saloon source available). Test:

```csharp
// tests/WildBunch.Integration.Tests/DevSaloonEndpointTests.cs
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class DevSaloonEndpointTests
{
    [Fact]
    public async Task GetSaloonDevContext_Returns200_InDevEnvironment()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(createdSession);

        var devContextResponse = await client.GetAsync(
            $"/api/dev/sessions/{createdSession!.Id}/saloon-context");

        devContextResponse.EnsureSuccessStatusCode();
        var devContext = await devContextResponse.Content.ReadFromJsonAsync<SaloonDevContextDto>();
        Assert.NotNull(devContext);
        Assert.Equal(createdSession.Id, devContext!.SessionId);
        Assert.True(devContext.SaloonAvailable);
    }

    [Fact]
    public async Task GetSaloonDevContext_Returns404_WhenSessionDoesNotExist()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/saloon-context");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ForceSaloonOverride_Returns204_AndForcesOverride()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();
        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(createdSession);

        var forceResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession!.Id}/saloon/force-override",
            new ForceSaloonOverrideRequestDto(ForcedKind: "Citizen", ForcedSuspectId: null));

        Assert.Equal(System.Net.HttpStatusCode.NoContent, forceResponse.StatusCode);

        // Verify the override is now pending
        var devContext = await client.GetFromJsonAsync<SaloonDevContextDto>(
            $"/api/dev/sessions/{createdSession.Id}/saloon-context");
        Assert.NotNull(devContext!.PendingDevOverride);
        Assert.Equal("Citizen", devContext.PendingDevOverride.ForcedKind);
    }

    [Fact]
    public async Task ForceSaloonOverride_Returns400_WhenForcedKindIsInvalid()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();
        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(createdSession);

        var forceResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession!.Id}/saloon/force-override",
            new ForceSaloonOverrideRequestDto(ForcedKind: "InvalidKind", ForcedSuspectId: null));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, forceResponse.StatusCode);
    }

    [Fact]
    public async Task ClearSaloonOverride_Returns204_AndClearsOverride()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();
        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(createdSession);

        // Force first
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession!.Id}/saloon/force-override",
            new ForceSaloonOverrideRequestDto(ForcedKind: "Citizen", ForcedSuspectId: null));

        // Clear
        var clearResponse = await client.PostAsync(
            $"/api/dev/sessions/{createdSession.Id}/saloon/clear-override", content: null);

        Assert.Equal(System.Net.HttpStatusCode.NoContent, clearResponse.StatusCode);

        // Verify the override is cleared
        var devContext = await client.GetFromJsonAsync<SaloonDevContextDto>(
            $"/api/dev/sessions/{createdSession.Id}/saloon-context");
        Assert.Null(devContext!.PendingDevOverride);
    }
}
```

Note: Check the existing `DevTravelEndpointTests.cs` for the `NonDevApiFactory` pattern to add a 403 test. If `NonDevApiFactory` exists in the test infrastructure, add a test that verifies 403 when not in dev environment.

- [ ] **Step 4: Run integration tests**

Run: `.\scripts\postgres-dev.ps1 ensure; dotnet test tests/WildBunch.Integration.Tests --filter "FullyQualifiedName~DevSaloonEndpoint"`
Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add src/WildBunch.Api/Dev/DevEndpoints.cs src/WildBunch.Api/DependencyInjection.cs tests/WildBunch.Integration.Tests/DevSaloonEndpointTests.cs
git commit -m "BUNCH-90: add dev saloon context, force-override, and clear-override endpoints"
```

---

## Task 5: Persistence — Event serializer + snapshot codec for dev saloon override

**Files:**
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs` — add 3 event types to `ResolveEventType`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` — add `PendingDevSaloonOverride` to snapshot
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs` — add serialize/deserialize methods
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionComponentNames.cs` — add component name constant
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` — persist/load component
- Test: verify existing event sourcing tests still pass + new replay test

**Interfaces:**
- Consumes: `DevSaloonOverrideForced`, `DevSaloonOverrideCleared`, `DevSaloonOverrideConsumed`, `DevSaloonOverride` from Tasks 1-2
- Produces: persistence round-trip for dev saloon override state.

- [ ] **Step 1: Add event types to ResolveEventType**

In `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`, add to the `ResolveEventType` switch (after line 52, the `DevTravelOverrideConsumed` case):

```csharp
nameof(DevSaloonOverrideForced) => typeof(DevSaloonOverrideForced),
nameof(DevSaloonOverrideCleared) => typeof(DevSaloonOverrideCleared),
nameof(DevSaloonOverrideConsumed) => typeof(DevSaloonOverrideConsumed),
```

The `using WildBunch.Domain.Events;` directive is already present (line 2).

- [ ] **Step 2: Add PendingDevSaloonOverride to snapshot**

In `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`, add a field to the `GameSessionSnapshot` record (after line 31, the `PendingDevTravelOverride` field):

```csharp
DevSaloonOverride? PendingDevSaloonOverride)
```

Add to `FromDomain` (after line 53, the `PendingDevTravelOverride` argument):

```csharp
session.PendingDevSaloonOverride);
```

Add to `ToDomain` (after line 87, the `PendingDevTravelOverride` rehydration block):

```csharp
if (PendingDevSaloonOverride is not null)
{
    GameSessionRehydrator.SetBackingField(session, "_pendingDevSaloonOverride", PendingDevSaloonOverride);
}
```

Add `using WildBunch.Domain.Game;` if not already present (it is, since `DevTravelOverride` is in that namespace and already referenced).

- [ ] **Step 3: Add serialize/deserialize methods to Components**

In `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`, add after the `DeserializePendingDevTravelOverride` method (after line 127):

```csharp
public string? SerializePendingDevSaloonOverride(DevSaloonOverride? overrideValue)
    => overrideValue is null ? null : JsonSerializer.Serialize(overrideValue, Options);

public DevSaloonOverride? DeserializePendingDevSaloonOverride(string? json)
    => json is null ? null : Deserialize<DevSaloonOverride>(json);
```

- [ ] **Step 4: Add component name constant**

In `src/WildBunch.Persistence/GameSessions/GameSessionComponentNames.cs`, add after line 17:

```csharp
internal const string PendingDevSaloonOverride = "pendingDevSaloonOverride";
```

- [ ] **Step 5: Persist/load component in EfGameSessionRepository**

In `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, in the `StageSnapshotAsync` method (or equivalent), after the `PendingDevTravelOverride` component persistence block (after line 111), add:

```csharp
var devSaloonOverrideJson = _serializer.SerializePendingDevSaloonOverride(session.PendingDevSaloonOverride);
if (devSaloonOverrideJson is null)
{
    await RemoveComponentAsync(entity.Id, GameSessionComponentNames.PendingDevSaloonOverride, cancellationToken).ConfigureAwait(false);
}
else
{
    UpsertComponent(entity.Id, GameSessionComponentNames.PendingDevSaloonOverride, devSaloonOverrideJson, now);
}
```

In the load path (after line 310, the `PendingDevTravelOverride` rehydration block), add:

```csharp
// Set PendingDevSaloonOverride from snapshot. If there are post-snapshot events,
// ApplyCommittedEvents will overwrite this via Apply(DevSaloonOverrideForced/Cleared/Consumed).
// When the snapshot is current, this restores the persisted dev override. See BUNCH-90.
var devSaloonOverrideJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.PendingDevSaloonOverride);
var pendingDevSaloonOverride = _serializer.DeserializePendingDevSaloonOverride(devSaloonOverrideJson);
if (pendingDevSaloonOverride is not null)
{
    GameSessionRehydrator.SetBackingField(session, "_pendingDevSaloonOverride", pendingDevSaloonOverride);
}
```

- [ ] **Step 6: Run event sourcing tests to verify no regressions**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~EventSourcing"`
Expected: PASS — existing replay tests still green.

- [ ] **Step 7: Verify replay tests pass with persistence**

The replay tests (`RehydrateFromEvents_WithDevSaloonOverrideForced_ReconstructsOverrideState`, `RehydrateFromEvents_AfterSaloonConsumption_HasNoPendingOverride`, `RehydrateFromEvents_WithNoDevSaloonOverride_HasNoPendingOverride`) were added in Task 2 Step 1. After adding the persistence serializer entries, these tests exercise the full persistence round-trip path. Verify they still pass:

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~DevSaloonOverrideTests"`
Expected: PASS — all 12 tests green, including the three replay tests.

- [ ] **Step 8: Run full domain test suite**

Run: `dotnet test tests/WildBunch.Domain.Tests`
Expected: PASS — no regressions from the serializer changes.

- [ ] **Step 9: Commit**

```powershell
git add src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs src/WildBunch.Persistence/GameSessions/GameSessionComponentNames.cs src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs
git commit -m "BUNCH-90: persist dev saloon override in event stream and snapshot"
```

---

## Task 6: Frontend — SaloonDevPanel + dev API client + registry

**Files:**
- Modify: `src/WildBunch.Web/src/dev/types.ts` — add saloon dev DTOs
- Modify: `src/WildBunch.Web/src/dev/devApi.ts` — add saloon dev API functions
- Create: `src/WildBunch.Web/src/dev/panels/SaloonDevPanel.tsx` — new panel component
- Modify: `src/WildBunch.Web/src/dev/DevPanelRegistry.tsx` — register the panel
- Test: `src/WildBunch.Web/src/tests/SaloonDevPanel.test.tsx`

**Interfaces:**
- Consumes: `/api/dev/sessions/{id}/saloon-context`, `/api/dev/sessions/{id}/saloon/force-override`, `/api/dev/sessions/{id}/saloon/clear-override` from Task 4
- Produces: `SaloonDevPanel` registered in the dev overlay sidebar.

**Contextual visibility:** The `SaloonDevPanel` appears in the dev overlay sidebar alongside `TravelDevPanel`. It is always available in the registry (the registry is static), but the panel content adapts to the current game state: if no session is active, it shows "No active session."; if the saloon is not available in the current town, it shows the saloon-unavailable state but still shows suspect/culprit truth for debugging.

- [ ] **Step 1: Add TypeScript DTOs to types.ts**

Append to `src/WildBunch.Web/src/dev/types.ts`:

```typescript
export interface SuspectDevDto {
  suspectId: string;
  name: string;
  isTrueCulprit: boolean;
  isEligibleSaloonCandidate: boolean;
  ineligibilityReason: string | null;
}

export interface DevSaloonOverrideDto {
  forcedKind: string;
  forcedSuspectId: string | null;
  forcedSuspectName: string | null;
}

export interface SaloonDevContextDto {
  sessionId: string;
  inSaloonContext: boolean;
  currentTownId: string | null;
  currentTownName: string | null;
  saloonAvailable: boolean;
  saloonSourceSpent: boolean;
  activeSaloonPersonOfInterestId: string | null;
  activeSaloonPersonOfInterestDescriptor: string | null;
  activeSaloonPersonOfInterestKind: string | null;
  trueCulpritId: string | null;
  trueCulpritName: string | null;
  suspects: SuspectDevDto[];
  pendingDevOverride: DevSaloonOverrideDto | null;
}

export interface ForceSaloonOverrideRequestDto {
  forcedKind: string;
  forcedSuspectId?: string | null;
}
```

- [ ] **Step 2: Add API functions to devApi.ts**

Append to `src/WildBunch.Web/src/dev/devApi.ts`:

```typescript
import type { SaloonDevContextDto, ForceSaloonOverrideRequestDto } from "./types";

export function getSaloonDevContext(gameId: string) {
  return requestJson<SaloonDevContextDto>(`/api/dev/sessions/${gameId}/saloon-context`);
}

export function forceSaloonOverride(gameId: string, request: ForceSaloonOverrideRequestDto) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/saloon/force-override`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function clearSaloonOverride(gameId: string) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/saloon/clear-override`, {
    method: "POST",
  });
}
```

Update the import line at the top of `devApi.ts` to include the new types:

```typescript
import type { ForceTravelOverrideRequestDto, SessionAuditDto, TravelDevContextDto, SaloonDevContextDto, ForceSaloonOverrideRequestDto } from "./types";
```

- [ ] **Step 3: Write the SaloonDevPanel component**

Create `src/WildBunch.Web/src/dev/panels/SaloonDevPanel.tsx`, mirroring the `TravelDevPanel.tsx` pattern (useState for form fields, useQuery for context, manual mutation via async functions with refresh):

```tsx
// src/WildBunch.Web/src/dev/panels/SaloonDevPanel.tsx
import { useState } from "react";
import styled from "styled-components";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useGameSession } from "../../state/useGameSession";
import { clearSaloonOverride, forceSaloonOverride, getSaloonDevContext } from "../devApi";

const POI_KINDS = ["Suspect", "Citizen", "FalseLead"] as const;

export function SaloonDevPanel() {
  const { gameId } = useGameSession();
  const queryClient = useQueryClient();

  const [kind, setKind] = useState<string>("Citizen");
  const [suspectId, setSuspectId] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const [actionPending, setActionPending] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["dev-saloon-context", gameId],
    queryFn: () => getSaloonDevContext(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  if (!gameId) {
    return <MutedText>No active session.</MutedText>;
  }

  if (isLoading) {
    return <MutedText>Loading saloon context...</MutedText>;
  }

  const refresh = () => queryClient.invalidateQueries({ queryKey: ["dev-saloon-context", gameId] });

  const handleForce = async () => {
    setError(null);
    setActionPending(true);
    try {
      await forceSaloonOverride(gameId, {
        forcedKind: kind,
        forcedSuspectId: suspectId.trim() === "" ? null : suspectId.trim(),
      });
      refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to force override.");
    } finally {
      setActionPending(false);
    }
  };

  const handleClear = async () => {
    setError(null);
    setActionPending(true);
    try {
      await clearSaloonOverride(gameId);
      refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to clear override.");
    } finally {
      setActionPending(false);
    }
  };

  return (
    <Container>
      <Section>
        <SectionTitle>Saloon context</SectionTitle>
        <Row>
          <Label>In saloon:</Label>
          <Value>{data?.inSaloonContext ? "Yes" : "No"}</Value>
        </Row>
        {data?.currentTownName && (
          <Row>
            <Label>Town:</Label>
            <Value>{data.currentTownName}</Value>
          </Row>
        )}
        <Row>
          <Label>Saloon available:</Label>
          <Value>{data?.saloonAvailable ? "Yes" : "No"}</Value>
        </Row>
        <Row>
          <Label>Source spent:</Label>
          <Value>{data?.saloonSourceSpent ? "Yes" : "No"}</Value>
        </Row>
        {data?.activeSaloonPersonOfInterestKind && (
          <Row>
            <Label>Active POI:</Label>
            <Value>
              {data.activeSaloonPersonOfInterestKind}
              {data.activeSaloonPersonOfInterestDescriptor
                ? ` (${data.activeSaloonPersonOfInterestDescriptor})`
                : ""}
            </Value>
          </Row>
        )}
      </Section>

      <Section>
        <SectionTitle>Hidden truth (dev-only)</SectionTitle>
        <Row>
          <Label>True culprit:</Label>
          <Value>
            {data?.trueCulpritName ?? "Unknown"} ({data?.trueCulpritId ?? "?"})
          </Value>
        </Row>
      </Section>

      <Section>
        <SectionTitle>Suspects</SectionTitle>
        {data?.suspects?.map((s) => (
          <SuspectRow key={s.suspectId}>
            <SuspectName>
              {s.name} ({s.suspectId})
              {s.isTrueCulprit && <CulpritBadge> CULPRIT</CulpritBadge>}
            </SuspectName>
            <SuspectDetail>
              {s.isEligibleSaloonCandidate
                ? "Eligible saloon candidate"
                : s.ineligibilityReason ?? "Ineligible"}
            </SuspectDetail>
          </SuspectRow>
        ))}
      </Section>

      <Section>
        <SectionTitle>Pending dev override</SectionTitle>
        {data?.pendingDevOverride ? (
          <Row>
            <Label>Override:</Label>
            <Value>
              {data.pendingDevOverride.forcedKind}
              {data.pendingDevOverride.forcedSuspectName
                ? ` (${data.pendingDevOverride.forcedSuspectName})`
                : data.pendingDevOverride.forcedSuspectId
                  ? ` (${data.pendingDevOverride.forcedSuspectId})`
                  : ""}
            </Value>
          </Row>
        ) : (
          <MutedText>None pending.</MutedText>
        )}
      </Section>

      <Section>
        <SectionTitle>Force next saloon override</SectionTitle>
        <Field>
          <Label>Kind:</Label>
          <Select value={kind} onChange={(e) => setKind(e.target.value)}>
            {POI_KINDS.map((k) => (
              <option key={k} value={k}>
                {k}
              </option>
            ))}
          </Select>
        </Field>
        {kind === "Suspect" && (
          <Field>
            <Label>Suspect ID:</Label>
            <Input
              type="text"
              value={suspectId}
              onChange={(e) => setSuspectId(e.target.value)}
              placeholder="(first eligible if blank)"
            />
          </Field>
        )}
        <ButtonRow>
          <Button type="button" onClick={handleForce} disabled={actionPending}>
            Force override
          </Button>
          <Button type="button" onClick={handleClear} disabled={actionPending}>
            Clear override
          </Button>
        </ButtonRow>
        {error && <ErrorText>{error}</ErrorText>}
      </Section>
    </Container>
  );
}

const Container = styled.div`
  display: grid;
  gap: 16px;
`;

const Section = styled.section`
  display: grid;
  gap: 6px;
`;

const SectionTitle = styled.h3`
  margin: 0 0 4px;
  font-size: 0.88rem;
  color: var(--accent);
`;

const Row = styled.div`
  display: flex;
  gap: 8px;
  font-size: 0.82rem;
`;

const Label = styled.span`
  color: var(--muted);
  flex-shrink: 0;
  min-width: 120px;
`;

const Value = styled.span`
  color: var(--text);
`;

const Field = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.82rem;
`;

const Input = styled.input`
  flex: 1;
  padding: 4px 8px;
  border-radius: 6px;
  border: 1px solid var(--border-strong);
  background: var(--bg);
  color: var(--text);
  font-size: 0.82rem;
`;

const Select = styled.select`
  flex: 1;
  padding: 4px 8px;
  border-radius: 6px;
  border: 1px solid var(--border-strong);
  background: var(--bg);
  color: var(--text);
  font-size: 0.82rem;
`;

const ButtonRow = styled.div`
  display: flex;
  gap: 8px;
  margin-top: 6px;
`;

const Button = styled.button`
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid var(--border-strong);
  background: transparent;
  color: var(--text);
  cursor: pointer;
  font-size: 0.8rem;
  font-weight: 600;
  min-height: 32px;
  transition-property: background-color, border-color;
  transition-duration: 120ms;
  transition-timing-function: ease-out;

  &:hover:not(:disabled) {
    background: var(--surface);
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

const SuspectRow = styled.div`
  display: grid;
  gap: 2px;
  font-size: 0.82rem;
  padding: 4px 0;
  border-top: 1px solid var(--border);
`;

const SuspectName = styled.span`
  color: var(--text);
  font-weight: 600;
`;

const SuspectDetail = styled.span`
  color: var(--muted);
`;

const CulpritBadge = styled.span`
  color: var(--danger);
  font-weight: 700;
  font-size: 0.7rem;
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

- [ ] **Step 4: Register SaloonDevPanel in DevPanelRegistry**

In `src/WildBunch.Web/src/dev/DevPanelRegistry.tsx`, add the import and registry entry:

```tsx
import type { ReactNode } from "react";
import { SessionAuditDevPanel } from "./panels/SessionAuditDevPanel";
import { TravelDevPanel } from "./panels/TravelDevPanel";
import { SaloonDevPanel } from "./panels/SaloonDevPanel";

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
    id: "travel-dev",
    label: "Travel dev",
    render: () => <TravelDevPanel />,
  },
  {
    id: "saloon-dev",
    label: "Saloon dev",
    render: () => <SaloonDevPanel />,
  },
];
```

- [ ] **Step 5: Write panel tests**

Create `src/WildBunch.Web/src/tests/SaloonDevPanel.test.tsx` following the existing `TravelDevPanel.test.tsx` pattern. Test:
- Renders saloon context when data is loaded.
- Shows "No active session" when no gameId.
- Shows hidden truth section with true culprit name.
- Shows suspect list with eligibility flags.
- Force button calls the API.
- Clear button works.

- [ ] **Step 6: Run frontend tests**

Run: `cd src/WildBunch.Web; npm test -- --run SaloonDevPanel`
Expected: PASS

- [ ] **Step 7: Run frontend build**

Run: `cd src/WildBunch.Web; npm run build`
Expected: Build succeeds.

- [ ] **Step 8: Commit**

```powershell
git add src/WildBunch.Web/src/dev/ src/WildBunch.Web/src/tests/SaloonDevPanel.test.tsx
git commit -m "BUNCH-90: add SaloonDevPanel with force/clear controls to dev overlay"
```

---

## Task 7: ADR update + hidden-truth guard test

**Files:**
- Modify: `docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md` — add dated status entry
- Modify: `docs/adr/INDEX.md` — update ADR-0030 timestamp
- Modify: `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs` — add dev saloon-context hidden-truth boundary test

- [ ] **Step 1: Update ADR-0030**

Add a new dated status entry to the Dated Status History in `docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md`:

```markdown
- 2026-06-26 - live (BUNCH-90): Second contextual dev module added. SaloonDevPanel in the dev overlay with dev endpoints for saloon-context query, force-override, and clear-override. New typed domain events DevSaloonOverrideForced, DevSaloonOverrideCleared, and DevSaloonOverrideConsumed. Dev saloon override is session-owned aggregate state consumed once by the next LookAroundSaloon. Normal saloon generation unchanged when no override is active. Dev DTOs separate from player DTOs. The saloon-context dev endpoint is the first dev endpoint to deliberately expose hidden culprit truth (TrueCulpritId, suspect eligibility) per ADR-0030 §7, guarded by DevRoleGuard and separated from player DTOs. Player-facing APIs remain clean of dev override state and hidden truth.
```

- [ ] **Step 2: Update ADR INDEX**

Update the ADR-0030 row in `docs/adr/INDEX.md` last-checked timestamp to `2026-06-26`.

- [ ] **Step 3: Add hidden-truth boundary test for dev saloon context**

In `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs`, add a new test method that verifies the **player-facing** saloon look-around API does not leak hidden truth, while acknowledging the dev endpoint deliberately does:

```csharp
[Fact]
public async Task DevSaloonContextDeliberatelyExposesHiddenCulprit_AndPlayerApiDoesNot()
{
    using var factory = new PostgreSqlApiFactory();
    using var client = factory.CreateClient();

    var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
    scenario.AssertReady();

    var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
    var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
    Assert.NotNull(createdSession);

    // The dev saloon-context endpoint deliberately exposes hidden truth per ADR-0030 §7.
    // This test documents that boundary: the dev endpoint contains trueCulpritId,
    // but the player-facing saloon look-around response does not.
    var devContextResponse = await client.GetAsync($"/api/dev/sessions/{createdSession!.Id}/saloon-context");
    devContextResponse.EnsureSuccessStatusCode();
    var devContextPayload = await devContextResponse.Content.ReadAsStringAsync();

    // Dev endpoint DOES contain trueCulpritId (deliberate dev-only exposure)
    Assert.Contains("\"trueCulpritId\"", devContextPayload, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("\"trueCulpritName\"", devContextPayload, StringComparison.OrdinalIgnoreCase);

    // Player-facing saloon look-around does NOT leak hidden truth
    var saloonResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/saloon/look-around", content: null);
    var saloonPayload = await saloonResponse.Content.ReadAsStringAsync();

    Assert.DoesNotContain("\"trueCulpritId\"", saloonPayload, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("\"isTrueCulprit\"", saloonPayload, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("\"linkedSuspectIds\"", saloonPayload, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("\"killerReleaseState\"", saloonPayload, StringComparison.OrdinalIgnoreCase);
}
```

Note: Verify the player-facing saloon look-around endpoint path. Check `src/WildBunch.Api/` for the actual route — it may be under `/api/games/{id}/actions/saloon/look-around` or similar. Inspect the existing `ActionEndpoints` or `InvestigationEndpoints` to find the exact route before writing the test.

- [ ] **Step 4: Run hidden truth guard tests**

Run: `.\scripts\postgres-dev.ps1 ensure; dotnet test tests/WildBunch.Integration.Tests --filter "FullyQualifiedName~HiddenTruth"`
Expected: PASS — no new hidden truth leaks through player APIs; dev endpoint deliberately exposes truth.

- [ ] **Step 5: Commit**

```powershell
git add docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md docs/adr/INDEX.md tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs
git commit -m "BUNCH-90: update ADR-0030 for saloon dev controls and hidden-truth boundary test"
```

---

## Task 8: Full validation + event-stream proof + screenshots

- [ ] **Step 1: Run full build**

Run: `dotnet build`
Expected: 0 errors, 0 warnings (or only pre-existing warnings).

- [ ] **Step 2: Run full domain test suite**

Run: `dotnet test tests/WildBunch.Domain.Tests`
Expected: All tests pass including new `DevSaloonOverrideTests` (12 tests) and existing `GameSessionSaloonPersonOfInterestTests`.

- [ ] **Step 3: Run full application test suite**

Run: `dotnet test tests/WildBunch.Application.Tests`
Expected: All tests pass including new dev handler tests.

- [ ] **Step 4: Run PostgreSQL-backed integration tests**

Run: `.\scripts\postgres-dev.ps1 ensure; dotnet test tests/WildBunch.Integration.Tests`
Expected: All tests pass including new `DevSaloonEndpointTests` and updated `GameApiHiddenTruthTests`.

- [ ] **Step 5: Run EF migrations check**

Run: `dotnet tool restore; dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
Expected: No new migration needed (dev override is in the snapshot JSON, no schema change). If a migration is needed, add it.

- [ ] **Step 6: Run frontend tests**

Run: `cd src/WildBunch.Web; npm test -- --run`
Expected: All tests pass.

- [ ] **Step 7: Run frontend build**

Run: `cd src/WildBunch.Web; npm run build`
Expected: Build succeeds.

- [ ] **Step 8: Event-stream proof (domain-level automated test)**

The event-stream proof is the `RehydrateFromEvents_AfterSaloonConsumption_HasNoPendingOverride` test in `DevSaloonOverrideTests` (Task 2 Step 1). It proves:
1. `DevSaloonOverrideForced` is emitted and sets the pending override.
2. `DevSaloonOverrideConsumed` is emitted by `LookAroundSaloon()` before `SaloonPersonOfInterestSpotted`.
3. `SaloonPersonOfInterestSpotted` is emitted with the forced POI shape.
4. Replay of `Forced → Consumed → Spotted` reconstructs a session with `_pendingDevSaloonOverride = null`.

Additionally, verify the full sequence by inspecting committed events in a test:

```csharp
[Fact]
public void EventStreamProof_ForceConsumeLookAroundConfront_ShowsCompleteSequence()
{
    var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
    var suspectId = new SuspectId("suspect-1");

    // 1. Force a suspect override
    session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
    session.MarkEventsCommitted();

    // 2. Look around the saloon (consumes override, spots suspect)
    session.LookAroundSaloon();
    session.MarkEventsCommitted();

    // 3. Confront the POI (normal gameplay continues)
    session.ConfrontSaloonPersonOfInterest();
    session.MarkEventsCommitted();

    // Verify the committed event stream contains the expected sequence
    var eventTypes = session.CommittedEvents.Select(e => e.GetType().Name).ToList();
    Assert.Contains("DevSaloonOverrideForced", eventTypes);
    Assert.Contains("DevSaloonOverrideConsumed", eventTypes);
    Assert.Contains("SaloonPersonOfInterestSpotted", eventTypes);

    // The forced event comes before consumed, which comes before spotted
    var forcedIdx = eventTypes.IndexOf("DevSaloonOverrideForced");
    var consumedIdx = eventTypes.IndexOf("DevSaloonOverrideConsumed");
    var spottedIdx = eventTypes.IndexOf("SaloonPersonOfInterestSpotted");
    Assert.True(forcedIdx < consumedIdx);
    Assert.True(consumedIdx < spottedIdx);

    // Override is null after the full sequence
    Assert.Null(session.PendingDevSaloonOverride);

    // Rehydrate from the full event stream — replay produces the same final state
    var gameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);
    var events = new[] { gameStarted }.Concat(session.CommittedEvents.OfType<IDomainEvent>()).ToList();
    var rehydrated = GameSession.RehydrateFromEvents(
        session.Id, session.World, session.CaseFile, events);
    Assert.Null(rehydrated.PendingDevSaloonOverride);
}
```

Add this test to `DevSaloonOverrideTests.cs` if not already present from Task 2.

- [ ] **Step 9: Playtest / screenshot steps**

Start the API and Vite dev server. Using the dev overlay:
1. Start a new game in a town with a saloon (e.g., Pinecross).
2. Open the dev overlay, select the "Saloon dev" panel.
3. Verify the saloon context shows: town name, saloon available = Yes, source spent = No.
4. Verify the "Hidden truth" section shows the true culprit name and ID.
5. Verify the "Suspects" section lists all suspects with eligibility flags and the culprit badge.
6. Force a "Citizen" override.
7. Verify the "Pending dev override" section shows "Citizen".
8. Perform a saloon look-around (normal player action).
9. Verify the saloon POI is a citizen (not a suspect).
10. Verify the dev override is cleared (no longer shown in the panel).
11. Force a "Suspect" override with a specific suspect ID from the suspect list.
12. Perform a saloon look-around.
13. Verify the saloon POI is the forced suspect.
14. Confront the POI normally (declare a wanted identity or not).
15. Verify the session audit panel shows the event sequence: `DevSaloonOverrideForced`, `DevSaloonOverrideConsumed`, `SaloonPersonOfInterestSpotted`, `SaloonPersonOfInterestConfronted`.
16. Take screenshots of: the SaloonDevPanel with hidden truth visible, the force-override form, the post-look-around state with override cleared, and the session audit event sequence.

- [ ] **Step 10: Final commit if any remaining changes**

```powershell
git add --all
git commit -m "BUNCH-90: event-stream proof test and validation"
```

---

## Split Conditions

This plan does not require splitting. The work is a single coherent slice: one domain concept (dev saloon override), one consume-once seam in `LookAroundSaloon()`, one set of dev endpoints, one frontend panel. The override shape (Suspect/Citizen/FalseLead) maps directly to the existing saloon POI kinds without requiring new domain concepts.

If the saloon POI forcing reveals missing domain concepts, split conditions are:

1. **If forcing a specific suspect by ID requires bypassing eligibility checks that are deeply embedded in the confrontation flow (not just the look-around flow)**, split the confrontation-bypass work into a follow-up issue. The current plan only bypasses eligibility in `LookAroundSaloon()` — the confrontation flow (`ConfrontSaloonPersonOfInterest`) still runs normally. If the dev needs to force a confrontation outcome (e.g., force surrender vs flee), that is a separate dev control surface and should be a follow-up.

2. **If the `FalseLead` kind needs to produce a different citizen descriptor or confrontation behavior than the normal citizen path**, split the false-lead-specific logic into a follow-up. The current plan treats `FalseLead` as semantically identical to `Citizen` — the false-lead outcome comes from the normal confrontation flow when the player declares a wrong wanted identity on a citizen POI. If a distinct false-lead POI shape is needed, that requires new domain concepts beyond the current saloon seams.

3. **If the dev context query needs to expose more hidden truth than `TrueCulpritId` and suspect eligibility** (e.g., warrant internals, clue anchors, seed diagnostics), split the additional truth exposure into a follow-up. The current plan exposes only the truth needed to understand saloon POI selection.
