# Dev Overlay Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current dev cockpit with a toggleable contextual dev overlay foundation that can dominate the play surface when open, disappear when closed, and become the shared extension point for future contextual dev controls.

**Architecture:** A single `DevOverlay` shell component renders as a fixed full-surface panel when toggled on, and renders nothing when off. It is mounted in `AppShell` alongside the existing `GlobalOverlays` so the normal play surface stays clean when the overlay is closed. A new `/api/dev/` endpoint namespace with a centralized `DevRoleGuard` seam separates dev-only queries from player-facing APIs. The old `DebugCockpitRoute` and its `/debug` route are retired. Future contextual panels (travel forcing, saloon forcing) register through a `DevOverlayPanel` registry pattern.

**Tech Stack:** React 18, TanStack Router, TanStack React Query, styled-components, TypeScript, Vite, Vitest, ASP.NET Core minimal APIs, C#.

## Global Constraints

- Backend remains authoritative for gameplay state; React renders server state.
- Normal player APIs and read models must not newly leak hidden truth. Dev-only endpoints MAY expose hidden truth and internal diagnostics when deliberately scoped, guarded, and separated from player DTOs. BUNCH-88 does not itself expose hidden truth, but the foundation must not establish an ADR/API convention that prevents later dev-only truth inspection.
- The dev overlay is dev-only scaffolding; keep it utilitarian per AGENTS.md and play-surface-ui.md.
- Do not implement travel encounter forcing or saloon/POI forcing in this issue.
- Do not implement real auth; add the seam only.
- The `GameSession` aggregate root remains the live-play mutation boundary.
- Dev endpoints live under `/api/dev/` and are gated by a centralized `DevRoleGuard` that returns an explicit 403 when access is denied — not an unhandled exception.
- `DevRoleGuard` lives in `WildBunch.Api` (where `IHostEnvironment` is naturally available) and is tested through `WildBunch.Integration.Tests` (which references Api and uses `WebApplicationFactory<Program>`), not `WildBunch.Application.Tests` (which references Application, Domain, GameContent — not Api).
- The overlay off state must preserve a clean normal play surface.
- Worker environment uses PowerShell; do not use `&&` for command chaining.

---

## Preflight Answers (source-grounded)

### 1. Where is the current dev cockpit rendered and what diagnostics/actions does it expose?

The dev cockpit is rendered at the `/debug` route via `DebugCockpitRoute` (`src/WildBunch.Web/src/routes/DebugCockpitRoute.tsx`). It is registered in the router (`src/WildBunch.Web/src/shell/router.tsx:17-21`) and linked from `AppShell` (`src/WildBunch.Web/src/shell/AppShell.tsx:16-24`) as a "Dev tools" link.

`DebugCockpitRoute` exposes:
- **Start game panel** (`StartGamePanel`) — seeded setup with player name, difficulty, seed code editor, seed summary, and start/refresh/randomize actions.
- **Field report panel** (`FieldReportPanel`) — player name, town, health, lawman heat, town details (services, world towns count, trails count, log entries count), inventory, store offers, and travel panel when a journey is active.
- **Available actions panel** (`AvailableActionsPanel`) — lists fetched available actions with buttons for read wanted posters, inspect notice board, check local records, follow telegraph leads, gather local gossip, look around saloon, and confront saloon person of interest.
- **Travel routes panel** (`TravelRoutesPanel`) — connected destinations with travel previews and travel buttons.
- **Case file overlay** (`CockpitOverlayFrame` + `CaseFileSurface`) — opened via "Open case file" button.
- **Reset button** — calls `handleReset`.

The cockpit duplicates functionality already available in the flow surfaces: `PreSessionSurface` has `StartGamePanel`, `TownHubSurface` has place cards for store/sheriff/saloon/trailhead, `TrailFlowSurface` has `TravelPanel`, and `GlobalOverlays` has case file / wanted / journal overlays.

### 2. Which current cockpit information should move into the new dev overlay foundation, and which should be retired?

**Move into dev overlay (dev-useful diagnostics not in the play surface):**
- Field report raw diagnostics: town ID, world towns count, trails count, log entries count, services bitmask — these are debug-shaped data, not player-facing.
- Raw session status / cockpit mode label.
- Travel route preview details at a diagnostic level (trail IDs, route profiles).

**Retire (already in the play surface or not needed):**
- `StartGamePanel` — already in `PreSessionSurface`; the cockpit duplicate is not needed.
- `AvailableActionsPanel` action buttons — already in `TownHubSurface` place cards and place surfaces.
- `TravelRoutesPanel` travel buttons — already in `TravelPrepSurface`.
- `CaseFileSurface` overlay — already in `GlobalOverlays`.
- `FieldReportPanel`'s inventory/store/travel panels — already in place surfaces.
- Reset button — can move into the dev overlay as a dev utility.

The dev overlay becomes a contextual diagnostics surface, not a duplicate command surface.

### 3. Where are the current web shell, layout, route/view, and play-surface state seams?

- **App entry:** `src/WildBunch.Web/src/App.tsx` — wraps `RouterProvider` in `GameSessionProvider`.
- **Router:** `src/WildBunch.Web/src/shell/router.tsx` — root route renders `AppShell`; `/` renders `GameFlowRouter`; `/debug` renders `DebugCockpitRoute`.
- **App shell:** `src/WildBunch.Web/src/shell/AppShell.tsx` — renders `Hud`, `GlobalOverlays` (overlay bar with case-file/wanted buttons + modal overlays), `shell-dev-nav` (Dev tools link), and `Outlet` for route content.
- **Game flow router:** `src/WildBunch.Web/src/flow/GameFlowRouter.tsx` — switches on `useGamePhase` (pre-session / in-town / on-trail / arrival) to render the appropriate flow surface.
- **Flow surfaces:** `PreSessionSurface`, `TownHubSurface`, `TrailFlowSurface`, `ArrivalSurface`, `TravelPrepSurface`, and place surfaces (`StorePlace`, `SheriffPlace`, `SaloonPlace`).
- **State:** `GameSessionProvider` (`src/state/GameSessionProvider.tsx`) wraps `useCurrentGameSession` hook and provides session/actions/journal/wantedPosters/storeOffers via context. `useGamePhase` derives phase from session.
- **Global overlays:** `GlobalOverlays` (`src/flow/GlobalOverlays.tsx`) owns the overlay bar buttons and renders `CockpitOverlayFrame` modals for case-file/wanted/journal.
- **Overlay primitive:** `CockpitOverlayFrame` (`src/components/CockpitOverlayFrame.tsx`) — modal dialog with focus management, Escape close, backdrop close.

The dev overlay should mount in `AppShell` as a sibling of `GlobalOverlays` and the route outlet, toggled by shell-level state, so it can dominate the play surface when open and disappear when closed.

### 4. How can the overlay be toggled so it can dominate the play surface when open and disappear cleanly when closed?

The overlay uses a fixed-position full-surface panel (`position: fixed; inset: 0; z-index` above the play surface) when open, and renders `null` when closed. Toggle state lives in `AppShell` as local `useState<boolean>`. The toggle button replaces the current "Dev tools" link in `shell-dev-nav`.

When open, the overlay covers the HUD, overlay bar, and route outlet. When closed, it renders nothing, so the normal play surface is completely clean. This is simpler than a route-based approach because it doesn't require URL changes and can coexist with any game phase.

Escape key closes the overlay (reusing the focus-management pattern from `CockpitOverlayFrame`).

### 5. What backend API command/query conventions exist today, and where should dev-only endpoints live?

