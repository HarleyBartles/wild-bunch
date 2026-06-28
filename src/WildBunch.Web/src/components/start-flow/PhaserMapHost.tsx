import { useEffect, useRef } from "react";
import styled from "styled-components";
import Phaser from "phaser";
import type { StartingTownMapDto } from "../../api/types";

interface PhaserMapHostProps {
  mapData: StartingTownMapDto;
  selectedTownId: string | null;
  onTownSelected: (townId: string) => void;
}

export class StartingTownMapScene extends Phaser.Scene {
  private readonly mapData: StartingTownMapDto;
  public readonly selectedTownId: string | null;
  private readonly onTownSelected: (townId: string) => void;

  constructor(
    mapData: StartingTownMapDto,
    selectedTownId: string | null,
    onTownSelected: (townId: string) => void,
  ) {
    super("starting-town-map");
    this.mapData = mapData;
    this.selectedTownId = selectedTownId;
    this.onTownSelected = onTownSelected;
  }

  selectTown(townId: string): void {
    const town = this.mapData.towns.find((t) => t.id === townId);
    if (town?.selectable) {
      this.onTownSelected(townId);
    }
  }

  create(): void {
    const width = this.scale.width;
    const height = this.scale.height;
    const padding = 70;

    const xs = this.mapData.towns.map((t) => t.x);
    const ys = this.mapData.towns.map((t) => t.y);
    const minX = Math.min(...xs);
    const maxX = Math.max(...xs);
    const minY = Math.min(...ys);
    const maxY = Math.max(...ys);

    const dataWidth = maxX - minX || 1;
    const dataHeight = maxY - minY || 1;
    const scale = Math.min(
      (width - padding * 2) / dataWidth,
      (height - padding * 2) / dataHeight,
    );

    const toScreenX = (x: number) => padding + (x - minX) * scale;
    const toScreenY = (y: number) => padding + (y - minY) * scale;

    const townById = new Map(this.mapData.towns.map((t) => [t.id, t]));

    const trailGraphics = this.add.graphics();
    for (const trail of this.mapData.trails) {
      const from = townById.get(trail.fromTownId);
      const to = townById.get(trail.toTownId);
      if (!from || !to) continue;

      trailGraphics.lineStyle(2, 0x8b7355, 0.8);
      trailGraphics.beginPath();
      trailGraphics.moveTo(toScreenX(from.x), toScreenY(from.y));
      trailGraphics.lineTo(toScreenX(to.x), toScreenY(to.y));
      trailGraphics.strokePath();

      const midX = (toScreenX(from.x) + toScreenX(to.x)) / 2;
      const midY = (toScreenY(from.y) + toScreenY(to.y)) / 2;
      this.add
        .text(midX, midY, `${trail.rideDayDistance} days`, {
          fontSize: "11px",
          color: "#a89070",
        })
        .setOrigin(0.5);
    }

    for (const town of this.mapData.towns) {
      const x = toScreenX(town.x);
      const y = toScreenY(town.y);
      const isSelected = this.selectedTownId === town.id;
      const radius = town.selectable ? 14 : 9;

      const circle = this.add.circle(
        x,
        y,
        radius,
        town.selectable ? 0xc9a84c : 0x5a4d3f,
      );

      if (isSelected) {
        circle.setStrokeStyle(4, 0xf0e6d2);
      } else if (town.selectable) {
        circle.setStrokeStyle(2, 0x8b7355);
      }

      if (town.selectable) {
        circle.setInteractive({ useHandCursor: true });
        circle.on("pointerover", () => circle.setScale(1.25));
        circle.on("pointerout", () => circle.setScale(1));
        circle.on("pointerdown", () => this.selectTown(town.id));
      }

      this.add
        .text(x, y + radius + 16, town.name, {
          fontSize: "13px",
          color: town.selectable ? "#e8d8b8" : "#7a6a5a",
        })
        .setOrigin(0.5);
    }
  }
}

export function PhaserMapHost({ mapData, selectedTownId, onTownSelected }: PhaserMapHostProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const onTownSelectedRef = useRef(onTownSelected);
  onTownSelectedRef.current = onTownSelected;

  useEffect(() => {
    if (!containerRef.current) return;

    const scene = new StartingTownMapScene(mapData, selectedTownId, (townId: string) =>
      onTownSelectedRef.current(townId),
    );

    const game = new Phaser.Game({
      parent: containerRef.current,
      width: 800,
      height: 500,
      backgroundColor: "#1a1410",
      scene: scene,
      scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
      },
    });

    return () => {
      game.destroy(true);
    };
  }, [mapData, selectedTownId]);

  return (
    <MapCanvas
      ref={containerRef}
      role="img"
      aria-label="Trail map of starting towns"
    />
  );
}

const MapCanvas = styled.div`
  width: 100%;
  max-width: 800px;
  aspect-ratio: 8 / 5;
  border-radius: 16px;
  border: 1px solid var(--border);
  background: rgba(26, 20, 16, 0.6);
  overflow: hidden;
`;
