# Dev Overlay Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current dev cockpit with a toggleable contextual dev overlay foundation that can dominate the play surface when open, disappear when closed, and become the shared extension point for future contextual dev controls.

**Architecture:** A single `DevOverlay` shell component renders as a fixed full-surface panel when toggled on, and renders nothing when off. It is mounted in `AppShell` alongside the existing `GlobalOverlays` so the normal play surface stays clean when the overlay is closed. A new `/api/dev/` endpoint namespace with a centralized `DevRoleGuard` seam separates dev-only queries from player-facing APIs. The old `DebugCockpitRoute` and its `/debug` route are retired. Future contextual panels (travel forcing, saloon forcing) register through a `DevOverlayPanel` registry pattern.

**Tech Stack:** React 18, TanStack Router, TanStack React Query, styled-components, TypeScript, Vite, Vitest, ASP.NET Core minimal APIs, C#.

## Global Constraints

- Backend remains authoritative for gameplay state; React renders server state.
- Hidden culprit truth must not be exposed through any dev endpoint or player API.
- The dev overlay is dev-only scaffolding; keep it utilitarian per AGENTS.md and play-surface-ui.md.
- Do not implement travel encounter forcing or saloon/POI forcing in this issue.
- Do not implement real auth; add the seam only.
- Normal player DTOs/APIs must not newly expose hidden truth.
- The `GameSession` aggregate root remains the live-play mutation boundary.
- Dev endpoints live under `/api/dev/` and are gated by a centralized `DevRoleGuard`.
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

A `DevRoleGuard` class registered as a scoped service in `DependencyInjection.cs`. It exposes a single `EnsureDevAccess()` method that currently always succeeds in development and throws/returns 403 in non-development environments. Each dev endpoint calls `devRoleGuard.EnsureDevAccess()` before processing.

This is the smallest seam: one class, one method, one registration. Future auth implementations replace the body of `EnsureDevAccess` without changing call sites. The guard checks `IHostEnvironment.IsDevelopment()` now; later it can check claims, headers, or a dev-role token.

### 7. Which normal player DTO/API/read-model surfaces currently expose state, and how will the dev endpoint boundary avoid hidden-truth leakage?

Current player-facing DTOs:
- `GameSessionDto` — player, world, caseFile, inventory, clock, pursuitState, journey, travelDiary, logEntries, activeSaloonPersonOfInterest. No hidden culprit fields exposed.
- `JournalDto` — caseFile (with caseSummary, discoveredSuspects, caseBoard, knownClues, knownWarrants, wantedPosters), logEntries, clock, currentTown.
- `HudProjection` / `DiaryProjection` — safe projections per ADR-0028.
- `AvailableActionDto[]` — action kinds and labels.
- `WantedPosterDto[]` — target display name, features, bounty, etc.

The `FullAuditProjection` (`FullAuditProjector.cs`) is explicitly a developer/replay surface and is NOT exposed through any current endpoint. It derives event type names and summaries from the event stream.

Dev endpoint boundary strategy:
- Dev endpoints under `/api/dev/` may return `FullAuditProjection` and other dev-only data.
- Dev DTOs are separate types in a `Dev/` folder, not reused player DTOs.
- Dev query handlers derive from the event stream but return dev-shaped DTOs.
- The `DevRoleGuard` prevents non-dev access.
- Player-facing DTOs are unchanged — no hidden truth leaks into them.

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

**Backend tests (xUnit):**
- `DevRoleGuard` allows access in development environment.
- `DevRoleGuard` denies access in non-development environment.
- Dev audit endpoint returns `FullAuditProjection`-shaped data.
- Dev audit endpoint returns 403 when guard denies.

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
- `WildBunch.Api/Dev/DevEndpoints.cs` — maps `/api/dev/` route group with audit endpoint.
- `WildBunch.Api/Dev/DevRoleGuard.cs` — centralized dev-role guard seam.
- `WildBunch.Application/Dev/Queries/GetSessionAuditQuery.cs` — dev query for session audit.
- `WildBunch.Application/Dev/Queries/GetSessionAuditHandler.cs` — dev query handler using `FullAuditProjector`.
- `WildBunch.Application/Dev/Models/SessionAuditDto.cs` — dev-only DTO.

