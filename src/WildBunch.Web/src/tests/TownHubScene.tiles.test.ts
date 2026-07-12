import { afterEach, describe, expect, it, vi } from "vitest";
import { BuildingKind, TownProsperity } from "../api/types";
import type { TownLayoutDto } from "../api/types";
import { TownHubScene } from "../components/town-hub/TownHubScene";

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    constructor(config: unknown) {
      this.config = config;
    }
    destroy() {}
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

function createLayout(): TownLayoutDto {
  const grid = Array.from({ length: 10 }, () => Array.from({ length: 10 }, () => 0));
  grid[0][4] = 1;
  grid[0][5] = 1;
  grid[1][4] = 1;
  grid[1][5] = 1;
  grid[1][3] = 3;
  grid[1][2] = 4;
  grid[2][4] = 1;
  grid[2][5] = 1;
  grid[2][3] = 2;
  grid[3][3] = 4;

  return {
    buildings: [
      { kind: BuildingKind.Store, view: 3, x: 25, y: 5, width: 8, height: 10 },
      { kind: BuildingKind.Sheriff, view: 0, x: 75, y: 25, width: 8, height: 10 },
    ],
    playerSpawnX: 50,
    playerSpawnY: 50,
    prosperity: TownProsperity.Prosperous,
    paths: [{ startX: 10, startY: 10, endX: 30, endY: 40 }],
    tileGrid: grid,
    layoutSalts: {
      resolverVersion: "1.0.0",
      buildingsSalt: "buildings-salt",
      roadsSalt: "roads-salt",
      dirtSalt: "dirt-salt",
      propsSalt: "props-salt",
    },
  };
}

describe("TownHubScene tile rendering", () => {
  it("loads the ground tile and prop asset families during preload", () => {
    const scene = new TownHubScene(createLayout(), [], vi.fn()) as TownHubScene & {
      load: { image: ReturnType<typeof vi.fn> };
    };
    (scene as any).load = { image: vi.fn() };

    scene.preload();

    expect(scene.load.image).toHaveBeenCalledWith("dirt-1", "/assets/town-hub-ground/dirt/dirt-1.png");
    expect(scene.load.image).toHaveBeenCalledWith("road-main-flat", "/assets/town-hub-roads/main-road/flat-edge.png");
    expect(scene.load.image).toHaveBeenCalledWith("spur-road-end-cap", "/assets/town-hub-roads/spur-road/end-cap.png");
    expect(scene.load.image).toHaveBeenCalledWith("path-vertical-diagonal", "/assets/town-hub-roads/path/path-vertical-diagonal.png");
    expect(scene.load.image).toHaveBeenCalledWith("prop-cactus", "/assets/town-hub-ground/props/cactus-normalized.png");
  });

  it("renders a grounded town surface with dirt, roads, spurs, paths, and props", () => {
    const imageCalls: Array<{
      key: string;
      x: number;
      y: number;
      flipX: boolean;
      flipY: boolean;
      scale: number | null;
    }> = [];

    const scene = new TownHubScene(createLayout(), [], vi.fn()) as TownHubScene & { add: any; load: any; textures: any };
    scene.add = {
      image: (x: number, y: number, key: string) => {
        const record = { key, x, y, flipX: false, flipY: false, scale: null as number | null };
        imageCalls.push(record);
        return {
          setDisplaySize() {
            return this;
          },
          setFlipX(value: boolean) {
            record.flipX = value;
            return this;
          },
          setFlipY(value: boolean) {
            record.flipY = value;
            return this;
          },
          setScale(value: number) {
            record.scale = value;
            return this;
          },
          setAlpha() {
            return this;
          },
          setInteractive() {
            return this;
          },
          on() {
            return this;
          },
        };
      },
      rectangle: () => ({
        setAlpha: () => ({
          setStrokeStyle: () => ({
            setInteractive: () => ({
              on: () => ({
                setScale: () => {},
              }),
            }),
          }),
        }),
      }),
      text: () => ({ setOrigin: () => ({}) }),
      circle: () => ({}),
      graphics: () => ({
        fillStyle: () => ({}),
        fillRect: () => ({}),
        lineStyle: () => ({}),
        moveTo: () => ({}),
        lineTo: () => ({}),
        strokePath: () => ({}),
      }),
    };
    (scene as any).textures = { exists: () => false };

    scene.create();

    expect(imageCalls.some((call) => call.key === "dirt-1" || call.key === "dirt-2" || call.key === "dirt-3")).toBe(true);
    expect(imageCalls.some((call) => call.key === "road-main-flat" || call.key === "road-main-path" || call.key === "road-main-spur")).toBe(true);
    expect(imageCalls.some((call) => call.key === "spur-road-straight" || call.key === "spur-road-path" || call.key === "spur-road-end-cap")).toBe(true);
    expect(imageCalls.some((call) => call.key === "path-horizontal-straight" || call.key === "path-horizontal-diagonal" || call.key === "path-vertical-straight" || call.key === "path-vertical-diagonal")).toBe(true);
    expect(imageCalls.some((call) => call.key.startsWith("prop-"))).toBe(true);
  });
});
