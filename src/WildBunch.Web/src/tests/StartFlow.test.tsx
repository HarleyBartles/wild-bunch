import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { PreSessionSurface } from "../flow/PreSessionSurface";
import type { GameSessionDto, StartGameRequest } from "../api/types";
import { createGame, getGame, getAvailableActions, getJournal, getPrologue, getStartingTowns } from "../api/wildBunchApi";

vi.mock("../api/wildBunchApi", () => ({
  createGame: vi.fn(),
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
  getPrologue: vi.fn(),
  getStartingTowns: vi.fn(),
}));

const mockedCreateGame = vi.mocked(createGame);
const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetPrologue = vi.mocked(getPrologue);
const mockedGetStartingTowns = vi.mocked(getStartingTowns);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function createSession(overrides: Partial<GameSessionDto> = {}): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    travelDifficulty: 0,
    player: {
      name: "Ruth",
      currentTownId: "t-town",
      health: 9,
    },
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
    ...overrides,
  };
}

function renderSurface() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <PreSessionSurface />
      </GameSessionProvider>
    </QueryClientProvider>,
  );

  return { queryClient };
}

function primeMocks() {
  mockedGetGame.mockResolvedValue(createSession());
  mockedGetAvailableActions.mockResolvedValue([]);
  mockedGetJournal.mockResolvedValue({
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
  });
  mockedCreateGame.mockResolvedValue(createSession());
  mockedGetPrologue.mockResolvedValue({
    heading: "The story so far",
    body: "A culprit is on the run. The trail is fresh, but it won't stay that way for long.",
    primaryAction: "I understand. Keep riding.",
    variantId: "variant-1",
  });
  mockedGetStartingTowns.mockResolvedValue([
    { id: "t-town", name: "Tumbleweed", services: 0 },
    { id: "dust-fork", name: "Dust Fork", services: 0 },
  ]);
}

describe("StartFlow", () => {
  it("starts on the name step and advances to the story step", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    expect(await screen.findByRole("heading", { name: /howdy, pard'ner/i })).toBeInTheDocument();

    const nameInput = screen.getByLabelText(/player name/i);
    await user.type(nameInput, "Ranger Vale");

    await user.click(screen.getByRole("button", { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });
  });

  it("preserves the player name draft through the full flow", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/player name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /i understand\. keep riding\./i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    await user.click(screen.getAllByRole("button", { name: /start in /i })[0]);

    await waitFor(() => {
      expect(mockedCreateGame).toHaveBeenCalledTimes(1);
    });

    const request: StartGameRequest = mockedCreateGame.mock.calls[0][0];
    expect(request.playerName).toBe("Ranger Vale");
  });

  it("advances from story to town step", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/player name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /i understand\. keep riding\./i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });
  });

  it("navigates back from town to story", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/player name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /i understand\. keep riding\./i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /back/i }));
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });
  });

  it("does not call createGame during the name or story steps", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/player name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /i understand\. keep riding\./i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    // createGame (POST /api/games) must not have been called yet — only at the final step.
    expect(mockedCreateGame).not.toHaveBeenCalled();
  });

  it("calls createGame with playerName and startingTownId only at the final step", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/player name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /i understand\. keep riding\./i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    await user.click(screen.getAllByRole("button", { name: /start in /i })[0]);

    await waitFor(() => {
      expect(mockedCreateGame).toHaveBeenCalledTimes(1);
    });

    const request: StartGameRequest = mockedCreateGame.mock.calls[0][0];
    expect(request.playerName).toBe("Ranger Vale");
    expect(request.startingTownId).toBe("t-town");
    expect(request.seedCode).toBeTruthy();
  });

  it("shows the creating step after selecting a town", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/player name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /i understand\. keep riding\./i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    await user.click(screen.getAllByRole("button", { name: /start in /i })[0]);

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /starting your hunt/i })).toBeInTheDocument();
    });
  });
});
