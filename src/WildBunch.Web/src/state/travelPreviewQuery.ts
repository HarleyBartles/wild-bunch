import { queryOptions } from "@tanstack/react-query";
import { previewTravel } from "../api/wildBunchApi";
import { gameResourceQueryKeys } from "./gameResourceQueries";

export function travelPreviewQueryKey(
  gameId: string | null,
  originTownId: string | null,
  destinationTownId: string | null,
  sessionUpdatedAt: number,
) {
  return gameResourceQueryKeys.travelPreview(
    gameId,
    originTownId,
    destinationTownId,
    sessionUpdatedAt,
  );
}

export function travelPreviewQueryOptions(
  gameId: string | null,
  originTownId: string | null,
  destinationTownId: string | null,
  sessionUpdatedAt: number,
) {
  return queryOptions({
    queryKey: travelPreviewQueryKey(gameId, originTownId, destinationTownId, sessionUpdatedAt),
    queryFn: () => previewTravel(gameId as string, destinationTownId as string),
    enabled: Boolean(gameId && originTownId && destinationTownId),
    retry: false,
  });
}
