import type { TravelDiaryDayDto } from "../../api/types";
import { JourneyStatus } from "../../api/types";
import { formatHorseTravelState, formatJourneyStatus, formatTravelMode } from "../../ui/formatters";
import { formatSignedNumber } from "./travelShared";
import styled from "styled-components";

interface TravelDiaryDayCardProps {
  day: TravelDiaryDayDto;
}

export function TravelDiaryDayCard({ day }: TravelDiaryDayCardProps) {
  const hasHorseState = day.horseStateBefore !== null || day.horseStateAfter !== null;
  const badgeState =
    day.status === JourneyStatus.Completed
      ? "arrival"
      : day.status === JourneyStatus.Interrupted
        ? "interrupted"
        : day.encounterResolution
          ? "resolved"
          : day.trailEvent
            ? "eventful"
            : day.openingNarration
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
            {day.originTownName} to {day.destinationTownName} | {formatTravelMode(day.startingTravelMode)} to{" "}
            {formatTravelMode(day.endingTravelMode)} | {day.status === JourneyStatus.Active ? "In motion" : formatJourneyStatus(day.status)}
          </DaySubhead>
        </div>
        <DayBadge data-state={badgeState}>{badgeLabel}</DayBadge>
      </DiaryDayHeader>

      <DiaryBody>
        {day.openingNarration ? <OpeningNote>{day.openingNarration}</OpeningNote> : null}
        {day.entries.map((entry, index) => (
          <DiaryParagraph key={`${day.dayNumber}-${index}`}>{entry}</DiaryParagraph>
        ))}
      </DiaryBody>

      {day.trailEvent ? (
        <TrailNote>
          <strong>{day.trailEvent.title}</strong>
          <TrailNoteMeta>
            <span>Wallet Δ {formatSignedNumber(day.trailEvent.walletDelta, 2)}</span>
            <span>Food Δ {formatSignedNumber(day.trailEvent.foodDelta)}</span>
            <span>Canteen Δ {formatSignedNumber(day.trailEvent.canteenChargeDelta)}</span>
            <span>Delay Δ {formatSignedNumber(day.trailEvent.delayDays)}</span>
            <span>Heat Δ {formatSignedNumber(day.trailEvent.heatIncrease)}</span>
            {hasHorseState ? <span>Horse hunger Δ {formatSignedNumber(day.trailEvent.horseHungerDelta)}</span> : null}
            {hasHorseState ? <span>Horse thirst Δ {formatSignedNumber(day.trailEvent.horseThirstDelta)}</span> : null}
            {hasHorseState ? <span>Horse exhaustion Δ {formatSignedNumber(day.trailEvent.horseExhaustionDelta)}</span> : null}
          </TrailNoteMeta>
        </TrailNote>
      ) : null}

      {day.encounterResolution ? (
        <ResolutionNote>
          <strong>{day.encounterResolution.choiceLabel}</strong>
          <p>{renderResolutionSummary(day.encounterResolution)}</p>
          <TrailNoteMeta>
            <span>Health Δ {formatSignedNumber(day.encounterResolution.healthDelta)}</span>
            <span>Wallet Δ {formatSignedNumber(day.encounterResolution.walletDelta, 2)}</span>
            <span>Ammo Δ {formatSignedNumber(-day.encounterResolution.ammoSpent)}</span>
            <span>Heat Δ {formatSignedNumber(day.encounterResolution.heatIncrease)}</span>
            {hasHorseState ? <span>Horse exhaustion Δ {formatSignedNumber(day.encounterResolution.horseExhaustionDelta)}</span> : null}
          </TrailNoteMeta>
        </ResolutionNote>
      ) : null}

      <DayMeta>{renderDayMeta(day)}</DayMeta>
    </DiaryDayCard>
  );
}

function renderResolutionSummary(resolution: NonNullable<TravelDiaryDayDto["encounterResolution"]>) {
  switch (resolution.choiceId) {
    case "run":
      return "I run for it and keep the trail moving.";
    case "fight":
      return resolution.ammoSpent > 0
        ? "I stand and fight, spending one round to force the rider off the trail."
        : "I stand and fight with my knife and force the rider off the trail.";
    case "bribe":
      return "I pay my way through and keep moving.";
    default:
      return `I choose to ${resolution.choiceLabel.toLowerCase()}.`;
  }
}

