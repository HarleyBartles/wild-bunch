import { useMemo } from "react";
import styled from "styled-components";
import type { GameLogEntryDto } from "../../api/types";
import { Eyebrow, Muted, Stack, StatusCard } from "../ui/sharedStyled";

interface JournalDayGroup {
  day: number;
  entries: GameLogEntryDto[];
}

interface JournalEntryTimelineProps {
  entries: GameLogEntryDto[];
  emptyMessage: string;
}

const JournalTimeline = styled.div`
  display: grid;
  gap: 14px;
  margin-top: 18px;
`;

const JournalDay = styled(StatusCard)`
  padding: 16px;
  border-radius: 22px;
`;

const JournalDayHeader = styled.header`
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
`;

const JournalEntry = styled.article`
  padding: 8px 0;
  &:not(:last-child) {
    border-bottom: 1px solid var(--border);
  }
`;

const JournalEntryMessage = styled.p`
  margin: 0;
  font-size: 0.94rem;
  line-height: 1.5;
  color: var(--text);
`;

const JournalEntryStack = styled(Stack)`
  gap: 4px;
`;

function groupEntriesByDay(entries: GameLogEntryDto[]) {
  const grouped = new Map<number, Array<{ entry: GameLogEntryDto; index: number }>>();

  entries.forEach((entry, index) => {
    const current = grouped.get(entry.day);
    if (current) {
      current.push({ entry, index });
      return;
    }

    grouped.set(entry.day, [{ entry, index }]);
  });

  return Array.from(grouped.entries())
    .sort(([leftDay], [rightDay]) => leftDay - rightDay)
    .map(([day, dayEntries]) => ({
      day,
      entries: [...dayEntries]
        .sort((left, right) => left.entry.turn - right.entry.turn || left.index - right.index)
        .map(({ entry }) => entry),
    }));
}

function formatJournalEntryMessage(message: string) {
  const openingMatch = message.match(/^The hunt begins in (.+)\.$/);
  return openingMatch ? `Started out in ${openingMatch[1]}.` : message;
}

function JournalEntryCard({ entry }: { entry: GameLogEntryDto }) {
  return (
    <JournalEntry>
      <JournalEntryMessage>{formatJournalEntryMessage(entry.message)}</JournalEntryMessage>
    </JournalEntry>
  );
}

function JournalDaySection({ group }: { group: JournalDayGroup }) {
  return (
    <JournalDay>
      <JournalDayHeader>
        <Eyebrow>Day {group.day}</Eyebrow>
      </JournalDayHeader>
      <JournalEntryStack>
        {group.entries.map((entry, index) => (
          <JournalEntryCard key={`${entry.day}-${entry.turn}-${index}`} entry={entry} />
        ))}
      </JournalEntryStack>
    </JournalDay>
  );
}

export function JournalEntryTimeline({ entries, emptyMessage }: JournalEntryTimelineProps) {
  const groups = useMemo(() => groupEntriesByDay(entries), [entries]);

  return (
    <JournalTimeline>
      {groups.length > 0 ? (
        groups.map((group) => <JournalDaySection key={group.day} group={group} />)
      ) : (
        <Muted>{emptyMessage}</Muted>
      )}
    </JournalTimeline>
  );
}
