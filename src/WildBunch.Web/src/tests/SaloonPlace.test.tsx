import { useState } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { SaloonPlace } from "../flow/places/SaloonPlace";
import { SheriffPlace } from "../flow/places/SheriffPlace";
import {
  confrontSaloonPersonOfInterest,
  getAvailableActions,
  getGame,
  getJournal,
  lookAroundSaloon,
} from "../api/wildBunchApi";
import { AvailableActionKind } from "../api/types";
import type { GameSessionDto, JournalDto, WantedPosterDto } from "../api/types";
import { gameSessionQueryKey } from "../state/gameSessionQuery";
import { gameResourceQueryKeys } from "../state/gameResourceQueries";

const mockNavigate = vi.fn();

vi.mock("@tanstack/react-router", () => ({
  useNavigate: () => mockNavigate,
}));

vi.mock("../api/wildBunchApi", () => ({
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  archiveGame: vi.fn(),
  buyStoreItem: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  gatherLocalGossip: vi.fn(),
  getAvailableActions: vi.fn(),
  getGame: vi.fn(),
  getJournal: vi.fn(),
  getTownStoreOffers: vi.fn(),
  lookAroundSaloon: vi.fn(),
  markPrologueViewed: vi.fn(),
  previewTravel: vi.fn(),
  readWantedPosters: vi.fn(),
  resolveTravelEncounter: vi.fn(),
  setupGame: vi.fn(),
  startGameWithTown: vi.fn(),
  travel: vi.fn(),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

const poster: WantedPosterDto = {
  posterId: "poster-mira",
  targetDisplayName: "Mira Cline",
} as WantedPosterDto;

const otherPoster: WantedPosterDto = {
  posterId: "poster-ada",
  targetDisplayName: "Ada Vale",
} as WantedPosterDto;

function createSession(wantedPosters: WantedPosterDto[] = []): GameSessionDto {
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
    activeSaloonPersonOfInterest: {
      descriptor: "a stranger with a raven-feather pin",
      kind: 1,
    },
    wantedPosters,
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

function renderProviderBackedSaloon(
  wantedPosters: WantedPosterDto[] = [],
  actions: Array<{ kind: AvailableActionKind; label: string }> = [],
) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
  vi.mocked(getGame).mockResolvedValue(createSession(wantedPosters));
  vi.mocked(getAvailableActions).mockResolvedValue(actions);
  vi.mocked(getJournal).mockResolvedValue(createJournal());

  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <SaloonPlace />
      </GameSessionProvider>
    </QueryClientProvider>,
  );

  return queryClient;
}

function SwitchableSaloonAndSheriff() {
  const [showSheriff, setShowSheriff] = useState(false);

  return (
    <>
      <button type="button" onClick={() => setShowSheriff(true)}>
        Switch to Sheriff Office
      </button>
      {showSheriff ? <SheriffPlace /> : <SaloonPlace />}
    </>
  );
}

function renderSwitchableProviderBackedSaloon() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
  vi.mocked(getGame).mockResolvedValue(createSession());
  vi.mocked(getAvailableActions).mockResolvedValue([
    { kind: AvailableActionKind.LookAroundSaloon, label: "Look around" },
  ]);
  vi.mocked(getJournal).mockResolvedValue(createJournal());

  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <SwitchableSaloonAndSheriff />
      </GameSessionProvider>
    </QueryClientProvider>,
  );

  return queryClient;
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise;
    reject = rejectPromise;
  });
  return { promise, resolve, reject };
}

