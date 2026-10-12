import { useEffect, useRef } from "react";
import styled from "styled-components";
import Phaser from "phaser";
import type { WorldMapDto } from "../../api/types";

interface PhaserMapHostProps {
  mapData: WorldMapDto;
  selectedTownId: string | null;
  onTownSelected: (townId: string) => void;
  currentTownId?: string | null;
  selectableTownIds?: string[] | null;
}

export class WorldMapScene extends Phaser.Scene {
  private readonly placedLabelBounds: Array<{
    left: number;
    right: number;
    top: number;
    bottom: number;
  }> = [];
  private mapData: WorldMapDto;
  public selectedTownId: string | null;
  private readonly onTownSelected: (townId: string) => void;
  private currentTownId: string | null;
  private selectableTownIds: Set<string> | null;
  private hasCreated = false;
  private readonly redrawOnResize = () => this.drawMap();

  constructor(
    mapData: WorldMapDto,
    selectedTownId: string | null,
    onTownSelected: (townId: string) => void,
    currentTownId: string | null = null,
    selectableTownIds: string[] | null = null,
  ) {
    super("world-map");
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

  setRenderState(
    mapData: WorldMapDto,
    selectedTownId: string | null,
    currentTownId: string | null,
    selectableTownIds: string[] | null,
  ): void {
    this.mapData = mapData;
    this.selectedTownId = selectedTownId;
    this.currentTownId = currentTownId;
    this.selectableTownIds = selectableTownIds ? new Set(selectableTownIds) : null;

    if (this.hasCreated) {
      this.drawMap();
    }
  }

  create(): void {
    this.hasCreated = true;
    this.scale.on(Phaser.Scale.Events.RESIZE, this.redrawOnResize);
    this.drawMap();
  }

  private drawMap(): void {
    this.children.removeAll(true);
    this.placedLabelBounds.length = 0;
    const width = this.scale.width;
    const height = this.scale.height;
    const padding = Math.min(70, width * 0.15, height * 0.15);

    const xs = this.mapData.towns.map((t) => t.x);
    const ys = this.mapData.towns.map((t) => t.y);
    const minX = Math.min(...xs);
    const maxX = Math.max(...xs);
    const minY = Math.min(...ys);
    const maxY = Math.max(...ys);

    const dataWidth = maxX - minX || 1;
    const dataHeight = maxY - minY || 1;
    const scale = Math.min((width - padding * 2) / dataWidth, (height - padding * 2) / dataHeight);

    const scaledWidth = dataWidth * scale;
    const scaledHeight = dataHeight * scale;
    const offsetX = (width - scaledWidth) / 2;
    const offsetY = (height - scaledHeight) / 2;

    const toScreenX = (x: number) => offsetX + (x - minX) * scale;
    const toScreenY = (y: number) => offsetY + (y - minY) * scale;

    const townById = new Map(this.mapData.towns.map((t) => [t.id, t]));
    const townPositions = this.mapData.towns.map((town) => ({
      x: toScreenX(town.x),
      y: toScreenY(town.y),
    }));

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
      const distanceLabel = this.add
        .text(midX, midY, `${trail.rideDayDistance} days`, {
          fontSize: "11px",
          color: "#1a1a1a",
          backgroundColor: "#a8c890",
          padding: { x: 3, y: 1 },
        })
        .setOrigin(0.5);
      this.placeLabel(distanceLabel, midX, midY, 0, townPositions);
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

      const townLabel = this.add
        .text(x, y + radius + 16, town.name, {
          fontSize: "13px",
          color: "#1a1a1a",
          backgroundColor: "rgba(168, 200, 144, 0.85)",
          padding: { x: 2, y: 1 },
        })
        .setOrigin(0.5);
      this.placeLabel(townLabel, x, y, radius, townPositions);
    }
  }

