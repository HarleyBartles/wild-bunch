import { createContext, useContext } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import styled from "styled-components";
import { advanceTravelDay, getGame, resolveTravelEncounter } from "../api/wildBunchApi";
import type {
  GameSessionDto,
  GameTurnResultDto,
  JourneyEncounterChoiceDto,
  JourneyEncounterDto,
  TravelDiaryDayDto,
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
  gameId: string;
  session: GameSessionDto;
  busy: boolean;
  onTurnResult: (result: GameTurnResultDto) => Promise<void> | void;
}

interface TravelUiContextValue {
  session: GameSessionDto;
  busy: boolean;
  refreshing: boolean;
  actionError: string | null;
  advanceTravelDay: () => Promise<void>;
  resolveTravelEncounter: (choiceId: string) => Promise<void>;
}

const TravelUiContext = createContext<TravelUiContextValue | null>(null);

function useTravelUi() {
  const context = useContext(TravelUiContext);
  if (!context) {
    throw new Error("Travel UI context is unavailable.");
  }

  return context;
}

function formatSignedNumber(value: number, digits = 0) {
  const formatted = value.toFixed(digits);
  return value > 0 ? `+${formatted}` : formatted;
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === "string" && error.trim()) {
    return error;
  }

  return "";
}

function JourneyDecision({ encounter }: { encounter: JourneyEncounterDto }) {
  const { busy, refreshing, resolveTravelEncounter } = useTravelUi();
  const disabled = busy || refreshing;

  return (
    <DecisionPanel>
      <DecisionHeading>
        <strong>Trail decision</strong>
        <span>{encounter.kind}</span>
      </DecisionHeading>
      <DecisionBody>{encounter.message}</DecisionBody>
      <ChoiceRow>
        {encounter.choices.map((choice: JourneyEncounterChoiceDto) => (
          <ChoiceButton key={choice.id} type="button" onClick={() => void resolveTravelEncounter(choice.id)} disabled={disabled}>
            {choice.label}
          </ChoiceButton>
        ))}
      </ChoiceRow>
      <DecisionHint>Resolve the encounter before advancing the next day.</DecisionHint>
    </DecisionPanel>
  );
}

