import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createAppRouter } from "../shell/router";
import { GameSessionProvider } from "../state/GameSessionProvider";
import {
  AvailableActionKind,
  type GameSessionDto,
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
  checkLocalRecords,
  followTelegraphLeads,
  gatherLocalGossip,
  inspectNoticeBoard,
  confrontSaloonPersonOfInterest,
  lookAroundSaloon,
  readWantedPosters,
  travel,
  setupGame,
  startGameWithTown,
  markPrologueViewed,
} from "../api/wildBunchApi";
import { getSessionAudit } from "../dev/devApi";

vi.mock("../api/wildBunchApi", () => ({
  archiveGame: vi.fn(),
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
const mockedCheckLocalRecords = vi.mocked(checkLocalRecords);
const mockedInspectNoticeBoard = vi.mocked(inspectNoticeBoard);
const mockedConfrontSaloonPersonOfInterest = vi.mocked(confrontSaloonPersonOfInterest);
const mockedLookAroundSaloon = vi.mocked(lookAroundSaloon);
const mockedReadWantedPosters = vi.mocked(readWantedPosters);
const mockedFollowTelegraphLeads = vi.mocked(followTelegraphLeads);
const mockedGatherLocalGossip = vi.mocked(gatherLocalGossip);
const mockedTravel = vi.mocked(travel);
const mockedGetSessionAudit = vi.mocked(getSessionAudit);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
  window.history.replaceState({}, "", "/");
});

function renderShell() {
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
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

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
      caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] },
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
    available: true,
    sourceNote: "General store",
    offers: [
      {
        vendorType: 0,
        itemKind: 0,
        displayName: "Canned beans",
        price: 1.5,
        availability: 0,
        sourceNote: "Shelf stock",
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
    travelDiary: null,
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
  mockedInspectNoticeBoard.mockResolvedValue({ success: true, message: "ok", currentJournal: createJournal() });
  mockedCheckLocalRecords.mockResolvedValue({ success: true, message: "ok", currentJournal: createJournal() });
  mockedFollowTelegraphLeads.mockResolvedValue({ success: true, message: "ok", currentJournal: createJournal() });
  mockedGatherLocalGossip.mockResolvedValue({ success: true, message: "ok", currentJournal: createJournal() });
  mockedLookAroundSaloon.mockResolvedValue({ success: true, message: "ok", currentJournal: createJournal() });
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
    travelDiary: null,
  });
  mockedGetSessionAudit.mockResolvedValue({ sessionId: "game-1", entries: [] });
}

describe("Store purchase feedback", () => {
  it("shows the purchase confirmation notice on the store surface after a successful buy", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    // The TownHubSurface now renders a Phaser canvas instead of place cards,
    // so navigate directly to the store route rather than clicking a card.
    window.history.replaceState({}, "", "/town/store");

    renderShell();

    const user = userEvent.setup();

    // The store surface should render with the buy button.
    const buyButton = await screen.findByRole("button", { name: /^buy$/i });
    await user.click(buyButton);

    // The purchase confirmation notice should appear.
    await waitFor(() => {
      expect(screen.getByText("You bought 1 canned beans for $1.50.")).toBeInTheDocument();
    });
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
    await user.click(within(confirmDialog).getByRole("button", { name: /archive and start over/i }));

    // The archive notice should appear on the pre-session surface.
    await waitFor(() => {
      expect(
        screen.getByText("Your old playthrough has been archived. Start a new one when you are ready."),
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
        screen.queryByText("Your old playthrough has been archived. Start a new one when you are ready."),
      ).not.toBeInTheDocument();
    });
  });
});
