import { BuildingKind, BuildingView, TownProsperity, type BuildingPlacementDto, type TownLayoutDto } from "../../api/types";

export interface BackgroundSlot {
  row: number;
  col: number;
  side: "east" | "west";
  attachesTo: "road" | "spur-above" | "spur-below";
}

export interface PlannedBackgroundBuilding {
  row: number;
  col: number;
  family: "background-house" | "background-shop";
  view: BuildingView;
  flipX: boolean;
  flipY: boolean;
  side: "east" | "west";
  attachesTo: "road" | "spur-above" | "spur-below";
}

export interface SpurCrossTile {
  row: number;
  col: number;
  flipX: boolean;
  flipY: boolean;
}

interface SlotCandidate extends BackgroundSlot {
  score: number;
}

const TileGridWidth = 10;
const TileGridHeight = 10;

const ROAD_VIEWS = [BuildingView.Front, BuildingView.Profile, BuildingView.Rear, BuildingView.FrontOblique, BuildingView.RearOblique] as const;
const SPUR_ABOVE_VIEWS = [BuildingView.Front, BuildingView.FrontOblique] as const;
const SPUR_BELOW_VIEWS = [BuildingView.Rear, BuildingView.RearOblique] as const;

function hashString(value: string): number {
  let hash = 2166136261;
  for (let index = 0; index < value.length; index++) {
    hash ^= value.charCodeAt(index);
    hash = Math.imul(hash, 16777619);
  }
  return hash >>> 0;
}

function seedForCell(seed: string, row: number, col: number, suffix: string): string {
  return `${seed}:${suffix}:${row}:${col}`;
}

function logicalToTileCell(building: BuildingPlacementDto): { row: number; col: number } {
  return {
    col: Math.floor(building.x / 10),
    row: Math.floor(building.y / 10),
  };
}

function getLayoutSeed(layout: TownLayoutDto): string {
  return layout.layoutSalts?.buildingsSalt ?? layout.resolverVersion ?? "town-hub-buildings";
}

function getCell(layout: TownLayoutDto, row: number, col: number): number {
  return layout.tileGrid?.[row]?.[col] ?? 0;
}

function isInsideGrid(row: number, col: number): boolean {
  return row >= 0 && row < TileGridHeight && col >= 0 && col < TileGridWidth;
}

function isEmptyCell(layout: TownLayoutDto, row: number, col: number): boolean {
  return isInsideGrid(row, col) && getCell(layout, row, col) === 0;
}

function getSlotKey(row: number, col: number): string {
  return `${row}:${col}`;
}

function candidateScore(seed: string, row: number, col: number, attachesTo: BackgroundSlot["attachesTo"]): number {
  return hashString(seedForCell(seed, row, col, attachesTo));
}

export function getTrailheadFootprintTiles(building: BuildingPlacementDto): Array<{ row: number; col: number }> {
  const left = Math.max(0, Math.floor((building.x - building.width / 2) / 10));
  const right = Math.min(TileGridWidth - 1, Math.ceil((building.x + building.width / 2) / 10) - 1);
  const top = Math.max(0, Math.floor((building.y - building.height / 2) / 10));
  const bottom = Math.min(TileGridHeight - 1, Math.ceil((building.y + building.height / 2) / 10) - 1);
  const tiles: Array<{ row: number; col: number }> = [];

  for (let row = top; row <= bottom; row++) {
    for (let col = left; col <= right; col++) {
      tiles.push({ row, col });
    }
  }

  return tiles;
}

function getBudgetRange(
  prosperity: TownProsperity,
  eligibleCount: number,
): { min: number; max: number } {
  switch (prosperity) {
    case TownProsperity.Destitute:
      return { min: 0, max: Math.min(2, eligibleCount) };
    case TownProsperity.Poor: {
      const min = Math.max(0, Math.ceil(eligibleCount * 0.2));
      const max = Math.max(min, Math.min(eligibleCount, Math.floor(eligibleCount * 0.4)));
      return { min, max };
    }
    case TownProsperity.Prosperous: {
      const min = Math.max(0, Math.ceil(eligibleCount * 0.6));
      const max = Math.max(min, Math.min(eligibleCount, Math.floor(eligibleCount * 0.8)));
      return { min, max };
    }
    case TownProsperity.Boomtown:
      return { min: Math.max(0, eligibleCount - 2), max: eligibleCount };
    default:
      return { min: 0, max: eligibleCount };
  }
}

