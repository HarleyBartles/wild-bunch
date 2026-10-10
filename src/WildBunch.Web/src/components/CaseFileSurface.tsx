import styled from "styled-components";
import type { ClueTimeAnchorDto, JournalDto } from "../api/types";
import { formatClueKind, formatSuspectStatus, formatWarrantDisposition } from "../ui/formatters";
import { formatClueWhen } from "../ui/beatFormatters";
import { WantedPosterSurface } from "./WantedPosterSurface";
import { Grid, ItemCard, Muted, PanelSubtitle, Stack, StatusCard } from "./ui/sharedStyled";

const ModalState = styled.div`
  padding: 18px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
  color: var(--text);
`;

const Section = styled(StatusCard)`
  grid-column: 1 / -1;
`;

const SectionHead = styled.div`
  margin-bottom: 12px;

  h3 {
    margin: 0;
    font-size: 1.1rem;
    color: var(--text);
  }
`;

const RecordTitle = styled.h4`
  margin: 0 0 8px;
  font-size: 1rem;
`;

const Fact = styled.p`
  margin: 8px 0 0;
  font-size: 0.9rem;
  line-height: 1.45;
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

const Minor = styled.span`
  display: block;
  margin-top: 6px;
  font-size: 0.84rem;
  color: var(--muted);
`;

const SuspectLine = styled.div`
  display: flex;
  justify-content: space-between;
  gap: 8px;
  align-items: baseline;
