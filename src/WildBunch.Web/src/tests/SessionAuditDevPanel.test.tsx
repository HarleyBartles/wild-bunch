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

function seedGameId(id: string) {
  window.localStorage.setItem("wild-bunch.current-game-id", id);
}

describe("SessionAuditDevPanel", () => {
  it("shows no active session when gameId is missing", () => {
    renderPanel();

    expect(screen.getByText(/no active session/i)).toBeInTheDocument();
  });

  it("shows a loading state while the audit query is pending", () => {
    seedGameId("game-loading");
    mockedGetSessionAudit.mockImplementation(() => new Promise(() => {}));

    renderPanel();

    expect(screen.getByText(/loading session audit/i)).toBeInTheDocument();
  });

  it("shows an error state when the audit query fails", async () => {
    seedGameId("game-error");
    mockedGetSessionAudit.mockRejectedValueOnce(new Error("Audit failed."));

    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent("Audit failed.");
    });
  });

  it("shows an empty state when the audit query returns no entries", async () => {
    seedGameId("game-empty");
    mockedGetSessionAudit.mockResolvedValue({
      sessionId: "game-empty",
      entries: [],
    });

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText(/no audit entries yet/i)).toBeInTheDocument();
    });
  });

  it("renders readable saloon dev-control audit entries", async () => {
    seedGameId("game-1");
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
      expect(screen.getByRole("list", { name: /session audit entries/i })).toBeInTheDocument();
    });
    const entries = screen.getAllByRole("listitem");
    expect(entries).toHaveLength(2);
    expect(entries[0]).toHaveTextContent("#1");
    expect(entries[0]).toHaveTextContent("DevSaloonOverrideForced");
    expect(entries[0]).toHaveTextContent("Forced saloon override");
    expect(entries[1]).toHaveTextContent("#2");
    expect(entries[1]).toHaveTextContent("Consumed pending saloon override");
  });
});
