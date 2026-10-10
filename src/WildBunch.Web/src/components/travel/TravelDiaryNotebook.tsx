import type { GameSessionDto } from "../../api/types";
import { Card, SectionHeader } from "./travelShared";
import { TravelDiaryDayCard } from "./TravelDiaryDayCard";
import { JournalEntryTimeline } from "../journal/JournalEntryTimeline";
import styled from "styled-components";

interface TravelDiaryNotebookProps {
  travelDiary: GameSessionDto["travelDiary"];
  refreshing: boolean;
}

export function TravelDiaryNotebook({ travelDiary, refreshing }: TravelDiaryNotebookProps) {
  return (
    <NotebookCard>
      <SectionHeader>
        <strong>Travel diary</strong>
        <span>
          {refreshing
            ? "Refreshing..."
            : travelDiary?.journeyEntries.length
              ? `${travelDiary.journeyEntries.length} entries`
              : travelDiary?.days.length
                ? `${travelDiary.days.length} travel days`
                : "Blank pages"}
        </span>
      </SectionHeader>

      {travelDiary?.days.length ? (
        <>
          <JournalEntryTimeline
            entries={travelDiary.journeyEntries}
            emptyMessage="This journey has no journal entries yet."
          />
          <DiaryStack>
            {travelDiary.days.map((day) => (
              <TravelDiaryDayCard key={day.dayNumber} day={day} />
            ))}
          </DiaryStack>
        </>
      ) : travelDiary?.journeyEntries.length ? (
        <JournalEntryTimeline
          entries={travelDiary.journeyEntries}
          emptyMessage="This journey has no journal entries yet."
        />
      ) : (
        <MutedNote>The notebook is waiting for the next mile of road.</MutedNote>
      )}
    </NotebookCard>
  );
}

const NotebookCard = styled(Card)`
  grid-column: 1 / -1;
`;

const DiaryStack = styled.div`
  display: grid;
  gap: 14px;
`;

const MutedNote = styled.p`
  margin: 0;
  color: color-mix(in srgb, var(--text) 66%, transparent);
`;
