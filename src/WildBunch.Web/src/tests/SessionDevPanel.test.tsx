import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SessionDevPanel } from "../dev/panels/SessionDevPanel";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { clearRng, getSessionDevContext, lockRng } from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
  getSessionDevContext: vi.fn(),
  lockRng: vi.fn(),
  clearRng: vi.fn(),
  getSessionAudit: vi.fn(),
  getTravelDevContext: vi.fn(),
  forceTravelOverride: vi.fn(),
  clearTravelOverride: vi.fn(),
  getSaloonDevContext: vi.fn(),
  forceSaloonOverride: vi.fn(),
  clearSaloonOverride: vi.fn(),
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

const mockedGetContext = vi.mocked(getSessionDevContext);
const mockedLock = vi.mocked(lockRng);
const mockedClear = vi.mocked(clearRng);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function renderPanel(expanded = false) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <SessionDevPanel expanded={expanded} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}

function seedGameId(id: string) {
  window.localStorage.setItem("wild-bunch.current-game-id", id);
}

const mockContext = {
  sessionId: "test-game-1",
  status: "Active",
  gameDifficulty: "Standard",
  gameEntropy: "Classic",
  saltPosture: { mode: "Runtime", salt: "abc123" },
  clock: { day: 1, turn: 0, timeOfDay: "Dawn" },
  currentTownId: "town-1",
  currentTownName: "Dodge",
  currentActionContext: "None",
  hasActiveJourney: false,
  seedCodeRetained: false,
  seedCodeText: null,
};

describe("SessionDevPanel", () => {
  it("shows no active session message when gameId is missing", () => {
    renderPanel();
    expect(screen.getByText(/no active session/i)).toBeInTheDocument();
  });

  it("renders session context when loaded", async () => {
    seedGameId("test-game-1");
    mockedGetContext.mockResolvedValue(mockContext);
    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Active")).toBeInTheDocument();
    });
    expect(screen.getByText("Standard")).toBeInTheDocument();
    expect(screen.getByText("Classic")).toBeInTheDocument();
    expect(screen.getByText("Runtime")).toBeInTheDocument();
  });

  it("calls lockRng when Lock RNG is clicked", async () => {
    seedGameId("test-game-2");
    mockedGetContext.mockResolvedValue({ ...mockContext, sessionId: "test-game-2" });
    mockedLock.mockResolvedValue(undefined);
    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /lock rng/i })).toBeInTheDocument();
    });
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /lock rng/i }));

    await waitFor(() => {
      expect(mockedLock).toHaveBeenCalledWith("test-game-2", expect.objectContaining({}));
    });
  });

  it("calls clearRng when Clear RNG is clicked", async () => {
    seedGameId("test-game-3");
    mockedGetContext.mockResolvedValue({ ...mockContext, sessionId: "test-game-3" });
    mockedClear.mockResolvedValue(undefined);
    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /clear rng/i })).toBeInTheDocument();
    });
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /clear rng/i }));

    await waitFor(() => {
      expect(mockedClear).toHaveBeenCalledWith("test-game-3");
    });
  });
});
