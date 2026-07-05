export enum BuildingKind {
  Store = 0,
  Sheriff = 1,
  Saloon = 2,
  Trailhead = 3,
  Telegraph = 4,
}

export interface BuildingPlacementDto {
  kind: BuildingKind;
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface TownLayoutDto {
  buildings: BuildingPlacementDto[];
  playerSpawnX: number;
  playerSpawnY: number;
}