**Modify:**
- `WildBunch.Api/DependencyInjection.cs` — register `DevRoleGuard` and dev query handler; map dev endpoints.
- `WildBunch.Api/Program.cs` — no change expected (dev endpoints mapped via `MapWildBunchApi`).

### Tests (`src/WildBunch.Application.Tests/` or equivalent)

**Create:**
- `Dev/DevRoleGuardTests.cs` — guard allows in dev, denies in non-dev.
- `Dev/GetSessionAuditHandlerTests.cs` — handler returns audit projection.

### Docs

**Create:**
- `docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md` — records the dev overlay shell, dev endpoint namespace, and dev-role guard seam decision.

**Modify:**
- `docs/adr/INDEX.md` — add ADR-0030 entry with freshness timestamp.

---

## Implementation Tasks

### Task 1: Backend dev-role guard seam

**Files:**
- Create: `src/WildBunch.Api/Dev/DevRoleGuard.cs`
- Test: `src/WildBunch.Application.Tests/Dev/DevRoleGuardTests.cs`

**Interfaces:**
- Consumes: `IHostEnvironment` (from Microsoft.Extensions.Hosting)
- Produces: `DevRoleGuard.EnsureDevAccess()` — throws `UnauthorizedAccessException` when not in development; no-op in development.

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using WildBunch.Api.Dev;

namespace WildBunch.Application.Tests.Dev;

public class DevRoleGuardTests
{
    [Fact]
    public void EnsureDevAccess_AllowsInDevelopmentEnvironment()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Development };
        var guard = new DevRoleGuard(env);

        // Should not throw
        guard.EnsureDevAccess();
    }

    [Fact]
    public void EnsureDevAccess_ThrowsInNonDevelopmentEnvironment()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var guard = new DevRoleGuard(env);

        Assert.Throws<UnauthorizedAccessException>(() => guard.EnsureDevAccess());
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/WildBunch.Application.Tests --filter "DevRoleGuardTests"`
Expected: FAIL — `DevRoleGuard` type not found.

- [ ] **Step 3: Write minimal implementation**

```csharp
using Microsoft.Extensions.Hosting;

namespace WildBunch.Api.Dev;

/// <summary>
/// Centralized dev-role guard seam. Currently checks development environment.
/// Future auth implementations replace the body of EnsureDevAccess without
/// changing call sites.
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
            throw new UnauthorizedAccessException("Dev endpoints are only available in the development environment.");
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/WildBunch.Application.Tests --filter "DevRoleGuardTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Api/Dev/DevRoleGuard.cs src/WildBunch.Application.Tests/Dev/DevRoleGuardTests.cs
git commit -m "feat: add DevRoleGuard centralized dev-access seam"
```

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

### Task 3: Backend dev endpoints and DI registration

**Files:**
- Create: `src/WildBunch.Api/Dev/DevEndpoints.cs`
- Modify: `src/WildBunch.Api/DependencyInjection.cs`

**Interfaces:**
- Consumes: `DevRoleGuard`, `GetSessionAuditHandler`
- Produces: `GET /api/dev/sessions/{id}/audit` → `SessionAuditDto`

- [ ] **Step 1: Write the dev endpoints**

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
        guard.EnsureDevAccess();

        try
        {
            var result = await handler.HandleAsync(new GetSessionAuditQuery(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
```

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

- [ ] **Step 3: Build and verify**

Run: `dotnet build`
Expected: PASS with no errors.

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Api/Dev/DevEndpoints.cs src/WildBunch.Api/DependencyInjection.cs
git commit -m "feat: add /api/dev/ endpoint namespace with audit endpoint"
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
- `related to`: ADR-0007 (hidden culprit boundaries — dev endpoints must not leak hidden truth to player APIs)

## Context

