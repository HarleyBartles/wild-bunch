import { useMutation, useQuery } from "@tanstack/react-query";
import {
  acknowledgeTravelArrival,
  advanceTravelDay,
  resolveTravelEncounter,
} from "../api/wildBunchApi";
import type { GameSessionDto, GameTurnResultDto } from "../api/types";
import { getErrorMessage } from "../components/travel/travelShared";
import { gameSessionQueryOptions } from "../state/gameSessionQuery";

interface UseTravelPanelStateArgs {
  gameId: string;
  session: GameSessionDto;
  busy: boolean;
  onTurnResult: (result: GameTurnResultDto) => Promise<void> | void;
}

interface TravelPanelState {
  session: GameSessionDto;
  busy: boolean;
  refreshing: boolean;
  actionError: string | null;
  advanceTravelDay: () => Promise<void>;
  acknowledgeTravelArrival: () => Promise<void>;
  resolveTravelEncounter: (
    choiceId: string,
    options?: {
      bulletSpend?: number | null;
      bribeAmount?: number | null;
    },
  ) => Promise<void>;
}

function useTravelMutationState(
  gameId: string,
  onTurnResult: UseTravelPanelStateArgs["onTurnResult"],
) {
  const advanceMutation = useMutation({
    mutationFn: () => advanceTravelDay(gameId),
    onSuccess: async (result) => {
      await onTurnResult(result);
    },
  });

  const acknowledgeMutation = useMutation({
    mutationFn: () => acknowledgeTravelArrival(gameId),
    onSuccess: async (result) => {
      await onTurnResult(result);
    },
  });

  const resolveMutation = useMutation({
    mutationFn: (payload: {
      choiceId: string;
      bulletSpend?: number | null;
      bribeAmount?: number | null;
    }) =>
      resolveTravelEncounter(gameId, payload.choiceId, {
        bulletSpend: payload.bulletSpend,
        bribeAmount: payload.bribeAmount,
      }),
    onSuccess: async (result) => {
      await onTurnResult(result);
    },
  });

  return { advanceMutation, acknowledgeMutation, resolveMutation };
}

export function useTravelPanelState({
  gameId,
  session,
  busy,
  onTurnResult,
}: UseTravelPanelStateArgs): TravelPanelState {
  const travelSessionQuery = useQuery({
    ...gameSessionQueryOptions(gameId),
    enabled: Boolean(gameId && session.journey),
    initialData: session,
    staleTime: 0,
  });

  const { advanceMutation, acknowledgeMutation, resolveMutation } = useTravelMutationState(
    gameId,
    onTurnResult,
  );

  const actionError =
    getErrorMessage(advanceMutation.error) ||
    getErrorMessage(acknowledgeMutation.error) ||
    getErrorMessage(resolveMutation.error) ||
    getErrorMessage(travelSessionQuery.error);

  return {
    session: travelSessionQuery.data ?? session,
    busy:
      busy ||
      advanceMutation.isPending ||
      acknowledgeMutation.isPending ||
      resolveMutation.isPending,
    refreshing: travelSessionQuery.isFetching,
    actionError: actionError || null,
    advanceTravelDay: async () => {
      await advanceMutation.mutateAsync();
    },
    acknowledgeTravelArrival: async () => {
      await acknowledgeMutation.mutateAsync();
    },
    resolveTravelEncounter: async (
      choiceId: string,
      options?: {
        bulletSpend?: number | null;
        bribeAmount?: number | null;
      },
    ) => {
      await resolveMutation.mutateAsync({
        choiceId,
        bulletSpend: options?.bulletSpend ?? null,
        bribeAmount: options?.bribeAmount ?? null,
      });
    },
  };
}
