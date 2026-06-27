import styled from "styled-components";
import type { GameSessionDto, GameTurnResultDto } from "../api/types";
import { JourneyStatus } from "../api/types";
import { formatJourneyStatus } from "../ui/formatters";
import { useTravelPanelState } from "../hooks/useTravelPanelState";
import { TravelActions } from "./travel/TravelActions";
import { TravelDiaryNotebook } from "./travel/TravelDiaryNotebook";
import { TravelSummary } from "./travel/TravelSummary";

interface TravelPanelProps {
  gameId: string;
  session: GameSessionDto;
  busy: boolean;
  onTurnResult: (result: GameTurnResultDto) => Promise<void> | void;
}

export function TravelPanel({ gameId, session, busy, onTurnResult }: TravelPanelProps) {
  const travelUi = useTravelPanelState({ gameId, session, busy, onTurnResult });

  return (
    <TravelStage>
      <TravelHeader>
        <div>
          <Eyebrow>Trail notebook</Eyebrow>
          <Title>Travel diary</Title>
          <Lead>
            A player-facing trail log that keeps the road in first person, with the next action sitting below the pages.
          </Lead>
        </div>
        <HeaderMeta>
          <MetaCard>
            <span>Journey</span>
            <strong>{formatJourneyStatus(travelUi.session.journey?.status ?? JourneyStatus.Active)}</strong>
          </MetaCard>
          <MetaCard>
            <span>Destination</span>
            <strong>{travelUi.session.journey?.destinationTownName ?? "Unknown"}</strong>
          </MetaCard>
        </HeaderMeta>
      </TravelHeader>

      {travelUi.actionError ? <ErrorBanner>{travelUi.actionError}</ErrorBanner> : null}
      {travelUi.refreshing ? <InfoBanner>Refreshing trail pages from the backend.</InfoBanner> : null}

      <TravelGrid>
        <TravelSummary session={travelUi.session} />
        <TravelDiaryNotebook travelDiary={travelUi.session.travelDiary} refreshing={travelUi.refreshing} />
        <TravelActions
          session={travelUi.session}
          busy={travelUi.busy}
          refreshing={travelUi.refreshing}
          actionError={travelUi.actionError}
          onAdvanceTravelDay={travelUi.advanceTravelDay}
          onAcknowledgeTravelArrival={travelUi.acknowledgeTravelArrival}
          onResolveEncounter={travelUi.resolveTravelEncounter}
        />
      </TravelGrid>
    </TravelStage>
  );
}

const TravelStage = styled.article`
  grid-column: 1 / -1;
  display: grid;
  gap: 18px;
  padding: 22px;
  border-radius: 28px;
  border: 1px solid color-mix(in srgb, var(--accent-strong) 24%, transparent);
  background:
    radial-gradient(circle at top right, color-mix(in srgb, var(--accent-strong) 12%, transparent), transparent 26%),
    linear-gradient(180deg, rgba(34, 25, 16, 0.98), rgba(18, 13, 8, 0.98)); /* no token match — surface gradient */
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.4);
`;

const TravelHeader = styled.header`
  display: flex;
  justify-content: space-between;
  gap: 18px;
  align-items: end;
`;

const Eyebrow = styled.p`
  margin: 0 0 6px;
  color: var(--accent-strong);
  text-transform: uppercase;
  letter-spacing: 0.22em;
  font-size: 0.74rem;
`;

const Title = styled.h2`
  margin: 0 0 8px;
  font-family: "Iowan Old Style", Georgia, serif;
  font-size: clamp(2rem, 4vw, 3.2rem);
  line-height: 0.98;
`;

const Lead = styled.p`
  max-width: 70ch;
  margin: 0;
  color: color-mix(in srgb, var(--text) 76%, transparent);
`;

const HeaderMeta = styled.div`
  display: grid;
  gap: 10px;
  width: min(100%, 300px);
`;

const MetaCard = styled.div`
  display: grid;
  gap: 4px;
  padding: 12px 14px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.09);

  span {
    color: color-mix(in srgb, var(--text) 62%, transparent);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    font-size: 0.75rem;
  }
`;

const TravelGrid = styled.div`
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
`;

const ErrorBanner = styled.div`
  padding: 13px 14px;
  border-radius: 16px;
  background: color-mix(in srgb, var(--danger) 15%, transparent);
  border: 1px solid color-mix(in srgb, var(--danger) 26%, transparent);
  color: var(--danger-text);
`;

const InfoBanner = styled.div`
  padding: 13px 14px;
  border-radius: 16px;
  background: color-mix(in srgb, var(--success) 12%, transparent);
  border: 1px solid color-mix(in srgb, var(--success) 20%, transparent);
  color: color-mix(in srgb, var(--text) 84%, transparent);
`;
