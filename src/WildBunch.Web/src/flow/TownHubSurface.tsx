// src/flow/TownHubSurface.tsx
import styled from "styled-components";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { useGameSession } from "../state/useGameSession";
import { AvailableActionKind } from "../api/types";
import { FlowSurface } from "../components/ui/sharedStyled";

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

const TownHubGrid = styled.div`
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 16px;

  @media (max-width: 1366px) {
    grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  }

  @media (max-width: 960px) {
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  }
`;

const PlaceCard = styled.button`
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px;
  background: var(--bg-elevated);
  border: 1px solid var(--border);
  border-radius: 20px;
  text-align: left;
  cursor: pointer;
  color: var(--text);
  transition:
    transform 0.15s ease-out,
    border-color 0.15s ease-out;

  &:hover {
    border-color: var(--accent);
    transform: translateY(-2px);
  }

  &.trailhead {
    border-color: var(--accent-strong);
    background: linear-gradient(180deg, var(--bg-elevated), rgba(223, 159, 79, 0.05));
  }
`;

const PlaceCardIcon = styled.div`
  font-size: 2rem;
  flex-shrink: 0;
`;

const PlaceCardBody = styled.div`
  display: grid;
  gap: 4px;

  strong {
    font-size: 1.05rem;
  }

  p {
    margin: 0;
    font-size: 0.9rem;
    color: var(--muted);
  }
`;

export function TownHubSurface() {
  const { session, currentTown, actions } = useGameSession();
  const navigate = useNavigate();
  const { arrived } = useSearch({ strict: false }) as { arrived?: string };

  if (!session) {
    return null;
  }

  const hasStore = actions.some((a) => a.kind === AvailableActionKind.BuySupplies);
  const hasSheriff =
    actions.some((a) => a.kind === AvailableActionKind.ReadWantedPosters) ||
    actions.some((a) => a.kind === AvailableActionKind.CheckSheriffRecords);
  const hasSaloon = actions.some((a) => a.kind === AvailableActionKind.LookAroundSaloon);
  const hasTrailhead = actions.some((a) => a.kind === AvailableActionKind.Travel);

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
      <TownHubGrid>
        {hasStore ? (
          <PlaceCard type="button" onClick={() => void navigate({ to: "/town/store" })}>
            <PlaceCardIcon aria-hidden="true">📦</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Store</strong>
              <p>Buy supplies, food, and gear.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
        {hasSheriff ? (
          <PlaceCard type="button" onClick={() => void navigate({ to: "/town/sheriff" })}>
            <PlaceCardIcon aria-hidden="true">⭐</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Sheriff Office</strong>
              <p>Read wanted posters and check records.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
        {hasSaloon ? (
          <PlaceCard type="button" onClick={() => void navigate({ to: "/town/saloon" })}>
            <PlaceCardIcon aria-hidden="true">🥃</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Saloon</strong>
              <p>Look around, gather gossip, confront a suspect.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
        {hasTrailhead ? (
          <PlaceCard type="button" className="trailhead" onClick={() => void navigate({ to: "/town/trailhead" })}>
            <PlaceCardIcon aria-hidden="true">🐎</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Hit the trail</strong>
              <p>Ride to the next town.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
      </TownHubGrid>
    </FlowSurface>
  );
}
