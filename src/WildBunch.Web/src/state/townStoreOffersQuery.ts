import { getTownStoreOffers } from "../api/wildBunchApi";

export function townStoreOffersQueryKey(gameId: string | null, townId: string | null | undefined) {
  return ["store-offers", gameId, townId] as const;
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
