# BUNCH-115: Fix Travel Flow and Map Visualization — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix three travel-related UX bugs: inverted travel-mode display on the travel prep screen, missing visual map for travel destination selection, and arrival routing that skips the town hub.

**Architecture:** The backend already owns the world map read model (`GetStartingTownMapHandler` returns towns with coordinates + trails with ride-day distances from the seed-derived layout). The frontend `PhaserMapHost` is a presentation/input adapter that receives map data and emits selection intent. This plan reuses both for travel selection instead of duplicating them. The travel-mode display fix is a one-line frontend enum correction. The arrival routing fix resets the town-hub place state when the game phase transitions away from `in-town`.

**Tech Stack:** C#/.NET 10, ASP.NET Core Minimal APIs, React 18, TanStack Query, styled-components, Phaser 2D, Vitest, xUnit.

## Global Constraints

- `GameSession` remains the live-play aggregate root; Phaser must not own gameplay truth.
- The Phaser layer is presentation/input only. It may emit `townSelected` intent, but it must not calculate legal moves, route eligibility, or travel truth.
- Travel, journey, and encounter DTO mapping stays in `TravelMapper`; `GameSessionMapper` delegates rather than duplicates.
- Travel advances one trail day at a time; do not reintroduce instant multi-day travel.
- Horse and saddle are separate inventory concepts. Mounted travel requires a living/non-lame horse plus saddle.
- Use `styled-components` for component-owned layout; reference design tokens via `var(--token-name)`.
- Re-use shared primitives from `src/components/ui/sharedStyled.tsx` for genuine cross-surface patterns.
- Do not invent frontend-only game truth. React renders backend/player-known state.
- Keep player-facing surfaces as in-world game surfaces, not cockpit dashboards.
- Do not create loose agent artifact files at repo root or in product folders.

---

## Preflight Findings

Current source inspection on `origin/main` (commit `79a4277`) found these three bugs and their seams:

### Bug 1: Travel prep shows wrong travel mode

`src/WildBunch.Web/src/flow/TravelPrepSurface.tsx:202` checks:
```tsx
{preview.travelMode === 1 ? " on horseback" : " on foot"}
```

The C# enum (`src/WildBunch.Domain/Travel/TravelRouteModels.cs:9-13`) is:
```csharp
public enum TravelMode
{
    Mounted = 0,
    Foot = 1
}
```

So `travelMode === 1` means **Foot**, but the ternary shows "on horseback" for that value. The condition is inverted. When the player has a horse, `travelMode` is `0` (Mounted), and the ternary falls to the else branch showing "on foot". The `TravelPreviewDto.travelMode` field is correctly populated by the backend (`TravelMapper.cs:26` maps `preview.TravelMode` directly).

The frontend `types.ts` declares `export type TravelMode = 0 | 1;` but has no const object (unlike `JourneyStatus` and `StartFlowPhase` which do). Adding a `TravelMode` const object and using it fixes the inversion and prevents recurrence.

### Bug 2: No visual map for travel selection

`TravelPrepSurface.tsx` renders destinations as a text list of `DestinationCard` buttons. The `PhaserMapHost` component (`src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`) already renders a Phaser map with towns, trail lines, and ride-day distance labels, but it is only used in `StartingTownStep.tsx` for starting-town selection.

The existing `GET /api/games/{id}/starting-town-map` endpoint (`GameSessionEndpoints.cs:38-41`) returns `StartingTownMapDto` with towns (id, name, services, x, y) and trails (id, fromTownId, toTownId, rideDayDistance). This is the full world map — the same data needed for travel selection. The handler (`GetStartingTownMapHandler.cs`) derives coordinates from the session's seed code via `SeedWorldMapLayout`.

The issue asks for `GET /api/games/{id}/world-map`. Rather than duplicating the handler, this plan adds the `world-map` route as a thin alias that calls the same `GetStartingTownMapHandler`. The frontend gets a `getWorldMap` function for semantic clarity in the travel flow.

The `StartingTownMapScene` needs generalization for travel mode: it currently makes all towns selectable. For travel, only connected destinations should be clickable, the current town should be highlighted as the origin, and non-connected towns should be visible but not interactive.

### Bug 3: Arrival skips town hub

`src/WildBunch.Web/src/flow/GameFlowRouter.tsx:21` holds `activePlace` state:
```tsx
const [activePlace, setActivePlace] = useState<TownPlace>(null);
```

When the player clicks "Hit the trail", `activePlace` is set to `"trailhead"`, and `TownHubSurface` renders `TravelPrepSurface`. When the player starts the ride, a journey is created, and `useGamePhase` returns `"on-trail"`. `GameFlowRouter` switches to `TrailFlowSurface`. But `activePlace` is never reset — it stays `"trailhead"`.

When the journey completes, `useGamePhase` returns `"arrival"`, and `ArrivalSurface` renders. When the player clicks "Step into town", `handleAcknowledgeArrival` clears the journey, `useGamePhase` returns `"in-town"`, and `GameFlowRouter` renders `TownHubSurface` with `activePlace` still set to `"trailhead"` — so `TravelPrepSurface` renders instead of the town hub.

The fix: reset `activePlace` to `null` whenever the game phase changes. This ensures the town hub shows after arrival, and also cleans up if the player navigates through other phase transitions.

---

## File Structure

- `src/WildBunch.Web/src/api/types.ts` — add `TravelMode` const object
- `src/WildBunch.Web/src/flow/TravelPrepSurface.tsx` — fix travel-mode display, integrate map for destination selection
- `src/WildBunch.Web/src/flow/GameFlowRouter.tsx` — reset `activePlace` on phase change
- `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx` — generalize scene for travel mode (current town highlight, restricted selectable towns)
- `src/WildBunch.Web/src/api/wildBunchApi.ts` — add `getWorldMap` function
- `src/WildBunch.Api/Games/GameSessionEndpoints.cs` — add `world-map` route aliasing existing handler
- `src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx` — new test file for travel-mode display and map integration
- `src/WildBunch.Web/src/tests/GameFlowRouter.test.tsx` — new test file for arrival routing
- `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx` — add tests for travel-mode scene behavior
- `tests/WildBunch.Application.Tests/GetWorldMapHandlerTests.cs` — new test file for world-map handler contract validation
- `tests/WildBunch.Integration.Tests/WorldMapEndpointTests.cs` — new integration test file for world-map endpoint (HTTP route, 200 OK, 404 for missing session)