function TravelSummary() {
  const { session } = useTravelUi();
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
        <SummaryItem>
          <dt>Horse</dt>
          <dd>{formatHorseTravelState(journey.horseState)}</dd>
        </SummaryItem>
        <SummaryItem>
          <dt>Water pressure</dt>
          <dd>{journey.waterSecure ? "Secure" : "Drying out"}</dd>
        </SummaryItem>
        <SummaryItem>
          <dt>Canteen needed</dt>
          <dd>
            {journey.requiredCanteenCharges} needed, {journey.availableCanteenCharges} available
          </dd>
        </SummaryItem>
        <SummaryItem>
          <dt>Delay margin</dt>
          <dd>{journey.delayMarginDays}</dd>
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
        <SummaryItem>
          <dt>Ride-day distance</dt>
          <dd>{journey.routeProfile.rideDayDistance.toFixed(2)}</dd>
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

function TravelActions() {
  const { actionError, advanceTravelDay, busy, refreshing, session } = useTravelUi();
  const journey = session.journey;

  if (!journey) {
    return null;
  }

  const disabled = busy || refreshing;
  const pendingEncounter = journey.pendingEncounter;
  const canAdvance = journey.status === 0 && !pendingEncounter;

  return (
    <ActionCard>
      <SectionHeader>
        <strong>Trail action</strong>
        <span>{pendingEncounter ? "Encounter waiting" : "Ready to ride"}</span>
      </SectionHeader>

      {actionError ? <InlineError>{actionError}</InlineError> : null}

      {pendingEncounter ? (
        <JourneyDecision encounter={pendingEncounter} />
      ) : canAdvance ? (
        <>
          <ActionCopy>
            The notebook is ready for the next stretch of road. Advance the day when you want the trail to continue.
          </ActionCopy>
          <PrimaryButton type="button" onClick={() => void advanceTravelDay()} disabled={disabled}>
            {busy ? "Advancing..." : "Advance travel day"}
          </PrimaryButton>
        </>
      ) : (
        <ActionCopy>The journey is paused. Refresh to sync the latest trail state.</ActionCopy>
      )}
    </ActionCard>
  );
}

function DayFooter({ day }: { day: TravelDiaryDayDto }) {
  const pieces = [
    `Health ${formatSignedNumber(day.healthDelta)}`,
    `Wallet ${formatSignedNumber(day.walletDelta, 2)}`,
    `Food ${formatSignedNumber(day.foodDelta)}`,
    `Horse feed ${formatSignedNumber(day.horseFeedDelta)}`,
    `Canteen ${formatSignedNumber(day.canteenChargeDelta)}`,
    `Ammo ${formatSignedNumber(-day.ammoSpent)}`,
    `Heat ${formatSignedNumber(day.heatIncrease)}`,
  ];

  return <DayMeta>{pieces.join(" | ")}</DayMeta>;
}

function DiaryDay({ day }: { day: TravelDiaryDayDto }) {
  return (
    <DiaryDayCard>
      <DiaryDayHeader>
        <div>
          <DayTitle>Day {day.dayNumber}</DayTitle>
          <DaySubhead>
            {day.originTownName} to {day.destinationTownName} | {formatTravelMode(day.startingTravelMode)} to{" "}
            {formatTravelMode(day.endingTravelMode)} | {day.status === 0 ? "In motion" : formatJourneyStatus(day.status)}
          </DaySubhead>
        </div>
        <DayBadge>
          {day.pendingEncounter ? "Interrupted" : day.openingNarration ? "Departure" : day.trailEvent ? "Eventful" : "Quiet trail"}
        </DayBadge>
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
          <p>{day.trailEvent.message}</p>
          <TrailNoteMeta>
            <span>Wallet {formatSignedNumber(day.trailEvent.walletDelta, 2)}</span>
            <span>Food {formatSignedNumber(day.trailEvent.foodDelta)}</span>
            <span>Canteen {formatSignedNumber(day.trailEvent.canteenChargeDelta)}</span>
            <span>Horse hunger {formatSignedNumber(day.trailEvent.horseHungerDelta)}</span>
            <span>Horse thirst {formatSignedNumber(day.trailEvent.horseThirstDelta)}</span>
            <span>Horse exhaustion {formatSignedNumber(day.trailEvent.horseExhaustionDelta)}</span>
            <span>Delay {formatSignedNumber(day.trailEvent.delayDays)}</span>
            <span>Heat {formatSignedNumber(day.trailEvent.heatIncrease)}</span>
          </TrailNoteMeta>
        </TrailNote>
      ) : null}

      {day.encounterResolution ? (
        <ResolutionNote>
          <strong>{day.encounterResolution.choiceLabel}</strong>
          <p>
            Choice {day.encounterResolution.choiceId} shifted the day by health {formatSignedNumber(day.encounterResolution.healthDelta)}
            , wallet {formatSignedNumber(day.encounterResolution.walletDelta, 2)}, ammo spent {day.encounterResolution.ammoSpent}, heat{" "}
            {formatSignedNumber(day.encounterResolution.heatIncrease)}, horse exhaustion{" "}
            {formatSignedNumber(day.encounterResolution.horseExhaustionDelta)}.
          </p>
        </ResolutionNote>
      ) : null}

      {day.warnings.length > 0 ? (
        <DayWarnings>
          {day.warnings.map((warning) => (
            <li key={warning}>{warning}</li>
          ))}
        </DayWarnings>
      ) : null}

      <DayFooter day={day} />
    </DiaryDayCard>
  );
}

function TravelDiaryNotebook() {
  const { refreshing, session } = useTravelUi();
  const travelDiary = session.travelDiary;

  return (
    <NotebookCard>
      <SectionHeader>
        <strong>Travel diary</strong>
        <span>{refreshing ? "Refreshing..." : travelDiary?.days.length ? `${travelDiary.days.length} entries` : "Blank pages"}</span>
      </SectionHeader>

      {travelDiary?.days.length ? (
        <DiaryStack>
          {travelDiary.days.map((day) => (
            <DiaryDay key={day.dayNumber} day={day} />
          ))}
        </DiaryStack>
      ) : (
        <MutedNote>The notebook is waiting for the next mile of road.</MutedNote>
      )}
    </NotebookCard>
  );
}

export function TravelPanel({ gameId, session, busy, onTurnResult }: TravelPanelProps) {
  const queryClient = useQueryClient();

  const travelSessionQuery = useQuery({
    queryKey: ["travel-session", gameId],
    queryFn: () => getGame(gameId),
    enabled: Boolean(gameId && session.journey),
    initialData: session,
    staleTime: 0,
  });

  const travelSession = session;

  const advanceMutation = useMutation({
    mutationFn: () => advanceTravelDay(gameId),
    onSuccess: async (result) => {
      await onTurnResult(result);
      await queryClient.invalidateQueries({ queryKey: ["travel-session", gameId] });
    },
  });

  const resolveMutation = useMutation({
    mutationFn: (choiceId: string) => resolveTravelEncounter(gameId, choiceId),
    onSuccess: async (result) => {
      await onTurnResult(result);
      await queryClient.invalidateQueries({ queryKey: ["travel-session", gameId] });
    },
  });

  const actionError =
    getErrorMessage(advanceMutation.error) ||
    getErrorMessage(resolveMutation.error) ||
    getErrorMessage(travelSessionQuery.error);

  const travelUi: TravelUiContextValue = {
    session: travelSession,
    busy: busy || advanceMutation.isPending || resolveMutation.isPending,
    refreshing: travelSessionQuery.isFetching,
    actionError: actionError || null,
    advanceTravelDay: async () => {
      await advanceMutation.mutateAsync();
    },
    resolveTravelEncounter: async (choiceId: string) => {
      await resolveMutation.mutateAsync(choiceId);
    },
  };

  return (
    <TravelUiContext.Provider value={travelUi}>
      <TravelStage>
        <TravelHeader>
          <div>
            <Eyebrow>Trail notebook</Eyebrow>
            <Title>Travel diary</Title>
            <Lead>
              A player-facing trail log that keeps the road in first person, with the next action sitting beside the pages.
            </Lead>
          </div>
          <HeaderMeta>
            <MetaCard>
              <span>Journey</span>
              <strong>{formatJourneyStatus(travelSession.journey?.status ?? 0)}</strong>
            </MetaCard>
            <MetaCard>
              <span>Destination</span>
              <strong>{travelSession.journey?.destinationTownName ?? "Unknown"}</strong>
            </MetaCard>
          </HeaderMeta>
        </TravelHeader>

        {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}
        {travelSessionQuery.isFetching ? <InfoBanner>Refreshing trail pages from the backend.</InfoBanner> : null}

        <TravelGrid>
          <TravelSummary />
          <TravelActions />
          <TravelDiaryNotebook />
        </TravelGrid>
      </TravelStage>
    </TravelUiContext.Provider>
  );
}

