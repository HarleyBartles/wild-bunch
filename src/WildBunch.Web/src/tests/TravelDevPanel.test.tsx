import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TravelDevPanel } from "../dev/panels/TravelDevPanel";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { clearTravelOverride, forceTravelOverride, getTravelDevContext } from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
  getTravelDevContext: vi.fn(),
  forceTravelOverride: vi.fn(),
  clearTravelOverride: vi.fn(),
  getSessionAudit: vi.fn(),
}));

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
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <TravelDevPanel />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
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
        foeProfile: { speed: 5, fightStrength: 4, minimumBribe: 8, speedBand: "fast", fightBand: "tough", bribeBand: "high" },
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
      expect(mockedForce).toHaveBeenCalledWith("test-game-3", expect.objectContaining({
        forcedCategory: "Foe",
      }));
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
});
