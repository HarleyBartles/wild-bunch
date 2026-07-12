import Phaser from "phaser";
import { AvailableActionKind, BuildingView } from "../../api/types";
import { BuildingKind } from "./types";
import type { BuildingPlacementDto, TownLayoutDto } from "./types";
import {
  getBackgroundSpriteKey,
  getBackgroundSpriteUrl,
  getSpriteUrl,
} from "./sprite-loader";
import {
  getDirtTileUrl,
  getPathTileUrl,
  getPropSpriteUrl,
  pickDirtMirroring,
  pickPropPlacement,
  getRoadTileUrl,
  getSpurTileUrl,
  pickDirtVariantIndex,
  pickPropKind,
  shouldPlaceProp,
} from "./ground-loader";
import {
  collectForegroundOccupiedSlots,
  planBackgroundBuildings,
  planSpurCrossTiles,
  getTrailheadFootprintTiles,
  type PlannedBackgroundBuilding,
  type SpurCrossTile,
} from "./background-building-planner";

const BUILDING_COLORS: Record<BuildingKind, number> = {
  [BuildingKind.Store]: 0x8b6914,
  [BuildingKind.Sheriff]: 0x4a6a8a,
  [BuildingKind.Saloon]: 0x8b3a3a,
  [BuildingKind.Trailhead]: 0x5a7a4a,
  [BuildingKind.Telegraph]: 0x6a6a6a,
};

const BUILDING_LABELS: Record<BuildingKind, string> = {
  [BuildingKind.Store]: "Store",
  [BuildingKind.Sheriff]: "Sheriff",
  [BuildingKind.Saloon]: "Saloon",
  [BuildingKind.Trailhead]: "Trailhead",
  [BuildingKind.Telegraph]: "Telegraph",
};

const TileGridWidth = 10;
const TileGridHeight = 10;
const TilePixelWidth = 80;
const TilePixelHeight = 50;
const BuildingNudgeRatio = 0.3;

type RoadVariant = "flat" | "path" | "spur";
type SpurVariant = "straight" | "path" | "end-cap" | "cross";
type PathOrientation = "horizontal" | "vertical";
type PathVariant = "straight" | "diagonal";

interface TilePoint {
  row: number;
  col: number;
}

function isRoadTile(tileType: number): boolean {
  return tileType === 1;
}

function isSpurStartTile(tileType: number): boolean {
  return tileType === 3;
}

function isSpurRoadTile(tileType: number): boolean {
  return tileType === 4;
}

function getCell(layout: TownLayoutDto, row: number, col: number): number {
  return layout.tileGrid?.[row]?.[col] ?? 0;
}

function logicalToTileCell(logicalX: number, logicalY: number): TilePoint {
  return {
    col: Math.floor(logicalX / 10),
    row: Math.floor(logicalY / 10),
  };
}

export function isBuildingAvailable(kind: BuildingKind, actions: AvailableActionKind[]): boolean {
  switch (kind) {
    case BuildingKind.Store:
      return actions.includes(AvailableActionKind.BuySupplies);
    case BuildingKind.Sheriff:
      return (
        actions.includes(AvailableActionKind.ReadWantedPosters) ||
        actions.includes(AvailableActionKind.CheckSheriffRecords)
      );
    case BuildingKind.Saloon:
      return (
        actions.includes(AvailableActionKind.LookAroundSaloon) ||
        actions.includes(AvailableActionKind.GatherLocalGossip)
      );
    case BuildingKind.Trailhead:
      return actions.includes(AvailableActionKind.Travel);
    case BuildingKind.Telegraph:
      return false;
    default:
      return false;
  }
}

export class TownHubScene extends Phaser.Scene {
  public readonly layout: TownLayoutDto;
  private readonly availableActions: AvailableActionKind[] = [];
  private readonly onBuildingSelected: (kind: BuildingKind) => void;
  public backgroundPlacements: PlannedBackgroundBuilding[];
  public spurCrossTiles: SpurCrossTile[];
  public spurCrossTileKeys: Set<string>;

  // Canvas dimensions in pixels. The play surface is a 10x10 tile grid at 80x50 each.
  private static readonly CanvasWidth = TileGridWidth * TilePixelWidth;
  private static readonly CanvasHeight = TileGridHeight * TilePixelHeight;

