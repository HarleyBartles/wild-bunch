import { useMemo, type ReactNode } from "react";
import styled from "styled-components";
import type { ClueTimeAnchorDto, JournalDto } from "../api/types";
import {
  formatCaseIdentityKind,
  formatCaseIdentityStatus,
  formatClueKind,
  formatGameStatus,
  formatSuspectStatus,
  formatWarrantDisposition,
} from "../ui/formatters";
import { formatClockBeat, formatClueWhen } from "../ui/beatFormatters";
import { WantedPosterSurface } from "./WantedPosterSurface";
import {
  StatusCard,
  ItemCard,
  PanelSubtitle,
  StatList,
  Grid,
  Muted,
  Stack,
} from "./ui/sharedStyled";

const ModalState = styled.div`
  padding: 18px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
  color: var(--text);
`;

const AnchorList = styled.ul`
  margin: 10px 0 0;
  padding: 0;
  list-style: none;
  font-size: 0.88rem;

  li {
    margin-bottom: 4px;
    color: var(--text);
  }

  strong {
    color: var(--muted);
    font-weight: 600;
    margin-right: 4px;
  }
`;

const LeadList = styled.ul`
  margin: 12px 0 0;
  padding: 0;
  list-style: none;
  display: grid;
  gap: 8px;

  li {
    padding: 8px 12px;
    background: rgba(255, 255, 255, 0.02);
    border-radius: 8px;
    font-size: 0.84rem;
    border-left: 2px solid var(--accent);
  }
`;

const Minor = styled.span`
  display: block;
  font-size: 0.84rem;
  color: var(--muted);
`;

const SectionHead = styled.div`
  margin-bottom: 12px;

  h3 {
    margin: 0;
    font-size: 1.1rem;
    color: var(--text);
  }
`;

const Tag = styled.span`
  padding: 5px 9px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid var(--border);
  font-size: 0.76rem;
  font-weight: 600;
  color: var(--muted);
`;

const SectionCard = styled(StatusCard)<{ $wide?: boolean }>`
  grid-column: ${({ $wide }) => ($wide ? "1 / -1" : "auto")};
`;

const CardTitle = styled.h4`
  margin: 0 0 8px;
  font-size: 0.98rem;
`;

const CaseSummary = styled.p`
  margin: 0 0 12px;
  font-size: 0.94rem;
  line-height: 1.5;
`;

const OpeningLead = styled.p`
  margin: 0 0 16px;
  font-size: 0.88rem;
`;

const OverviewStatList = styled(StatList)`
  margin-bottom: 12px;
`;

const ClueMeta = styled.p<{ $spaced?: boolean }>`
  margin: ${({ $spaced }) => ($spaced ? "4px 0 0" : "0")};
  font-size: 0.84rem;
  color: var(--muted);
`;

const SpacedMinor = styled(Minor)`
  margin-top: 8px;
`;

const RecordTitle = styled.h4<{ $large?: boolean }>`
  margin: 0;
  font-size: ${({ $large }) => ($large ? "1rem" : "0.94rem")};
`;

const WarrantFactsBox = styled.div`
  margin-top: 10px;
  font-size: 0.84rem;
`;

const EvidenceKind = styled.p`
  margin: 6px 0 0;
  font-size: 0.88rem;
`;

const SuspectSection = styled.div`
  margin-top: 20px;
`;

const SuspectHeader = styled.div`
  display: flex;
  justify-content: space-between;
  gap: 8px;
`;

const DeductionsGrid = styled.div`
  display: grid;
  gap: 20px;
`;

const DeductionsIntro = styled.p`
  margin: 0;
  font-size: 0.94rem;
  color: var(--muted);
`;

const WarrantText = styled.p<{ $spaced?: boolean }>`
  margin-top: ${({ $spaced }) => ($spaced ? "8px" : "0")};
  font-size: 0.9rem;
`;

const FineLine = styled.p`
  font-size: 0.88rem;
`;

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

const capturedCaseIdentityStatus = 3;

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
  times: ClueTimeAnchorDto[];
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
    addUniqueRow(rows, seenValues, "When", formatClueWhen(time));
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
    <AnchorList>
      {rows.map((row) => (
        <li key={`${row.label}:${row.value}`}>
          <strong>{row.label}:</strong> {row.value}
        </li>
      ))}
    </AnchorList>
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
  return `${formatClockBeat(journal.clock)} in ${journal.currentTown.name}`;
}

function formatBounty(amount: number) {
  return currencyFormatter.format(amount);
}

function normalizeText(text: string) {
  return text.trim().replace(/\s+/g, " ").toLowerCase();
}

