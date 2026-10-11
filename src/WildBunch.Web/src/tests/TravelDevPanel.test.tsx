import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TravelDevPanel } from "../dev/panels/TravelDevPanel";
import { GameSessionContext } from "../state/GameSessionProvider";
import { clearTravelOverride, forceTravelOverride, getTravelDevContext } from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
  getTravelDevContext: vi.fn(),
  forceTravelOverride: vi.fn(),
  clearTravelOverride: vi.fn(),
  getSessionAudit: vi.fn(),
}));

vi.mock("../api/wildBunchApi", () => ({
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
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  resolveTravelEncounter: vi.fn(),
  previewTravel: vi.fn(),
}));

const mockedGetContext = vi.mocked(getTravelDevContext);
const mockedForce = vi.mocked(forceTravelOverride);
const mockedClear = vi.mocked(clearTravelOverride);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function renderPanel() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const renderForCurrentGame = () => (
    <QueryClientProvider client={queryClient}>
      <GameSessionContext.Provider
        value={{ gameId: window.localStorage.getItem("wild-bunch.current-game-id") } as never}
      >
        <TravelDevPanel />
      </GameSessionContext.Provider>
    </QueryClientProvider>
  );
  const result = render(renderForCurrentGame());
  return {
    ...result,
    switchGame(id: string) {
      seedGameId(id);
      result.rerender(renderForCurrentGame());
    },
  };
}

function seedGameId(id: string) {
  window.localStorage.setItem("wild-bunch.current-game-id", id);
}

describe("TravelDevPanel", () => {
  it("shows no active session message when gameId is missing", () => {
    renderPanel();
    expect(screen.getByText(/no active session/i)).toBeInTheDocument();
  });

  it("renders journey state when context loads", async () => {
    seedGameId("test-game-1");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-1",
      hasActiveJourney: true,
      journeyStatus: "Active",
      daysTravelled: 2,
      remainingDays: 1,
      pendingEncounterKind: null,
      pendingEncounterMessage: null,
      pendingFoeProfile: null,
      pendingDevOverride: null,
    });

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Active")).toBeInTheDocument();
    });
    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.getByText("1")).toBeInTheDocument();
  });

  it("shows pending override when one is set", async () => {
    seedGameId("test-game-2");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-2",
      hasActiveJourney: true,
      journeyStatus: "Active",
      daysTravelled: 0,
      remainingDays: 2,
      pendingEncounterKind: null,
      pendingEncounterMessage: null,
      pendingFoeProfile: null,
      pendingDevOverride: {
        forcedCategory: "Foe",
        foeProfile: {
          speed: 5,
          fightStrength: 4,
          minimumBribe: 8,
          speedBand: "fast",
          fightBand: "tough",
          bribeBand: "high",
        },
        encounterMessage: "A hard-eyed rider blocks the trail.",
      },
    });

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText(/Foe.*S5.*F4.*\$8/)).toBeInTheDocument();
    });
  });

  it("calls forceTravelOverride when Force button is clicked", async () => {
    seedGameId("test-game-3");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-3",
      hasActiveJourney: true,
      journeyStatus: "Active",
      daysTravelled: 0,
      remainingDays: 2,
      pendingEncounterKind: null,
      pendingEncounterMessage: null,
      pendingFoeProfile: null,
      pendingDevOverride: null,
    });
    mockedForce.mockResolvedValue(undefined);

    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /force override/i })).toBeInTheDocument();
    });

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /force override/i }));

    await waitFor(() => {
      expect(mockedForce).toHaveBeenCalledWith(
        "test-game-3",
        expect.objectContaining({
          forcedCategory: "Foe",
        }),
      );
    });
  });

  it("calls clearTravelOverride when Clear button is clicked", async () => {
    seedGameId("test-game-4");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-4",
      hasActiveJourney: true,
      journeyStatus: "Active",
      daysTravelled: 0,
      remainingDays: 2,
      pendingEncounterKind: null,
      pendingEncounterMessage: null,
      pendingFoeProfile: null,
      pendingDevOverride: {
        forcedCategory: "Foe",
        foeProfile: null,
        encounterMessage: null,
      },
    });
    mockedClear.mockResolvedValue(undefined);

    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /clear override/i })).toBeInTheDocument();
    });

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /clear override/i }));

    await waitFor(() => {
      expect(mockedClear).toHaveBeenCalledWith("test-game-4");
    });
  });

  it("does not submit travel draft values from the previous playthrough", async () => {
    seedGameId("test-game-a");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-a",
      hasActiveJourney: true,
      journeyStatus: "Active",
      daysTravelled: 0,
      remainingDays: 2,
      pendingEncounterKind: null,
      pendingEncounterMessage: null,
      pendingFoeProfile: null,
      pendingDevOverride: null,
    });
    mockedForce.mockResolvedValue(undefined);

    const panel = renderPanel();
    const user = userEvent.setup();
    await waitFor(() =>
      expect(screen.getByRole("button", { name: /force override/i })).toBeInTheDocument(),
    );
    await user.selectOptions(screen.getByDisplayValue("Foe"), "Lucky");
    await user.type(screen.getByPlaceholderText("(default)"), "Game A only");

    panel.switchGame("test-game-b");
    await waitFor(() => expect(screen.getByDisplayValue("Foe")).toBeInTheDocument());
    await user.click(screen.getByRole("button", { name: /force override/i }));

    await waitFor(() => {
      expect(mockedForce).toHaveBeenCalledWith(
        "test-game-b",
        expect.objectContaining({
          forcedCategory: "Foe",
          encounterMessage: null,
        }),
      );
    });
  });
});