---

### Task 1: Fix inverted travel-mode display

**Files:**
- Modify: `src/WildBunch.Web/src/api/types.ts:18` — add `TravelMode` const object
- Modify: `src/WildBunch.Web/src/flow/TravelPrepSurface.tsx:202` — fix the inverted ternary
- Test: `src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx` — new file

**Interfaces:**
- Consumes: `TravelPreviewDto.travelMode` from the backend (already correct)
- Produces: correct "on horseback" / "on foot" display text

- [ ] **Step 1: Write the failing test for travel-mode display**

Create `src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx`:

```tsx
import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { GameSessionDto, TravelPreviewResultDto } from "../api/types";
import { previewTravel, travel } from "../api/wildBunchApi";
import { TravelPrepSurface } from "../flow/TravelPrepSurface";

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    constructor(config: unknown) { this.config = config; }
    destroy() {}
  }
  class Scene { constructor(_key?: string) {} }
  const Scale = { FIT: 0, CENTER_BOTH: 0 };
  return { default: { Game, Scene, Scale }, Game, Scene, Scale };
});

vi.mock("../api/wildBunchApi", () => ({
  previewTravel: vi.fn(),
  travel: vi.fn(),
  getWorldMap: vi.fn(),
}));

const mockedPreviewTravel = vi.mocked(previewTravel);
const mockedTravel = vi.mocked(travel);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

function createSession(overrides: Partial<GameSessionDto> = {}): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
    startFlowPhase: 3,
    player: { name: "Ruth", currentTownId: "t-town", health: 9 },
    world: {
      towns: [
        { id: "t-town", name: "Tumbleweed", services: 0 },
        { id: "dust-fork", name: "Dust Fork", services: 0 },
      ],
      trails: [
        { id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", risk: 1, terrain: 0, waterFeature: 1, rideDayDistance: 3 },
      ],
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
    clock: { day: 1, turn: 0, timeOfDay: "Morning" },
    pursuitState: { heat: 0 },
    journey: null,
    travelDiary: null,
    logEntries: [],
    activeSaloonPersonOfInterest: null,
    wantedPosters: [],
    ...overrides,
  };
}

function createPreview(overrides: Partial<TravelPreviewResultDto["preview"]> = {}): TravelPreviewResultDto {
  return {
    success: true,
    message: "Preview ready.",
    preview: {
      originTownId: "t-town",
      originTownName: "Tumbleweed",
      destinationTownId: "dust-fork",
      destinationTownName: "Dust Fork",
      travelMode: 0,
      mountedTravelAvailable: true,
      waterSecure: true,
      rideDayDistance: 3,
      remainingRideDayDistance: 3,
      baselineRideDays: 2,
      expectedDays: 2,
      remainingDays: 2,
      canteenChargesPerDay: 0,
      requiredCanteenCharges: 0,
      availableCanteenCharges: 10,
      canteenReserveCharges: 10,
      delayMarginDays: 0,
      delayRisk: false,
      requiredFood: 2,
      availableFood: 6,
      requiredHorseFeed: 2,
      availableHorseFeed: 6,
      horseState: { hunger: 0, thirst: 0, exhaustion: 0, isLame: false, isDead: false, canProvideMountedTravel: true },
      warnings: [],
      routeProfile: {
        trailId: "trail-1",
        risk: 1,
        terrain: 0,
        waterFeature: 1,
        rideDayDistance: 3,
        mountedRideDayProgress: 1.5,
        footRideDayProgress: 0.75,
        warnings: [],
      },
      ...overrides,
    },
  };
}
```

Add the test that fails with the current inverted logic — when `travelMode` is `0` (Mounted), the surface should say "on horseback", not "on foot":

```tsx
describe("TravelPrepSurface travel-mode display", () => {
  it("shows 'on horseback' when travelMode is Mounted (0)", async () => {
    const user = userEvent.setup();
    const session = createSession();
    mockedPreviewTravel.mockResolvedValue(createPreview({ travelMode: 0 }));

    render(
      <TravelPrepSurface onBack={vi.fn()} session={session} gameId={session.id} loading={false} handleTravel={vi.fn()} notice="" error="" />,
    );

    // Click the destination to enter the prep/confirmation screen
    const destButton = await screen.findByRole("button", { name: /dust fork/i });
    await user.click(destButton);

    await waitFor(() => {
      expect(screen.getByText(/on horseback/i)).toBeInTheDocument();
    });
    expect(screen.queryByText(/on foot/i)).not.toBeInTheDocument();
  });

  it("shows 'on foot' when travelMode is Foot (1)", async () => {
    const user = userEvent.setup();
    const session = createSession();
    mockedPreviewTravel.mockResolvedValue(createPreview({ travelMode: 1 }));

    render(
      <TravelPrepSurface onBack={vi.fn()} session={session} gameId={session.id} loading={false} handleTravel={vi.fn()} notice="" error="" />,
    );

    const destButton = await screen.findByRole("button", { name: /dust fork/i });
    await user.click(destButton);

    await waitFor(() => {
      expect(screen.getByText(/on foot/i)).toBeInTheDocument();
    });
    expect(screen.queryByText(/on horseback/i)).not.toBeInTheDocument();
  });
});
```

