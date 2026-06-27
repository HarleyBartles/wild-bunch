import styled from "styled-components";
import { useGameSession } from "../../state/useGameSession";
import {
  FlowSurface,
  FlowNotice,
  FlowError,
  BackButton,
  Panel,
  PanelHead,
  Stack,
  Button,
  Field,
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

interface SaloonPlaceProps {
  onLeave: () => void;
}

export function SaloonPlace({ onLeave }: SaloonPlaceProps) {
  const {
    session,
    wantedPosters,
    declaredWantedIdentityHandle,
    setDeclaredWantedIdentityHandle,
    loading,
    busyMode,
    gameId,
    selectedWantedPoster,
    canLookAroundSaloon,
    canGatherLocalGossip,
    canConfrontSaloonPersonOfInterest,
    handleLookAroundSaloon,
    handleGatherLocalGossip,
    handleConfrontSaloonPersonOfInterest,
    notice,
    error,
  } = useGameSession();

  if (!session) {
    return null;
  }

  const personOfInterest = session.activeSaloonPersonOfInterest;

  return (
    <FlowSurface $variant="place">
      <PlaceHeader>
        <BackButton type="button" onClick={onLeave}>
          ← Back to town
        </BackButton>
        <h1>Saloon</h1>
      </PlaceHeader>
      <PlaceBody>
        <Panel>
          <PanelHead>
            <h2>Saloon floor</h2>
          </PanelHead>
          <Stack>
            <Button
              type="button"
              onClick={handleLookAroundSaloon}
              disabled={loading || !canLookAroundSaloon}
            >
              {busyMode === "investigating" ? "Looking..." : "Look around"}
            </Button>
            <Button
              type="button"
              onClick={handleGatherLocalGossip}
              disabled={loading || !canGatherLocalGossip}
            >
              {busyMode === "investigating" ? "Gathering..." : "Gather gossip"}
            </Button>
          </Stack>
        </Panel>
        {personOfInterest ? (
          <Panel>
            <PanelHead>
              <h2>Person of interest</h2>
            </PanelHead>
            <Stack>
              <p>
                <strong>{personOfInterest.descriptor}</strong> is waiting in the saloon.
              </p>
              {wantedPosters.length > 0 ? (
                <Field as="label">
                  <span>Declare wanted identity</span>
                  <select
                    value={declaredWantedIdentityHandle}
                    onChange={(event) => setDeclaredWantedIdentityHandle(event.target.value)}
                    disabled={loading}
                  >
                    {wantedPosters.map((poster) => (
                      <option key={poster.posterId} value={poster.posterId}>
                        {poster.targetDisplayName}
                      </option>
                    ))}
                  </select>
                </Field>
              ) : (
                <Muted>
                  Read wanted posters at the Sheriff Office to choose the identity you want to
                  declare.
                </Muted>
              )}
              <Button
                type="button"
                onClick={handleConfrontSaloonPersonOfInterest}
                disabled={!gameId || loading || !canConfrontSaloonPersonOfInterest}
              >
                {busyMode === "investigating"
                  ? "Taking in..."
                  : selectedWantedPoster
                    ? `Take ${personOfInterest.descriptor} to sheriff as ${selectedWantedPoster.targetDisplayName}`
                    : `Take ${personOfInterest.descriptor} to sheriff`}
              </Button>
            </Stack>
          </Panel>
        ) : null}
        {notice ? <FlowNotice>{notice}</FlowNotice> : null}
        {error ? <FlowError>{error}</FlowError> : null}
      </PlaceBody>
    </FlowSurface>
  );
}
