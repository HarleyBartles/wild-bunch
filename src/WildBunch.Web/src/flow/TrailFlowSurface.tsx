import styled from "styled-components";
import { useGameSession } from "../state/useGameSession";
import { JourneyStatus } from "../api/types";
import { TravelPanel } from "../components/TravelPanel";
import { FlowSurface, Button } from "../components/ui/sharedStyled";

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

const ArrivalCard = styled.div`
  padding: 32px;
  border-radius: 28px;
  background: var(--bg-elevated);
  border: 1px solid var(--border-strong);
  text-align: center;
  display: grid;
  gap: 20px;
  justify-items: center;

  h1 {
    margin: 0;
  }
`;

const ArrivalLead = styled.p`
  margin: 0;
  color: var(--muted);
  line-height: 1.5;
`;

export function TrailFlowSurface() {
  const { session, gameId, loading, handleTravelTurnResult, handleAcknowledgeArrival } = useGameSession();

  if (!session || !gameId) {
    return null;
  }

  const journey = session.journey;

  // Completed journey — show arrival content and acknowledge button
  if (journey && journey.status === JourneyStatus.Completed) {
    const destinationName = journey.destinationTownName;
    const daysTravelled = journey.daysTravelled;

    return (
      <FlowSurface $variant="trail">
        <ArrivalCard>
          <h1>You've arrived in {destinationName}</h1>
          <ArrivalLead>
            You put {daysTravelled} day{daysTravelled === 1 ? "" : "s"} of trail behind you.
          </ArrivalLead>
          <Button
            type="button"
            $variant="primary"
            onClick={() => void handleAcknowledgeArrival()}
            disabled={loading}
          >
            {loading ? "Stepping into town..." : "Step into town"}
          </Button>
        </ArrivalCard>
      </FlowSurface>
    );
  }

  // Active or Interrupted — render the trail day normally
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
