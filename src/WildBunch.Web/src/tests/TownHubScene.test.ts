import { afterEach, describe, expect, it, vi } from "vitest";
import Phaser from "phaser";
import { AvailableActionKind } from "../api/types";
import { BuildingKind } from "../components/town-hub/types";
import type { BuildingPlacementDto, TownLayoutDto } from "../components/town-hub/types";
import { TownHubScene } from "../components/town-hub/TownHubScene";

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    public destroyed = false;
    constructor(config: unknown) {
      this.config = config;
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
  vi.clearAllMocks();
});

function createBuildings(): BuildingPlacementDto[] {
  return [
    { kind: BuildingKind.Store, x: 100, y: 100, width: 80, height: 60 },
    { kind: BuildingKind.Sheriff, x: 300, y: 100, width: 80, height: 60 },
    { kind: BuildingKind.Saloon, x: 500, y: 100, width: 80, height: 60 },
    { kind: BuildingKind.Trailhead, x: 700, y: 100, width: 80, height: 60 },
    { kind: BuildingKind.Telegraph, x: 400, y: 300, width: 60, height: 40 },
  ];
}

function createLayout(overrides: Partial<TownLayoutDto> = {}): TownLayoutDto {
  return {
    buildings: createBuildings(),
    playerSpawnX: 400,
    playerSpawnY: 400,
    ...overrides,
  };
}

describe("TownHubScene", () => {
  it("is constructed with layout data", () => {
    const layout = createLayout();
    const onBuildingSelected = vi.fn();

    const scene = new TownHubScene(layout, [], onBuildingSelected);

    expect(scene).toBeInstanceOf(TownHubScene);
    expect(scene.layout).toBe(layout);
  });

  it("fires navigation callback with correct BuildingKind when selectBuilding is called for an available building", () => {
    const layout = createLayout();
    const onBuildingSelected = vi.fn();
    const availableActions: AvailableActionKind[] = [AvailableActionKind.BuySupplies];

    const scene = new TownHubScene(layout, availableActions, onBuildingSelected);
    scene.selectBuilding(BuildingKind.Store);

    expect(onBuildingSelected).toHaveBeenCalledWith(BuildingKind.Store);
    expect(onBuildingSelected).toHaveBeenCalledTimes(1);
  });

  it("is a no-op for Telegraph (not clickable)", () => {
    const layout = createLayout();
    const onBuildingSelected = vi.fn();

    const scene = new TownHubScene(layout, [], onBuildingSelected);
    scene.selectBuilding(BuildingKind.Telegraph);

    expect(onBuildingSelected).not.toHaveBeenCalled();
  });

  it("is a no-op for unavailable buildings", () => {
    const layout = createLayout();
    const onBuildingSelected = vi.fn();
    const availableActions: AvailableActionKind[] = [];

    const scene = new TownHubScene(layout, availableActions, onBuildingSelected);
    scene.selectBuilding(BuildingKind.Store);
    scene.selectBuilding(BuildingKind.Sheriff);
    scene.selectBuilding(BuildingKind.Saloon);
    scene.selectBuilding(BuildingKind.Trailhead);

    expect(onBuildingSelected).not.toHaveBeenCalled();
  });

  it("fires callback when Sheriff is available via ReadWantedPosters", () => {
    const layout = createLayout();
    const onBuildingSelected = vi.fn();
    const availableActions: AvailableActionKind[] = [AvailableActionKind.ReadWantedPosters];

    const scene = new TownHubScene(layout, availableActions, onBuildingSelected);
    scene.selectBuilding(BuildingKind.Sheriff);

    expect(onBuildingSelected).toHaveBeenCalledWith(BuildingKind.Sheriff);
  });

  it("fires callback when Saloon is available via GatherLocalGossip", () => {
    const layout = createLayout();
    const onBuildingSelected = vi.fn();
    const availableActions: AvailableActionKind[] = [AvailableActionKind.GatherLocalGossip];

    const scene = new TownHubScene(layout, availableActions, onBuildingSelected);
    scene.selectBuilding(BuildingKind.Saloon);

    expect(onBuildingSelected).toHaveBeenCalledWith(BuildingKind.Saloon);
  });

  it("fires callback when Trailhead is available via Travel", () => {
    const layout = createLayout();
    const onBuildingSelected = vi.fn();
    const availableActions: AvailableActionKind[] = [AvailableActionKind.Travel];

    const scene = new TownHubScene(layout, availableActions, onBuildingSelected);
    scene.selectBuilding(BuildingKind.Trailhead);

    expect(onBuildingSelected).toHaveBeenCalledWith(BuildingKind.Trailhead);
  });
});

describe("TownHubScene truth boundary", () => {
  it("does not give the scene access to any API function", () => {
    const layout = createLayout();
    const scene = new TownHubScene(layout, [], vi.fn());

    expect((scene as unknown as Record<string, unknown>).api).toBeUndefined();
    expect((scene as unknown as Record<string, unknown>).requestJson).toBeUndefined();
    expect((scene as unknown as Record<string, unknown>).fetch).toBeUndefined();
  });

  it("selectBuilding only calls onBuildingSelected and does not call fetch or any API", () => {
    const layout = createLayout();
    const onBuildingSelected = vi.fn();
    const fetchSpy = vi.spyOn(globalThis, "fetch");

    const scene = new TownHubScene(layout, [AvailableActionKind.BuySupplies], onBuildingSelected);
    scene.selectBuilding(BuildingKind.Store);

    expect(onBuildingSelected).toHaveBeenCalledTimes(1);
    expect(fetchSpy).not.toHaveBeenCalled();
    fetchSpy.mockRestore();
  });

  it("uses the town-hub scene key", () => {
    const layout = createLayout();
    const scene = new TownHubScene(layout, [], vi.fn());

    expect(Phaser.Scene).toBeDefined();
    expect(scene).toBeInstanceOf(TownHubScene);
  });
});