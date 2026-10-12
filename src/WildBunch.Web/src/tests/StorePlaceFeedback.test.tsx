import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import { QueryClient, QueryClientProvider, useQuery } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { act, cleanup, render, screen, waitFor, within } from "@testing-library/react";
import type { ReactNode } from "react";
import userEvent from "@testing-library/user-event";
import { createAppRouter } from "../shell/router";
import { GameSessionContext, GameSessionProvider } from "../state/GameSessionProvider";
import { useGameSession } from "../state/useGameSession";
import {
  AvailableActionKind,
  type GameSessionDto,
  type GameTurnResultDto,
  type JournalDto,
  type TownStoreOffersDto,
} from "../api/types";
import {
  archiveGame,
  buyStoreItem,
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
  gatherLocalGossip,
  confrontSaloonPersonOfInterest,
  lookAroundSaloon,
  readWantedPosters,
  travel,
  setupGame,
  startGameWithTown,
  markPrologueViewed,
} from "../api/wildBunchApi";
import { getSessionAudit } from "../dev/devApi";
import { gameResourceQueryKeys } from "../state/gameResourceQueries";

vi.mock("../api/wildBunchApi", () => ({
  archiveGame: vi.fn(),
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
  setupGame: vi.fn(),
  startGameWithTown: vi.fn(),
  markPrologueViewed: vi.fn(),
}));

vi.mock("../dev/devApi", () => ({
  getSessionAudit: vi.fn(),
}));

const mockedArchiveGame = vi.mocked(archiveGame);
const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetTownStoreOffers = vi.mocked(getTownStoreOffers);
const mockedBuyStoreItem = vi.mocked(buyStoreItem);
const mockedSetupGame = vi.mocked(setupGame);
const mockedStartGameWithTown = vi.mocked(startGameWithTown);
const mockedMarkPrologueViewed = vi.mocked(markPrologueViewed);
const mockedConfrontSaloonPersonOfInterest = vi.mocked(confrontSaloonPersonOfInterest);
const mockedLookAroundSaloon = vi.mocked(lookAroundSaloon);
const mockedReadWantedPosters = vi.mocked(readWantedPosters);
const mockedGatherLocalGossip = vi.mocked(gatherLocalGossip);
const mockedTravel = vi.mocked(travel);
const mockedGetSessionAudit = vi.mocked(getSessionAudit);

beforeAll(async () => {
  // Preload the lazy store component so the route resolves quickly during
  // full-suite runs where the module has not been imported by another test.
  await import("../flow/places/StorePlace");
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  refreshProbeCalls.length = 0;
  window.localStorage.clear();
  window.history.replaceState({}, "", "/");
});

function renderShell(extraContent: ReactNode = null) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <RouterProvider router={createAppRouter()} />
        {extraContent}
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

function SwitchablePlaythrough({ children }: { children: ReactNode }) {
  const initialValue = useGameSession();
  const [switched, setSwitched] = useState(false);
  const gameTwoSession = createSession();
  gameTwoSession.id = "game-2";
  gameTwoSession.player.currentTownId = "other-town";
  const value = switched
    ? {
        ...initialValue,
        gameId: "game-2",
        currentTown: { id: "other-town", name: "Dust Fork" },
        session: gameTwoSession,
      }
    : initialValue;
  return (
    <GameSessionContext.Provider value={value}>
      <button type="button" onClick={() => setSwitched(true)}>
        Switch playthrough
      </button>
      {children}
    </GameSessionContext.Provider>
  );
}

function renderSwitchableShell(extraContent: ReactNode = null) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <SwitchablePlaythrough>
          <RouterProvider router={createAppRouter()} />
          {extraContent}
        </SwitchablePlaythrough>
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

function RefreshProbe({ gameId }: { gameId: string }) {
  const { data } = useQuery({
    queryKey: gameResourceQueryKeys.devSession(gameId),
    queryFn: async () => {
      const count = refreshProbeCalls.filter((id) => id === gameId).length + 1;
      refreshProbeCalls.push(gameId);
      return `${gameId} context ${count}`;
    },
  });
  return <output>{data}</output>;
}