Current API conventions:
- All player-facing endpoints are under `/api/games` (mapped in `GameEndpoints.cs`).
- Sub-groups: session (`POST /api/games`, `GET /api/games/{id}`), actions, investigations, journal, wanted-posters, town-store, travel, projections.
- Commands are `POST`; queries are `GET`.
- Each endpoint calls an Application-layer handler (`StartNewGameHandler`, `GetGameSessionHandler`, etc.).
- Handlers are registered in `DependencyInjection.cs` as scoped services.
- Projection endpoints (`ProjectionEndpoints.cs`) expose only HUD and diary projections; full audit is explicitly not player-facing.

Dev-only endpoints should live under a new `/api/dev/` route group, mapped by a `DevEndpoints` static class. This separates the dev namespace from player-facing `/api/games`. Dev endpoints call dev-specific query handlers that can access the full audit projection and other dev-only data. The dev route group is registered in `MapWildBunchApi` alongside `MapGameEndpoints`.

### 6. What is the smallest centralized future auth/dev-role guard seam?

A `DevRoleGuard` class in `WildBunch.Api/Dev/`, registered as a scoped service in `DependencyInjection.cs`. It exposes a single `EnsureDevAccess()` method that no-ops in development and throws `DevAccessDeniedException` in non-development environments. Each dev endpoint calls `guard.EnsureDevAccess()` inside a try block that catches `DevAccessDeniedException` and returns `Results.Forbid()` (HTTP 403). This produces an explicit 403 response, not an unhandled exception.

The guard lives in `WildBunch.Api` because it depends on `IHostEnvironment`, which is naturally available there. `WildBunch.Application.Tests` does not reference Api, so the guard is tested through `WildBunch.Integration.Tests` (which references Api and uses `WebApplicationFactory<Program>`) — the same pattern used for `ProjectionEndpointTests`.

This is the smallest seam: one class, one exception type, one method, one registration. Future auth implementations replace the body of `EnsureDevAccess` without changing call sites. The guard checks `IHostEnvironment.IsDevelopment()` now; later it can check claims, headers, or a dev-role token.

### 7. Which normal player DTO/API/read-model surfaces currently expose state, and how will the dev endpoint boundary avoid hidden-truth leakage?

Current player-facing DTOs:
- `GameSessionDto` — player, world, caseFile, inventory, clock, pursuitState, journey, travelDiary, logEntries, activeSaloonPersonOfInterest. No hidden culprit fields exposed.
- `JournalDto` — caseFile (with caseSummary, discoveredSuspects, caseBoard, knownClues, knownWarrants, wantedPosters), logEntries, clock, currentTown.
- `HudProjection` / `DiaryProjection` — safe projections per ADR-0028.
- `AvailableActionDto[]` — action kinds and labels.
- `WantedPosterDto[]` — target display name, features, bounty, etc.

The `FullAuditProjection` (`FullAuditProjector.cs`) is explicitly a developer/replay surface and is NOT exposed through any current player-facing endpoint. It derives event type names and summaries from the event stream. The existing `ProjectionEndpointTests.GetAuditProjection_IsNotExposedOnPlayerFacingApi` test proves the audit endpoint is not reachable under `/api/games/{id}/projections/audit` (returns 404).

The existing `GameApiHiddenTruthTests` test proves player-facing responses don't contain hidden markers (`trueCulpritId`, `isTrueCulprit`, `linkedSuspectIds`, `killerReleaseState`, gang member names).

Dev endpoint boundary strategy:
- **The boundary is player-vs-dev, not truth-vs-no-truth.** Normal player APIs and read models must not newly leak hidden truth. Dev-only endpoints MAY expose hidden truth and internal diagnostics when deliberately scoped, guarded, and separated from player DTOs. BUNCH-88 does not itself expose hidden truth, but the foundation must not establish a convention that prevents later dev-only truth inspection.
- Dev endpoints under `/api/dev/` may return `FullAuditProjection` and other dev-only data now, and may later expose hidden truth (culprit identity, internal encounter state, seed internals) when a future issue deliberately scopes that.
- Dev DTOs are separate types in a `Dev/` folder, not reused player DTOs.
- Dev query handlers derive from the event stream but return dev-shaped DTOs.
- The `DevRoleGuard` prevents non-dev access with an explicit 403 response.
- Player-facing DTOs are unchanged — no hidden truth leaks into them.
- The existing `GameApiHiddenTruthTests` pattern continues to guard the player boundary. A new integration test proves the dev endpoint is reachable under `/api/dev/` while the player-facing audit path remains 404.

### 8. What extension pattern will later travel and saloon/POI panels use?

A `DevOverlayPanel` registry pattern:
- Each contextual dev panel is a small React component under `src/dev/panels/`.
- A `DevPanelRegistry` (simple array of `{ id, label, render }` entries) defines which panels are available.
- The `DevOverlay` shell renders a sidebar/tab bar from the registry and renders the active panel's content.
- Future travel forcing adds a `TravelDevPanel` entry; saloon forcing adds a `SaloonDevPanel` entry.
- Each panel fetches from `/api/dev/` endpoints via a `devApi.ts` client module.

For the backend, each dev feature adds endpoints to the `/api/dev/` group and dev query handlers in `Application.Dev` (or a dev-specific area). The `DevRoleGuard` already gates them.

This issue establishes the registry, the shell, the first diagnostic panel, and the endpoint namespace — but does not implement travel or saloon forcing panels.

### 9. What tests and screenshots prove overlay off/on behavior and cockpit retirement?

**Automated tests (Vitest + Testing Library):**
- AppShell renders the dev overlay toggle button.
- Clicking the toggle opens the dev overlay (overlay content is visible).
- Clicking the toggle again or pressing Escape closes the dev overlay (overlay content is not in the DOM).
- When the overlay is closed, the normal play surface (HUD, flow content) is visible and unobstructed.
- The `/debug` route no longer renders `DebugCockpitRoute` (route removed or redirects).
- `DebugCockpitRoute` is no longer imported by the router.
- Dev overlay renders the first diagnostic panel content.
- Dev API client calls `/api/dev/` namespace.

**Backend tests (xUnit — `WildBunch.Integration.Tests`):**
- Dev audit endpoint (`GET /api/dev/sessions/{id}/audit`) returns 200 with `SessionAuditDto`-shaped data when running in the development environment (the default `WebApplicationFactory` environment).
- Dev audit endpoint returns 403 when the environment is overridden to Production (proves the `DevRoleGuard` denial path produces an explicit HTTP 403, not an unhandled exception).
- Dev audit endpoint returns 404 when the session ID does not exist.
- Player-facing audit path (`GET /api/games/{id}/projections/audit`) still returns 404 (the existing `ProjectionEndpointTests.GetAuditProjection_IsNotExposedOnPlayerFacingApi` test continues to pass — dev namespace does not reopen the player-facing audit path).
- The existing `GameApiHiddenTruthTests` continue to pass (player DTOs unchanged).

These are endpoint-level integration tests using the existing `PostgreSqlApiFactory` / `WebApplicationFactory<Program>` pattern. The denial-path test uses a test-specific factory that overrides the environment to Production via `builder.UseEnvironment("Production")` in `ConfigureWebHost`.

**Browser screenshots (playtest evidence):**
- Overlay off: clean play surface with HUD and flow content.
- Overlay on: dev overlay visible, covering the play surface, with diagnostic panel content.

### 10. What split condition would make this too large for one PR?

This issue is one PR because:
- The dev overlay shell, toggle, and retirement are tightly coupled — retiring the cockpit requires the overlay to exist.
- The dev endpoint namespace and guard seam are small and foundational.
- The first diagnostic panel (session audit) is minimal.

