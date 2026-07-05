import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createAppRouter } from "../shell/router";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { AvailableActionKind, JourneyStatus, StartFlowPhase, type GameSessionDto, type JournalDto, type TownStoreOffersDto } from "../api/types";
import {
  acknowledgeTravelArrival,
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
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
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  resolveTravelEncounter: vi.fn(),
  getWorldMap: vi.fn(),
  getStartingTownMap: vi.fn(),
  setupGame: vi.fn(),
  markPrologueViewed: vi.fn(),
  startGameWithTown: vi.fn(),
  archiveGame: vi.fn(),
  getPrologue: vi.fn(),
  getStartingTowns: vi.fn(),
  previewTravel: vi.fn(),
}));

vi.mock("../dev/devApi", () => ({
  getSessionAudit: vi.fn(),
}));

const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetTownStoreOffers = vi.mocked(getTownStoreOffers);
const mockedBuyStoreItem = vi.mocked(buyStoreItem);
const mockedCheckLocalRecords = vi.mocked(checkLocalRecords);
const mockedInspectNoticeBoard = vi.mocked(inspectNoticeBoard);
const mockedConfrontSaloonPersonOfInterest = vi.mocked(confrontSaloonPersonOfInterest);
const mockedLookAroundSaloon = vi.mocked(lookAroundSaloon);
const mockedReadWantedPosters = vi.mocked(readWantedPosters);
const mockedFollowTelegraphLeads = vi.mocked(followTelegraphLeads);
const mockedGatherLocalGossip = vi.mocked(gatherLocalGossip);
const mockedTravel = vi.mocked(travel);
const mockedAcknowledgeTravelArrival = vi.mocked(acknowledgeTravelArrival);
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
    offers: [],
  };
}

