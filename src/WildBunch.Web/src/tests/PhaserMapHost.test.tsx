import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render } from "@testing-library/react";
import { PhaserMapHost, StartingTownMapScene } from "../components/start-flow/PhaserMapHost";
import type { StartingTownMapDto } from "../api/types";
import Phaser from "phaser";

const mockState = vi.hoisted(() => ({
  games: [] as Array<{ config: { scene: StartingTownMapScene }; destroyed: boolean; destroy: () => void }>,
}));

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    public destroyed = false;
    constructor(config: unknown) {
      this.config = config;
      mockState.games.push(this as never);
    }
    destroy() {
      this.destroyed = true;
    }
  }
  class Scene {
    constructor(_key?: string) {}
  }
  const Scale = { FIT: 0, CENTER_BOTH: 0 };
  return { default: { Game, Scene, Scale }, Game, Scene, Scale };
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  mockState.games.length = 0;
});

function createMapData(overrides: Partial<StartingTownMapDto> = {}): StartingTownMapDto {
  return {
    towns: [
      { id: "t-town", name: "Tumbleweed", services: 0, x: 150, y: 500 },
      { id: "dust-fork", name: "Dust Fork", services: 0, x: 450, y: 400 },
      { id: "hardpan", name: "Hardpan", services: 0, x: 100, y: 300 },
    ],
    trails: [
      { id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", rideDayDistance: 3 },
    ],
    ...overrides,
  };
}

function renderHost(overrides: {
  mapData?: StartingTownMapDto;
  selectedTownId?: string | null;
  onTownSelected?: (townId: string) => void;
} = {}) {
  const onTownSelected = overrides.onTownSelected ?? vi.fn();
  const mapData = overrides.mapData ?? createMapData();

  render(
    <PhaserMapHost
      mapData={mapData}
      selectedTownId={overrides.selectedTownId ?? null}
      onTownSelected={onTownSelected}
    />,
  );

  return { onTownSelected, mapData };
}

describe("PhaserMapHost", () => {
  it("creates a Phaser game on mount", () => {
    renderHost();

    expect(mockState.games).toHaveLength(1);
  });

  it("destroys the Phaser game on unmount", () => {
    const { unmount } = render(<PhaserMapHost mapData={createMapData()} selectedTownId={null} onTownSelected={vi.fn()} />);

    expect(mockState.games).toHaveLength(1);
    expect(mockState.games[0].destroyed).toBe(false);

    unmount();

    expect(mockState.games[0].destroyed).toBe(true);
  });

  it("emits onTownSelected when a town is selected through the scene", () => {
    const onTownSelected = vi.fn();
    renderHost({ onTownSelected });

    const scene = mockState.games[0].config.scene;
    scene.selectTown("t-town");

    expect(onTownSelected).toHaveBeenCalledWith("t-town");
  });

  it("does not emit onTownSelected for an unknown town id", () => {
    const onTownSelected = vi.fn();
    renderHost({ onTownSelected });

    const scene = mockState.games[0].config.scene;
    scene.selectTown("nonexistent");

    expect(onTownSelected).not.toHaveBeenCalled();
  });

  it("passes the selectedTownId to the scene for highlighting", () => {
    renderHost({ selectedTownId: "dust-fork" });

    const scene = mockState.games[0].config.scene;
    expect(scene).toBeInstanceOf(StartingTownMapScene);
    expect(scene.selectedTownId).toBe("dust-fork");
  });

  it("creates exactly one game per mount", () => {
    renderHost();

    expect(mockState.games).toHaveLength(1);
    expect(Phaser.Game).toBeDefined();
  });
});

describe("PhaserMapHost truth boundary", () => {
  it("does not give the scene access to any API function", () => {
    renderHost();

    const scene = mockState.games[0].config.scene as StartingTownMapScene;
    expect((scene as unknown as Record<string, unknown>).api).toBeUndefined();
    expect((scene as unknown as Record<string, unknown>).requestJson).toBeUndefined();
    expect((scene as unknown as Record<string, unknown>).fetch).toBeUndefined();
    expect((scene as unknown as Record<string, unknown>).getStartingTownMap).toBeUndefined();
  });

  it("selectTown only calls onTownSelected and does not call fetch or any API", () => {
    const onTownSelected = vi.fn();
    const fetchSpy = vi.spyOn(globalThis, "fetch");
    renderHost({ onTownSelected });

    const scene = mockState.games[0].config.scene as StartingTownMapScene;
    scene.selectTown("t-town");

    expect(onTownSelected).toHaveBeenCalledTimes(1);
    expect(onTownSelected).toHaveBeenCalledWith("t-town");
    expect(fetchSpy).not.toHaveBeenCalled();
    fetchSpy.mockRestore();
  });

  it("does not mutate selectedTownId when selectTown is called", () => {
    renderHost({ selectedTownId: null });

    const scene = mockState.games[0].config.scene as StartingTownMapScene;
    const before = scene.selectedTownId;
    scene.selectTown("t-town");

    expect(scene.selectedTownId).toBe(before);
    expect(scene.selectedTownId).toBeNull();
  });

  it("receives selectedTownId as a readonly prop, not as stored truth", () => {
    renderHost({ selectedTownId: "dust-fork" });

    const scene = mockState.games[0].config.scene as StartingTownMapScene;
    expect(scene.selectedTownId).toBe("dust-fork");
    scene.selectTown("t-town");
    expect(scene.selectedTownId).toBe("dust-fork");
  });
});