Split would be warranted if:
- Travel forcing or saloon forcing implementation were included (explicitly non-goals).
- A real auth system were required (explicitly non-goal — seam only).
- Multiple dev panels with complex backend queries were needed (this issue adds one panel).

None of these apply, so one PR is correct.

### 11. Current gameplay dev-surface inventory (seeds closeout)

This inventory maps the current meaningful gameplay surfaces that exist in the play-surface flow today, and identifies which will later need dev overlay coverage. It is the project-level inventory BUNCH-88 needs to seed closeout — not just a list of cockpit panels being moved or retired.

**Current player-facing gameplay surfaces (flow surfaces + HUD + overlays):**

| Surface | Source | Gameplay meaning | Dev overlay coverage needed? |
|---|---|---|---|
| Pre-session | `PreSessionSurface.tsx` | Seed setup, start new hunt | Yes — seed descriptor inspection, world variant, culprit identity |
| Town hub | `TownHubSurface.tsx` | Place selection (store, sheriff, saloon, trailhead) | Yes — force place availability, action gating diagnostics |
| Store place | `places/StorePlace.tsx` | Buy supplies, food, gear | Yes — force store offers, vendor availability |
| Sheriff place | `places/SheriffPlace.tsx` | Read wanted posters, check records | Yes — force wanted poster content, record results |
| Saloon place | `places/SaloonPlace.tsx` | Look around, gather gossip, confront POI | Yes — force POI spawn, control confrontation outcomes |
| Travel prep | `TravelPrepSurface.tsx` | Destination selection, route preview, start ride | Yes — force route profile, trail selection |
| Trail flow | `TrailFlowSurface.tsx` | Active journey, travel panel, encounter resolution | Yes — force encounter kind, control encounter outcomes (BUNCH-87 area) |
| Arrival | `ArrivalSurface.tsx` | Acknowledge arrival, enter town | Minimal — arrival is deterministic |
| HUD | `shell/Hud.tsx` | Persistent status (player, clock, location, health, cash, heat, status) | Yes — resource state diagnostics (wallet, inventory, horse, canteen at diagnostic level) |
| Case file overlay | `GlobalOverlays.tsx` → `CaseFileSurface` | Player-known clues, suspects, warrants | Yes — hidden truth inspection (culprit identity, internal suspect linkage) |
| Wanted posters overlay | `GlobalOverlays.tsx` → `WantedPosterSurface` | Posters read from town notice boards | Yes — force poster content, poster feature salience |
| Journal overlay | `GlobalOverlays.tsx` → `JournalSurface` | Player's authored record | Minimal — journal is projection output |

**Dev surfaces that later issues will add to the overlay:**

| Dev panel | Later issue | What it forces/inspects |
|---|---|---|
| Seed & world inspection | Future | Starting world descriptor, UUID seed decode, world variant, difficulty, entropy, culprit identity |
| Travel encounter forcing | BUNCH-87 area | Force specific encounter kinds on the trail, control encounter choice outcomes |
| Saloon/POI forcing | Future | Force POI spawn, control confrontation outcomes, inspect hidden POI state |
| Session state inspection | This issue (first panel) | Full audit projection from event stream |
| Resource state diagnostics | Future | Wallet, inventory, horse, canteen at a diagnostic level (not player-facing) |
| Pursuit/heat diagnostics | Future | Lawman pressure state, heat progression diagnostics |

This inventory is the seed for closeout: it tells future issues which gameplay surfaces exist, what dev overlay coverage they'll need, and which later issue owns each dev panel. BUNCH-88 establishes the foundation (shell, registry, endpoint namespace, guard, first panel) that these future panels register into.

---

## File Structure

### Frontend (`src/WildBunch.Web/src/`)

**Create:**
- `dev/DevOverlay.tsx` — toggleable full-surface dev overlay shell with sidebar/tab bar from panel registry.
- `dev/DevPanelRegistry.ts` — registry of dev panels (`{ id, label, render }` entries).
- `dev/panels/SessionAuditDevPanel.tsx` — first contextual dev panel: fetches and displays session audit from `/api/dev/`.
- `dev/devApi.ts` — dev-only API client module for `/api/dev/` endpoints.
- `dev/types.ts` — dev-only DTO types (separate from player `api/types.ts`).
- `tests/DevOverlay.test.tsx` — overlay toggle, open/close, and clean-surface tests.

**Modify:**
- `shell/AppShell.tsx` — replace "Dev tools" link with dev overlay toggle button; mount `DevOverlay`.
- `shell/router.tsx` — remove `/debug` route and `DebugCockpitRoute` import.
- `tests/AppShell.test.tsx` — update to verify dev overlay toggle instead of dev tools link.

**Delete:**
- `routes/DebugCockpitRoute.tsx` — retired.
- `tests/App.test.tsx` — retired (tests DebugCockpitRoute specifically).

### Backend (`src/WildBunch.Api/`, `src/WildBunch.Application/`)

**Create:**
- `WildBunch.Api/Dev/DevEndpoints.cs` — maps `/api/dev/` route group with audit endpoint; catches `DevAccessDeniedException` and returns 403.
- `WildBunch.Api/Dev/DevRoleGuard.cs` — centralized dev-role guard seam; throws `DevAccessDeniedException` when access is denied.
- `WildBunch.Api/Dev/DevAccessDeniedException.cs` — specific exception for dev-access denial (caught by endpoints, not a generic `UnauthorizedAccessException`).
- `WildBunch.Application/Dev/Queries/GetSessionAuditQuery.cs` — dev query for session audit.
- `WildBunch.Application/Dev/Queries/GetSessionAuditHandler.cs` — dev query handler using `FullAuditProjector`.
- `WildBunch.Application/Dev/Models/SessionAuditDto.cs` — dev-only DTO.

**Modify:**
- `WildBunch.Api/DependencyInjection.cs` — register `DevRoleGuard` and dev query handler; map dev endpoints.
- `WildBunch.Api/Program.cs` — no change expected (dev endpoints mapped via `MapWildBunchApi`).

### Tests (`tests/WildBunch.Integration.Tests/`)

**Create:**
- `Dev/DevEndpointTests.cs` — endpoint-level integration tests using `PostgreSqlApiFactory` / `WebApplicationFactory<Program>`: audit returns 200 in dev, audit returns 403 in Production environment, audit returns 404 for missing session, player-facing audit path still 404.

**Create (test infrastructure):**
- `Dev/NonDevApiFactory.cs` — test-specific `WebApplicationFactory<Program>` that overrides the environment to Production via `builder.UseEnvironment("Production")` in `ConfigureWebHost`, for the 403 denial-path test.

### Docs

**Create:**
- `docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md` — records the dev overlay shell, dev endpoint namespace, and dev-role guard seam decision.

**Modify:**
- `docs/adr/INDEX.md` — add ADR-0030 entry with freshness timestamp.

---

## Implementation Tasks

### Task 1: Backend dev-role guard seam and dev-access exception

**Files:**
- Create: `src/WildBunch.Api/Dev/DevRoleGuard.cs`
- Create: `src/WildBunch.Api/Dev/DevAccessDeniedException.cs`

**Interfaces:**
- Consumes: `IHostEnvironment` (from Microsoft.Extensions.Hosting — available in the Api project)
- Produces: `DevRoleGuard.EnsureDevAccess()` — throws `DevAccessDeniedException` when not in development; no-op in development. `DevAccessDeniedException` is a specific exception caught by dev endpoints to return 403.

