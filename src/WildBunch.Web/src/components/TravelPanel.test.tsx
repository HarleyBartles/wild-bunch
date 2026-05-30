import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TravelPanel } from "./TravelPanel";
import { acknowledgeTravelArrival, advanceTravelDay, getGame, resolveTravelEncounter } from "../api/wildBunchApi";
import type { GameSessionDto, GameTurnResultDto } from "../api/types";

vi.mock("../api/wildBunchApi", () => ({
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  getGame: vi.fn(),
  resolveTravelEncounter: vi.fn(),
}));

const mockedAcknowledgeTravelArrival = vi.mocked(acknowledgeTravelArrival);
const mockedGetGame = vi.mocked(getGame);
const mockedAdvanceTravelDay = vi.mocked(advanceTravelDay);
const mockedResolveTravelEncounter = vi.mocked(resolveTravelEncounter);

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
      towns: [
        { id: "t-town", name: "Tumbleweed", services: 0 },
        { id: "dust-fork", name: "Dust Fork", services: 0 },
      ],
      trails: [],
    },
    caseFile: {
      accusationId: null,
      openingLead: "The trail went cold outside town.",
      killerReleaseState: {
        isReleased: false,
        progress: 0,
        requiredPublicClues: 3,
        statusText: "Still chasing leads.",
      },
      discoveredSuspects: [],
      knownClues: [],
    },
    inventory: {
      wallet: { cash: 14 },
      items: [],
      horseState: {
        hunger: 1,
        thirst: 1,
        exhaustion: 0,
        isLame: false,
        isDead: false,
        canProvideMountedTravel: true,
      },
      canteenState: {
        charges: 2,
        capacity: 3,
        hasWater: true,
      },
      capabilities: {
        mountedTravelAvailable: true,
        horseUpkeepRequired: true,
        normalRouteWaterSecure: true,
        trailUtility: false,
        closeThreatAvailable: false,
        firearmThreatAvailable: false,
        gunfightCapable: false,
        revolverUsable: false,
        rifleUsable: false,
      },
    },
    clock: { day: 5, turn: 2 },
    pursuitState: { heat: 1 },
    journey: {
      originTownId: "t-town",
      originTownName: "Tumbleweed",
      destinationTownId: "dust-fork",
      destinationTownName: "Dust Fork",
      travelMode: 0,
      status: 0,
      mountedTravelAvailable: true,
      waterSecure: true,
      rideDayDistance: 14,
      remainingRideDayDistance: 10,
      baselineRideDays: 3,
      expectedDays: 2,
      remainingDays: 2,
      canteenChargesPerDay: 1,
      requiredCanteenCharges: 2,
      availableCanteenCharges: 2,
      canteenReserveCharges: 0,
      delayMarginDays: 1,
      delayRisk: false,
      requiredFood: 2,
      availableFood: 2,
      requiredHorseFeed: 1,
      availableHorseFeed: 1,
      horseState: {
        hunger: 1,
        thirst: 1,
        exhaustion: 0,
        isLame: false,
        isDead: false,
        canProvideMountedTravel: true,
      },
      daysTravelled: 0,
      delayDays: 0,
      pendingEncounter: null,
      warnings: ["The ridge may blow up after dusk."],
      routeProfile: {
        trailId: "trail-1",
        risk: 2,
        terrain: 1,
        waterFeature: 1,
        rideDayDistance: 14,
        mountedRideDayProgress: 0,
        footRideDayProgress: 0,
        warnings: ["Keep an eye on the ridge line."],
      },
    },
    travelDiary: {
      days: [
        {
          dayNumber: 1,
          originTownName: "Tumbleweed",
          destinationTownName: "Dust Fork",
          startingTravelMode: 0,
          endingTravelMode: 0,
          status: 0,
          startingRideDayDistance: 14,
          remainingRideDayDistance: 10,
          startingDaysRemaining: 2,
          remainingDays: 2,
          horseStateBefore: null,
          horseStateAfter: null,
          trailEvent: null,
          pendingEncounter: null,
          encounterResolution: null,
          healthDelta: 0,
          walletDelta: 0,
          foodDelta: 0,
          horseFeedDelta: 0,
          canteenChargeDelta: 0,
          ammoSpent: 0,
          horseHungerDelta: 0,
          horseThirstDelta: 0,
          horseExhaustionDelta: 0,
          delayDays: 0,
          heatIncrease: 0,
          openingNarration: "I set out for Dust Fork on a 3-day badlands trail by mounted travel.",
          journeyBeat: "I cross the open range with the horse moving steady under me.",
          resourceBeat: null,
          entries: ["The first light caught the dust behind us, and the road stayed open."],
          warnings: [],
        },
      ],
    },
    logEntries: [],
    ...overrides,
  };
}

function createNoHorseSession(overrides: Partial<GameSessionDto> = {}): GameSessionDto {
  const session = createSession();

  return {
    ...session,
    inventory: {
      ...session.inventory,
      horseState: null,
    },
    journey: {
      ...session.journey!,
      horseState: null,
    },
    travelDiary: {
      days: [
        {
          ...session.travelDiary!.days[0],
          horseStateBefore: null,
          horseStateAfter: null,
          horseFeedDelta: 0,
          horseHungerDelta: 0,
          horseThirstDelta: 0,
          horseExhaustionDelta: 0,
        },
      ],
    },
    ...overrides,
  };
}

function renderTravelPanel(session: GameSessionDto, busy = false, onTurnResult = vi.fn()) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
      mutations: {
        retry: false,
      },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <TravelPanel gameId={session.id} session={session} busy={busy} onTurnResult={onTurnResult} />
    </QueryClientProvider>,
  );

  return { queryClient, onTurnResult };
}

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  const promise = new Promise<T>((resolvePromise) => {
    resolve = resolvePromise;
  });

  return { promise, resolve };
}