Note: `TravelPrepSurface` currently pulls from `useGameSession()` context. The test must either wrap in a `GameSessionProvider` with mocked queries, or the component must be refactored to accept props. Since the existing `TravelPrepSurface` uses `useGameSession()`, the test should use `renderInSessionProvider` from `test-utils/renderHelpers.tsx` with mocked API functions. Adjust the test to use the session provider pattern. See `tests/TravelRoutesPanel.test.tsx` for the direct-props pattern and `tests/StartFlow.test.tsx` for the provider pattern. Use whichever matches the component's actual interface after Task 5 props adjustment.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/WildBunch.Web && npx vitest run src/tests/TravelPrepSurface.test.tsx`
Expected: FAIL — "on horseback" not found when `travelMode === 0` (the current code shows "on foot" for Mounted).

- [ ] **Step 3: Add TravelMode const object to types.ts**

In `src/WildBunch.Web/src/api/types.ts`, after line 18 (`export type TravelMode = 0 | 1;`), add:

```ts
export const TravelMode = {
  Mounted: 0,
  Foot: 1,
} as const;
```

- [ ] **Step 4: Fix the inverted ternary in TravelPrepSurface.tsx**

In `src/WildBunch.Web/src/flow/TravelPrepSurface.tsx`, add the import at the top:

```tsx
import { TravelMode } from "../api/types";
```

Change line 202 from:
```tsx
{preview.travelMode === 1 ? " on horseback" : " on foot"}.
```
to:
```tsx
{preview.travelMode === TravelMode.Mounted ? " on horseback" : " on foot"}.
```

- [ ] **Step 5: Run test to verify it passes**

Run: `cd src/WildBunch.Web && npx vitest run src/tests/TravelPrepSurface.test.tsx`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Web/src/api/types.ts src/WildBunch.Web/src/flow/TravelPrepSurface.tsx src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx
git commit -m "fix: correct inverted travel-mode display on travel prep surface

TravelMode.Mounted is 0 and TravelMode.Foot is 1, but the ternary
checked travelMode === 1 for horseback. Add a TravelMode const object
and use TravelMode.Mounted for the check."
```

---

### Task 2: Fix arrival routing to show town hub

**Files:**
- Modify: `src/WildBunch.Web/src/flow/GameFlowRouter.tsx:21-22` — reset `activePlace` on phase change
- Test: `src/WildBunch.Web/src/tests/GameFlowRouter.test.tsx` — new file

**Interfaces:**
- Consumes: `useGamePhase()` phase string
- Produces: `activePlace` resets to `null` when phase changes, so town hub shows after arrival

- [ ] **Step 1: Write the failing test for arrival routing**

Create `src/WildBunch.Web/src/tests/GameFlowRouter.test.tsx`:

```tsx
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { GameFlowRouter } from "../flow/GameFlowRouter";
import {
  AvailableActionKind,
  JourneyStatus,
  StartFlowPhase,
  type GameSessionDto,
  type JournalDto,
} from "../api/types";
import {
  acknowledgeTravelArrival,
  getAvailableActions,
  getGame,
  getJournal,
  previewTravel,
  travel,
  getWorldMap,
} from "../api/wildBunchApi";

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    constructor(config: unknown) { this.config = config; }
    destroy() {}
  }
  class Scene { constructor(_key?: string) {} }
  const Scale = { FIT: 0, CENTER_BOTH: 0 };
  return { default: { Game, Scene, Scale }, Game, Scene, Scale };
});

vi.mock("../api/wildBunchApi", () => ({
  getGame: vi.fn(),
  getAvailableActions: vi.fn(),
  getJournal: vi.fn(),
  previewTravel: vi.fn(),
  travel: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
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
}));

const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedAcknowledgeTravelArrival = vi.mocked(acknowledgeTravelArrival);
const mockedPreviewTravel = vi.mocked(previewTravel);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

const routeProfile = {
  trailId: "trail-1",
  risk: 1 as const,
  terrain: 0 as const,
  waterFeature: 0 as const,
  rideDayDistance: 3,
  mountedRideDayProgress: 1,
  footRideDayProgress: 0.5,
  warnings: [],
};

