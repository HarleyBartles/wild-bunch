import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { PreSessionSurface } from "../flow/PreSessionSurface";
import type { GameSessionDto, StartGameRequest } from "../api/types";
import { createGame, getGame, getAvailableActions, getJournal, getPrologue, getStartingTowns, getStartingTownMap } from "../api/wildBunchApi";

const phaserMockState = vi.hoisted(() => ({
  games: [] as Array<{ config: { scene: { selectTown: (townId: string) => void; onTownSelected?: (townId: string) => void } } }>,
}));

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    public destroyed = false;
    constructor(config: unknown) {
      this.config = config;
      phaserMockState.games.push(this as never);
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
  getStartingTownMap: vi.fn(),
}));

const mockedCreateGame = vi.mocked(createGame);
const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetPrologue = vi.mocked(getPrologue);
const mockedGetStartingTowns = vi.mocked(getStartingTowns);
const mockedGetStartingTownMap = vi.mocked(getStartingTownMap);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
  phaserMockState.games.length = 0;
});

function createSession(overrides: Partial<GameSessionDto> = {}): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
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
    wantedPosters: [],
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
    primaryAction: "Ride on",
    variantId: "variant-1",
  });
  mockedGetStartingTowns.mockResolvedValue([
    { id: "t-town", name: "Tumbleweed", services: 0 },
    { id: "dust-fork", name: "Dust Fork", services: 0 },
  ]);
  mockedGetStartingTownMap.mockResolvedValue({
    towns: [
      { id: "t-town", name: "Tumbleweed", services: 0, x: 150, y: 500 },
      { id: "dust-fork", name: "Dust Fork", services: 0, x: 450, y: 400 },
    ],
    trails: [
      { id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", rideDayDistance: 3 },
    ],
  });
}

describe("StartFlow", () => {
  it("starts on the setup step and advances to the story step", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    expect(await screen.findByRole("heading", { name: /set up your hunt/i })).toBeInTheDocument();

    const nameInput = screen.getByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });
  });

  it("preserves the player name draft through the full flow", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    // Select a town through the Phaser map
    const game = phaserMockState.games[0];
    const scene = (game.config as any).scene;
    scene.onTownSelected("t-town");

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

    const nameInput = await screen.findByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });
  });

  it("does not call createGame during the setup or story steps", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    // createGame (POST /api/games) must not have been called yet — only at the final step.
    expect(mockedCreateGame).not.toHaveBeenCalled();
  });

  it("calls createGame with playerName, difficulty, gameEntropy, and startingTownId at the final step", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");

    // Select Challenging (difficulty 2) and Wild (gameEntropy 3)
    await user.click(screen.getByRole("button", { name: /^challenging$/i }));
    await user.click(screen.getByRole("button", { name: /^wild$/i }));

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    // Select a town through the Phaser map
    const game = phaserMockState.games[0];
    const scene = (game.config as any).scene;
    scene.onTownSelected("t-town");

    await waitFor(() => {
      expect(mockedCreateGame).toHaveBeenCalledTimes(1);
    });

    const request: StartGameRequest = mockedCreateGame.mock.calls[0][0];
    expect(request.playerName).toBe("Ranger Vale");
    expect(request.startingTownId).toBe("t-town");
    expect(request.seedCode).toBeTruthy();
    expect(request.gameDifficulty).toBe(2);
    expect(request.gameEntropy).toBe(3);
  });

  it("shows the creating step after selecting a town", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    // Select a town through the Phaser map
    const game = phaserMockState.games[0];
    const scene = (game.config as any).scene;
    scene.onTownSelected("t-town");

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /starting your hunt/i })).toBeInTheDocument();
    });
  });

  it("does not render a Back button on the town step", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    expect(screen.queryByRole("button", { name: /back/i })).not.toBeInTheDocument();
  });

  it("calls createGame with the correct startingTownId when a town is selected via the map host", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(phaserMockState.games.length).toBeGreaterThan(0);
    });

    const scene = phaserMockState.games[0].config.scene;
    scene.selectTown("dust-fork");

    await waitFor(() => {
      expect(mockedCreateGame).toHaveBeenCalledTimes(1);
    });

    const request: StartGameRequest = mockedCreateGame.mock.calls[0][0];
    expect(request.startingTownId).toBe("dust-fork");
    expect(request.playerName).toBe("Ranger Vale");
  });

  it("mounts the Phaser map but does not call createGame until a town is selected", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(phaserMockState.games.length).toBeGreaterThan(0);
    });

    expect(mockedCreateGame).not.toHaveBeenCalled();

    const scene = phaserMockState.games[0].config.scene;
    scene.selectTown("t-town");

    await waitFor(() => {
      expect(mockedCreateGame).toHaveBeenCalledTimes(1);
    });
  });

  it("does not call createGame when the map mounts and no town is selected", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /pick a starting town/i })).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(phaserMockState.games.length).toBeGreaterThan(0);
    });

    expect(mockedCreateGame).not.toHaveBeenCalled();
  });
});
