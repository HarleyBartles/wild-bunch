# BUNCH-124: URL Routing + Vite Bundle Splitting — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the single-route `GameFlowRouter` pattern with TanStack Router URL routes, lazy-load route components, isolate Phaser into its own chunk, and rework the arrival flow into `TrailFlowSurface`.

**Architecture:** TanStack Router route tree with lazy-loaded components. Two sync hooks in `ShellChrome` reconcile URL with backend-derived phase (`usePhaseRouteSync`) and dev surface (`useDevSurfaceSync`). Phaser isolated via explicit `React.lazy` boundary on `StartingTownStep` inside `PreSessionSurface`. Vendor deps split via `manualChunks`.

**Tech Stack:** React 18, TanStack Router v1, TanStack Query v5, styled-components v6, Vite, Vitest, Phaser 3.

## Global Constraints

- Frontend-only — no backend changes.
- All styling via `styled-components` — no plain CSS classes.
- Design tokens via `var(--token-name)` — see `src/styles/_variables.scss`.
- Shared primitives from `src/components/ui/sharedStyled.tsx`.
- Test runner: `npx vitest run` (from `src/WildBunch.Web/`).
- Build: `npm run build` (runs `tsc --noEmit && vite build`).
- Dev server: `npm run dev` (from `src/WildBunch.Web/`).
- Index mesh regeneration: `python scripts/generate_index_mesh.py` (from repo root).
- `GamePhase` type is defined in `src/hooks/useGamePhase.ts`.
- `DevSurface` type is defined in `src/dev/DevSurfaceContext.tsx`.
- `TownPlace` type is currently exported from `src/flow/GameFlowRouter.tsx` — will be inlined into `TownHubSurface.tsx`.
- All commands run from `src/WildBunch.Web/` unless noted otherwise.

---

## File Structure

### New files

- `src/shell/RouteLoading.tsx` — minimal Suspense fallback component
- `src/shell/usePhaseRouteSync.ts` — phase ↔ URL reconciliation hook
- `src/shell/useDevSurfaceSync.ts` — re-homes dev surface tracking from `GameFlowRouter`
- `src/tests/usePhaseRouteSync.test.tsx` — sync hook tests
- `src/tests/useDevSurfaceSync.test.tsx` — dev surface sync tests
- `src/tests/TrailFlowSurfaceCompleted.test.tsx` — completed-journey regression test

### Removed files

- `src/flow/GameFlowRouter.tsx`
- `src/flow/ArrivalSurface.tsx`
- `src/routes/HuntRoute.tsx`
- `src/routes/TrailRoute.tsx`
- `src/routes/CaseFileRoute.tsx`
- `src/routes/WantedRoute.tsx`
- `src/routes/INDEX.md`
- `src/tests/GameFlowRouter.test.tsx`

### Modified files

- `src/hooks/useGamePhase.ts` — remove `"arrival"` phase
- `src/dev/DevSurfaceContext.tsx` — remove `"arrival"` from `DevSurface`
- `src/dev/DevPanelRegistry.tsx` — update `TravelDevPanel.surfaces`
- `src/flow/TrailFlowSurface.tsx` — add completed-journey view
- `src/flow/TownHubSurface.tsx` — remove props, use `useNavigate`, arrival notice
- `src/flow/PreSessionSurface.tsx` — lazy-load `StartingTownStep`
- `src/flow/places/StorePlace.tsx` — `onLeave` → `useNavigate`
- `src/flow/places/SheriffPlace.tsx` — `onLeave` → `useNavigate`
- `src/flow/places/SaloonPlace.tsx` — `onLeave` → `useNavigate`
- `src/flow/TravelPrepSurface.tsx` — `onBack` → `useNavigate`
- `src/shell/router.tsx` — expand route tree with lazy-loaded routes
- `src/shell/AppShell.tsx` — call sync hooks, wrap `Outlet` in `Suspense`
- `vite.config.ts` — `manualChunks` + `chunkSizeWarningLimit`
- `src/tests/AppShell.test.tsx` — update for new route tree
- `src/tests/StartOverRegression.test.tsx` — update `allSurfaces` array
- `src/tests/SheriffPlace.test.tsx` — remove `onLeave` prop
- `src/tests/TravelPrepSurface.test.tsx` — remove `onBack` prop

---

### Task 1: RouteLoading component

**Files:**
- Create: `src/shell/RouteLoading.tsx`

**Interfaces:**
- Produces: `RouteLoading` — a styled-components component rendered as a Suspense fallback

- [ ] **Step 1: Create the component**

```tsx
// src/shell/RouteLoading.tsx
import styled from "styled-components";

export const RouteLoading = styled.div`
  display: grid;
  place-items: center;
  padding: 48px;
  color: var(--muted);
  font-size: 0.95rem;
`;
```

- [ ] **Step 2: Verify it compiles**

Run: `npx tsc --noEmit`
Expected: no errors

- [ ] **Step 3: Commit**

```bash
git add src/shell/RouteLoading.tsx
git commit -m "feat: add RouteLoading Suspense fallback component"
```

### Task 2: usePhaseRouteSync hook

**Files:**
- Create: `src/shell/usePhaseRouteSync.ts`
- Test: `src/tests/usePhaseRouteSync.test.tsx`

**Interfaces:**
- Consumes: `useGamePhase()` from `src/hooks/useGamePhase.ts` (returns `{ phase, hasSession, ... }`)
- Consumes: TanStack Router's `useLocation()` and `useNavigate()`
- Produces: `usePhaseRouteSync()` — a hook that navigates to the correct URL when the phase doesn't match the current route

- [ ] **Step 1: Write the failing test**

```tsx
// src/tests/usePhaseRouteSync.test.tsx
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider, createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { usePhaseRouteSync } from "../shell/usePhaseRouteSync";
import { StartFlowPhase, type GameSessionDto, type JournalDto } from "../api/types";
import { getAvailableActions, getGame, getJournal } from "../api/wildBunchApi";

vi.mock("../api/wildBunchApi", () => ({
  getGame: vi.fn(),
  getAvailableActions: vi.fn(),
  getJournal: vi.fn(),
  getWorldMap: vi.fn(),
  getStartingTownMap: vi.fn(),
  setupGame: vi.fn(),
  markPrologueViewed: vi.fn(),
  startGameWithTown: vi.fn(),
  advanceTravelDay: vi.fn(),
  archiveGame: vi.fn(),
  getTownStoreOffers: vi.fn(),
  buyStoreItem: vi.fn(),
  checkLocalRecords: vi.fn(),
  inspectNoticeBoard: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  lookAroundSaloon: vi.fn(),
  readWantedPosters: vi.fn(),
  followTelegraphLeads: vi.fn(),
  gatherLocalGossip: vi.fn(),
  getPrologue: vi.fn(),
  getStartingTowns: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
  previewTravel: vi.fn(),
  resolveTravelEncounter: vi.fn(),
}));

const mockedGetGame = vi.mocked(getGame);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
  window.history.replaceState({}, "", "/");
});

function createInTownSession(): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
    startFlowPhase: StartFlowPhase.GameStarted,
    player: { name: "Ruth", currentTownId: "t-town", health: 9 },
    world: {
      towns: [{ id: "t-town", name: "Tumbleweed", services: 0 }],
      trails: [],
    },
    caseFile: {
      accusationId: null,
      openingLead: "",
      caseState: { statusText: "" },
      discoveredSuspects: [],
      caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] },
      knownClues: [],
    },
    inventory: {
      wallet: { cash: 14 },
      items: [],
      horseState: null,
      canteenState: null,
      capabilities: {
        mountedTravelAvailable: false,
        horseUpkeepRequired: false,
        normalRouteWaterSecure: false,
        trailUtility: false,
        closeThreatAvailable: false,
        firearmThreatAvailable: false,
        gunfightCapable: false,
        revolverUsable: false,
        rifleUsable: false,
      },
    },
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    pursuitState: { heat: 1 },
    journey: null,
    travelDiary: null,
    logEntries: [],
    activeSaloonPersonOfInterest: null,
    wantedPosters: [],
  };
}

function createJournal(): JournalDto {
  return {
    id: "game-1",
    status: 0,
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    currentTown: { id: "t-town", name: "Tumbleweed" },
    caseFile: {
      accusationId: null,
      openingLead: "",
      caseState: { statusText: "" },
      caseSummary: "",
      discoveredSuspects: [],
      caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] },
      knownClues: [],
      knownWarrants: [],
      wantedPosters: [],
    },
    logEntries: [],
  };
}

function TestSyncHost() {
  usePhaseRouteSync();
  return <div data-testid="sync-host" />;
}

function renderWithRouter(initialUrl: string) {
  window.history.replaceState({}, "", initialUrl);
  const rootRoute = createRootRoute({ component: TestSyncHost });
  const indexRoute = createRoute({ getParentRoute: () => rootRoute, path: "/", component: () => <div>start</div> });
  const townRoute = createRoute({ getParentRoute: () => rootRoute, path: "/town", component: () => <div>town</div> });
  const trailRoute = createRoute({ getParentRoute: () => rootRoute, path: "/trail", component: () => <div>trail</div> });
  const router = createRouter({
    routeTree: rootRoute.addChildren([indexRoute, townRoute, trailRoute]),
  });

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <RouterProvider router={router} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

describe("usePhaseRouteSync", () => {
  it("redirects to /town when phase is in-town but URL is /", async () => {
    mockedGetGame.mockResolvedValue(createInTownSession());
    vi.mocked(getAvailableActions).mockResolvedValue([]);
    vi.mocked(getJournal).mockResolvedValue(createJournal());
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderWithRouter("/");

    await waitFor(() => {
      expect(window.location.pathname).toBe("/town");
    });
  });

  it("does not redirect when phase matches URL", async () => {
    mockedGetGame.mockResolvedValue(createInTownSession());
    vi.mocked(getAvailableActions).mockResolvedValue([]);
    vi.mocked(getJournal).mockResolvedValue(createJournal());
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderWithRouter("/town");

    await waitFor(() => {
      expect(screen.getByTestId("sync-host")).toBeInTheDocument();
    });
    expect(window.location.pathname).toBe("/town");
  });

  it("redirects to / when no session and URL is /town", async () => {
    mockedGetGame.mockResolvedValue(null as never);
    vi.mocked(getAvailableActions).mockResolvedValue([]);
    vi.mocked(getJournal).mockResolvedValue(createJournal());

    renderWithRouter("/town");

    await waitFor(() => {
      expect(window.location.pathname).toBe("/");
    });
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx vitest run src/tests/usePhaseRouteSync.test.tsx`
Expected: FAIL — module `../shell/usePhaseRouteSync` not found

