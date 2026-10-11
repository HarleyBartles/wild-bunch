import { useQueryClient } from "@tanstack/react-query";
import { createContext, useCallback, useMemo, type ReactNode } from "react";
import { buyStoreItem } from "../api/wildBunchApi";
import type { TownStoreOffersDto, WantedPosterDto } from "../api/types";
import { useCurrentGameSession } from "../hooks/useCurrentGameSession";
import { townStoreOffersQueryKey } from "./townStoreOffersQuery";

type GameSessionValue = ReturnType<typeof useCurrentGameSession> & {
  handleBuyOffer: (offer: TownStoreOffersDto["offers"][number], quantity: number) => Promise<void>;
  selectedWantedPoster: WantedPosterDto | null;
};

export const GameSessionContext = createContext<GameSessionValue | null>(null);

export function GameSessionProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
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

  const selectedWantedPoster = useMemo(
    () => wantedPosters.find((poster) => poster.posterId === declaredWantedIdentityHandle) ?? null,
    [wantedPosters, declaredWantedIdentityHandle],
  );

  const handleBuyOffer = useCallback(
    async (offer: TownStoreOffersDto["offers"][number], quantity: number) => {
      const activeGameId = gameId;
      const activeTownId = currentTown?.id;
      if (!activeGameId || !activeTownId) {
        return;
      }

      setNotice("");
      setError("");

      try {
        const result = await buyStoreItem(activeGameId, activeTownId, {
          itemKind: offer.itemKind,
          quantity,
        });

        setSession(result.currentSession);

        if (result.success) {
          await reloadCurrentGame(activeGameId);
          void queryClient.invalidateQueries({
            queryKey: townStoreOffersQueryKey(activeGameId, activeTownId),
          });
          setNotice(result.message);
          setError("");
          return;
        }

        setNotice("");
        setError(result.message);
      } catch (exception) {
        setError(
          exception instanceof Error ? exception.message : "Unable to buy the selected item.",
        );
      }
    },
    [gameId, currentTown?.id, setNotice, setError, setSession, reloadCurrentGame, queryClient],
  );

  const value = useMemo<GameSessionValue>(
    () => ({
      ...session,
      handleBuyOffer,
      selectedWantedPoster,
    }),
    [session, handleBuyOffer, selectedWantedPoster],
  );

  return <GameSessionContext.Provider value={value}>{children}</GameSessionContext.Provider>;
}