function formatDiscoveryBasis(basisSourceLabel: string | null | undefined) {
  const lower = normalizeText(basisSourceLabel ?? "");
  if (
    lower.includes("notice board") ||
    lower.includes("wanted poster") ||
    lower.includes("wanted notice")
  ) {
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
    const subjectMatches = candidate.anchors.subjects.some(
      (subject) => normalizeText(subject.label) === normalizedSuspectName,
    );
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
    <SectionCard as="article" $wide={wide}>
      <SectionHead>
        <h3>{title}</h3>
        {subtitle ? <PanelSubtitle>{subtitle}</PanelSubtitle> : null}
      </SectionHead>
      {children}
    </SectionCard>
  );
}

function Card({ title, children }: { title: string; children: ReactNode }) {
  return (
    <ItemCard as="article">
      <CardTitle>{title}</CardTitle>
      {children}
    </ItemCard>
  );
}

export function CaseFileSurface({ journal, loading, error }: CaseFileSurfaceProps) {
  const activeJournal = journal;

  const capturedWantedTargetNames = useMemo(() => {
    if (!activeJournal) {
      return new Set<string>();
    }

    return new Set(
      activeJournal.caseFile.caseBoard.namedRecords
        .filter((record) => record.status === capturedCaseIdentityStatus)
        .map((record) => normalizeText(record.displayName)),
    );
  }, [activeJournal]);

  const visibleKnownClues = useMemo(() => {
    if (!activeJournal) {
      return [];
    }

    const activeEvidenceIds = new Set(
      activeJournal.caseFile.caseBoard.evidenceItems.map((evidence) => evidence.id),
    );
    return activeJournal.caseFile.knownClues.filter((clue) => activeEvidenceIds.has(clue.id));
  }, [activeJournal]);

  const activeKnownWarrants = useMemo(() => {
    if (!activeJournal) {
      return [];
    }

    return activeJournal.caseFile.knownWarrants.filter(
      (warrant) => !capturedWantedTargetNames.has(normalizeText(warrant.targetName)),
    );
  }, [activeJournal, capturedWantedTargetNames]);

  const activeWantedPosters = useMemo(() => {
    if (!activeJournal) {
      return [];
    }

    return activeJournal.caseFile.wantedPosters.filter(
      (poster) => !capturedWantedTargetNames.has(normalizeText(poster.targetDisplayName)),
    );
  }, [activeJournal, capturedWantedTargetNames]);

  const trailClues = useMemo(() => {
    const explicitTrailClues = visibleKnownClues.filter((clue) => clue.kind === 4);
    if (explicitTrailClues.length > 0) {
      return explicitTrailClues;
    }

    return visibleKnownClues.slice(0, 3);
  }, [visibleKnownClues]);

  const contradictionClues = useMemo(() => {
    return visibleKnownClues.filter((clue) => clue.kind === 9);
  }, [visibleKnownClues]);

  const visibleDiscoveredSuspects = useMemo<DiscoveredSuspectCard[]>(() => {
    if (!activeJournal) {
      return [];
    }

    return activeJournal.caseFile.discoveredSuspects
      .map((suspect) => {
        const suspectName = suspect.name;
        const basis = findSuspectDiscoveryBasis(activeJournal, suspectName) || "Player observation";
        const record = activeJournal.caseFile.caseBoard.namedRecords.find(
          (r) => normalizeText(r.displayName) === normalizeText(suspectName),
        );

        return suspectName
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
    return <ModalState>Loading the latest case file...</ModalState>;
  }

  if (!loading && !activeJournal) {
    return <ModalState>{error || "Load a game to inspect the case file."}</ModalState>;
  }

  const caseJournal = activeJournal;

  if (!caseJournal) {
    return null;
  }

  return (
    <Grid $cols={3} $tabletCols={2} $mobileCols={1}>
      <Section
        title="Overview"
        subtitle={`${formatClockContext(caseJournal)} - ${formatGameStatus(caseJournal.status)}`}
      >
        <CaseSummary>
          {caseJournal.caseFile.caseSummary}
        </CaseSummary>
        <OpeningLead>
          <strong>Opening lead:</strong> {caseJournal.caseFile.openingLead}
        </OpeningLead>
        <OverviewStatList>
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
              {formatClockBeat(caseJournal.clock)}
            </dd>
          </div>
          <div>
            <dt>Status</dt>
            <dd>{formatGameStatus(caseJournal.status)}</dd>
          </div>
        </OverviewStatList>
      </Section>

      <Section
        title="Culprit trail"
        subtitle={
          visibleKnownClues.some((clue) => clue.kind === 4)
            ? "Clues tagged directly as culprit trail."
            : "No clue is explicitly tagged as culprit trail yet, so this board shows the strongest known leads."
        }
      >
        <Stack>
          {trailClues.length > 0 ? (
            trailClues.map((clue, index) => (
              <ItemCard key={`${clue.description}-${index}`}>
                <strong>{clue.description}</strong>
                <ClueMeta $spaced>
                  {formatClueKind(clue.kind)}
                </ClueMeta>
                {clue.sourceLabel ? (
                  <SpacedMinor>
                    <strong>Source:</strong> {clue.sourceLabel}
                    {clue.context ? ` - ${clue.context}` : ""}
                  </SpacedMinor>
                ) : null}
              </ItemCard>
            ))
          ) : (
            <Muted>No clues have been recorded yet.</Muted>
          )}
        </Stack>
      </Section>

      <Section title="Identity board" subtitle="Player-known identity threads, loose leads, and earned links." wide>
        <Grid $cols={3} $tabletCols={2} $mobileCols={1}>
          <Stack>
            <SectionHead>
              <h3>Named records</h3>
              <PanelSubtitle>
                Wanted posters and other named records the player has earned.
              </PanelSubtitle>
            </SectionHead>
            {caseJournal.caseFile.caseBoard.namedRecords.length > 0 ? (
              caseJournal.caseFile.caseBoard.namedRecords.map((record) => (
                <ItemCard key={record.id}>
                  <RecordTitle $large>{record.displayName}</RecordTitle>
                  <Minor>Type: {formatCaseIdentityKind(record.kind)}</Minor>
                  <Minor>Status: {formatCaseIdentityStatus(record.status)}</Minor>
                  {record.resolvedToDisplayName ? (
                    <Minor>Resolved to: {record.resolvedToDisplayName}</Minor>
                  ) : (
                    <Minor>No named record links this lead yet.</Minor>
                  )}
                  {record.summaryLines.length > 0 ? (
                    <LeadList>
                      {record.summaryLines.map((line) => (
                        <li key={line}>{line}</li>
                      ))}
                    </LeadList>
                  ) : null}
                  <WarrantFactsBox>
                    {renderWarrantFacts(record)}
                  </WarrantFactsBox>
                </ItemCard>
              ))
            ) : (
              <Muted>No named records have been earned yet.</Muted>
            )}
          </Stack>

          <Stack>
            <SectionHead>
              <h3>Loose leads</h3>
              <PanelSubtitle>
                Identity-bearing leads that have not resolved into a named record yet.
              </PanelSubtitle>
            </SectionHead>
            {caseJournal.caseFile.caseBoard.looseLeads.length > 0 ? (
              caseJournal.caseFile.caseBoard.looseLeads.map((lead) => (
                <ItemCard key={lead.id}>
                  <RecordTitle>{lead.displayName}</RecordTitle>
                  <Minor>Type: {formatCaseIdentityKind(lead.kind)}</Minor>
                  <Minor>Status: {formatCaseIdentityStatus(lead.status)}</Minor>
                  {lead.resolvedToDisplayName ? (
                    <Minor>Resolved to: {lead.resolvedToDisplayName}</Minor>
                  ) : (
                    <Minor>No named record links this lead yet.</Minor>
                  )}
                  {lead.summaryLines.length > 0 ? (
                    <LeadList>
                      {lead.summaryLines.map((line) => (
                        <li key={line}>{line}</li>
                      ))}
                    </LeadList>
                  ) : null}
                </ItemCard>
              ))
            ) : (
              <Muted>No loose leads have been logged yet.</Muted>
            )}
          </Stack>

          <Stack>
            <SectionHead>
              <h3>Evidence items</h3>
              <PanelSubtitle>Material evidence collected and linked to the case board.</PanelSubtitle>
            </SectionHead>
            {caseJournal.caseFile.caseBoard.evidenceItems.length > 0 ? (
              caseJournal.caseFile.caseBoard.evidenceItems.map((evidence) => (
                <ItemCard key={evidence.id}>
                  <RecordTitle>{evidence.summary}</RecordTitle>
                  <EvidenceKind>{evidence.kindLabel}</EvidenceKind>
                  <SpacedMinor>Source: {evidence.sourceLabel}</SpacedMinor>
                </ItemCard>
              ))
            ) : (
              <Muted>No evidence items have been linked yet.</Muted>
            )}
          </Stack>
        </Grid>

        <SuspectSection>
          <SectionHead>
            <h3>Discovered suspects</h3>
            <PanelSubtitle>Only suspects the player has already uncovered are shown.</PanelSubtitle>
          </SectionHead>
          <Grid $cols={2} $mobileCols={1}>
            {visibleDiscoveredSuspects.length > 0 ? (
              visibleDiscoveredSuspects.map(({ suspect, basis, record }) => (
                <ItemCard key={suspect.name}>
                  <SuspectHeader>
                    <RecordTitle $large>{suspect.name}</RecordTitle>
                    <Tag>{formatSuspectStatus(suspect.status)}</Tag>
                  </SuspectHeader>
                  <Minor>{basis}</Minor>
                  {record ? (
                    <>
                      {record.summaryLines.length > 0 ? (
                        <LeadList>
                          {record.summaryLines.map((line) => (
                            <li key={line}>{line}</li>
                          ))}
                        </LeadList>
                      ) : null}
                      <WarrantFactsBox>
                        {renderWarrantFacts(record)}
                      </WarrantFactsBox>
                    </>
                  ) : null}
                </ItemCard>
              ))
            ) : (
              <Muted>No suspects have been confirmed from player-known evidence yet.</Muted>
            )}
          </Grid>
        </SuspectSection>
      </Section>

      <Section title="Warrants" subtitle="Known warrants and their safe terms.">
        <Stack>
          {activeKnownWarrants.length > 0 ? (
            activeKnownWarrants.map((warrant) => (
              <Card key={`${warrant.targetName}-${warrant.issuingSource}`} title={warrant.targetName}>
                <WarrantText>
                  <strong>Bounty:</strong> {formatBounty(warrant.bountyAmount)}
                </WarrantText>
                <FineLine>
                  <strong>Disposition:</strong> {formatWarrantDisposition(warrant.disposition)}
                </FineLine>
                <FineLine>
                  <strong>Source:</strong> {warrant.issuingSource}
                </FineLine>
                <WarrantText $spaced>
                  {warrant.summary || "No warrant summary recorded."}
                </WarrantText>
              </Card>
            ))
          ) : (
            <Muted>No warrants have been logged yet.</Muted>
          )}
        </Stack>
      </Section>

      <Section title="Evidence stack" subtitle="All player-known clues with their safe anchors." wide>
        <Stack>
          {caseJournal.caseFile.caseBoard.evidenceItems.length > 0 ? (
            caseJournal.caseFile.caseBoard.evidenceItems.map((evidence) => (
              <Card key={evidence.id} title={evidence.summary}>
                <FineLine>
                  <strong>Kind:</strong> {evidence.kindLabel}
                </FineLine>
                <FineLine>
                  <strong>Source:</strong> {evidence.sourceLabel}
                </FineLine>
                {evidence.identityBearing ? (
                  <Minor>Identity-bearing evidence</Minor>
                ) : (
                  <Minor>Color-only observation</Minor>
                )}
                {renderAnchorRows(buildAnchorRows(evidence.anchors))}
              </Card>
            ))
          ) : (
            <Muted>No clues recorded yet.</Muted>
          )}
        </Stack>
      </Section>

      <WantedPosterSurface wantedPosters={activeWantedPosters} />

      <Section
        title="Deductions and contradictions"
        subtitle="A safe comparison board for known facts and unresolved links."
        wide
      >
        <DeductionsGrid>
          <DeductionsIntro>
            Compare the opening lead, the clue stack, and the discovered suspects. This board stays
            anchored to player-known facts and does not guess at hidden truth.
          </DeductionsIntro>
          <StatList>
            <div>
              <dt>Clues</dt>
              <dd>{visibleKnownClues.length}</dd>
            </div>
            <div>
              <dt>Suspects</dt>
              <dd>{visibleDiscoveredSuspects.length}</dd>
            </div>
            <div>
              <dt>Warrants</dt>
              <dd>{activeKnownWarrants.length}</dd>
            </div>
            <div>
              <dt>Contradictions</dt>
              <dd>{contradictionClues.length > 0 ? contradictionClues.length : "None recorded"}</dd>
            </div>
          </StatList>
          {contradictionClues.length > 0 ? (
            <Stack>
              {contradictionClues.map((clue, index) => (
                <ItemCard key={`${clue.description}-${index}`}>
                  <strong>{clue.description}</strong>
                  <ClueMeta>
                    {clue.sourceLabel ? clue.sourceLabel : "Contradiction note"}
                  </ClueMeta>
                </ItemCard>
              ))}
            </Stack>
          ) : (
            <Minor>No explicit contradictions have been logged yet.</Minor>
          )}
        </DeductionsGrid>
      </Section>
    </Grid>
  );
}