function pickBudgetCount(seed: string, prosperity: TownProsperity, eligibleCount: number): number {
  if (eligibleCount <= 0) {
    return 0;
  }

  const { min, max } = getBudgetRange(prosperity, eligibleCount);
  const spread = Math.max(0, max - min);
  if (spread === 0) {
    return min;
  }

  return min + (hashString(seedForCell(seed, min, max, "budget")) % (spread + 1));
}

function pickFamily(seed: string, row: number, col: number, attachesTo: BackgroundSlot["attachesTo"]): "background-house" | "background-shop" {
  return hashString(seedForCell(seed, row, col, `${attachesTo}:family`)) % 2 === 0 ? "background-house" : "background-shop";
}

function pickView(
  seed: string,
  row: number,
  col: number,
  attachesTo: BackgroundSlot["attachesTo"],
): BuildingView {
  const pool =
    attachesTo === "spur-above"
      ? SPUR_ABOVE_VIEWS
      : attachesTo === "spur-below"
        ? SPUR_BELOW_VIEWS
        : ROAD_VIEWS;
  return pool[hashString(seedForCell(seed, row, col, `${attachesTo}:view`)) % pool.length];
}

function pickFlipX(col: number, attachesTo: BackgroundSlot["attachesTo"], view: BuildingView): boolean {
  if (attachesTo === "road") {
    return col < 5;
  }

  return view === BuildingView.FrontOblique || view === BuildingView.RearOblique ? col < 5 : false;
}

function pickFlipY(attachesTo: BackgroundSlot["attachesTo"]): boolean {
  return false;
}

export function collectForegroundOccupiedSlots(layout: TownLayoutDto): Set<string> {
  const occupied = new Set<string>();

  for (const building of layout.buildings) {
    const tiles =
      building.kind === BuildingKind.Trailhead ? getTrailheadFootprintTiles(building) : [logicalToTileCell(building)];
    for (const tile of tiles) {
      occupied.add(getSlotKey(tile.row, tile.col));
    }
  }

  return occupied;
}

function collectTrailheadExcludedSlots(layout: TownLayoutDto): Set<string> {
  const excluded = new Set<string>();

  for (const building of layout.buildings) {
    if (building.kind !== BuildingKind.Trailhead) {
      continue;
    }

    for (const tile of getTrailheadFootprintTiles(building)) {
      if (tile.col - 1 >= 0) {
        excluded.add(getSlotKey(tile.row, tile.col - 1));
      }
      if (tile.col + 1 < TileGridWidth) {
        excluded.add(getSlotKey(tile.row, tile.col + 1));
      }
    }
  }

  return excluded;
}

