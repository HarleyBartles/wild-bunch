import { useMemo, type ReactNode } from "react";
import type { JournalDto } from "../api/types";
import {
  formatClueKind,
  formatGameStatus,
  formatSuspectStatus,
  formatWarrantDisposition,
} from "../ui/formatters";

interface CaseFileSurfaceProps {
  journal: JournalDto | null;
  loading: boolean;
  error: string;
}

const currencyFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

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

type AnchorRow = {
  label: string;
  value: string;
};

function cleanText(text: string) {
  return text.trim().replace(/\s+/g, " ").replace(/[.?!]+$/u, "");
}

function lowerFirst(text: string) {
  if (text.length === 0) {
    return text;
  }

  return text[0].toLowerCase() + text.slice(1);
}

function addUniqueRow(rows: AnchorRow[], label: string, value: string) {
  const cleanedValue = cleanText(value);
  if (!cleanedValue) {
    return;
  }

  if (rows.some((row) => row.value.toLowerCase() === cleanedValue.toLowerCase())) {
    return;
  }

  rows.push({ label, value: cleanedValue });
}

function formatSubjectRows(subjects: { label: string; alias: string | null; feature: string | null; fact: string | null }[]) {
  const rows: AnchorRow[] = [];

  for (const subject of subjects) {
    addUniqueRow(rows, "Subject", subject.label);

    if (subject.alias && subject.alias !== subject.label) {
      addUniqueRow(rows, "Alias", subject.alias);
    }

    if (subject.feature) {
      addUniqueRow(rows, "Feature", lowerFirst(cleanText(subject.feature)));
    }

    if (subject.fact) {
      addUniqueRow(rows, "Fact", lowerFirst(cleanText(subject.fact)));
    }
  }

  return rows;
}

function formatLocationRows(locations: { label: string; place: string | null; route: string | null }[]) {
  const rows: AnchorRow[] = [];

  for (const location of locations) {
    addUniqueRow(rows, "Location", location.label);

    if (location.place && location.place !== location.label) {
      addUniqueRow(rows, "Place", location.place);
    }

    if (location.route) {
      addUniqueRow(rows, "Route", location.route);
    }
  }

  return rows;
}

function formatTimeRows(times: { recency: number; day: number | null; turn: number | null }[]) {
  const rows: AnchorRow[] = [];

  for (const time of times) {
    const parts = [formatClueRecency(time.recency)];
    if (time.day !== null) {
      parts.push(`day ${time.day}`);
    }
    if (time.turn !== null) {
      parts.push(`turn ${time.turn}`);
    }

    addUniqueRow(rows, "When", parts.join(", "));
  }

  return rows;
}

function formatDirectionRows(directions: { label: string; movement: string | null; route: string | null }[]) {
  const rows: AnchorRow[] = [];

  for (const direction of directions) {
    addUniqueRow(rows, "Direction", direction.label);

    if (direction.movement && direction.movement !== direction.label) {
      addUniqueRow(rows, "Movement", direction.movement);
    }

    if (direction.route) {
      addUniqueRow(rows, "Route", direction.route);
    }
  }

  return rows;
}

function renderAnchorRows(rows: AnchorRow[]) {
  if (rows.length === 0) {
    return null;
  }

  return (
    <ul className="case-modal__anchor-list">
      {rows.map((row) => (
        <li key={`${row.label}:${row.value}`}>
          <strong>{row.label}:</strong> {row.value}
        </li>
      ))}
    </ul>
  );
}

function formatClockContext(journal: JournalDto) {
  return `Day ${journal.clock.day}, turn ${journal.clock.turn} in ${journal.currentTown.name}`;
}

function formatBounty(amount: number) {
  return currencyFormatter.format(amount);
}

