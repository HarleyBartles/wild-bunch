import type { QueryClient } from "@tanstack/react-query";

export const gameResourceQueryKeys = {
  session: (gameId: string | null) => ["session", gameId] as const,
  actions: (gameId: string | null) => ["actions", gameId] as const,
  journal: (gameId: string | null) => ["journal", gameId] as const,
  storeOffersPrefix: (gameId: string | null) => ["store-offers", gameId] as const,
  storeOffers: (gameId: string | null, townId: string | null | undefined) =>
    ["store-offers", gameId, townId] as const,
  travelPreviewPrefix: (gameId: string | null) => ["travel-preview", gameId] as const,
  travelPreview: (
    gameId: string | null,
    originTownId: string | null,
    destinationTownId: string | null,
    sessionUpdatedAt: number,
  ) => ["travel-preview", gameId, originTownId, destinationTownId, sessionUpdatedAt] as const,
  devSession: (gameId: string | null) => ["dev-session-context", gameId] as const,
  devSaloon: (gameId: string | null) => ["dev-saloon-context", gameId] as const,
  devTravel: (gameId: string | null) => ["dev-travel-context", gameId] as const,
  devSessionAudit: (gameId: string | null) => ["dev-session-audit", gameId] as const,
};

export async function refreshGameResources(queryClient: QueryClient, gameId: string) {
  await Promise.all(
    [
      gameResourceQueryKeys.session(gameId),
      gameResourceQueryKeys.actions(gameId),
      gameResourceQueryKeys.journal(gameId),
      gameResourceQueryKeys.storeOffersPrefix(gameId),
      gameResourceQueryKeys.travelPreviewPrefix(gameId),
      gameResourceQueryKeys.devSession(gameId),
      gameResourceQueryKeys.devSaloon(gameId),
      gameResourceQueryKeys.devTravel(gameId),
      gameResourceQueryKeys.devSessionAudit(gameId),
    ].map((queryKey) => queryClient.invalidateQueries({ queryKey })),
  );
}
