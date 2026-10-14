import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  RouterProvider,
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
} from "@tanstack/react-router";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { useGameSession } from "../state/useGameSession";
import { gameSessionQueryKey } from "../state/gameSessionQuery";
import { TravelPrepSurface } from "../flow/TravelPrepSurface";
import { WorldMapScene } from "../components/start-flow/PhaserMapHost";
import {
  AvailableActionKind,
  StartFlowPhase,
  type GameSessionDto,
  type GameTurnResultDto,
  type JournalDto,
  type TravelPreviewResultDto,
} from "../api/types";
import {
  getAvailableActions,
  getGame,
  getJournal,
  getWorldMap,
  previewTravel,
  travel,
} from "../api/wildBunchApi";

const mockState = vi.hoisted(() => ({
  games: [] as Array<{
    config: { scene: WorldMapScene };
    destroyed: boolean;
    destroy: () => void;
  }>,
}));

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    public destroyed = false;
    constructor(config: unknown) {
      this.config = config;
      mockState.games.push(this as never);
    }
    destroy() {
      this.destroyed = true;
    }
  }
  class Scene {
    constructor(_key?: string) {}
  }
  const Scale = { FIT: 0, CENTER_BOTH: 0 };
  return { default: { Game, Scene, Scale }, Game, Scene, Scale };
});

vi.mock("../api/wildBunchApi", () => ({
  getGame: vi.fn(),
  getAvailableActions: vi.fn(),
  getJournal: vi.fn(),
  previewTravel: vi.fn(),
  travel: vi.fn(),
  getWorldMap: vi.fn(),
  setupGame: vi.fn(),
  markPrologueViewed: vi.fn(),
  startGameWithTown: vi.fn(),
  advanceTravelDay: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
  archiveGame: vi.fn(),
  getTownStoreOffers: vi.fn(),
  buyStoreItem: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  lookAroundSaloon: vi.fn(),
  readWantedPosters: vi.fn(),
  gatherLocalGossip: vi.fn(),
  getPrologue: vi.fn(),
}));

const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedPreviewTravel = vi.mocked(previewTravel);
const mockedGetWorldMap = vi.mocked(getWorldMap);
const mockedTravel = vi.mocked(travel);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
  mockState.games.length = 0;
});

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  const promise = new Promise<T>((resolvePromise) => {
    resolve = resolvePromise;
  });

  return { promise, resolve };
}

function createSession(overrides: Partial<GameSessionDto> = {}): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
    startFlowPhase: StartFlowPhase.GameStarted,
    player: { name: "Ruth", currentTownId: "t-town", health: 9 },
    world: {
      towns: [
        { id: "t-town", name: "Tumbleweed" },
        { id: "dust-fork", name: "Dust Fork" },
        { id: "agua-fria", name: "Agua Fria" },
      ],
      trails: [
        {
          id: "trail-1",
          fromTownId: "t-town",
          toTownId: "dust-fork",
          risk: 1,
          terrain: 0,
          waterFeature: 1,
          rideDayDistance: 3,
        },
        {
          id: "trail-2",
          fromTownId: "t-town",
          toTownId: "agua-fria",
          risk: 1,
          terrain: 0,
          waterFeature: 1,
          rideDayDistance: 4,
        },
      ],
    },
    caseFile: {
      accusationId: null,
      openingLead: "",
      caseState: { statusText: "" },
      discoveredSuspects: [],
      caseBoard: { warrants: [], clues: [] },
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

function createJournal(): JournalDto {
  return {
    id: "game-1",
    status: 0,
    clock: { day: 1, turn: 0, timeOfDay: "Morning" },
    currentTown: { id: "t-town", name: "Tumbleweed" },
    caseFile: {
      accusationId: null,
      openingLead: "",
      caseState: { statusText: "" },
      caseSummary: "",
      discoveredSuspects: [],
      caseBoard: { warrants: [], clues: [] },
      knownClues: [],
      knownWarrants: [],
      wantedPosters: [],
    },
    logEntries: [],
  };
}

function createPreview(
  overrides: Partial<TravelPreviewResultDto["preview"]> = {},
): TravelPreviewResultDto {
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
      horseState: {
        hunger: 0,
        thirst: 0,
        exhaustion: 0,
        isLame: false,
        isDead: false,
        canProvideMountedTravel: true,
      },
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

function primeMocks(session: GameSessionDto = createSession()) {
  mockedGetGame.mockResolvedValue(session);
  mockedGetAvailableActions.mockResolvedValue([
    { kind: AvailableActionKind.Travel, label: "Hit the trail" },
  ]);
  mockedGetJournal.mockResolvedValue(createJournal());
  mockedGetWorldMap.mockResolvedValue({
    towns: [
      { id: "t-town", name: "Tumbleweed", x: 150, y: 500 },
      { id: "dust-fork", name: "Dust Fork", x: 450, y: 400 },
      { id: "agua-fria", name: "Agua Fria", x: 750, y: 400 },
    ],
    trails: [
      { id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", rideDayDistance: 3 },
      { id: "trail-2", fromTownId: "t-town", toTownId: "agua-fria", rideDayDistance: 4 },
    ],
  });
}

function renderPrep() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  const rootRoute = createRootRoute({ component: () => <Outlet /> });
  const trailheadRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/town/trailhead",
    component: () => <TravelPrepSurface />,
  });
  const awayRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/town",
    component: AwayFromTravel,
  });
  const router = createRouter({ routeTree: rootRoute.addChildren([trailheadRoute, awayRoute]) });
  window.history.replaceState({}, "", "/town/trailhead");
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <RouterProvider router={router} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient, router };
}

