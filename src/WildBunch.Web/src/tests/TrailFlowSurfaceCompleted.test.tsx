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