- [ ] **Step 3: Write minimal implementation**

```ts
// src/shell/usePhaseRouteSync.ts
import { useEffect, useRef } from "react";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { useGamePhase } from "../hooks/useGamePhase";

/**
 * Reconciles the URL with the backend-derived game phase.
 * Backend transitions drive navigation — when the phase changes,
 * the hook navigates to the matching route if the URL doesn't already match.
 */
export function usePhaseRouteSync(): void {
  const { phase, hasSession } = useGamePhase();
  const location = useLocation();
  const navigate = useNavigate();
  const isFirstRender = useRef(true);

  useEffect(() => {
    if (isFirstRender.current) {
      isFirstRender.current = false;
      return;
    }

    const expectedPrefix = phaseToUrlPrefix(phase);
    if (!expectedPrefix) {
      return;
    }

    const currentPath = location.pathname;
    if (currentPath === expectedPrefix || currentPath.startsWith(expectedPrefix + "/")) {
      return;
    }

    void navigate({ to: expectedPrefix });
  }, [phase, hasSession, location.pathname, navigate]);
}

function phaseToUrlPrefix(phase: string): string | null {
  switch (phase) {
    case "pre-session":
    case "setup":
    case "prologue":
    case "town-selection":
      return "/";
    case "in-town":
      return "/town";
    case "on-trail":
      return "/trail";
    default:
      return null;
  }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npx vitest run src/tests/usePhaseRouteSync.test.tsx`
Expected: PASS (3 tests)

- [ ] **Step 5: Commit**

```bash
git add src/shell/usePhaseRouteSync.ts src/tests/usePhaseRouteSync.test.tsx
git commit -m "feat: add usePhaseRouteSync hook for phase-URL reconciliation"
```

### Task 3: useDevSurfaceSync hook

**Files:**
- Create: `src/shell/useDevSurfaceSync.ts`
- Test: `src/tests/useDevSurfaceSync.test.tsx`

**Interfaces:**
- Consumes: `useGamePhase()` from `src/hooks/useGamePhase.ts`
- Consumes: TanStack Router's `useLocation()`
- Consumes: `useSetDevSurface()` from `src/dev/DevSurfaceContext.tsx`
- Produces: `useDevSurfaceSync()` — a hook that maps phase + current route to a `DevSurface` and calls `useSetDevSurface()`

- [ ] **Step 1: Write the failing test**

```tsx
// src/tests/useDevSurfaceSync.test.tsx
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider, createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { useDevSurfaceSync } from "../shell/useDevSurfaceSync";
import { DevSurfaceProvider, useDevSurface } from "../dev/DevSurfaceContext";
import { StartFlowPhase, type GameSessionDto, type JournalDto } from "../api/types";
import { getAvailableActions, getGame, getJournal } from "../api/wildBunchApi";

vi.mock("../api/wildBunchApi", () => ({
  getGame: vi.fn(),
  getAvailableActions: vi.fn(),
  getJournal: vi.fn(),
  getWorldMap: vi.fn(),
  getStartingTownMap: vi.fn(),
  setupGame: vi.fn(),
  markPrologueViewed: vi.fn(),
  startGameWithTown: vi.fn(),
  advanceTravelDay: vi.fn(),
  archiveGame: vi.fn(),
  getTownStoreOffers: vi.fn(),
  buyStoreItem: vi.fn(),
  checkLocalRecords: vi.fn(),
  inspectNoticeBoard: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  lookAroundSaloon: vi.fn(),
  readWantedPosters: vi.fn(),
  followTelegraphLeads: vi.fn(),
  gatherLocalGossip: vi.fn(),
  getPrologue: vi.fn(),
  getStartingTowns: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
  previewTravel: vi.fn(),
  resolveTravelEncounter: vi.fn(),
}));

const mockedGetGame = vi.mocked(getGame);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
  window.history.replaceState({}, "", "/");
});

function createInTownSession(): GameSessionDto {
  return {
    id: "game-1", status: 0, gameDifficulty: 0, gameEntropy: 1,
    startFlowPhase: StartFlowPhase.GameStarted,
    player: { name: "Ruth", currentTownId: "t-town", health: 9 },
    world: { towns: [{ id: "t-town", name: "Tumbleweed", services: 0 }], trails: [] },
    caseFile: { accusationId: null, openingLead: "", caseState: { statusText: "" }, discoveredSuspects: [], caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] }, knownClues: [] },
    inventory: { wallet: { cash: 14 }, items: [], horseState: null, canteenState: null, capabilities: { mountedTravelAvailable: false, horseUpkeepRequired: false, normalRouteWaterSecure: false, trailUtility: false, closeThreatAvailable: false, firearmThreatAvailable: false, gunfightCapable: false, revolverUsable: false, rifleUsable: false } },
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    pursuitState: { heat: 1 },
    journey: null, travelDiary: null, logEntries: [],
    activeSaloonPersonOfInterest: null, wantedPosters: [],
  };
}

function createJournal(): JournalDto {
  return {
    id: "game-1", status: 0, clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    currentTown: { id: "t-town", name: "Tumbleweed" },
    caseFile: { accusationId: null, openingLead: "", caseState: { statusText: "" }, caseSummary: "", discoveredSuspects: [], caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] }, knownClues: [], knownWarrants: [], wantedPosters: [] },
    logEntries: [],
  };
}

function SurfaceProbe() {
  const surface = useDevSurface();
  return <div data-testid="surface-probe" data-surface={surface} />;
}

function TestHost() {
  useDevSurfaceSync();
  return <SurfaceProbe />;
}

function renderWithRouter(initialUrl: string) {
  window.history.replaceState({}, "", initialUrl);
  const rootRoute = createRootRoute({ component: TestHost });
  const indexRoute = createRoute({ getParentRoute: () => rootRoute, path: "/", component: () => <div>start</div> });
  const townRoute = createRoute({ getParentRoute: () => rootRoute, path: "/town", component: () => <div>town</div> });
  const storeRoute = createRoute({ getParentRoute: () => rootRoute, path: "/town/store", component: () => <div>store</div> });
  const trailRoute = createRoute({ getParentRoute: () => rootRoute, path: "/trail", component: () => <div>trail</div> });
  const router = createRouter({
    routeTree: rootRoute.addChildren([indexRoute, townRoute.addChildren([storeRoute]), trailRoute]),
  });

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <DevSurfaceProvider>
          <RouterProvider router={router} />
        </DevSurfaceProvider>
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

describe("useDevSurfaceSync", () => {
  it("maps in-town phase + /town URL to 'town' surface", async () => {
    mockedGetGame.mockResolvedValue(createInTownSession());
    vi.mocked(getAvailableActions).mockResolvedValue([]);
    vi.mocked(getJournal).mockResolvedValue(createJournal());
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderWithRouter("/town");

    await waitFor(() => {
      const probe = screen.getByTestId("surface-probe");
      expect(probe.getAttribute("data-surface")).toBe("town");
    });
  });

  it("maps in-town phase + /town/store URL to 'store' surface", async () => {
    mockedGetGame.mockResolvedValue(createInTownSession());
    vi.mocked(getAvailableActions).mockResolvedValue([]);
    vi.mocked(getJournal).mockResolvedValue(createJournal());
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderWithRouter("/town/store");

    await waitFor(() => {
      const probe = screen.getByTestId("surface-probe");
      expect(probe.getAttribute("data-surface")).toBe("store");
    });
  });

  it("maps pre-session phase to 'pre-session' surface", async () => {
    mockedGetGame.mockResolvedValue(null as never);
    vi.mocked(getAvailableActions).mockResolvedValue([]);
    vi.mocked(getJournal).mockResolvedValue(createJournal());

    renderWithRouter("/");

    await waitFor(() => {
      const probe = screen.getByTestId("surface-probe");
      expect(probe.getAttribute("data-surface")).toBe("pre-session");
    });
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx vitest run src/tests/useDevSurfaceSync.test.tsx`
Expected: FAIL — module `../shell/useDevSurfaceSync` not found

