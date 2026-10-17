import { createContext, type ReactNode } from "react";
import { useCurrentGameSession } from "../hooks/useCurrentGameSession";

type GameSessionValue = ReturnType<typeof useCurrentGameSession>;

export const GameSessionContext = createContext<GameSessionValue | null>(null);

export function GameSessionProvider({ children }: { children: ReactNode }) {
  const session = useCurrentGameSession();

  return <GameSessionContext.Provider value={session}>{children}</GameSessionContext.Provider>;
}