function AwayFromTravel() {
  const { notice } = useGameSession();
  return (
    <>
      <button type="button">Away from travel</button>
      {notice ? <p>{notice}</p> : null}
    </>
  );
}

describe("TravelPrepSurface travel-mode display", () => {
  it("shows 'on horseback' when travelMode is Mounted (0)", async () => {
    primeMocks();
    mockedPreviewTravel.mockResolvedValue(createPreview({ travelMode: 0 }));
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    renderPrep();

    // TravelPrepSurface renders the destination selection screen with a Phaser map.
    // Select a destination through the scene to enter the prep/confirmation screen.
    await waitFor(() => {
      expect(mockState.games.length).toBeGreaterThan(0);
    });
    const scene = mockState.games[0].config.scene;
    scene.selectTown("dust-fork");

    await waitFor(() => {
      expect(screen.getByText(/on horseback/i)).toBeInTheDocument();
    });
    expect(screen.queryByText(/on foot/i)).not.toBeInTheDocument();
  });

  it("shows 'on foot' when travelMode is Foot (1)", async () => {
    primeMocks();
    mockedPreviewTravel.mockResolvedValue(createPreview({ travelMode: 1 }));
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    renderPrep();

    await waitFor(() => {
      expect(mockState.games.length).toBeGreaterThan(0);
    });
    const scene = mockState.games[0].config.scene;
    scene.selectTown("dust-fork");

    await waitFor(() => {
      expect(screen.getByText(/on foot/i)).toBeInTheDocument();
    });
    expect(screen.queryByText(/on horseback/i)).not.toBeInTheDocument();
  });
});

