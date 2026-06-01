import { useMemo, type ReactNode } from "react";
import type { JournalDto } from "../api/types";
import {
  formatCaseIdentityKind,
  formatCaseIdentityStatus,
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

type DiscoveredSuspectCard = {
  suspect: JournalDto["caseFile"]["discoveredSuspects"][number];
  basis: string;
  record: JournalDto["caseFile"]["caseBoard"]["namedRecords"][number] | undefined;
};

function cleanText(text: string) {
  return text.trim().replace(/\s+/g, " ").replace(/[.?!]+$/u, "");
}

function addUniqueRow(rows: AnchorRow[], seenValues: Set<string>, label: string, value: string) {
  const cleanedValue = cleanText(value);
  if (!cleanedValue) {
    return;
  }

  const normalizedValue = cleanedValue.toLowerCase();
  if (seenValues.has(normalizedValue)) {
    return;
  }

  seenValues.add(normalizedValue);
  rows.push({ label, value: cleanedValue });
}

function buildAnchorRows(anchors: {
  subjects: { label: string; alias: string | null; feature: string | null; fact: string | null }[];
  locations: { label: string; place: string | null; route: string | null }[];
  times: { recency: number; day: number | null; turn: number | null }[];
  directions: { label: string; movement: string | null; route: string | null }[];
}) {
  const rows: AnchorRow[] = [];
  const seenValues = new Set<string>();

  for (const subject of anchors.subjects) {
    addUniqueRow(rows, seenValues, "Subject", subject.label);

    if (subject.alias && subject.alias !== subject.label) {
      addUniqueRow(rows, seenValues, "Alias", subject.alias);
    }

    if (subject.feature) {
      addUniqueRow(rows, seenValues, "Feature", cleanText(subject.feature));
    }

    if (subject.fact) {
      addUniqueRow(rows, seenValues, "Fact", cleanText(subject.fact));
    }
  }

  for (const location of anchors.locations) {
    addUniqueRow(rows, seenValues, "Location", location.label);

    if (location.place && location.place !== location.label) {
      addUniqueRow(rows, seenValues, "Place", location.place);
    }

    if (location.route) {
      addUniqueRow(rows, seenValues, "Route", location.route);
    }
  }

  for (const time of anchors.times) {
    const parts = [formatClueRecency(time.recency)];
    if (time.day !== null) {
      parts.push(`day ${time.day}`);
    }
    if (time.turn !== null) {
      parts.push(`turn ${time.turn}`);
    }

    addUniqueRow(rows, seenValues, "When", parts.join(", "));
  }

  for (const direction of anchors.directions) {
    addUniqueRow(rows, seenValues, "Direction", direction.label);

    if (direction.movement && direction.movement !== direction.label) {
      addUniqueRow(rows, seenValues, "Movement", direction.movement);
    }

    if (direction.route) {
      addUniqueRow(rows, seenValues, "Route", direction.route);
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

function renderListField(label: string, values: string[]) {
  if (values.length === 0) {
    return null;
  }

  return (
    <p>
      <strong>{label}:</strong> {values.join(", ")}
    </p>
  );
}

function renderWarrantFacts(record: {
  knownAliases: string[];
  distinguishingFeatures: string[];
  warrantDisposition: number | null;
  bountyAmount: number | null;
  issuingAuthority: string | null;
  crimeSummary: string | null;
}) {
  return (
    <>
      {renderListField("Known aliases", record.knownAliases)}
      {renderListField("Distinguishing features", record.distinguishingFeatures)}
      {record.warrantDisposition !== null ? (
        <p>
          <strong>Disposition:</strong> {formatWarrantDisposition(record.warrantDisposition)}
        </p>
      ) : null}
      {record.bountyAmount !== null ? (
        <p>
          <strong>Bounty:</strong> {formatBounty(record.bountyAmount)}
        </p>
      ) : null}
      {record.issuingAuthority ? (
        <p>
          <strong>Issuing authority:</strong> {record.issuingAuthority}
        </p>
      ) : null}
      {record.crimeSummary ? (
        <p>
          <strong>Crime summary:</strong> {record.crimeSummary}
        </p>
      ) : null}
    </>
  );
}

function formatClockContext(journal: JournalDto) {
  return `Day ${journal.clock.day}, turn ${journal.clock.turn} in ${journal.currentTown.name}`;
}

function formatBounty(amount: number) {
  return currencyFormatter.format(amount);
}

function normalizeText(text: string) {
  return text.trim().replace(/\s+/g, " ").toLowerCase();
}

function formatDiscoveryBasis(basisSourceLabel: string | null | undefined) {
  const lower = normalizeText(basisSourceLabel ?? "");
  if (lower.includes("notice board") || lower.includes("wanted poster") || lower.includes("wanted notice")) {
    return "Discovered from wanted poster";
  }

  if (lower.includes("sheriff")) {
    return "Named in sheriff record";
  }

  if (lower.includes("telegraph")) {
    return "Named in telegraph lead";
  }

  if (lower.includes("gossip") || lower.includes("saloon talk")) {
    return "Mentioned in local gossip";
  }

  return basisSourceLabel?.trim() || "Player-known evidence";
}

function findSuspectDiscoveryBasis(journal: JournalDto, suspectName: string) {
  const normalizedSuspectName = normalizeText(suspectName);

  const matchingRecord = journal.caseFile.caseBoard.namedRecords.find(
    (record) => normalizeText(record.displayName) === normalizedSuspectName,
  );
  if (matchingRecord) {
    return formatDiscoveryBasis("wanted poster");
  }

  const clue = journal.caseFile.knownClues.find((candidate) => {
    const descriptionMatches = normalizeText(candidate.description).includes(normalizedSuspectName);
    const subjectMatches = candidate.anchors.subjects.some((subject) => normalizeText(subject.label) === normalizedSuspectName);
    return descriptionMatches || subjectMatches;
  });

  if (!clue) {
    return null;
  }

  return formatDiscoveryBasis(clue.sourceLabel ?? clue.context);
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

  const visibleDiscoveredSuspects = useMemo<DiscoveredSuspectCard[]>(() => {
    if (!activeJournal) {
      return [];
    }

    return activeJournal.caseFile.discoveredSuspects
      .map((suspect) => {
        const basis = findSuspectDiscoveryBasis(activeJournal, suspect.name);
        const record = activeJournal.caseFile.caseBoard.namedRecords.find(
          (entry) => normalizeText(entry.displayName) === normalizeText(suspect.name),
        );

        return basis
          ? {
              suspect,
              basis,
              record,
            }
          : null;
      })
      .filter((entry): entry is DiscoveredSuspectCard => entry !== null);
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

      <Section title="Identity board" subtitle="Player-known identity threads, loose leads, and earned links." wide>
        <div className="case-modal__identity-grid">
          <div className="stack">
            <div className="case-modal__section-head">
              <div>
                <h3>Named records</h3>
                <p className="panel-subtitle">Wanted posters and other named records the player has earned.</p>
              </div>
            </div>
            {caseJournal.caseFile.caseBoard.namedRecords.length > 0 ? (
              caseJournal.caseFile.caseBoard.namedRecords.map((record) => (
                <article key={record.id} className="case-modal__card">
                  <h4>{record.displayName}</h4>
                  <p>
                    <strong>Type:</strong> {formatCaseIdentityKind(record.kind)}
                  </p>
                  <p>
                    <strong>Status:</strong> {formatCaseIdentityStatus(record.status)}
                  </p>
                  {record.resolvedToDisplayName ? (
                    <p>
                      <strong>Resolved to:</strong> {record.resolvedToDisplayName}
                    </p>
                  ) : null}
                  {record.summaryLines.length > 0 ? (
                    <ul className="case-modal__lead-list">
                      {record.summaryLines.map((line) => (
                        <li key={line}>{line}</li>
                      ))}
                    </ul>
                  ) : null}
                  {renderWarrantFacts(record)}
                </article>
              ))
            ) : (
              <p className="muted">No named records have been earned yet.</p>
            )}
          </div>

          <div className="stack">
            <div className="case-modal__section-head">
              <div>
                <h3>Loose leads</h3>
                <p className="panel-subtitle">Identity-bearing leads that have not resolved into a named record yet.</p>
              </div>
            </div>
            {caseJournal.caseFile.caseBoard.looseLeads.length > 0 ? (
              caseJournal.caseFile.caseBoard.looseLeads.map((lead) => (
                <article key={lead.id} className="case-modal__card">
                  <h4>{lead.displayName}</h4>
                  <p>
                    <strong>Type:</strong> {formatCaseIdentityKind(lead.kind)}
                  </p>
                  <p>
                    <strong>Status:</strong> {formatCaseIdentityStatus(lead.status)}
                  </p>
                  {lead.resolvedToDisplayName ? (
                    <p>
                      <strong>Resolved to:</strong> {lead.resolvedToDisplayName}
                    </p>
                  ) : (
                    <p className="case-modal__minor">No named record links this lead yet.</p>
                  )}
                  {lead.summaryLines.length > 0 ? (
                    <ul className="case-modal__lead-list">
                      {lead.summaryLines.map((line) => (
                        <li key={line}>{line}</li>
                      ))}
                    </ul>
                  ) : null}
                </article>
              ))
            ) : (
              <p className="muted">No loose leads have been logged yet.</p>
            )}
          </div>
        </div>

        <div className="case-modal__identity-suspects">
          <div className="case-modal__section-head">
            <div>
              <h3>Discovered suspects</h3>
              <p className="panel-subtitle">Only suspects the player has already uncovered are shown.</p>
            </div>
          </div>
          <div className="stack">
            {visibleDiscoveredSuspects.length > 0 ? (
              visibleDiscoveredSuspects.map(({ suspect, basis, record }) => (
                  <div key={suspect.name} className="compact-item">
                    <strong>{suspect.name}</strong>
                    <p>{formatSuspectStatus(suspect.status)}</p>
                    <p className="case-modal__minor">{basis}</p>
                    {record ? (
                      <>
                        {record.summaryLines.length > 0 ? (
                          <ul className="case-modal__lead-list">
                            {record.summaryLines.map((line) => (
                              <li key={line}>{line}</li>
                            ))}
                          </ul>
                        ) : null}
                        {renderWarrantFacts(record)}
                      </>
                    ) : null}
                  </div>
                ))
            ) : (
              <p className="muted">No suspects have been confirmed from player-known evidence yet.</p>
            )}
          </div>
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
          {caseJournal.caseFile.caseBoard.evidenceItems.length > 0 ? (
            caseJournal.caseFile.caseBoard.evidenceItems.map((evidence) => (
              <Card key={evidence.id} title={evidence.summary}>
                <p>
                  <strong>Kind:</strong> {evidence.kindLabel}
                </p>
                <p>
                  <strong>Source:</strong> {evidence.sourceLabel}
                </p>
                {evidence.identityBearing ? <p className="case-modal__minor">Identity-bearing evidence</p> : <p className="case-modal__minor">Color-only observation</p>}
                {renderAnchorRows(buildAnchorRows(evidence.anchors))}
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
              <dd>{visibleDiscoveredSuspects.length}</dd>
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