  constructor(
    layout: TownLayoutDto,
    availableActions: AvailableActionKind[],
    onBuildingSelected: (kind: BuildingKind) => void,
  ) {
    super("town-hub");
    this.layout = layout;
    this.availableActions = availableActions;
    this.onBuildingSelected = onBuildingSelected;
    const occupiedSlots = collectForegroundOccupiedSlots(layout);
    this.backgroundPlacements = planBackgroundBuildings(layout, occupiedSlots);
    this.spurCrossTiles = planSpurCrossTiles(layout, this.backgroundPlacements);
    this.spurCrossTileKeys = new Set(this.spurCrossTiles.map((tile) => `${tile.row}:${tile.col}`));
  }

  preload(): void {
    const prosperity = this.layout.prosperity;

    for (const building of this.layout.buildings) {
      const spriteUrl = getSpriteUrl(building.kind, building.view, prosperity);
      if (spriteUrl) {
        this.load.image(`building-${building.kind}`, spriteUrl);
      }
    }

    this.load.image("dirt-1", getDirtTileUrl(0));
    this.load.image("dirt-2", getDirtTileUrl(1));
    this.load.image("dirt-3", getDirtTileUrl(2));

    this.load.image("road-main-flat", getRoadTileUrl("flat"));
    this.load.image("road-main-path", getRoadTileUrl("path"));
    this.load.image("road-main-spur", getRoadTileUrl("spur"));

    this.load.image("spur-road-straight", getSpurTileUrl("straight"));
    this.load.image("spur-road-path", getSpurTileUrl("path"));
    this.load.image("spur-road-end-cap", getSpurTileUrl("end-cap"));
    this.load.image("spur-road-cross", getSpurTileUrl("cross"));

    const backgroundTileLoads = new Map<string, string>();
    for (const placement of this.backgroundPlacements) {
      backgroundTileLoads.set(
        getBackgroundSpriteKey(placement.family, placement.view, prosperity),
        getBackgroundSpriteUrl(placement.family, placement.view, prosperity),
      );
    }
    for (const [spriteKey, spriteUrl] of backgroundTileLoads) {
      this.load.image(spriteKey, spriteUrl);
    }

    this.load.image("path-horizontal-straight", getPathTileUrl("horizontal", "straight"));
    this.load.image("path-horizontal-diagonal", getPathTileUrl("horizontal", "diagonal"));
    this.load.image("path-vertical-straight", getPathTileUrl("vertical", "straight"));
    this.load.image("path-vertical-diagonal", getPathTileUrl("vertical", "diagonal"));

    this.load.image("prop-barrel", getPropSpriteUrl("barrel"));
    this.load.image("prop-cactus", getPropSpriteUrl("cactus"));
    this.load.image("prop-fence-piece", getPropSpriteUrl("fence-piece"));
    this.load.image("prop-tumbleweed", getPropSpriteUrl("tumbleweed"));
    this.load.image("prop-water-trough", getPropSpriteUrl("water-trough"));
  }

  selectBuilding(kind: BuildingKind): void {
    if (!isBuildingAvailable(kind, this.availableActions)) {
      return;
    }
    this.onBuildingSelected(kind);
  }

  create(): void {
    this.renderDirtTiles();
    this.renderBuildingGroundTiles();
    this.renderPathTiles();
    this.renderRoadTiles();
    this.renderSpurTiles();
    this.renderSpurEndCapTiles();
    this.renderBackgroundBuildingGroundTiles();
    this.renderPropTiles();
    this.renderBackgroundBuildings();
    this.renderBuildings();
    this.add.circle(this.layout.playerSpawnX * 8, this.layout.playerSpawnY * 5, 12, 0xffd700);
  }

  private renderDirtTiles(): void {
    const seed = this.layout.layoutSalts?.dirtSalt ?? this.layout.resolverVersion ?? "town-hub-dirt";

    for (let row = 0; row < TileGridHeight; row++) {
      for (let col = 0; col < TileGridWidth; col++) {
        const variantIndex = pickDirtVariantIndex(seed, row, col);
        const mirroring = pickDirtMirroring(seed, row, col);
        this.add
          .image(
            col * TilePixelWidth + TilePixelWidth / 2,
            row * TilePixelHeight + TilePixelHeight / 2,
            `dirt-${variantIndex + 1}`,
          )
          .setDisplaySize(TilePixelWidth, TilePixelHeight)
          .setFlipX(mirroring.flipX)
          .setFlipY(mirroring.flipY);
      }
    }
  }

