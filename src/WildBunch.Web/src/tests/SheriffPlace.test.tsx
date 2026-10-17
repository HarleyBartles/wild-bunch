import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  RouterProvider,
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
} from "@tanstack/react-router";
import { useNavigate } from "@tanstack/react-router";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SheriffPlace } from "../flow/places/SheriffPlace";
import { TravelPrepSurface } from "../flow/TravelPrepSurface";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { gameResourceQueryKeys } from "../state/gameResourceQueries";
import {
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
  getWorldMap,
  previewTravel,
  readWantedPosters,
} from "../api/wildBunchApi";
import { AvailableActionKind } from "../api/types";
import type { GameSessionDto, JournalDto, WantedPostersResultDto } from "../api/types";

vi.mock("../api/wildBunchApi", () => ({
  buyStoreItem: vi.fn(),
  getAvailableActions: vi.fn(),
  getGame: vi.fn(),
  getJournal: vi.fn(),
  getTownStoreOffers: vi.fn(),
  getWorldMap: vi.fn(),
  previewTravel: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  lookAroundSaloon: vi.fn(),
  readWantedPosters: vi.fn(),
  gatherLocalGossip: vi.fn(),
  travel: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  resolveTravelEncounter: vi.fn(),
}));

vi.mock("../components/start-flow/PhaserMapHost", () => ({
  PhaserMapHost: ({ onTownSelected }: { onTownSelected: (townId: string) => void }) => (
    <button type="button" onClick={() => onTownSelected("dust-fork")}>
      Select Dust Fork
    </button>
  ),
}));

const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetTownStoreOffers = vi.mocked(getTownStoreOffers);
const mockedReadWantedPosters = vi.mocked(readWantedPosters);

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((resolvePromise) => {
    resolve = resolvePromise;
  });
  return { promise, resolve };
}

function TrailheadNavigation() {
  const navigate = useNavigate();
  return (
    <button type="button" onClick={() => void navigate({ to: "/town/trailhead" })}>
      Go to trailhead
    </button>
  );
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function createSession(): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
    startFlowPhase: 4,
    player: { name: "Ruth", currentTownId: "t-town", health: 9 },
    world: {
      towns: [
        { id: "t-town", name: "Tumbleweed" },
        { id: "dust-fork", name: "Dust Fork" },
      ],
      trails: [
        {
          id: "trail-1",
          fromTownId: "t-town",
          toTownId: "dust-fork",
          risk: 1,
          terrain: 0,
          waterFeature: 0,
          rideDayDistance: 1,
        },
      ],
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

function renderSheriffPlace() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
  const rootRoute = createRootRoute({ component: () => <Outlet /> });
  const sheriffRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/town/sheriff",
    component: () => <SheriffPlace />,
  });
  const router = createRouter({ routeTree: rootRoute.addChildren([sheriffRoute]) });
  window.history.replaceState({}, "", "/town/sheriff");
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <RouterProvider router={router} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}

function renderSheriffAndTrailhead() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
  const rootRoute = createRootRoute({
    component: () => (
      <>
        <TrailheadNavigation />
        <Outlet />
      </>
    ),
  });
  const sheriffRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/town/sheriff",
    component: () => <SheriffPlace />,
  });
  const trailheadRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/town/trailhead",
    component: () => <TravelPrepSurface />,
  });
  const router = createRouter({ routeTree: rootRoute.addChildren([sheriffRoute, trailheadRoute]) });
  window.history.replaceState({}, "", "/town/sheriff");
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <RouterProvider router={router} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return queryClient;
}

