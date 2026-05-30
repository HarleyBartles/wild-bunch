import { useEffect, useState } from "react";
import { getTownStoreOffers } from "../api/wildBunchApi";
import type { TownStoreOffersDto } from "../api/types";

export function useTownStoreOffers(gameId: string | null, townId: string | null | undefined) {
  const [storeOffers, setStoreOffers] = useState<TownStoreOffersDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [refreshToken, setRefreshToken] = useState(0);

  useEffect(() => {
    if (!gameId || !townId) {
      setStoreOffers(null);
      setLoading(false);
      return;
    }

    const activeGameId = gameId;
    const activeTownId = townId;
    let cancelled = false;

    void (async () => {
      setLoading(true);

      try {
        const offers = await getTownStoreOffers(activeGameId, activeTownId);
        if (!cancelled) {
          setStoreOffers(offers);
        }
      } catch {
        if (!cancelled) {
          setStoreOffers(null);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [gameId, townId, refreshToken]);

  function refreshStoreOffers() {
    setRefreshToken((current) => current + 1);
  }

  return {
    storeOffers,
    loading,
    refreshStoreOffers,
  };
}