function createInTownSession(): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
    startFlowPhase: StartFlowPhase.GameStarted,
    player: { name: "Ruth", currentTownId: "t-town", health: 9 },
    world: {
      towns: [
        { id: "t-town", name: "Tumbleweed", services: 0 },
        { id: "dust-fork", name: "Dust Fork", services: 0 },
      ],
      trails: [
        { id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", risk: 1, terrain: 0, waterFeature: 0, rideDayDistance: 3 },
      ],
    },
    caseFile: {
      accusationId: null,
      openingLead: "The trail went cold outside town.",
      caseState: { statusText: "Still chasing leads." },
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

function createArrivalSession(): GameSessionDto {
  return {
    ...createInTownSession(),
    journey: {
      originTownId: "t-town",
      originTownName: "Tumbleweed",
      destinationTownId: "dust-fork",
      destinationTownName: "Dust Fork",
      travelMode: 1,
      status: JourneyStatus.Completed,
      mountedTravelAvailable: false,
      waterSecure: true,
      rideDayDistance: 3,
      remainingRideDayDistance: 0,
      baselineRideDays: 3,
      expectedDays: 3,
      remainingDays: 0,
      canteenChargesPerDay: 0,
      requiredCanteenCharges: 0,
      availableCanteenCharges: 0,
      canteenReserveCharges: 0,
      delayMarginDays: 0,
      delayRisk: false,
      requiredFood: 0,
      availableFood: 0,
      requiredHorseFeed: 0,
      availableHorseFeed: 0,
      horseState: null,
      daysTravelled: 3,
      delayDays: 0,
      pendingEncounter: null,
      warnings: [],
      routeProfile,
    },
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

function primeMocks() {
  mockedGetGame.mockResolvedValue(createInTownSession());
  mockedGetAvailableActions.mockResolvedValue([
    { kind: AvailableActionKind.Travel, label: "Hit the trail" },
  ]);
  mockedGetJournal.mockResolvedValue(createJournal());
  mockedPreviewTravel.mockResolvedValue({ success: false, message: "", preview: null });
  mockedAcknowledgeTravelArrival.mockResolvedValue({
    success: true,
    message: "You step into town.",
    currentSession: createInTownSession(),
    journeyStatus: null,
    journey: null,
    trailEvent: null,
    travelDiary: null,
  });
}

function renderRouter() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <GameFlowRouter />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

describe("GameFlowRouter arrival routing", () => {
  it("shows town hub after acknowledging arrival, not travel prep", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    const user = userEvent.setup();
    const { queryClient } = renderRouter();

    // Wait for the town hub to render (in-town phase, journey is null).
    const townHeading = await screen.findByRole("heading", { name: /tumbleweed/i });
    expect(townHeading).toBeInTheDocument();

    // Click "Hit the trail" to enter the travel prep surface.
    // This sets activePlace to "trailhead" inside GameFlowRouter.
    await user.click(screen.getByRole("button", { name: /hit the trail/i }));
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /hit the trail/i })).toBeInTheDocument();
    });

    // Simulate the journey completing: directly set the session to one
    // with a Completed journey. This triggers useGamePhase to return
    // "arrival", so GameFlowRouter renders ArrivalSurface.
    queryClient.setQueryData(["session", "game-1"], createArrivalSession());

    // Wait for the arrival surface to render.
    const arrivalHeading = await screen.findByRole("heading", { name: /you've arrived in dust fork/i });
    expect(arrivalHeading).toBeInTheDocument();

    // Click "Step into town" to acknowledge arrival.
    // The acknowledgeTravelArrival mutation fires; its onSuccess sets the
    // session back to the in-town session (journey: null) and invalidates
    // queries so getGame refetches and confirms the in-town state.
    await user.click(screen.getByRole("button", { name: /step into town/i }));

    // After acknowledgment, the phase returns to "in-town".
    // BUG (before fix): activePlace is still "trailhead", so
    //   TownHubSurface renders TravelPrepSurface (heading "Hit the trail")
    //   instead of the town hub (heading "Tumbleweed").
    // FIX (after fix): activePlace resets to null on phase change, so
    //   the town hub renders with the town name heading.
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /tumbleweed/i })).toBeInTheDocument();
    });
    expect(screen.queryByRole("heading", { name: /hit the trail/i })).not.toBeInTheDocument();
  });
});
```

This test fails before the fix because `activePlace` stays `"trailhead"` after the phase transitions from `arrival` back to `in-town`, causing `TownHubSurface` to render `TravelPrepSurface` (heading "Hit the trail") instead of the town hub (heading "Tumbleweed"). After the fix (`useEffect` resetting `activePlace` to `null` on phase change), the town hub renders correctly.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/WildBunch.Web && npx vitest run src/tests/GameFlowRouter.test.tsx`
Expected: FAIL — the town hub heading is not found because `activePlace` is still `"trailhead"` after arrival, so `TravelPrepSurface` renders instead.

- [ ] **Step 3: Reset activePlace on phase change**

In `src/WildBunch.Web/src/flow/GameFlowRouter.tsx`, add a `useEffect` that resets `activePlace` to `null` whenever `phase` changes. Add it after the existing `useEffect` block (after line 34):

```tsx
useEffect(() => {
  setActivePlace(null);
}, [phase]);
```

This ensures that whenever the game phase transitions (e.g., `in-town` → `on-trail` → `arrival` → `in-town`), the place state resets. When the player returns to `in-town` after arrival, `activePlace` is `null` and the town hub renders.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/WildBunch.Web && npx vitest run src/tests/GameFlowRouter.test.tsx`
Expected: PASS

- [ ] **Step 5: Run full test suite to check for regressions**

Run: `cd src/WildBunch.Web && npx vitest run`
Expected: PASS — no regressions in existing tests. Watch for `AppShell.test.tsx` and `StartFlow.test.tsx` which may exercise the flow router.

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Web/src/flow/GameFlowRouter.tsx src/WildBunch.Web/src/tests/GameFlowRouter.test.tsx
git commit -m "fix: reset town hub place state on game phase change

After travel arrival acknowledgment, activePlace was still 'trailhead'
from the pre-travel selection, so TravelPrepSurface rendered instead
of the town hub. Reset activePlace to null on phase change."
```

---

### Task 3: Add world-map API endpoint

**Files:**
- Modify: `src/WildBunch.Api/Games/GameSessionEndpoints.cs:38-41` — add `world-map` route aliasing `GetStartingTownMapHandler`
- Modify: `src/WildBunch.Web/src/api/wildBunchApi.ts` — add `getWorldMap` function
- Test: `tests/WildBunch.Integration.Tests/WorldMapEndpointTests.cs` — new file (endpoint-level integration test)
- Test: `tests/WildBunch.Application.Tests/GetWorldMapHandlerTests.cs` — new file (handler contract test)

**Interfaces:**
- Consumes: `GetStartingTownMapHandler` (existing) — returns `StartingTownMapDto`
- Produces: `GET /api/games/{id}/world-map` endpoint returning `StartingTownMapDto`; `getWorldMap(sessionId)` frontend function

- [ ] **Step 1: Write the failing integration test for the world-map endpoint**

Create `tests/WildBunch.Integration.Tests/WorldMapEndpointTests.cs`. This test exercises the actual HTTP route — it fails before the endpoint exists (404 NotFound for the route itself) and passes after the endpoint is added. Follow the pattern from `StartingTownMapEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class WorldMapEndpointTests
{
    [Fact]
    public async Task GetWorldMapReturnsOkWithTownsAndTrails()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/games/{sessionId}/world-map");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var map = await response.Content.ReadFromJsonAsync<StartingTownMapDto>();
        Assert.NotNull(map);
        Assert.NotEmpty(map!.Towns);
        Assert.NotEmpty(map.Trails);
    }

    [Fact]
    public async Task GetWorldMapReturnsSameShapeAsStartingTownMap()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var sessionId = await CreateSessionAsync(client);

        var worldMapResponse = await client.GetAsync($"/api/games/{sessionId}/world-map");
        var startingTownMapResponse = await client.GetAsync($"/api/games/{sessionId}/starting-town-map");

        Assert.Equal(HttpStatusCode.OK, worldMapResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, startingTownMapResponse.StatusCode);

        var worldMap = await worldMapResponse.Content.ReadFromJsonAsync<StartingTownMapDto>();
        var startingTownMap = await startingTownMapResponse.Content.ReadFromJsonAsync<StartingTownMapDto>();

        Assert.NotNull(worldMap);
        Assert.NotNull(startingTownMap);
        Assert.Equal(startingTownMap!.Towns.Count, worldMap!.Towns.Count);
        Assert.Equal(startingTownMap.Trails.Count, worldMap.Trails.Count);
    }

    [Fact]
    public async Task GetWorldMapReturnsNotFoundForMissingSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}/world-map");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<Guid> CreateSessionAsync(HttpClient client)
    {
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var response = await client.PostAsJsonAsync("/api/games/setup", scenario.CreateRequest("Ranger Vale"));
        var session = await response.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(session);
        return session!.Id;
    }
}
```

