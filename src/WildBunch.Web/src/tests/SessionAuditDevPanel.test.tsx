import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { SessionAuditDevPanel } from "../dev/panels/SessionAuditDevPanel";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { getSessionAudit } from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
  getSessionAudit: vi.fn(),
  getSaloonDevContext: vi.fn(),
  forceSaloonOverride: vi.fn(),
  clearSaloonOverride: vi.fn(),
  getTravelDevContext: vi.fn(),
  forceTravelOverride: vi.fn(),
  clearTravelOverride: vi.fn(),
}));

const mockedGetSessionAudit = vi.mocked(getSessionAudit);

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
        <SessionAuditDevPanel />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}

describe("SessionAuditDevPanel", () => {
  it("renders readable saloon dev-control audit entries", async () => {
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
    mockedGetSessionAudit.mockResolvedValue({
      sessionId: "game-1",
      entries: [
        {
          sequence: 1,
          eventType: "DevSaloonOverrideForced",
          summary: "Forced saloon override: Suspect for suspect suspect-1.",
          occurredAtUtc: "2026-06-26T00:00:00Z",
        },
        {
          sequence: 2,
          eventType: "DevSaloonOverrideConsumed",
          summary: "Consumed pending saloon override during saloon look-around.",
          occurredAtUtc: "2026-06-26T00:01:00Z",
        },
      ],
    });

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("DevSaloonOverrideForced")).toBeInTheDocument();
    });
    expect(screen.getByText(/Forced saloon override/i)).toBeInTheDocument();
    expect(screen.getByText(/Consumed pending saloon override/i)).toBeInTheDocument();
  });
});
