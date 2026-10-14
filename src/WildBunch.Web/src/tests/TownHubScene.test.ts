import { afterEach, describe, expect, it, vi } from "vitest";
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
    {
      kind: BuildingKind.Telegraph,
      view: BuildingView.FrontOblique,
      x: 46,
      y: 70,
      width: 8,
      height: 10,
    },
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
    scale: number;
    setAlpha: (a: number) => MockRect;
    setStrokeStyle: (lw: number, color: number) => MockRect;
    setInteractive: (opts?: unknown) => MockRect;
    on: (event: string, handler: () => void) => MockRect;
    emit: (event: string) => void;
    setScale: (s: number) => MockRect;
  }

  interface MockImage {
    key: string;
    x: number;
    y: number;
    readonly displayWidth: number;
    readonly displayHeight: number;
    flipX: boolean;
    flipY: boolean;
    scaleX: number;
    scaleY: number;
    setDisplaySize: (width: number, height: number) => MockImage;
    setFlipX: (flip: boolean) => MockImage;
    setFlipY: (flip: boolean) => MockImage;
    setInteractive: (opts?: unknown) => MockImage;
    on: (event: string, handler: () => void) => MockImage;
    emit: (event: string) => void;
    setScale: (scaleX: number, scaleY?: number) => MockImage;
  }

  function createSceneWithMockedAdd(
    layout: TownLayoutDto,
    availableActions: AvailableActionKind[],
    textureKeys: string[] = [],
  ): {
    scene: TownHubScene;
    rects: MockRect[];
    images: MockImage[];
    onBuildingSelected: ReturnType<typeof vi.fn>;
  } {
    const rects: MockRect[] = [];
    const images: MockImage[] = [];
    const onBuildingSelected = vi.fn();

    const scene = new TownHubScene(layout, availableActions, onBuildingSelected) as TownHubScene & {
      add: any;
    };

    scene.add = {
      image: (x: number, y: number, key: string) => {
        const textureWidth = 128;
        const textureHeight = 100;
        const handlers = new Map<string, () => void>();
        const image: MockImage = {
          key,
          x,
          y,
          flipX: false,
          flipY: false,
          scaleX: 1,
          scaleY: 1,
          get displayWidth() {
            return textureWidth * Math.abs(this.scaleX);
          },
          get displayHeight() {
            return textureHeight * Math.abs(this.scaleY);
          },
          setDisplaySize(width, height) {
            this.scaleX = width / textureWidth;
            this.scaleY = height / textureHeight;
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
          setInteractive() {
            return this;
          },
          on(event, handler) {
            handlers.set(event, handler);
            return this;
          },
          emit(event) {
            handlers.get(event)?.();
          },
          setScale(scaleX, scaleY = scaleX) {
            this.scaleX = scaleX;
            this.scaleY = scaleY;
            return this;
          },
        };
        images.push(image);
        return image;
      },
      rectangle: (_x: number, _y: number, _w: number, _h: number, _color: number) => {
        const handlers = new Map<string, () => void>();
        const rect: MockRect = {
          kind: rects.length as BuildingKind,
          alpha: 1,
          strokeStyle: null,
          interactive: false,
          scale: 1,
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
          on(event, handler) {
            handlers.set(event, handler);
            return this;
          },
          emit(event) {
            handlers.get(event)?.();
          },
          setScale(scale) {
            this.scale = scale;
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

    (scene as any).textures = {
      exists: (key: string) => textureKeys.includes(key),
    };

    // Track which building kind each rect corresponds to by order of creation.
    // The create() method iterates layout.buildings in order, so rect[i]
    // corresponds to layout.buildings[i].
    scene.create();
    for (let i = 0; i < rects.length && i < layout.buildings.length; i++) {
      rects[i].kind = layout.buildings[i].kind;
    }

    return { scene, rects, images, onBuildingSelected };
  }

  it("preserves a fitted sprite's aspect ratio and dimensions through hover and selection", () => {
    const layout = createLayout({
      buildings: [
        {
          kind: BuildingKind.Store,
          view: BuildingView.Profile,
          x: 12,
          y: 15,
          width: 8,
          height: 12,
        },
      ],
    });
    const { images, onBuildingSelected } = createSceneWithMockedAdd(
      layout,
      [AvailableActionKind.BuySupplies],
      [`building-${BuildingKind.Store}`],
    );
    const sprite = images.find((image) => image.key === `building-${BuildingKind.Store}`)!;
    const fittedScaleX = sprite.scaleX;
    const fittedScaleY = sprite.scaleY;
    const fittedWidth = sprite.displayWidth;
    const fittedHeight = sprite.displayHeight;

    expect(fittedScaleX).not.toBe(fittedScaleY);
    expect(fittedWidth).toBeGreaterThan(0);
    expect(fittedHeight).toBeGreaterThan(0);

    sprite.emit("pointerover");
    expect(sprite.scaleX).toBeCloseTo(fittedScaleX * 1.05);
    expect(sprite.scaleY).toBeCloseTo(fittedScaleY * 1.05);
    expect(sprite.displayWidth).toBeCloseTo(fittedWidth * 1.05);
    expect(sprite.displayHeight).toBeCloseTo(fittedHeight * 1.05);

    sprite.emit("pointerout");
    expect(sprite.scaleX).toBe(fittedScaleX);
    expect(sprite.scaleY).toBe(fittedScaleY);
    expect(sprite.displayWidth).toBe(fittedWidth);
    expect(sprite.displayHeight).toBe(fittedHeight);

    sprite.emit("pointerdown");
    expect(onBuildingSelected).toHaveBeenCalledWith(BuildingKind.Store);
  });

  it("keeps the rectangle fallback hover at 1.05 and restores its unit scale", () => {
    const layout = createLayout({
      buildings: [
        {
          kind: BuildingKind.Store,
          view: BuildingView.Profile,
          x: 12,
          y: 15,
          width: 8,
          height: 10,
        },
      ],
    });
    const { rects, onBuildingSelected } = createSceneWithMockedAdd(layout, [
      AvailableActionKind.BuySupplies,
    ]);
    const rect = rects.find((building) => building.kind === BuildingKind.Store)!;

    rect.emit("pointerover");
    expect(rect.scale).toBe(1.05);
    rect.emit("pointerout");
    expect(rect.scale).toBe(1);
    rect.emit("pointerdown");
    expect(onBuildingSelected).toHaveBeenCalledWith(BuildingKind.Store);
  });

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
});

describe("TownHubScene building selection", () => {
  it("forwards an eligible building selection without fetching during selection", () => {
    const layout = createLayout();
    const onBuildingSelected = vi.fn();
    const fetchSpy = vi.spyOn(globalThis, "fetch");

    const scene = new TownHubScene(layout, [AvailableActionKind.BuySupplies], onBuildingSelected);
    scene.selectBuilding(BuildingKind.Store);

    expect(onBuildingSelected).toHaveBeenCalledTimes(1);
    expect(fetchSpy).not.toHaveBeenCalled();
    fetchSpy.mockRestore();
  });
});
