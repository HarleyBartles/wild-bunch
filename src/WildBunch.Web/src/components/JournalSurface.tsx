import { useMemo } from "react";
import type { GameLogEntryDto, JournalDto } from "../api/types";
import { formatLogKind } from "../ui/formatters";

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

function formatEntryClock(entry: GameLogEntryDto) {
  return `Turn ${entry.turn}`;
}

function JournalEntryCard({ entry }: { entry: GameLogEntryDto }) {
  return (
    <article className="journal-entry">
      <div className="journal-entry__meta">
        <span className="tag journal-entry__kind">{formatLogKind(entry.kind)}</span>
        <span className="journal-entry__clock">
          Day {entry.day}, {formatEntryClock(entry)}
        </span>
      </div>
      <p className="journal-entry__message">{entry.message}</p>
    </article>
  );
}

function JournalDaySection({ group }: { group: JournalDayGroup }) {
  return (
    <section className="journal-day">
      <header className="journal-day__header">
        <div>
          <p className="eyebrow">Day {group.day}</p>
          <p className="panel-subtitle">{group.entries.length} public entries</p>
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
    return <div className="case-modal__state">Loading the latest journal...</div>;
  }

  if (error) {
    return <div className="case-modal__state">{error || "Load a game to inspect the journal."}</div>;
  }

  if (!journal) {
    return <div className="case-modal__state">Load a game to inspect the journal.</div>;
  }

  return (
    <section className="case-modal__section case-modal__section--wide journal-surface">
      <div className="case-modal__section-head journal-surface__head">
        <div>
          <h3>Journal</h3>
          <p className="panel-subtitle">A readable, player-safe timeline of what the hunt has made public.</p>
        </div>
        <p className="journal-surface__clock">{formatJournalClock(journal)}</p>
      </div>

      <div className="journal-summary">
        <article className="journal-summary__card">
          <p className="eyebrow">Current town</p>
          <strong>{journal.currentTown.name}</strong>
          <p>
            Day {journal.clock.day}, {journal.clock.timeOfDay}
          </p>
        </article>
        <article className="journal-summary__card">
          <p className="eyebrow">Case summary</p>
          <strong>{journal.caseFile.caseSummary}</strong>
          <p>{journal.caseFile.caseState.statusText}</p>
        </article>
        <article className="journal-summary__card">
          <p className="eyebrow">Entries</p>
          <strong>{entries.length}</strong>
          <p>{entries.length === 1 ? "entry recorded" : "entries recorded"}</p>
        </article>
      </div>

      <div className="journal-timeline">
        {groups.length > 0 ? (
          groups.map((group) => <JournalDaySection key={group.day} group={group} />)
        ) : (
          <p className="muted">Journal entries will appear here as the hunt unfolds.</p>
        )}
      </div>
    </section>
  );
}