function Section({
  title,
  subtitle,
  children,
  wide = false,
}: {
  title: string;
  subtitle?: string;
  children: ReactNode;
  wide?: boolean;
}) {
  return (
    <article className={`case-modal__section${wide ? " case-modal__section--wide" : ""}`}>
      <div className="case-modal__section-head">
        <div>
          <h3>{title}</h3>
          {subtitle ? <p className="panel-subtitle">{subtitle}</p> : null}
        </div>
      </div>
      {children}
    </article>
  );
}

function Card({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <article className="case-modal__card">
      <h4>{title}</h4>
      {children}
    </article>
  );
}

export function CaseFileSurface({ journal, loading, error }: CaseFileSurfaceProps) {
  const activeJournal = journal;

  const trailClues = useMemo(() => {
    if (!activeJournal) {
      return [];
    }

    const explicitTrailClues = activeJournal.caseFile.knownClues.filter((clue) => clue.kind === 4);
    if (explicitTrailClues.length > 0) {
      return explicitTrailClues;
    }

    return activeJournal.caseFile.knownClues.slice(0, 3);
  }, [activeJournal]);

  const contradictionClues = useMemo(() => {
    if (!activeJournal) {
      return [];
    }

    return activeJournal.caseFile.knownClues.filter((clue) => clue.kind === 9);
  }, [activeJournal]);

  if (loading && !activeJournal) {
    return <div className="case-modal__state">Loading the latest case file...</div>;
  }

  if (!loading && !activeJournal) {
    return <div className="case-modal__state">{error || "Load a game to inspect the case file."}</div>;
  }

  const caseJournal = activeJournal;

  if (!caseJournal) {
    return null;
  }

  return (
    <div className="case-modal__grid">
      <Section title="Overview" subtitle={`${formatClockContext(caseJournal)} - ${formatGameStatus(caseJournal.status)}`}>
        <p className="case-summary">{caseJournal.caseFile.caseSummary}</p>
        <p className="case-lead">
          <strong>Opening lead:</strong> {caseJournal.caseFile.openingLead}
        </p>
        <dl className="stat-list case-modal__stats">
          <div>
            <dt>Case state</dt>
            <dd>{caseJournal.caseFile.caseState.statusText}</dd>
          </div>
          <div>
            <dt>Town</dt>
            <dd>{caseJournal.currentTown.name}</dd>
          </div>
          <div>
            <dt>Time</dt>
            <dd>
              Day {caseJournal.clock.day}, turn {caseJournal.clock.turn}
            </dd>
          </div>
          <div>
            <dt>Status</dt>
            <dd>{formatGameStatus(caseJournal.status)}</dd>
          </div>
        </dl>
      </Section>

      <Section
        title="Culprit trail"
        subtitle={
          caseJournal.caseFile.knownClues.some((clue) => clue.kind === 4)
            ? "Clues tagged directly as culprit trail."
            : "No clue is explicitly tagged as culprit trail yet, so this board shows the strongest known leads."
        }
      >
        <div className="stack">
          {trailClues.length > 0 ? (
            trailClues.map((clue, index) => (
              <div key={`${clue.description}-${index}`} className="compact-item">
                <strong>{clue.description}</strong>
                <p>{formatClueKind(clue.kind)}</p>
                {clue.sourceLabel ? (
                  <p>
                    <strong>Source:</strong> {clue.sourceLabel}
                    {clue.context ? ` - ${clue.context}` : ""}
                  </p>
                ) : null}
              </div>
            ))
          ) : (
            <p className="muted">No clues have been recorded yet.</p>
          )}
        </div>
      </Section>

      <Section title="Discovered suspects" subtitle="Only suspects the player has already uncovered are shown.">
        <div className="stack">
          {caseJournal.caseFile.discoveredSuspects.length > 0 ? (
            caseJournal.caseFile.discoveredSuspects.map((suspect) => {
              return (
                <div key={suspect.id} className="compact-item">
                  <strong>{suspect.name}</strong>
                  <p>{formatSuspectStatus(suspect.status)}</p>
                  {suspect.leadSummaries.length > 0 ? (
                    <ul className="case-modal__lead-list">
                      {suspect.leadSummaries.map((leadSummary) => (
                        <li key={leadSummary}>{leadSummary}</li>
                      ))}
                    </ul>
                  ) : (
                    <p className="case-modal__minor">No known clues connect this suspect to the opening lead yet.</p>
                  )}
                </div>
              );
            })
          ) : (
            <p className="muted">No suspects have been discovered yet.</p>
          )}
        </div>
      </Section>

      <Section title="Warrants" subtitle="Known warrants and their safe terms.">
        <div className="stack">
          {caseJournal.caseFile.knownWarrants.length > 0 ? (
            caseJournal.caseFile.knownWarrants.map((warrant) => (
              <Card key={`${warrant.targetName}-${warrant.issuingSource}`} title={warrant.targetName}>
                <p>
                  <strong>Bounty:</strong> {formatBounty(warrant.bountyAmount)}
                </p>
                <p>
                  <strong>Disposition:</strong> {formatWarrantDisposition(warrant.disposition)}
                </p>
                <p>
                  <strong>Source:</strong> {warrant.issuingSource}
                </p>
                <p>{warrant.summary || "No warrant summary recorded."}</p>
              </Card>
            ))
          ) : (
            <p className="muted">No warrants have been logged yet.</p>
          )}
        </div>
      </Section>

      <Section title="Evidence stack" subtitle="All player-known clues with their safe anchors." wide>
        <div className="stack">
          {caseJournal.caseFile.knownClues.length > 0 ? (
            caseJournal.caseFile.knownClues.map((clue) => (
              <Card key={clue.id} title={clue.description}>
                <p>
                  <strong>Kind:</strong> {formatClueKind(clue.kind)}
                </p>
                {clue.sourceLabel ? (
                  <p>
                    <strong>Source:</strong> {clue.sourceLabel}
                    {clue.context ? ` - ${clue.context}` : ""}
                  </p>
                ) : null}
                {renderAnchorRows(formatSubjectRows(clue.anchors.subjects))}
                {renderAnchorRows(formatLocationRows(clue.anchors.locations))}
                {renderAnchorRows(formatTimeRows(clue.anchors.times))}
                {renderAnchorRows(formatDirectionRows(clue.anchors.directions))}
              </Card>
            ))
          ) : (
            <p className="muted">No clues recorded yet.</p>
          )}
        </div>
      </Section>

      <Section title="Deductions and contradictions" subtitle="A safe comparison board for known facts and unresolved links." wide>
        <div className="case-modal__deductions">
          <p>
            Compare the opening lead, the clue stack, and the discovered suspects. This board stays anchored to player-known facts and does not guess at hidden truth.
          </p>
          <dl className="stat-list case-modal__stats">
            <div>
              <dt>Clues</dt>
              <dd>{caseJournal.caseFile.knownClues.length}</dd>
            </div>
            <div>
              <dt>Suspects</dt>
              <dd>{caseJournal.caseFile.discoveredSuspects.length}</dd>
            </div>
            <div>
              <dt>Warrants</dt>
              <dd>{caseJournal.caseFile.knownWarrants.length}</dd>
            </div>
            <div>
              <dt>Contradictions</dt>
              <dd>{contradictionClues.length > 0 ? contradictionClues.length : "None recorded"}</dd>
            </div>
          </dl>
          {contradictionClues.length > 0 ? (
            <div className="stack">
              {contradictionClues.map((clue, index) => (
                <div key={`${clue.description}-${index}`} className="compact-item">
                  <strong>{clue.description}</strong>
                  <p>{clue.sourceLabel ? clue.sourceLabel : "Contradiction note"}</p>
                </div>
              ))}
            </div>
          ) : (
            <p className="case-modal__minor">No explicit contradictions have been logged yet.</p>
          )}
        </div>
      </Section>
    </div>
  );
}
