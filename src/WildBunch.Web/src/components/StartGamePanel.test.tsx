import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps } from "react";
import { StartGamePanel } from "./StartGamePanel";
import type { GameSessionDto, StartGameRequest } from "../api/types";
import { decodeGameSetupSeed } from "../ui/gameSetupSeedCodec";

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
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
    clock: { day: 1, turn: 0 },
    pursuitState: { heat: 0 },
    journey: null,
    travelDiary: null,
    logEntries: [],
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
      session={props.session ?? null}
      busy={props.busy ?? false}
      gameId={props.gameId ?? null}
      resetToken={props.resetToken ?? 0}
      onStartGame={onStartGame}
      onRefresh={onRefresh}
    />,
  );

  return { onStartGame, onRefresh };
}

describe("StartGamePanel", () => {
  it("renders a UUID seed and starts a game with the current seed", async () => {
    const user = userEvent.setup();
    const { onStartGame } = renderPanel();

    const playerName = screen.getByLabelText(/player name/i);
    const difficulty = screen.getByLabelText(/travel difficulty/i);
    const seedInput = await screen.findByLabelText(/setup seed/i);

    await waitFor(() => {
      expect((seedInput as HTMLInputElement).value).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/);
    });

    await user.selectOptions(difficulty, "2");
    await user.type(playerName, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /start new game/i }));

    await waitFor(() => {
      expect(onStartGame).toHaveBeenCalledTimes(1);
    });

    const [request] = onStartGame.mock.calls[0];
    expect(request.playerName).toBe("Ranger Vale");
    expect(request.travelDifficulty).toBe(2);
    expect(request.seedCode).toBe((seedInput as HTMLInputElement).value);
  });

  it("validates a pasted UUID until Apply is clicked", async () => {
    const user = userEvent.setup();
    renderPanel();

    const seedInput = await screen.findByLabelText(/setup seed/i);
    const applyButton = screen.getByRole("button", { name: /apply seed/i });

    await user.clear(seedInput);
    await user.type(seedInput, "7D455293-F269-A642-72AF-0193FDBDFB51");

    expect(seedInput).toHaveValue("7D455293-F269-A642-72AF-0193FDBDFB51");
    expect(screen.getByText(/seed changes are staged until you apply them/i)).toBeInTheDocument();

    await user.click(applyButton);

    await waitFor(() => {
      expect(seedInput).toHaveValue("7d455293-f269-a642-72af-0193fdbdfb51");
    });

    const decoded = await decodeGameSetupSeed((seedInput as HTMLInputElement).value);
    expect(decoded.seedCode).toBe("7d455293-f269-a642-72af-0193fdbdfb51");
  });

  it("starts with the applied seed even if a dirty draft is still staged", async () => {
    const user = userEvent.setup();
    const { onStartGame } = renderPanel();

    const seedInput = await screen.findByLabelText(/setup seed/i);
    const canonicalSeedCode = (seedInput as HTMLInputElement).value;

    await user.clear(seedInput);
    await user.type(seedInput, "7d455293-f269-a642-72af-0193fdbdfb51");
    await user.type(screen.getByLabelText(/player name/i), "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /start new game/i }));

    await waitFor(() => {
      expect(onStartGame).toHaveBeenCalledTimes(1);
    });

    const [request] = onStartGame.mock.calls[0];
    expect(request.seedCode).toBe(canonicalSeedCode);
    expect(request.travelDifficulty).toBe(0);
  });

  it("randomizes the seed to a fresh UUID and sends it to the backend", async () => {
    const user = userEvent.setup();
    const randomUUID = vi.spyOn(globalThis.crypto, "randomUUID").mockReturnValue("11111111-2222-3333-4444-555555555555");
    const { onStartGame } = renderPanel();

    const seedInput = await screen.findByLabelText(/setup seed/i);
    const beforeRandomize = (seedInput as HTMLInputElement).value;

    await user.selectOptions(screen.getByLabelText(/travel difficulty/i), "1");
    await user.click(screen.getByRole("button", { name: /randomize seed/i }));

    await waitFor(() => {
      expect(randomUUID).toHaveBeenCalledTimes(1);
      expect((seedInput as HTMLInputElement).value).toBe("11111111-2222-3333-4444-555555555555");
      expect((seedInput as HTMLInputElement).value).not.toBe(beforeRandomize);
    });

    await user.type(screen.getByLabelText(/player name/i), "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /start new game/i }));

    await waitFor(() => {
      expect(onStartGame).toHaveBeenCalledTimes(1);
    });

    const [request] = onStartGame.mock.calls[0];
    expect(request.travelDifficulty).toBe(1);
    expect(request.seedCode).toBe("11111111-2222-3333-4444-555555555555");
  });
});
