import type { GameSessionDto } from "../../api/types";
import { formatHorseTravelState, formatJourneyStatus, formatRisk, formatTrailTerrain, formatTravelMode, formatWaterFeature } from "../../ui/formatters";
import { Card, SectionHeader } from "./travelShared";
import styled from "styled-components";

interface TravelSummaryProps {
  session: GameSessionDto;
}

export function TravelSummary({ session }: TravelSummaryProps) {
  const journey = session.journey;

  if (!journey) {
    return null;
  }

  const hasWarnings = journey.warnings.length > 0;

  return (
    <SummaryCard>
      <SectionHeader>
        <strong>Trail ledger</strong>
        <StatusPill>{formatJourneyStatus(journey.status)}</StatusPill>
      </SectionHeader>

      <SummaryGrid>
        <SummaryItem>
          <dt>Route</dt>
          <dd>
            {journey.originTownName} to {journey.destinationTownName}
          </dd>
        </SummaryItem>
        <SummaryItem>
          <dt>Travel mode</dt>
          <dd>{formatTravelMode(journey.travelMode)}</dd>
        </SummaryItem>
        <SummaryItem>
          <dt>Remaining days</dt>
          <dd>{journey.remainingDays}</dd>
        </SummaryItem>
        <SummaryItem>
          <dt>Remaining distance</dt>
          <dd>{journey.remainingRideDayDistance.toFixed(2)}</dd>
        </SummaryItem>
        {journey.horseState ? (
          <SummaryItem>
            <dt>Horse</dt>
            <dd>{formatHorseTravelState(journey.horseState)}</dd>
          </SummaryItem>
        ) : null}
        <SummaryItem>
          <dt>Water pressure</dt>
          <dd>{journey.waterSecure ? "Secure" : "Drying out"}</dd>
        </SummaryItem>
        <SummaryItem>
          <dt>Terrain</dt>
          <dd>{formatTrailTerrain(journey.routeProfile.terrain)}</dd>
        </SummaryItem>
        <SummaryItem>
          <dt>Water feature</dt>
          <dd>{formatWaterFeature(journey.routeProfile.waterFeature)}</dd>
        </SummaryItem>
        <SummaryItem>
          <dt>Risk</dt>
          <dd>{formatRisk(journey.routeProfile.risk)}</dd>
        </SummaryItem>
      </SummaryGrid>

      {hasWarnings ? (
        <WarningsBlock>
          <strong>Urgent warnings</strong>
          <WarningList>
            {journey.warnings.map((warning) => (
              <li key={warning}>{warning}</li>
            ))}
          </WarningList>
        </WarningsBlock>
      ) : (
        <MutedNote>The route looks steady for now.</MutedNote>
      )}
    </SummaryCard>
  );
}

const SummaryCard = styled(Card)`
  grid-column: 1 / -1;
`;

const StatusPill = styled.span`
  display: inline-flex;
  align-items: center;
  padding: 5px 10px;
  border-radius: 999px;
  color: var(--accent-ink) !important;
  background: linear-gradient(180deg, var(--accent-strong), var(--accent-strong-dark));
  font-weight: 700;
  font-size: 0.78rem !important;
`;

const SummaryGrid = styled.dl`
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin: 0;
`;

const SummaryItem = styled.div`
  dt {
    color: color-mix(in srgb, var(--text) 58%, transparent);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    font-size: 0.74rem;
  }

  dd {
    margin: 3px 0 0;
    font-weight: 600;
  }
`;

const WarningsBlock = styled.div`
  display: grid;
  gap: 8px;
  padding: 14px 15px;
  border-radius: 16px;
  border: 1px solid color-mix(in srgb, var(--danger) 22%, transparent);
  background: color-mix(in srgb, var(--danger) 11%, transparent);

  strong {
    color: var(--danger-text);
  }
`;

const WarningList = styled.ul`
  margin: 0;
  padding-left: 18px;
  color: color-mix(in srgb, var(--text) 84%, transparent);
`;

const MutedNote = styled.p`
  margin: 0;
  color: color-mix(in srgb, var(--text) 66%, transparent);
`;
