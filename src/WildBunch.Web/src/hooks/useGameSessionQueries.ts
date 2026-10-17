import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { getAvailableActions, getJournal } from "../api/wildBunchApi";
import { gameSessionQueryOptions } from "../state/gameSessionQuery";
import { gameResourceQueryKeys } from "../state/gameResourceQueries";

type CockpitMode = "home" | "travel";

export function useGameSessionQueries(gameId: string | null) {
  const sessionQuery = useQuery(gameSessionQueryOptions(gameId));

  const actionsQuery = useQuery({
    queryKey: gameResourceQueryKeys.actions(gameId),
    queryFn: () => getAvailableActions(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  const journalQuery = useQuery({
    queryKey: gameResourceQueryKeys.journal(gameId),
    queryFn: () => getJournal(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  const session = sessionQuery.data ?? null;
  const journal = journalQuery.data ?? null;
  const journalLoading = journalQuery.isPending && journalQuery.data === undefined;
  const journalRefreshing = journalQuery.isFetching && journalQuery.data !== undefined;
  const journalError = journalQuery.isError ? "Could not read the journal." : null;
  const actions = actionsQuery.data ?? [];
  const actionsLoading = actionsQuery.isPending && actionsQuery.data === undefined;
  const actionsError = actionsQuery.isError ? "Could not read town services." : null;

  const currentTown = useMemo(() => {
    if (!session) {
      return null;
    }
    return session.world.towns.find((town) => town.id === session.player.currentTownId) ?? null;
  }, [session]);

  const cockpitMode: CockpitMode = session?.journey ? "travel" : "home";

  return {
    sessionQuery,
    actionsQuery,
    journalQuery,
    session,
    journal,
    journalLoading,
    journalRefreshing,
    journalError,
    retryJournal: async () => {
      await journalQuery.refetch();
    },
    actions,
    actionsLoading,
    actionsError,
    retryActions: async () => {
      await actionsQuery.refetch();
    },
    currentTown,
    cockpitMode,
  };
}
