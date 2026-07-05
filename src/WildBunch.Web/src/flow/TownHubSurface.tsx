// src/flow/TownHubSurface.tsx
import { useCallback, useMemo } from "react";
import styled from "styled-components";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { useGameSession } from "../state/useGameSession";
import { AvailableActionKind, BuildingKind } from "../api/types";
import { FlowSurface } from "../components/ui/sharedStyled";
import { PhaserTownHubHost } from "../components/town-hub/PhaserTownHubHost";
import { isBuildingAvailable } from "../components/town-hub/TownHubScene";

const TownHubHeader = styled.header`
  display: grid;
  gap: 8px;
  padding: 24px 0 4px;

  h1 {
    margin: 0;
  }
`;

const TownHubLead = styled.p`
  margin: 0;
  font-size: 1.1rem;
  color: var(--muted);
`;

const ArrivalNotice = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 18px;
  background: rgba(223, 159, 79, 0.12);
  border: 1px solid rgba(223, 159, 79, 0.22);
  border-radius: 12px;
  color: var(--accent-strong);
  font-size: 0.95rem;
  font-weight: 600;
`;

const DismissButton = styled.button`
  border: none;
  background: none;
  color: var(--muted);
  cursor: pointer;
  font-size: 1.2rem;
  padding: 4px 8px;
  border-radius: 6px;

  &:hover {
    color: var(--text);
    background: rgba(255, 255, 255, 0.06);
  }
`;

// Visible keyboard-operable fallback for building navigation (ADR-0035).
// Sighted mouse users click buildings on the Phaser canvas; keyboard users
// tab through these visible buttons beneath the canvas. This preserves
// visible focus indication — the player can see which building they are
// about to activate.
const KeyboardFallbackNav = styled.nav`
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  padding: 12px 0 4px;

  button {
    padding: 8px 14px;
    border-radius: 8px;
    border: 1px solid var(--border);
    background: rgba(255, 255, 255, 0.04);
    color: var(--text);
    font-size: 0.9rem;
    cursor: pointer;
    transition: border-color 0.15s, background 0.15s;

    &:hover {
      background: rgba(255, 255, 255, 0.08);
      border-color: color-mix(in srgb, var(--accent-strong) 40%, transparent);
    }

    &:focus-visible {
      outline: 2px solid var(--accent-strong);
      outline-offset: 2px;
      background: rgba(223, 159, 79, 0.12);
      border-color: var(--accent-strong);
    }
  }
`;

const BUILDING_ROUTES: Partial<Record<BuildingKind, string>> = {
  [BuildingKind.Store]: "/town/store",
  [BuildingKind.Sheriff]: "/town/sheriff",
  [BuildingKind.Saloon]: "/town/saloon",
  [BuildingKind.Trailhead]: "/town/trailhead",
};

const BUILDING_LABELS: Partial<Record<BuildingKind, string>> = {
  [BuildingKind.Store]: "Store",
  [BuildingKind.Sheriff]: "Sheriff Office",
  [BuildingKind.Saloon]: "Saloon",
  [BuildingKind.Trailhead]: "Hit the trail",
};

const NAVIGABLE_BUILDINGS = [
  BuildingKind.Store,
  BuildingKind.Sheriff,
  BuildingKind.Saloon,
  BuildingKind.Trailhead,
];

export function TownHubSurface() {
  const { session, currentTown, actions } = useGameSession();
  const navigate = useNavigate();
  const { arrived } = useSearch({ strict: false }) as { arrived?: string };

  // Map AvailableActionDto[] → AvailableActionKind[] and memoize so the
  // Phaser game is not recreated on every parent render (the actions array
  // reference from react-query is stable across renders unless data changes).
  const availableActions = useMemo(
    () => actions.map((a) => a.kind),
    [actions],
  );

  const onBuildingSelected = useCallback(
    (kind: BuildingKind) => {
      const route = BUILDING_ROUTES[kind];
      if (route) {
        void navigate({ to: route });
      }
    },
    [navigate],
  );

  if (!session) {
    return null;
  }

  const townName = currentTown?.name ?? session.player.currentTownId;

  return (
    <FlowSurface $variant="town-hub">
      <TownHubHeader>
        <h1>{townName}</h1>
        <TownHubLead>Where to next?</TownHubLead>
      </TownHubHeader>
      {arrived === "1" ? (
        <ArrivalNotice role="status">
          <span>You've arrived in {townName}. Take a moment to look around.</span>
          <DismissButton
            type="button"
            aria-label="Dismiss arrival notice"
            onClick={() => void navigate({ to: "/town", search: {} })}
          >
            ×
          </DismissButton>
        </ArrivalNotice>
      ) : null}
      <PhaserTownHubHost
        layout={currentTown?.layout}
        availableActions={availableActions}
        onBuildingSelected={onBuildingSelected}
      />
      <KeyboardFallbackNav aria-label="Town buildings">
        {NAVIGABLE_BUILDINGS.filter((kind) =>
          isBuildingAvailable(kind, availableActions),
        ).map((kind) => (
          <button
            key={kind}
            type="button"
            onClick={() => onBuildingSelected(kind)}
          >
            {BUILDING_LABELS[kind]}
          </button>
        ))}
      </KeyboardFallbackNav>
    </FlowSurface>
  );
}
