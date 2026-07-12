import { describe, expect, it } from "vitest";
import { BuildingKind, BuildingView, TownProsperity, type TownLayoutDto } from "../../api/types";
import {
  collectEligibleBackgroundSlots,
  collectForegroundOccupiedSlots,
  planBackgroundBuildings,
  planSpurCrossTiles,
} from "./background-building-planner";

function createGrid(): number[][] {
  return Array.from({ length: 10 }, () => Array.from({ length: 10 }, () => 0));
}

function createLayout(overrides: Partial<TownLayoutDto> = {}): TownLayoutDto {
  return {
    buildings: [
      { kind: BuildingKind.Store, view: BuildingView.Profile, x: 13, y: 13, width: 8, height: 10 },
    ],
    playerSpawnX: 50,
    playerSpawnY: 50,
    prosperity: TownProsperity.Prosperous,
    paths: [],
    tileGrid: createGrid(),
    layoutSalts: {
      resolverVersion: "1.0.0",
      buildingsSalt: "buildings-salt",
      roadsSalt: "roads-salt",
      dirtSalt: "dirt-salt",
      propsSalt: "props-salt",
    },
    ...overrides,
  };
}

describe("background building planner", () => {
  it("includes road and spur slots and excludes foreground occupancy", () => {
    const grid = createGrid();
    grid[2][4] = 1;
    grid[2][5] = 1;
    grid[4][6] = 4;

    const layout = createLayout({ tileGrid: grid });
    const occupied = collectForegroundOccupiedSlots(layout);
    const slots = collectEligibleBackgroundSlots(layout);

    expect(occupied.has("1:1")).toBe(true);
    expect(slots).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ row: 2, col: 3, attachesTo: "road" }),
        expect.objectContaining({ row: 2, col: 6, attachesTo: "road" }),
        expect.objectContaining({ row: 3, col: 6, attachesTo: "spur-above" }),
        expect.objectContaining({ row: 5, col: 6, attachesTo: "spur-below" }),
      ]),
    );
  });

  it("keeps destitute coverage within the expected filler range", () => {
    const grid = createGrid();
    grid[1][4] = 1;
    grid[1][5] = 1;
    grid[4][6] = 4;
    grid[6][3] = 4;

    const layout = createLayout({
      prosperity: TownProsperity.Destitute,
      tileGrid: grid,
    });

    const placements = planBackgroundBuildings(layout, collectForegroundOccupiedSlots(layout));
    expect(placements.length).toBeGreaterThanOrEqual(0);
    expect(placements.length).toBeLessThanOrEqual(2);
  });

  it("keeps prosperous coverage in the deterministic 60 to 80 percent band", () => {
    const grid = createGrid();
    grid[1][4] = 1;
    grid[1][5] = 1;
    grid[4][6] = 4;
    grid[6][3] = 4;
    grid[7][6] = 4;

    const layout = createLayout({
      prosperity: TownProsperity.Prosperous,
      tileGrid: grid,
    });

    const eligibleCount = collectEligibleBackgroundSlots(layout).length;
    const placements = planBackgroundBuildings(layout, collectForegroundOccupiedSlots(layout));
    const min = Math.max(0, Math.ceil(eligibleCount * 0.6));
    const max = Math.max(min, Math.min(eligibleCount, Math.floor(eligibleCount * 0.8)));

    expect(placements.length).toBeGreaterThanOrEqual(min);
    expect(placements.length).toBeLessThanOrEqual(max);
  });

  it("makes boomtown leave one or two empty eligible spaces", () => {
    const grid = createGrid();
    grid[1][4] = 1;
    grid[1][5] = 1;
    grid[4][6] = 4;
    grid[6][3] = 4;
    grid[7][6] = 4;
    grid[7][3] = 4;

    const layout = createLayout({
      prosperity: TownProsperity.Boomtown,
      tileGrid: grid,
    });

    const eligibleCount = collectEligibleBackgroundSlots(layout).length;
    const placements = planBackgroundBuildings(layout, collectForegroundOccupiedSlots(layout));
    expect(placements.length).toBeGreaterThanOrEqual(Math.max(0, eligibleCount - 2));
    expect(placements.length).toBeLessThanOrEqual(eligibleCount);
  });

  it("uses rear views for below-spur placements", () => {
    const grid = createGrid();
    grid[4][6] = 4;

    const layout = createLayout({
      prosperity: TownProsperity.Boomtown,
      tileGrid: grid,
      buildings: [{ kind: BuildingKind.Store, view: BuildingView.Profile, x: 65, y: 35, width: 8, height: 10 }],
      layoutSalts: {
        resolverVersion: "1.0.0",
        buildingsSalt: "town-hub-buildings",
        roadsSalt: "roads-salt",
        dirtSalt: "dirt-salt",
        propsSalt: "props-salt",
      },
    });

    const placements = planBackgroundBuildings(layout, collectForegroundOccupiedSlots(layout));
    expect(placements).toHaveLength(1);
    expect([BuildingView.Rear, BuildingView.RearOblique]).toContain(placements[0].view);
    expect(placements[0].flipY).toBe(true);
    expect(placements[0].attachesTo).toBe("spur-below");
  });

  it("produces spur cross tiles only when both sides of the same spur are occupied", () => {
    const grid = createGrid();
    grid[4][6] = 4;
    const layout = createLayout({ tileGrid: grid, buildings: [] });
    const backgroundPlacements = [
      {
        row: 3,
        col: 6,
        family: "background-house" as const,
        view: BuildingView.Front,
        flipX: false,
        flipY: false,
        side: "east" as const,
        attachesTo: "spur-above" as const,
      },
      {
        row: 5,
        col: 6,
        family: "background-shop" as const,
        view: BuildingView.Rear,
        flipX: false,
        flipY: true,
        side: "east" as const,
        attachesTo: "spur-below" as const,
      },
    ];

    expect(planSpurCrossTiles(layout, backgroundPlacements)).toEqual([
      { row: 4, col: 6, flipX: false, flipY: false },
    ]);
  });
});
