import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render } from "@testing-library/react";
import { PhaserTownHubHost } from "../components/town-hub/PhaserTownHubHost";
import { TownHubScene } from "../components/town-hub/TownHubScene";
import { AvailableActionKind, BuildingKind, type TownLayoutDto } from "../api/types";
import Phaser from "phaser";

const mockState = vi.hoisted(() => ({
  games: [] as Array<{ config: { scene: TownHubScene }; destroyed: boolean; destroy: () => void }>,
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

function createLayout(overrides: Partial<TownLayoutDto> = {}): TownLayoutDto {
  return {
    buildings: [
      { kind: BuildingKind.Store, x: 12, y: 15, width: 8, height: 10 },
      { kind: BuildingKind.Sheriff, x: 46, y: 15, width: 8, height: 10 },
      { kind: BuildingKind.Saloon, x: 80, y: 15, width: 8, height: 10 },
      { kind: BuildingKind.Trailhead, x: 90, y: 50, width: 8, height: 10 },
    ],
    playerSpawnX: 50,
    playerSpawnY: 50,
    ...overrides,
  };
}

function renderHost(overrides: {
  layout?: TownLayoutDto | null;
  availableActions?: AvailableActionKind[];
  onBuildingSelected?: (kind: BuildingKind) => void;
} = {}) {
  const onBuildingSelected = overrides.onBuildingSelected ?? vi.fn();
  const layout = "layout" in overrides ? overrides.layout : createLayout();
  const availableActions =
    overrides.availableActions ?? [AvailableActionKind.BuySupplies, AvailableActionKind.Travel];

  render(
    <PhaserTownHubHost
      layout={layout}
      availableActions={availableActions}
      onBuildingSelected={onBuildingSelected}
    />,
  );

  return { onBuildingSelected, layout, availableActions };
}

describe("PhaserTownHubHost", () => {
  it("creates a Phaser game on mount", () => {
    renderHost();

    expect(mockState.games).toHaveLength(1);
  });

  it("passes the current town's layout data to the TownHubScene constructor", () => {
    const layout = createLayout({
      buildings: [
        { kind: BuildingKind.Saloon, x: 100, y: 100, width: 70, height: 50 },
      ],
      playerSpawnX: 120,
      playerSpawnY: 130,
    });

    renderHost({ layout });

    const scene = mockState.games[0].config.scene;
    expect(scene).toBeInstanceOf(TownHubScene);
    expect(scene.layout).toBe(layout);
  });

  it("does not create a Phaser game when layout is null", () => {
    renderHost({ layout: null });

    expect(mockState.games).toHaveLength(0);
  });

  it("destroys the Phaser game on unmount", () => {
    const { unmount } = render(
      <PhaserTownHubHost
        layout={createLayout()}
        availableActions={[AvailableActionKind.Travel]}
        onBuildingSelected={vi.fn()}
      />,
    );

    expect(mockState.games).toHaveLength(1);
    expect(mockState.games[0].destroyed).toBe(false);

    unmount();

    expect(mockState.games[0].destroyed).toBe(true);
  });

  it("emits onBuildingSelected when a building is selected through the scene", () => {
    const onBuildingSelected = vi.fn();
    renderHost({
      onBuildingSelected,
      availableActions: [AvailableActionKind.BuySupplies],
    });

    const scene = mockState.games[0].config.scene;
    scene.selectBuilding(BuildingKind.Store);

    expect(onBuildingSelected).toHaveBeenCalledWith(BuildingKind.Store);
  });

  it("uses the latest onBuildingSelected callback without remounting the game", () => {
    const first = vi.fn();
    const second = vi.fn();
    const stableActions: AvailableActionKind[] = [AvailableActionKind.BuySupplies];
    const stableLayout = createLayout();

    const { rerender } = render(
      <PhaserTownHubHost
        layout={stableLayout}
        availableActions={stableActions}
        onBuildingSelected={first}
      />,
    );

    expect(mockState.games).toHaveLength(1);
    const gameBefore = mockState.games[0];

    rerender(
      <PhaserTownHubHost
        layout={stableLayout}
        availableActions={stableActions}
        onBuildingSelected={second}
      />,
    );

    // Same game instance — callback ref updated without recreating the Phaser game.
    expect(mockState.games).toHaveLength(1);
    expect(mockState.games[0]).toBe(gameBefore);

    const scene = mockState.games[0].config.scene;
    scene.selectBuilding(BuildingKind.Store);

    expect(first).not.toHaveBeenCalled();
    expect(second).toHaveBeenCalledWith(BuildingKind.Store);
  });
});

describe("PhaserTownHubHost truth boundary", () => {
  it("does not give the scene access to any API function", () => {
    renderHost();

    const scene = mockState.games[0].config.scene as unknown as Record<string, unknown>;
    expect(scene.api).toBeUndefined();
    expect(scene.requestJson).toBeUndefined();
    expect(scene.fetch).toBeUndefined();
    expect(scene.getGame).toBeUndefined();
  });

  it("selectBuilding only calls onBuildingSelected and does not call fetch or any API", () => {
    const onBuildingSelected = vi.fn();
    const fetchSpy = vi.spyOn(globalThis, "fetch");
    renderHost({ onBuildingSelected, availableActions: [AvailableActionKind.BuySupplies] });

    const scene = mockState.games[0].config.scene;
    scene.selectBuilding(BuildingKind.Store);

    expect(onBuildingSelected).toHaveBeenCalledTimes(1);
    expect(fetchSpy).not.toHaveBeenCalled();
    fetchSpy.mockRestore();
  });
});
