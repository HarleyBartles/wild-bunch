import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { router } from "../shell/router";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { AvailableActionKind, type GameSessionDto, type JournalDto, type TownStoreOffersDto } from "../api/types";
import {
  archiveGame,
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
  archiveGame: vi.fn(),
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

const mockedArchiveGame = vi.mocked(archiveGame);
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
  mockedArchiveGame.mockResolvedValue(undefined);
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

async function openConfirmDialog(user: ReturnType<typeof userEvent.setup>) {
  const hud = await screen.findByRole("banner", { name: /game status/i });

  await waitFor(() => {
    expect(mockedGetGame).toHaveBeenCalledWith("game-1");
  });

  await user.click(within(hud).getByRole("button", { name: /game settings/i }));
  const overlay = await screen.findByRole("dialog", { name: /game settings/i });

  await user.click(within(overlay).getByRole("button", { name: /start over/i }));

  return screen.getByRole("dialog", { name: /start over\?/i });
}

describe("Start Over confirmation", () => {
  it("calls archiveGame with the correct game id when Confirm is clicked", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    const confirmDialog = await openConfirmDialog(user);

    await user.click(within(confirmDialog).getByRole("button", { name: /archive and start over/i }));

    await waitFor(() => {
      expect(mockedArchiveGame).toHaveBeenCalledWith("game-1");
    });
  });

  it("clears localStorage and storedGameId on Confirm, returning to the start flow", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    const confirmDialog = await openConfirmDialog(user);

    await user.click(within(confirmDialog).getByRole("button", { name: /archive and start over/i }));

    // localStorage key is cleared.
    await waitFor(() => {
      expect(window.localStorage.getItem("wild-bunch.current-game-id")).toBeNull();
    });

    // The session is gone — the Game Settings button becomes disabled (no session).
    await waitFor(() => {
      const hud = screen.getByRole("banner", { name: /game status/i });
      expect(within(hud).getByRole("button", { name: /game settings/i })).toBeDisabled();
    });

    // The pre-session surface renders the start flow heading.
    expect(await screen.findByRole("heading", { name: /^wild bunch$/i })).toBeInTheDocument();
  });

  it("shows the success notice after archiving", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    const confirmDialog = await openConfirmDialog(user);

    await user.click(within(confirmDialog).getByRole("button", { name: /archive and start over/i }));

    await waitFor(() => {
      expect(
        screen.getByText("Your old playthrough has been archived. Start a new one when you are ready."),
      ).toBeInTheDocument();
    });
  });

  it("does not call archiveGame when Cancel is clicked", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    const confirmDialog = await openConfirmDialog(user);

    await user.click(within(confirmDialog).getByRole("button", { name: /keep riding/i }));

    await waitFor(() => {
      expect(screen.queryByRole("dialog", { name: /start over\?/i })).not.toBeInTheDocument();
    });

    expect(mockedArchiveGame).not.toHaveBeenCalled();
  });

  it("leaves session state unchanged when Cancel is clicked", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    const confirmDialog = await openConfirmDialog(user);

    await user.click(within(confirmDialog).getByRole("button", { name: /keep riding/i }));

    await waitFor(() => {
      expect(screen.queryByRole("dialog", { name: /start over\?/i })).not.toBeInTheDocument();
    });

    // localStorage still holds the game id.
    expect(window.localStorage.getItem("wild-bunch.current-game-id")).toBe("game-1");

    // The Game Settings overlay remains open and the session is still active.
    expect(screen.getByRole("dialog", { name: /game settings/i })).toBeInTheDocument();

    const hud = screen.getByRole("banner", { name: /game status/i });
    expect(within(hud).getByRole("button", { name: /game settings/i })).toBeEnabled();
  });
});