Also create `tests/WildBunch.Application.Tests/GetWorldMapHandlerTests.cs` as a handler-level contract test:

```csharp
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;
using WildBunch.GameContent.NewGame;
using DomainGameDifficulty = WildBunch.Domain.Travel.GameDifficulty;

namespace WildBunch.Application.Tests;

public sealed class GetWorldMapHandlerTests
{
    [Fact]
    public async Task ReturnsAllSeededTownsAndTrails()
    {
        var (handler, sessionId) = CreateHandlerWithSession();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery(sessionId));
        Assert.Equal(8, result.Towns.Count);
        Assert.Equal(14, result.Trails.Count);
    }

    [Fact]
    public async Task ThrowsForMissingSession()
    {
        var repo = new InMemoryGameSessionRepository();
        var handler = new GetStartingTownMapHandler(repo);
        await Assert.ThrowsAsync<GameSessionNotFoundException>(() =>
            handler.HandleAsync(new GetStartingTownMapQuery(Guid.NewGuid())));
    }

    private static (GetStartingTownMapHandler Handler, Guid SessionId) CreateHandlerWithSession()
    {
        var repo = new InMemoryGameSessionRepository();
        var session = CreateTestSession();
        repo.Seed(session);
        return (new GetStartingTownMapHandler(repo), session.Id.Value);
    }

    private static GameSession CreateTestSession()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var difficulty = DifficultyEnvelope.For(DomainGameDifficulty.Standard);
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());
        return factory.Create(
            "Test Player",
            difficulty.Difficulty,
            seedWorld.SeedCode.ToString("D"),
            GameEntropy.Boring);
    }

    private sealed class TestFixedSaltSourceFactory : ISaltSourceFactory
    {
        public SaltSource Create(string? setupSeedCode, DomainGameDifficulty gameDifficulty)
            => SaltSource.CreateFixed("test-fixed-salt");
    }
}
```

- [ ] **Step 2: Run integration test to verify it fails (endpoint does not exist yet)**

Run: `.\scripts\postgres-dev.ps1 test -- dotnet test tests/WildBunch.Integration.Tests --filter WorldMapEndpointTests`
Expected: FAIL — `GetWorldMapReturnsOkWithTownsAndTrails` gets 404 NotFound because the `/api/games/{id}/world-map` route does not exist yet. `GetWorldMapReturnsNotFoundForMissingSession` may pass trivially (any unknown route returns 404), but `GetWorldMapReturnsOkWithTownsAndTrails` and `GetWorldMapReturnsSameShapeAsStartingTownMap` fail because the route is not mapped.

The handler-level test (`GetWorldMapHandlerTests`) will pass because the handler already exists — that is expected. The handler test guards the contract; the integration test guards the route.

- [ ] **Step 3: Add the world-map endpoint route**

In `src/WildBunch.Api/Games/GameSessionEndpoints.cs`, after the `starting-town-map` route (after line 41), add:

```csharp
games.MapGet("{id:guid}/world-map", GetWorldMapAsync)
    .WithName("GetWorldMap")
    .Produces<StartingTownMapDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);
```

Add the handler method (after `GetStartingTownMapAsync`, around line 157):

```csharp
private static async Task<IResult> GetWorldMapAsync(
    Guid id,
    GetStartingTownMapHandler handler,
    CancellationToken cancellationToken)
{
    try
    {
        var map = await handler.HandleAsync(new GetStartingTownMapQuery(id), cancellationToken);
        return Results.Ok(map);
    }
    catch (GameSessionNotFoundException)
    {
        return Results.NotFound();
    }
}
```

- [ ] **Step 4: Add getWorldMap to the frontend API client**

In `src/WildBunch.Web/src/api/wildBunchApi.ts`, add `StartingTownMapDto` is already imported. Add after `getStartingTownMap` (after line 187):

```ts
export function getWorldMap(sessionId: string) {
  return requestJson<StartingTownMapDto>(`/api/games/${sessionId}/world-map`);
}
```

- [ ] **Step 5: Build and run backend tests**

Run: `dotnet build`
Expected: PASS

Run: `dotnet test tests/WildBunch.Application.Tests --filter GetWorldMapHandlerTests`
Expected: PASS — handler contract test passes.

- [ ] **Step 6: Run integration test to verify the endpoint now works**

Run: `.\scripts\postgres-dev.ps1 test -- dotnet test tests/WildBunch.Integration.Tests --filter WorldMapEndpointTests`
Expected: PASS — `GetWorldMapReturnsOkWithTownsAndTrails` gets 200 OK, `GetWorldMapReturnsSameShapeAsStartingTownMap` confirms the world-map and starting-town-map endpoints return the same shape, and `GetWorldMapReturnsNotFoundForMissingSession` gets 404 for a random GUID.

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Api/Games/GameSessionEndpoints.cs src/WildBunch.Web/src/api/wildBunchApi.ts tests/WildBunch.Application.Tests/GetWorldMapHandlerTests.cs tests/WildBunch.Integration.Tests/WorldMapEndpointTests.cs
git commit -m "feat: add GET /api/games/{id}/world-map endpoint aliasing starting-town-map

