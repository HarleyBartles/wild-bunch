import type { GameLogEntryDto, JournalDto } from "../api/types";
import { formatLogKind } from "../ui/formatters";

interface LogPanelProps {
  journal: JournalDto | null;
  sessionLogEntries: GameLogEntryDto[];
}

function getEntries(journal: JournalDto | null, sessionLogEntries: GameLogEntryDto[]) {
  return journal?.logEntries ?? sessionLogEntries;
}

export function LogPanel({ journal, sessionLogEntries }: LogPanelProps) {
  const entries = getEntries(journal, sessionLogEntries);

  return (
    <section className="panel panel--wide">
      <div className="panel-head">
        <h2>Log</h2>
        <span className="panel-subtitle">{sessionLogEntries.length} entries</span>
      </div>
      <div className="log-list">
        {entries.length > 0 ? (
          entries.map((entry, index) => (
            <article key={`${entry.day}-${entry.turn}-${index}`} className="log-entry">
              <div className="log-entry__meta">
                <strong>{formatLogKind(entry.kind)}</strong>
                <span>
                  Day {entry.day}, Turn {entry.turn}
                </span>
              </div>
              <p>{entry.message}</p>
            </article>
          ))
        ) : (
          <p className="muted">Log entries will appear here as the hunt unfolds.</p>
        )}
      </div>
    </section>
  );
}
