import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps } from "react";
import { StartGamePanel } from "./StartGamePanel";
import type { GameSessionDto, StartGameRequest } from "../api/types";
import {
  createCanonicalSeedState,
  decodeGameSetupSeed,
  encodeGameSetupSeed,
  withDifficulty,
  withJourneyRandomnessMode,
  withLoadoutProfile,
  withRandomEntropy,
  withStartWithHorse,
} from "../ui/gameSetupSeedCodec";

vi.mock("../ui/gameSetupSeedCodec", async () => {
  const actual = await vi.importActual<typeof import("../ui/gameSetupSeedCodec")>("../ui/gameSetupSeedCodec");
  return {
    ...actual,
    withRandomEntropy: vi.fn((seed) => ({ ...seed, entropy: 1234n })),
  };
});

const mockedWithRandomEntropy = vi.mocked(withRandomEntropy);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
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
      killerReleaseState: {
        isReleased: false,
        progress: 0,
        requiredPublicClues: 3,
        statusText: "",
      },
      discoveredSuspects: [],
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
  it("renders the seed setup UI and starts a game with the current seed", async () => {
    const user = userEvent.setup();
    const { onStartGame } = renderPanel();

    const playerName = screen.getByLabelText(/player name/i);
    const seedInput = await screen.findByLabelText(/setup seed/i);

    await waitFor(() => {
      expect((seedInput as HTMLInputElement).value).toMatch(/^WB1-/);
    });

    await user.type(playerName, "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /start new game/i }));

    await waitFor(() => {
      expect(onStartGame).toHaveBeenCalledTimes(1);
    });

    const [request] = onStartGame.mock.calls[0];
    expect(request.playerName).toBe("Ranger Vale");
    expect(request.travelDifficulty).toBe(0);
    expect(request.seedCode).toBe((seedInput as HTMLInputElement).value);
  });

  it("stages a manually entered seed until Apply is clicked", async () => {
    const user = userEvent.setup();
    renderPanel();

    const seedState = withLoadoutProfile(
      withJourneyRandomnessMode(withStartWithHorse(withDifficulty(createCanonicalSeedState(), 2), false), 1),
      2,
    );
    const seedCode = await encodeGameSetupSeed(seedState);
    const canonicalSeedCode = await encodeGameSetupSeed(createCanonicalSeedState());

    const seedInput = await screen.findByLabelText(/setup seed/i);
    const difficulty = screen.getByLabelText(/difficulty/i);
    const horse = screen.getByLabelText(/start with horse/i);
    const loadout = screen.getByLabelText(/loadout profile/i);
    const journeyRandomness = screen.getByLabelText(/journey randomness/i);
    const applyButton = screen.getByRole("button", { name: /apply seed/i });

    await waitFor(() => {
      expect(seedInput).toHaveValue(canonicalSeedCode);
    });

    await user.clear(seedInput);
    await user.type(seedInput, seedCode);

    expect(seedInput).toHaveValue(seedCode);
    expect(difficulty).toHaveValue("0");
    expect(horse).toBeChecked();
    expect(loadout).toHaveValue("0");
    expect(journeyRandomness).toHaveValue("0");
    expect(screen.getByText(/seed changes are staged until you apply them/i)).toBeInTheDocument();

    await user.click(applyButton);

    await waitFor(() => {
      expect(difficulty).toHaveValue("2");
      expect(horse).not.toBeChecked();
      expect(loadout).toHaveValue("2");
      expect(journeyRandomness).toHaveValue("1");
      expect((seedInput as HTMLInputElement).value).toBe(seedCode);
    });
  });

  it("starts with the applied seed even if a dirty draft is still staged", async () => {
    const user = userEvent.setup();
    const { onStartGame } = renderPanel();

    const dirtySeed = await encodeGameSetupSeed(
      withLoadoutProfile(
        withJourneyRandomnessMode(withStartWithHorse(withDifficulty(createCanonicalSeedState(), 2), false), 1),
        2,
      ),
    );
    const canonicalSeedCode = await encodeGameSetupSeed(createCanonicalSeedState());

    const seedInput = await screen.findByLabelText(/setup seed/i);

    await waitFor(() => {
      expect(seedInput).toHaveValue(canonicalSeedCode);
    });

    await user.clear(seedInput);
    await user.type(seedInput, dirtySeed);

    await user.type(screen.getByLabelText(/player name/i), "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /start new game/i }));

    await waitFor(() => {
      expect(onStartGame).toHaveBeenCalledTimes(1);
    });

    const [request] = onStartGame.mock.calls[0];
    expect(request.seedCode).toBe(canonicalSeedCode);
    expect(request.travelDifficulty).toBe(0);
  });

  it("rewrites the seed when the visible options change", async () => {
    const user = userEvent.setup();
    renderPanel();

    const seedInput = await screen.findByLabelText(/setup seed/i);
    const initialCode = (seedInput as HTMLInputElement).value;

    await user.selectOptions(screen.getByLabelText(/difficulty/i), "2");
    await user.click(screen.getByLabelText(/start with horse/i));
    await user.selectOptions(screen.getByLabelText(/loadout profile/i), "2");
    await user.selectOptions(screen.getByLabelText(/journey randomness/i), "1");

    await waitFor(() => {
      expect((seedInput as HTMLInputElement).value).not.toBe(initialCode);
      expect(screen.getByLabelText(/difficulty/i)).toHaveValue("2");
      expect(screen.getByLabelText(/start with horse/i)).not.toBeChecked();
      expect(screen.getByLabelText(/loadout profile/i)).toHaveValue("2");
      expect(screen.getByLabelText(/journey randomness/i)).toHaveValue("1");
    });
  });

  it("randomizes the seed without losing the selected v1 options", async () => {
    const user = userEvent.setup();
    const { onStartGame } = renderPanel();

    const seedInput = await screen.findByLabelText(/setup seed/i);
    await user.selectOptions(screen.getByLabelText(/difficulty/i), "1");
    await user.click(screen.getByLabelText(/start with horse/i));
    await user.selectOptions(screen.getByLabelText(/loadout profile/i), "1");
    await user.selectOptions(screen.getByLabelText(/journey randomness/i), "1");

    const beforeRandomize = (seedInput as HTMLInputElement).value;
    await user.click(screen.getByRole("button", { name: /randomize seed/i }));

    await waitFor(() => {
      expect(mockedWithRandomEntropy).toHaveBeenCalled();
      expect((seedInput as HTMLInputElement).value).not.toBe(beforeRandomize);
      expect(screen.getByLabelText(/difficulty/i)).toHaveValue("1");
      expect(screen.getByLabelText(/start with horse/i)).not.toBeChecked();
      expect(screen.getByLabelText(/loadout profile/i)).toHaveValue("1");
      expect(screen.getByLabelText(/journey randomness/i)).toHaveValue("1");
    });

    await user.type(screen.getByLabelText(/player name/i), "Ranger Vale");
    await user.click(screen.getByRole("button", { name: /start new game/i }));

    await waitFor(() => {
      expect(onStartGame).toHaveBeenCalledTimes(1);
    });

    const [request] = onStartGame.mock.calls[0];
    const decoded = await decodeGameSetupSeed(request.seedCode);

    expect(request.seedCode).toBe((seedInput as HTMLInputElement).value);
    expect(decoded.difficulty).toBe(1);
    expect(decoded.startWithHorse).toBe(false);
    expect(decoded.loadoutProfile).toBe(1);
    expect(decoded.journeyRandomnessMode).toBe(1);
    expect(decoded.entropy).toBe(1234n);
  });
});