**Test placement note:** `DevRoleGuard` lives in `WildBunch.Api` because it depends on `IHostEnvironment`, which is naturally available in the Api project. `WildBunch.Application.Tests` references Application, Domain, and GameContent — not Api — so it cannot test this type. The guard and endpoint behavior are tested together through `WildBunch.Integration.Tests` (which references Api and uses `WebApplicationFactory<Program>`) in Task 3. This matches the repo's existing pattern: `ProjectionEndpointTests` tests projection endpoints through the integration test project, not through unit tests.

- [ ] **Step 1: Write the dev-access exception**

```csharp
namespace WildBunch.Api.Dev;

/// <summary>
/// Thrown by DevRoleGuard when dev endpoint access is denied.
/// Dev endpoints catch this and return 403 Forbid.
/// </summary>
public sealed class DevAccessDeniedException : Exception
{
    public DevAccessDeniedException(string message) : base(message) { }
}
```

- [ ] **Step 2: Write the guard**

```csharp
using Microsoft.Extensions.Hosting;

namespace WildBunch.Api.Dev;

/// <summary>
/// Centralized dev-role guard seam. Currently checks development environment.
/// Future auth implementations replace the body of EnsureDevAccess without
/// changing call sites. Throws DevAccessDeniedException when access is denied;
/// dev endpoints catch this and return 403.
/// </summary>
public sealed class DevRoleGuard
{
    private readonly IHostEnvironment _environment;

    public DevRoleGuard(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public void EnsureDevAccess()
    {
        if (!_environment.IsDevelopment())
        {
            throw new DevAccessDeniedException("Dev endpoints are only available in the development environment.");
        }
    }
}
```

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Api/Dev/DevRoleGuard.cs src/WildBunch.Api/Dev/DevAccessDeniedException.cs
git commit -m "feat: add DevRoleGuard and DevAccessDeniedException dev-access seam"
```

Note: The guard is not unit-tested in isolation because it lives in Api and the unit test projects don't reference Api. Its behavior (allow in dev, deny in non-dev → 403) is proven by the endpoint-level integration tests in Task 3, which test the actual route contract including the denial path.

### Task 2: Backend dev session audit query and DTO

**Files:**
- Create: `src/WildBunch.Application/Dev/Models/SessionAuditDto.cs`
- Create: `src/WildBunch.Application/Dev/Queries/GetSessionAuditQuery.cs`
- Create: `src/WildBunch.Application/Dev/Queries/GetSessionAuditHandler.cs`
- Test: `src/WildBunch.Application.Tests/Dev/GetSessionAuditHandlerTests.cs`

**Interfaces:**
- Consumes: `IGameSessionRepository` (from `WildBunch.Application.Abstractions`), `FullAuditProjector` (from `WildBunch.Application.Projections`)
- Produces: `GetSessionAuditQuery(Guid sessionId)`, `GetSessionAuditHandler.HandleAsync(query, ct)` → `SessionAuditDto`

- [ ] **Step 1: Write the failing test**

```csharp
using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Application.Tests.TestHelpers;

namespace WildBunch.Application.Tests.Dev;

public class GetSessionAuditHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAuditEntriesFromEventStream()
    {
    }
}
```

Note: The full test body depends on existing test helper patterns for creating a session with events. Inspect existing handler tests in `src/WildBunch.Application.Tests/` for the fixture pattern (e.g., `StartNewGameHandlerTests` or `GetGameSessionHandlerTests`) and follow that pattern to create a session, store it, then query the audit.

The test should:
1. Create a session with at least one event (GameStarted).
2. Store it via the repository.
3. Call `GetSessionAuditHandler.HandleAsync` with the session ID.
4. Assert the returned `SessionAuditDto` has at least one entry with `EventType` = "GameStarted".

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/WildBunch.Application.Tests --filter "GetSessionAuditHandlerTests"`
Expected: FAIL — types not found.

- [ ] **Step 3: Write the DTO**

```csharp
namespace WildBunch.Application.Dev.Models;

public sealed record SessionAuditDto(
    Guid SessionId,
    IReadOnlyList<SessionAuditEntryDto> Entries);

public sealed record SessionAuditEntryDto(
    int Sequence,
    string EventType,
    string Summary,
    DateTime OccurredAtUtc);
```

- [ ] **Step 4: Write the query and handler**

```csharp
namespace WildBunch.Application.Dev.Queries;

public sealed record GetSessionAuditQuery(Guid SessionId);
```

```csharp
using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Projections;

namespace WildBunch.Application.Dev.Queries;

public sealed class GetSessionAuditHandler
{
    private readonly IGameSessionRepository _repository;
    private readonly FullAuditProjector _auditProjector;

    public GetSessionAuditHandler(IGameSessionRepository repository, FullAuditProjector auditProjector)
    {
        _repository = repository;
        _auditProjector = auditProjector;
    }

    public async Task<SessionAuditDto> HandleAsync(GetSessionAuditQuery query, CancellationToken cancellationToken)
    {
        var sessionId = new GameSessionId(query.SessionId);
        var session = await _repository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new GameSessionNotFoundException(query.SessionId);
        }

        var events = await _repository.GetEventStreamAsync(sessionId, 0, cancellationToken).ConfigureAwait(false);
        var projection = _auditProjector.Project(events);

        return new SessionAuditDto(
            query.SessionId,
            projection.Entries
                .Select(e => new SessionAuditEntryDto(e.Sequence, e.EventType, e.Summary, e.OccurredAtUtc))
                .ToList());
    }
}
```

Note: Check the existing `GameSessionNotFoundException` namespace and `GameSessionId` type in the codebase and adjust imports accordingly. The handler follows the same pattern as `ProjectionEndpoints.cs` which loads the session and event stream.

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test src/WildBunch.Application.Tests --filter "GetSessionAuditHandlerTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Application/Dev/ src/WildBunch.Application.Tests/Dev/GetSessionAuditHandlerTests.cs
git commit -m "feat: add dev session audit query and DTO"
```

### Task 3: Backend dev endpoints, DI registration, and endpoint-level integration tests

**Files:**
- Create: `src/WildBunch.Api/Dev/DevEndpoints.cs`
- Modify: `src/WildBunch.Api/DependencyInjection.cs`
- Create: `tests/WildBunch.Integration.Tests/Dev/DevEndpointTests.cs`
- Create: `tests/WildBunch.Integration.Tests/Dev/NonDevApiFactory.cs`

**Interfaces:**
- Consumes: `DevRoleGuard`, `GetSessionAuditHandler`, `PostgreSqlApiFactory`, `BoringScenarioBuilder`
- Produces: `GET /api/dev/sessions/{id}/audit` → `SessionAuditDto` (200), 403 (denied), 404 (not found)

- [ ] **Step 1: Write the dev endpoints with 403 catch**

```csharp
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Games.Exceptions;

namespace WildBunch.Api.Dev;

