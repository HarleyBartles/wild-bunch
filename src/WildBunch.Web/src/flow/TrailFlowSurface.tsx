import styled from "styled-components";
import { useGameSession } from "../state/useGameSession";
import { TravelPanel } from "../components/TravelPanel";
import { FlowSurface } from "../components/ui/sharedStyled";

const TrailLockBanner = styled.div`
  padding: 12px 18px;
  background: rgba(223, 159, 79, 0.12);
  border: 1px solid rgba(223, 159, 79, 0.22);
  border-radius: 12px;
  color: var(--accent-strong);
  font-size: 0.95rem;
  font-weight: 600;
  text-align: center;
`;

export function TrailFlowSurface() {
  const { session, gameId, loading, handleTravelTurnResult } = useGameSession();

  if (!session || !gameId) {
    return null;
  }

  return (
    <FlowSurface $variant="trail">
      <TrailLockBanner role="status">
        You're on the trail. No turning back until you reach your destination.
      </TrailLockBanner>
      <TravelPanel
        gameId={gameId}
        session={session}
        busy={loading}
        onTurnResult={handleTravelTurnResult}
      />
    </FlowSurface>
  );
}