const TravelStage = styled.article`
  grid-column: 1 / -1;
  display: grid;
  gap: 18px;
  padding: 22px;
  border-radius: 28px;
  border: 1px solid rgba(228, 186, 126, 0.24);
  background:
    radial-gradient(circle at top right, rgba(236, 203, 146, 0.12), transparent 26%),
    linear-gradient(180deg, rgba(34, 25, 16, 0.98), rgba(18, 13, 8, 0.98));
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
  color: #efc37e;
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
  color: rgba(242, 239, 232, 0.76);
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
    color: rgba(242, 239, 232, 0.62);
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

const Card = styled.section`
  display: grid;
  gap: 14px;
  padding: 18px;
  border-radius: 22px;
  background: rgba(255, 255, 255, 0.035);
  border: 1px solid rgba(255, 255, 255, 0.08);
`;

const SummaryCard = styled(Card)`
  grid-column: 1 / -1;
`;

const ActionCard = styled(Card)``;

const NotebookCard = styled(Card)`
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

const StatusPill = styled.span`
  display: inline-flex;
  align-items: center;
  padding: 5px 10px;
  border-radius: 999px;
  color: #1b1308 !important;
  background: linear-gradient(180deg, #efc37e, #bc7a36);
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
    color: rgba(242, 239, 232, 0.58);
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
  border: 1px solid rgba(240, 126, 110, 0.22);
  background: rgba(240, 126, 110, 0.11);

  strong {
    color: #ffd9d2;
  }
`;

const WarningList = styled.ul`
  margin: 0;
  padding-left: 18px;
  color: rgba(242, 239, 232, 0.84);
`;

const MutedNote = styled.p`
  margin: 0;
  color: rgba(242, 239, 232, 0.66);
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

const DecisionPanel = styled.div`
  display: grid;
  gap: 12px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.03);
`;

const DecisionHeading = styled.div`
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: baseline;

  span {
    color: rgba(242, 239, 232, 0.6);
    font-size: 0.82rem;
  }
`;

const DecisionBody = styled.p`
  margin: 0;
  color: rgba(242, 239, 232, 0.86);
`;

const ChoiceRow = styled.div`
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
`;

const ButtonBase = styled.button`
  border-radius: 999px;
  padding: 10px 16px;
  font-weight: 700;
  border: 1px solid transparent;
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

const ChoiceButton = styled(ButtonBase)`
  background: transparent;
  color: #f2efe8;
  border-color: rgba(255, 255, 255, 0.16);

  &:disabled {
    cursor: not-allowed;
    opacity: 0.55;
  }
`;

const DecisionHint = styled.p`
  margin: 0;
  color: rgba(242, 239, 232, 0.58);
  font-size: 0.88rem;
`;

const DiaryStack = styled.div`
  display: grid;
  gap: 14px;
`;

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

const DayWarnings = styled.ul`
  margin: 0;
  padding-left: 18px;
  color: rgba(242, 239, 232, 0.74);
`;

const DayMeta = styled.p`
  margin: 0;
  color: rgba(242, 239, 232, 0.54);
  font-size: 0.84rem;
`;

const ErrorBanner = styled.div`
  padding: 13px 14px;
  border-radius: 16px;
  background: rgba(240, 126, 110, 0.15);
  border: 1px solid rgba(240, 126, 110, 0.26);
  color: #ffe4de;
`;

const InfoBanner = styled.div`
  padding: 13px 14px;
  border-radius: 16px;
  background: rgba(95, 159, 111, 0.12);
  border: 1px solid rgba(95, 159, 111, 0.2);
  color: rgba(242, 239, 232, 0.84);
`;