  private renderRoadTiles(): void {
    for (let row = 0; row < TileGridHeight; row++) {
      for (const col of [4, 5]) {
        const tileType = getCell(this.layout, row, col);
        if (!isRoadTile(tileType)) {
          continue;
        }

        const variant = this.getRoadVariantForTile(row, col);
        const sprite = this.add
          .image(
            col * TilePixelWidth + TilePixelWidth / 2,
            row * TilePixelHeight + TilePixelHeight / 2,
            this.getRoadKey(variant),
          )
          .setDisplaySize(TilePixelWidth, TilePixelHeight);

        if (col === 4) {
          sprite.setFlipX(true);
        }
      }
    }
  }

  private renderSpurTiles(): void {
    for (let row = 0; row < TileGridHeight; row++) {
      for (let col = 0; col < TileGridWidth; col++) {
        const tileType = getCell(this.layout, row, col);
        if (!isSpurStartTile(tileType) && !isSpurRoadTile(tileType)) {
          continue;
        }

        const side = col < 5 ? "west" : "east";
        const above = this.hasPlacementAboveSpur(row, col);
        const below = this.hasPlacementBelowSpur(row, col);
        const variant: SpurVariant =
          tileType === 4 && this.spurCrossTileKeys.has(`${row}:${col}`)
            ? "cross"
            : tileType === 3
              ? "straight"
              : above || below
                ? "path"
                : "straight";
        const sprite = this.add
          .image(
            col * TilePixelWidth + TilePixelWidth / 2,
            row * TilePixelHeight + TilePixelHeight / 2,
            this.getSpurKey(variant),
          )
          .setDisplaySize(TilePixelWidth, TilePixelHeight);

        if (side === "west") {
          sprite.setFlipX(true);
        }
      }
    }
  }

  private renderSpurEndCapTiles(): void {
    for (const building of this.layout.buildings) {
      const tile = logicalToTileCell(building.x, building.y);
      if (!this.hasSpurBelow(tile.row, tile.col)) {
        continue;
      }

      const isWestSide = tile.col < 5;
      const capCol = isWestSide ? tile.col - 1 : tile.col + 1;
      const capRow = tile.row + 1;
      if (capCol < 0 || capCol >= TileGridWidth || capRow < 0 || capRow >= TileGridHeight) {
        continue;
      }

      this.add
        .image(
          capCol * TilePixelWidth + TilePixelWidth / 2,
          capRow * TilePixelHeight + TilePixelHeight / 2,
          "spur-road-end-cap",
        )
        .setDisplaySize(TilePixelWidth, TilePixelHeight)
        .setFlipX(isWestSide);
    }
  }

  private renderPathTiles(): void {
    for (const path of this.layout.paths) {
      const start = logicalToTileCell(path.startX, path.startY);
      const end = logicalToTileCell(path.endX, path.endY);
      const points = this.rasterizeLine(start, end);

      for (let index = 0; index < points.length; index++) {
        const point = points[index];
        const previous = index > 0 ? points[index - 1] : null;
        const next = index < points.length - 1 ? points[index + 1] : null;
        const { orientation, variant, flipX } = this.getPathSpriteForPoint(point, previous, next);
        const key = this.getPathKey(orientation, variant);

        this.add
          .image(
            point.col * TilePixelWidth + TilePixelWidth / 2,
            point.row * TilePixelHeight + TilePixelHeight / 2,
            key,
          )
          .setDisplaySize(TilePixelWidth, TilePixelHeight)
          .setFlipX(flipX);
      }
    }
  }

  private renderPropTiles(): void {
    const seed = this.layout.layoutSalts?.propsSalt ?? this.layout.resolverVersion ?? "town-hub-props";

    for (let row = 0; row < TileGridHeight; row++) {
      for (let col = 0; col < TileGridWidth; col++) {
        if (getCell(this.layout, row, col) !== 0) {
          continue;
        }
        if (!shouldPlaceProp(seed, row, col, this.isBlockedByAnyBuildingPlacement(row, col))) {
          continue;
        }

        const kind = pickPropKind(seed, row, col);
        const placement = pickPropPlacement(seed, row, col, kind);
        this.add
          .image(
            col * TilePixelWidth + TilePixelWidth / 2 + placement.offsetX,
            row * TilePixelHeight + TilePixelHeight / 2 + placement.offsetY,
            `prop-${kind}`,
          )
          .setScale(placement.scale);
      }
    }
  }