public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        var dev = app.MapGroup("/api/dev");

        dev.MapGet("/sessions/{id:guid}/audit", GetSessionAuditAsync)
            .WithName("GetSessionAudit")
            .Produces<SessionAuditDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetSessionAuditAsync(
        Guid id,
        DevRoleGuard guard,
        GetSessionAuditHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            var result = await handler.HandleAsync(new GetSessionAuditQuery(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (DevAccessDeniedException)
        {
            return Results.Forbid();
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
```

Note: `guard.EnsureDevAccess()` is inside the try block so `DevAccessDeniedException` is caught and returns 403, not an unhandled exception. This is the explicit dev-boundary behavior the DOD requires.

- [ ] **Step 2: Register in DependencyInjection**

Add to `AddWildBunchServices`:
```csharp
services.AddScoped<DevRoleGuard>();
services.AddScoped<GetSessionAuditHandler>();
services.AddSingleton<FullAuditProjector>();
```

Add to `MapWildBunchApi`:
```csharp
app.MapDevEndpoints();
```

Add using statements for `WildBunch.Api.Dev` and `WildBunch.Application.Dev.Queries`.

- [ ] **Step 3: Write the NonDevApiFactory for the 403 denial-path test**

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WildBunch.Api;
using WildBunch.Application.Abstractions;
using WildBunch.GameContent.Abstractions;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Dev;

/// <summary>
/// WebApplicationFactory that overrides the environment to Production
/// so DevRoleGuard denies access. Proves the 403 denial path.
/// </summary>
public sealed class NonDevApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly PostgreSqlTestDatabase _database;
    private bool _disposed;

    public NonDevApiFactory()
    {
        _database = new PostgreSqlTestDatabase();

        using var context = new WildBunchDbContext(new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options);

        context.Database.Migrate();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<WildBunchDbContext>>();
            services.RemoveAll<WildBunchDbContext>();
            services.RemoveAll<IGameSessionRepository>();
            services.RemoveAll<ITravelRandomnessSource>();

            services.AddSingleton(_database);
            services.AddDbContext<WildBunchDbContext>((_, options) => options.UseNpgsql(_database.ConnectionString));
            services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();
            services.AddSingleton<ITravelRandomnessSource, DeterministicTravelRandomnessSource>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && !_disposed)
        {
            _disposed = true;
            _database.Dispose();
        }
    }
}
```

- [ ] **Step 4: Write the endpoint-level integration tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Dev;

public sealed class DevEndpointTests
{
    [Fact]
    public async Task GetSessionAudit_Returns200_WithAuditEntriesInDevEnvironment()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(created);

        var auditResponse = await client.GetAsync($"/api/dev/sessions/{created!.Id}/audit");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var payload = await auditResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"entries\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GameStarted", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSessionAudit_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();

        // Even a valid session ID should be denied — the guard runs before the handler.
        var auditResponse = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/audit");
        Assert.Equal(HttpStatusCode.Forbidden, auditResponse.StatusCode);
    }

    [Fact]
    public async Task GetSessionAudit_Returns404_WhenSessionDoesNotExist()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var auditResponse = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/audit");
        Assert.Equal(HttpStatusCode.NotFound, auditResponse.StatusCode);
    }

    [Fact]
    public async Task PlayerFacingAuditPath_StillReturns404()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(created);

        // The player-facing audit path must remain closed even though /api/dev/ exists.
        var playerAuditResponse = await client.GetAsync($"/api/games/{created!.Id}/projections/audit");
        Assert.Equal(HttpStatusCode.NotFound, playerAuditResponse.StatusCode);
    }
}
```

Note: The `GameSessionDto` record used here follows the pattern in `ProjectionEndpointTests.cs` (line 110: `private sealed record GameSessionDto(Guid Id);`). Check whether to use the full `GameSessionDto` from `WildBunch.Application.Games.Models` or the minimal local record — the existing `ProjectionEndpointTests` uses a minimal local record, while `GameApiTests` uses the full DTO. Follow whichever pattern the existing tests in the file use.

- [ ] **Step 5: Build and run integration tests**

Run: `dotnet build`
Expected: PASS

Run: `.\scripts\postgres-dev.ps1 ensure`
Run: `dotnet test tests/WildBunch.Integration.Tests --filter "DevEndpointTests"`
Expected: PASS — all four tests pass (200 in dev, 403 in non-dev, 404 for missing session, player-facing audit still 404).

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Api/Dev/DevEndpoints.cs src/WildBunch.Api/DependencyInjection.cs tests/WildBunch.Integration.Tests/Dev/DevEndpointTests.cs tests/WildBunch.Integration.Tests/Dev/NonDevApiFactory.cs
git commit -m "feat: add /api/dev/ endpoint namespace with audit endpoint and integration tests"
```

### Task 4: Frontend dev API client and types

**Files:**
- Create: `src/WildBunch.Web/src/dev/types.ts`
- Create: `src/WildBunch.Web/src/dev/devApi.ts`

**Interfaces:**
- Consumes: `getApiBaseUrl` and `requestJson` from `../api/wildBunchApi` (extract or duplicate the base URL logic)
- Produces: `getSessionAudit(gameId: string): Promise<SessionAuditDto>`

- [ ] **Step 1: Write the dev types**

```typescript
export interface SessionAuditEntryDto {
  sequence: number;
  eventType: string;
  summary: string;
  occurredAtUtc: string;
}

export interface SessionAuditDto {
  sessionId: string;
  entries: SessionAuditEntryDto[];
}
```

- [ ] **Step 2: Write the dev API client**

The dev API client needs the same base URL logic as the player API. Extract `getApiBaseUrl` and `requestJson` from `wildBunchApi.ts` into a shared `api/httpClient.ts` module, or duplicate the minimal logic. Prefer extraction to avoid drift.

Create `src/WildBunch.Web/src/api/httpClient.ts` with `getApiBaseUrl` and `requestJson` extracted from `wildBunchApi.ts`, then update `wildBunchApi.ts` to import from `httpClient.ts`.

Then create `src/WildBunch.Web/src/dev/devApi.ts`:

```typescript
import { requestJson } from "../api/httpClient";
import type { SessionAuditDto } from "./types";

export function getSessionAudit(gameId: string) {
  return requestJson<SessionAuditDto>(`/api/dev/sessions/${gameId}/audit`);
}
```

- [ ] **Step 3: Typecheck**

Run: `cd src/WildBunch.Web; npm run typecheck`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Web/src/api/httpClient.ts src/WildBunch.Web/src/api/wildBunchApi.ts src/WildBunch.Web/src/dev/types.ts src/WildBunch.Web/src/dev/devApi.ts
git commit -m "feat: add frontend dev API client and extract shared HTTP client"
```

### Task 5: Frontend dev panel registry and first panel

**Files:**
- Create: `src/WildBunch.Web/src/dev/DevPanelRegistry.ts`
- Create: `src/WildBunch.Web/src/dev/panels/SessionAuditDevPanel.tsx`

**Interfaces:**
- Consumes: `useGameSession` (for gameId), `getSessionAudit` from `../devApi`
- Produces: `DevPanelRegistry` (array of panel definitions), `SessionAuditDevPanel` component

- [ ] **Step 1: Write the panel registry**

```typescript
import type { ReactNode } from "react";

export interface DevPanelDefinition {
  id: string;
  label: string;
  render: () => ReactNode;
}

export const devPanels: DevPanelDefinition[] = [];

export function registerDevPanel(panel: DevPanelDefinition) {
  devPanels.push(panel);
}
```

- [ ] **Step 2: Write the session audit panel**

```tsx
import { useQuery } from "@tanstack/react-query";
import { useGameSession } from "../../state/useGameSession";
import { getSessionAudit } from "../devApi";

export function SessionAuditDevPanel() {
  const { gameId } = useGameSession();

  const { data, isLoading, error } = useQuery({
    queryKey: ["dev-session-audit", gameId],
    queryFn: () => getSessionAudit(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  if (!gameId) {
    return <p className="dev-panel-muted">No active session.</p>;
  }

  if (isLoading) {
    return <p className="dev-panel-muted">Loading audit...</p>;
  }

  if (error) {
    return <p className="dev-panel-error">{error instanceof Error ? error.message : "Failed to load audit."}</p>;
  }

  if (!data || data.entries.length === 0) {
    return <p className="dev-panel-muted">No audit entries.</p>;
  }

  return (
    <div className="dev-audit-list">
      {data.entries.map((entry) => (
        <div key={entry.sequence} className="dev-audit-entry">
          <span className="dev-audit-sequence">#{entry.sequence}</span>
          <span className="dev-audit-type">{entry.eventType}</span>
          <span className="dev-audit-summary">{entry.summary}</span>
        </div>
      ))}
    </div>
  );
}
```

- [ ] **Step 3: Register the panel**

In `DevPanelRegistry.ts`, add the import and registration at module level:

```typescript
import { SessionAuditDevPanel } from "./panels/SessionAuditDevPanel";

export const devPanels: DevPanelDefinition[] = [
  {
    id: "session-audit",
    label: "Session audit",
    render: () => <SessionAuditDevPanel />,
  },
];
```

Note: Since `DevPanelRegistry.ts` now imports JSX, rename to `DevPanelRegistry.tsx` if the project requires it for JSX. Check the tsconfig `jsx` setting.

- [ ] **Step 4: Typecheck**

Run: `cd src/WildBunch.Web; npm run typecheck`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/dev/DevPanelRegistry.tsx src/WildBunch.Web/src/dev/panels/SessionAuditDevPanel.tsx
git commit -m "feat: add dev panel registry and session audit panel"
```

### Task 6: Frontend DevOverlay shell component

**Files:**
- Create: `src/WildBunch.Web/src/dev/DevOverlay.tsx`

**Interfaces:**
- Consumes: `devPanels` from `DevPanelRegistry`
- Produces: `DevOverlay` component with props `{ open: boolean; onClose: () => void }`

- [ ] **Step 1: Write the DevOverlay component**

```tsx
import { useEffect, useRef, useState } from "react";
import { devPanels } from "./DevPanelRegistry";

interface DevOverlayProps {
  open: boolean;
  onClose: () => void;
}

export function DevOverlay({ open, onClose }: DevOverlayProps) {
  const [activePanelId, setActivePanelId] = useState(devPanels[0]?.id ?? null);
  const closeButtonRef = useRef<HTMLButtonElement | null>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    previousFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    closeButtonRef.current?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      previousFocusRef.current?.focus();
    };
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  const activePanel = devPanels.find((p) => p.id === activePanelId) ?? devPanels[0];

  return (
    <div className="dev-overlay" role="dialog" aria-modal="true" aria-label="Developer overlay">
      <header className="dev-overlay__header">
        <div className="dev-overlay__title-group">
          <p className="dev-overlay__eyebrow">Dev</p>
          <h2 className="dev-overlay__title">Developer overlay</h2>
        </div>
        <button ref={closeButtonRef} type="button" className="dev-overlay__close" onClick={onClose}>
          Close
        </button>
      </header>
      <div className="dev-overlay__body">
        <nav className="dev-overlay__sidebar" aria-label="Dev panels">
          {devPanels.map((panel) => (
            <button
              key={panel.id}
              type="button"
              className={`dev-overlay__tab${panel.id === activePanel?.id ? " dev-overlay__tab--active" : ""}`}
              onClick={() => setActivePanelId(panel.id)}
            >
              {panel.label}
            </button>
          ))}
        </nav>
        <div className="dev-overlay__content">
          {activePanel ? activePanel.render() : <p className="dev-panel-muted">No panels registered.</p>}
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Add dev overlay CSS to styles.css**

Append to `src/WildBunch.Web/src/styles.css`:

```css
/* ===== Dev overlay ===== */
.dev-overlay {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: flex;
  flex-direction: column;
  background: rgba(12, 10, 8, 0.98);
  color: var(--text);
}

.dev-overlay__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 14px 20px;
  border-bottom: 1px solid var(--border);
}

.dev-overlay__eyebrow {
  margin: 0;
  color: var(--accent);
  text-transform: uppercase;
  letter-spacing: 0.18em;
  font-size: 0.72rem;
}

.dev-overlay__title {
  margin: 2px 0 0;
  font-size: 1.2rem;
}

.dev-overlay__close {
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid var(--border-strong);
  background: transparent;
  color: var(--text);
  cursor: pointer;
}

.dev-overlay__body {
  flex: 1;
  display: flex;
  overflow: hidden;
}

.dev-overlay__sidebar {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 16px 12px;
  border-right: 1px solid var(--border);
  min-width: 180px;
}

.dev-overlay__tab {
  padding: 8px 12px;
  border-radius: 8px;
  border: 1px solid transparent;
  background: transparent;
  color: var(--muted);
  text-align: left;
  cursor: pointer;
  font-size: 0.88rem;
}

.dev-overlay__tab--active {
  color: var(--text);
  background: rgba(255, 255, 255, 0.06);
  border-color: var(--border-strong);
}

.dev-overlay__content {
  flex: 1;
  overflow: auto;
  padding: 20px;
}

.dev-panel-muted {
  color: var(--muted);
}

.dev-panel-error {
  color: #f07e6e;
}

.dev-audit-list {
  display: grid;
  gap: 8px;
}

.dev-audit-entry {
  display: grid;
  grid-template-columns: auto auto 1fr;
  gap: 12px;
  padding: 10px 14px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
  font-size: 0.86rem;
}

.dev-audit-sequence {
  color: var(--muted);
  font-variant-numeric: tabular-nums;
}

.dev-audit-type {
  color: var(--accent);
  font-weight: 600;
}

.dev-audit-summary {
  color: var(--text);
}

@media (max-width: 640px) {
  .dev-overlay__body {
    flex-direction: column;
  }
  .dev-overlay__sidebar {
    flex-direction: row;
    flex-wrap: wrap;
    border-right: none;
    border-bottom: 1px solid var(--border);
    min-width: 0;
  }
}
```

- [ ] **Step 3: Typecheck**

Run: `cd src/WildBunch.Web; npm run typecheck`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Web/src/dev/DevOverlay.tsx src/WildBunch.Web/src/styles.css
git commit -m "feat: add toggleable DevOverlay shell component"
```

### Task 7: Wire DevOverlay into AppShell and retire DebugCockpitRoute

**Files:**
- Modify: `src/WildBunch.Web/src/shell/AppShell.tsx`
- Modify: `src/WildBunch.Web/src/shell/router.tsx`
- Delete: `src/WildBunch.Web/src/routes/DebugCockpitRoute.tsx`

**Interfaces:**
- Consumes: `DevOverlay` from `../dev/DevOverlay`
- Produces: AppShell with dev overlay toggle; router without `/debug` route

- [ ] **Step 1: Update AppShell to mount DevOverlay**

Replace the `ShellChrome` component in `AppShell.tsx`:

```tsx
import { useState } from "react";
import { Outlet, useRouterState } from "@tanstack/react-router";
import { Hud } from "./Hud";
import { GlobalOverlays, type OverlayKind } from "../flow/GlobalOverlays";
import { DevOverlay } from "../dev/DevOverlay";

function ShellChrome() {
  const [openOverlay, setOpenOverlay] = useState<OverlayKind>(null);
  const [devOverlayOpen, setDevOverlayOpen] = useState(false);

  return (
    <div className="v0-1-shell v0-1-shell--flow">
      <Hud onOpenJournal={() => setOpenOverlay("journal")} />
      <div className="shell-overlay-bar">
        <GlobalOverlays openOverlay={openOverlay} onOpenOverlay={setOpenOverlay} />
        <nav className="shell-dev-nav" aria-label="Developer tools">
          <button
            type="button"
            className={`shell-nav__link shell-nav__link--dev${devOverlayOpen ? " shell-nav__link--active" : ""}`}
            onClick={() => setDevOverlayOpen(true)}
          >
            Dev overlay
          </button>
        </nav>
      </div>
      <main className="route-outlet" aria-live="polite">
        <div className="route">
          <Outlet />
        </div>
      </main>
      <DevOverlay open={devOverlayOpen} onClose={() => setDevOverlayOpen(false)} />
    </div>
  );
}

export function AppShell() {
  return <ShellChrome />;
}
```

Note: Remove the `Link` and `useRouterState` imports if they are no longer used. The `isDebug` path check is no longer needed since there is no `/debug` route.

- [ ] **Step 2: Remove /debug route from router**

In `router.tsx`, remove:
- The `DebugCockpitRoute` import.
- The `debugRoute` definition.
- `debugRoute` from the `routeTree` children array.
- `debugRoute` from the exported types.

Updated `router.tsx`:

```tsx
import { createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { AppShell } from "./AppShell";
import { GameFlowRouter } from "../flow/GameFlowRouter";

const rootRoute = createRootRoute({
  component: AppShell,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: GameFlowRouter,
});

const routeTree = rootRoute.addChildren([
  indexRoute,
]);

export const router = createRouter({
  routeTree,
  defaultPreload: "intent",
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

export type { rootRoute, indexRoute };
```

- [ ] **Step 3: Delete DebugCockpitRoute**

Delete `src/WildBunch.Web/src/routes/DebugCockpitRoute.tsx`.

- [ ] **Step 4: Typecheck**

Run: `cd src/WildBunch.Web; npm run typecheck`
Expected: PASS — no references to `DebugCockpitRoute` remain.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/shell/AppShell.tsx src/WildBunch.Web/src/shell/router.tsx
git rm src/WildBunch.Web/src/routes/DebugCockpitRoute.tsx
git commit -m "feat: wire DevOverlay into AppShell, retire DebugCockpitRoute"
```

### Task 8: Update and add frontend tests

**Files:**
- Modify: `src/WildBunch.Web/src/tests/AppShell.test.tsx`
- Create: `src/WildBunch.Web/src/tests/DevOverlay.test.tsx`
- Delete: `src/WildBunch.Web/src/tests/App.test.tsx`

**Interfaces:**
- Consumes: `DevOverlay`, `router`, `GameSessionProvider`, mock API functions

- [ ] **Step 1: Update AppShell tests**

In `AppShell.test.tsx`:
- Remove any tests that navigate to `/debug` or assert "Dev tools" link text.
- Add a test that the "Dev overlay" button exists and opens the overlay.
- Add a test that pressing Escape closes the dev overlay.
- Add a test that when the overlay is closed, the normal play surface is visible.

The existing `renderShell()` and `primeMocks()` helpers can be reused. Add a mock for `getSessionAudit` to the mock block.

```typescript
// Add to the vi.mock block:
getSessionAudit: vi.fn(),

// Add to primeMocks:
mockedGetSessionAudit.mockResolvedValue({ sessionId: "game-1", entries: [] });
```

Add tests:

```typescript
it("shows a Dev overlay button that opens the developer overlay", async () => {
  primeMocks();
  renderShell();

  const user = userEvent.setup();
  const devButton = await screen.findByRole("button", { name: /dev overlay/i });
  await user.click(devButton);

  expect(await screen.findByRole("dialog", { name: /developer overlay/i })).toBeInTheDocument();
});

it("closes the dev overlay on Escape", async () => {
  primeMocks();
  renderShell();

  const user = userEvent.setup();
  await user.click(await screen.findByRole("button", { name: /dev overlay/i }));
  const dialog = await screen.findByRole("dialog", { name: /developer overlay/i });
  expect(dialog).toBeInTheDocument();

  await user.keyboard("{Escape}");
  expect(screen.queryByRole("dialog", { name: /developer overlay/i })).not.toBeInTheDocument();
});
```

- [ ] **Step 2: Write DevOverlay unit tests**

Create `src/WildBunch.Web/src/tests/DevOverlay.test.tsx`:

```typescript
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { DevOverlay } from "../dev/DevOverlay";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { getSessionAudit } from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
  getSessionAudit: vi.fn(),
}));

