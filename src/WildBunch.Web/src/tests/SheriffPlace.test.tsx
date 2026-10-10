import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  RouterProvider,
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
} from "@tanstack/react-router";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SheriffPlace } from "../flow/places/SheriffPlace";
import { GameSessionProvider } from "../state/GameSessionProvider";
import {
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
  readWantedPosters,
} from "../api/wildBunchApi";
import { AvailableActionKind } from "../api/types";
import type { GameSessionDto, JournalDto } from "../api/types";

vi.mock("../api/wildBunchApi", () => ({
  buyStoreItem: vi.fn(),
  getAvailableActions: vi.fn(),
  getGame: vi.fn(),
  getJournal: vi.fn(),
  getTownStoreOffers: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  lookAroundSaloon: vi.fn(),
  readWantedPosters: vi.fn(),
  gatherLocalGossip: vi.fn(),
  travel: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  resolveTravelEncounter: vi.fn(),
  previewTravel: vi.fn(),
}));

const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetTownStoreOffers = vi.mocked(getTownStoreOffers);
const mockedReadWantedPosters = vi.mocked(readWantedPosters);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function createSession(): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
    startFlowPhase: 4,
    player: { name: "Ruth", currentTownId: "t-town", health: 9 },
    world: { towns: [{ id: "t-town", name: "Tumbleweed" }], trails: [] },
    caseFile: {
      accusationId: null,
      openingLead: "The trail went cold outside town.",
      caseState: { statusText: "Still chasing leads." },
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
      openingLead: "The trail went cold outside town.",
      caseState: { statusText: "Still chasing leads." },
      caseSummary: "Find the culprit before the law closes in.",
      discoveredSuspects: [],
      caseBoard: { warrants: [], clues: [] },
      knownClues: [],
      knownWarrants: [],
      wantedPosters: [],
    },
    logEntries: [],
  };
}

function renderSheriffPlace() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
  const rootRoute = createRootRoute({ component: () => <Outlet /> });
  const sheriffRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/town/sheriff",
    component: () => <SheriffPlace />,
  });
  const router = createRouter({ routeTree: rootRoute.addChildren([sheriffRoute]) });
  window.history.replaceState({}, "", "/town/sheriff");
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <RouterProvider router={router} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}

describe("SheriffPlace", () => {
  it("offers wanted posters and routes the read without showing fake records", async () => {
    const user = userEvent.setup();
    const journal = createJournal();
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
    ]);
    mockedGetJournal.mockResolvedValue(journal);
    mockedGetTownStoreOffers.mockResolvedValue({
      townId: "t-town",
      townName: "Tumbleweed",
      offers: [],
    });
    mockedReadWantedPosters.mockResolvedValue({
      success: true,
      message: "You read the wanted posters.",
      currentJournal: journal,
      wantedPosters: [],
    });

    renderSheriffPlace();

    const readButton = await screen.findByRole("button", { name: /read wanted posters/i });
    expect(screen.getByRole("heading", { name: "Sheriff Office" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /records/i })).not.toBeInTheDocument();
    expect(screen.queryByText(/local records/i)).not.toBeInTheDocument();

    await user.click(readButton);
    await waitFor(() => expect(mockedReadWantedPosters).toHaveBeenCalledWith("game-1"));
  });
});
