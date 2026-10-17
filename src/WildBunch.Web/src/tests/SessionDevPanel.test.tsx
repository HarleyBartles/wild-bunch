import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SessionDevPanel } from "../dev/panels/SessionDevPanel";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { forceDevDifficulty, getSessionDevContext, setDevEntropy } from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
  getSessionDevContext: vi.fn(),
  forceDevDifficulty: vi.fn(),
  setDevEntropy: vi.fn(),
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

const mockedGetContext = vi.mocked(getSessionDevContext);
const mockedForceDifficulty = vi.mocked(forceDevDifficulty);
const mockedSetEntropy = vi.mocked(setDevEntropy);

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
  travelRules: {
    canteenCapacity: 10,
    mountedRideDayProgress: 1,
    footRideDayProgress: 0.5,
    encounterFightAmmoHealthLoss: 5,
    encounterFightUnarmedHealthLoss: 10,
    encounterRunFootHealthLoss: 5,
  },
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

  it("renders difficulty control and travel-rule facts", async () => {
    seedGameId("test-game-4");
    mockedGetContext.mockResolvedValue({ ...mockContext, sessionId: "test-game-4" });
    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Active")).toBeInTheDocument();
    });
    // Difficulty SegmentedToggle labels are visible
    expect(screen.getByRole("button", { name: "Easy" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Standard" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Brutal" })).toBeInTheDocument();
    // Travel-rule facts are visible
    expect(screen.getByText("Canteen capacity:")).toBeInTheDocument();
    // The canteen capacity value (10) appears next to the label
    const canteenRow = screen.getByText("Canteen capacity:").closest("div");
    expect(canteenRow).toBeInTheDocument();
    expect(canteenRow?.textContent).toContain("10");
  });

  it("calls forceDevDifficulty when difficulty option is clicked", async () => {
    seedGameId("test-game-5");
    mockedGetContext.mockResolvedValue({ ...mockContext, sessionId: "test-game-5" });
    mockedForceDifficulty.mockResolvedValue(undefined);
    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Brutal" })).toBeInTheDocument();
    });
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Brutal" }));

    await waitFor(() => {
      expect(mockedForceDifficulty).toHaveBeenCalledWith("test-game-5", { difficulty: "Brutal" });
    });
  });

  it("preserves the backend difficulty after rejection and shows refreshed state after retry", async () => {
    seedGameId("difficulty-retry-game");
    mockedGetContext
      .mockResolvedValueOnce({ ...mockContext, sessionId: "difficulty-retry-game" })
      .mockResolvedValueOnce({
        ...mockContext,
        sessionId: "difficulty-retry-game",
        gameDifficulty: "Brutal",
      });
    mockedForceDifficulty
      .mockRejectedValueOnce(new Error("Difficulty update failed."))
      .mockResolvedValueOnce(undefined);
    renderPanel();
    const user = userEvent.setup();

    const standard = await screen.findByRole("button", { name: "Standard" });
    const brutal = screen.getByRole("button", { name: "Brutal" });
    expect(standard).toHaveAttribute("aria-pressed", "true");
    expect(brutal).toHaveAttribute("aria-pressed", "false");

    await user.click(brutal);

    expect(await screen.findByText("Difficulty update failed.")).toBeInTheDocument();
    expect(standard).toHaveAttribute("aria-pressed", "true");
    expect(brutal).toHaveAttribute("aria-pressed", "false");

    await user.click(brutal);

    await waitFor(() => {
      expect(brutal).toHaveAttribute("aria-pressed", "true");
      expect(standard).toHaveAttribute("aria-pressed", "false");
      expect(screen.queryByText("Difficulty update failed.")).not.toBeInTheDocument();
    });
    expect(mockedForceDifficulty).toHaveBeenNthCalledWith(1, "difficulty-retry-game", {
      difficulty: "Brutal",
    });
    expect(mockedForceDifficulty).toHaveBeenNthCalledWith(2, "difficulty-retry-game", {
      difficulty: "Brutal",
    });
  });

  it("renders entropy control", async () => {
    seedGameId("test-game-6");
    mockedGetContext.mockResolvedValue({ ...mockContext, sessionId: "test-game-6" });
    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Active")).toBeInTheDocument();
    });
    // Entropy SegmentedToggle labels are visible
    expect(screen.getByRole("button", { name: "Boring" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Classic" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Wild" })).toBeInTheDocument();
  });

  it("calls setDevEntropy when entropy option is clicked", async () => {
    seedGameId("test-game-7");
    mockedGetContext.mockResolvedValue({ ...mockContext, sessionId: "test-game-7" });
    mockedSetEntropy.mockResolvedValue(undefined);
    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Wild" })).toBeInTheDocument();
    });
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Wild" }));

    await waitFor(() => {
      expect(mockedSetEntropy).toHaveBeenCalledWith("test-game-7", { entropy: "Wild" });
    });
  });

  it("preserves the backend entropy after rejection and shows refreshed state after retry", async () => {
    seedGameId("entropy-retry-game");
    mockedGetContext
      .mockResolvedValueOnce({ ...mockContext, sessionId: "entropy-retry-game" })
      .mockResolvedValueOnce({
        ...mockContext,
        sessionId: "entropy-retry-game",
        gameEntropy: "Wild",
      });
    mockedSetEntropy
      .mockRejectedValueOnce(new Error("Entropy update failed."))
      .mockResolvedValueOnce(undefined);
    renderPanel();
    const user = userEvent.setup();

    const classic = await screen.findByRole("button", { name: "Classic" });
    const wild = screen.getByRole("button", { name: "Wild" });
    expect(classic).toHaveAttribute("aria-pressed", "true");
    expect(wild).toHaveAttribute("aria-pressed", "false");

    await user.click(wild);

    expect(await screen.findByText("Entropy update failed.")).toBeInTheDocument();
    expect(classic).toHaveAttribute("aria-pressed", "true");
    expect(wild).toHaveAttribute("aria-pressed", "false");

    await user.click(wild);

    await waitFor(() => {
      expect(wild).toHaveAttribute("aria-pressed", "true");
      expect(classic).toHaveAttribute("aria-pressed", "false");
      expect(screen.queryByText("Entropy update failed.")).not.toBeInTheDocument();
    });
    expect(mockedSetEntropy).toHaveBeenNthCalledWith(1, "entropy-retry-game", { entropy: "Wild" });
    expect(mockedSetEntropy).toHaveBeenNthCalledWith(2, "entropy-retry-game", { entropy: "Wild" });
  });
});