describe("TravelPrepSurface preview identity", () => {
  it("does not send a late departure result to a route that did not issue it", async () => {
    primeMocks();
    const user = userEvent.setup();
    const pendingTravel = deferred<GameTurnResultDto>();
    const domainResult: GameTurnResultDto = {
      success: false,
      message: "That route is no longer open.",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
    };
    mockedPreviewTravel.mockResolvedValue(createPreview());
    mockedTravel.mockReturnValue(pendingTravel.promise);
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    const { queryClient, router } = renderPrep();

    await waitFor(() => expect(mockState.games.length).toBeGreaterThan(0));
    await waitFor(() =>
      expect(queryClient.getQueryState(gameSessionQueryKey("game-1"))?.data).toBeDefined(),
    );
    mockState.games[0].config.scene.selectTown("dust-fork");
    const startRideButton = await screen.findByRole("button", { name: /start the ride/i });
    await waitFor(() => expect(startRideButton).toBeEnabled());
    await user.click(startRideButton);
    await waitFor(() => expect(mockedTravel).toHaveBeenCalledWith("game-1", "dust-fork"));
    await router.navigate({ to: "/town" });
    expect(await screen.findByRole("button", { name: "Away from travel" })).toBeInTheDocument();
    const completedFetchCount = mockedGetGame.mock.calls.length;
    pendingTravel.resolve(domainResult);

    await waitFor(() => expect(mockedGetGame).toHaveBeenCalledTimes(completedFetchCount + 1));
    expect(screen.queryByText(domainResult.message)).not.toBeInTheDocument();
  });

  it("shows a departure request failure without changing the session", async () => {
    primeMocks();
    const user = userEvent.setup();
    mockedPreviewTravel.mockResolvedValue(createPreview());
    mockedTravel.mockRejectedValue(new Error("The trail request could not be completed."));
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    const { queryClient } = renderPrep();

    await waitFor(() => expect(mockState.games.length).toBeGreaterThan(0));
    mockState.games[0].config.scene.selectTown("dust-fork");
    const startRideButton = await screen.findByRole("button", { name: /start the ride/i });
    await waitFor(() => expect(startRideButton).toBeEnabled());
    await user.click(startRideButton);

    expect(
      await screen.findByText("The trail request could not be completed."),
    ).toBeInTheDocument();
    expect(queryClient.getQueryData(gameSessionQueryKey("game-1"))).toEqual(createSession());
    expect(mockedTravel).toHaveBeenCalledWith("game-1", "dust-fork");
  });

  it("fetches a fresh preview when the active session changes for the selected destination", async () => {
    primeMocks();
    const firstPreview = deferred<TravelPreviewResultDto>();
    const refreshedPreview = deferred<TravelPreviewResultDto>();
    mockedPreviewTravel
      .mockReturnValueOnce(firstPreview.promise)
      .mockReturnValueOnce(refreshedPreview.promise);
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    const { queryClient } = renderPrep();

    await waitFor(() => {
      expect(mockState.games.length).toBeGreaterThan(0);
    });
    mockState.games[0].config.scene.selectTown("dust-fork");
    await waitFor(() => {
      expect(mockedPreviewTravel).toHaveBeenCalledTimes(1);
    });
    firstPreview.resolve(createPreview({ baselineRideDays: 2 }));

    expect(await screen.findByText(/2-day ride/i)).toBeInTheDocument();

    const sessionKey = gameSessionQueryKey("game-1");
    const updatedAt = queryClient.getQueryState(sessionKey)?.dataUpdatedAt ?? Date.now();
    queryClient.setQueryData(
      sessionKey,
      createSession({ clock: { day: 2, turn: 0, timeOfDay: "Morning" } }),
      { updatedAt: updatedAt + 1000 },
    );

    await waitFor(() => {
      expect(mockedPreviewTravel).toHaveBeenCalledTimes(2);
    });
    expect(screen.queryByText(/2-day ride/i)).not.toBeInTheDocument();
    expect(screen.getByText("Calculating the ride...")).toBeInTheDocument();
    refreshedPreview.resolve(createPreview({ baselineRideDays: 1 }));
    expect(await screen.findByText(/1-day ride/i)).toBeInTheDocument();
  });

  it("does not restore an old origin's preview after the player changes towns", async () => {
    primeMocks();
    const loadingPreview = deferred<TravelPreviewResultDto>();
    mockedPreviewTravel.mockReturnValue(loadingPreview.promise);
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    const { queryClient } = renderPrep();

    await waitFor(() => {
      expect(mockState.games.length).toBeGreaterThan(0);
    });
    mockState.games[0].config.scene.selectTown("dust-fork");
    await waitFor(() => {
      expect(mockedPreviewTravel).toHaveBeenCalledTimes(1);
    });

    const sessionKey = gameSessionQueryKey("game-1");
    const updatedAt = queryClient.getQueryState(sessionKey)?.dataUpdatedAt ?? Date.now();
    queryClient.setQueryData(
      sessionKey,
      createSession({ player: { name: "Ruth", currentTownId: "dust-fork", health: 9 } }),
      { updatedAt: updatedAt + 1000 },
    );

    expect(await screen.findByRole("img", { name: /trail map/i })).toBeInTheDocument();
    loadingPreview.resolve(createPreview());

    expect(screen.queryByText("Prepare to ride")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /start the ride/i })).not.toBeInTheDocument();
  });

  it("shows a rejected preview message and keeps travel unavailable", async () => {
    primeMocks();
    mockedPreviewTravel.mockResolvedValue({
      success: false,
      message: "No route can be ridden today.",
      preview: null,
    });
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    renderPrep();

    await waitFor(() => {
      expect(mockState.games.length).toBeGreaterThan(0);
    });
    mockState.games[0].config.scene.selectTown("dust-fork");

    expect(await screen.findByText("No route can be ridden today.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /start the ride/i })).not.toBeInTheDocument();
  });

  it("shows transport failures and retries the preview on request", async () => {
    primeMocks();
    mockedPreviewTravel
      .mockRejectedValueOnce(new Error("The trail could not be checked."))
      .mockResolvedValueOnce(createPreview());
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    renderPrep();

    await waitFor(() => {
      expect(mockState.games.length).toBeGreaterThan(0);
    });
    mockState.games[0].config.scene.selectTown("dust-fork");

    expect(await screen.findByText("The trail could not be checked.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));

    expect(await screen.findByText(/2-day ride/i)).toBeInTheDocument();
    expect(mockedPreviewTravel).toHaveBeenCalledTimes(2);
  });

  it("rejects a preview for a different origin and only travels with a matching preview", async () => {
    primeMocks();
    mockedPreviewTravel.mockResolvedValueOnce(createPreview({ originTownId: "dust-fork" }));
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    renderPrep();

    await waitFor(() => {
      expect(mockState.games.length).toBeGreaterThan(0);
    });
    mockState.games[0].config.scene.selectTown("dust-fork");

    expect(await screen.findByText(/preview no longer matches this route/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /start the ride/i })).not.toBeInTheDocument();
    expect(mockedTravel).not.toHaveBeenCalled();

    mockedPreviewTravel.mockResolvedValueOnce(createPreview());
    mockedTravel.mockResolvedValue({
      success: true,
      message: "Ride started.",
      currentSession: createSession({
        player: { name: "Ruth", currentTownId: null, health: 9 },
      }),
    });
    fireEvent.click(screen.getByRole("button", { name: "Pick another destination" }));
    await waitFor(() => expect(mockState.games.length).toBeGreaterThan(1));
    mockState.games[mockState.games.length - 1].config.scene.selectTown("dust-fork");
    expect(await screen.findByRole("button", { name: /start the ride/i })).toBeEnabled();
    fireEvent.click(screen.getByRole("button", { name: /start the ride/i }));

    await waitFor(() => {
      expect(mockedTravel).toHaveBeenCalledWith("game-1", "dust-fork");
    });
  });

  it("keeps the second destination selected when the first preview resolves last", async () => {
    primeMocks();
    const firstPreview = deferred<TravelPreviewResultDto>();
    const secondPreview = deferred<TravelPreviewResultDto>();
    mockedPreviewTravel
      .mockReturnValueOnce(firstPreview.promise)
      .mockReturnValueOnce(secondPreview.promise);
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    renderPrep();

    await waitFor(() => expect(mockState.games.length).toBeGreaterThan(0));
    mockState.games[mockState.games.length - 1].config.scene.selectTown("dust-fork");
    await waitFor(() => expect(mockedPreviewTravel).toHaveBeenCalledTimes(1));
    fireEvent.click(screen.getByRole("button", { name: "Pick another destination" }));
    await waitFor(() => expect(mockState.games.length).toBeGreaterThan(1));
    mockState.games[mockState.games.length - 1].config.scene.selectTown("agua-fria");
    await waitFor(() => expect(mockedPreviewTravel).toHaveBeenCalledTimes(2));

    secondPreview.resolve(
      createPreview({ destinationTownId: "agua-fria", destinationTownName: "Agua Fria" }),
    );
    expect(await screen.findByText("Agua Fria")).toBeInTheDocument();
    firstPreview.resolve(createPreview());

    expect(screen.getByText("Agua Fria")).toBeInTheDocument();
    expect(screen.queryByText("Dust Fork")).not.toBeInTheDocument();
  });
});

describe("TravelPrepSurface map integration", () => {
  it("renders the Phaser map for destination selection", async () => {
    primeMocks();
    mockedPreviewTravel.mockResolvedValue(createPreview());
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    renderPrep();

    // The map should render (PhaserMapHost renders an img element).
    expect(await screen.findByRole("img", { name: /trail map/i })).toBeInTheDocument();
  });
});
