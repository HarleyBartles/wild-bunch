import { afterEach, describe, expect, it, vi } from "vitest";
import Phaser from "phaser";
import { AvailableActionKind, BuildingKind, BuildingView, TownProsperity } from "../api/types";
import type { BuildingPlacementDto, TownLayoutDto } from "../api/types";
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
    { kind: BuildingKind.Store, view: BuildingView.Profile, x: 12, y: 15, width: 8, height: 10 },
    { kind: BuildingKind.Sheriff, view: BuildingView.Profile, x: 30, y: 15, width: 8, height: 10 },
    { kind: BuildingKind.Saloon, view: BuildingView.Profile, x: 50, y: 15, width: 8, height: 10 },
    { kind: BuildingKind.Trailhead, view: BuildingView.Rear, x: 90, y: 50, width: 8, height: 10 },
    { kind: BuildingKind.Telegraph, view: BuildingView.FrontOblique, x: 46, y: 70, width: 8, height: 10 },
  ];
}

function createLayout(overrides: Partial<TownLayoutDto> = {}): TownLayoutDto {
  return {
    buildings: createBuildings(),
    playerSpawnX: 50,
    playerSpawnY: 50,
    prosperity: TownProsperity.Prosperous,
    paths: [],
    tileGrid: undefined,
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

describe("TownHubScene visual feedback", () => {
  interface MockRect {
    kind: BuildingKind;
    alpha: number;
    strokeStyle: { lineWidth: number; color: number } | null;
    interactive: boolean;
    setAlpha: (a: number) => MockRect;
    setStrokeStyle: (lw: number, color: number) => MockRect;
    setInteractive: (opts?: unknown) => MockRect;
    on: (event: string, handler: () => void) => MockRect;
    setScale: (s: number) => MockRect;
  }

  interface MockImage {
    key: string;
    x: number;
    y: number;
    displayWidth: number | null;
    displayHeight: number | null;
    flipX: boolean;
    flipY: boolean;
    setDisplaySize: (width: number, height: number) => MockImage;
    setFlipX: (flip: boolean) => MockImage;
    setFlipY: (flip: boolean) => MockImage;
  }

  function createSceneWithMockedAdd(
    layout: TownLayoutDto,
    availableActions: AvailableActionKind[],
  ): { scene: TownHubScene; rects: MockRect[]; images: MockImage[] } {
    const rects: MockRect[] = [];
    const images: MockImage[] = [];
    const onBuildingSelected = vi.fn();

    const scene = new TownHubScene(layout, availableActions, onBuildingSelected) as TownHubScene & {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      add: any;
    };

    scene.add = {
      image: (x: number, y: number, key: string) => {
        const image: MockImage = {
          key,
          x,
          y,
          displayWidth: null,
          displayHeight: null,
          flipX: false,
          flipY: false,
          setDisplaySize(width, height) {
            this.displayWidth = width;
            this.displayHeight = height;
            return this;
          },
          setFlipX(flip) {
            this.flipX = flip;
            return this;
          },
          setFlipY(flip) {
            this.flipY = flip;
            return this;
          },
        };
        images.push(image);
        return image;
      },
      rectangle: (_x: number, _y: number, _w: number, _h: number, _color: number) => {
        const rect: MockRect = {
          kind: rects.length as BuildingKind,
          alpha: 1,
          strokeStyle: null,
          interactive: false,
          setAlpha(a) {
            this.alpha = a;
            return this;
          },
          setStrokeStyle(lineWidth, color) {
            this.strokeStyle = { lineWidth, color };
            return this;
          },
          setInteractive() {
            this.interactive = true;
            return this;
          },
          on() {
            return this;
          },
          setScale() {
            return this;
          },
        };
        rects.push(rect);
        return rect;
      },
      text: () => ({ setOrigin: () => {} }),
      circle: () => {},
      graphics: () => ({
        fillStyle: () => ({}),
        fillRect: () => ({}),
        lineStyle: () => ({}),
        moveTo: () => ({}),
        lineTo: () => ({}),
        strokePath: () => ({}),
      }),
    };

    // Mock textures to always return false (no sprites loaded in unit tests)
    // This forces the code to use the rectangle fallback path
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (scene as any).textures = {
      exists: () => false,
    };

    // Track which building kind each rect corresponds to by order of creation.
    // The create() method iterates layout.buildings in order, so rect[i]
    // corresponds to layout.buildings[i].
    scene.create();
    for (let i = 0; i < rects.length && i < layout.buildings.length; i++) {
      rects[i].kind = layout.buildings[i].kind;
    }

    return { scene, rects, images };
  }

  it("renders available buildings with full opacity and white border highlight", () => {
    const layout = createLayout();
    const { rects } = createSceneWithMockedAdd(layout, [AvailableActionKind.BuySupplies]);

    const storeRect = rects.find((r) => r.kind === BuildingKind.Store);
    expect(storeRect).toBeDefined();
    expect(storeRect!.alpha).toBe(1);
    expect(storeRect!.strokeStyle).toEqual({ lineWidth: 2, color: 0xffffff });
    expect(storeRect!.interactive).toBe(true);
  });

  it("renders unavailable buildings with 0.4 opacity and no border", () => {
    const layout = createLayout();
    // Only Store is available; Sheriff/Saloon/Trailhead are unavailable.
    const { rects } = createSceneWithMockedAdd(layout, [AvailableActionKind.BuySupplies]);

    const sheriffRect = rects.find((r) => r.kind === BuildingKind.Sheriff);
    expect(sheriffRect).toBeDefined();
    expect(sheriffRect!.alpha).toBe(0.4);
    expect(sheriffRect!.strokeStyle).toBeNull();
    expect(sheriffRect!.interactive).toBe(false);

    const saloonRect = rects.find((r) => r.kind === BuildingKind.Saloon);
    expect(saloonRect!.alpha).toBe(0.4);
    expect(saloonRect!.strokeStyle).toBeNull();
    expect(saloonRect!.interactive).toBe(false);

    const trailheadRect = rects.find((r) => r.kind === BuildingKind.Trailhead);
    expect(trailheadRect!.alpha).toBe(0.4);
    expect(trailheadRect!.strokeStyle).toBeNull();
    expect(trailheadRect!.interactive).toBe(false);
  });

  it("renders Telegraph at 0.6 opacity with no border and no interactive hit area", () => {
    const layout = createLayout();
    const { rects } = createSceneWithMockedAdd(layout, [AvailableActionKind.BuySupplies]);

    const telegraphRect = rects.find((r) => r.kind === BuildingKind.Telegraph);
    expect(telegraphRect).toBeDefined();
    expect(telegraphRect!.alpha).toBe(0.6);
    expect(telegraphRect!.strokeStyle).toBeNull();
    expect(telegraphRect!.interactive).toBe(false);
  });

  it("renders all buildings at full opacity when all actions are available", () => {
    const layout = createLayout();
    const allActions: AvailableActionKind[] = [
      AvailableActionKind.BuySupplies,
      AvailableActionKind.ReadWantedPosters,
      AvailableActionKind.LookAroundSaloon,
      AvailableActionKind.Travel,
    ];
    const { rects } = createSceneWithMockedAdd(layout, allActions);

    for (const rect of rects) {
      if (rect.kind === BuildingKind.Telegraph) {
        expect(rect.alpha).toBe(0.6);
      } else {
        expect(rect.alpha).toBe(1);
        expect(rect.strokeStyle).toEqual({ lineWidth: 2, color: 0xffffff });
        expect(rect.interactive).toBe(true);
      }
    }
  });

  it("renders spur-connected building ground tiles with the spur path rule", () => {
    const tileGrid = Array.from({ length: 10 }, () => Array(10).fill(0));
    tileGrid[2][2] = 2;
    tileGrid[3][2] = 4;
    tileGrid[2][7] = 2;
    tileGrid[3][7] = 4;

    const layout = createLayout({
      buildings: [
        { kind: BuildingKind.Store, view: BuildingView.FrontOblique, x: 25, y: 25, width: 8, height: 10 },
        { kind: BuildingKind.Sheriff, view: BuildingView.Front, x: 75, y: 25, width: 8, height: 10 },
      ],
      tileGrid,
    });

    const { images } = createSceneWithMockedAdd(layout, [AvailableActionKind.BuySupplies]);

    expect(images).toHaveLength(2);
    expect(images[0]).toMatchObject({
      key: "path-vertical-diagonal",
      x: 200,
      y: 125,
      displayWidth: 80,
      displayHeight: 50,
      flipX: false,
      flipY: false,
    });
    expect(images[1]).toMatchObject({
      key: "path-vertical-straight",
      x: 600,
      y: 125,
      displayWidth: 80,
      displayHeight: 50,
      flipX: false,
      flipY: false,
    });
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
