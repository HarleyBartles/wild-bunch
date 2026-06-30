import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { getAvailableActions, getGame, getJournal } from "../api/wildBunchApi";
import type {
  AvailableActionDto,
  GameSessionDto,
  JournalDto,
} from "../api/types";

type CockpitMode = "home" | "travel";

export function useGameSessionQueries(gameId: string | null) {
  const sessionQuery = useQuery({
    queryKey: ["session", gameId],
    queryFn: () => getGame(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  const actionsQuery = useQuery({
    queryKey: ["actions", gameId],
    queryFn: () => getAvailableActions(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  const journalQuery = useQuery({
    queryKey: ["journal", gameId],
    queryFn: () => getJournal(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  const session = sessionQuery.data ?? null;
  const journal = journalQuery.data ?? null;
  const actions = actionsQuery.data ?? [];

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
    actions,
    currentTown,
    cockpitMode,
  };
}
