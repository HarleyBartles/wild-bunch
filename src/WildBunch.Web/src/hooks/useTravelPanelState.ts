import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { advanceTravelDay, resolveTravelEncounter } from "../api/wildBunchApi";
import type { GameSessionDto } from "../api/types";
import { getErrorMessage } from "../components/travel/travelShared";
import { gameSessionQueryOptions } from "../state/gameSessionQuery";
import { gameSessionQueryKey } from "../state/gameSessionQuery";
import { refreshGameResources } from "../state/gameResourceQueries";

interface UseTravelPanelStateArgs {
  gameId: string;
  session: GameSessionDto;
  busy: boolean;
}

interface TravelPanelState {
  session: GameSessionDto;
  busy: boolean;
  refreshing: boolean;
  message: string | null;
  actionError: string | null;
  advanceTravelDay: () => void;
  resolveTravelEncounter: (
    choiceId: string,
    options?: {
      bulletSpend?: number | null;
      bribeAmount?: number | null;
    },
  ) => void;
}

function useTravelMutationState() {
  const queryClient = useQueryClient();

  const advanceMutation = useMutation({
    mutationFn: ({ gameId }: { gameId: string }) => advanceTravelDay(gameId),
    onSuccess: async (result, variables) => {
      const updatedGameId = variables.gameId;
      queryClient.setQueryData(gameSessionQueryKey(updatedGameId), result.currentSession);
      await refreshGameResources(queryClient, updatedGameId);
    },
  });

  const resolveMutation = useMutation({
    mutationFn: (payload: {
      gameId: string;
      choiceId: string;
      bulletSpend?: number | null;
      bribeAmount?: number | null;
    }) =>
      resolveTravelEncounter(payload.gameId, payload.choiceId, {
        bulletSpend: payload.bulletSpend,
        bribeAmount: payload.bribeAmount,
      }),
    onSuccess: async (result, variables) => {
      const updatedGameId = variables.gameId;
      queryClient.setQueryData(gameSessionQueryKey(updatedGameId), result.currentSession);
      await refreshGameResources(queryClient, updatedGameId);
    },
  });

  return { advanceMutation, resolveMutation };
}

export function useTravelPanelState({
  gameId,
  session,
  busy,
}: UseTravelPanelStateArgs): TravelPanelState {
  const travelSessionQuery = useQuery({
    ...gameSessionQueryOptions(gameId),
    enabled: Boolean(gameId && session.journey),
    initialData: session,
    staleTime: 0,
  });

  const { advanceMutation, resolveMutation } = useTravelMutationState();

  const actionError =
    (advanceMutation.variables?.gameId === gameId && getErrorMessage(advanceMutation.error)) ||
    (resolveMutation.variables?.gameId === gameId && getErrorMessage(resolveMutation.error)) ||
    getErrorMessage(travelSessionQuery.error);

  const message =
    (advanceMutation.variables?.gameId === gameId && advanceMutation.data?.message) ||
    (resolveMutation.variables?.gameId === gameId && resolveMutation.data?.message) ||
    null;

  return {
    session: travelSessionQuery.data ?? session,
    busy:
      busy ||
      (advanceMutation.isPending && advanceMutation.variables?.gameId === gameId) ||
      (resolveMutation.isPending && resolveMutation.variables?.gameId === gameId),
    refreshing: travelSessionQuery.isFetching,
    message,
    actionError: actionError || null,
    advanceTravelDay: () => {
      advanceMutation.reset();
      resolveMutation.reset();
      advanceMutation.mutate({ gameId });
    },
    resolveTravelEncounter: (
      choiceId: string,
      options?: {
        bulletSpend?: number | null;
        bribeAmount?: number | null;
      },
    ) => {
      advanceMutation.reset();
      resolveMutation.reset();
      resolveMutation.mutate({
        gameId,
        choiceId,
        bulletSpend: options?.bulletSpend ?? null,
        bribeAmount: options?.bribeAmount ?? null,
      });
    },
  };
}