describe("TravelPanel", () => {
  it("renders backend travel diary entries and the advance action for an active journey", async () => {
    const session = createSession();
    mockedGetGame.mockResolvedValue(session);

    renderTravelPanel(session);

    expect(await screen.findByRole("heading", { name: /travel diary/i })).toBeInTheDocument();
    expect(screen.getByText(/^horse$/i)).toBeInTheDocument();
    expect(screen.getAllByText("I set out for Dust Fork on a 3-day badlands trail by mounted travel.")).toHaveLength(1);
    expect(screen.getByText("The first light caught the dust behind us, and the road stayed open.")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /advance travel day/i })).toBeEnabled();
    });
  });

  it("hides horse-only travel diary fields when the journey has no horse", async () => {
    const session = createNoHorseSession({
      travelDiary: {
        days: [
          {
            ...createSession().travelDiary!.days[0],
            horseStateBefore: null,
            horseStateAfter: null,
            trailEvent: {
              id: 0,
              kind: 0,
              title: "Coin cache",
              message: "A little trail luck turns up a hidden cache.",
              walletDelta: 4,
              foodDelta: 0,
              canteenChargeDelta: 0,
              horseHungerDelta: 0,
              horseThirstDelta: 0,
              horseExhaustionDelta: 0,
              delayDays: 0,
              heatIncrease: 0,
            },
            horseFeedDelta: 0,
            horseHungerDelta: 0,
            horseThirstDelta: 0,
            horseExhaustionDelta: 0,
          },
        ],
      },
    });

    mockedGetGame.mockResolvedValue(session);

    renderTravelPanel(session);

    await screen.findByRole("heading", { name: /travel diary/i });
    expect(screen.queryByText(/^horse$/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/horse hunger/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/horse thirst/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/horse exhaustion/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/horse feed/i)).not.toBeInTheDocument();
  });

  it("shows pending encounter choices as buttons and resolves the selected choice", async () => {
    const user = userEvent.setup();
    const onTurnResult = vi.fn().mockResolvedValue(undefined);
    const session = createSession({
      journey: {
        ...createSession().journey!,
        pendingEncounter: {
          kind: "Dust storm",
          message: "The canyon mouth is blocked by swirling grit.",
          choices: [
            { id: "wait-it-out", label: "Wait it out" },
            { id: "press-on", label: "Press on" },
          ],
        },
      },
    });
    const result: GameTurnResultDto = {
      success: true,
      message: "Encounter resolved.",
      currentSession: session,
      journeyStatus: 0,
      journey: session.journey,
      travelDiary: session.travelDiary,
    };

    mockedGetGame.mockResolvedValue(session);
    mockedResolveTravelEncounter.mockResolvedValue(result);

    renderTravelPanel(session, false, onTurnResult);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Wait it out" })).toBeEnabled();
      expect(screen.getByRole("button", { name: "Press on" })).toBeEnabled();
    });
    expect(screen.queryByRole("button", { name: /advance travel day/i })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Press on" }));

    await waitFor(() => {
      expect(mockedResolveTravelEncounter).toHaveBeenCalledWith("game-1", "press-on");
      expect(onTurnResult).toHaveBeenCalledWith(result);
    });
  });

  it("calls the advance travel API path when advancing the journey", async () => {
    const user = userEvent.setup();
    const onTurnResult = vi.fn().mockResolvedValue(undefined);
    const session = createSession();
    const result: GameTurnResultDto = {
      success: true,
      message: "Another day on the trail.",
      currentSession: session,
      journeyStatus: 0,
      journey: session.journey,
      travelDiary: session.travelDiary,
    };

    mockedGetGame.mockResolvedValue(session);
    mockedAdvanceTravelDay.mockResolvedValue(result);

    renderTravelPanel(session, false, onTurnResult);

    await user.click(screen.getByRole("button", { name: /advance travel day/i }));

    await waitFor(() => {
      expect(mockedAdvanceTravelDay).toHaveBeenCalledWith("game-1");
      expect(onTurnResult).toHaveBeenCalledWith(result);
    });
  });

  it("shows an arrival acknowledgement button when the journey is completed", async () => {
    const user = userEvent.setup();
    const onTurnResult = vi.fn().mockResolvedValue(undefined);
    const session = createSession({
      journey: {
        ...createSession().journey!,
        status: 2,
        pendingEncounter: null,
      },
    });
    const result: GameTurnResultDto = {
      success: true,
      message: "Arrival acknowledged.",
      currentSession: {
        ...session,
        journey: null,
      },
      journeyStatus: null,
      journey: null,
      travelDiary: session.travelDiary,
    };

    mockedGetGame.mockResolvedValue(session);
    mockedAcknowledgeTravelArrival.mockResolvedValue(result);

    renderTravelPanel(session, false, onTurnResult);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /enter town/i })).toBeEnabled();
    });
    expect(screen.queryByRole("button", { name: /advance travel day/i })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /enter town/i }));

    await waitFor(() => {
      expect(mockedAcknowledgeTravelArrival).toHaveBeenCalledWith("game-1");
      expect(onTurnResult).toHaveBeenCalledWith(result);
    });
  });

  it("shows a refresh state while the travel session query is pending", async () => {
    const travelSession = createSession();
    const loading = deferred<GameSessionDto>();

    mockedGetGame.mockReturnValue(loading.promise);

    renderTravelPanel(travelSession, false, vi.fn().mockResolvedValue(undefined));

    expect(await screen.findByText(/refreshing trail pages from the backend/i)).toBeInTheDocument();

    loading.resolve(travelSession);

    await waitFor(() => {
      expect(screen.queryByText(/refreshing trail pages from the backend/i)).not.toBeInTheDocument();
    });
  });
});
