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
