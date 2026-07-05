import styled from "styled-components";
import { useNavigate } from "@tanstack/react-router";
import { useGameSession } from "../../state/useGameSession";
import { WantedPosterSurface } from "../../components/WantedPosterSurface";
import { InvestigationSourceKind } from "../../api/types";
import type { ClueDto } from "../../api/types";
import {
  FlowSurface,
  FlowNotice,
  FlowError,
  BackButton,
  Panel,
  PanelHead,
  Stack,
  Button,
  Muted,
  ItemCard,
} from "../../components/ui/sharedStyled";

const PlaceHeader = styled.header`
  display: grid;
  gap: 12px;
  padding: 24px 0 4px;

  h1 {
    margin: 0;
  }
`;

const PlaceBody = styled.div`
  display: grid;
  gap: 20px;
`;

const LeadMeta = styled.p`
  margin: 4px 0 0;
  font-size: 0.85rem;
  color: var(--muted);
`;

export function SheriffPlace() {
  const navigate = useNavigate();
  const {
    session,
    journal,
    wantedPosters,
    loading,
    busyMode,
    canReadWantedPosters,
    canCheckLocalRecords,
    handleReadWantedPosters,
    handleCheckLocalRecords,
    notice,
    error,
  } = useGameSession();

  if (!session) {
    return null;
  }

  const sheriffLeads: ClueDto[] = (journal?.caseFile.knownClues ?? []).filter(
    (clue) => clue.sourceKind === InvestigationSourceKind.LocalRecords,
  );

  return (
    <FlowSurface $variant="place">
      <PlaceHeader>
        <BackButton type="button" onClick={() => void navigate({ to: "/town" })}>
          ← Back to town
        </BackButton>
        <h1>Sheriff Office</h1>
      </PlaceHeader>
      <PlaceBody>
        <Panel>
          <PanelHead>
            <h2>Wanted posters</h2>
          </PanelHead>
          <Stack>
            <Button
              type="button"
              onClick={handleReadWantedPosters}
              disabled={loading || !canReadWantedPosters}
            >
              {busyMode === "reading" ? "Reading..." : "Read wanted posters"}
            </Button>
            {wantedPosters.length > 0 ? (
              <WantedPosterSurface wantedPosters={wantedPosters} />
            ) : (
              <Muted>No wanted posters read yet.</Muted>
            )}
          </Stack>
        </Panel>
        <Panel>
          <PanelHead>
            <h2>Local records</h2>
          </PanelHead>
          <Stack>
            <Button
              type="button"
              onClick={handleCheckLocalRecords}
              disabled={loading || !canCheckLocalRecords}
            >
              {busyMode === "investigating" ? "Checking..." : "Check local records"}
            </Button>
            {sheriffLeads.length > 0 ? (
              <Stack>
                {sheriffLeads.map((clue) => (
                  <ItemCard key={clue.id}>
                    <strong>{clue.description}</strong>
                    {clue.sourceLabel ? (
                      <LeadMeta>Source: {clue.sourceLabel}</LeadMeta>
                    ) : null}
                  </ItemCard>
                ))}
              </Stack>
            ) : (
              <Muted>No leads from local records yet.</Muted>
            )}
          </Stack>
        </Panel>
        {notice ? <FlowNotice>{notice}</FlowNotice> : null}
        {error ? <FlowError>{error}</FlowError> : null}
      </PlaceBody>
    </FlowSurface>
  );
}
