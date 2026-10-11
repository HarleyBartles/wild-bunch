import { createContext, useMemo, type ReactNode } from "react";
import type { WantedPosterDto } from "../api/types";
import { useCurrentGameSession } from "../hooks/useCurrentGameSession";

type GameSessionValue = ReturnType<typeof useCurrentGameSession> & {
  selectedWantedPoster: WantedPosterDto | null;
};

export const GameSessionContext = createContext<GameSessionValue | null>(null);

export function GameSessionProvider({ children }: { children: ReactNode }) {
  const session = useCurrentGameSession();
  const { wantedPosters, declaredWantedIdentityHandle } = session;

  const selectedWantedPoster = useMemo(
    () => wantedPosters.find((poster) => poster.posterId === declaredWantedIdentityHandle) ?? null,
    [wantedPosters, declaredWantedIdentityHandle],
  );

  const value = useMemo<GameSessionValue>(
    () => ({
      ...session,
      selectedWantedPoster,
    }),
    [session, selectedWantedPoster],
  );

  return <GameSessionContext.Provider value={value}>{children}</GameSessionContext.Provider>;
}