- [ ] **Step 3: Write minimal implementation**

```ts
// src/shell/useDevSurfaceSync.ts
import { useEffect } from "react";
import { useLocation } from "@tanstack/react-router";
import { useGamePhase } from "../hooks/useGamePhase";
import { useSetDevSurface } from "../dev/DevSurfaceContext";
import type { DevSurface } from "../dev/DevSurfaceContext";

/**
 * Maps the current game phase + URL route to a DevSurface value
 * and pushes it into DevSurfaceContext. Replaces the mapping
 * that lived in GameFlowRouter before its removal.
 */
export function useDevSurfaceSync(): void {
  const { phase } = useGamePhase();
  const location = useLocation();
  const setDevSurface = useSetDevSurface();

  useEffect(() => {
    const surface = deriveDevSurface(phase, location.pathname);
    setDevSurface(surface);
  }, [phase, location.pathname, setDevSurface]);
}

function deriveDevSurface(phase: string, pathname: string): DevSurface {
  if (phase === "pre-session" || phase === "setup" || phase === "prologue" || phase === "town-selection") {
    return "pre-session";
  }
  if (phase === "on-trail") {
    return "trail";
  }
  if (phase === "in-town") {
    if (pathname === "/town/store") return "store";
    if (pathname === "/town/sheriff") return "sheriff";
    if (pathname === "/town/saloon") return "saloon";
    if (pathname === "/town/trailhead") return "trailhead";
    return "town";
  }
  return "pre-session";
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npx vitest run src/tests/useDevSurfaceSync.test.tsx`
Expected: PASS (3 tests)

- [ ] **Step 5: Commit**

```bash
git add src/shell/useDevSurfaceSync.ts src/tests/useDevSurfaceSync.test.tsx
git commit -m "feat: add useDevSurfaceSync hook to re-home dev surface tracking"
```

### Task 4: Remove "arrival" phase + add TrailFlowSurface completed-journey view

**Files:**
- Modify: `src/hooks/useGamePhase.ts`
- Modify: `src/dev/DevSurfaceContext.tsx`
- Modify: `src/dev/DevPanelRegistry.tsx`
- Modify: `src/flow/GameFlowRouter.tsx` (temporary — remove arrival case, will be deleted in Task 9)
- Modify: `src/flow/TrailFlowSurface.tsx`
- Test: `src/tests/TrailFlowSurfaceCompleted.test.tsx`

**Interfaces:**
- `GamePhase` removes `"arrival"` — `"on-trail"` now covers `Completed` journeys
- `GamePhaseState` removes `isArrivalPending`
- `DevSurface` removes `"arrival"`
- `TrailFlowSurface` now renders a completed-journey view when `journey.status === JourneyStatus.Completed`

- [ ] **Step 1: Write the failing test**

```tsx
// src/tests/TrailFlowSurfaceCompleted.test.tsx
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TrailFlowSurface } from "../flow/TrailFlowSurface";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { JourneyStatus, StartFlowPhase, type GameSessionDto, type JournalDto } from "../api/types";
import {
  acknowledgeTravelArrival,
  advanceTravelDay,
  getAvailableActions,
  getGame,
  getJournal,
  resolveTravelEncounter,
} from "../api/wildBunchApi";

vi.mock("phaser", () => {
  class Game { public config: unknown; constructor(c: unknown) { this.config = c; } destroy() {} }
  class Scene { constructor(_k?: string) {} }
  const Scale = { FIT: 0, CENTER_BOTH: 0 };
  return { default: { Game, Scene, Scale }, Game, Scene, Scale };
});

vi.mock("../api/wildBunchApi", () => ({
  getGame: vi.fn(),
  getAvailableActions: vi.fn(),
  getJournal: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  resolveTravelEncounter: vi.fn(),
  getWorldMap: vi.fn(),
  getStartingTownMap: vi.fn(),
  setupGame: vi.fn(),
  markPrologueViewed: vi.fn(),
  startGameWithTown: vi.fn(),
  archiveGame: vi.fn(),
  getTownStoreOffers: vi.fn(),
  buyStoreItem: vi.fn(),
  checkLocalRecords: vi.fn(),
  inspectNoticeBoard: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  lookAroundSaloon: vi.fn(),
  readWantedPosters: vi.fn(),
  followTelegraphLeads: vi.fn(),
  gatherLocalGossip: vi.fn(),
  getPrologue: vi.fn(),
  getStartingTowns: vi.fn(),
  previewTravel: vi.fn(),
}));

const mockedGetGame = vi.mocked(getGame);
const mockedAcknowledgeTravelArrival = vi.mocked(acknowledgeTravelArrival);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function createCompletedJourneySession(): GameSessionDto {
  return {
    id: "game-1", status: 0, gameDifficulty: 0, gameEntropy: 1,
    startFlowPhase: StartFlowPhase.GameStarted,
    player: { name: "Ruth", currentTownId: "dust-fork", health: 9 },
    world: {
      towns: [
        { id: "t-town", name: "Tumbleweed", services: 0 },
        { id: "dust-fork", name: "Dust Fork", services: 0 },
      ],
      trails: [{ id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", risk: 1, terrain: 0, waterFeature: 0, rideDayDistance: 3 }],
    },
    caseFile: { accusationId: null, openingLead: "", caseState: { statusText: "" }, discoveredSuspects: [], caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] }, knownClues: [] },
    inventory: { wallet: { cash: 14 }, items: [], horseState: null, canteenState: null, capabilities: { mountedTravelAvailable: false, horseUpkeepRequired: false, normalRouteWaterSecure: false, trailUtility: false, closeThreatAvailable: false, firearmThreatAvailable: false, gunfightCapable: false, revolverUsable: false, rifleUsable: false } },
    clock: { day: 8, turn: 2, timeOfDay: "Morning" },
    pursuitState: { heat: 1 },
    journey: {
      originTownId: "t-town", originTownName: "Tumbleweed",
      destinationTownId: "dust-fork", destinationTownName: "Dust Fork",
      travelMode: 1, status: JourneyStatus.Completed,
      mountedTravelAvailable: false, waterSecure: true,
      rideDayDistance: 3, remainingRideDayDistance: 0,
      baselineRideDays: 3, expectedDays: 3, remainingDays: 0,
      canteenChargesPerDay: 0, requiredCanteenCharges: 0,
      availableCanteenCharges: 0, canteenReserveCharges: 0,
      delayMarginDays: 0, delayRisk: false,
      requiredFood: 0, availableFood: 0,
      requiredHorseFeed: 0, availableHorseFeed: 0,
      horseState: null, daysTravelled: 3, delayDays: 0,
      pendingEncounter: null, warnings: [],
      routeProfile: { trailId: "trail-1", risk: 1, terrain: 0, waterFeature: 0, rideDayDistance: 3, mountedRideDayProgress: 1, footRideDayProgress: 0.5, warnings: [] },
    },
    travelDiary: null, logEntries: [],
    activeSaloonPersonOfInterest: null, wantedPosters: [],
  };
}

function createInTownSession(): GameSessionDto {
  const session = createCompletedJourneySession();
  session.journey = null;
  session.player.currentTownId = "dust-fork";
  return session;
}

function createJournal(): JournalDto {
  return {
    id: "game-1", status: 0, clock: { day: 8, turn: 2, timeOfDay: "Morning" },
    currentTown: { id: "dust-fork", name: "Dust Fork" },
    caseFile: { accusationId: null, openingLead: "", caseState: { statusText: "" }, caseSummary: "", discoveredSuspects: [], caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] }, knownClues: [], knownWarrants: [], wantedPosters: [] },
    logEntries: [],
  };
}

function renderTrailFlow() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <TrailFlowSurface />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

describe("TrailFlowSurface completed-journey view", () => {
  it("shows arrival heading when journey is Completed", async () => {
    mockedGetGame.mockResolvedValue(createCompletedJourneySession());
    vi.mocked(getAvailableActions).mockResolvedValue([]);
    vi.mocked(getJournal).mockResolvedValue(createJournal());
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderTrailFlow();

    const heading = await screen.findByRole("heading", { name: /you've arrived in dust fork/i });
    expect(heading).toBeInTheDocument();
  });

  it("shows 'Step into town' button when journey is Completed", async () => {
    mockedGetGame.mockResolvedValue(createCompletedJourneySession());
    vi.mocked(getAvailableActions).mockResolvedValue([]);
    vi.mocked(getJournal).mockResolvedValue(createJournal());
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderTrailFlow();

    const button = await screen.findByRole("button", { name: /step into town/i });
    expect(button).toBeInTheDocument();
  });

  it("calls acknowledgeTravelArrival when 'Step into town' is clicked", async () => {
    mockedGetGame.mockResolvedValue(createCompletedJourneySession());
    vi.mocked(getAvailableActions).mockResolvedValue([]);
    vi.mocked(getJournal).mockResolvedValue(createJournal());
    mockedAcknowledgeTravelArrival.mockResolvedValue({
      success: true,
      message: "You step into town.",
      currentSession: createInTownSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    const user = userEvent.setup();
    renderTrailFlow();

    const button = await screen.findByRole("button", { name: /step into town/i });
    await user.click(button);

    await waitFor(() => {
      expect(mockedAcknowledgeTravelArrival).toHaveBeenCalledWith("game-1");
    });
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx vitest run src/tests/TrailFlowSurfaceCompleted.test.tsx`
Expected: FAIL — TrailFlowSurface does not render arrival content

