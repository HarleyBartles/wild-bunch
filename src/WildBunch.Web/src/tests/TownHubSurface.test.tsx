import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { TownHubSurface } from "../flow/TownHubSurface";
import { TownHubScene } from "../components/town-hub/TownHubScene";
import {
  AvailableActionKind,
  BuildingKind,
  StartFlowPhase,
  type GameSessionDto,
  type JournalDto,
  type TownLayoutDto,
} from "../api/types";
import {
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
} from "../api/wildBunchApi";

const mockState = vi.hoisted(() => ({
  games: [] as Array<{ config: { scene: TownHubScene }; destroyed: boolean; destroy: () => void }>,
  navigate: vi.fn(),
  search: {} as Record<string, unknown>,
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

vi.mock("@tanstack/react-router", () => ({
  useNavigate: () => mockState.navigate,
  useSearch: () => mockState.search,
}));

vi.mock("../api/wildBunchApi", () => ({
  getGame: vi.fn(),
  getAvailableActions: vi.fn(),
  getJournal: vi.fn(),
  getTownStoreOffers: vi.fn(),
  buyStoreItem: vi.fn(),
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
  getWorldMap: vi.fn(),
  getStartingTownMap: vi.fn(),
  setupGame: vi.fn(),
  markPrologueViewed: vi.fn(),
  startGameWithTown: vi.fn(),
  archiveGame: vi.fn(),
  getPrologue: vi.fn(),
  getStartingTowns: vi.fn(),
  previewTravel: vi.fn(),
}));

const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetTownStoreOffers = vi.mocked(getTownStoreOffers);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
  mockState.games.length = 0;
  mockState.navigate.mockReset();
  mockState.search = {};
});

function createLayout(): TownLayoutDto {
  return {
    buildings: [
      { kind: BuildingKind.Store, x: 12, y: 15, width: 8, height: 10 },
      { kind: BuildingKind.Sheriff, x: 46, y: 15, width: 8, height: 10 },
      { kind: BuildingKind.Saloon, x: 80, y: 15, width: 8, height: 10 },
      { kind: BuildingKind.Trailhead, x: 90, y: 50, width: 8, height: 10 },
      { kind: BuildingKind.Telegraph, x: 46, y: 70, width: 8, height: 10 },
    ],
    playerSpawnX: 50,
    playerSpawnY: 50,
  };
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
        { id: "t-town", name: "Tumbleweed", services: 0, layout: createLayout() },
        { id: "dust-fork", name: "Dust Fork", services: 0 },
      ],
      trails: [],
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
      openingLead: "The trail went cold outside town.",
      caseState: { statusText: "Still chasing leads." },
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

function primeMocks(
  session: GameSessionDto = createSession(),
  actions: AvailableActionKind[] = [
    AvailableActionKind.BuySupplies,
    AvailableActionKind.ReadWantedPosters,
    AvailableActionKind.LookAroundSaloon,
    AvailableActionKind.Travel,
  ],
) {
  mockedGetGame.mockResolvedValue(session);
  mockedGetAvailableActions.mockResolvedValue(actions.map((kind) => ({ kind, label: "action" })));
  mockedGetJournal.mockResolvedValue(createJournal());
  mockedGetTownStoreOffers.mockResolvedValue({
    townId: "t-town",
    townName: "Tumbleweed",
    available: true,
    sourceNote: "General store",
    offers: [],
  });
}

function renderHub() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <TownHubSurface />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

describe("TownHubSurface Phaser integration", () => {
  it("renders PhaserTownHubHost (creates a Phaser game) instead of the card grid", async () => {
    primeMocks();
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    // The visible card grid is gone — replaced by the Phaser canvas.
    // The old place cards with descriptions like "Buy supplies" are gone.
    // (A visible keyboard fallback nav with building buttons exists beneath
    // the canvas — those are tested in the accessibility fallback suite.)
    expect(screen.queryByText(/buy supplies, food, and gear/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/look around, gather gossip/i)).not.toBeInTheDocument();
  });

  it("passes the current town's layout to the TownHubScene", async () => {
    const session = createSession();
    primeMocks(session);
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    const scene = mockState.games[0].config.scene;
    expect(scene).toBeInstanceOf(TownHubScene);
    expect(scene.layout).toBe(session.world.towns[0].layout);
  });

  it("navigates to /town/store when the Store building is selected", async () => {
    primeMocks();
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    const scene = mockState.games[0].config.scene;
    scene.selectBuilding(BuildingKind.Store);

    expect(mockState.navigate).toHaveBeenCalledWith({ to: "/town/store" });
  });

  it("navigates to /town/sheriff when the Sheriff building is selected", async () => {
    primeMocks();
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    const scene = mockState.games[0].config.scene;
    scene.selectBuilding(BuildingKind.Sheriff);

    expect(mockState.navigate).toHaveBeenCalledWith({ to: "/town/sheriff" });
  });

  it("navigates to /town/saloon when the Saloon building is selected", async () => {
    primeMocks();
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    const scene = mockState.games[0].config.scene;
    scene.selectBuilding(BuildingKind.Saloon);

    expect(mockState.navigate).toHaveBeenCalledWith({ to: "/town/saloon" });
  });

  it("navigates to /town/trailhead when the Trailhead building is selected", async () => {
    primeMocks();
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    const scene = mockState.games[0].config.scene;
    scene.selectBuilding(BuildingKind.Trailhead);

    expect(mockState.navigate).toHaveBeenCalledWith({ to: "/town/trailhead" });
  });

  it("does not navigate when the Telegraph building is selected (no route)", async () => {
    primeMocks();
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    const scene = mockState.games[0].config.scene;
    // Telegraph is never available (isBuildingAvailable returns false), so
    // selectBuilding is a no-op and onBuildingSelected is never called.
    scene.selectBuilding(BuildingKind.Telegraph);

    expect(mockState.navigate).not.toHaveBeenCalled();
  });
});

describe("TownHubSurface accessibility fallback", () => {
  it("renders a visible keyboard-operable nav with buttons for available buildings", async () => {
    primeMocks();
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    const nav = screen.getByRole("navigation", { name: /town buildings/i });
    expect(nav).toBeInTheDocument();
    // The nav is visible (not clipped to 1px) so sighted keyboard users can see focus.
    expect(nav).toBeVisible();

    // Available buildings have keyboard-accessible buttons.
    expect(screen.getByRole("button", { name: /store/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sheriff office/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /saloon/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /hit the trail/i })).toBeInTheDocument();
  });

  it("does not render fallback buttons for unavailable buildings", async () => {
    // Only BuySupplies is available — Sheriff, Saloon, Trailhead are not.
    primeMocks(
      createSession(),
      [AvailableActionKind.BuySupplies],
    );
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    expect(screen.getByRole("button", { name: /store/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /sheriff office/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /saloon/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /hit the trail/i })).not.toBeInTheDocument();
  });

  it("fallback buttons navigate to the correct routes when activated", async () => {
    primeMocks();
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    const user = userEvent.setup();
    const storeButton = screen.getByRole("button", { name: /store/i });
    await user.click(storeButton);

    expect(mockState.navigate).toHaveBeenCalledWith({ to: "/town/store" });
  });

  it("fallback buttons are keyboard-focusable with visible focus indication", async () => {
    primeMocks();
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    const user = userEvent.setup();
    const storeButton = screen.getByRole("button", { name: /store/i });
    storeButton.focus();
    expect(storeButton).toHaveFocus();

    // Tab moves to the next building button.
    await user.tab();
    const sheriffButton = screen.getByRole("button", { name: /sheriff office/i });
    expect(sheriffButton).toHaveFocus();
  });
});

describe("TownHubSurface arrival notice", () => {
  it("shows the arrival notice when arrived=1 search param is present", async () => {
    primeMocks();
    mockState.search = { arrived: "1" };
    renderHub();

    const notice = await screen.findByRole("status");
    expect(notice).toBeInTheDocument();
    expect(notice.textContent).toMatch(/you've arrived in tumbleweed/i);
  });

  it("does not show the arrival notice without the arrived search param", async () => {
    primeMocks();
    renderHub();

    await waitFor(() => {
      expect(mockState.games).toHaveLength(1);
    });

    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("dismisses the arrival notice by navigating to /town with empty search", async () => {
    primeMocks();
    mockState.search = { arrived: "1" };
    renderHub();

    const dismissButton = await screen.findByRole("button", { name: /dismiss arrival notice/i });
    const user = userEvent.setup();
    await user.click(dismissButton);

    expect(mockState.navigate).toHaveBeenCalledWith({ to: "/town", search: {} });
  });
});