vi.mock("../api/wildBunchApi", () => ({
  buyStoreItem: vi.fn(),
  createGame: vi.fn(),
  getAvailableActions: vi.fn(),
  getGame: vi.fn(),
  getJournal: vi.fn(),
  getTownStoreOffers: vi.fn(),
  checkLocalRecords: vi.fn(),
  inspectNoticeBoard: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  lookAroundSaloon: vi.fn(),
  readWantedPosters: vi.fn(),
  followTelegraphLeads: vi.fn(),
  gatherLocalGossip: vi.fn(),
  travel: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  resolveTravelEncounter: vi.fn(),
  previewTravel: vi.fn(),
}));

const mockedGetSessionAudit = vi.mocked(getSessionAudit);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function renderOverlay(open: boolean, onClose = () => {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <DevOverlay open={open} onClose={onClose} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}

describe("DevOverlay", () => {
  it("renders nothing when closed", () => {
    renderOverlay(false);
    expect(screen.queryByRole("dialog", { name: /developer overlay/i })).not.toBeInTheDocument();
  });

  it("renders the overlay dialog when open", () => {
    renderOverlay(true);
    expect(screen.getByRole("dialog", { name: /developer overlay/i })).toBeInTheDocument();
  });

  it("calls onClose when the Close button is clicked", async () => {
    const onClose = vi.fn();
    renderOverlay(true, onClose);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /close/i }));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("calls onClose on Escape key", async () => {
    const onClose = vi.fn();
    renderOverlay(true, onClose);

    const user = userEvent.setup();
    await user.keyboard("{Escape}");
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("renders the session audit panel tab", () => {
    renderOverlay(true);
    expect(screen.getByRole("button", { name: /session audit/i })).toBeInTheDocument();
  });
});
```

- [ ] **Step 3: Delete App.test.tsx**

Delete `src/WildBunch.Web/src/tests/App.test.tsx` — it tested `DebugCockpitRoute` which is retired.

- [ ] **Step 4: Run frontend tests**

Run: `cd src/WildBunch.Web; npm run test`
Expected: PASS — all tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/tests/AppShell.test.tsx src/WildBunch.Web/src/tests/DevOverlay.test.tsx
git rm src/WildBunch.Web/src/tests/App.test.tsx
git commit -m "test: update AppShell tests and add DevOverlay tests, retire cockpit tests"
```

### Task 9: ADR-0030 and ADR index update

**Files:**
- Create: `docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md`
- Modify: `docs/adr/INDEX.md`

- [ ] **Step 1: Write ADR-0030**

Create `docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md`:

```markdown
# ADR-0030 Dev Overlay and Dev Endpoint Namespace

## Status

`live`

## Dated Status History

- 2026-06-25 - live: Dev overlay foundation implemented. Toggleable DevOverlay shell in AppShell replaces DebugCockpitRoute. Dev endpoints under /api/dev/ with centralized DevRoleGuard. SessionAuditDevPanel as first contextual panel. Panel registry pattern established for future travel/saloon dev panels.

## Decision Type

architecture, ui, process

## Related ADRs

- `depends on`: ADR-0028 (projection posture — FullAuditProjector is the dev audit source)
- `related to`: ADR-0007 (hidden culprit boundaries — the player-vs-dev boundary, not a blanket prohibition on dev truth)

## Context

The previous dev cockpit (`DebugCockpitRoute` at `/debug`) duplicated player-facing functionality (start game, actions, travel, case file) already available in the flow surfaces. It was a separate route that competed with the play surface rather than a contextual overlay that could augment it.

Future dev controls (travel encounter forcing, saloon/POI forcing) need a shared extension point that is clearly dev-only, toggleable, and separated from player-facing APIs. Some future dev panels will need to inspect hidden truth (culprit identity, internal encounter state, seed internals) for debugging and playtesting. The foundation must not establish a convention that prevents that.

## Decision

1. **DevOverlay shell.** A single toggleable `DevOverlay` component mounted in `AppShell` renders as a fixed full-surface panel when open and nothing when closed. Toggle state is shell-local. This replaces the `/debug` route.

2. **Dev endpoint namespace.** Dev-only endpoints live under `/api/dev/`, mapped by `DevEndpoints`, separate from player-facing `/api/games/`. Dev endpoints may return dev-only projections (FullAuditProjection) and dev-only DTOs.

3. **DevRoleGuard seam.** A centralized `DevRoleGuard` with `EnsureDevAccess()` gates every dev endpoint. Currently checks `IHostEnvironment.IsDevelopment()`. Throws `DevAccessDeniedException` which endpoints catch and return as 403. Future auth replaces the method body without changing call sites.

4. **Panel registry.** A `DevPanelRegistry` defines available dev panels as `{ id, label, render }` entries. The DevOverlay renders a sidebar from the registry. Future panels (TravelDevPanel, SaloonDevPanel) add entries without modifying the shell.

5. **Cockpit retirement.** `DebugCockpitRoute` and the `/debug` route are removed. There is one dev surface: the DevOverlay.

6. **Player-vs-dev truth boundary.** The boundary is player-vs-dev, not truth-vs-no-truth. Normal player APIs and read models must not newly leak hidden truth (per ADR-0007 and ADR-0028 §10). Dev-only endpoints MAY expose hidden truth and internal diagnostics when deliberately scoped, guarded, and separated from player DTOs. BUNCH-88 does not itself expose hidden truth, but the `/api/dev/` namespace and `DevRoleGuard` seam establish the route through which later issues can deliberately expose dev-only truth. Dev DTOs are separate types from player DTOs. The existing `GameApiHiddenTruthTests` continue to guard the player boundary.

## Options Considered and Rejected

- **Keep DebugCockpitRoute and add overlay beside it.** Rejected: two dev surfaces creates confusion and duplication.
- **Route-based overlay at /dev.** Rejected: URL-based toggle adds navigation complexity and doesn't coexist cleanly with game phase routing.
- **Generic Modal/Panel abstraction.** Rejected per play-surface-ui.md: avoid generic React infrastructure before demand. The DevOverlay is a specific dev surface, not a reusable abstraction.
- **Blanket prohibition on dev hidden-truth exposure.** Rejected: this would prevent future dev-only truth inspection (culprit identity, encounter internals, seed diagnostics) that playtesting and debugging require. The correct boundary is player-vs-dev with the guard seam, not truth-vs-no-truth.

## Consequences

- Future dev panels register through the registry and fetch from `/api/dev/`.
- Dev endpoint access is centralized through one guard seam with an explicit 403 denial path.
- The play surface is clean when the overlay is closed.
- The FullAuditProjector is now exposed through a dev endpoint, but only in development and only through dev DTOs.
- Later issues may add dev endpoints that expose hidden truth (culprit identity, internal state) through the same guarded `/api/dev/` namespace without violating ADR-0007, because ADR-0007's boundary is player-facing, not dev-facing.
```

- [ ] **Step 2: Update ADR index**

Add to `docs/adr/INDEX.md`:

```
| ADR-0030 | 2026-06-25 | New — dev overlay and dev endpoint namespace |
```

- [ ] **Step 3: Commit**

```bash
git add docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md docs/adr/INDEX.md
git commit -m "docs: add ADR-0030 for dev overlay and dev endpoint namespace"
```

### Task 10: Full validation and browser screenshots

**Files:**
- No new files — validation and evidence gathering only.

- [ ] **Step 1: Run backend build and tests**

```powershell
dotnet build
.\scripts\postgres-dev.ps1 ensure
dotnet test
```

Expected: All builds and tests pass.

- [ ] **Step 2: Run frontend typecheck, build, and tests**

```powershell
cd src/WildBunch.Web
npm run typecheck
npm run build
npm run test
cd ../..
```

Expected: All pass.

- [ ] **Step 3: Run EF migrations check (standing validation)**

```powershell
dotnet tool restore
dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api
```

Expected: No migration errors (this issue should not add migrations, but standing validation confirms no breakage).

- [ ] **Step 4: Browser screenshots**

Start the API and Vite dev servers, open the browser, and capture:
1. Overlay off — clean play surface with HUD and flow content.
2. Overlay on — dev overlay visible, covering the play surface, with session audit panel.
3. Overlay closed via Escape — back to clean play surface.

Clean up all worker-owned processes (API server, Vite dev server, browser) before returning GREEN.

- [ ] **Step 5: Final commit if any cleanup needed**

If browser evidence or final adjustments require changes, commit them. Otherwise, the branch is ready for PR.

---

## DOD Clause Mapping

| DOD Clause | Proof |
|---|---|
| Current `main` is inspected and source seams are reported | Preflight answers 1-11 above with file references; gameplay dev-surface inventory in answer 11 |
| Old dev cockpit is retired or no longer rendered | Task 7 deletes `DebugCockpitRoute.tsx` and removes `/debug` route |
| Dev overlay can be toggled on and off | Task 6 + Task 7: `DevOverlay` with `open` prop, toggle button in `AppShell` |
| Overlay off preserves a clean normal play surface | Task 8 test: overlay closed → dialog not in DOM; Task 10 screenshot |
| Overlay on is visibly dev-only and can render contextual panel content | Task 6: `DevOverlay` with "Developer overlay" dialog label; Task 5: `SessionAuditDevPanel` |
| Dev-only API/query namespace exists separately from normal player APIs | Task 3: `/api/dev/` route group with `DevEndpoints`; `DevEndpointTests` proves 200 in dev, 403 in non-dev, 404 for missing session, player-facing audit still 404 |
| Future auth/dev-role guard is centralized | Task 1: `DevRoleGuard.EnsureDevAccess()` in `WildBunch.Api`; Task 3: endpoint catches `DevAccessDeniedException` → 403; `DevEndpointTests` proves the denial path returns explicit 403 |
| Normal player DTOs/APIs do not newly expose hidden truth | Dev DTOs are separate types; player DTOs unchanged; `GameApiHiddenTruthTests` continues to pass; `DevEndpointTests.PlayerFacingAuditPath_StillReturns404` proves dev namespace does not reopen player-facing audit; ADR-0030 records the player-vs-dev boundary (not a blanket prohibition on dev truth) |
| Extension pattern for future contextual panels is clear | Task 5: `DevPanelRegistry` with `{ id, label, render }` entries; Preflight answer 11: gameplay dev-surface inventory maps which surfaces will need coverage |
| Validation covers backend/frontend surfaces touched | Task 10: `dotnet build`, `dotnet test` (including `DevEndpointTests` integration tests), `npm run typecheck`, `npm run build`, `npm run test`, EF migrations check |
| Return evidence includes branch, PR URL, SHA, changed files, validation, screenshots, DOD mapping | Final worker return per AGENTS.md return format |