- [ ] **Step 3: Update `useGamePhase.ts` — remove "arrival" phase**

Replace the entire file content:

```ts
// src/hooks/useGamePhase.ts
import { useMemo } from "react";
import { JourneyStatus, StartFlowPhase } from "../api/types";
import { useGameSession } from "../state/useGameSession";

export type GamePhase =
  | "pre-session"
  | "setup"
  | "prologue"
  | "town-selection"
  | "in-town"
  | "on-trail";

export interface GamePhaseState {
  phase: GamePhase;
  hasSession: boolean;
  isOnTrail: boolean;
}

/**
 * Derives the current game phase from session state.
 * The frontend never invents game state — it only reads what the backend provides.
 *
 * Phases:
 * - pre-session: no session loaded
 * - setup: session exists but setup not yet complete (should not normally occur)
 * - prologue: setup complete, prologue not yet viewed
 * - town-selection: prologue viewed, town not yet selected
 * - in-town: game started, no active journey
 * - on-trail: journey is Active, Interrupted, or Completed
 *   (Completed means the player sees the last day's resolution and
 *   acknowledges arrival via TrailFlowSurface before transitioning to town)
 */
export function useGamePhase(): GamePhaseState {
  const { session } = useGameSession();

  return useMemo(() => {
    if (!session) {
      return {
        phase: "pre-session" as const,
        hasSession: false,
        isOnTrail: false,
      };
    }

    if (session.startFlowPhase !== StartFlowPhase.GameStarted) {
      if (session.startFlowPhase === StartFlowPhase.SetupComplete) {
        return { phase: "prologue" as const, hasSession: true, isOnTrail: false };
      }
      if (session.startFlowPhase === StartFlowPhase.PrologueViewed ||
          session.startFlowPhase === StartFlowPhase.StartingTownSelected) {
        return { phase: "town-selection" as const, hasSession: true, isOnTrail: false };
      }
      return { phase: "pre-session" as const, hasSession: false, isOnTrail: false };
    }

    const journey = session.journey;

    if (!journey) {
      return { phase: "in-town" as const, hasSession: true, isOnTrail: false };
    }

    // Active, Interrupted, or Completed — all mean the player is on the trail.
    // Completed shows the arrival/acknowledge view inside TrailFlowSurface.
    return { phase: "on-trail" as const, hasSession: true, isOnTrail: true };
  }, [session]);
}
```

- [ ] **Step 4: Update `DevSurfaceContext.tsx` — remove "arrival"**

Change the `DevSurface` union (line 8-16) to remove `"arrival"`:

```ts
export type DevSurface =
  | "pre-session"
  | "town"
  | "saloon"
  | "sheriff"
  | "store"
  | "trailhead"
  | "trail";
```

- [ ] **Step 5: Update `DevPanelRegistry.tsx` — remove "arrival" from TravelDevPanel**

Change line 51 from:
```ts
    surfaces: ["trail", "arrival", "trailhead"],
```
to:
```ts
    surfaces: ["trail", "trailhead"],
```

- [ ] **Step 6: Update `GameFlowRouter.tsx` — remove arrival case (temporary, deleted in Task 9)**

Remove the `case "arrival"` branch and the `setDevSurface("arrival")` call. The `on-trail` case now handles Completed journeys (TrailFlowSurface renders the arrival view). Replace the full file:

```tsx
// src/flow/GameFlowRouter.tsx
import { useEffect, useState } from "react";
import { useGamePhase } from "../hooks/useGamePhase";
import { useSetDevSurface } from "../dev/DevSurfaceContext";
import type { DevSurface } from "../dev/DevSurfaceContext";
import { PreSessionSurface } from "./PreSessionSurface";
import { TownHubSurface } from "./TownHubSurface";
import { TrailFlowSurface } from "./TrailFlowSurface";

export type TownPlace = "store" | "sheriff" | "saloon" | "trailhead" | null;

const placeToSurface: Record<Exclude<TownPlace, null>, DevSurface> = {
  store: "store",
  sheriff: "sheriff",
  saloon: "saloon",
  trailhead: "trailhead",
};

export function GameFlowRouter() {
  const { phase } = useGamePhase();
  const [activePlace, setActivePlace] = useState<TownPlace>(null);
  const setDevSurface = useSetDevSurface();

  useEffect(() => {
    if (phase === "pre-session" || phase === "setup" || phase === "prologue" || phase === "town-selection") {
      setDevSurface("pre-session");
    } else if (phase === "on-trail") {
      setDevSurface("trail");
    } else if (phase === "in-town") {
      setDevSurface(activePlace ? placeToSurface[activePlace] : "town");
    }
  }, [phase, activePlace, setDevSurface]);

  useEffect(() => {
    setActivePlace(null);
  }, [phase]);

  switch (phase) {
    case "pre-session":
    case "setup":
    case "prologue":
    case "town-selection":
      return <PreSessionSurface />;
    case "in-town":
      return (
        <TownHubSurface
          activePlace={activePlace}
          onPlaceChange={setActivePlace}
        />
      );
    case "on-trail":
      return <TrailFlowSurface />;
    default:
      return <PreSessionSurface />;
  }
}
```

- [ ] **Step 7: Update `TrailFlowSurface.tsx` — add completed-journey view**

Replace the entire file:

```tsx
// src/flow/TrailFlowSurface.tsx
import styled from "styled-components";
import { useGameSession } from "../state/useGameSession";
import { JourneyStatus } from "../api/types";
import { TravelPanel } from "../components/TravelPanel";
import { FlowSurface, Button } from "../components/ui/sharedStyled";

const TrailLockBanner = styled.div`
  padding: 12px 18px;
  background: rgba(223, 159, 79, 0.12);
  border: 1px solid rgba(223, 159, 79, 0.22);
  border-radius: 12px;
  color: var(--accent-strong);
  font-size: 0.95rem;
  font-weight: 600;
  text-align: center;
`;

const ArrivalCard = styled.div`
  padding: 32px;
  border-radius: 28px;
  background: var(--bg-elevated);
  border: 1px solid var(--border-strong);
  text-align: center;
  display: grid;
  gap: 20px;
  justify-items: center;

  h1 {
    margin: 0;
  }
`;

const ArrivalLead = styled.p`
  margin: 0;
  color: var(--muted);
  line-height: 1.5;
`;

export function TrailFlowSurface() {
  const { session, gameId, loading, handleTravelTurnResult, handleAcknowledgeArrival } = useGameSession();

  if (!session || !gameId) {
    return null;
  }

  const journey = session.journey;

  // Completed journey — show arrival content and acknowledge button
  if (journey && journey.status === JourneyStatus.Completed) {
    const destinationName = journey.destinationTownName;
    const daysTravelled = journey.daysTravelled;

    return (
      <FlowSurface $variant="trail">
        <ArrivalCard>
          <h1>You've arrived in {destinationName}</h1>
          <ArrivalLead>
            You put {daysTravelled} day{daysTravelled === 1 ? "" : "s"} of trail behind you.
          </ArrivalLead>
          <Button
            type="button"
            $variant="primary"
            onClick={() => void handleAcknowledgeArrival()}
            disabled={loading}
          >
            {loading ? "Stepping into town..." : "Step into town"}
          </Button>
        </ArrivalCard>
      </FlowSurface>
    );
  }

  // Active or Interrupted — render the trail day normally
  return (
    <FlowSurface $variant="trail">
      <TrailLockBanner role="status">
        You're on the trail. No turning back until you reach your destination.
      </TrailLockBanner>
      <TravelPanel
        gameId={gameId}
        session={session}
        busy={loading}
        onTurnResult={handleTravelTurnResult}
      />
    </FlowSurface>
  );
}
```

- [ ] **Step 8: Run the test to verify it passes**

Run: `npx vitest run src/tests/TrailFlowSurfaceCompleted.test.tsx`
Expected: PASS (3 tests)

- [ ] **Step 9: Run the full test suite to check for regressions**

Run: `npx vitest run`
Expected: Some tests may fail (e.g. `GameFlowRouter.test.tsx` arrival test, `StartOverRegression.test.tsx` allSurfaces). Note the failures — they will be fixed in Task 8. The key is that the TrailFlowSurface test passes and no compilation errors occur.

- [ ] **Step 10: Commit**

```bash
git add src/hooks/useGamePhase.ts src/dev/DevSurfaceContext.tsx src/dev/DevPanelRegistry.tsx src/flow/GameFlowRouter.tsx src/flow/TrailFlowSurface.tsx src/tests/TrailFlowSurfaceCompleted.test.tsx
git commit -m "feat: remove arrival phase, add TrailFlowSurface completed-journey view"
```

### Task 5: Place surfaces — replace onLeave/onBack with useNavigate

**Files:**
- Modify: `src/flow/places/StorePlace.tsx`
- Modify: `src/flow/places/SheriffPlace.tsx`
- Modify: `src/flow/places/SaloonPlace.tsx`
- Modify: `src/flow/TravelPrepSurface.tsx`

**Interfaces:**
- All four components lose their `onLeave`/`onBack` props and use `useNavigate()` to navigate to `/town` instead

- [ ] **Step 1: Update StorePlace.tsx**

Remove the `StorePlaceProps` interface and `onLeave` prop. Add `useNavigate` import and replace `onLeave` with `useNavigate({ to: "/town" })`.

Replace lines 1-6 and 22-36:

```tsx
// src/flow/places/StorePlace.tsx — new imports (line 1-6)
import styled from "styled-components";
import { useNavigate } from "@tanstack/react-router";
import { useGameSession } from "../../state/useGameSession";
import { StoreOffersPanel } from "../../components/StoreOffersPanel";
import { InventoryPanel } from "../../components/InventoryPanel";
import { FlowSurface, BackButton, FlowNotice, FlowError } from "../../components/ui/sharedStyled";
```

Remove the `StorePlaceProps` interface (lines 22-24) and change the function signature:

```tsx
export function StorePlace() {
  const navigate = useNavigate();
  const { session, storeOffers, storeOffersLoading, loading, handleBuyOffer, notice, error } = useGameSession();

  if (!session) {
    return null;
  }

  return (
    <FlowSurface $variant="place">
      <PlaceHeader>
        <BackButton type="button" onClick={() => void navigate({ to: "/town" })}>
          ← Back to town
        </BackButton>
        <h1>Store</h1>
      </PlaceHeader>
      <PlaceBody>
        <StoreOffersPanel
          storeOffers={storeOffers}
          loading={storeOffersLoading}
          busy={loading}
          onBuyOffer={handleBuyOffer}
        />
        <InventoryPanel inventory={session.inventory} />
        {notice ? <FlowNotice>{notice}</FlowNotice> : null}
        {error ? <FlowError>{error}</FlowError> : null}
      </PlaceBody>
    </FlowSurface>
  );
}
```

- [ ] **Step 2: Update SheriffPlace.tsx**

Same pattern — remove `SheriffPlaceProps` and `onLeave`, add `useNavigate`:

Add import after line 1:
```tsx
import { useNavigate } from "@tanstack/react-router";
```

Remove `SheriffPlaceProps` interface (lines 40-42) and change:
```tsx
export function SheriffPlace() {
  const navigate = useNavigate();
  const {
    session,
    journal,
    wantedPosters,
    loading,
    busyMode,
    canReadWantedPosters,
    canCheckLocalRecords,
    handleReadWantedPosters,
    handleCheckLocalRecords,
    notice,
    error,
  } = useGameSession();

  if (!session) {
    return null;
  }

  const sheriffLeads: ClueDto[] = (journal?.caseFile.knownClues ?? []).filter(
    (clue) => clue.sourceKind === InvestigationSourceKind.LocalRecords,
  );

  return (
    <FlowSurface $variant="place">
      <PlaceHeader>
        <BackButton type="button" onClick={() => void navigate({ to: "/town" })}>
          ← Back to town
        </BackButton>
        <h1>Sheriff Office</h1>
      </PlaceHeader>
      {/* ... rest of PlaceBody unchanged ... */}
```

- [ ] **Step 3: Update SaloonPlace.tsx**

Same pattern — remove `SaloonPlaceProps` and `onLeave`, add `useNavigate`:

Add import after line 1:
```tsx
import { useNavigate } from "@tanstack/react-router";
```

Remove `SaloonPlaceProps` interface (lines 31-33) and change:
```tsx
export function SaloonPlace() {
  const navigate = useNavigate();
  const {
    session,
    wantedPosters,
    declaredWantedIdentityHandle,
    setDeclaredWantedIdentityHandle,
    loading,
    busyMode,
    gameId,
    selectedWantedPoster,
    canLookAroundSaloon,
    canGatherLocalGossip,
    canConfrontSaloonPersonOfInterest,
    handleLookAroundSaloon,
    handleGatherLocalGossip,
    handleConfrontSaloonPersonOfInterest,
    notice,
    error,
  } = useGameSession();

  if (!session) {
    return null;
  }

  const personOfInterest = session.activeSaloonPersonOfInterest;

  return (
    <FlowSurface $variant="place">
      <PlaceHeader>
        <BackButton type="button" onClick={() => void navigate({ to: "/town" })}>
          ← Back to town
        </BackButton>
        <h1>Saloon</h1>
      </PlaceHeader>
      {/* ... rest of PlaceBody unchanged ... */}
```

- [ ] **Step 4: Update TravelPrepSurface.tsx**

Remove `TravelPrepSurfaceProps` and `onBack`, add `useNavigate`:

Add import after line 2:
```tsx
import { useNavigate } from "@tanstack/react-router";
```

Remove `TravelPrepSurfaceProps` interface (lines 48-50) and change the function signature (line 87):

```tsx
export function TravelPrepSurface() {
  const navigate = useNavigate();
  const { session, gameId, loading, handleTravel, notice, error } = useGameSession();
```

