import { useEffect, useRef } from "react";
import styled from "styled-components";
import Phaser from "phaser";
import type { AvailableActionKind, BuildingKind, TownLayoutDto } from "../../api/types";
import { TownHubScene } from "./TownHubScene";

interface PhaserTownHubHostProps {
  layout: TownLayoutDto | null | undefined;
  availableActions: AvailableActionKind[];
  onBuildingSelected: (kind: BuildingKind) => void;
}

export function PhaserTownHubHost({
  layout,
  availableActions,
  onBuildingSelected,
}: PhaserTownHubHostProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const onBuildingSelectedRef = useRef(onBuildingSelected);
  onBuildingSelectedRef.current = onBuildingSelected;

  useEffect(() => {
    if (!containerRef.current || !layout) {
      return;
    }

    const scene = new TownHubScene(
      layout,
      availableActions,
      (kind: BuildingKind) => onBuildingSelectedRef.current(kind),
    );

    const game = new Phaser.Game({
      parent: containerRef.current,
      width: 800,
      height: 500,
      backgroundColor: "#c4a87a",
      scene: scene,
      scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
      },
    });

    return () => {
      game.destroy(true);
    };
  }, [layout, availableActions]);

  return (
    <TownHubCanvas
      ref={containerRef}
      role="img"
      aria-label="Town hub surface"
    />
  );
}

const TownHubCanvas = styled.div`
  width: 100%;
  max-width: 800px;
  aspect-ratio: 8 / 5;
  border-radius: 16px;
  border: 1px solid var(--border);
  background: #c4a87a;
  overflow: hidden;
  display: flex;
  justify-content: center;
  align-items: center;
`;
