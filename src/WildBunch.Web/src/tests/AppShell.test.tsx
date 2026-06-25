import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { router } from "../shell/router";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { AvailableActionKind, type GameSessionDto, type JournalDto, type TownStoreOffersDto } from "../api/types";
import {
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
  createGame,
  buyStoreItem,
  checkLocalRecords,
  followTelegraphLeads,
  gatherLocalGossip,
  inspectNoticeBoard,
  confrontSaloonPersonOfInterest,
  lookAroundSaloon,
  readWantedPosters,
  travel,
} from "../api/wildBunchApi";
import { getSessionAudit } from "../dev/devApi";

vi.mock("../api/wildBunchApi", () => ({
  buyStoreItem: vi.fn(),
  createGame: vi.fn(),
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
}));

vi.mock("../dev/devApi", () => ({
  getSessionAudit: vi.fn(),
}));

const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetTownStoreOffers = vi.mocked(getTownStoreOffers);
const mockedCreateGame = vi.mocked(createGame);
const mockedBuyStoreItem = vi.mocked(buyStoreItem);
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
        <RouterProvider router={router} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

function createSession(): GameSessionDto {
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
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    pursuitState: { heat: 1 },
    journey: null,
    travelDiary: null,
    logEntries: [],
    activeSaloonPersonOfInterest: null,
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
    offers: [],
  };
}

function primeMocks() {
  mockedGetGame.mockResolvedValue(createSession());
  mockedGetAvailableActions.mockResolvedValue([
    { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
  ]);
  mockedGetJournal.mockResolvedValue(createJournal());
  mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
  mockedCreateGame.mockResolvedValue(createSession());
  mockedBuyStoreItem.mockResolvedValue({
    success: true,
    message: "Purchased",
    currentSession: createSession(),
    journeyStatus: null,
    journey: null,
    trailEvent: null,
    travelDiary: null,
  });
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

describe("AppShell", () => {
  it("renders the persistent HUD with player name and clock once a session hydrates", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const hud = await screen.findByRole("banner", { name: /game status/i });
    expect(within(hud).getByText("Ruth")).toBeInTheDocument();
    expect(within(hud).getByText("Day 5, Morning")).toBeInTheDocument();
  });

  it("defaults to the flow router and shows the pre-session surface", async () => {
    primeMocks();

    renderShell();

    expect(await screen.findByRole("heading", { name: /^wild bunch$/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /start new game/i })).toBeInTheDocument();
  });

  it("opens the Case file overlay and renders the case surface", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    await waitFor(() => {
      expect(mockedGetJournal).toHaveBeenCalledWith("game-1");
    });

    await user.click(screen.getByRole("button", { name: /^case file$/i }));

    expect(await screen.findByRole("heading", { name: /^case file$/i })).toBeInTheDocument();
    expect(screen.getByText(/player-known facts and does not guess at hidden truth/i)).toBeInTheDocument();
  });

  it("opens the Journal overlay and renders the journal surface", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    const hud = await screen.findByRole("banner", { name: /game status/i });

    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
    });

    await user.click(within(hud).getByRole("button", { name: /journal/i }));

    const journalDialog = await screen.findByRole("dialog", { name: /journal/i });
    const journalScope = within(journalDialog);
    expect(journalScope.getByRole("heading", { level: 2, name: /^journal$/i })).toBeInTheDocument();
    expect(journalScope.getByText("Day 5, Morning in Tumbleweed")).toBeInTheDocument();
    expect(journalScope.queryByText("Find the culprit before the law closes in.")).not.toBeInTheDocument();
  });

  it("shows a Dev toggle button that opens the developer overlay drawer", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
    });

    await user.click(screen.getByRole("button", { name: /^dev$/i }));

    expect(await screen.findByRole("region", { name: /developer overlay/i })).toBeInTheDocument();
  });

  it("closes the dev overlay on Escape", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
    });

    await user.click(screen.getByRole("button", { name: /^dev$/i }));
    const drawer = await screen.findByRole("region", { name: /developer overlay/i });
    expect(drawer).toBeInTheDocument();

    await user.keyboard("{Escape}");
    expect(screen.queryByRole("region", { name: /developer overlay/i })).not.toBeInTheDocument();
  });

  it("closes the dev overlay when clicking the play surface outside the drawer", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
    });

    await user.click(screen.getByRole("button", { name: /^dev$/i }));
    expect(await screen.findByRole("region", { name: /developer overlay/i })).toBeInTheDocument();

    const clickAway = screen.getByTestId("dev-click-away");
    await user.click(clickAway);

    expect(screen.queryByRole("region", { name: /developer overlay/i })).not.toBeInTheDocument();
  });

  it("keeps the game HUD visible above the dev drawer when the drawer is open", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
    });

    const hud = await screen.findByRole("banner", { name: /game status/i });
    await waitFor(() => {
      expect(within(hud).getByText("Ruth")).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /^dev$/i }));
    expect(await screen.findByRole("region", { name: /developer overlay/i })).toBeInTheDocument();

    expect(screen.getByRole("banner", { name: /game status/i })).toBeInTheDocument();
    expect(within(hud).getByText("Ruth")).toBeInTheDocument();
  });
});
