import { JourneyStatus } from "../../api/types";
import type { GameSessionDto } from "../../api/types";
import { Card, ButtonBase } from "./travelShared";
import { JourneyDecision } from "./JourneyDecision";
import styled from "styled-components";

interface TravelActionsProps {
  session: GameSessionDto;
  busy: boolean;
  refreshing: boolean;
  actionError: string | null;
  onAdvanceTravelDay: () => Promise<void>;
  onAcknowledgeTravelArrival: () => Promise<void>;
  onResolveEncounter: (choiceId: string) => Promise<void>;
}

export function TravelActions({
  session,
  busy,
  refreshing,
  actionError,
  onAdvanceTravelDay,
  onAcknowledgeTravelArrival,
  onResolveEncounter,
}: TravelActionsProps) {
  const journey = session.journey;

  if (!journey) {
    return null;
  }

  const disabled = busy || refreshing;
  const pendingEncounter = journey.pendingEncounter;
  const arrivalPending = journey.status === JourneyStatus.Completed;
  const canAdvance = journey.status === JourneyStatus.Active && !pendingEncounter;

  return (
    <ActionCard>
      <SectionHeader>
        <strong>Trail action</strong>
        <span>{pendingEncounter ? "Encounter waiting" : arrivalPending ? "Arrival pending" : "Ready to ride"}</span>
      </SectionHeader>

      {actionError ? <InlineError>{actionError}</InlineError> : null}

      {pendingEncounter ? (
        <JourneyDecision encounter={pendingEncounter} busy={busy} refreshing={refreshing} onResolveEncounter={onResolveEncounter} />
      ) : arrivalPending ? (
        <>
          <ActionCopy>The trail pages are still open. Acknowledge the arrival when you are ready to step into town.</ActionCopy>
          <PrimaryButton type="button" onClick={() => void onAcknowledgeTravelArrival()} disabled={disabled}>
            {busy ? "Entering town..." : "Enter town"}
          </PrimaryButton>
        </>
      ) : canAdvance ? (
        <>
          <ActionCopy>
            The notebook is ready for the next stretch of road. Advance the day when you want the trail to continue.
          </ActionCopy>
          <PrimaryButton type="button" onClick={() => void onAdvanceTravelDay()} disabled={disabled}>
            {busy ? "Advancing..." : "Advance travel day"}
          </PrimaryButton>
        </>
      ) : (
        <ActionCopy>The journey is paused. Refresh to sync the latest trail state.</ActionCopy>
      )}
    </ActionCard>
  );
}

const ActionCard = styled(Card)`
  grid-column: 1 / -1;
`;

const SectionHeader = styled.div`
  display: flex;
  justify-content: space-between;
  gap: 14px;
  align-items: baseline;

  strong {
    font-size: 1rem;
  }

  span {
    color: rgba(242, 239, 232, 0.62);
    font-size: 0.9rem;
  }
`;

const ActionCopy = styled.p`
  margin: 0;
  color: rgba(242, 239, 232, 0.74);
`;

const InlineError = styled.div`
  padding: 12px 14px;
  border-radius: 16px;
  background: rgba(240, 126, 110, 0.14);
  border: 1px solid rgba(240, 126, 110, 0.24);
  color: #ffe8e3;
`;

const PrimaryButton = styled(ButtonBase)`
  background: linear-gradient(180deg, #efc37e, #bf7a35);
  color: #1b1308;
  border-color: rgba(239, 195, 126, 0.55);

  &:disabled {
    cursor: not-allowed;
    opacity: 0.55;
  }
`;