function createOnTrailSession(): GameSessionDto {
  const session = createSession();
  session.journey = {
    originTownId: "t-town", originTownName: "Tumbleweed",
    destinationTownId: "dust-fork", destinationTownName: "Dust Fork",
    travelMode: 1, status: JourneyStatus.Active,
    mountedTravelAvailable: false, waterSecure: true,
    rideDayDistance: 3, remainingRideDayDistance: 2,
    baselineRideDays: 3, expectedDays: 3, remainingDays: 1,
    canteenChargesPerDay: 0, requiredCanteenCharges: 0,
    availableCanteenCharges: 0, canteenReserveCharges: 0,
    delayMarginDays: 0, delayRisk: false,
    requiredFood: 0, availableFood: 0,
    requiredHorseFeed: 0, availableHorseFeed: 0,
    horseState: null, daysTravelled: 1, delayDays: 0,
    pendingEncounter: null, warnings: [],
    routeProfile: { trailId: "trail-1", risk: 1, terrain: 0, waterFeature: 0, rideDayDistance: 3, mountedRideDayProgress: 1, footRideDayProgress: 0.5, warnings: [] },
  };
  session.world.trails = [
    { id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", risk: 1, terrain: 0, waterFeature: 0, rideDayDistance: 3 },
  ];
  return session;
}

function createCompletedJourneySession(): GameSessionDto {
  const session = createOnTrailSession();
  session.journey!.status = JourneyStatus.Completed;
  session.journey!.daysTravelled = 3;
  session.journey!.remainingDays = 0;
  session.journey!.remainingRideDayDistance = 0;
  return session;
}

function createInTownSessionAtDestination(): GameSessionDto {
  const session = createSession();
  session.player.currentTownId = "dust-fork";
  session.world.towns = [
    { id: "t-town", name: "Tumbleweed", services: 0 },
    { id: "dust-fork", name: "Dust Fork", services: 0 },
  ];
  return session;
}

function primeMocks() {
  mockedGetGame.mockResolvedValue(createSession());
  mockedGetAvailableActions.mockResolvedValue([
    { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
  ]);
  mockedGetJournal.mockResolvedValue(createJournal());
  mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
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
    expect(screen.getByRole("heading", { name: /set up your hunt/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /ride on/i })).toBeInTheDocument();
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

  it("syncs the dev surface to 'trail' when on /trail with an on-trail session (real AppShell wiring)", async () => {
    mockedGetGame.mockResolvedValue(createOnTrailSession());
    mockedGetAvailableActions.mockResolvedValue([]);
    mockedGetJournal.mockResolvedValue(createJournal());
    mockedGetSessionAudit.mockResolvedValue({ sessionId: "game-1", entries: [] });
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    window.history.replaceState({}, "", "/trail");

    renderShell();

    const user = userEvent.setup();
    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
    });

    await user.click(screen.getByRole("button", { name: /^dev$/i }));
    const drawer = await screen.findByRole("region", { name: /developer overlay/i });

    // The "Travel dev" panel only appears for "trail" or "trailhead" surfaces.
    // Its presence as the default active tab proves useDevSurfaceSync set the
    // surface to "trail" through the real AppShell provider boundary.
    const travelTab = within(drawer).getByRole("button", { name: /travel dev/i });
    expect(travelTab).toBeInTheDocument();
    expect(travelTab.getAttribute("aria-pressed")).toBe("true");
  });

  it("syncs the dev surface to 'store' when on /town/store with an in-town session (real AppShell wiring)", async () => {
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.BuySupplies, label: "Buy supplies" },
    ]);
    mockedGetJournal.mockResolvedValue(createJournal());
    mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
    mockedGetSessionAudit.mockResolvedValue({ sessionId: "game-1", entries: [] });
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    window.history.replaceState({}, "", "/town/store");

    renderShell();

    const user = userEvent.setup();
    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
    });

    await user.click(screen.getByRole("button", { name: /^dev$/i }));
    const drawer = await screen.findByRole("region", { name: /developer overlay/i });

    // "store" surface has no surface-owner panel, so only "Session audit" and
    // "Session dev" appear (no "Travel dev" or "Saloon dev"). This proves the
    // surface is NOT stuck at the default "trail" — it changed to "store".
    expect(within(drawer).queryByRole("button", { name: /travel dev/i })).not.toBeInTheDocument();
    expect(within(drawer).queryByRole("button", { name: /saloon dev/i })).not.toBeInTheDocument();
    expect(within(drawer).getByRole("button", { name: /session audit/i })).toBeInTheDocument();
  });

  it("shows the arrival notice after acknowledging a completed journey (trail → town?arrived=1)", async () => {
    // First getGame call returns the completed journey (on-trail phase).
    // Subsequent calls (after invalidateGameQueries refetch) return in-town.
    mockedGetGame.mockResolvedValueOnce(createCompletedJourneySession());
    mockedGetGame.mockResolvedValue(createInTownSessionAtDestination());
    mockedGetAvailableActions.mockResolvedValue([]);
    mockedGetJournal.mockResolvedValue(createJournal());
    mockedGetSessionAudit.mockResolvedValue({ sessionId: "game-1", entries: [] });
    mockedAcknowledgeTravelArrival.mockResolvedValue({
      success: true,
      message: "You step into town.",
      currentSession: createInTownSessionAtDestination(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    window.history.replaceState({}, "", "/trail");

    renderShell();

    const user = userEvent.setup();
    const stepButton = await screen.findByRole("button", { name: /step into town/i }, { timeout: 10000 });
    await user.click(stepButton);

    // After acknowledge, the session becomes in-town, usePhaseRouteSync
    // navigates to /town?arrived=1, and TownHubSurface shows the arrival notice.
    const arrivalNotice = await screen.findByRole("status", {}, { timeout: 10000 });
    expect(within(arrivalNotice).getByText(/you've arrived in dust fork/i)).toBeInTheDocument();

    // Verify the URL is /town with the arrived search param.
    // TanStack Router JSON-serializes string values, so "1" appears as %221%22.
    expect(window.location.pathname).toBe("/town");
    expect(window.location.search).toContain("arrived");
  }, 30000);
});
