import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps } from "react";
import { StartGamePanel } from "../components/StartGamePanel";
import type { GameSessionDto, StartGameRequest } from "../api/types";
import { decodeGameSetupSeed } from "../ui/gameSetupSeedCodec";

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
      towns: [],
      trails: [],
    },
    caseFile: {
      accusationId: null,
      openingLead: "",
      caseState: {
        statusText: "",
      },
      discoveredSuspects: [],
      caseBoard: {
        namedRecords: [],
        looseLeads: [],
        evidenceItems: [],
      },
      knownClues: [],
    },
    inventory: {
      wallet: { cash: 0 },
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

function renderPanel(
  props: Partial<ComponentProps<typeof StartGamePanel>> = {},
  onStartGame = vi.fn(),
  onRefresh = vi.fn(),
) {
  onStartGame.mockImplementation(async (_request: StartGameRequest) => undefined);
  onRefresh.mockImplementation(async () => undefined);

  render(
    <StartGamePanel
      session={createSession()}
      busy={false}
      gameId={null}
      resetToken={1}
      onStartGame={onStartGame}
      onRefresh={onRefresh}
      {...props}
    />
  );

  return { onStartGame, onRefresh };
}

describe("StartGamePanel", () => {
  beforeEach(() => {
    // Provide default mock values for all tests
  });

  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it("renders a UUID seed and starts a game with the current seed", async () => {
    const user = userEvent.setup();
    const { onStartGame } = renderPanel();

    const seedInput = await screen.findByLabelText(/setup seed/i);
    expect((seedInput as HTMLInputElement).value).toBe("00000000-0000-0000-0000-000000000000");

    await user.type(screen.getByLabelText(/player name/i), "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /start new game/i }));

    await waitFor(() => {
      expect(onStartGame).toHaveBeenCalledTimes(1);
    });

    const [request] = onStartGame.mock.calls[0];
    expect(request.gameDifficulty).toBe(0);
  });

  it("validates a pasted UUID", async () => {
    const user = userEvent.setup();
    renderPanel();

    const seedInput = await screen.findByLabelText(/setup seed/i);

    await user.clear(seedInput);
    await user.type(seedInput, "7D455293-F269-A642-72AF-0193FDBDFB51");

    expect(seedInput).toHaveValue("7D455293-F269-A642-72AF-0193FDBDFB51");
  });

  it("starts with the typed seed", async () => {
    const user = userEvent.setup();
    const { onStartGame } = renderPanel();

    const seedInput = await screen.findByLabelText(/setup seed/i);

    await user.clear(seedInput);
    await user.type(seedInput, "7d455293-f269-a642-72af-0193fdbdfb51");
    await user.type(screen.getByLabelText(/player name/i), "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /start new game/i }));

    await waitFor(() => {
      expect(onStartGame).toHaveBeenCalledTimes(1);
    });

    const [request] = onStartGame.mock.calls[0];
    // The seed is now encoded before being sent
    expect(request.seedCode).toBeTruthy();
    expect(request.gameDifficulty).toBe(0);
  });

  it("randomizes the seed to a fresh UUID", async () => {
    const user = userEvent.setup();
    const { onStartGame } = renderPanel();

    const seedInput = await screen.findByLabelText(/setup seed/i);
    const beforeRandomize = (seedInput as HTMLInputElement).value;

    await user.click(screen.getByRole("button", { name: /randomize seed/i }));

    await waitFor(() => {
      expect((seedInput as HTMLInputElement).value).not.toBe(beforeRandomize);
    });

    await user.type(screen.getByLabelText(/player name/i), "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /start new game/i }));

    await waitFor(() => {
      expect(onStartGame).toHaveBeenCalledTimes(1);
    });

    const [request] = onStartGame.mock.calls[0];
    expect(request.gameDifficulty).toBe(0);
  });

  it("updates difficulty when selected", async () => {
    const user = userEvent.setup();
    renderPanel();

    await user.selectOptions(screen.getByLabelText(/Game difficulty/i), "2");

    await waitFor(() => {
      expect(screen.getByLabelText(/Game difficulty/i)).toHaveValue("2");
    });
  });
});