describe("SaloonPlace", () => {
  it("does not leak a pending Saloon result into the Sheriff route", async () => {
    const user = userEvent.setup();
    const queryClient = renderSwitchableProviderBackedSaloon();
    const response = deferred<Awaited<ReturnType<typeof lookAroundSaloon>>>();
    vi.mocked(lookAroundSaloon).mockReturnValue(response.promise);

    await user.click(await screen.findByRole("button", { name: "Look around" }));
    await user.click(screen.getByRole("button", { name: "Switch to Sheriff Office" }));
    expect(screen.getByRole("heading", { name: "Sheriff Office" })).toBeInTheDocument();

    response.resolve({
      success: true,
      message: "Saloon-only clue discovery result.",
      currentJournal: createJournal(),
    });

    await waitFor(() =>
      expect(
        queryClient
          .getMutationCache()
          .getAll()
          .some((mutation) => mutation.state.status === "success"),
      ).toBe(true),
    );
    expect(vi.mocked(lookAroundSaloon)).toHaveBeenCalledWith("game-1");
    expect(queryClient.getQueryData(gameResourceQueryKeys.journal("game-1"))).toEqual(
      createJournal(),
    );
    expect(vi.mocked(getJournal).mock.calls.length).toBeGreaterThan(1);
    expect(screen.queryByText("Saloon-only clue discovery result.")).not.toBeInTheDocument();
  });

  it("requires the player to declare an identity and displays the server outcome", async () => {
    const user = userEvent.setup();
    renderProviderBackedSaloon([poster]);
    vi.mocked(confrontSaloonPersonOfInterest).mockResolvedValue({
      success: false,
      message: "They refuse to be taken in without a gun.",
      outcome: 0,
      currentSession: createSession([poster]),
      declaredWantedIdentityHandle: poster.posterId,
      targetName: "Mira Cline",
      disposition: null,
      isAlive: null,
      isSecured: false,
      isCitizen: false,
      fineAmount: 5,
      walletBefore: 14,
      walletAfter: 9,
      sessionChanged: true,
    });

    const identity = await screen.findByRole("combobox", { name: "Declare wanted identity" });
    const takeIn = await screen.findByRole("button", {
      name: /Take a stranger with a raven-feather pin to sheriff/,
    });

    expect(identity).toHaveValue("");
    expect(takeIn).toBeDisabled();

    await user.selectOptions(identity, "poster-mira");

    const declaredTakeIn = screen.getByRole("button", {
      name: "Take a stranger with a raven-feather pin to sheriff as Mira Cline",
    });
    expect(declaredTakeIn).toBeEnabled();

    await user.click(declaredTakeIn);

    expect(confrontSaloonPersonOfInterest).toHaveBeenCalledWith("game-1", poster.posterId);
    expect(
      await screen.findByText("They refuse to be taken in without a gun. Wallet $14.00 -> $9.00."),
    ).toBeInTheDocument();
  });

  it("disables competing Saloon commands while a command is pending", async () => {
    const user = userEvent.setup();
    renderProviderBackedSaloon(
      [],
      [
        { kind: AvailableActionKind.LookAroundSaloon, label: "Look around" },
        { kind: AvailableActionKind.GatherLocalGossip, label: "Gather gossip" },
      ],
    );
    const response = deferred<Awaited<ReturnType<typeof lookAroundSaloon>>>();
    vi.mocked(lookAroundSaloon).mockReturnValue(response.promise);

    const lookAroundButton = await screen.findByRole("button", { name: "Look around" });
    await user.click(lookAroundButton);
    expect(await screen.findByRole("button", { name: "Looking..." })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Gather gossip" })).toBeDisabled();
    await user.click(lookAroundButton);
    expect(lookAroundSaloon).toHaveBeenCalledTimes(1);

    response.resolve({
      success: true,
      message: "You scan the room.",
      currentJournal: createJournal(),
    });
    await screen.findByText("You scan the room.");
  });

  it("shows a request error for a rejected permitted Saloon command", async () => {
    const user = userEvent.setup();
    renderProviderBackedSaloon(
      [],
      [{ kind: AvailableActionKind.LookAroundSaloon, label: "Look around" }],
    );
    vi.mocked(lookAroundSaloon).mockRejectedValue(new Error("The saloon is unavailable."));

    await user.click(await screen.findByRole("button", { name: "Look around" }));

    expect(await screen.findByText("The saloon is unavailable.")).toBeInTheDocument();
  });

  it("does not issue a Saloon command when the server says it is unavailable", async () => {
    renderProviderBackedSaloon();

    expect(await screen.findByRole("button", { name: "Look around" })).toBeDisabled();
    expect(lookAroundSaloon).not.toHaveBeenCalled();
  });

  it("does not imply an identity when the session query reveals wanted posters", async () => {
    const user = userEvent.setup();
    const queryClient = renderProviderBackedSaloon();

    await waitFor(() => expect(vi.mocked(getGame)).toHaveBeenCalledWith("game-1"));
    act(() => {
      queryClient.setQueryData(gameSessionQueryKey("game-1"), createSession([poster]));
    });

    const identity = await screen.findByRole("combobox", { name: "Declare wanted identity" });
    const takeIn = screen.getByRole("button", {
      name: /Take a stranger with a raven-feather pin to sheriff/,
    });
    expect(identity).toHaveValue("");
    expect(takeIn).toBeDisabled();

    await user.selectOptions(identity, poster.posterId);
    expect(
      screen.getByRole("button", {
        name: "Take a stranger with a raven-feather pin to sheriff as Mira Cline",
      }),
    ).toBeEnabled();

    act(() => {
      queryClient.setQueryData(gameSessionQueryKey("game-1"), createSession([otherPoster]));
    });

    await waitFor(() => expect(identity).toHaveValue(""));
    expect(takeIn).toBeDisabled();
  });
});
