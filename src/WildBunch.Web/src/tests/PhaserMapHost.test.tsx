import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render } from "@testing-library/react";
import { PhaserMapHost, WorldMapScene } from "../components/start-flow/PhaserMapHost";
import type { WorldMapDto } from "../api/types";
import Phaser from "phaser";

const mockState = vi.hoisted(() => ({
  circles: [] as Array<{
    x: number;
    y: number;
    radius: number;
    strokeWidth: number;
    strokeColor: number;
    handlers: Record<string, () => void>;
  }>,
  labels: [] as Array<{
    x: number;
    y: number;
    width: number;
    height: number;
    text: string;
    fontSize: string;
    setOrigin: () => unknown;
    setPosition: (x: number, y: number) => unknown;
  }>,
  trails: [] as Array<{ from: [number, number]; to: [number, number] }>,
  games: [] as Array<{
    config: { scene: WorldMapScene; scale: { mode: number } };
    destroyed: boolean;
    destroy: () => void;
  }>,
}));

vi.mock("phaser", () => {
  class Game {
    public config: { scene: WorldMapScene; scale: { mode: number } };
    public destroyed = false;
    constructor(config: { scene: WorldMapScene; scale: { mode: number } }) {
      this.config = config;
      mockState.games.push(this);
      config.scene.create();
    }
    destroy() {
      this.destroyed = true;
    }
  }
  class Scene {
    public scale = {
      width: 800,
      height: 500,
      handlers: new Map<string, () => void>(),
      on(event: string, handler: () => void) {
        this.handlers.set(event, handler);
      },
      resize(width: number, height: number) {
        this.width = width;
        this.height = height;
        this.handlers.get("resize")?.();
      },
    };
    public children = {
      removeAll() {
        mockState.circles.length = 0;
        mockState.labels.length = 0;
        mockState.trails.length = 0;
      },
    };
    public add = {
      graphics() {
        let from: [number, number] = [0, 0];
        let to: [number, number] = [0, 0];
        return {
          lineStyle() {},
          beginPath() {},
          moveTo(x: number, y: number) {
            from = [x, y];
          },
          lineTo(x: number, y: number) {
            to = [x, y];
          },
          strokePath() {
            mockState.trails.push({ from, to });
          },
        };
      },
      circle(x: number, y: number, radius: number) {
        const circle = {
          x,
          y,
          radius,
          strokeWidth: 0,
          strokeColor: 0,
          handlers: {} as Record<string, () => void>,
          setStrokeStyle(width: number, color: number) {
            circle.strokeWidth = width;
            circle.strokeColor = color;
            return circle;
          },
          setInteractive() {
            return circle;
          },
          on(event: string, handler: () => void) {
            circle.handlers[event] = handler;
            return circle;
          },
          setScale() {
            return circle;
          },
        };
        mockState.circles.push(circle);
        return circle;
      },
      text(x: number, y: number, text: string, style: { fontSize: string }) {
        const label = {
          x,
          y,
          width: text.length * 7,
          height: 16,
          text,
          fontSize: style.fontSize,
          setOrigin() {
            return label;
          },
          setPosition(nextX: number, nextY: number) {
            label.x = nextX;
            label.y = nextY;
            return label;
          },
        };
        mockState.labels.push(label);
        return label;
      },
    };
    constructor(_key?: string) {}
  }
  const Scale = { FIT: 0, RESIZE: 1, CENTER_BOTH: 0, Events: { RESIZE: "resize" } };
  return { default: { Game, Scene, Scale }, Game, Scene, Scale };
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  mockState.games.length = 0;
  mockState.circles.length = 0;
  mockState.labels.length = 0;
  mockState.trails.length = 0;
});

