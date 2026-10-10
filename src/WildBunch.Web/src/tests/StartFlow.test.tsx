import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { HttpRequestError } from "../api/httpClient";
import { PreSessionSurface } from "../flow/PreSessionSurface";
import type { GameSessionDto } from "../api/types";
import {
  getGame,
  getAvailableActions,
  getJournal,
  getPrologue,
  getWorldMap,
  setupGame,
  markPrologueViewed,
  startGameWithTown,
} from "../api/wildBunchApi";

const phaserMockState = vi.hoisted(() => ({
  games: [] as Array<{
    config: {
      scene: { selectTown: (townId: string) => void; onTownSelected?: (townId: string) => void };
    };
  }>,
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
  setupGame: vi.fn(),
  markPrologueViewed: vi.fn(),
  startGameWithTown: vi.fn(),
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
  gatherLocalGossip: vi.fn(),
  travel: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  getPrologue: vi.fn(),
  getWorldMap: vi.fn(),
}));

const mockedSetupGame = vi.mocked(setupGame);
const mockedMarkPrologueViewed = vi.mocked(markPrologueViewed);
const mockedStartGameWithTown = vi.mocked(startGameWithTown);
const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetPrologue = vi.mocked(getPrologue);
const mockedGetWorldMap = vi.mocked(getWorldMap);

