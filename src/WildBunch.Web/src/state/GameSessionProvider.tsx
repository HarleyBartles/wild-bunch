import { createContext, useCallback, useMemo, type ReactNode } from "react";
import { buyStoreItem } from "../api/wildBunchApi";
import type { TownStoreOffersDto, WantedPosterDto } from "../api/types";
import { useCurrentGameSession } from "../hooks/useCurrentGameSession";
import { useTownStoreOffers } from "../hooks/useTownStoreOffers";

type GameSessionValue = ReturnType<typeof useCurrentGameSession> & {
  storeOffers: TownStoreOffersDto | null;
  storeOffersLoading: boolean;
  refreshStoreOffers: () => void;
  handleBuyOffer: (offer: TownStoreOffersDto["offers"][number], quantity: number) => Promise<void>;
  selectedWantedPoster: WantedPosterDto | null;
};

export const GameSessionContext = createContext<GameSessionValue | null>(null);

export function GameSessionProvider({ children }: { children: ReactNode }) {
  const session = useCurrentGameSession();
  const {
    gameId,
    currentTown,
    wantedPosters,
    declaredWantedIdentityHandle,
    setNotice,
    setError,
    setSession,
    reloadCurrentGame,
  } = session;

  const { storeOffers, loading: storeOffersLoading, refreshStoreOffers } = useTownStoreOffers(
    gameId,
    currentTown?.id,
  );

  const selectedWantedPoster = useMemo(
    () =>
      wantedPosters.find((poster) => poster.posterId === declaredWantedIdentityHandle) ??
      wantedPosters[0] ??
      null,
    [wantedPosters, declaredWantedIdentityHandle],
  );

  const handleBuyOffer = useCallback(
    async (offer: TownStoreOffersDto["offers"][number], quantity: number) => {
      if (!gameId || !currentTown?.id) {
        return;
      }

      setNotice("");
      setError("");

      try {
        const result = await buyStoreItem(gameId, currentTown.id, {
          vendorType: offer.vendorType,
          itemKind: offer.itemKind,
          quantity,
        });

        setSession(result.currentSession);

        if (result.success) {
          await reloadCurrentGame(gameId);
          refreshStoreOffers();
          setNotice(result.message);
          setError("");
          return;
        }

        setNotice("");
        setError(result.message);
      } catch (exception) {
        setError(exception instanceof Error ? exception.message : "Unable to buy the selected item.");
      }
    },
    [gameId, currentTown?.id, setNotice, setError, setSession, reloadCurrentGame, refreshStoreOffers],
  );

  const value = useMemo<GameSessionValue>(
    () => ({
      ...session,
      storeOffers,
      storeOffersLoading,
      refreshStoreOffers,
      handleBuyOffer,
      selectedWantedPoster,
    }),
    [session, storeOffers, storeOffersLoading, refreshStoreOffers, handleBuyOffer, selectedWantedPoster],
  );

  return <GameSessionContext.Provider value={value}>{children}</GameSessionContext.Provider>;
}
