import styled from "styled-components";
import { useEffect, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import {
  confrontSaloonPersonOfInterest,
  gatherLocalGossip,
  lookAroundSaloon,
} from "../../api/wildBunchApi";
import type {
  InvestigationActionResultDto,
  SaloonPersonOfInterestConfrontationResultDto,
} from "../../api/types";
import { useGameSession } from "../../state/useGameSession";
import { gameSessionQueryKey } from "../../state/gameSessionQuery";
import { gameResourceQueryKeys, refreshGameResources } from "../../state/gameResourceQueries";
import {
  actionIsGatherLocalGossip,
  actionIsLookAroundSaloon,
} from "../../utils/actionTypePredicates";
import { formatMoney } from "../../utils/formatting";
import {
  FlowSurface,
  FlowNotice,
  FlowError,
  BackButton,
  Panel,
  PanelHead,
  PanelHeading,
  Stack,
  Button,
  Muted,
} from "../../components/ui/sharedStyled";

const DeclaredIdentityField = styled.label`
  display: grid;
  gap: 4px;
`;

const DeclaredIdentityLabel = styled.span`
  font-size: 0.82rem;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.06em;
`;

const DeclaredIdentitySelect = styled.select`
  padding: 8px 10px;
  border-radius: 10px;
  border: 1px solid var(--border-strong);
  background: rgba(0, 0, 0, 0.25);
  color: var(--text);
  font-size: 0.94rem;
`;

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

type SaloonCommand =
  | { kind: "look-around" | "gather-gossip"; gameId: string }
  | { kind: "confront"; gameId: string; declaredWantedIdentityHandle: string };

type SaloonCommandResult =
  | { kind: "journal"; result: InvestigationActionResultDto }
  | { kind: "confrontation"; result: SaloonPersonOfInterestConfrontationResultDto };

export function SaloonPlace() {
  const { gameId } = useGameSession();
  return <SaloonPlaceForGame key={gameId ?? "no-game"} />;
}

function SaloonPlaceForGame() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { session, wantedPosters, loading, gameId, actions } = useGameSession();
  const [declaredWantedIdentityHandle, setDeclaredWantedIdentityHandle] = useState("");

  const selectedWantedPoster =
    wantedPosters.find((poster) => poster.posterId === declaredWantedIdentityHandle) ?? null;
  const personOfInterest = session?.activeSaloonPersonOfInterest;
  const canLookAroundSaloon = actions.some(actionIsLookAroundSaloon);
  const canGatherLocalGossip = actions.some(actionIsGatherLocalGossip);
  const canConfrontSaloonPersonOfInterest = Boolean(personOfInterest && selectedWantedPoster);

  useEffect(() => {
    if (
      declaredWantedIdentityHandle &&
      !wantedPosters.some((poster) => poster.posterId === declaredWantedIdentityHandle)
    ) {
      setDeclaredWantedIdentityHandle("");
    }
  }, [declaredWantedIdentityHandle, wantedPosters]);

  const commandMutation = useMutation<SaloonCommandResult, Error, SaloonCommand>({
    mutationFn: async (command) => {
      switch (command.kind) {
        case "look-around":
          return { kind: "journal", result: await lookAroundSaloon(command.gameId) };
        case "gather-gossip":
          return { kind: "journal", result: await gatherLocalGossip(command.gameId) };
        case "confront":
          return {
            kind: "confrontation",
            result: await confrontSaloonPersonOfInterest(
              command.gameId,
              command.declaredWantedIdentityHandle,
            ),
          };
      }
    },
    onSuccess: async (outcome) => {
      if (outcome.kind === "journal") {
        queryClient.setQueryData(
          gameResourceQueryKeys.journal(outcome.result.currentJournal.id),
          outcome.result.currentJournal,
        );
      } else {
        queryClient.setQueryData(
          gameSessionQueryKey(outcome.result.currentSession.id),
          outcome.result.currentSession,
        );
      }
      const completedGameId =
        outcome.kind === "journal"
          ? outcome.result.currentJournal.id
          : outcome.result.currentSession.id;
      await refreshGameResources(queryClient, completedGameId);
    },
  });

  const submit = (command: SaloonCommand) => commandMutation.mutate(command);

  if (!session) {
    return null;
  }

  const notice = commandMutation.data
    ? commandMutation.data.kind === "confrontation" &&
      commandMutation.data.result.fineAmount !== null &&
      commandMutation.data.result.walletBefore !== null &&
      commandMutation.data.result.walletAfter !== null
      ? `${commandMutation.data.result.message} Wallet ${formatMoney(commandMutation.data.result.walletBefore)} -> ${formatMoney(commandMutation.data.result.walletAfter)}.`
      : commandMutation.data.result.message
    : "";
  const error = commandMutation.error?.message ?? "";
  const commandPending = commandMutation.isPending;
  const lookAround = () => {
    if (gameId && canLookAroundSaloon) submit({ kind: "look-around", gameId });
  };
  const gatherGossip = () => {
    if (gameId && canGatherLocalGossip) submit({ kind: "gather-gossip", gameId });
  };
  const confront = () => {
    if (gameId && canConfrontSaloonPersonOfInterest && declaredWantedIdentityHandle) {
      submit({ kind: "confront", gameId, declaredWantedIdentityHandle });
    }
  };

  return (
    <FlowSurface $variant="place">
      <PlaceHeader>
        <BackButton type="button" onClick={() => void navigate({ to: "/town" })}>
          ← Back to town
        </BackButton>
        <h1>Saloon</h1>
      </PlaceHeader>
      <PlaceBody>
        <Panel>
          <PanelHead>
            <PanelHeading>Saloon floor</PanelHeading>
          </PanelHead>
          <Stack>
            <Button
              type="button"
              onClick={lookAround}
              disabled={loading || commandPending || !canLookAroundSaloon}
            >
              {commandPending && commandMutation.variables?.kind === "look-around"
                ? "Looking..."
                : "Look around"}
            </Button>
            <Button
              type="button"
              onClick={gatherGossip}
              disabled={loading || commandPending || !canGatherLocalGossip}
            >
              {commandPending && commandMutation.variables?.kind === "gather-gossip"
                ? "Gathering..."
                : "Gather gossip"}
            </Button>
          </Stack>
        </Panel>
        {personOfInterest ? (
          <Panel>
            <PanelHead>
              <PanelHeading>Person of interest</PanelHeading>
            </PanelHead>
            <Stack>
              <p>
                <strong>{personOfInterest.descriptor}</strong> is waiting in the saloon.
              </p>
              {wantedPosters.length > 0 ? (
                <DeclaredIdentityField>
                  <DeclaredIdentityLabel>Declare wanted identity</DeclaredIdentityLabel>
                  <DeclaredIdentitySelect
                    value={declaredWantedIdentityHandle}
                    onChange={(event) => setDeclaredWantedIdentityHandle(event.target.value)}
                    disabled={loading || commandPending}
                  >
                    <option value="">Choose who you believe this is</option>
                    {wantedPosters.map((poster) => (
                      <option key={poster.posterId} value={poster.posterId}>
                        {poster.targetDisplayName}
                      </option>
                    ))}
                  </DeclaredIdentitySelect>
                </DeclaredIdentityField>
              ) : (
                <Muted>
                  Read wanted posters at the Sheriff Office to choose the identity you want to
                  declare.
                </Muted>
              )}
              <Button
                type="button"
                onClick={confront}
                disabled={
                  !gameId || loading || commandPending || !canConfrontSaloonPersonOfInterest
                }
              >
                {commandPending && commandMutation.variables?.kind === "confront"
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
