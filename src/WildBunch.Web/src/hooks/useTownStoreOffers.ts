import { useQuery } from "@tanstack/react-query";
import { townStoreOffersQueryOptions } from "../state/townStoreOffersQuery";

export function useTownStoreOffers(gameId: string | null, townId: string | null | undefined) {
  const query = useQuery(townStoreOffersQueryOptions(gameId, townId));

  return {
    storeOffers: query.isError ? null : (query.data ?? null),
    loading: query.isFetching,
  };
}
