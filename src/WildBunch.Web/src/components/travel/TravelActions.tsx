import { JourneyStatus } from "../../api/types";
import type { GameSessionDto } from "../../api/types";
import { Card, ButtonBase } from "./travelShared";
import { JourneyDecision } from "./JourneyDecision";
import styled from "styled-components";

const revolverAmmoKind = 7;

interface TravelActionsProps {
  session: GameSessionDto;
  busy: boolean;
  refreshing: boolean;
  onAdvanceTravelDay: () => void;
  onResolveEncounter: (
    choiceId: string,
    options?: {
      bulletSpend?: number | null;
      bribeAmount?: number | null;
    },
  ) => void;
}

export function TravelActions({
  session,
  busy,
  refreshing,
  onAdvanceTravelDay,
  onResolveEncounter,
}: TravelActionsProps) {
  const journey = session.journey;

  if (!journey || session.inventory === null) {
    return null;
  }

  const firearmAmmo = session.inventory.items
    .filter((item) => item.kind === revolverAmmoKind)
    .reduce((total, item) => total + item.quantity, 0);

  const disabled = busy || refreshing;
  const pendingEncounter = journey.pendingEncounter;
  const canAdvance = journey.status === JourneyStatus.Active && !pendingEncounter;

  return (
    <ActionCard>
      <SectionHeader>
        <strong>Trail action</strong>
        <span>
          {pendingEncounter
            ? "Encounter waiting"
            : journey.status === JourneyStatus.Active
              ? "Ready to ride"
              : "Journey paused"}
        </span>
      </SectionHeader>

      {pendingEncounter ? (
        <JourneyDecision
          encounter={pendingEncounter}
          busy={busy}
          refreshing={refreshing}
          ammo={firearmAmmo}
          cash={session.inventory.wallet.cash}
          onResolveEncounter={onResolveEncounter}
        />
      ) : canAdvance ? (
        <>
          <ActionCopy>
            The notebook is ready for the next stretch of road. Advance the day when you want the
            trail to continue.
          </ActionCopy>
          <PrimaryButton type="button" onClick={onAdvanceTravelDay} disabled={disabled}>
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

const PrimaryButton = styled(ButtonBase)`
  background: linear-gradient(180deg, var(--accent-strong), var(--accent-strong-dark));
  color: var(--accent-ink);
  border-color: color-mix(in srgb, var(--accent-strong) 55%, transparent);

  &:disabled {
    cursor: not-allowed;
    opacity: 0.55;
  }
`;
