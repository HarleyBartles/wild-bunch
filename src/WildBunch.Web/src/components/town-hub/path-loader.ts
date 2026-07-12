export type PathTileName =
  | "path-horizontal-diagonal"
  | "path-horizontal-straight"
  | "path-vertical-diagonal"
  | "path-vertical-straight";

export function getPathTileUrl(name: PathTileName): string {
  return `/assets/town-hub-roads/path/${name}.png`;
}