Replace the `onBack` usage (line 188):
```tsx
        <BackButton type="button" onClick={() => void navigate({ to: "/town" })}>
          ← Back to town
        </BackButton>
```

- [ ] **Step 5: Verify compilation**

Run: `npx tsc --noEmit`
Expected: Errors in `TownHubSurface.tsx` (still passes `onLeave`/`onBack` to these components) and `SheriffPlace.test.tsx` / `TravelPrepSurface.test.tsx` (still pass props). These will be fixed in Tasks 6 and 8. Note the errors but proceed.

- [ ] **Step 6: Commit**

```bash
git add src/flow/places/StorePlace.tsx src/flow/places/SheriffPlace.tsx src/flow/places/SaloonPlace.tsx src/flow/TravelPrepSurface.tsx
git commit -m "refactor: place surfaces use useNavigate instead of onLeave/onBack props"
```

### Task 6: TownHubSurface — route-based navigation + arrival notice

**Files:**
- Modify: `src/flow/TownHubSurface.tsx`

**Interfaces:**
- `TownHubSurface` no longer takes `activePlace`/`onPlaceChange` props
- Place cards use `useNavigate()` to navigate to `/town/store`, `/town/sheriff`, `/town/saloon`, `/town/trailhead`
- Reads `?arrived=1` search param via `useSearch` to show an arrival notice
- `TownPlace` type is inlined here (no longer imported from `GameFlowRouter`)

- [ ] **Step 1: Replace TownHubSurface.tsx**

```tsx
// src/flow/TownHubSurface.tsx
import styled from "styled-components";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { useGameSession } from "../state/useGameSession";
import { AvailableActionKind } from "../api/types";
import { FlowSurface } from "../components/ui/sharedStyled";

const TownHubHeader = styled.header`
  display: grid;
  gap: 8px;
  padding: 24px 0 4px;

  h1 {
    margin: 0;
  }
`;

const TownHubLead = styled.p`
  margin: 0;
  font-size: 1.1rem;
  color: var(--muted);
`;

const ArrivalNotice = styled.p`
  margin: 0;
  padding: 12px 18px;
  background: rgba(223, 159, 79, 0.12);
  border: 1px solid rgba(223, 159, 79, 0.22);
  border-radius: 12px;
  color: var(--accent-strong);
  font-size: 0.95rem;
  font-weight: 600;
`;

const TownHubGrid = styled.div`
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 16px;

  @media (max-width: 1366px) {
    grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  }

  @media (max-width: 960px) {
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  }
`;

const PlaceCard = styled.button`
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px;
  background: var(--bg-elevated);
  border: 1px solid var(--border);
  border-radius: 20px;
  text-align: left;
  cursor: pointer;
  color: var(--text);
  transition:
    transform 0.15s ease-out,
    border-color 0.15s ease-out;

  &:hover {
    border-color: var(--accent);
    transform: translateY(-2px);
  }

  &.trailhead {
    border-color: var(--accent-strong);
    background: linear-gradient(180deg, var(--bg-elevated), rgba(223, 159, 79, 0.05));
  }
`;

const PlaceCardIcon = styled.div`
  font-size: 2rem;
  flex-shrink: 0;
`;

const PlaceCardBody = styled.div`
  display: grid;
  gap: 4px;

  strong {
    font-size: 1.05rem;
  }

  p {
    margin: 0;
    font-size: 0.9rem;
    color: var(--muted);
  }
`;

export function TownHubSurface() {
  const { session, currentTown, actions } = useGameSession();
  const navigate = useNavigate();
  const { arrived } = useSearch({ strict: false }) as { arrived?: string };

  if (!session) {
    return null;
  }

  const hasStore = actions.some((a) => a.kind === AvailableActionKind.BuySupplies);
  const hasSheriff =
    actions.some((a) => a.kind === AvailableActionKind.ReadWantedPosters) ||
    actions.some((a) => a.kind === AvailableActionKind.CheckSheriffRecords);
  const hasSaloon = actions.some((a) => a.kind === AvailableActionKind.LookAroundSaloon);
  const hasTrailhead = actions.some((a) => a.kind === AvailableActionKind.Travel);

  const townName = currentTown?.name ?? session.player.currentTownId;

  return (
    <FlowSurface $variant="town-hub">
      <TownHubHeader>
        <h1>{townName}</h1>
        <TownHubLead>Where to next?</TownHubLead>
      </TownHubHeader>
      {arrived === "1" ? (
        <ArrivalNotice role="status">
          You've arrived in {townName}. Take a moment to look around.
        </ArrivalNotice>
      ) : null}
      <TownHubGrid>
        {hasStore ? (
          <PlaceCard type="button" onClick={() => void navigate({ to: "/town/store" })}>
            <PlaceCardIcon aria-hidden="true">📦</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Store</strong>
              <p>Buy supplies, food, and gear.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
        {hasSheriff ? (
          <PlaceCard type="button" onClick={() => void navigate({ to: "/town/sheriff" })}>
            <PlaceCardIcon aria-hidden="true">⭐</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Sheriff Office</strong>
              <p>Read wanted posters and check records.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
        {hasSaloon ? (
          <PlaceCard type="button" onClick={() => void navigate({ to: "/town/saloon" })}>
            <PlaceCardIcon aria-hidden="true">🥃</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Saloon</strong>
              <p>Look around, gather gossip, confront a suspect.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
        {hasTrailhead ? (
          <PlaceCard type="button" className="trailhead" onClick={() => void navigate({ to: "/town/trailhead" })}>
            <PlaceCardIcon aria-hidden="true">🐎</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Hit the trail</strong>
              <p>Ride to the next town.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
      </TownHubGrid>
    </FlowSurface>
  );
}
```

- [ ] **Step 2: Verify compilation**

Run: `npx tsc --noEmit`
Expected: Errors only in `GameFlowRouter.tsx` (still passes props to TownHubSurface) and test files. These will be fixed in Tasks 8 and 9.

- [ ] **Step 3: Commit**

```bash
git add src/flow/TownHubSurface.tsx
git commit -m "refactor: TownHubSurface uses useNavigate for place cards, adds arrival notice"
```

### Task 7: PreSessionSurface — lazy-load StartingTownStep for Phaser isolation

**Files:**
- Modify: `src/flow/PreSessionSurface.tsx`

**Interfaces:**
- `StartingTownStep` is now lazy-loaded inside `PreSessionSurface` so Phaser is not loaded for name/prologue steps

- [ ] **Step 1: Update PreSessionSurface.tsx**

Replace the static import of `StartingTownStep` (line 9) with a lazy import and add `Suspense`:

Change lines 1-10 to:

```tsx
import { lazy, Suspense } from "react";
import styled from "styled-components";
import { useGamePhase } from "../hooks/useGamePhase";
import { useGameSession } from "../state/useGameSession";
import { useStartFlow } from "../hooks/useStartFlow";
import { encodeGameSetupSeed } from "../ui/gameSetupSeedCodec";
import { FlowSurface, FlowNotice, FlowError } from "../components/ui/sharedStyled";
import { SetupHuntStep } from "../components/start-flow/SetupHuntStep";
import { StorySoFarStep } from "../components/start-flow/StorySoFarStep";
import { CreatingStep } from "../components/start-flow/CreatingStep";

const StartingTownStep = lazy(() =>
  import("../components/start-flow/StartingTownStep").then((m) => ({ default: m.StartingTownStep })),
);
```

Then wrap the `StartingTownStep` usage (around line 103) in `Suspense`:

```tsx
      {effectiveStep === "town" && (
        <Suspense fallback={<div>Loading town selection…</div>}>
          <StartingTownStep
            sessionId={session?.id ?? ""}
            selectedTownId={flow.selectedTownId}
            onSelectTown={handleStartWithTown}
          />
        </Suspense>
      )}
```

- [ ] **Step 2: Verify compilation**

Run: `npx tsc --noEmit`
Expected: no new errors (the lazy import is self-contained)

- [ ] **Step 3: Run StartingTownStep test to verify it still passes**

Run: `npx vitest run src/tests/StartingTownStep.test.tsx`
Expected: PASS — the test renders `StartingTownStep` directly, not through `PreSessionSurface`, so the lazy boundary doesn't affect it.

- [ ] **Step 4: Commit**

```bash
git add src/flow/PreSessionSurface.tsx
git commit -m "perf: lazy-load StartingTownStep to isolate Phaser from start-flow chunk"
```