const refreshProbeCalls: string[] = [];

function createSession(): GameSessionDto {
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

function createStoreOffers(): TownStoreOffersDto {
  return {
    townId: "t-town",
    townName: "Tumbleweed",
    offers: [
      {
        itemKind: 0,
        displayName: "Canned beans",
        price: 1.5,
      },
    ],
  };
}

function primeMocks() {
  mockedGetGame.mockResolvedValue(createSession());
  mockedGetAvailableActions.mockResolvedValue([
    { kind: AvailableActionKind.BuySupplies, label: "Buy supplies" },
  ]);
  mockedGetJournal.mockResolvedValue(createJournal());
  mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
  mockedArchiveGame.mockResolvedValue(undefined);
  mockedBuyStoreItem.mockResolvedValue({
    success: true,
    message: "You bought 1 canned beans for $1.50.",
    currentSession: createSession(),
    journeyStatus: null,
    journey: null,
    trailEvent: null,
  });
  mockedSetupGame.mockResolvedValue(createSession());
  mockedStartGameWithTown.mockResolvedValue(createSession());
  mockedMarkPrologueViewed.mockResolvedValue(createSession());
  mockedReadWantedPosters.mockResolvedValue({
    success: true,
    message: "Read wanted posters",
    currentJournal: createJournal(),
    wantedPosters: [],
  });
  mockedGatherLocalGossip.mockResolvedValue({
    success: true,
    message: "ok",
    currentJournal: createJournal(),
  });
  mockedLookAroundSaloon.mockResolvedValue({
    success: true,
    message: "ok",
    currentJournal: createJournal(),
  });
  mockedConfrontSaloonPersonOfInterest.mockResolvedValue({
    success: true,
    message: "ok",
    outcome: 0,
    currentSession: createSession(),
    declaredWantedIdentityHandle: null,
    targetName: null,
    disposition: null,
    isAlive: null,
    isSecured: null,
    isCitizen: null,
    fineAmount: null,
    walletBefore: null,
    walletAfter: null,
    sessionChanged: false,
    personOfInterestKind: 0,
  });
  mockedTravel.mockResolvedValue({
    success: true,
    message: "Travelled",
    currentSession: createSession(),
    journeyStatus: null,
    journey: null,
    trailEvent: null,
  });
  mockedGetSessionAudit.mockResolvedValue({ sessionId: "game-1", entries: [] });
}

describe("Store purchase feedback", () => {
  it("loads town offers only after entering the Store route", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    window.history.replaceState({}, "", "/town");

    renderShell();

    const user = userEvent.setup();
    await screen.findByRole("heading", { name: "Tumbleweed" });
    expect(mockedGetTownStoreOffers).not.toHaveBeenCalled();

    await user.click(screen.getByRole("button", { name: "Store" }));

    expect(await screen.findByText("Canned beans")).toBeInTheDocument();
    expect(mockedGetTownStoreOffers).toHaveBeenCalledWith("game-1", "t-town");
  });

  it("shows the purchase confirmation notice on the store surface after a successful buy", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    // The TownHubSurface now renders a Phaser canvas instead of place cards,
    // so navigate directly to the store route rather than clicking a card.
    window.history.replaceState({}, "", "/town/store");

    renderShell();

    const user = userEvent.setup();

    // The store surface should render with the buy button.
    // StorePlace is lazy-loaded via React.lazy, so under full-suite load
    // it may take longer than the default 1000ms findByRole timeout.
    const buyButton = await screen.findByRole("button", { name: /^buy$/i }, { timeout: 10000 });
    const quantityInput = screen.getByRole("spinbutton", { name: "Quantity for Canned beans" });
    await user.clear(quantityInput);
    await user.type(quantityInput, "3");
    mockedBuyStoreItem.mockResolvedValueOnce({
      success: true,
      message: "You bought 3 canned beans for $4.50.",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
    });
    await user.click(buyButton);

    expect(mockedBuyStoreItem).toHaveBeenCalledWith("game-1", "t-town", {
      itemKind: 0,
      quantity: 3,
    });

    // The purchase confirmation notice should appear.
    await waitFor(() => {
      expect(screen.getByText("You bought 3 canned beans for $4.50.")).toBeInTheDocument();
    });
  });

  it("disables duplicate purchase submission until the command and resource refresh settle", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    window.history.replaceState({}, "", "/town/store");

    let resolvePurchase!: (result: GameTurnResultDto) => void;
    mockedBuyStoreItem.mockImplementation(
      () => new Promise((resolve) => (resolvePurchase = resolve)),
    );

    renderShell();

    const user = userEvent.setup();
    const buyButton = await screen.findByRole("button", { name: /^buy$/i });
    await user.click(buyButton);

    await waitFor(() => expect(mockedBuyStoreItem).toHaveBeenCalledTimes(1));
    expect(buyButton).toBeDisabled();
    await user.click(buyButton);
    expect(mockedBuyStoreItem).toHaveBeenCalledTimes(1);

    await act(async () => {
      resolvePurchase({
        success: true,
        message: "You bought 1 canned beans for $1.50.",
        currentSession: createSession(),
        journeyStatus: null,
        journey: null,
        trailEvent: null,
      });
    });

    expect(await screen.findByText("You bought 1 canned beans for $1.50.")).toBeInTheDocument();
    await waitFor(() => expect(buyButton).toBeEnabled());
  });

  it("reconciles an unsuccessful purchase result and refreshes only that game's resources", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    window.history.replaceState({}, "", "/town/store");
    const updatedSession = createSession();
    updatedSession.inventory!.wallet.cash = 7;
    mockedGetGame.mockResolvedValueOnce(createSession()).mockResolvedValue(updatedSession);
    mockedBuyStoreItem.mockResolvedValueOnce({
      success: false,
      message: "You cannot afford that.",
      currentSession: updatedSession,
      journeyStatus: null,
      journey: null,
      trailEvent: null,
    });

    renderShell(
      <>
        <RefreshProbe gameId="game-1" />
        <RefreshProbe gameId="game-2" />
      </>,
    );

    const user = userEvent.setup();
    await screen.findByText("game-1 context 1");
    await screen.findByText("game-2 context 1");
    await user.click(await screen.findByRole("button", { name: /^buy$/i }));

    expect(await screen.findByText("You cannot afford that.")).toBeInTheDocument();
    expect(await screen.findByText("game-1 context 2")).toBeInTheDocument();
    expect(screen.getByText("game-2 context 1")).toBeInTheDocument();
    expect(screen.getByText("Inventory").closest("section")).toHaveTextContent("$7.00");
    expect(refreshProbeCalls.filter((gameId) => gameId === "game-1")).toHaveLength(2);
    expect(refreshProbeCalls.filter((gameId) => gameId === "game-2")).toHaveLength(1);
  });

  it("keeps a delayed purchase with its captured playthrough after switching games", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    window.history.replaceState({}, "", "/town/store");
    const updatedSession = createSession();
    updatedSession.inventory!.wallet.cash = 10;
    mockedGetGame.mockResolvedValueOnce(createSession()).mockResolvedValue(updatedSession);

    let resolvePurchase!: (result: GameTurnResultDto) => void;
    mockedBuyStoreItem.mockImplementation(
      () => new Promise((resolve) => (resolvePurchase = resolve)),
    );

    renderSwitchableShell(
      <>
        <RefreshProbe gameId="game-1" />
        <RefreshProbe gameId="game-2" />
      </>,
    );

    const user = userEvent.setup();
    await screen.findByText("Canned beans");
    await screen.findByText("game-1 context 1");
    await screen.findByText("game-2 context 1");
    await user.click(screen.getByRole("button", { name: /^buy$/i }));
    expect(mockedBuyStoreItem).toHaveBeenCalledWith("game-1", "t-town", {
      itemKind: 0,
      quantity: 1,
    });

    await user.click(screen.getByRole("button", { name: "Switch playthrough" }));
    await waitFor(() => {
      expect(mockedGetTownStoreOffers).toHaveBeenCalledWith("game-2", "other-town");
    });
    expect(screen.getByText("Inventory").closest("section")).toHaveTextContent("$14.00");

    await act(async () => {
      resolvePurchase({
        success: true,
        message: "You bought 1 canned beans for $1.50.",
        currentSession: updatedSession,
        journeyStatus: null,
        journey: null,
        trailEvent: null,
      });
    });

    await screen.findByText("game-1 context 2");
    expect(screen.getByText("game-2 context 1")).toBeInTheDocument();
    expect(screen.queryByText("You bought 1 canned beans for $1.50.")).toBeNull();
    expect(screen.getByText("Inventory").closest("section")).toHaveTextContent("$14.00");
    expect(refreshProbeCalls.filter((gameId) => gameId === "game-1")).toHaveLength(2);
    expect(refreshProbeCalls.filter((gameId) => gameId === "game-2")).toHaveLength(1);
  });

  it("hides a cached catalog when the purchase refresh cannot reload it", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    window.history.replaceState({}, "", "/town/store");
    mockedGetTownStoreOffers
      .mockResolvedValueOnce(createStoreOffers())
      .mockRejectedValueOnce(new Error("Store offers are unavailable."));

    renderShell();

    const user = userEvent.setup();
    await screen.findByText("Canned beans");
    await user.click(screen.getByRole("button", { name: /^buy$/i }));

    expect(await screen.findByText("Town catalog unavailable.")).toBeInTheDocument();
    expect(screen.queryByRole("spinbutton", { name: "Quantity for Canned beans" })).toBeNull();
    expect(mockedGetTownStoreOffers).toHaveBeenCalledTimes(2);
  });

  it.each([
    ["blank", ""],
    ["zero", "0"],
    ["negative", "-1"],
    ["fractional", "1.5"],
  ])("rejects a %s quantity before sending a purchase request", async (_label, value) => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    window.history.replaceState({}, "", "/town/store");

    renderShell();

    const user = userEvent.setup();
    const quantityInput = await screen.findByRole("spinbutton", {
      name: "Quantity for Canned beans",
    });
    await user.clear(quantityInput);
    if (value) {
      await user.type(quantityInput, value);
    }
    await user.click(screen.getByRole("button", { name: /^buy$/i }));

    expect(mockedBuyStoreItem).not.toHaveBeenCalled();
    expect(screen.getByText("Enter a whole number of at least 1.")).toBeInTheDocument();
  });
});