The world-map route returns the same StartingTownMapDto (towns with
coordinates + trails with ride-day distances) via the existing
GetStartingTownMapHandler. This provides a semantically named endpoint
for the travel flow's map selection. Integration test proves the route
returns 200 OK with map data and 404 for missing sessions."
```

---

### Task 4: Generalize PhaserMapHost for travel selection

**Files:**
- Modify: `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx` — add optional `currentTownId` and `selectableTownIds` props to restrict interactivity and highlight origin
- Test: `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx` — add tests for travel-mode behavior

**Interfaces:**
- Consumes: `StartingTownMapDto` (map data), optional `currentTownId` (travel origin), optional `selectableTownIds` (connected destinations)
- Produces: `onTownSelected` callback only fires for selectable towns; current town is visually distinct; non-selectable towns are visible but not interactive

- [ ] **Step 1: Write the failing tests for travel-mode scene behavior**

Add to `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`, in a new describe block:

```tsx
describe("PhaserMapHost travel mode", () => {
  it("only emits onTownSelected for selectable towns when selectableTownIds is provided", () => {
    const onTownSelected = vi.fn();
    render(
      <PhaserMapHost
        mapData={createMapData()}
        selectedTownId={null}
        onTownSelected={onTownSelected}
        currentTownId="t-town"
        selectableTownIds={["dust-fork"]}
      />,
    );

    const scene = mockState.games[0].config.scene;
    scene.selectTown("dust-fork");
    expect(onTownSelected).toHaveBeenCalledWith("dust-fork");

    scene.selectTown("t-town");
    expect(onTownSelected).not.toHaveBeenCalledWith("t-town");
  });

  it("makes all towns selectable when selectableTownIds is not provided", () => {
    const onTownSelected = vi.fn();
    renderHost({ onTownSelected });

    const scene = mockState.games[0].config.scene;
    scene.selectTown("t-town");
    expect(onTownSelected).toHaveBeenCalledWith("t-town");
  });
});
```

Note: The `selectTown` method on the scene currently calls `onTownSelected` for any town in `mapData.towns`. The test expects that when `selectableTownIds` is provided, `selectTown` only calls `onTownSelected` for towns in that list. This requires the scene to filter.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/WildBunch.Web && npx vitest run src/tests/PhaserMapHost.test.tsx`
Expected: FAIL — `selectTown("t-town")` still calls `onTownSelected` because the scene does not filter by `selectableTownIds`.

- [ ] **Step 3: Extend PhaserMapHost props and scene logic**

In `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`, extend the props interface:

```tsx
interface PhaserMapHostProps {
  mapData: StartingTownMapDto;
  selectedTownId: string | null;
  onTownSelected: (townId: string) => void;
  currentTownId?: string | null;
  selectableTownIds?: string[] | null;
}
```

Extend `StartingTownMapScene` to accept and use the new fields:

```tsx
export class StartingTownMapScene extends Phaser.Scene {
  private readonly mapData: StartingTownMapDto;
  public readonly selectedTownId: string | null;
  private readonly onTownSelected: (townId: string) => void;
  private readonly currentTownId: string | null;
  private readonly selectableTownIds: Set<string> | null;

  constructor(
    mapData: StartingTownMapDto,
    selectedTownId: string | null,
    onTownSelected: (townId: string) => void,
    currentTownId: string | null = null,
    selectableTownIds: string[] | null = null,
  ) {
    super("starting-town-map");
    this.mapData = mapData;
    this.selectedTownId = selectedTownId;
    this.onTownSelected = onTownSelected;
    this.currentTownId = currentTownId;
    this.selectableTownIds = selectableTownIds ? new Set(selectableTownIds) : null;
  }

  selectTown(townId: string): void {
    if (this.selectableTownIds && !this.selectableTownIds.has(townId)) {
      return;
    }
    const town = this.mapData.towns.find((t) => t.id === townId);
    if (town) {
      this.onTownSelected(townId);
    }
  }
```

In the `create()` method, update the town rendering loop to handle the current town and non-selectable towns differently. Replace the town rendering block (lines 90-117) with:

```tsx
for (const town of this.mapData.towns) {
  const x = toScreenX(town.x);
  const y = toScreenY(town.y);
  const isSelected = this.selectedTownId === town.id;
  const isCurrent = this.currentTownId === town.id;
  const isSelectable = !this.selectableTownIds || this.selectableTownIds.has(town.id);
  const radius = 14;

  let fillColor = 0xc9a84c;
  if (isCurrent) {
    fillColor = 0x8b6914;
  } else if (!isSelectable) {
    fillColor = 0x9a9a8a;
  }

  const circle = this.add.circle(x, y, radius, fillColor);

  if (isSelected) {
    circle.setStrokeStyle(4, 0xf0e6d2);
  } else if (isCurrent) {
    circle.setStrokeStyle(3, 0xf0e6d2);
  } else {
    circle.setStrokeStyle(2, 0x000000);
  }

  if (isSelectable && !isCurrent) {
    circle.setInteractive({ useHandCursor: true });
    circle.on("pointerover", () => circle.setScale(1.25));
    circle.on("pointerout", () => circle.setScale(1));
    circle.on("pointerdown", () => this.selectTown(town.id));
  }

  this.add
    .text(x, y + radius + 16, town.name, {
      fontSize: "13px",
      color: "#1a1a1a",
      backgroundColor: "rgba(168, 200, 144, 0.85)",
      padding: { x: 2, y: 1 },
    })
    .setOrigin(0.5);
}
```

Update the `PhaserMapHost` function to pass the new props to the scene:

```tsx
export function PhaserMapHost({ mapData, selectedTownId, onTownSelected, currentTownId, selectableTownIds }: PhaserMapHostProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const onTownSelectedRef = useRef(onTownSelected);
  onTownSelectedRef.current = onTownSelected;

  useEffect(() => {
    if (!containerRef.current) return;

    const scene = new StartingTownMapScene(
      mapData,
      selectedTownId,
      (townId: string) => onTownSelectedRef.current(townId),
      currentTownId ?? null,
      selectableTownIds ?? null,
    );

    const game = new Phaser.Game({
      parent: containerRef.current,
      width: 800,
      height: 500,
      backgroundColor: "#a8c890",
      scene: scene,
      scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
      },
    });

    return () => {
      game.destroy(true);
    };
  }, [mapData, selectedTownId, currentTownId, selectableTownIds]);

  return (
    <MapCanvas
      ref={containerRef}
      role="img"
      aria-label="Trail map of starting towns"
    />
  );
}
```

