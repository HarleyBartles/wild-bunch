export function hashTownVisualSeed(value: string): number {
  let hash = 2166136261;
  for (let index = 0; index < value.length; index++) {
    hash ^= value.charCodeAt(index);
    hash = Math.imul(hash, 16777619);
  }
  return hash >>> 0;
}

export function townVisualCellSeed(seed: string, row: number, col: number, suffix: string): string {
  return `${seed}:${suffix}:${row}:${col}`;
}