The previous dev cockpit (`DebugCockpitRoute` at `/debug`) duplicated player-facing functionality (start game, actions, travel, case file) already available in the flow surfaces. It was a separate route that competed with the play surface rather than a contextual overlay that could augment it.

Future dev controls (travel encounter forcing, saloon/POI forcing) need a shared extension point that is clearly dev-only, toggleable, and separated from player-facing APIs.

## Decision

1. **DevOverlay shell.** A single toggleable `DevOverlay` component mounted in `AppShell` renders as a fixed full-surface panel when open and nothing when closed. Toggle state is shell-local. This replaces the `/debug` route.

2. **Dev endpoint namespace.** Dev-only endpoints live under `/api/dev/`, mapped by `DevEndpoints`, separate from player-facing `/api/games/`. Dev endpoints may return dev-only projections (FullAuditProjection) and dev-only DTOs.

3. **DevRoleGuard seam.** A centralized `DevRoleGuard` with `EnsureDevAccess()` gates every dev endpoint. Currently checks `IHostEnvironment.IsDevelopment()`. Future auth replaces the method body without changing call sites.

4. **Panel registry.** A `DevPanelRegistry` defines available dev panels as `{ id, label, render }` entries. The DevOverlay renders a sidebar from the registry. Future panels (TravelDevPanel, SaloonDevPanel) add entries without modifying the shell.

5. **Cockpit retirement.** `DebugCockpitRoute` and the `/debug` route are removed. There is one dev surface: the DevOverlay.

6. **Hidden-truth safety.** Dev endpoints return dev-shaped DTOs (`SessionAuditDto`) separate from player DTOs. The FullAuditProjection exposes event type names and summaries but not hidden culprit identity fields. Player-facing DTOs are unchanged.

## Options Considered and Rejected

- **Keep DebugCockpitRoute and add overlay beside it.** Rejected: two dev surfaces creates confusion and duplication.
- **Route-based overlay at /dev.** Rejected: URL-based toggle adds navigation complexity and doesn't coexist cleanly with game phase routing.
- **Generic Modal/Panel abstraction.** Rejected per play-surface-ui.md: avoid generic React infrastructure before demand. The DevOverlay is a specific dev surface, not a reusable abstraction.

## Consequences

- Future dev panels register through the registry and fetch from `/api/dev/`.
- Dev endpoint access is centralized through one guard seam.
- The play surface is clean when the overlay is closed.
- The FullAuditProjector is now exposed through a dev endpoint, but only in development and only through dev DTOs.
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
| Current `main` is inspected and source seams are reported | Preflight answers 1-10 above with file references |
| Old dev cockpit is retired or no longer rendered | Task 7 deletes `DebugCockpitRoute.tsx` and removes `/debug` route |
| Dev overlay can be toggled on and off | Task 6 + Task 7: `DevOverlay` with `open` prop, toggle button in `AppShell` |
| Overlay off preserves a clean normal play surface | Task 8 test: overlay closed → dialog not in DOM; Task 10 screenshot |
| Overlay on is visibly dev-only and can render contextual panel content | Task 6: `DevOverlay` with "Developer overlay" dialog label; Task 5: `SessionAuditDevPanel` |
| Dev-only API/query namespace exists separately from normal player APIs | Task 3: `/api/dev/` route group with `DevEndpoints` |
| Future auth/dev-role guard is centralized | Task 1: `DevRoleGuard.EnsureDevAccess()` |
| Normal player DTOs/APIs do not newly expose hidden truth | Dev DTOs are separate types; player DTOs unchanged; FullAuditProjection exposes event summaries only |
| Extension pattern for future contextual panels is clear | Task 5: `DevPanelRegistry` with `{ id, label, render }` entries |
| Validation covers backend/frontend surfaces touched | Task 10: `dotnet build`, `dotnet test`, `npm run typecheck`, `npm run build`, `npm run test`, EF migrations check |
| Return evidence includes branch, PR URL, SHA, changed files, validation, screenshots, DOD mapping | Final worker return per AGENTS.md return format |