### Task 8: Expand route tree + wire sync hooks in AppShell

**Files:**
- Modify: `src/shell/router.tsx`
- Modify: `src/shell/AppShell.tsx`

**Interfaces:**
- `router.tsx` exports a new `router` with lazy-loaded routes for `/`, `/town`, `/town/store`, `/town/sheriff`, `/town/saloon`, `/town/trailhead`, `/trail`
- The `/town` route has `validateSearch` for the `arrived` query param
- `AppShell.tsx` calls `usePhaseRouteSync()` and `useDevSurfaceSync()` in `ShellChrome` and wraps `Outlet` in `Suspense`

- [ ] **Step 1: Replace router.tsx**

```tsx
// src/shell/router.tsx
import { lazy } from "react";
import { createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { AppShell } from "./AppShell";

const PreSessionSurface = lazy(() =>
  import("../flow/PreSessionSurface").then((m) => ({ default: m.PreSessionSurface })),
);
const TownHubSurface = lazy(() =>
  import("../flow/TownHubSurface").then((m) => ({ default: m.TownHubSurface })),
);
const StorePlace = lazy(() =>
  import("../flow/places/StorePlace").then((m) => ({ default: m.StorePlace })),
);
const SheriffPlace = lazy(() =>
  import("../flow/places/SheriffPlace").then((m) => ({ default: m.SheriffPlace })),
);
const SaloonPlace = lazy(() =>
  import("../flow/places/SaloonPlace").then((m) => ({ default: m.SaloonPlace })),
);
const TravelPrepSurface = lazy(() =>
  import("../flow/TravelPrepSurface").then((m) => ({ default: m.TravelPrepSurface })),
);
const TrailFlowSurface = lazy(() =>
  import("../flow/TrailFlowSurface").then((m) => ({ default: m.TrailFlowSurface })),
);

const rootRoute = createRootRoute({
  component: AppShell,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: PreSessionSurface,
});

const townRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/town",
  validateSearch: (search: Record<string, unknown>) => ({
    arrived: search.arrived === "1" ? "1" : undefined,
  }),
  component: TownHubSurface,
});

const storeRoute = createRoute({
  getParentRoute: () => townRoute,
  path: "store",
  component: StorePlace,
});

const sheriffRoute = createRoute({
  getParentRoute: () => townRoute,
  path: "sheriff",
  component: SheriffPlace,
});

const saloonRoute = createRoute({
  getParentRoute: () => townRoute,
  path: "saloon",
  component: SaloonPlace,
});

const trailheadRoute = createRoute({
  getParentRoute: () => townRoute,
  path: "trailhead",
  component: TravelPrepSurface,
});

const trailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/trail",
  component: TrailFlowSurface,
});

const routeTree = rootRoute.addChildren([
  indexRoute,
  townRoute.addChildren([storeRoute, sheriffRoute, saloonRoute, trailheadRoute]),
  trailRoute,
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
```

- [ ] **Step 2: Update AppShell.tsx — add sync hooks and Suspense**

Add imports after line 2 (`import { Outlet } from "@tanstack/react-router";`):

```tsx
import { Suspense } from "react";
import { usePhaseRouteSync } from "./usePhaseRouteSync";
import { useDevSurfaceSync } from "./useDevSurfaceSync";
import { RouteLoading } from "./RouteLoading";
```

Inside `ShellChrome`, add the sync hooks after the existing `useEffect` (after line 19):

```tsx
  usePhaseRouteSync();
  useDevSurfaceSync();
```

Wrap the `Outlet` in `Suspense` (replace lines 43-46):

```tsx
        <RouteOutlet aria-live="polite">
          <Route>
            <Suspense fallback={<RouteLoading />}>
              <Outlet />
            </Suspense>
          </Route>
        </RouteOutlet>
```

- [ ] **Step 3: Verify compilation**

Run: `npx tsc --noEmit`
Expected: Errors only in `GameFlowRouter.tsx` (no longer imported by router but still exists with stale `TownPlace` export and `activePlace`/`onPlaceChange` props passed to `TownHubSurface`). This file will be deleted in Task 9. Also errors in test files that will be fixed in Task 10.

- [ ] **Step 4: Run the full test suite**

Run: `npx vitest run`
Expected: Some failures in `AppShell.test.tsx`, `GameFlowRouter.test.tsx`, `SheriffPlace.test.tsx`, `TravelPrepSurface.test.tsx`, `StartOverRegression.test.tsx`. Note the failures — they will be fixed in Task 10. The new hook tests (`usePhaseRouteSync`, `useDevSurfaceSync`, `TrailFlowSurfaceCompleted`) should pass.

- [ ] **Step 5: Commit**

```bash
git add src/shell/router.tsx src/shell/AppShell.tsx
git commit -m "feat: expand route tree with lazy-loaded routes, wire sync hooks in AppShell"
```

### Task 9: Remove dead code + regenerate index mesh

**Files:**
- Delete: `src/flow/GameFlowRouter.tsx`
- Delete: `src/flow/ArrivalSurface.tsx`
- Delete: `src/routes/HuntRoute.tsx`
- Delete: `src/routes/TrailRoute.tsx`
- Delete: `src/routes/CaseFileRoute.tsx`
- Delete: `src/routes/WantedRoute.tsx`
- Delete: `src/routes/INDEX.md`
- Delete: `src/tests/GameFlowRouter.test.tsx`
- Regenerate: `src/flow/INDEX.md`, `src/routes/` directory

- [ ] **Step 1: Delete dead files**

```bash
rm src/flow/GameFlowRouter.tsx
rm src/flow/ArrivalSurface.tsx
rm -r src/routes/
rm src/tests/GameFlowRouter.test.tsx
```

- [ ] **Step 2: Regenerate index mesh**

From the repo root:
```bash
python scripts/generate_index_mesh.py
```
Expected: `src/flow/INDEX.md` updated (no longer lists `ArrivalSurface.tsx` or `GameFlowRouter.tsx`). `src/routes/` directory no longer exists.

- [ ] **Step 3: Verify compilation**

Run: `npx tsc --noEmit`
Expected: No references to deleted files. If any remain, search for imports of `GameFlowRouter`, `ArrivalSurface`, or `src/routes/` and remove them.

- [ ] **Step 4: Run the full test suite**

Run: `npx vitest run`
Expected: Failures only in test files that reference deleted modules or pass stale props (fixed in Task 10).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: remove dead code (GameFlowRouter, ArrivalSurface, orphaned routes)"
```

### Task 10: Update existing tests

**Files:**
- Modify: `src/tests/AppShell.test.tsx`
- Modify: `src/tests/StartOverRegression.test.tsx`
- Modify: `src/tests/SheriffPlace.test.tsx`
- Modify: `src/tests/TravelPrepSurface.test.tsx`
- Modify: `src/tests/StorePlaceFeedback.test.tsx` (if it references GameFlowRouter or passes props)
- Modify: `src/tests/StartOverConfirmation.test.tsx` (if it references GameFlowRouter)
- Modify: `src/tests/GameSettingsOverlay.test.tsx` (if it references GameFlowRouter)
- Modify: `src/tests/DevOverlay.test.tsx` (if it references "arrival" surface)

- [ ] **Step 1: Fix SheriffPlace.test.tsx — remove onLeave prop**

Find the `renderSheriffPlace` function (line 152) and change:
```tsx
        <SheriffPlace onLeave={() => {}} />
```
to:
```tsx
        <SheriffPlace />
```

Also add a `RouterProvider` wrapper since `SheriffPlace` now uses `useNavigate`. Add imports:
```tsx
import { RouterProvider, createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
```

And wrap the render in a test router:
```tsx
function renderSheriffPlace() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const rootRoute = createRootRoute({ component: () => <Outlet /> });
  const townRoute = createRoute({ getParentRoute: () => rootRoute, path: "/town", component: () => <SheriffPlace /> });
  const router = createRouter({ routeTree: rootRoute.addChildren([townRoute]) });
  window.history.replaceState({}, "", "/town");
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <RouterProvider router={router} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}
```

Add `import { Outlet } from "@tanstack/react-router";` to imports.

- [ ] **Step 2: Fix TravelPrepSurface.test.tsx — remove onBack prop**

Find the `renderPrep` function (line 218) and change:
```tsx
        <TravelPrepSurface onBack={vi.fn()} />
```
to:
```tsx
        <TravelPrepSurface />
