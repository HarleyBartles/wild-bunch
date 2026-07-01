import { useMemo } from "react";
import styled from "styled-components";
import type { GameLogEntryDto, JournalDto } from "../api/types";
import {
  StatusCard,
  Eyebrow,
  Muted,
  Stack,
  ItemCard,
} from "./ui/sharedStyled";
import { formatClockBeat } from "../ui/beatFormatters";

const ModalState = styled.div`
  padding: 18px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
  color: var(--text);
`;

const JournalClock = styled.h3`
  margin: 0;
  padding: 10px 14px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid var(--border);
  color: var(--text);
  font-size: 1rem;
  line-height: 1.3;
  font-variant-numeric: tabular-nums;
  text-wrap: pretty;
  white-space: nowrap;
`;

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

const JournalSurfaceSection = styled(StatusCard).attrs({ as: "section" })`
  grid-column: 1 / -1;
  display: grid;
  gap: 18px;
`;

interface JournalSurfaceProps {
  journal: JournalDto | null;
  loading: boolean;
  error: string;
  sessionLogEntries?: GameLogEntryDto[];
}

interface JournalDayGroup {
  day: number;
  entries: GameLogEntryDto[];
}

function getEntries(journal: JournalDto | null, sessionLogEntries: GameLogEntryDto[] | undefined) {
  return journal?.logEntries ?? sessionLogEntries ?? [];
}

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
        .map((group) => group.entry),
    }));
}

function formatJournalClock(journal: JournalDto) {
  return `${formatClockBeat(journal.clock)} in ${journal.currentTown.name}`;
}

function formatJournalEntryMessage(message: string) {
  const openingMatch = message.match(/^The hunt begins in (.+)\.$/);
  if (openingMatch) {
    return `Started out in ${openingMatch[1]}.`;
  }

  return message;
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

export function JournalSurface({
  journal,
  loading,
  error,
  sessionLogEntries,
}: JournalSurfaceProps) {
  const entries = getEntries(journal, sessionLogEntries);
  const groups = useMemo(() => groupEntriesByDay(entries), [entries]);

  if (loading) {
    return <ModalState>Opening the trail journal...</ModalState>;
  }

  if (error) {
    return <ModalState>{error || "Load a game to read the trail journal."}</ModalState>;
  }

  if (!journal) {
    return <ModalState>Load a game to read the trail journal.</ModalState>;
  }

  return (
    <JournalSurfaceSection>
      <header>
        <JournalClock>{formatJournalClock(journal)}</JournalClock>
      </header>

      <JournalTimeline>
        {groups.length > 0 ? (
          groups.map((group) => <JournalDaySection key={group.day} group={group} />)
        ) : (
          <Muted>No notes yet. The trail is still being written.</Muted>
        )}
      </JournalTimeline>
    </JournalSurfaceSection>
  );
}