function createMapData(overrides: Partial<WorldMapDto> = {}): WorldMapDto {
  return {
    towns: [
      { id: "t-town", name: "Tumbleweed", x: 150, y: 500 },
      { id: "dust-fork", name: "Dust Fork", x: 450, y: 400 },
      { id: "hardpan", name: "Hardpan", x: 100, y: 300 },
    ],
    trails: [{ id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", rideDayDistance: 3 }],
    ...overrides,
  };
}

function renderHost(
  overrides: {
    mapData?: WorldMapDto;
    selectedTownId?: string | null;
    onTownSelected?: (townId: string) => void;
    currentTownId?: string | null;
    selectableTownIds?: string[] | null;
  } = {},
) {
  const onTownSelected = overrides.onTownSelected ?? vi.fn();
  const mapData = overrides.mapData ?? createMapData();

  const view = render(
    <PhaserMapHost
      mapData={mapData}
      selectedTownId={overrides.selectedTownId ?? null}
      onTownSelected={onTownSelected}
      currentTownId={overrides.currentTownId}
      selectableTownIds={overrides.selectableTownIds}
    />,
  );

  return {
    onTownSelected,
    mapData,
    update: (next: Partial<typeof overrides>) =>
      view.rerender(
        <PhaserMapHost
          mapData={next.mapData ?? mapData}
          selectedTownId={next.selectedTownId ?? null}
          onTownSelected={next.onTownSelected ?? onTownSelected}
          currentTownId={next.currentTownId}
          selectableTownIds={next.selectableTownIds}
        />,
      ),
  };
}

describe("PhaserMapHost", () => {
  it("renders the supplied towns and trail when mounted", () => {
    renderHost();

    expect(mockState.circles).toHaveLength(3);
    expect(mockState.trails).toHaveLength(1);
  });

  it("destroys the Phaser game on unmount", () => {
    const { unmount } = render(
      <PhaserMapHost mapData={createMapData()} selectedTownId={null} onTownSelected={vi.fn()} />,
    );

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

  it("keeps the rendered map and selection handler live when React selection changes", () => {
    const originalSelection = vi.fn();
    const latestSelection = vi.fn();
    const { update } = renderHost({ onTownSelected: originalSelection });
    const game = mockState.games[0];
    const scene = game.config.scene;

    update({ selectedTownId: "dust-fork", onTownSelected: latestSelection });

    expect(game.destroyed).toBe(false);
    expect(mockState.games).toHaveLength(1);
    expect(scene.selectedTownId).toBe("dust-fork");
    expect(mockState.circles.filter((circle) => circle.strokeWidth >= 4)).toHaveLength(1);
    scene.selectTown("hardpan");
    expect(latestSelection).toHaveBeenCalledWith("hardpan");
    expect(originalSelection).not.toHaveBeenCalled();
  });

  it("applies the latest travel eligibility before accepting another pointer selection", () => {
    const onTownSelected = vi.fn();
    const { update } = renderHost({
      onTownSelected,
      currentTownId: "t-town",
      selectableTownIds: ["dust-fork"],
    });
    const game = mockState.games[0];
    const scene = game.config.scene;

    update({ selectableTownIds: ["hardpan"] });

    scene.selectTown("dust-fork");
    scene.selectTown("hardpan");

    expect(game.destroyed).toBe(false);
    expect(mockState.games).toHaveLength(1);
    expect(onTownSelected).toHaveBeenCalledTimes(1);
    expect(onTownSelected).toHaveBeenCalledWith("hardpan");
  });

  it("keeps towns, trail connections, labels, and pointer selection inside a narrow viewport after resize", () => {
    const onTownSelected = vi.fn();
    const mapData = createMapData({
      towns: [
        { id: "holloway", name: "Holloway", x: 671, y: 169 },
        { id: "drygulch", name: "Dry Gulch", x: 222, y: 344 },
        { id: "canyonfalls", name: "Canyon Falls", x: 625, y: 136 },
        { id: "silvercreek", name: "Silver Creek", x: 482, y: 397 },
        { id: "cinnabar", name: "Cinnabar", x: 299, y: 266 },
        { id: "hangmanstree", name: "Hangman's Tree", x: 151, y: 259 },
        { id: "barrelcactus", name: "Barrel Cactus", x: 692, y: 225 },
      ],
      trails: [
        {
          id: "trail-holloway-canyon",
          fromTownId: "holloway",
          toTownId: "canyonfalls",
          rideDayDistance: 2,
        },
        {
          id: "trail-canyon-barrel",
          fromTownId: "canyonfalls",
          toTownId: "barrelcactus",
          rideDayDistance: 4,
        },
        {
          id: "trail-dry-cinnabar",
          fromTownId: "drygulch",
          toTownId: "cinnabar",
          rideDayDistance: 4,
        },
      ],
    });
    renderHost({ onTownSelected, mapData });

    const game = mockState.games[0];
    const scene = game.config.scene as unknown as {
      scale: { resize: (width: number, height: number) => void };
    };
    scene.scale.resize(320, 320);

    expect(game.config.scale.mode).toBe(Phaser.Scale.RESIZE);
    expect(mockState.circles).toHaveLength(7);
    expect(
      mockState.circles.every(
        (circle) =>
          circle.x - circle.radius >= 0 &&
          circle.x + circle.radius <= 320 &&
          circle.y - circle.radius >= 0 &&
          circle.y + circle.radius <= 320,
      ),
    ).toBe(true);
    expect(mockState.labels).toHaveLength(10);
    expect(
      mockState.labels.every(
        (label) =>
          label.x - label.width / 2 >= 0 &&
          label.x + label.width / 2 <= 320 &&
          label.y - label.height / 2 >= 0 &&
          label.y + label.height / 2 <= 320,
      ),
    ).toBe(true);
    for (let left = 0; left < mockState.labels.length; left += 1) {
      for (let right = left + 1; right < mockState.labels.length; right += 1) {
        const first = mockState.labels[left];
        const second = mockState.labels[right];
        expect(
          Math.abs(first.x - second.x) >= (first.width + second.width) / 2 + 6 ||
            Math.abs(first.y - second.y) >= (first.height + second.height) / 2 + 6,
        ).toBe(true);
      }
    }
    expect(
      Number.parseFloat(
        mockState.labels.find((label) => label.text === "Holloway")?.fontSize ?? "0",
      ),
    ).toBeGreaterThanOrEqual(12);
    expect(mockState.trails).toHaveLength(3);
    const connectedTownCenters = mockState.circles.map((circle) => `${circle.x},${circle.y}`);
    expect(connectedTownCenters).toContain(mockState.trails[0].from.join(","));
    expect(connectedTownCenters).toContain(mockState.trails[0].to.join(","));

    mockState.circles[0].handlers.pointerdown();
    expect(onTownSelected).toHaveBeenCalledWith("holloway");
  });
});

describe("PhaserMapHost town selection", () => {
  it("forwards the selected town without fetching during selection", () => {
    const onTownSelected = vi.fn();
    const fetchSpy = vi.spyOn(globalThis, "fetch");
    renderHost({ onTownSelected });

    const scene = mockState.games[0].config.scene;
    scene.selectTown("t-town");

    expect(onTownSelected).toHaveBeenCalledTimes(1);
    expect(onTownSelected).toHaveBeenCalledWith("t-town");
    expect(fetchSpy).not.toHaveBeenCalled();
    fetchSpy.mockRestore();
  });

  it("does not mutate selectedTownId when selectTown is called", () => {
    renderHost({ selectedTownId: null });

    const scene = mockState.games[0].config.scene;
    const before = scene.selectedTownId;
    scene.selectTown("t-town");

    expect(scene.selectedTownId).toBe(before);
    expect(scene.selectedTownId).toBeNull();
  });
});

describe("PhaserMapHost travel mode", () => {
  it("only emits onTownSelected for selectable towns when selectableTownIds is provided", () => {
    const onTownSelected = vi.fn();
    render(
      <PhaserMapHost
        mapData={createMapData()}
        selectedTownId={null}
        onTownSelected={onTownSelected}
        currentTownId="t-town"
        selectableTownIds={["dust-fork"]}
      />,
    );

    const scene = mockState.games[0].config.scene;
    scene.selectTown("dust-fork");
    expect(onTownSelected).toHaveBeenCalledWith("dust-fork");

    scene.selectTown("t-town");
    expect(onTownSelected).not.toHaveBeenCalledWith("t-town");
  });

  it("makes all towns selectable when selectableTownIds is not provided", () => {
    const onTownSelected = vi.fn();
    renderHost({ onTownSelected });

    const scene = mockState.games[0].config.scene;
    scene.selectTown("t-town");
    expect(onTownSelected).toHaveBeenCalledWith("t-town");
  });
});
