import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { TravelPrepSurface } from "../flow/TravelPrepSurface";
import { StartingTownMapScene } from "../components/start-flow/PhaserMapHost";
import {
  AvailableActionKind,
  StartFlowPhase,
  type GameSessionDto,
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
  games: [] as Array<{ config: { scene: StartingTownMapScene }; destroyed: boolean; destroy: () => void }>,
}));

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    public destroyed = false;
    constructor(config: unknown) {
      this.config = config;
      mockState.games.push(this as never);
    }
    destroy() { this.destroyed = true; }
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
  getWorldMap: vi.fn(),
  getStartingTownMap: vi.fn(),
  setupGame: vi.fn(),
  markPrologueViewed: vi.fn(),
  startGameWithTown: vi.fn(),
  advanceTravelDay: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
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
const mockedPreviewTravel = vi.mocked(previewTravel);
const mockedGetWorldMap = vi.mocked(getWorldMap);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
  mockState.games.length = 0;
});

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
      caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] },
      knownClues: [],
      knownWarrants: [],
      wantedPosters: [],
    },
    logEntries: [],
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

function primeMocks(session: GameSessionDto = createSession()) {
  mockedGetGame.mockResolvedValue(session);
  mockedGetAvailableActions.mockResolvedValue([
    { kind: AvailableActionKind.Travel, label: "Hit the trail" },
  ]);
  mockedGetJournal.mockResolvedValue(createJournal());
  mockedGetWorldMap.mockResolvedValue({
    towns: [
      { id: "t-town", name: "Tumbleweed", services: 0, x: 150, y: 500 },
      { id: "dust-fork", name: "Dust Fork", services: 0, x: 450, y: 400 },
    ],
    trails: [
      { id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", rideDayDistance: 3 },
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
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <TravelPrepSurface onBack={vi.fn()} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
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

describe("TravelPrepSurface map integration", () => {
  it("renders the Phaser map for destination selection", async () => {
    primeMocks();
    mockedPreviewTravel.mockResolvedValue(createPreview());
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    renderPrep();

    // The map should render (PhaserMapHost renders an img element).
    expect(await screen.findByRole("img", { name: /trail map/i })).toBeInTheDocument();
  });

  it("does not render the old text destination list", async () => {
    primeMocks();
    mockedPreviewTravel.mockResolvedValue(createPreview());
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    renderPrep();

    await screen.findByRole("img", { name: /trail map/i });

    // The old text list showed "Click to check the ride" on each card.
    expect(screen.queryByText(/click to check the ride/i)).not.toBeInTheDocument();
  });
});