  private renderBackgroundBuildingGroundTiles(): void {
    for (const placement of this.backgroundPlacements) {
      const groundTile = this.getBackgroundPlacementGroundTile(placement);
      if (!groundTile) {
        continue;
      }

      this.add
        .image(
          placement.col * TilePixelWidth + TilePixelWidth / 2,
          placement.row * TilePixelHeight + TilePixelHeight / 2,
          groundTile.key,
        )
        .setDisplaySize(TilePixelWidth, TilePixelHeight)
        .setFlipX(groundTile.flipX)
        .setFlipY(groundTile.flipY);
    }
  }

  private renderBackgroundBuildings(): void {
    for (const placement of this.backgroundPlacements) {
      const spriteKey = getBackgroundSpriteKey(placement.family, placement.view, this.layout.prosperity);
      const offset = this.getBackgroundPlacementOffset(placement);
      this.add
        .image(
          placement.col * TilePixelWidth + TilePixelWidth / 2 + offset.x,
          placement.row * TilePixelHeight + TilePixelHeight / 2 + offset.y,
          spriteKey,
        )
        .setDisplaySize(TilePixelWidth, TilePixelHeight)
        .setFlipX(placement.flipX)
        .setFlipY(placement.flipY);
    }
  }

  private renderBuildings(): void {
    const sx = TownHubScene.CanvasWidth / 100;
    const sy = TownHubScene.CanvasHeight / 100;

    for (const building of this.layout.buildings) {
      const buildingTile = logicalToTileCell(building.x, building.y);
      const buildingOffset = this.getBuildingOffset(buildingTile.row, buildingTile.col);
      const px = building.x * sx + buildingOffset.x;
      const py = building.y * sy + buildingOffset.y;
      const pw = building.width * sx;
      const ph = building.height * sy;

      const spriteKey = `building-${building.kind}`;
      if (this.textures.exists(spriteKey)) {
        const sprite = this.add.image(px, py, spriteKey);
        sprite.setDisplaySize(pw, ph);

        if (building.x < 50) {
          sprite.setFlipX(true);
        }

        if (building.kind === BuildingKind.Telegraph) {
          sprite.setAlpha(0.6);
        } else if (isBuildingAvailable(building.kind, this.availableActions)) {
          sprite.setInteractive({ useHandCursor: true });
          sprite.on("pointerover", () => sprite.setScale(1.05));
          sprite.on("pointerout", () => sprite.setScale(1));
          sprite.on("pointerdown", () => this.selectBuilding(building.kind));
        } else {
          sprite.setAlpha(0.4);
        }
      } else {
        const color = BUILDING_COLORS[building.kind] ?? 0x6a6a6a;
        const rect = this.add.rectangle(px, py, pw, ph, color);

        if (building.kind === BuildingKind.Telegraph) {
          rect.setAlpha(0.6);
        } else if (isBuildingAvailable(building.kind, this.availableActions)) {
          rect.setStrokeStyle(2, 0xffffff);
          rect.setInteractive({ useHandCursor: true });
          rect.on("pointerover", () => rect.setScale(1.05));
          rect.on("pointerout", () => rect.setScale(1));
          rect.on("pointerdown", () => this.selectBuilding(building.kind));
        } else {
          rect.setAlpha(0.4);
        }
      }

      const label = BUILDING_LABELS[building.kind] ?? "Building";
      this.add
        .text(px, py + ph / 2 + 12, label, {
          fontSize: "12px",
          color: "#fff",
        })
        .setOrigin(0.5);
    }
  }

  private renderBuildingGroundTiles(): void {
    for (const building of this.layout.buildings) {
      const tile = logicalToTileCell(building.x, building.y);
      const groundTile = this.getBuildingGroundTile(this.layout, building, tile.row, tile.col);
      if (!groundTile) {
        continue;
      }

      this.add
        .image(
          tile.col * TilePixelWidth + TilePixelWidth / 2,
          tile.row * TilePixelHeight + TilePixelHeight / 2,
          groundTile.key,
        )
        .setDisplaySize(TilePixelWidth, TilePixelHeight)
        .setFlipX(groundTile.flipX)
        .setFlipY(groundTile.flipY);
    }
  }

  private getRoadVariantForTile(row: number, col: number): RoadVariant {
    if (col === 4) {
      if (this.hasBuildingInTile(row, 3)) {
        return "path";
      }
      if (getCell(this.layout, row, 3) === 3) {
        return "spur";
      }
    }
    if (col === 5) {
      if (this.hasBuildingInTile(row, 6)) {
        return "path";
      }
      if (getCell(this.layout, row, 6) === 3) {
        return "spur";
      }
    }
    return "flat";
  }

