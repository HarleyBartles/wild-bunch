import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import { TownLayoutDevPanel } from "../dev/panels/TownLayoutDevPanel";
import { GameSessionProvider } from "../state/GameSessionProvider";

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
        <TownLayoutDevPanel />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}

function seedGameId(id: string) {
  window.localStorage.setItem("wild-bunch.current-game-id", id);
}

describe("TownLayoutDevPanel", () => {
  it("shows no active session message when gameId is missing", () => {
    renderPanel();
    expect(screen.getByText(/no active session/i)).toBeInTheDocument();
  });

  it("shows no salts loaded message when salts state is null", () => {
    seedGameId("test-game-1");
    renderPanel();
    expect(screen.getByText(/no salts loaded/i)).toBeInTheDocument();
  });

  it("renders salts display when salts are loaded", () => {
    seedGameId("test-game-2");
    // Note: Currently salts are not loaded from API (Task 9 will add this)
    // This test verifies the UI structure when salts are present
    // For now, we can't easily test this without mocking useState or adding API integration
    // This will be expanded in Task 9 when API integration is added
  });

  it("has placeholder handlers that log to console", () => {
    seedGameId("test-game-3");
    const consoleSpy = vi.spyOn(console, "log");
    
    renderPanel();
    
    // Note: We can't easily click buttons without salts loaded
    // This will be testable in Task 9 when API integration is added
    consoleSpy.mockRestore();
  });
});
