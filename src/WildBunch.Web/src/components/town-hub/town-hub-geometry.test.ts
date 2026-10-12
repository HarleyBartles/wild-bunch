import { describe, expect, it } from "vitest";
import type { TownLayoutDto } from "../../api/types";
import { getTownTileCell, logicalToTileCell } from "./town-hub-geometry";

describe("town hub tile geometry", () => {
  it("maps the origin to the first tile cell", () => {
    expect(logicalToTileCell(0, 0)).toEqual({ row: 0, col: 0 });
  });

  it("maps logical coordinates to the cell beginning at their ten-unit boundary", () => {
    expect(logicalToTileCell(20, 30)).toEqual({ row: 3, col: 2 });
  });

  it("maps the last supported logical coordinates to the final grid cell", () => {
    expect(logicalToTileCell(99, 99)).toEqual({ row: 9, col: 9 });
  });

  it("reads the requested tile and falls back to zero for absent rows, cells, or grids", () => {
    const layout = { tileGrid: [[7], [0, 2]] } as TownLayoutDto;
    const missingGrid = {} as TownLayoutDto;

    expect(getTownTileCell(layout, 0, 0)).toBe(7);
    expect(getTownTileCell(layout, 1, 1)).toBe(2);
    expect(getTownTileCell(layout, 1, 3)).toBe(0);
    expect(getTownTileCell(layout, 3, 1)).toBe(0);
    expect(getTownTileCell(missingGrid, 0, 0)).toBe(0);
  });
});