export function collectEligibleBackgroundSlots(layout: TownLayoutDto): BackgroundSlot[] {
  if (!layout.tileGrid) {
    return [];
  }

  const occupied = collectForegroundOccupiedSlots(layout);
  const trailheadExcluded = collectTrailheadExcludedSlots(layout);
  const seed = getLayoutSeed(layout);
  const seen = new Map<string, SlotCandidate>();

  const addCandidate = (slot: BackgroundSlot) => {
    const key = getSlotKey(slot.row, slot.col);
    if (trailheadExcluded.has(key)) {
      return;
    }
    const score = candidateScore(seed, slot.row, slot.col, slot.attachesTo);
    const candidate: SlotCandidate = { ...slot, score };
    const existing = seen.get(key);
    if (!existing || candidate.score < existing.score || (candidate.score === existing.score && candidate.attachesTo < existing.attachesTo)) {
      seen.set(key, candidate);
    }
  };

  for (let row = 0; row < TileGridHeight; row++) {
    if (getCell(layout, row, 4) === 1 && isEmptyCell(layout, row, 3) && !occupied.has(getSlotKey(row, 3))) {
      addCandidate({ row, col: 3, side: "west", attachesTo: "road" });
    }

    if (getCell(layout, row, 5) === 1 && isEmptyCell(layout, row, 6) && !occupied.has(getSlotKey(row, 6))) {
      addCandidate({ row, col: 6, side: "east", attachesTo: "road" });
    }
  }

  for (let row = 0; row < TileGridHeight; row++) {
    for (let col = 0; col < TileGridWidth; col++) {
      if (getCell(layout, row, col) !== 4) {
        continue;
      }

      if (isEmptyCell(layout, row - 1, col) && !occupied.has(getSlotKey(row - 1, col))) {
        addCandidate({
          row: row - 1,
          col,
          side: col < 5 ? "west" : "east",
          attachesTo: "spur-above",
        });
      }

      if (isEmptyCell(layout, row + 1, col) && !occupied.has(getSlotKey(row + 1, col))) {
        addCandidate({
          row: row + 1,
          col,
          side: col < 5 ? "west" : "east",
          attachesTo: "spur-below",
        });
      }
    }
  }

  return [...seen.values()].sort((left, right) => {
    if (left.score !== right.score) {
      return left.score - right.score;
    }
    if (left.row !== right.row) {
      return left.row - right.row;
    }
    if (left.col !== right.col) {
      return left.col - right.col;
    }
    return left.attachesTo.localeCompare(right.attachesTo);
  });
}

export function planBackgroundBuildings(
  layout: TownLayoutDto,
  occupiedSlots: Set<string>,
): PlannedBackgroundBuilding[] {
  if (!layout.tileGrid) {
    return [];
  }

  const seed = getLayoutSeed(layout);
  const eligible = collectEligibleBackgroundSlots(layout).filter((slot) => !occupiedSlots.has(getSlotKey(slot.row, slot.col)));
  const budget = pickBudgetCount(seed, layout.prosperity, eligible.length);
  const placements: PlannedBackgroundBuilding[] = [];

  for (const slot of eligible.slice(0, budget)) {
    const family = pickFamily(seed, slot.row, slot.col, slot.attachesTo);
    const view = pickView(seed, slot.row, slot.col, slot.attachesTo);
    placements.push({
      row: slot.row,
      col: slot.col,
      family,
      view,
      flipX: pickFlipX(slot.col, slot.attachesTo, view),
      flipY: pickFlipY(slot.attachesTo),
      side: slot.side,
      attachesTo: slot.attachesTo,
    });
  }

  return placements;
}

export function planSpurCrossTiles(
  layout: TownLayoutDto,
  backgroundPlacements: PlannedBackgroundBuilding[],
): SpurCrossTile[] {
  if (!layout.tileGrid) {
    return [];
  }

  const foregroundPlacements = collectForegroundOccupiedSlots(layout);
  const backgroundOccupied = new Set(backgroundPlacements.map((placement) => getSlotKey(placement.row, placement.col)));
  const hasPlacementAt = (row: number, col: number): boolean => {
    const key = getSlotKey(row, col);
    return foregroundPlacements.has(key) || backgroundOccupied.has(key);
  };

  const crossings = new Map<string, { row: number; col: number; above: boolean; below: boolean }>();

  for (let row = 0; row < TileGridHeight; row++) {
    for (let col = 0; col < TileGridWidth; col++) {
      if (getCell(layout, row, col) !== 4) {
        continue;
      }

      const above = isInsideGrid(row - 1, col) && hasPlacementAt(row - 1, col);
      const below = isInsideGrid(row + 1, col) && hasPlacementAt(row + 1, col);
      if (!above || !below) {
        continue;
      }

      crossings.set(getSlotKey(row, col), { row, col, above, below });
    }
  }

  return [...crossings.values()]
    .sort((left, right) => (left.row === right.row ? left.col - right.col : left.row - right.row))
    .map((crossing) => ({
      row: crossing.row,
      col: crossing.col,
      flipX: false,
      flipY: false,
    }));
}
