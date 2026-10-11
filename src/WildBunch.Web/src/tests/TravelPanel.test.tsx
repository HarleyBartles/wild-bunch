import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TravelPanel } from "../components/TravelPanel";
import { gameSessionQueryKey } from "../state/gameSessionQuery";
import { advanceTravelDay, getGame, resolveTravelEncounter } from "../api/wildBunchApi";
import type { GameSessionDto, GameTurnResultDto } from "../api/types";

vi.mock("../api/wildBunchApi", () => ({
  advanceTravelDay: vi.fn(),
  getGame: vi.fn(),
  resolveTravelEncounter: vi.fn(),
}));

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
      caseState: {
        statusText: "Still chasing leads.",
      },
      discoveredSuspects: [],
      caseBoard: { warrants: [], clues: [] },
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
      },
    },
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
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
      journeyEntries: [
        {
          kind: 1,
          message: "I set out for Dust Fork on a 3-day badlands trail by mounted travel.",
          day: 1,
          turn: 0,
        },
        {
          kind: 1,
          message: "The first light caught the dust behind us, and the road stayed open.",
          day: 2,
          turn: 0,
        },
      ],
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
          currentHealth: 9,
          currentWallet: 14,
          currentFood: 2,
          currentHorseFeed: 1,
          currentCanteenCharges: 2,
          currentAmmo: 0,
          currentHeat: 1,
          warnings: [],
        },
      ],
    },
    logEntries: [],
    activeSaloonPersonOfInterest: null,
    ...overrides,
    wantedPosters: overrides.wantedPosters ?? [],
  };
}

function createNoHorseSession(overrides: Partial<GameSessionDto> = {}): GameSessionDto {
  const session = createSession();

  return {
    ...session,
    inventory: {
      ...session.inventory!,
      horseState: null,
    },
    journey: {
      ...session.journey!,
      horseState: null,
    },
    travelDiary: {
      journeyEntries: session.travelDiary!.journeyEntries,
      days: [
        {
          ...session.travelDiary!.days[0],
          horseStateBefore: null,
          horseStateAfter: null,
          horseFeedDelta: 0,
          horseHungerDelta: 0,
          horseThirstDelta: 0,
          horseExhaustionDelta: 0,
          currentHorseFeed: 0,
        },
      ],
    },
    ...overrides,
  };
}

function renderTravelPanel(session: GameSessionDto, busy = false) {
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
      <TravelPanel gameId={session.id} session={session} busy={busy} />
    </QueryClientProvider>,
  );

  return { queryClient };
}

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  const promise = new Promise<T>((resolvePromise) => {
    resolve = resolvePromise;
  });

  return { promise, resolve };
}

