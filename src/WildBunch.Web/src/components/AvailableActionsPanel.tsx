import styled from "styled-components";
import { AvailableActionKind } from "../api/types";
import { formatActionKind } from "../ui/formatters";
import { useGameSession } from "../state/useGameSession";
import {
  Panel,
  PanelHead,
  PanelSubtitle,
  Stack,
  ItemCard,
  Field,
  Muted,
  Button,
} from "./ui/sharedStyled";

const ActionRow = styled(ItemCard)`
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;

  p {
    margin: 0;
    font-size: 0.88rem;
    color: var(--muted);
  }
`;

export function AvailableActionsPanel() {
  const {
    session,
    actions,
    wantedPosters,
    declaredWantedIdentityHandle,
    setDeclaredWantedIdentityHandle,
    loading,
    busyMode,
    gameId,
    selectedWantedPoster,
    canReadWantedPosters,
    canInspectNoticeBoard,
    canCheckLocalRecords,
    canFollowTelegraphLeads,
    canGatherLocalGossip,
    canLookAroundSaloon,
    canConfrontSaloonPersonOfInterest,
    handleReadWantedPosters,
    handleInspectNoticeBoard,
    handleCheckLocalRecords,
    handleFollowTelegraphLeads,
    handleGatherLocalGossip,
    handleLookAroundSaloon,
    handleConfrontSaloonPersonOfInterest,
  } = useGameSession();

  return (
    <Panel>
      <PanelHead>
        <h2>Available actions</h2>
        <PanelSubtitle as="span">{actions.length} fetched</PanelSubtitle>
      </PanelHead>
      <Stack>
        {session?.activeSaloonPersonOfInterest ? (
          <ActionRow>
            <div>
              <strong>Person of interest spotted</strong>
              <p>{session.activeSaloonPersonOfInterest.descriptor} is waiting in the saloon.</p>
              {wantedPosters.length > 0 ? (
                <Field as="label" style={{ marginTop: "0.75rem" }}>
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
                <Muted>Read wanted posters to choose the identity you want to declare.</Muted>
              )}
            </div>
            <Button
              type="button"
              onClick={handleConfrontSaloonPersonOfInterest}
              disabled={!gameId || loading || !canConfrontSaloonPersonOfInterest}
            >
              {busyMode === "investigating"
                ? "Taking in..."
                : selectedWantedPoster
                  ? `Take ${session.activeSaloonPersonOfInterest.descriptor} to sheriff as ${selectedWantedPoster.targetDisplayName}`
                  : `Take ${session.activeSaloonPersonOfInterest.descriptor} to sheriff`}
            </Button>
          </ActionRow>
        ) : null}
        {actions.length > 0 ? (
          actions.map((action) => (
            <ActionRow key={`${action.kind}-${action.label}`}>
              <div>
                <strong>{action.label}</strong>
                <p>{formatActionKind(action.kind)}</p>
              </div>
              {action.kind === AvailableActionKind.ReadWantedPosters ? (
                <Button
                  type="button"
                  onClick={handleReadWantedPosters}
                  disabled={!gameId || loading || !canReadWantedPosters}
                >
                  {busyMode === "reading" ? "Reading..." : "Read wanted posters"}
                </Button>
              ) : action.kind === AvailableActionKind.InspectNoticeBoard ? (
                <Button
                  type="button"
                  onClick={handleInspectNoticeBoard}
                  disabled={!gameId || loading || !canInspectNoticeBoard}
                >
                  {busyMode === "investigating" ? "Inspecting..." : "Inspect notice board"}
                </Button>
              ) : action.kind === AvailableActionKind.CheckSheriffRecords ? (
                <Button
                  type="button"
                  onClick={handleCheckLocalRecords}
                  disabled={!gameId || loading || !canCheckLocalRecords}
                >
                  {busyMode === "investigating" ? "Checking..." : "Check local records"}
                </Button>
              ) : action.kind === AvailableActionKind.FollowTelegraphLeads ? (
                <Button
                  type="button"
                  onClick={handleFollowTelegraphLeads}
                  disabled={!gameId || loading || !canFollowTelegraphLeads}
                >
                  {busyMode === "investigating" ? "Following..." : "Follow telegraph leads"}
                </Button>
              ) : action.kind === AvailableActionKind.GatherLocalGossip ? (
                <Button
                  type="button"
                  onClick={handleGatherLocalGossip}
                  disabled={!gameId || loading || !canGatherLocalGossip}
                >
                  {busyMode === "investigating" ? "Gathering..." : "Gather local gossip"}
                </Button>
              ) : action.kind === AvailableActionKind.LookAroundSaloon ? (
                <Button
                  type="button"
                  onClick={handleLookAroundSaloon}
                  disabled={!gameId || loading || !canLookAroundSaloon}
                >
                  {busyMode === "investigating" ? "Looking..." : "Look around saloon"}
                </Button>
              ) : null}
            </ActionRow>
          ))
        ) : (
          <Muted>Actions will appear here after a game loads.</Muted>
        )}
      </Stack>
    </Panel>
  );
}
