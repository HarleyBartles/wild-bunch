import type { JournalDto } from "../api/types";
import { formatGameStatus, formatClueKind, formatSuspectStatus } from "../ui/formatters";

interface CaseFilePanelProps {
  journal: JournalDto | null;
}

export function CaseFilePanel({ journal }: CaseFilePanelProps) {
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
                <dt>Release</dt>
                <dd>
                  {journal.caseFile.killerReleaseState.progress}/{journal.caseFile.killerReleaseState.requiredPublicClues}
                </dd>
              </div>
            </dl>
            <p className="case-release">{journal.caseFile.killerReleaseState.statusText}</p>
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