describe("TravelPanel", () => {
  it("shows an authoritative session update made by another session reader", async () => {
    const session = createSession();
    mockedGetGame.mockResolvedValue(session);

    const { queryClient } = renderTravelPanel(session);

    expect(await screen.findByText("The ridge may blow up after dusk.")).toBeInTheDocument();

    const refreshedSession: GameSessionDto = {
      ...session,
      journey: {
        ...session.journey!,
        warnings: ["Rain has washed out the crossing."],
      },
    };
    queryClient.setQueryData(["session", session.id], refreshedSession);

    expect(await screen.findByText("Rain has washed out the crossing.")).toBeInTheDocument();
    expect(screen.queryByText("The ridge may blow up after dusk.")).not.toBeInTheDocument();
  });

  it("renders backend travel diary entries and the advance action for an active journey", async () => {
    const session = createSession();
    mockedGetGame.mockResolvedValue(session);

    renderTravelPanel(session);

    const diaryHeading = await screen.findByRole("heading", { name: /travel diary/i });
    expect(screen.getByText(/^horse$/i)).toBeInTheDocument();
    expect(
      screen.getAllByText("I set out for Dust Fork on a 3-day badlands trail by mounted travel."),
    ).toHaveLength(1);
    expect(
      screen.getByText("The first light caught the dust behind us, and the road stayed open."),
    ).toBeInTheDocument();
    expect(screen.getByText(/Health 9 \| Wallet 14\.00/i)).toBeInTheDocument();
    expect(screen.queryByText(/Ammo 0/i)).not.toBeInTheDocument();
    const advanceButton = await screen.findByRole("button", { name: /advance travel day/i });
    expect(
      diaryHeading.compareDocumentPosition(advanceButton) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).not.toBe(0);
    await waitFor(() => {
      expect(advanceButton).toBeEnabled();
    });
  });

  it("keeps route warnings in the ledger instead of repeating them on the diary day", async () => {
    const session = createSession({
      journey: {
        ...createSession().journey!,
        warnings: ["Keep an eye on the ridge line."],
      },
      travelDiary: {
        journeyEntries: [],
        days: [
          {
            ...createSession().travelDiary!.days[0],
            warnings: ["Keep an eye on the ridge line."],
          },
        ],
      },
    });
    mockedGetGame.mockResolvedValue(session);

    renderTravelPanel(session);

    await screen.findByRole("heading", { name: /travel diary/i });
    expect(screen.getByText(/urgent warnings/i)).toBeInTheDocument();
    expect(screen.getAllByText("Keep an eye on the ridge line.")).toHaveLength(1);
  });

  it("hides horse-only travel diary fields when the journey has no horse", async () => {
    const session = createNoHorseSession({
      travelDiary: {
        journeyEntries: [],
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

  it("submits a chosen bullet spend when resolving a fight encounter", async () => {
    const user = userEvent.setup();
    const session = createSession({
      inventory: {
        ...createSession().inventory!,
        items: [{ kind: 7, quantity: 4, horseState: null, canteenState: null }],
      },
      journey: {
        ...createSession().journey!,
        pendingEncounter: {
          kind: "Dust storm",
          message: "The canyon mouth is blocked by swirling grit.",
          choices: [
            { id: "run", label: "Run" },
            { id: "fight", label: "Fight" },
            { id: "bribe", label: "Bribe" },
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

    renderTravelPanel(session);

    const fightBullets = await screen.findByLabelText(/^bullets$/i);
    fireEvent.change(fightBullets, { target: { value: "3" } });

    await user.click(screen.getByRole("button", { name: "Fight" }));

    await waitFor(() => {
      expect(mockedResolveTravelEncounter).toHaveBeenCalledWith("game-1", "fight", {
        bulletSpend: 3,
        bribeAmount: null,
      });
    });
    expect(await screen.findByText(result.message)).toBeInTheDocument();
  });

  it("submits a chosen bribe amount when resolving a bribe encounter", async () => {
    const user = userEvent.setup();
    const session = createSession({
      journey: {
        ...createSession().journey!,
        pendingEncounter: {
          kind: "Dust storm",
          message: "The canyon mouth is blocked by swirling grit.",
          choices: [
            { id: "run", label: "Run" },
            { id: "fight", label: "Fight" },
            { id: "bribe", label: "Bribe" },
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

    renderTravelPanel(session);

    const bribeAmount = await screen.findByLabelText(/^bribe$/i);
    fireEvent.change(bribeAmount, { target: { value: "7.25" } });

    await user.click(screen.getByRole("button", { name: "Bribe" }));

    await waitFor(() => {
      expect(mockedResolveTravelEncounter).toHaveBeenCalledWith("game-1", "bribe", {
        bulletSpend: null,
        bribeAmount: 7.25,
      });
    });
    expect(await screen.findByText(result.message)).toBeInTheDocument();
  });

  it("calls the advance travel API path when advancing the journey", async () => {
    const user = userEvent.setup();
    const session = createSession();
    const updatedSession = createSession({ clock: { day: 6, turn: 0, timeOfDay: "Morning" } });
    const result: GameTurnResultDto = {
      success: false,
      message: "Another day on the trail.",
      currentSession: updatedSession,
      journeyStatus: 0,
      journey: updatedSession.journey,
      travelDiary: updatedSession.travelDiary,
    };

    mockedGetGame.mockResolvedValueOnce(session).mockResolvedValue(updatedSession);
    mockedAdvanceTravelDay.mockResolvedValue(result);

    const { queryClient } = renderTravelPanel(session);

    await user.click(screen.getByRole("button", { name: /advance travel day/i }));

    await waitFor(() => {
      expect(mockedAdvanceTravelDay).toHaveBeenCalledWith("game-1");
    });
    expect(await screen.findByText(result.message)).toBeInTheDocument();
    await waitFor(() => {
      expect(queryClient.getQueryData(gameSessionQueryKey("game-1"))).toEqual(updatedSession);
    });
  });

  it("shows encounter feedback instead of the older advance-day message", async () => {
    const user = userEvent.setup();
    const session = createSession();
    const encounterSession = createSession({
      journey: {
        ...session.journey!,
        pendingEncounter: {
          kind: "Road encounter",
          message: "A rider blocks the road.",
          choices: [{ id: "run", label: "Run" }],
        },
      },
    });
    const resolvedSession = createSession({
      journey: { ...session.journey!, daysTravelled: 1 },
    });
    mockedGetGame
      .mockResolvedValueOnce(session)
      .mockResolvedValueOnce(encounterSession)
      .mockResolvedValue(resolvedSession);
    mockedAdvanceTravelDay.mockResolvedValue({
      success: true,
      message: "The day ended at the roadside.",
      currentSession: encounterSession,
      journeyStatus: 0,
      journey: encounterSession.journey,
      travelDiary: encounterSession.travelDiary,
    });
    mockedResolveTravelEncounter.mockResolvedValue({
      success: true,
      message: "The rider lets me pass.",
      currentSession: resolvedSession,
      journeyStatus: 0,
      journey: resolvedSession.journey,
      travelDiary: resolvedSession.travelDiary,
    });

    renderTravelPanel(session);

    await user.click(await screen.findByRole("button", { name: /advance travel day/i }));
    expect(await screen.findByText("A rider blocks the road.")).toBeInTheDocument();
    expect(await screen.findByText("The day ended at the roadside.")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Run" }));

    expect(await screen.findByText("The rider lets me pass.")).toBeInTheDocument();
    expect(screen.queryByText("The day ended at the roadside.")).not.toBeInTheDocument();
  });

  it("shows a rejected travel command locally and leaves the session unchanged", async () => {
    const user = userEvent.setup();
    const session = createSession();
    mockedGetGame.mockResolvedValue(session);
    mockedAdvanceTravelDay.mockRejectedValue(new Error("Trail service unavailable."));

    const { queryClient } = renderTravelPanel(session);
    await user.click(await screen.findByRole("button", { name: /advance travel day/i }));

    expect(await screen.findByText("Trail service unavailable.")).toBeInTheDocument();
    expect(mockedAdvanceTravelDay).toHaveBeenCalledWith("game-1");
    expect(queryClient.getQueryData(gameSessionQueryKey("game-1"))).toEqual(session);
  });

  it("keeps a late command result and feedback with the game that submitted it", async () => {
    const user = userEvent.setup();
    const firstGame = createSession({ id: "game-1" });
    const secondGame = createSession({
      id: "game-2",
      journey: { ...createSession().journey!, destinationTownName: "Red Mesa" },
    });
    const updatedFirstGame = createSession({
      id: "game-1",
      clock: { day: 6, turn: 0, timeOfDay: "Morning" },
    });
    const response = deferred<GameTurnResultDto>();
    mockedGetGame.mockResolvedValueOnce(firstGame).mockResolvedValue(secondGame);
    mockedAdvanceTravelDay.mockReturnValue(response.promise);

    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    queryClient.setQueryData(gameSessionQueryKey(firstGame.id), firstGame);
    queryClient.setQueryData(gameSessionQueryKey(secondGame.id), secondGame);
    const renderPanel = (session: GameSessionDto) => (
      <QueryClientProvider client={queryClient}>
        <TravelPanel gameId={session.id} session={session} busy={false} />
      </QueryClientProvider>
    );
    const view = render(renderPanel(firstGame));

    await waitFor(() => expect(mockedGetGame).toHaveBeenCalledWith("game-1"));
    await user.click(await screen.findByRole("button", { name: /advance travel day/i }));
    expect(mockedAdvanceTravelDay).toHaveBeenCalledWith("game-1");
    view.rerender(renderPanel(secondGame));
    await waitFor(() => expect(mockedGetGame).toHaveBeenCalledWith("game-2"));
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /advance travel day|advancing/i })).toBeEnabled();
    });
    response.resolve({
      success: true,
      message: "Another day on the trail.",
      currentSession: updatedFirstGame,
      journeyStatus: 0,
      journey: updatedFirstGame.journey,
      travelDiary: updatedFirstGame.travelDiary,
    });

    await waitFor(() => {
      expect(queryClient.getQueryData(gameSessionQueryKey("game-1"))).toEqual(updatedFirstGame);
    });
    expect(queryClient.getQueryData(gameSessionQueryKey("game-2"))).toEqual(secondGame);
    expect(screen.queryByText("Another day on the trail.")).not.toBeInTheDocument();
    expect(screen.getByText("Red Mesa")).toBeInTheDocument();
  });

  it("shows a refresh state while the travel session query is pending", async () => {
    const travelSession = createSession();
    const loading = deferred<GameSessionDto>();

    mockedGetGame.mockReturnValue(loading.promise);

    renderTravelPanel(travelSession);

    expect(await screen.findByText(/refreshing trail pages from the backend/i)).toBeInTheDocument();

    loading.resolve(travelSession);

    await waitFor(() => {
      expect(
        screen.queryByText(/refreshing trail pages from the backend/i),
      ).not.toBeInTheDocument();
    });
  });

  it("renders the hydrated travel session from react query when the backend returns fresher data", async () => {
    const travelSession = createSession();
    const refreshedSession = createSession({
      journey: {
        ...createSession().journey!,
        destinationTownName: "Red Mesa",
      },
    });
    const loading = deferred<GameSessionDto>();

    mockedGetGame.mockReturnValue(loading.promise);

    renderTravelPanel(travelSession);

    expect(screen.getByText("Dust Fork")).toBeInTheDocument();

    loading.resolve(refreshedSession);

    await waitFor(() => {
      expect(screen.getByText("Red Mesa")).toBeInTheDocument();
    });
  });

  it("renders the recorded encounter message without inventing a fight outcome", async () => {
    const recordedMessage = "I spent 3 round(s), but the rider kept coming.";
    const session = createSession({
      travelDiary: {
        journeyEntries: [{ kind: 1, message: recordedMessage, day: 2, turn: 0 }],
        days: [
          {
            ...createSession().travelDiary!.days[0],
            encounterResolution: {
              choiceId: "fight",
              choiceLabel: "Fight",
              healthDelta: -5,
              walletDelta: 0,
              ammoSpent: 3,
              heatIncrease: 1,
              horseExhaustionDelta: 0,
              continuedOnFoot: false,
            },
          },
        ],
      },
    });

    mockedGetGame.mockResolvedValue(session);

    renderTravelPanel(session);

    expect(await screen.findByText(recordedMessage)).toBeInTheDocument();
    expect(screen.getByText(/Ammo spent 3/)).toBeInTheDocument();
    expect(
      screen.queryByText(/one round to force the rider off the trail/i),
    ).not.toBeInTheDocument();
    expect(screen.queryByText(/I stand and fight/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Choice .* shifted/i)).not.toBeInTheDocument();
  });
});
