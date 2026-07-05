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
    routeTree: rootRoute.addChildren([indexRoute, townRoute, storeRoute, trailRoute]),
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
