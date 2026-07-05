import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider, createRootRoute, createRoute, createRouter, Outlet } from "@tanstack/react-router";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SheriffPlace } from "../flow/places/SheriffPlace";
import { GameSessionProvider } from "../state/GameSessionProvider";
import {
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
  checkLocalRecords,
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
  archiveGame: vi.fn(),
  setupGame: vi.fn(),
  startGame: vi.fn(),
  completePlayerSetup: vi.fn(),
  startOver: vi.fn(),
  confrontWantedSuspect: vi.fn(),
  settleSheriffTurnIn: vi.fn(),
}));

function createSession(): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
    startFlowPhase: 4,
    player: { name: "Ruth", currentTownId: "t-town", health: 9 },
    world: {
      towns: [
        { id: "t-town", name: "Tumbleweed", services: 0 },
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
      caseSummary: "Find the culprit.",
      discoveredSuspects: [],
      caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] },
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

function primeMocks() {
  vi.mocked(getGame).mockResolvedValue(createSession());
  vi.mocked(getJournal).mockResolvedValue(createJournal());
  vi.mocked(getAvailableActions).mockResolvedValue([
    { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
    { kind: AvailableActionKind.CheckSheriffRecords, label: "Check local records" },
  ]);
  vi.mocked(getTownStoreOffers).mockResolvedValue({
    townId: "t-town",
    townName: "Tumbleweed",
    available: true,
    sourceNote: "General store",
    offers: [],
  });
  vi.mocked(readWantedPosters).mockResolvedValue({
    success: true,
    message: "You read the wanted posters.",
    currentJournal: createJournal(),
    wantedPosters: [],
  });
}

describe("beatNarration in investigation notice", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    primeMocks();
  });

  afterEach(() => {
    cleanup();
    window.localStorage.clear();
  });

  it("checkLocalRecords notice includes beatNarration text", async () => {
    const beatNarration = "You spent the morning at the sheriff's office in Tumbleweed";
    vi.mocked(checkLocalRecords).mockResolvedValue({
      success: true,
      message: "You check the local records and uncover a public lead.",
      currentJournal: createJournal(),
      beatNarration,
    });

    renderSheriffPlace();
    const user = userEvent.setup();

    const checkButton = await screen.findByRole("button", { name: /check local records/i });
    await user.click(checkButton);

    // The notice must contain the beat narration text — this only happens if the hook
    // consumed result.beatNarration and composed it via formatInvestigationNotice.
    await waitFor(() => {
      const notice = screen.getByText(/sheriff's office/i);
      expect(notice).toBeInTheDocument();
      expect(notice.textContent).toContain("sheriff's office");
      expect(notice.textContent).toContain("public lead");
    });
  });

  it("checkLocalRecords notice does not show raw turn counter", async () => {
    vi.mocked(checkLocalRecords).mockResolvedValue({
      success: true,
      message: "No new warrants.",
      currentJournal: createJournal(),
      beatNarration: "You spent the morning at the sheriff's office in Tumbleweed",
    });

    renderSheriffPlace();
    const user = userEvent.setup();

    const checkButton = await screen.findByRole("button", { name: /check local records/i });
    await user.click(checkButton);

    await waitFor(() => {
      const notice = screen.getByText(/sheriff's office/i);
      expect(notice.textContent).not.toMatch(/turn\s*\d/i);
    });
  });

  it("falls back to message-only when beatNarration is null", async () => {
    vi.mocked(checkLocalRecords).mockResolvedValue({
      success: true,
      message: "No new warrants.",
      currentJournal: createJournal(),
      beatNarration: null,
    });

    renderSheriffPlace();
    const user = userEvent.setup();

    const checkButton = await screen.findByRole("button", { name: /check local records/i });
    await user.click(checkButton);

    await waitFor(() => {
      expect(screen.getByText("No new warrants.")).toBeInTheDocument();
    });
  });
});
