import type { TravelDiaryDayDto } from "../../api/types";
import { JourneyStatus, TrailBeatSlotType } from "../../api/types";
import { formatHorseTravelState, formatJourneyStatus, formatTravelMode } from "../../ui/formatters";
import { formatBeatSlotLabel } from "../../ui/beatFormatters";
import styled from "styled-components";

interface TravelDiaryDayCardProps {
  day: TravelDiaryDayDto;
}

export function TravelDiaryDayCard({ day }: TravelDiaryDayCardProps) {
  const resolutionFacts = day.encounterResolution
    ? renderResolutionFacts(day.encounterResolution)
    : [];
  const badgeState =
    day.status === JourneyStatus.Completed
      ? "arrival"
      : day.status === JourneyStatus.Interrupted
        ? "interrupted"
        : day.encounterResolution
          ? "resolved"
          : day.trailEvent
            ? "eventful"
            : day.dayNumber === 1
              ? "departure"
              : "quiet";
  const badgeLabel =
    badgeState === "arrival"
      ? "Arrival"
      : badgeState === "interrupted"
        ? "Interrupted"
        : badgeState === "resolved"
          ? "Encounter resolved"
          : badgeState === "eventful"
            ? "Eventful"
            : badgeState === "departure"
              ? "Departure"
              : "Quiet trail";

  return (
    <DiaryDayCard>
      <DiaryDayHeader>
        <div>
          <DayTitle>Day {day.dayNumber}</DayTitle>
          <DaySubhead>
            {day.originTownName} to {day.destinationTownName} |{" "}
            {formatTravelMode(day.startingTravelMode)} to {formatTravelMode(day.endingTravelMode)} |{" "}
            {day.status === JourneyStatus.Active ? "In motion" : formatJourneyStatus(day.status)}
          </DaySubhead>
        </div>
        <DayBadge data-state={badgeState}>{badgeLabel}</DayBadge>
      </DiaryDayHeader>

      {day.beatSlots && day.beatSlots.length > 0 ? (
        <DiaryBody>
          <BeatSlotList>
            {day.beatSlots.map((slot) => (
              <BeatSlotItem
                key={slot.slotIndex}
                data-slot-type={beatSlotTypeCssName(slot.slotType)}
              >
                {formatBeatSlotLabel(slot.slotType)}
              </BeatSlotItem>
            ))}
          </BeatSlotList>
        </DiaryBody>
      ) : null}

      {day.trailEvent ? (
        <TrailNote>
          <strong>{day.trailEvent.title}</strong>
        </TrailNote>
      ) : null}

      {day.encounterResolution ? (
        <ResolutionNote>
          <strong>{day.encounterResolution.choiceLabel}</strong>
          {resolutionFacts.length > 0 ? (
            <ResolutionFacts>{resolutionFacts.join(" | ")}</ResolutionFacts>
          ) : null}
        </ResolutionNote>
      ) : null}

      <DayMeta>{renderDayMeta(day)}</DayMeta>
    </DiaryDayCard>
  );
}

function renderResolutionFacts(resolution: NonNullable<TravelDiaryDayDto["encounterResolution"]>) {
  const facts: string[] = [];
  if (resolution.healthDelta !== 0) facts.push(`Health ${formatSigned(resolution.healthDelta)}`);
  if (resolution.walletDelta !== 0) {
    facts.push(
      `Cash ${resolution.walletDelta > 0 ? "+" : "-"}$${Math.abs(resolution.walletDelta).toFixed(2)}`,
    );
  }
  if (resolution.ammoSpent !== 0) facts.push(`Ammo spent ${resolution.ammoSpent}`);
  if (resolution.heatIncrease !== 0) facts.push(`Heat ${formatSigned(resolution.heatIncrease)}`);
  if (resolution.horseExhaustionDelta !== 0) {
    facts.push(`Horse exhaustion ${formatSigned(resolution.horseExhaustionDelta)}`);
  }
  if (resolution.continuedOnFoot) facts.push("Continued on foot");
  return facts;
}

function formatSigned(value: number) {
  return value > 0 ? `+${value}` : String(value);
}

function renderDayMeta(day: TravelDiaryDayDto) {
  const hasHorseState = day.horseStateAfter !== null;
  const pieces = [
    `Health ${day.currentHealth}`,
    `Wallet ${day.currentWallet.toFixed(2)}`,
    `Food ${day.currentFood}`,
    `Canteen ${day.currentCanteenCharges}`,
    `Lawman heat ${day.currentHeat}`,
  ];

  if (hasHorseState) {
    pieces.splice(3, 0, `Horse ${formatHorseTravelState(day.horseStateAfter)}`);
  }

  return pieces.join(" | ");
}

