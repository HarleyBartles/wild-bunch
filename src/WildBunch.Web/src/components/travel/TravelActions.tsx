import { JourneyStatus } from "../../api/types";
import type { GameSessionDto } from "../../api/types";
import { Card, ButtonBase } from "./travelShared";
import { JourneyDecision } from "./JourneyDecision";
import styled from "styled-components";

const revolverAmmoKind = 7;
const rifleAmmoKind = 9;

interface TravelActionsProps {
  session: GameSessionDto;
  busy: boolean;
  refreshing: boolean;
  actionError: string | null;
  onAdvanceTravelDay: () => Promise<void>;
  onAcknowledgeTravelArrival: () => Promise<void>;
  onResolveEncounter: (
    choiceId: string,
    options?: {
      bulletSpend?: number | null;
      bribeAmount?: number | null;
    },
  ) => Promise<void>;
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
  const firearmAmmo = session.inventory.items
    .filter((item) => item.kind === revolverAmmoKind || item.kind === rifleAmmoKind)
    .reduce((total, item) => total + item.quantity, 0);

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
        <JourneyDecision
          encounter={pendingEncounter}
          busy={busy}
          refreshing={refreshing}
          ammo={firearmAmmo}
          cash={session.inventory.wallet.cash}
          onResolveEncounter={onResolveEncounter}
        />
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
    color: color-mix(in srgb, var(--text) 62%, transparent);
    font-size: 0.9rem;
  }
`;

const ActionCopy = styled.p`
  margin: 0;
  color: color-mix(in srgb, var(--text) 74%, transparent);
`;

const InlineError = styled.div`
  padding: 12px 14px;
  border-radius: 16px;
  background: color-mix(in srgb, var(--danger) 14%, transparent);
  border: 1px solid color-mix(in srgb, var(--danger) 24%, transparent);
  color: var(--danger-text);
`;

const PrimaryButton = styled(ButtonBase)`
  background: linear-gradient(180deg, var(--accent-strong), var(--accent-strong-dark));
  color: var(--accent-ink);
  border-color: color-mix(in srgb, var(--accent-strong) 55%, transparent);

  &:disabled {
    cursor: not-allowed;
    opacity: 0.55;
  }
`;