beforeAll(async () => {
  // Preload the lazy town-selection step so it resolves quickly during
  // full-suite runs where the module has not been imported by another test.
  await import("../components/start-flow/StartingTownStep");
});

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
    startFlowPhase: 4,
    player: {
      name: "Ruth",
      currentTownId: "t-town",
      health: 9,
    },
    world: {
      towns: [
        { id: "t-town", name: "Tumbleweed" },
        { id: "dust-fork", name: "Dust Fork" },
      ],
      trails: [],
    },
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
      caseBoard: { warrants: [], clues: [] },
      knownClues: [],
      knownWarrants: [],
      wantedPosters: [],
    },
    logEntries: [],
  });
  mockedSetupGame.mockResolvedValue(createSession({ startFlowPhase: 1 }));
  mockedMarkPrologueViewed.mockResolvedValue(createSession({ startFlowPhase: 2 }));
  mockedStartGameWithTown.mockResolvedValue(createSession({ startFlowPhase: 4 }));
  mockedGetPrologue.mockResolvedValue({
    heading: "The story so far",
    body: "A culprit is on the run. The trail is fresh, but it won't stay that way for long.",
    primaryAction: "Ride on",
    variantId: "variant-1",
  });
  mockedGetWorldMap.mockResolvedValue({
    towns: [
      { id: "t-town", name: "Tumbleweed", x: 150, y: 500 },
      { id: "dust-fork", name: "Dust Fork", x: 450, y: 400 },
    ],
    trails: [{ id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", rideDayDistance: 3 }],
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

  it("loads the settled prologue for the saved session after a refresh", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "settled-session-42");
    mockedGetGame.mockResolvedValue(createSession({ id: "settled-session-42", startFlowPhase: 1 }));

    renderSurface();

    expect(await screen.findByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    await waitFor(() => {
      expect(mockedGetPrologue).toHaveBeenCalledWith("settled-session-42");
    });
    expect(mockedSetupGame).not.toHaveBeenCalled();
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

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }, { timeout: 5000 }),
    ).toBeInTheDocument();

    // Select a town through the Phaser map
    await waitFor(() => {
      expect(phaserMockState.games.length).toBeGreaterThan(0);
    });
    const game = phaserMockState.games[0];
    const scene = (game.config as any).scene;
    scene.onTownSelected("t-town");

    await waitFor(() => {
      expect(mockedStartGameWithTown).toHaveBeenCalledTimes(1);
    });

    expect(mockedStartGameWithTown.mock.calls[0][1]).toEqual({ startingTownId: "t-town" });
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

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }, { timeout: 5000 }),
    ).toBeInTheDocument();
  });

  it("shows entropy description for selected entropy", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /set up your hunt/i })).toBeInTheDocument();
    });

    // Default entropy is Classic (value 1)
    expect(screen.getByText(/balanced variance/i)).toBeInTheDocument();

    // Click on Wild entropy
    await user.click(screen.getByRole("button", { name: "Wild" }));

    // Description should update to Wild description
    await waitFor(() => {
      expect(screen.getByText(/big swings/i)).toBeInTheDocument();
    });
  });

  it("calls setupGame with playerName, difficulty, gameEntropy at the setup step and startGameWithTown at the final step", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const nameInput = await screen.findByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");
    const seedInput = screen.getByLabelText(/world seed/i);
    await user.clear(seedInput);
    await user.type(seedInput, "A1234567-B123-C123-D123-E123456789AB");

    // Select Challenging (difficulty 2) and Wild (gameEntropy 3)
    await user.click(screen.getByRole("button", { name: /^challenging$/i }));
    await user.click(screen.getByRole("button", { name: /^wild$/i }));

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    // setupGame should have been called at the setup step
    await waitFor(() => {
      expect(mockedSetupGame).toHaveBeenCalledTimes(1);
    });

    const setupRequest = mockedSetupGame.mock.calls[0][0];
    const setupOperationId = mockedSetupGame.mock.calls[0][1];
    expect(setupOperationId).toMatch(/^[0-9a-f-]{36}$/i);
    expect(setupRequest.playerName).toBe("Ranger Vale");
    expect(setupRequest.seedCode).toBe("a1234567-b123-c123-d123-e123456789ab");
    expect(setupRequest.gameDifficulty).toBe(2);
    expect(setupRequest.gameEntropy).toBe(3);

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }, { timeout: 5000 }),
    ).toBeInTheDocument();

    // Select a town through the Phaser map
    await waitFor(() => {
      expect(phaserMockState.games.length).toBeGreaterThan(0);
    });
    const game = phaserMockState.games[0];
    const scene = (game.config as any).scene;
    scene.onTownSelected("t-town");

    await waitFor(() => {
      expect(mockedStartGameWithTown).toHaveBeenCalledTimes(1);
    });

    expect(mockedStartGameWithTown.mock.calls[0][1]).toEqual({ startingTownId: "t-town" });
    expect(mockedStartGameWithTown.mock.calls[0][2]).toMatch(/^[0-9a-f-]{36}$/i);
    expect(window.localStorage.getItem("wild-bunch.pending-first-arrival:game-1")).toBeNull();
  });

  it("retries a pending setup after refresh with the same operation ID and exact payload", async () => {
    primeMocks();
    const pendingOperation = {
      operationId: "8b883c75-4e2e-4e82-90b4-95cb3867ae51",
      request: {
        playerName: "Ranger Vale",
        gameDifficulty: 2,
        gameEntropy: 3,
        seedCode: "a1234567-b123-c123-d123-e123456789ab",
      },
    };
    window.localStorage.setItem(
      "wild-bunch.pending-setup-operation",
      JSON.stringify(pendingOperation),
    );
    mockedGetGame.mockResolvedValue(createSession({ startFlowPhase: 1 }));

    renderSurface();

    await waitFor(() => {
      expect(mockedSetupGame).toHaveBeenCalledWith(
        pendingOperation.request,
        pendingOperation.operationId,
      );
    });
    expect(await screen.findByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    expect(window.localStorage.getItem("wild-bunch.pending-setup-operation")).toBeNull();
  });

  it("keeps and retries the exact setup operation after an uncertain response", async () => {
    primeMocks();
    mockedSetupGame.mockRejectedValueOnce(new Error("connection lost"));
    const user = userEvent.setup();
    const pending = {
      operationId: "8b883c75-4e2e-4e82-90b4-95cb3867ae51",
      request: {
        playerName: "Ranger Vale",
        gameDifficulty: 2 as const,
        gameEntropy: 3 as const,
        seedCode: "a1234567-b123-c123-d123-e123456789ab",
      },
    };
    window.localStorage.setItem("wild-bunch.pending-setup-operation", JSON.stringify(pending));
    renderSurface();

    expect(await screen.findByText("connection lost")).toBeInTheDocument();
    const savedPending = window.localStorage.getItem("wild-bunch.pending-setup-operation");
    expect(savedPending).not.toBeNull();
    const firstCall = mockedSetupGame.mock.calls[0];
    expect(screen.getByLabelText(/your name/i)).toHaveValue("Ranger Vale");
    expect(screen.getByLabelText(/your name/i)).toBeDisabled();
    expect(screen.getByRole("button", { name: /retry setup/i })).toBeEnabled();
    expect(firstCall).toEqual([pending.request, pending.operationId]);

    await user.click(screen.getByRole("button", { name: /retry setup/i }));

    expect(await screen.findByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    expect(mockedSetupGame).toHaveBeenCalledTimes(2);
    expect(mockedSetupGame.mock.calls[1]).toEqual(firstCall);
    expect(window.localStorage.getItem("wild-bunch.pending-setup-operation")).toBeNull();
  });

  it("uses a new operation ID after a definitive setup rejection is corrected", async () => {
    primeMocks();
    mockedSetupGame.mockRejectedValueOnce(new HttpRequestError("Invalid setup", 400));
    const user = userEvent.setup();
    renderSurface();

    await user.type(await screen.findByLabelText(/your name/i), "Ranger Vale");
    const seedInput = screen.getByLabelText(/world seed/i);
    await user.clear(seedInput);
    await user.type(seedInput, "11111111-1111-4111-8111-111111111111");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    expect(await screen.findByText("Invalid setup")).toBeInTheDocument();
    const rejectedOperationId = mockedSetupGame.mock.calls[0][1];
    expect(window.localStorage.getItem("wild-bunch.pending-setup-operation")).toBeNull();
    expect(screen.getByLabelText(/your name/i)).toBeEnabled();

    const playerNameInput = screen.getByLabelText(/your name/i);
    await user.clear(playerNameInput);
    await user.type(playerNameInput, "Clint Eastwood");
    await user.clear(seedInput);
    await user.type(seedInput, "22222222-2222-4222-8222-222222222222");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    expect(await screen.findByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    expect(mockedSetupGame).toHaveBeenCalledTimes(2);
    expect(mockedSetupGame.mock.calls[1][0]).toMatchObject({
      playerName: "Clint Eastwood",
      seedCode: "22222222-2222-4222-8222-222222222222",
    });
    expect(mockedSetupGame.mock.calls[1][1]).not.toBe(rejectedOperationId);
  });

  it("retries a pending first arrival with its original town and operation ID", async () => {
    primeMocks();
    const pending = {
      operationId: "fab21f5d-7f89-4382-9520-24a5b4f558d5",
      sessionId: "saved-session",
      townId: "dust-fork",
    };
    window.localStorage.setItem("wild-bunch.current-game-id", pending.sessionId);
    window.localStorage.setItem(
      `wild-bunch.pending-first-arrival:${pending.sessionId}`,
      JSON.stringify(pending),
    );
    mockedGetGame.mockResolvedValue(createSession({ id: pending.sessionId, startFlowPhase: 2 }));
    mockedStartGameWithTown.mockResolvedValue(
      createSession({ id: pending.sessionId, startFlowPhase: 4 }),
    );

    renderSurface();

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }, { timeout: 5000 }),
    ).toBeInTheDocument();
    await waitFor(() => expect(phaserMockState.games.length).toBeGreaterThan(0));
    phaserMockState.games[0].config.scene.selectTown(pending.townId);

    await waitFor(() => expect(mockedStartGameWithTown).toHaveBeenCalledTimes(1));
    expect(mockedStartGameWithTown).toHaveBeenCalledWith(
      pending.sessionId,
      { startingTownId: pending.townId },
      pending.operationId,
    );
    expect(
      window.localStorage.getItem(`wild-bunch.pending-first-arrival:${pending.sessionId}`),
    ).toBeNull();
  });

  it("keeps a definitive first-arrival conflict when the server already shows a started game", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    await user.type(await screen.findByLabelText(/your name/i), "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /ride on/i }));
    await screen.findByRole("heading", { name: /the story so far/i });
    await user.click(screen.getByRole("button", { name: /ride on/i }));
    await screen.findByRole("heading", { name: /pick a starting town/i }, { timeout: 5000 });
    await waitFor(() => expect(phaserMockState.games.length).toBeGreaterThan(0));

    const conflict = new HttpRequestError("The starting town can only be selected once.", 409);
    mockedStartGameWithTown.mockRejectedValue(conflict);
    mockedGetGame.mockResolvedValue(createSession({ id: "game-1", startFlowPhase: 4 }));
    phaserMockState.games[0].config.scene.selectTown("t-town");

    expect(await screen.findByText(conflict.message)).toBeInTheDocument();
    expect(screen.queryByText(/new game started/i)).not.toBeInTheDocument();
    expect(window.localStorage.getItem("wild-bunch.pending-first-arrival:game-1")).toBeNull();
  });

  it("keeps a name-only quick start on defaults and submits the visit seed", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    const seedInput = await screen.findByLabelText(/world seed/i);
    const visitSeed = (seedInput as HTMLInputElement).value;
    expect(visitSeed).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i);

    await user.type(screen.getByLabelText(/your name/i), "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    await waitFor(() => expect(mockedSetupGame).toHaveBeenCalledTimes(1));
    expect(mockedSetupGame.mock.calls[0][0]).toMatchObject({
      playerName: "Ranger Vale",
      gameDifficulty: 0,
      gameEntropy: 1,
      seedCode: visitSeed.toLowerCase(),
    });
  });

  it("does not submit setup when the edited seed is invalid", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    await user.type(await screen.findByLabelText(/your name/i), "Ranger Vale");
    const seedInput = screen.getByLabelText(/world seed/i);
    await user.clear(seedInput);
    await user.type(seedInput, "not-a-uuid");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(/seed/i);
    expect(mockedSetupGame).not.toHaveBeenCalled();
    expect(screen.getByRole("heading", { name: /set up your hunt/i })).toBeInTheDocument();
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

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }, { timeout: 5000 }),
    ).toBeInTheDocument();

    // Select a town through the Phaser map
    await waitFor(() => {
      expect(phaserMockState.games.length).toBeGreaterThan(0);
    });
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

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }, { timeout: 5000 }),
    ).toBeInTheDocument();

    expect(screen.queryByRole("button", { name: /back/i })).not.toBeInTheDocument();
  });

  it("calls startGameWithTown with the correct startingTownId when a town is selected via the map host", async () => {
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

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }, { timeout: 5000 }),
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(phaserMockState.games.length).toBeGreaterThan(0);
    });

    const scene = phaserMockState.games[0].config.scene;
    scene.selectTown("dust-fork");

    await waitFor(() => {
      expect(mockedStartGameWithTown).toHaveBeenCalledTimes(1);
    });

    expect(mockedStartGameWithTown.mock.calls[0][1]).toEqual({ startingTownId: "dust-fork" });
  });

  it("mounts the Phaser map but does not call startGameWithTown until a town is selected", async () => {
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

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }, { timeout: 5000 }),
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(phaserMockState.games.length).toBeGreaterThan(0);
    });

    expect(mockedStartGameWithTown).not.toHaveBeenCalled();

    const scene = phaserMockState.games[0].config.scene;
    scene.selectTown("t-town");

    await waitFor(() => {
      expect(mockedStartGameWithTown).toHaveBeenCalledTimes(1);
    });
  });

  it("shows a description for the selected difficulty", async () => {
    primeMocks();
    const user = userEvent.setup();
    renderSurface();

    await screen.findByRole("heading", { name: /set up your hunt/i });

    // Default difficulty is Standard (0) — check its description is visible
    expect(screen.getByText(/a fair chase/i)).toBeInTheDocument();

    // Click "Brutal" and check its description appears
    await user.click(screen.getByRole("button", { name: /brutal/i }));
    expect(screen.getByText(/the desert wants you dead/i)).toBeInTheDocument();
  });
});