  private getRoadKey(variant: RoadVariant): string {
    return variant === "flat" ? "road-main-flat" : variant === "path" ? "road-main-path" : "road-main-spur";
  }

  private getSpurKey(variant: SpurVariant): string {
    return variant === "straight"
      ? "spur-road-straight"
      : variant === "path"
        ? "spur-road-path"
        : variant === "end-cap"
          ? "spur-road-end-cap"
          : "spur-road-cross";
  }

  private getPathKey(orientation: PathOrientation, variant: PathVariant): string {
    return orientation === "horizontal"
      ? variant === "straight"
        ? "path-horizontal-straight"
        : "path-horizontal-diagonal"
      : variant === "straight"
        ? "path-vertical-straight"
        : "path-vertical-diagonal";
  }

  private getPathSpriteForPoint(
    point: TilePoint,
    previous: TilePoint | null,
    next: TilePoint | null,
  ): { orientation: PathOrientation; variant: PathVariant; flipX: boolean } {
    if (!previous || !next) {
      const step = next ?? previous;
      const orientation = step && step.row !== point.row ? "vertical" : "horizontal";
      return {
        orientation,
        variant: "straight",
        flipX: orientation === "horizontal" ? (step?.col ?? point.col) < point.col : false,
      };
    }

    const dx = next.col - previous.col;
    const dy = next.row - previous.row;
    const orientation: PathOrientation = Math.abs(dx) >= Math.abs(dy) ? "horizontal" : "vertical";
    return {
      orientation,
      variant: dx !== 0 && dy !== 0 ? "diagonal" : "straight",
      flipX: orientation === "horizontal" ? dx < 0 : false,
    };
  }

  private getBuildingOffset(row: number, col: number): { x: number; y: number } {
    if (this.hasSpurBelow(row, col) || getCell(this.layout, row, col) === 4) {
      return {
        x: 0,
        y: -(TilePixelHeight * BuildingNudgeRatio),
      };
    }

    if (col === 3 && getCell(this.layout, row, 4) === 1) {
      return {
        x: -(TilePixelWidth * BuildingNudgeRatio),
        y: 0,
      };
    }

    if (col === 6 && getCell(this.layout, row, 5) === 1) {
      return {
        x: TilePixelWidth * BuildingNudgeRatio,
        y: 0,
      };
    }

    return { x: 0, y: 0 };
  }

  private isBlockedByAnyBuildingPlacement(row: number, col: number): boolean {
    const blocksForeground = this.layout.buildings.some((building) => {
      if (building.kind === BuildingKind.Trailhead) {
        return getTrailheadFootprintTiles(building).some((tile) => tile.row === row && tile.col === col);
      }

      const tile = logicalToTileCell(building.x, building.y);
      const offset = this.getBuildingOffset(tile.row, tile.col);
      if (offset.x === 0 && offset.y === 0) {
        return tile.row === row && tile.col === col;
      }

      const blockedRow = tile.row + Math.sign(offset.y);
      const blockedCol = tile.col + Math.sign(offset.x);
      return (tile.row === row && tile.col === col) || (blockedRow === row && blockedCol === col);
    });

    if (blocksForeground) {
      return true;
    }

    return this.backgroundPlacements.some((placement) => {
      const offset = this.getBackgroundPlacementOffset(placement);
      if (offset.x === 0 && offset.y === 0) {
        return placement.row === row && placement.col === col;
      }

      const blockedRow = placement.row + Math.sign(offset.y);
      const blockedCol = placement.col + Math.sign(offset.x);
      return (placement.row === row && placement.col === col) || (blockedRow === row && blockedCol === col);
    });
  }

  private hasSpurBelow(row: number, col: number): boolean {
    return row + 1 < TileGridHeight && getCell(this.layout, row + 1, col) === 4;
  }

  private hasBuildingInTile(row: number, col: number): boolean {
    return this.layout.buildings.some((building) => {
      const tile = logicalToTileCell(building.x, building.y);
      return tile.row === row && tile.col === col;
    });
  }

  private hasPlacementAboveSpur(row: number, col: number): boolean {
    return this.hasPlacementAt(row - 1, col);
  }