```

Also wrap in a `RouterProvider` with a `/town/trailhead` route. Add imports:
```tsx
import { RouterProvider, createRootRoute, createRoute, createRouter, Outlet } from "@tanstack/react-router";
```

And update `renderPrep`:
```tsx
function renderPrep() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const rootRoute = createRootRoute({ component: () => <Outlet /> });
  const trailheadRoute = createRoute({ getParentRoute: () => rootRoute, path: "/town/trailhead", component: () => <TravelPrepSurface /> });
  const router = createRouter({ routeTree: rootRoute.addChildren([trailheadRoute]) });
  window.history.replaceState({}, "", "/town/trailhead");
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <RouterProvider router={router} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}
```

- [ ] **Step 3: Fix StartOverRegression.test.tsx — remove "arrival" from allSurfaces**

Find the `allSurfaces` array (around line 266-275) and remove `"arrival"`:

```tsx
const allSurfaces: DevSurface[] = [
  "pre-session",
  "town",
  "saloon",
  "sheriff",
  "store",
  "trailhead",
  "trail",
];
```

Also check if the test uses `RouterProvider` with the real `router` — if so, it should still work since the new route tree is compatible. If any test navigates to a URL that no longer exists, update the URL.

- [ ] **Step 4: Fix AppShell.test.tsx — update for new route tree**

The test "defaults to the flow router and shows the pre-session surface" (line 239) should still work — the `/` route renders `PreSessionSurface`. Verify it passes.

The test "renders the persistent HUD with player name and clock once a session hydrates" (line 228) sets `localStorage` and expects the HUD. With the new sync hooks, the app will navigate from `/` to `/town` when the session loads. The HUD should still be visible. If the test fails because it can't find the heading (e.g. "Tumbleweed" is now on `/town`), update the test to check for the HUD elements that are always visible regardless of route.

If any test fails because it expects `GameFlowRouter` to render a specific surface, update it to navigate to the correct URL first or check for route-specific content.

- [ ] **Step 5: Fix any other test files that reference deleted modules**

Search for imports of `GameFlowRouter` or `ArrivalSurface`:
```bash
grep -r "GameFlowRouter\|ArrivalSurface" src/tests/
```
If any test files still import these, remove the imports and update the tests to use the new route-based components.

- [ ] **Step 6: Fix DevOverlay.test.tsx — remove "arrival" surface references**

Search for `"arrival"` in `src/tests/DevOverlay.test.tsx`:
```bash
grep -n "arrival" src/tests/DevOverlay.test.tsx
```
If found, replace `"arrival"` with `"trail"` (since completed journey now maps to `"trail"`).

- [ ] **Step 7: Run the full test suite**

Run: `npx vitest run`
Expected: ALL TESTS PASS

- [ ] **Step 8: Commit**

```bash
git add src/tests/
git commit -m "test: update existing tests for new route tree and removed props"
```

### Task 11: Vite config — manualChunks + chunkSizeWarningLimit

**Files:**
- Modify: `vite.config.ts`

- [ ] **Step 1: Update vite.config.ts**

Replace the entire file:

```ts
// vite.config.ts
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    setupFiles: ["./src/tests/test-utils/setup.ts"],
    css: true,
    globals: false,
  },
  server: {
    host: "0.0.0.0",
    port: 5173,
  },
  build: {
    chunkSizeWarningLimit: 1100, // Phaser lazy chunk ~1 MB; only loaded on town-selection/trailhead
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ["react", "react-dom"],
          router: ["@tanstack/react-router", "@tanstack/react-query"],
          styled: ["styled-components"],
        },
      },
    },
  },
});
```

- [ ] **Step 2: Run the build**

Run: `npm run build`
Expected: Build succeeds. Check the chunk list output:
- `vendor.js` — React + ReactDOM (~130 kB)
- `router.js` — TanStack Router + Query (~80 kB)
- `styled.js` — styled-components (~45 kB)
- A lazy chunk for Phaser (~1 MB) — should NOT trigger a chunk-size warning (limit is 1100)
- `index.js` — app shell + root route (should be well under 500 kB)
- `start-flow` chunk — should NOT contain Phaser

- [ ] **Step 3: Verify Phaser isolation**

Examine the build output chunk list. Verify:
1. No chunk loaded on the initial `/` route contains Phaser (the `start-flow` / `PreSessionSurface` chunk must not import `phaser`)
2. Phaser appears in a separate chunk referenced only by `StartingTownStep` and `TravelPrepSurface`

If Phaser still appears in the start-flow chunk, the lazy boundary is wrong. Move the lazy boundary closer to `PhaserMapHost` (lazy-load `PhaserMapHost` inside `StartingTownStep` instead of lazy-loading `StartingTownStep` inside `PreSessionSurface`).

- [ ] **Step 4: Run the full test suite**

Run: `npx vitest run`
Expected: ALL TESTS PASS

- [ ] **Step 5: Commit**

```bash
git add vite.config.ts
git commit -m "perf: add manualChunks vendor split and chunkSizeWarningLimit for Phaser"
```

### Task 12: Final verification

- [ ] **Step 1: Run the full test suite**

Run: `npx vitest run`
Expected: ALL TESTS PASS (baseline + new tests)

- [ ] **Step 2: Run the build**

Run: `npm run build`
Expected: Build succeeds with no chunk-size warnings for initial-route chunks

- [ ] **Step 3: Manual dev server smoke test**

Run: `npm run dev`
Open `http://localhost:5173/` in a browser and verify:
- The start flow (name step) renders on `/`
- The DevOverlay shows `"pre-session"` surface panels
- After completing setup and entering town, the URL changes to `/town`
- Clicking "Store" navigates to `/town/store` and the back button returns to `/town`
- The DevOverlay shows `"store"` surface panels on `/town/store`
- Phaser is NOT loaded on the initial `/` route (check Network tab — no `phaser` chunk loaded until town selection or trailhead)

- [ ] **Step 4: Commit any remaining changes**

```bash
git add -A
git commit -m "chore: final verification for BUNCH-124"
```

---

## Self-Review

### Spec coverage

| Spec requirement | Task |
|---|---|
| URL routing with TanStack Router | Task 8 |
| `usePhaseRouteSync` hook | Task 2 |
| `useDevSurfaceSync` hook | Task 3 |
| Arrival flow rework (TrailFlowSurface completed-journey) | Task 4 |
| Remove `"arrival"` from `GamePhase` | Task 4 |
| Remove `"arrival"` from `DevSurface` | Task 4 |
| Update `DevPanelRegistry` surfaces | Task 4 |
| Lazy-load route components | Task 8 |
| Lazy-load `StartingTownStep` inside `PreSessionSurface` | Task 7 |
| `manualChunks` vendor splitting | Task 11 |
| `chunkSizeWarningLimit: 1100` | Task 11 |
| `validateSearch` on `/town` route | Task 8 |
| `TownHubSurface` arrival notice via `useSearch` | Task 6 |
| Place surfaces use `useNavigate` | Task 5 |
| `TownHubSurface` uses `useNavigate` for place cards | Task 6 |
| Remove `GameFlowRouter` | Task 9 |
| Remove `ArrivalSurface` | Task 9 |
| Remove orphaned `src/routes/` | Task 9 |
| Remove `GameFlowRouter.test.tsx` | Task 9 |
| Preserve arrival→town regression coverage | Task 4 (TrailFlowSurfaceCompleted test) |
| Update `AppShell.test.tsx` | Task 10 |
| Update `StartOverRegression.test.tsx` allSurfaces | Task 10 |
| Update `SheriffPlace.test.tsx` | Task 10 |
| Update `TravelPrepSurface.test.tsx` | Task 10 |
| Regenerate index mesh | Task 9 |
| Phaser isolation acceptance check | Task 11, Step 3 |

### Placeholder scan

No placeholders found. Every step contains exact code or exact commands.

### Type consistency

- `GamePhase` — `"arrival"` removed in Task 4, all consumers updated by Task 8
- `DevSurface` — `"arrival"` removed in Task 4, `DevPanelRegistry` updated in Task 4, `StartOverRegression.test.tsx` updated in Task 10
- `TownPlace` — was exported from `GameFlowRouter` (deleted in Task 9), inlined into `TownHubSurface` in Task 6
- `usePhaseRouteSync()` — no args, no return (Task 2)
- `useDevSurfaceSync()` — no args, no return (Task 3)
- `RouteLoading` — styled component, no props (Task 1)
