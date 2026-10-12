import type { TownLayoutDto } from "./types";

export const TownTileColumns = 10;
export const TownTileRows = 10;

export interface TownTileCell {
  row: number;
  col: number;
}

export function logicalToTileCell(logicalX: number, logicalY: number): TownTileCell {
  return {
    col: Math.floor(logicalX / 10),
    row: Math.floor(logicalY / 10),
  };
}

export function getTownTileCell(layout: TownLayoutDto, row: number, col: number): number {
  return layout.tileGrid?.[row]?.[col] ?? 0;
}
