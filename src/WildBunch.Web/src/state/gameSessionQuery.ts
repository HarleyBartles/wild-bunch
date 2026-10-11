import { getGame } from "../api/wildBunchApi";

export function gameSessionQueryKey(gameId: string | null) {
  return ["session", gameId] as const;
}

export function gameSessionQueryOptions(gameId: string | null) {
  return {
    queryKey: gameSessionQueryKey(gameId),
    queryFn: () => getGame(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  };
}