describe("Archive notice clears on new game setup", () => {
  it("clears the archive notice when a new game is set up", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();

    // Archive the playthrough via Game Settings.
    const hud = await screen.findByRole("banner", { name: /game status/i });
    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
    });
    await user.click(within(hud).getByRole("button", { name: /game settings/i }));
    const settingsOverlay = await screen.findByRole("dialog", { name: /game settings/i });
    await user.click(within(settingsOverlay).getByRole("button", { name: /start over/i }));
    const confirmDialog = await screen.findByRole("dialog", { name: /start over\?/i });
    await user.click(
      within(confirmDialog).getByRole("button", { name: /archive and start over/i }),
    );

    // The archive notice should appear on the pre-session surface.
    await waitFor(() => {
      expect(
        screen.getByText(
          "Your old playthrough has been archived. Start a new one when you are ready.",
        ),
      ).toBeInTheDocument();
    });

    // Now set up a new game. Enter a player name and continue.
    const nameInput = screen.getByLabelText(/your name/i);
    await user.type(nameInput, "Jesse");
    await user.click(screen.getByRole("button", { name: /ride on/i }));

    // Wait for setupGame to be called.
    await waitFor(() => {
      expect(mockedSetupGame).toHaveBeenCalled();
    });

    // The archive notice should be cleared — it should no longer be in the document.
    await waitFor(() => {
      expect(
        screen.queryByText(
          "Your old playthrough has been archived. Start a new one when you are ready.",
        ),
      ).not.toBeInTheDocument();
    });
  });
});
