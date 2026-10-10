import styled from "styled-components";
import { useNavigate } from "@tanstack/react-router";
import { useGameSession } from "../../state/useGameSession";
import { WantedPosterSurface } from "../../components/WantedPosterSurface";
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

export function SheriffPlace() {
  const navigate = useNavigate();
  const {
    session,
    wantedPosters,
    loading,
    busyMode,
    canReadWantedPosters,
    handleReadWantedPosters,
    notice,
    error,
  } = useGameSession();

  if (!session) {
    return null;
  }

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
        {notice ? <FlowNotice>{notice}</FlowNotice> : null}
        {error ? <FlowError>{error}</FlowError> : null}
      </PlaceBody>
    </FlowSurface>
  );
}
