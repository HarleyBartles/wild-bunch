const DIRT_VARIANTS = ["dirt-1", "dirt-2", "dirt-3"] as const;
const PROP_KINDS = ["barrel", "cactus", "fence-piece", "tumbleweed", "water-trough"] as const;

type Side = "east" | "west";
type RoadVariant = "flat" | "path" | "spur";
type SpurVariant = "straight" | "path" | "end-cap" | "cross";
type PathOrientation = "horizontal" | "vertical";
type PathVariant = "straight" | "diagonal";
type PropKind = (typeof PROP_KINDS)[number];

export interface PropPlacement {
  offsetX: number;
  offsetY: number;
  scale: number;
}

export interface TileMirroring {
  flipX: boolean;
  flipY: boolean;
}

const PropScale = 0.6;
const PropJitterX = 12;
const PropJitterY = 8;

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

function jitter(seed: string, magnitude: number): number {
  return (hashString(seed) % (magnitude * 2 + 1)) - magnitude;
}

export function getDirtTileUrl(variantIndex: number): string {
  const variant = DIRT_VARIANTS[((variantIndex % DIRT_VARIANTS.length) + DIRT_VARIANTS.length) % DIRT_VARIANTS.length];
  return `/assets/town-hub-ground/dirt/${variant}.png`;
}

export function getRoadTileUrl(variant: RoadVariant): string {
  const fileName = variant === "flat" ? "road-flat-edge" : variant === "path" ? "road-path-edge" : "road-spur-edge";
  return `/assets/town-hub-roads/main-road/${fileName}.png`;
}

export function getSpurTileUrl(variant: SpurVariant): string {
  const fileName =
    variant === "straight"
      ? "spur-straight"
      : variant === "path"
        ? "spur-path-edge"
        : variant === "end-cap"
          ? "spur-end-cap"
          : "spur-path-cross";
  return `/assets/town-hub-roads/spur-road/${fileName}.png`;
}

export function getPathTileUrl(orientation: PathOrientation, variant: PathVariant): string {
  return `/assets/town-hub-roads/path/path-${orientation}-${variant}.png`;
}

export function getPropSpriteUrl(kind: PropKind): string {
  return `/assets/town-hub-ground/props/${kind}-normalized.png`;
}

export function pickDirtVariantIndex(seed: string, row: number, col: number): number {
  return hashString(seedForCell(seed, row, col, "dirt")) % DIRT_VARIANTS.length;
}

export function pickDirtMirroring(seed: string, row: number, col: number): TileMirroring {
  const orientation = hashString(seedForCell(seed, row, col, "dirt-mirror")) % 4;
  return {
    flipX: orientation === 1 || orientation === 3,
    flipY: orientation === 2 || orientation === 3,
  };
}

export function shouldPlaceProp(seed: string, row: number, col: number, blockedByBuilding = false): boolean {
  if (blockedByBuilding) {
    return false;
  }
  return hashString(seedForCell(seed, row, col, "prop")) % 23 === 0;
}

export function pickPropKind(seed: string, row: number, col: number): PropKind {
  return PROP_KINDS[hashString(seedForCell(seed, row, col, "prop-kind")) % PROP_KINDS.length];
}

export function pickPropPlacement(seed: string, row: number, col: number, kind: PropKind): PropPlacement {
  const placementSeed = seedForCell(seed, row, col, `prop-${kind}`);
  return {
    offsetX: jitter(`${placementSeed}:x`, PropJitterX),
    offsetY: jitter(`${placementSeed}:y`, PropJitterY),
    scale: PropScale,
  };
}
