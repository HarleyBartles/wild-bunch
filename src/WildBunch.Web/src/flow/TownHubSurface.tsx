import styled from "styled-components";
import { useGameSession } from "../state/useGameSession";
import { AvailableActionKind } from "../api/types";
import type { TownPlace } from "./GameFlowRouter";
import { StorePlace } from "./places/StorePlace";
import { SheriffPlace } from "./places/SheriffPlace";
import { SaloonPlace } from "./places/SaloonPlace";
import { TravelPrepSurface } from "./TravelPrepSurface";
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

interface TownHubSurfaceProps {
  activePlace: TownPlace;
  onPlaceChange: (place: TownPlace) => void;
}

export function TownHubSurface({ activePlace, onPlaceChange }: TownHubSurfaceProps) {
  const { session, currentTown, actions } = useGameSession();

  if (!session) {
    return null;
  }

  // If a place is active, render the place surface with a back button
  if (activePlace === "store") {
    return <StorePlace onLeave={() => onPlaceChange(null)} />;
  }
  if (activePlace === "sheriff") {
    return <SheriffPlace onLeave={() => onPlaceChange(null)} />;
  }
  if (activePlace === "saloon") {
    return <SaloonPlace onLeave={() => onPlaceChange(null)} />;
  }
  if (activePlace === "trailhead") {
    return <TravelPrepSurface onBack={() => onPlaceChange(null)} />;
  }

  // Otherwise render the town hub with place cards
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
      <TownHubGrid>
        {hasStore ? (
          <PlaceCard type="button" onClick={() => onPlaceChange("store")}>
            <PlaceCardIcon aria-hidden="true">📦</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Store</strong>
              <p>Buy supplies, food, and gear.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
        {hasSheriff ? (
          <PlaceCard type="button" onClick={() => onPlaceChange("sheriff")}>
            <PlaceCardIcon aria-hidden="true">⭐</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Sheriff Office</strong>
              <p>Read wanted posters and check records.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
        {hasSaloon ? (
          <PlaceCard type="button" onClick={() => onPlaceChange("saloon")}>
            <PlaceCardIcon aria-hidden="true">🥃</PlaceCardIcon>
            <PlaceCardBody>
              <strong>Saloon</strong>
              <p>Look around, gather gossip, confront a suspect.</p>
            </PlaceCardBody>
          </PlaceCard>
        ) : null}
        {hasTrailhead ? (
          <PlaceCard type="button" className="trailhead" onClick={() => onPlaceChange("trailhead")}>
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