  private placeLabel(
    label: Phaser.GameObjects.Text,
    x: number,
    y: number,
    markerRadius: number,
    townPositions: Array<{ x: number; y: number }>,
  ): void {
    const width = label.width;
    const height = label.height;
    const horizontalGap = markerRadius + width / 2 + 5;
    const verticalGap = markerRadius + height / 2 + 5;
    const candidates: Array<[number, number]> = [
      [x, y + verticalGap],
      [x, y - verticalGap],
      [x + horizontalGap, y],
      [x - horizontalGap, y],
      [x + horizontalGap, y + verticalGap],
      [x - horizontalGap, y + verticalGap],
      [x + horizontalGap, y - verticalGap],
      [x - horizontalGap, y - verticalGap],
      [x, y + verticalGap * 2],
      [x, y - verticalGap * 2],
    ];

    let selected = candidates[0];
    let lowestScore = Number.POSITIVE_INFINITY;
    const canvasInset = 6;

    for (let index = 0; index < candidates.length; index += 1) {
      const [rawX, rawY] = candidates[index];
      const candidateX = Math.min(
        Math.max(width / 2 + canvasInset, rawX),
        this.scale.width - width / 2 - canvasInset,
      );
      const candidateY = Math.min(
        Math.max(height / 2 + canvasInset, rawY),
        this.scale.height - height / 2 - canvasInset,
      );
      const bounds = {
        left: candidateX - width / 2,
        right: candidateX + width / 2,
        top: candidateY - height / 2,
        bottom: candidateY + height / 2,
      };
      let score = index / 100;

      for (const placed of this.placedLabelBounds) {
        const horizontalCollision =
          bounds.right + 6 > placed.left && bounds.left < placed.right + 6;
        const verticalCollision = bounds.bottom + 6 > placed.top && bounds.top < placed.bottom + 6;
        if (horizontalCollision && verticalCollision) score += 1_000_000;
      }

      for (const town of townPositions) {
        if (
          bounds.right >= town.x - 14 &&
          bounds.left <= town.x + 14 &&
          bounds.bottom >= town.y - 14 &&
          bounds.top <= town.y + 14
        ) {
          score += 10000;
        }
      }

      if (score < lowestScore) {
        lowestScore = score;
        selected = [candidateX, candidateY];
      }
    }

    label.setPosition(selected[0], selected[1]);
    this.placedLabelBounds.push({
      left: selected[0] - width / 2,
      right: selected[0] + width / 2,
      top: selected[1] - height / 2,
      bottom: selected[1] + height / 2,
    });
  }
}

export function PhaserMapHost({
  mapData,
  selectedTownId,
  onTownSelected,
  currentTownId,
  selectableTownIds,
}: PhaserMapHostProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const sceneRef = useRef<WorldMapScene | null>(null);
  const renderStateRef = useRef({ mapData, selectedTownId, currentTownId, selectableTownIds });
  renderStateRef.current = { mapData, selectedTownId, currentTownId, selectableTownIds };
  const onTownSelectedRef = useRef(onTownSelected);
  onTownSelectedRef.current = onTownSelected;

  useEffect(() => {
    if (!containerRef.current) return;

    const initialState = renderStateRef.current;
    const scene = new WorldMapScene(
      initialState.mapData,
      initialState.selectedTownId,
      (townId: string) => onTownSelectedRef.current(townId),
      initialState.currentTownId ?? null,
      initialState.selectableTownIds ?? null,
    );
    sceneRef.current = scene;

    const game = new Phaser.Game({
      parent: containerRef.current,
      width: 800,
      height: 500,
      backgroundColor: "#a8c890",
      scene: scene,
      scale: {
        mode: Phaser.Scale.RESIZE,
        autoCenter: Phaser.Scale.CENTER_BOTH,
      },
    });

    return () => {
      sceneRef.current = null;
      game.destroy(true);
    };
  }, []);

  useEffect(() => {
    sceneRef.current?.setRenderState(
      mapData,
      selectedTownId,
      currentTownId ?? null,
      selectableTownIds ?? null,
    );
  }, [mapData, selectedTownId, currentTownId, selectableTownIds]);

  return <MapCanvas ref={containerRef} role="img" aria-label="Trail map of starting towns" />;
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

  @media (max-width: 520px) {
    aspect-ratio: 1;
  }
`;
