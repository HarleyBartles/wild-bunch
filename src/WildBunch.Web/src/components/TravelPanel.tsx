import type {
  GameTurnResultDto,
  JourneyEncounterDto,
  JourneyTrailEventDto,
  TravelDiaryDayDto,
  TravelDiaryDto,
  TravelJourneyDto,
} from "../api/types";
import {
  formatHorseTravelState,
  formatJourneyStatus,
  formatRisk,
  formatTrailTerrain,
  formatTravelMode,
  formatWaterFeature,
} from "../ui/formatters";

interface TravelPanelProps {
  journey: TravelJourneyDto | null;
  travelDiary: TravelDiaryDto | null;
  latestTravelResult: GameTurnResultDto | null;
}

function renderJourneyLine(label: string, value: string | number | null | undefined) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value ?? "Unknown"}</dd>
    </div>
  );
}

function EncounterCard({ encounter }: { encounter: JourneyEncounterDto }) {
  return (
    <div className="compact-item">
      <strong>Pending encounter</strong>
      <p>
        {encounter.kind}: {encounter.message}
      </p>
      <p>Choices: {encounter.choices.map((choice) => `${choice.id} (${choice.label})`).join(", ")}</p>
    </div>
  );
}

function TrailEventCard({ trailEvent }: { trailEvent: JourneyTrailEventDto }) {
  return (
    <div className="compact-item">
      <strong>{trailEvent.title}</strong>
      <p>
        {trailEvent.id} | {trailEvent.kind === 0 ? "Lucky" : "Bad luck"}
      </p>
      <p>{trailEvent.message}</p>
      <p>
        Consequences: wallet {trailEvent.walletDelta >= 0 ? "+" : ""}
        {trailEvent.walletDelta.toFixed(2)}, food {trailEvent.foodDelta >= 0 ? "+" : ""}
        {trailEvent.foodDelta}, canteen {trailEvent.canteenChargeDelta >= 0 ? "+" : ""}
        {trailEvent.canteenChargeDelta}, horse hunger {trailEvent.horseHungerDelta >= 0 ? "+" : ""}
        {trailEvent.horseHungerDelta}, horse thirst {trailEvent.horseThirstDelta >= 0 ? "+" : ""}
        {trailEvent.horseThirstDelta}, horse exhaustion {trailEvent.horseExhaustionDelta >= 0 ? "+" : ""}
        {trailEvent.horseExhaustionDelta}, delay {trailEvent.delayDays >= 0 ? "+" : ""}
        {trailEvent.delayDays}, heat {trailEvent.heatIncrease >= 0 ? "+" : ""}
        {trailEvent.heatIncrease}
      </p>
    </div>
  );
}

function DiaryDayCard({ day }: { day: TravelDiaryDayDto }) {
  return (
    <article className="compact-item">
      <strong>Day {day.dayNumber}</strong>
      <p>
        {day.originTownName} to {day.destinationTownName} | {day.startingTravelMode === 0 ? "mounted" : "foot"} to{" "}
        {day.endingTravelMode === 0 ? "mounted" : "foot"} | {day.status === 0 ? "Active" : day.status === 1 ? "Interrupted" : day.status === 2 ? "Completed" : "Failed"}
      </p>
      <div className="stack">
        {day.entries.map((entry, index) => (
          <p key={`${day.dayNumber}-${index}`}>{entry}</p>
        ))}
      </div>
    </article>
  );
}

export function TravelPanel({ journey, travelDiary, latestTravelResult }: TravelPanelProps) {
  const activeJourney = journey ?? latestTravelResult?.journey ?? null;
  const trailEvent = latestTravelResult?.trailEvent ?? null;
  const latestJourneyStatus = latestTravelResult?.journeyStatus ?? null;
  const activeTravelDiary = latestTravelResult?.travelDiary ?? travelDiary;

  return (
    <article className="status-card">
      <h3>Travel</h3>
      {activeJourney ? (
        <>
          <dl className="stat-list">
            {renderJourneyLine("Route", `${activeJourney.originTownName} to ${activeJourney.destinationTownName}`)}
            {renderJourneyLine("Travel mode", formatTravelMode(activeJourney.travelMode))}
            {renderJourneyLine("Status", formatJourneyStatus(activeJourney.status))}
            {renderJourneyLine("Ride-day distance", activeJourney.rideDayDistance.toFixed(2))}
            {renderJourneyLine("Remaining ride-day distance", activeJourney.remainingRideDayDistance.toFixed(2))}
            {renderJourneyLine("Expected days", activeJourney.expectedDays)}
            {renderJourneyLine("Remaining days", activeJourney.remainingDays)}
            {renderJourneyLine("Delay days", activeJourney.delayDays)}
            {renderJourneyLine("Delay margin", activeJourney.delayMarginDays)}
            {renderJourneyLine("Water", formatWaterFeature(activeJourney.routeProfile.waterFeature))}
            {renderJourneyLine("Terrain", formatTrailTerrain(activeJourney.routeProfile.terrain))}
            {renderJourneyLine("Risk", formatRisk(activeJourney.routeProfile.risk))}
            {renderJourneyLine("Canteen/day", activeJourney.canteenChargesPerDay)}
            {renderJourneyLine("Canteen needed", activeJourney.requiredCanteenCharges)}
            {renderJourneyLine("Canteen available", activeJourney.availableCanteenCharges)}
            {renderJourneyLine("Canteen reserve", activeJourney.canteenReserveCharges)}
          </dl>

          <div className="stack">
            <div className="compact-item">
              <strong>Horse</strong>
              <p>{formatHorseTravelState(activeJourney.horseState)}</p>
              <p>
                Mounted travel: {activeJourney.mountedTravelAvailable ? "available" : "unavailable"} | Water secure:{" "}
                {activeJourney.waterSecure ? "yes" : "no"}
              </p>
            </div>

            {activeJourney.warnings.length > 0 ? (
              <div className="compact-item">
                <strong>Route warnings</strong>
                <p>{activeJourney.warnings.join(" | ")}</p>
              </div>
            ) : null}

            {activeJourney.pendingEncounter ? <EncounterCard encounter={activeJourney.pendingEncounter} /> : null}
          </div>
        </>
      ) : (
        <p className="muted">No active journey.</p>
      )}

      {latestTravelResult ? (
        <div className="stack">
          {trailEvent ? <TrailEventCard trailEvent={trailEvent} /> : null}
          <div className="compact-item">
            <strong>Latest turn result</strong>
            <p>{latestTravelResult.message}</p>
            <p>
              Status: {latestJourneyStatus === null ? "Unknown" : formatJourneyStatus(latestJourneyStatus)}
            </p>
          </div>
        </div>
      ) : null}

      {activeTravelDiary && activeTravelDiary.days.length > 0 ? (
        <div className="stack">
          <h4>Travel diary</h4>
          {activeTravelDiary.days.map((day) => (
            <DiaryDayCard key={day.dayNumber} day={day} />
          ))}
        </div>
      ) : null}
    </article>
  );
}