Note: Update the `aria-label` to be context-appropriate. For travel mode, "Trail map" is still accurate. Keep the existing label for backward compatibility with `StartingTownStep.test.tsx` which asserts `name: /trail map of starting towns/i`. If a different label is needed for travel, make it a prop. For now, keep the shared label since both surfaces show a trail map.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd src/WildBunch.Web && npx vitest run src/tests/PhaserMapHost.test.tsx`
Expected: PASS — both the existing starting-town tests and the new travel-mode tests pass.

- [ ] **Step 5: Run full test suite to check for regressions**

Run: `cd src/WildBunch.Web && npx vitest run`
Expected: PASS — `StartingTownStep.test.tsx` still passes because `selectableTownIds` defaults to `null` (all towns selectable).

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx
git commit -m "feat: generalize PhaserMapHost for travel destination selection

Add optional currentTownId and selectableTownIds props. When
selectableTownIds is provided, only those towns are interactive and
the current town is highlighted as the origin. Defaults preserve
existing starting-town selection behavior."
```

---

### Task 5: Integrate visual map into TravelPrepSurface

**Files:**
- Modify: `src/WildBunch.Web/src/flow/TravelPrepSurface.tsx` — replace text destination list with PhaserMapHost, keep the prep/confirmation screen
- Test: `src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx` — extend with map integration tests

**Interfaces:**
- Consumes: `getWorldMap` (from Task 3), `PhaserMapHost` with travel props (from Task 4), `previewTravel` (existing), `connectedDestinations` logic (existing)
- Produces: visual map for destination selection; clicking a connected town enters the prep/confirmation screen; non-connected towns are visible but not clickable

- [ ] **Step 1: Write the failing test for map-based destination selection**

Add to `src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx`:

```tsx
describe("TravelPrepSurface map integration", () => {
  it("renders the Phaser map for destination selection", async () => {
    const session = createSession();
    mockedPreviewTravel.mockResolvedValue(createPreview());

    render(
      <TravelPrepSurface onBack={vi.fn()} session={session} gameId={session.id} loading={false} handleTravel={vi.fn()} notice="" error="" />,
    );

    expect(await screen.findByRole("img", { name: /trail map/i })).toBeInTheDocument();
  });

  it("does not render the old text destination list", async () => {
    const session = createSession();
    mockedPreviewTravel.mockResolvedValue(createPreview());

    render(
      <TravelPrepSurface onBack={vi.fn()} session={session} gameId={session.id} loading={false} handleTravel={vi.fn()} notice="" error="" />,
    );

    // The old text list showed "Click to check the ride" on each card
    await waitFor(() => {
      expect(screen.queryByText(/click to check the ride/i)).not.toBeInTheDocument();
    });
  });
});
```

Note: These tests assume `TravelPrepSurface` is refactored to accept props directly instead of pulling from `useGameSession()`. If the component keeps using `useGameSession()`, use `renderInSessionProvider` and mock the API functions instead. Match the pattern from `tests/StartFlow.test.tsx`.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/WildBunch.Web && npx vitest run src/tests/TravelPrepSurface.test.tsx`
Expected: FAIL — the map (`role="img"`) is not rendered; the old text list is still present.

- [ ] **Step 3: Replace the text destination list with PhaserMapHost**

In `src/WildBunch.Web/src/flow/TravelPrepSurface.tsx`, replace the destination selection screen (the second `return` block, lines 233-272) with a map-based view.

Add imports at the top:

```tsx
import { useQuery } from "@tanstack/react-query";
import { getWorldMap } from "../api/wildBunchApi";
import { PhaserMapHost } from "../components/start-flow/PhaserMapHost";
```

Replace the destination selection return block with:

```tsx
  // Destination selection screen — visual map
  const currentTownId = session.player.currentTownId;
  const selectableTownIds = destinations.map((d) => d.town.id);

  return (
    <FlowSurface $variant="travel-prep">
      <PlaceHeader>
        <BackButton type="button" onClick={onBack}>
          ← Back to town
        </BackButton>
        <h1>Hit the trail</h1>
      </PlaceHeader>
      <TravelPrepBody>
        <Stack>
          {destinations.length > 0 ? (
            <TravelMapSelection
              gameId={gameId}
              currentTownId={currentTownId}
              selectableTownIds={selectableTownIds}
              selectedDestId={selectedDestId}
              onSelectDestination={(townId) => setSelectedDestId(townId)}
            />
          ) : (
            <Muted>No trails lead out of this town.</Muted>
          )}
        </Stack>
      </TravelPrepBody>
    </FlowSurface>
  );
```

Add the `TravelMapSelection` component in the same file (or inline if small enough). It fetches the world map and renders `PhaserMapHost`:

```tsx
function TravelMapSelection({
  gameId,
  currentTownId,
  selectableTownIds,
  selectedDestId,
  onSelectDestination,
}: {
  gameId: string | null;
  currentTownId: string;
  selectableTownIds: string[];
  selectedDestId: string | null;
  onSelectDestination: (townId: string) => void;
}) {
  const mapQuery = useQuery({
    queryKey: ["world-map", gameId],
    queryFn: () => getWorldMap(gameId as string),
    enabled: Boolean(gameId),
    staleTime: Infinity,
    retry: false,
  });

  const mapData = mapQuery.data ?? null;

  if (mapQuery.isLoading || !mapData) {
    return <Muted>Unfolding the map…</Muted>;
  }

  return (
    <PhaserMapHost
      mapData={mapData}
      selectedTownId={selectedDestId}
      onTownSelected={onSelectDestination}
      currentTownId={currentTownId}
      selectableTownIds={selectableTownIds}
    />
  );
}
```

Remove the now-unused styled components: `DestinationCard`, `RouteDetails`, `RoutePreview`, `RouteMeta`. Keep `TravelPrepRide`, `TravelPrepActions`, `TravelPrepBody` which are used by the prep/confirmation screen.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/WildBunch.Web && npx vitest run src/tests/TravelPrepSurface.test.tsx`
Expected: PASS