  private hasPlacementBelowSpur(row: number, col: number): boolean {
    return this.hasPlacementAt(row + 1, col);
  }

  private hasPlacementAt(row: number, col: number): boolean {
    return (
      this.layout.buildings.some((building) => {
        if (building.kind === BuildingKind.Trailhead) {
          return getTrailheadFootprintTiles(building).some((tile) => tile.row === row && tile.col === col);
        }

        const tile = logicalToTileCell(building.x, building.y);
        return tile.row === row && tile.col === col;
      }) ||
      this.backgroundPlacements.some((placement) => placement.row === row && placement.col === col)
    );
  }

  private getBuildingGroundTile(
    layout: TownLayoutDto,
    building: BuildingPlacementDto,
    row: number,
    col: number,
  ): { key: string; flipX: boolean; flipY: boolean } | null {
    if (this.hasSpurBelow(row, col)) {
      return this.getSpurBuildingGroundTile(building.view, building.x < 50 ? "west" : "east");
    }

    if (col === 3 && getCell(layout, row, 4) === 1) {
      return this.getMainRoadBuildingGroundTile(building.view, "west");
    }

    if (col === 6 && getCell(layout, row, 5) === 1) {
      return this.getMainRoadBuildingGroundTile(building.view, "east");
    }

    return null;
  }

  private getSpurBuildingGroundTile(
    view: BuildingView,
    side: "east" | "west",
    flipY = false,
  ): { key: string; flipX: boolean; flipY: boolean } {
    const mirrored = side === "west";
    switch (view) {
      case BuildingView.Front:
      case BuildingView.Profile:
      case BuildingView.Rear:
        return { key: "path-vertical-straight", flipX: false, flipY };
      case BuildingView.FrontOblique:
      case BuildingView.RearOblique:
        return { key: "path-vertical-diagonal", flipX: mirrored, flipY };
      default:
        return { key: "path-vertical-straight", flipX: false, flipY };
    }
  }

  private getBackgroundPlacementOffset(placement: PlannedBackgroundBuilding): { x: number; y: number } {
    if (placement.attachesTo === "road") {
      return {
        x: placement.side === "west" ? -(TilePixelWidth * BuildingNudgeRatio) : TilePixelWidth * BuildingNudgeRatio,
        y: 0,
      };
    }

    return {
      x: 0,
      y: placement.attachesTo === "spur-below" ? TilePixelHeight * BuildingNudgeRatio : -(TilePixelHeight * BuildingNudgeRatio),
    };
  }

  private getBackgroundPlacementGroundTile(
    placement: PlannedBackgroundBuilding,
  ): { key: string; flipX: boolean; flipY: boolean } | null {
    if (placement.attachesTo === "road") {
      return this.getMainRoadBuildingGroundTile(placement.view, placement.side);
    }

    return this.getSpurBuildingGroundTile(
      placement.view,
      placement.side,
      placement.attachesTo === "spur-below",
    );
  }

  private getMainRoadBuildingGroundTile(
    view: BuildingView,
    side: "east" | "west",
  ): { key: string; flipX: boolean; flipY: boolean } {
    const base =
      view === BuildingView.Profile
        ? { key: "path-horizontal-straight", flipX: false, flipY: false }
        : view === BuildingView.FrontOblique
          ? { key: "path-horizontal-diagonal", flipX: false, flipY: false }
          : view === BuildingView.RearOblique
            ? { key: "path-horizontal-diagonal", flipX: false, flipY: true }
            : { key: "path-horizontal-straight", flipX: false, flipY: false };

    if (side === "west") {
      return { ...base, flipX: !base.flipX };
    }

    return base;
  }

  private rasterizeLine(start: TilePoint, end: TilePoint): TilePoint[] {
    const points: TilePoint[] = [];
    let x0 = start.col;
    let y0 = start.row;
    const x1 = end.col;
    const y1 = end.row;

    const dx = Math.abs(x1 - x0);
    const sx = x0 < x1 ? 1 : -1;
    const dy = -Math.abs(y1 - y0);
    const sy = y0 < y1 ? 1 : -1;
    let error = dx + dy;

    while (true) {
      points.push({ row: y0, col: x0 });
      if (x0 === x1 && y0 === y1) {
        break;
      }

      const doubled = error * 2;
      if (doubled >= dy) {
        error += dy;
        x0 += sx;
      }
      if (doubled <= dx) {
        error += dx;
        y0 += sy;
      }
    }

    return points;
  }
}
