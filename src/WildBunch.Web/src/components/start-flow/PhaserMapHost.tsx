import { useEffect, useRef } from "react";
import styled from "styled-components";
import Phaser from "phaser";
import type { StartingTownMapDto } from "../../api/types";

interface PhaserMapHostProps {
  mapData: StartingTownMapDto;
  selectedTownId: string | null;
  onTownSelected: (townId: string) => void;
  currentTownId?: string | null;
  selectableTownIds?: string[] | null;
}

export class StartingTownMapScene extends Phaser.Scene {
  private readonly mapData: StartingTownMapDto;
  public readonly selectedTownId: string | null;
  private readonly onTownSelected: (townId: string) => void;
  private readonly currentTownId: string | null;
  private readonly selectableTownIds: Set<string> | null;

  constructor(
    mapData: StartingTownMapDto,
    selectedTownId: string | null,
    onTownSelected: (townId: string) => void,
    currentTownId: string | null = null,
    selectableTownIds: string[] | null = null,
  ) {
    super("starting-town-map");
    this.mapData = mapData;
    this.selectedTownId = selectedTownId;
    this.onTownSelected = onTownSelected;
    this.currentTownId = currentTownId;
    this.selectableTownIds = selectableTownIds ? new Set(selectableTownIds) : null;
  }

  selectTown(townId: string): void {
    if (this.selectableTownIds && !this.selectableTownIds.has(townId)) {
      return;
    }
    const town = this.mapData.towns.find((t) => t.id === townId);
    if (town) {
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

    const scaledWidth = dataWidth * scale;
    const scaledHeight = dataHeight * scale;
    const offsetX = (width - scaledWidth) / 2;
    const offsetY = (height - scaledHeight) / 2;

    const toScreenX = (x: number) => offsetX + (x - minX) * scale;
    const toScreenY = (y: number) => offsetY + (y - minY) * scale;

    const townById = new Map(this.mapData.towns.map((t) => [t.id, t]));

    // Black trail lines on green map background
    const trailGraphics = this.add.graphics();
    for (const trail of this.mapData.trails) {
      const from = townById.get(trail.fromTownId);
      const to = townById.get(trail.toTownId);
      if (!from || !to) continue;

      trailGraphics.lineStyle(2, 0x000000, 0.85);
      trailGraphics.beginPath();
      trailGraphics.moveTo(toScreenX(from.x), toScreenY(from.y));
      trailGraphics.lineTo(toScreenX(to.x), toScreenY(to.y));
      trailGraphics.strokePath();

      const midX = (toScreenX(from.x) + toScreenX(to.x)) / 2;
      const midY = (toScreenY(from.y) + toScreenY(to.y)) / 2;
      this.add
        .text(midX, midY, `${trail.rideDayDistance} days`, {
          fontSize: "11px",
          color: "#1a1a1a",
          backgroundColor: "#a8c890",
          padding: { x: 3, y: 1 },
        })
        .setOrigin(0.5);
    }

    // All listed towns are visible; interactivity is gated by selectableTownIds
    for (const town of this.mapData.towns) {
      const x = toScreenX(town.x);
      const y = toScreenY(town.y);
      const isSelected = this.selectedTownId === town.id;
      const isCurrent = this.currentTownId === town.id;
      const isSelectable = !this.selectableTownIds || this.selectableTownIds.has(town.id);
      const radius = 14;

      let fillColor = 0xc9a84c;
      if (isCurrent) {
        fillColor = 0x8b6914;
      } else if (!isSelectable) {
        fillColor = 0x9a9a8a;
      }

      const circle = this.add.circle(x, y, radius, fillColor);

      if (isSelected) {
        circle.setStrokeStyle(4, 0xf0e6d2);
      } else if (isCurrent) {
        circle.setStrokeStyle(3, 0xf0e6d2);
      } else {
        circle.setStrokeStyle(2, 0x000000);
      }

      if (isSelectable && !isCurrent) {
        circle.setInteractive({ useHandCursor: true });
        circle.on("pointerover", () => circle.setScale(1.25));
        circle.on("pointerout", () => circle.setScale(1));
        circle.on("pointerdown", () => this.selectTown(town.id));
      }

      this.add
        .text(x, y + radius + 16, town.name, {
          fontSize: "13px",
          color: "#1a1a1a",
          backgroundColor: "rgba(168, 200, 144, 0.85)",
          padding: { x: 2, y: 1 },
        })
        .setOrigin(0.5);
    }
  }
}

export function PhaserMapHost({ mapData, selectedTownId, onTownSelected, currentTownId, selectableTownIds }: PhaserMapHostProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const onTownSelectedRef = useRef(onTownSelected);
  onTownSelectedRef.current = onTownSelected;

  useEffect(() => {
    if (!containerRef.current) return;

    const scene = new StartingTownMapScene(
      mapData,
      selectedTownId,
      (townId: string) => onTownSelectedRef.current(townId),
      currentTownId ?? null,
      selectableTownIds ?? null,
    );

    const game = new Phaser.Game({
      parent: containerRef.current,
      width: 800,
      height: 500,
      backgroundColor: "#a8c890",
      scene: scene,
      scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
      },
    });

    return () => {
      game.destroy(true);
    };
  }, [mapData, selectedTownId, currentTownId, selectableTownIds]);

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
  background: #a8c890;
  overflow: hidden;
  display: flex;
  justify-content: center;
  align-items: center;
`;
