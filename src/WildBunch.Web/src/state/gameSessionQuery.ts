import { getGame } from "../api/wildBunchApi";
import { gameResourceQueryKeys } from "./gameResourceQueries";

export function gameSessionQueryKey(gameId: string | null) {
  return gameResourceQueryKeys.session(gameId);
}

export function gameSessionQueryOptions(gameId: string | null) {
  return {
    queryKey: gameSessionQueryKey(gameId),
    queryFn: () => getGame(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  };
}