function beatSlotTypeCssName(slotType: TrailBeatSlotType): string {
  switch (slotType) {
    case TrailBeatSlotType.Quiet:
      return "quiet";
    case TrailBeatSlotType.Minor:
      return "minor";
    case TrailBeatSlotType.Eventful:
      return "eventful";
    case TrailBeatSlotType.Interrupting:
      return "interrupting";
    default:
      return String(slotType);
  }
}

const DiaryDayCard = styled.article`
  display: grid;
  gap: 12px;
  padding: 16px;
  border-radius: 18px;
  background:
    linear-gradient(
      180deg,
      color-mix(in srgb, var(--text) 6%, transparent),
      rgba(255, 255, 255, 0.02)
    ),
    rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.08);
  color: var(--text);
`;

const DiaryDayHeader = styled.header`
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: start;
`;

const DayTitle = styled.h3`
  margin: 0 0 5px;
  font-family: "Iowan Old Style", Georgia, serif;
  font-size: 1.2rem;
`;

const DaySubhead = styled.p`
  margin: 0;
  color: color-mix(in srgb, var(--text) 68%, transparent);
  font-size: 0.92rem;
`;

const DayBadge = styled.span`
  padding: 5px 10px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  color: color-mix(in srgb, var(--text) 82%, transparent);
  background: rgba(255, 255, 255, 0.04);
  font-size: 0.78rem;
  white-space: nowrap;

  &[data-state="arrival"] {
    color: var(--accent-ink);
    background: linear-gradient(180deg, var(--accent-strong), var(--accent-strong-dark));
    border-color: color-mix(in srgb, var(--accent-strong) 58%, transparent);
  }

  &[data-state="interrupted"] {
    color: var(--danger-text);
    background: color-mix(in srgb, var(--danger) 14%, transparent);
    border-color: color-mix(in srgb, var(--danger) 24%, transparent);
  }

  &[data-state="resolved"] {
    color: var(--success-text);
    background: color-mix(in srgb, var(--success) 14%, transparent);
    border-color: color-mix(in srgb, var(--success) 24%, transparent);
  }

  &[data-state="eventful"],
  &[data-state="departure"] {
    color: var(--accent-ink);
    background: linear-gradient(180deg, var(--accent-strong), var(--accent-strong-dark));
    border-color: color-mix(in srgb, var(--accent-strong) 42%, transparent);
  }
`;

const DiaryBody = styled.div`
  display: grid;
  gap: 10px;
  font-family: "Iowan Old Style", Georgia, serif;
  font-size: 1.02rem;
  line-height: 1.65;
`;

const TrailNote = styled.div`
  display: grid;
  gap: 8px;
  padding: 13px 14px;
  border-radius: 16px;
  background: color-mix(in srgb, var(--accent) 9%, transparent);
  border: 1px solid color-mix(in srgb, var(--accent) 18%, transparent);

  p {
    margin: 0;
    color: color-mix(in srgb, var(--text) 80%, transparent);
  }
`;

const ResolutionNote = styled.div`
  display: grid;
  gap: 6px;
  padding: 13px 14px;
  border-radius: 16px;
  background: color-mix(in srgb, var(--success) 11%, transparent);
  border: 1px solid color-mix(in srgb, var(--success) 20%, transparent);

  p {
    margin: 0;
    color: color-mix(in srgb, var(--text) 82%, transparent);
  }
`;

const ResolutionFacts = styled.p`
  margin: 0;
  color: color-mix(in srgb, var(--text) 82%, transparent);
`;

const DayMeta = styled.p`
  margin: 0;
  color: color-mix(in srgb, var(--text) 54%, transparent);
  font-size: 0.84rem;
`;

const BeatSlotList = styled.ul`
  list-style: none;
  padding: 0;
  margin: 0 0 0.5rem 0;
`;

const BeatSlotItem = styled.li`
  font-size: 0.85rem;
  color: color-mix(in srgb, var(--text) 54%, transparent);
  padding: 0.15rem 0;
  border-left: 2px solid rgba(255, 255, 255, 0.1);
  padding-left: 0.5rem;
  margin-bottom: 0.15rem;

  &[data-slot-type="eventful"],
  &[data-slot-type="interrupting"] {
    color: var(--accent-ink);
    border-left-color: var(--accent-strong);
  }
`;
