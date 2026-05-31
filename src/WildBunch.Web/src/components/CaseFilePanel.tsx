import type { JournalDto } from "../api/types";
import { formatGameStatus, formatClueKind, formatSuspectStatus } from "../ui/formatters";

interface CaseFilePanelProps {
  journal: JournalDto | null;
}

function formatClueRecency(recency: number) {
  switch (recency) {
    case 1:
      return "recent";
    case 2:
      return "yesterday";
    case 3:
      return "today";
    case 4:
      return "old";
    default:
      return "unknown";
  }
}

export function CaseFilePanel({ journal }: CaseFilePanelProps) {
  const renderAnchorSummary = (label: string, items: { label: string; alias: string | null; feature: string | null; fact: string | null }[] | { label: string; place: string | null; route: string | null }[] | { label: string; movement: string | null; route: string | null }[] | { recency: number; day: number | null; turn: number | null }[]) =>
    items.length > 0 ? (
      <p className="case-anchor">
        <strong>{label}:</strong>{" "}
        {items
          .map((item) => {
            if ("recency" in item) {
              const parts = [formatClueRecency(item.recency)];
              if (item.day !== null) parts.push(`day ${item.day}`);
              if (item.turn !== null) parts.push(`turn ${item.turn}`);
              return parts.join(", ");
            }

            const parts = [item.label];
            if ("alias" in item && item.alias) parts.push(`alias ${item.alias}`);
            if ("feature" in item && item.feature) parts.push(item.feature);
            if ("fact" in item && item.fact) parts.push(item.fact);
            if ("place" in item && item.place) parts.push(item.place);
            if ("route" in item && item.route) parts.push(item.route);
            if ("movement" in item && item.movement) parts.push(item.movement);
            return parts.join(" - ");
          })
          .join(" | ")}
      </p>
    ) : null;

  return (
    <section className="panel panel--wide">
      <div className="panel-head">
        <h2>Case file</h2>
        <span className="panel-subtitle">{journal ? `Updated day ${journal.clock.day}, turn ${journal.clock.turn}` : "No journal loaded"}</span>
      </div>

      {journal ? (
        <div className="case-grid">
          <article className="status-card">
            <h3>Summary</h3>
            <p className="case-summary">{journal.caseFile.caseSummary}</p>
            <p className="case-lead">
              <strong>Opening lead:</strong> {journal.caseFile.openingLead}
            </p>
            <dl className="stat-list">
              <div>
                <dt>Status</dt>
                <dd>{formatGameStatus(journal.status)}</dd>
              </div>
              <div>
                <dt>Town</dt>
                <dd>{journal.currentTown.name}</dd>
              </div>
              <div>
                <dt>Accusation</dt>
                <dd>{journal.caseFile.accusationId ?? "None"}</dd>
              </div>
              <div>
                <dt>Case state</dt>
                <dd>{journal.caseFile.caseState.statusText}</dd>
              </div>
            </dl>
            <p className="case-release">{journal.caseFile.caseState.statusText}</p>
          </article>

          <article className="status-card">
            <h3>Discovered suspects</h3>
            <div className="stack">
              {journal.caseFile.discoveredSuspects.length > 0 ? (
                journal.caseFile.discoveredSuspects.map((suspect) => (
                  <div key={suspect.id} className="compact-item">
                    <strong>{suspect.name}</strong>
                    <p>
                      {suspect.id} - {formatSuspectStatus(suspect.status)}
                    </p>
                  </div>
                ))
              ) : (
                <p className="muted">No suspects have been discovered yet.</p>
              )}
            </div>
          </article>

          <article className="status-card">
            <h3>Known clues</h3>
            <div className="stack">
              {journal.caseFile.knownClues.length > 0 ? (
                journal.caseFile.knownClues.map((clue) => (
                  <div key={clue.id} className="compact-item">
                    <strong>{clue.description}</strong>
                    <p>
                      {clue.id} - {formatClueKind(clue.kind)}
                    </p>
                    {clue.sourceLabel ? (
                      <p>
                        <strong>Source:</strong> {clue.sourceLabel}
                        {clue.context ? ` - ${clue.context}` : ""}
                      </p>
                    ) : null}
                    {renderAnchorSummary("Subjects", clue.anchors.subjects)}
                    {renderAnchorSummary("Locations", clue.anchors.locations)}
                    {renderAnchorSummary("Times", clue.anchors.times)}
                    {renderAnchorSummary("Directions", clue.anchors.directions)}
                  </div>
                ))
              ) : (
                <p className="muted">No clues recorded yet.</p>
              )}
            </div>
          </article>
        </div>
      ) : (
        <p className="muted">Load a game to inspect the case file.</p>
      )}
    </section>
  );
}
