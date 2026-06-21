import { createContext, useCallback, useContext, useMemo, type ReactNode } from "react";
import { buyStoreItem } from "../api/wildBunchApi";
import type { StoreOfferDto, TownStoreOffersDto } from "../api/types";
import { useCurrentGameSession } from "../hooks/useCurrentGameSession";
import { useTownStoreOffers } from "../hooks/useTownStoreOffers";

type CurrentGameSessionStore = ReturnType<typeof useCurrentGameSession>;

interface TownStoreStore {
  storeOffers: TownStoreOffersDto | null;
  storeOffersLoading: boolean;
  refreshStoreOffers: () => void;
  handleBuyOffer: (offer: StoreOfferDto, quantity: number) => Promise<void>;
}

export type GameSessionStore = CurrentGameSessionStore & TownStoreStore;

const GameSessionContext = createContext<GameSessionStore | null>(null);

export function GameSessionProvider({ children }: { children: ReactNode }) {
  const session = useCurrentGameSession();
  const { gameId, currentTown, setSession, setNotice, setError, reloadCurrentGame } = session;
  const { storeOffers, loading: storeOffersLoading, refreshStoreOffers } = useTownStoreOffers(
    gameId,
    currentTown?.id,
  );

  const handleBuyOffer = useCallback(
    async (offer: StoreOfferDto, quantity: number) => {
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
    [gameId, currentTown?.id, reloadCurrentGame, refreshStoreOffers, setSession, setNotice, setError],
  );

  const store = useMemo<GameSessionStore>(
    () => ({ ...session, storeOffers, storeOffersLoading, refreshStoreOffers, handleBuyOffer }),
    [session, storeOffers, storeOffersLoading, refreshStoreOffers, handleBuyOffer],
  );

  return <GameSessionContext.Provider value={store}>{children}</GameSessionContext.Provider>;
}

export function useGameSession(): GameSessionStore {
  const store = useContext(GameSessionContext);
  if (store === null) {
    throw new Error("useGameSession must be used within a GameSessionProvider.");
  }

  return store;
}
