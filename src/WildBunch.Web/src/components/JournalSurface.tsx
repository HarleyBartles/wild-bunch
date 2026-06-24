import { useMemo } from "react";
import type { GameLogEntryDto, JournalDto } from "../api/types";

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
  return `Day ${journal.clock.day}, ${journal.clock.timeOfDay} in ${journal.currentTown.name}`;
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
    <article className="journal-entry">
      <p className="journal-entry__message">{formatJournalEntryMessage(entry.message)}</p>
    </article>
  );
}

function JournalDaySection({ group }: { group: JournalDayGroup }) {
  return (
    <section className="journal-day">
      <header className="journal-day__header">
        <div>
          <p className="eyebrow">Day {group.day}</p>
        </div>
      </header>
      <div className="journal-day__entries">
        {group.entries.map((entry, index) => (
          <JournalEntryCard key={`${entry.day}-${entry.turn}-${index}`} entry={entry} />
        ))}
      </div>
    </section>
  );
}

export function JournalSurface({ journal, loading, error, sessionLogEntries }: JournalSurfaceProps) {
  const entries = getEntries(journal, sessionLogEntries);
  const groups = useMemo(() => groupEntriesByDay(entries), [entries]);

  if (loading) {
    return <div className="case-modal__state">Opening the trail journal...</div>;
  }

  if (error) {
    return <div className="case-modal__state">{error || "Load a game to read the trail journal."}</div>;
  }

  if (!journal) {
    return <div className="case-modal__state">Load a game to read the trail journal.</div>;
  }

  return (
    <section className="case-modal__section case-modal__section--wide journal-surface">
      <div className="case-modal__section-head journal-surface__head">
        <h3 className="journal-surface__clock">{formatJournalClock(journal)}</h3>
      </div>

      <div className="journal-timeline">
        {groups.length > 0 ? (
          groups.map((group) => <JournalDaySection key={group.day} group={group} />)
        ) : (
          <p className="muted">No notes yet. The trail is still being written.</p>
        )}
      </div>
    </section>
  );
}
