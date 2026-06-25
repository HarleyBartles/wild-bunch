import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { DevOverlay } from "../dev/DevOverlay";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { getSessionAudit } from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
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

const mockedGetSessionAudit = vi.mocked(getSessionAudit);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function renderOverlay(open: boolean, onClose = () => {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <DevOverlay open={open} onClose={onClose} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}

describe("DevOverlay", () => {
  it("renders nothing when closed", () => {
    renderOverlay(false);
    expect(screen.queryByRole("region", { name: /developer overlay/i })).not.toBeInTheDocument();
  });

  it("renders the overlay region when open", () => {
    renderOverlay(true);
    expect(screen.getByRole("region", { name: /developer overlay/i })).toBeInTheDocument();
  });

  it("calls onClose when the Close button is clicked", async () => {
    const onClose = vi.fn();
    renderOverlay(true, onClose);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /close/i }));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("calls onClose on Escape key when not expanded", async () => {
    const onClose = vi.fn();
    renderOverlay(true, onClose);

    const user = userEvent.setup();
    await user.keyboard("{Escape}");
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("renders the session audit panel tab", () => {
    renderOverlay(true);
    expect(screen.getByRole("button", { name: /session audit/i })).toBeInTheDocument();
  });

  it("expands when Expand is clicked and shows Shrink button", async () => {
    renderOverlay(true);

    const user = userEvent.setup();
    const expandBtn = screen.getByRole("button", { name: /expand/i });
    await user.click(expandBtn);

    expect(screen.getByRole("button", { name: /shrink/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /expand/i })).not.toBeInTheDocument();
  });

  it("Escape shrinks instead of closing when expanded", async () => {
    const onClose = vi.fn();
    renderOverlay(true, onClose);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /expand/i }));

    await user.keyboard("{Escape}");
    expect(onClose).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: /expand/i })).toBeInTheDocument();
  });
});
