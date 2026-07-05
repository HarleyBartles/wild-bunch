import Phaser from "phaser";
import { AvailableActionKind } from "../../api/types";
import { BuildingKind } from "./types";
import type { TownLayoutDto } from "./types";

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

    for (const building of layout.buildings) {
      const color = BUILDING_COLORS[building.kind] ?? 0x6a6a6a;
      const px = building.x * sx;
      const py = building.y * sy;
      const pw = building.width * sx;
      const ph = building.height * sy;
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

      const label = BUILDING_LABELS[building.kind] ?? "Building";
      this.add
        .text(px, py + ph / 2 + 12, label, {
          fontSize: "12px",
          color: "#fff",
        })
        .setOrigin(0.5);
    }

    this.add.circle(layout.playerSpawnX * sx, layout.playerSpawnY * sy, 12, 0xffd700);
  }
}