describe("SheriffPlace", () => {
  it("does not leak a pending poster result into the Trailhead route", async () => {
    const user = userEvent.setup();
    const journal = createJournal();
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
    ]);
    mockedGetJournal.mockResolvedValue(journal);
    mockedGetTownStoreOffers.mockResolvedValue({
      townId: "t-town",
      townName: "Tumbleweed",
      offers: [],
    });
    vi.mocked(getWorldMap).mockResolvedValue({
      towns: [
        { id: "t-town", name: "Tumbleweed", x: 0, y: 0 },
        { id: "dust-fork", name: "Dust Fork", x: 1, y: 0 },
      ],
      trails: [{ id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", rideDayDistance: 1 }],
    });
    vi.mocked(previewTravel).mockResolvedValue({} as Awaited<ReturnType<typeof previewTravel>>);
    const response = deferred<WantedPostersResultDto>();
    mockedReadWantedPosters.mockReturnValue(response.promise);

    const queryClient = renderSheriffAndTrailhead();

    await user.click(await screen.findByRole("button", { name: /read wanted posters/i }));
    await user.click(screen.getByRole("button", { name: "Go to trailhead" }));
    expect(await screen.findByRole("heading", { name: "Hit the trail" })).toBeInTheDocument();
    await user.click(await screen.findByRole("button", { name: "Select Dust Fork" }));
    expect(await screen.findByRole("heading", { name: "Prepare to ride" })).toBeInTheDocument();

    response.resolve({
      success: false,
      message: "Sheriff-only poster result.",
      currentJournal: journal,
      wantedPosters: [],
    });

    await waitFor(() => expect(mockedReadWantedPosters).toHaveBeenCalledWith("game-1"));
    await waitFor(() =>
      expect(
        queryClient
          .getMutationCache()
          .getAll()
          .some((mutation) => mutation.state.status === "success"),
      ).toBe(true),
    );
    expect(queryClient.getQueryData(gameResourceQueryKeys.journal("game-1"))).toEqual(journal);
    expect(mockedGetJournal).toHaveBeenCalledWith("game-1");
    expect(mockedGetJournal.mock.calls.length).toBeGreaterThan(1);
    expect(screen.queryByText("Sheriff-only poster result.")).not.toBeInTheDocument();
  });

  it("keeps poster-read pending and result feedback on the Sheriff route", async () => {
    const user = userEvent.setup();
    const journal = createJournal();
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
    ]);
    mockedGetJournal.mockResolvedValue(journal);
    mockedGetTownStoreOffers.mockResolvedValue({
      townId: "t-town",
      townName: "Tumbleweed",
      offers: [],
    });
    const response = deferred<WantedPostersResultDto>();
    mockedReadWantedPosters.mockReturnValue(response.promise);

    renderSheriffPlace();

    const readButton = await screen.findByRole("button", { name: /read wanted posters/i });
    await user.click(readButton);
    expect(await screen.findByRole("button", { name: "Reading..." })).toBeDisabled();
    await user.click(readButton);
    expect(mockedReadWantedPosters).toHaveBeenCalledTimes(1);

    response.resolve({
      success: true,
      message: "You read the latest wanted posters.",
      currentJournal: journal,
      wantedPosters: [],
    });
    expect(await screen.findByText("You read the latest wanted posters.")).toBeInTheDocument();
  });

  it("does not call the poster-read command when the server marks it unavailable", async () => {
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([]);
    mockedGetJournal.mockResolvedValue(createJournal());
    mockedGetTownStoreOffers.mockResolvedValue({
      townId: "t-town",
      townName: "Tumbleweed",
      offers: [],
    });

    renderSheriffPlace();

    expect(await screen.findByRole("button", { name: /read wanted posters/i })).toBeDisabled();
    expect(mockedReadWantedPosters).not.toHaveBeenCalled();
  });

  it("shows a request error for a rejected permitted poster read", async () => {
    const user = userEvent.setup();
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
    ]);
    mockedGetJournal.mockResolvedValue(createJournal());
    mockedGetTownStoreOffers.mockResolvedValue({
      townId: "t-town",
      townName: "Tumbleweed",
      offers: [],
    });
    mockedReadWantedPosters.mockRejectedValue(new Error("The poster board is unavailable."));

    renderSheriffPlace();
    await user.click(await screen.findByRole("button", { name: /read wanted posters/i }));

    expect(await screen.findByText("The poster board is unavailable.")).toBeInTheDocument();
  });

  it("offers wanted posters and routes the read without showing fake records", async () => {
    const user = userEvent.setup();
    const journal = createJournal();
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
    ]);
    mockedGetJournal.mockResolvedValue(journal);
    mockedGetTownStoreOffers.mockResolvedValue({
      townId: "t-town",
      townName: "Tumbleweed",
      offers: [],
    });
    mockedReadWantedPosters.mockResolvedValue({
      success: true,
      message: "You read the wanted posters.",
      currentJournal: journal,
      wantedPosters: [],
    });

    renderSheriffPlace();

    const readButton = await screen.findByRole("button", { name: /read wanted posters/i });
    expect(screen.getByRole("heading", { name: "Sheriff Office" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /records/i })).not.toBeInTheDocument();
    expect(screen.queryByText(/local records/i)).not.toBeInTheDocument();

    await user.click(readButton);
    await waitFor(() => expect(mockedReadWantedPosters).toHaveBeenCalledWith("game-1"));
  });
});
