import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { TownLayoutDevPanel } from "../dev/panels/TownLayoutDevPanel";
import { GameSessionProvider } from "../state/GameSessionProvider";
import * as devApi from "../dev/devApi";

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

  it("shows loading state when gameId is present", () => {
    seedGameId("test-game-1");
    renderPanel();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it("shows salts display when salts are loaded", async () => {
    seedGameId("test-game-2");
    
    const mockSalts = {
      resolverVersion: "1.0.0",
      buildingsSalt: "buildings-salt",
      roadsSalt: "roads-salt",
      dirtSalt: "dirt-salt",
      propsSalt: "props-salt",
    };
    
    vi.spyOn(devApi, "getTownLayoutSalts").mockResolvedValue(mockSalts);
    
    renderPanel();
    
    await waitFor(() => {
      expect(screen.getByDisplayValue("buildings-salt")).toBeInTheDocument();
      expect(screen.getByDisplayValue("roads-salt")).toBeInTheDocument();
      expect(screen.getByDisplayValue("dirt-salt")).toBeInTheDocument();
      expect(screen.getByDisplayValue("props-salt")).toBeInTheDocument();
    });
  });

  it("shows no salts loaded message when API returns null", async () => {
    seedGameId("test-game-3");
    vi.spyOn(devApi, "getTownLayoutSalts").mockResolvedValue(undefined as any);
    
    renderPanel();
    
    await waitFor(() => {
      expect(screen.getByText(/no salts loaded/i)).toBeInTheDocument();
    });
  });

  it("calls setTownLayoutSalts when Set Salts button is clicked", async () => {
    seedGameId("test-game-4");
    
    const mockSalts = {
      resolverVersion: "1.0.0",
      buildingsSalt: "buildings-salt",
      roadsSalt: "roads-salt",
      dirtSalt: "dirt-salt",
      propsSalt: "props-salt",
    };
    
    vi.spyOn(devApi, "getTownLayoutSalts").mockResolvedValue(mockSalts);
    const setSaltsSpy = vi.spyOn(devApi, "setTownLayoutSalts").mockResolvedValue(undefined);
    
    renderPanel();
    
    await waitFor(() => {
      expect(screen.getByText(/set salts/i)).toBeInTheDocument();
    });
    
    const setButton = screen.getByText(/set salts/i);
    setButton.click();
    
    expect(setSaltsSpy).toHaveBeenCalledWith("test-game-4", mockSalts);
  });

  it("calls generateRandomTownLayoutSalts when Generate Random button is clicked", async () => {
    seedGameId("test-game-5");
    
    const mockSalts = {
      resolverVersion: "1.0.0",
      buildingsSalt: "buildings-salt",
      roadsSalt: "roads-salt",
      dirtSalt: "dirt-salt",
      propsSalt: "props-salt",
    };
    
    vi.spyOn(devApi, "getTownLayoutSalts").mockResolvedValue(mockSalts);
    const randomSalts = {
      resolverVersion: "1.0.0",
      buildingsSalt: "random-buildings",
      roadsSalt: "random-roads",
      dirtSalt: "random-dirt",
      propsSalt: "random-props",
    };
    const generateSpy = vi.spyOn(devApi, "generateRandomTownLayoutSalts").mockResolvedValue(randomSalts);
    
    renderPanel();
    
    await waitFor(() => {
      expect(screen.getByText(/generate random/i)).toBeInTheDocument();
    });
    
    const generateButton = screen.getByText(/generate random/i);
    generateButton.click();
    
    expect(generateSpy).toHaveBeenCalledWith("test-game-5");
  });

  it("updates salts state after generating random salts", async () => {
    seedGameId("test-game-6");
    
    const mockSalts = {
      resolverVersion: "1.0.0",
      buildingsSalt: "buildings-salt",
      roadsSalt: "roads-salt",
      dirtSalt: "dirt-salt",
      propsSalt: "props-salt",
    };
    
    vi.spyOn(devApi, "getTownLayoutSalts").mockResolvedValue(mockSalts);
    const randomSalts = {
      resolverVersion: "1.0.0",
      buildingsSalt: "random-buildings",
      roadsSalt: "random-roads",
      dirtSalt: "random-dirt",
      propsSalt: "random-props",
    };
    vi.spyOn(devApi, "generateRandomTownLayoutSalts").mockResolvedValue(randomSalts);
    
    renderPanel();
    
    await waitFor(() => {
      expect(screen.getByDisplayValue("buildings-salt")).toBeInTheDocument();
    });
    
    const generateButton = screen.getByText(/generate random/i);
    generateButton.click();
    
    await waitFor(() => {
      expect(screen.getByDisplayValue("random-buildings")).toBeInTheDocument();
      expect(screen.getByDisplayValue("random-roads")).toBeInTheDocument();
    });
  });
});