- [ ] **Step 5: Run full frontend test suite**

Run: `cd src/WildBunch.Web && npx vitest run`
Expected: PASS — no regressions. Check `TravelRoutesPanel.test.tsx` (the cockpit panel, which is separate from the play surface `TravelPrepSurface`).

- [ ] **Step 6: Run lint/typecheck**

Run: `cd src/WildBunch.Web && npx tsc --noEmit`
Expected: PASS — no unused imports or type errors.

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Web/src/flow/TravelPrepSurface.tsx src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx
git commit -m "feat: replace text destination list with visual Phaser map in travel prep

TravelPrepSurface now fetches the world map via getWorldMap and renders
PhaserMapHost with currentTownId and selectableTownIds. Connected
destinations are clickable; the current town is highlighted as origin;
non-connected towns are visible but not interactive."
```

---

### Task 6: Full validation and index mesh refresh

**Files:**
- No new files — validation only

- [ ] **Step 1: Run dotnet build**

Run: `dotnet build`
Expected: PASS

- [ ] **Step 2: Run dotnet test**

Run: `dotnet test`
Expected: PASS

- [ ] **Step 3: Run frontend tests**

Run: `cd src/WildBunch.Web && npx vitest run`
Expected: PASS

- [ ] **Step 4: Run frontend typecheck**

Run: `cd src/WildBunch.Web && npx tsc --noEmit`
Expected: PASS

- [ ] **Step 5: Regenerate index mesh**

Run: `python scripts/generate_index_mesh.py`
Expected: If any INDEX.md files changed (new test files, new plan file), commit the updated indexes.

- [ ] **Step 6: Commit index mesh updates if any**

```bash
git add -A
git status
# If INDEX.md files changed:
git commit -m "chore: regenerate index mesh for BUNCH-115 changes"
```

- [ ] **Step 7: Browser smoke playtest (required — BUNCH-115 is a travel-flow UI issue)**

BUNCH-115 is a travel-flow UI issue and the Linear validation explicitly asks for manual playtest evidence. This step is **required**, not optional. The closeout is not GREEN without it.

Start the dev server (`cd src/WildBunch.Web && npm run dev` or the repo-local dev command) and capture browser screenshots under `.agents/superpowers/output/screenshots/` proving all three fixes:

1. **Mounted travel displays as "on horseback"** — Start a game with a horse (mounted travel available). Go to travel prep, select a connected destination. Verify the prep text says "on horseback" (not "on foot"). Screenshot the prep screen showing the correct travel-mode text.

2. **Travel map renders and connected towns can be selected** — On the travel prep destination selection screen, verify the visual Phaser map renders with the current town highlighted as origin. Click a connected destination — verify it is selectable and the prep screen appears. Screenshot the map showing the current town and connected destinations.

3. **After arrival acknowledgement, the town hub renders** — Start the ride, advance travel days until arrival. Click "Step into town". Verify the town hub renders (town name heading + place cards), not the travel prep surface ("Hit the trail" heading). Screenshot the town hub after arrival.

Store screenshots under `.agents/superpowers/output/screenshots/bunch-115/` with descriptive filenames (e.g. `01-mounted-travel-mode.png`, `02-travel-map-selection.png`, `03-town-hub-after-arrival.png`). These are git-ignored and cited in the PR closeout, not committed to the repo.

---

## Self-Review

**1. Spec coverage:**
- Bug 1 (travel mode display): Task 1 fixes the inverted ternary and adds a `TravelMode` const. ✓
- Bug 2 (visual map for travel): Task 3 adds the `world-map` endpoint (with integration test proving the route returns 200 + 404), Task 4 generalizes `PhaserMapHost`, Task 5 integrates the map into `TravelPrepSurface`. ✓
- Bug 3 (arrival routing): Task 2 resets `activePlace` on phase change. ✓
- Issue validation: `npm test` covered by Task 6 Step 3; browser smoke playtest required by Task 6 Step 7 with three specific smoke checks (mounted travel text, map rendering + selection, town hub after arrival). ✓

**2. Placeholder scan:** No "TBD", "TODO", "implement later", or placeholder assertions in the plan. All test code blocks contain real, executable assertions:
- Task 2 `GameFlowRouter.test.tsx`: renders through `GameSessionProvider`, clicks "Hit the trail" to set `activePlace = "trailhead"`, uses `queryClient.setQueryData` to transition to arrival phase, clicks "Step into town", and asserts `screen.getByRole("heading", { name: /tumbleweed/i })` is present while `screen.queryByRole("heading", { name: /hit the trail/i })` is absent. This test fails before the `activePlace` reset fix and passes after. ✓
- Task 3 `WorldMapEndpointTests.cs`: integration test hitting `GET /api/games/{id}/world-map` via `HttpClient`, asserting 200 OK with map data and 404 for missing sessions. Fails before the route exists, passes after. ✓
- Task 3 `GetWorldMapHandlerTests.cs`: handler-level contract test (passes before endpoint exists — guards the handler contract, not the route). ✓

**3. Type consistency:**
- `TravelMode` const: `Mounted: 0, Foot: 1` — matches C# enum. ✓
- `getWorldMap` returns `StartingTownMapDto` — matches the endpoint and `PhaserMapHost` prop type. ✓
- `PhaserMapHost` props: `currentTownId?: string | null`, `selectableTownIds?: string[] | null` — consistent across Task 4 and Task 5. ✓
- `TravelMapSelection` passes `selectedDestId` as `selectedTownId` to `PhaserMapHost` — consistent with the existing prop name. ✓
