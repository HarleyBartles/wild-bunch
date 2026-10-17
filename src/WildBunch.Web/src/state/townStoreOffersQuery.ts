import { getTownStoreOffers } from "../api/wildBunchApi";
import { gameResourceQueryKeys } from "./gameResourceQueries";

export function townStoreOffersQueryKey(gameId: string | null, townId: string | null | undefined) {
  return gameResourceQueryKeys.storeOffers(gameId, townId);
}

export function townStoreOffersQueryOptions(
  gameId: string | null,
  townId: string | null | undefined,
) {
  return {
    queryKey: townStoreOffersQueryKey(gameId, townId),
    queryFn: () => getTownStoreOffers(gameId as string, townId as string),
    enabled: Boolean(gameId && townId),
    retry: false,
  };
}
