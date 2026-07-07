import Phaser from "phaser";
import { AvailableActionKind } from "../../api/types";
import { BuildingKind } from "./types";
import type { TownLayoutDto } from "./types";
import { getSpriteUrl } from "./sprite-loader";

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
  private readonly availableActions: AvailableActionKind[];
  private readonly onBuildingSelected: (kind: BuildingKind) => void;

  // Canvas dimensions in pixels. Logical coordinates from the domain (0-100)
  // are scaled to these dimensions for rendering.
  private static readonly CanvasWidth = 800;
  private static readonly CanvasHeight = 500;

  constructor(
    layout: TownLayoutDto,
    availableActions: AvailableActionKind[],
    onBuildingSelected: (kind: BuildingKind) => void,
  ) {
    super("town-hub");
    this.layout = layout;
    this.availableActions = availableActions;
    this.onBuildingSelected = onBuildingSelected;
  }

  preload(): void {
    // Load all building sprites based on the layout's prosperity tier
    const prosperity = this.layout.prosperity;

    for (const building of this.layout.buildings) {
      const spriteUrl = getSpriteUrl(building.kind, building.view, prosperity);
      if (spriteUrl) {
        this.load.image(`building-${building.kind}`, spriteUrl);
      }
    }
  }

  selectBuilding(kind: BuildingKind): void {
    if (!isBuildingAvailable(kind, this.availableActions)) {
      return;
    }
    this.onBuildingSelected(kind);
  }

  create(): void {
    const layout = this.layout;
    const sx = TownHubScene.CanvasWidth / 100;
    const sy = TownHubScene.CanvasHeight / 100;

    // Render tile grid first (behind buildings)
    this.renderTileGrid(layout, sx, sy);

    for (const building of layout.buildings) {
      const px = building.x * sx;
      const py = building.y * sy;
      const pw = building.width * sx;
      const ph = building.height * sy;

      // Try to use sprite if available, otherwise fall back to colored rectangle
      const spriteKey = `building-${building.kind}`;
      if (this.textures.exists(spriteKey)) {
        const sprite = this.add.image(px, py, spriteKey);
        sprite.setDisplaySize(pw, ph);

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
        // Fallback to colored rectangle for buildings without sprites (e.g., Trailhead)
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

    // Render paths using Phaser graphics
    if (layout.paths && layout.paths.length > 0) {
      const graphics = this.add.graphics();
      graphics.lineStyle(2, 0xc0c0c0); // Silver/gray path color

      for (const path of layout.paths) {
        const startX = path.startX * sx;
        const startY = path.startY * sy;
        const endX = path.endX * sx;
        const endY = path.endY * sy;

        graphics.moveTo(startX, startY);
        graphics.lineTo(endX, endY);
      }

      graphics.strokePath();
    }

    this.add.circle(layout.playerSpawnX * sx, layout.playerSpawnY * sy, 12, 0xffd700);
  }

  private renderTileGrid(layout: TownLayoutDto, sx: number, sy: number): void {
    if (!layout.tileGrid || layout.tileGrid.length === 0) {
      return;
    }

    const graphics = this.add.graphics();
    const tileSize = 10; // Each tile is 10 logical units

    for (let row = 0; row < layout.tileGrid.length; row++) {
      for (let col = 0; col < layout.tileGrid[row].length; col++) {
        const tileType = layout.tileGrid[row][col];
        const x = col * tileSize * sx;
        const y = row * tileSize * sy;
        const width = tileSize * sx;
        const height = tileSize * sy;

        // Tile type colors: 0=Empty, 1=Road, 2=BuildingZone, 3=SpurStart, 4=SpurRoad
        switch (tileType) {
          case 0: // Empty - don't render
            break;
          case 1: // Road - light brown
            graphics.fillStyle(0x8b7355, 0.6);
            graphics.fillRect(x, y, width, height);
            break;
          case 2: // BuildingZone - light gray placeholder
            graphics.fillStyle(0xd3d3d3, 0.3);
            graphics.fillRect(x, y, width, height);
            break;
          case 3: // SpurStart - darker road
            graphics.fillStyle(0x6b5344, 0.8);
            graphics.fillRect(x, y, width, height);
            break;
          case 4: // SpurRoad - medium road
            graphics.fillStyle(0x7b6349, 0.7);
            graphics.fillRect(x, y, width, height);
            break;
        }
      }
    }
  }
}
