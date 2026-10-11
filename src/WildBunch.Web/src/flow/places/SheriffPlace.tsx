import styled from "styled-components";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { readWantedPosters } from "../../api/wildBunchApi";
import type { WantedPostersResultDto } from "../../api/types";
import { useGameSession } from "../../state/useGameSession";
import { gameResourceQueryKeys, refreshGameResources } from "../../state/gameResourceQueries";
import { actionIsWantedPosters } from "../../utils/actionTypePredicates";
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
  const queryClient = useQueryClient();
  const { session, wantedPosters, gameId, actions, loading } = useGameSession();
  const canReadWantedPosters = actions.some(actionIsWantedPosters);
  const readMutation = useMutation<WantedPostersResultDto, Error, { gameId: string }>({
    mutationFn: ({ gameId: submittedGameId }) => readWantedPosters(submittedGameId),
    onSuccess: async (result) => {
      queryClient.setQueryData(
        gameResourceQueryKeys.journal(result.currentJournal.id),
        result.currentJournal,
      );
      await refreshGameResources(queryClient, result.currentJournal.id);
    },
  });

  if (!session) {
    return null;
  }

  const notice = readMutation.data?.message ?? "";
  const error = readMutation.error?.message ?? "";

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
              onClick={() => {
                if (gameId && canReadWantedPosters) readMutation.mutate({ gameId });
              }}
              disabled={loading || readMutation.isPending || !canReadWantedPosters}
            >
              {readMutation.isPending ? "Reading..." : "Read wanted posters"}
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