`;

const currencyFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

interface CaseFileSurfaceProps {
  journal: JournalDto | null;
  loading: boolean;
  error: string;
}

type AnchorRow = { label: string; value: string };

function buildAnchorRows(anchors: {
  subjects: { label: string; alias: string | null; feature: string | null; fact: string | null }[];
  locations: { label: string; place: string | null; route: string | null }[];
  times: ClueTimeAnchorDto[];
  directions: { label: string; movement: string | null; route: string | null }[];
}): AnchorRow[] {
  const rows: AnchorRow[] = [];
  const add = (label: string, value: string | null | undefined) => {
    const text = value?.trim();
    if (text) rows.push({ label, value: text });
  };

  for (const subject of anchors.subjects) {
    add("Subject", subject.label);
    if (subject.alias && subject.alias !== subject.label) add("Alias", subject.alias);
    add("Feature", subject.feature);
    add("Fact", subject.fact);
  }
  for (const location of anchors.locations) {
    add("Location", location.label);
    if (location.place && location.place !== location.label) add("Place", location.place);
    add("Route", location.route);
  }
  for (const time of anchors.times) add("When", formatClueWhen(time));
  for (const direction of anchors.directions) {
    add("Direction", direction.label);
    if (direction.movement && direction.movement !== direction.label)
      add("Movement", direction.movement);
    add("Route", direction.route);
  }
  return rows;
}

function renderAnchors(rows: AnchorRow[]) {
  if (rows.length === 0) return null;
  return (
    <AnchorList>
      {rows.map((row, index) => (
        <li key={`${row.label}:${row.value}:${index}`}>
          <strong>{row.label}:</strong> {row.value}
        </li>
      ))}
    </AnchorList>
  );
}

export function CaseFileSurface({ journal, loading, error }: CaseFileSurfaceProps) {
  if (loading && !journal) return <ModalState>Loading the latest case file...</ModalState>;
  if (!loading && !journal)
    return <ModalState>{error || "Load a game to inspect the case file."}</ModalState>;
  if (!journal) return null;

  const settledWarrantIds = new Set(
    journal.caseFile.caseBoard.warrants
      .filter((warrant) => warrant.settlement)
      .map((warrant) => warrant.id),
  );
  const activeWantedPosters = journal.caseFile.wantedPosters.filter(
    (poster) => !settledWarrantIds.has(poster.posterId),
  );

  return (
    <Grid $cols={2} $tabletCols={2} $mobileCols={1}>
      <Section as="article">
        <SectionHead>
          <PanelSubtitle>
            {journal.currentTown.name}, day {journal.clock.day}
          </PanelSubtitle>
        </SectionHead>
        <Fact>{journal.caseFile.caseSummary}</Fact>
        <Fact>
          <strong>Opening lead:</strong> {journal.caseFile.openingLead}
        </Fact>
        <Minor>{journal.caseFile.caseState.statusText}</Minor>
      </Section>

      <Section as="article">
        <SectionHead>
          <h3>Warrants</h3>
        </SectionHead>
        <Stack>
          {journal.caseFile.caseBoard.warrants.length > 0 ? (
            journal.caseFile.caseBoard.warrants.map((warrant) => (
              <ItemCard as="article" key={warrant.id}>
                <RecordTitle>{warrant.targetName}</RecordTitle>
                <Fact>{warrant.summary}</Fact>
                <Minor>Issued by {warrant.issuingSource}</Minor>
                <Fact>
                  <strong>Bounty:</strong> {currencyFormatter.format(warrant.bountyAmount)}
                </Fact>
                <Fact>
                  <strong>Disposition:</strong> {formatWarrantDisposition(warrant.disposition)}
                </Fact>
                {warrant.knownAliases.length > 0 ? (
                  <Fact>
                    <strong>Known aliases:</strong> {warrant.knownAliases.join(", ")}
                  </Fact>
                ) : null}
                {warrant.knownFeatures.length > 0 ? (
                  <Fact>
                    <strong>Known features:</strong> {warrant.knownFeatures.join("; ")}
                  </Fact>
                ) : null}
                {warrant.settlement ? (
                  <Minor>
                    Turned in {warrant.settlement.isAlive ? "alive" : "dead"} on day{" "}
                    {warrant.settlement.day}; bounty paid{" "}
                    {currencyFormatter.format(warrant.settlement.bountyAmount)}.
                  </Minor>
                ) : null}
              </ItemCard>
            ))
          ) : (
            <Muted>No warrants have been recorded.</Muted>
          )}
        </Stack>
      </Section>

      <Section as="article">
        <SectionHead>
          <h3>Clues</h3>
        </SectionHead>
        <Stack>
          {journal.caseFile.caseBoard.clues.length > 0 ? (
            journal.caseFile.caseBoard.clues.map((clue) => (
              <ItemCard as="article" key={clue.id}>
                <RecordTitle>{clue.description}</RecordTitle>
                <Minor>{formatClueKind(clue.kind)}</Minor>
                {clue.source ? (
                  <Fact>
                    <strong>Source:</strong> {clue.source}
                  </Fact>
                ) : null}
                {clue.context ? (
                  <Fact>
                    <strong>Context:</strong> {clue.context}
                  </Fact>
                ) : null}
                {renderAnchors(buildAnchorRows(clue.anchors))}
              </ItemCard>
            ))
          ) : (
            <Muted>No clues have been recorded.</Muted>
          )}
        </Stack>
      </Section>

      <Section as="article">
        <SectionHead>
          <h3>Known suspects</h3>
        </SectionHead>
        <Stack>
          {journal.caseFile.discoveredSuspects.length > 0 ? (
            journal.caseFile.discoveredSuspects.map((suspect, index) => (
              <ItemCard as="article" key={`${suspect.name}:${index}`}>
                <SuspectLine>
                  <RecordTitle>{suspect.name}</RecordTitle>
                  <Minor>{formatSuspectStatus(suspect.status)}</Minor>
                </SuspectLine>
              </ItemCard>
            ))
          ) : (
            <Muted>No names have been recorded.</Muted>
          )}
        </Stack>
      </Section>

      <Section as="article">
        <SectionHead>
          <h3>Wanted posters</h3>
        </SectionHead>
        <WantedPosterSurface wantedPosters={activeWantedPosters} />
      </Section>
    </Grid>
  );
}
