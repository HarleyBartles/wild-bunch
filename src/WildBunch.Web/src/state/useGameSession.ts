import { useContext } from "react";
import { GameSessionContext } from "./GameSessionProvider";

export function useGameSession() {
  const value = useContext(GameSessionContext);
  if (value === null) {
    throw new Error("useGameSession must be used inside a GameSessionProvider.");
  }
  return value;
}
