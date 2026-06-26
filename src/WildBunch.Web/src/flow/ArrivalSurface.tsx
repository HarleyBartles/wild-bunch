import styled from "styled-components";
import { useGameSession } from "../state/useGameSession";
import { FlowSurface, Button } from "../components/ui/sharedStyled";

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

export function ArrivalSurface() {
  const { session, loading, handleAcknowledgeArrival } = useGameSession();

  if (!session || !session.journey) {
    return null;
  }

  const journey = session.journey;
  const destinationName = journey.destinationTownName;
  const daysTravelled = journey.daysTravelled;

  return (
    <FlowSurface $variant="arrival">
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