function renderDayMeta(day: TravelDiaryDayDto) {
  const hasHorseState = day.horseStateAfter !== null;
  const pieces = [
    `Health ${day.currentHealth} (${formatSignedNumber(day.healthDelta)})`,
    `Wallet ${day.currentWallet.toFixed(2)} (${formatSignedNumber(day.walletDelta, 2)})`,
    `Food ${day.currentFood} (${formatSignedNumber(day.foodDelta)})`,
    `Canteen ${day.currentCanteenCharges} (${formatSignedNumber(day.canteenChargeDelta)})`,
    `Ammo ${day.currentAmmo} (${formatSignedNumber(-day.ammoSpent)})`,
    `Heat ${day.currentHeat} (${formatSignedNumber(day.heatIncrease)})`,
  ];

  if (hasHorseState) {
    pieces.splice(3, 0, `Horse ${formatHorseTravelState(day.horseStateAfter)}`);
    pieces.splice(4, 0, `Horse feed ${day.currentHorseFeed} (${formatSignedNumber(day.horseFeedDelta)})`);
  }

  return pieces.join(" | ");
}

const DiaryDayCard = styled.article`
  display: grid;
  gap: 12px;
  padding: 16px;
  border-radius: 18px;
  background:
    linear-gradient(180deg, rgba(250, 244, 232, 0.055), rgba(255, 255, 255, 0.02)),
    rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.08);
  color: #f7f3ea;
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
  color: rgba(242, 239, 232, 0.68);
  font-size: 0.92rem;
`;

const DayBadge = styled.span`
  padding: 5px 10px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  color: rgba(242, 239, 232, 0.82);
  background: rgba(255, 255, 255, 0.04);
  font-size: 0.78rem;
  white-space: nowrap;

  &[data-state="arrival"] {
    color: #1b1308;
    background: linear-gradient(180deg, #f0d39b, #c8843d);
    border-color: rgba(240, 211, 155, 0.58);
  }

  &[data-state="interrupted"] {
    color: #ffe8e3;
    background: rgba(240, 126, 110, 0.14);
    border-color: rgba(240, 126, 110, 0.24);
  }

  &[data-state="resolved"] {
    color: #def3e0;
    background: rgba(95, 159, 111, 0.14);
    border-color: rgba(95, 159, 111, 0.24);
  }

  &[data-state="eventful"],
  &[data-state="departure"] {
    color: #1b1308;
    background: linear-gradient(180deg, #efc37e, #b87634);
    border-color: rgba(239, 195, 126, 0.42);
  }
`;

const DiaryBody = styled.div`
  display: grid;
  gap: 10px;
  font-family: "Iowan Old Style", Georgia, serif;
  font-size: 1.02rem;
  line-height: 1.65;
`;

const OpeningNote = styled.p`
  margin: 0;
  padding: 12px 14px;
  border-left: 3px solid rgba(239, 195, 126, 0.72);
  border-radius: 12px;
  background: rgba(239, 195, 126, 0.08);
  color: rgba(247, 243, 234, 0.94);
`;

const DiaryParagraph = styled.p`
  margin: 0;
`;

const TrailNote = styled.div`
  display: grid;
  gap: 8px;
  padding: 13px 14px;
  border-radius: 16px;
  background: rgba(223, 159, 79, 0.09);
  border: 1px solid rgba(223, 159, 79, 0.18);

  p {
    margin: 0;
    color: rgba(242, 239, 232, 0.8);
  }
`;

const TrailNoteMeta = styled.div`
  display: flex;
  flex-wrap: wrap;
  gap: 8px 12px;
  color: rgba(242, 239, 232, 0.64);
  font-size: 0.84rem;
`;

const ResolutionNote = styled.div`
  display: grid;
  gap: 6px;
  padding: 13px 14px;
  border-radius: 16px;
  background: rgba(95, 159, 111, 0.11);
  border: 1px solid rgba(95, 159, 111, 0.2);

  p {
    margin: 0;
    color: rgba(242, 239, 232, 0.82);
  }
`;

const DayMeta = styled.p`
  margin: 0;
  color: rgba(242, 239, 232, 0.54);
  font-size: 0.84rem;
`;
