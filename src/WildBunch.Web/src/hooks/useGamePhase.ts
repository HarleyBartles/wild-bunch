import { useMemo } from "react";
import { JourneyStatus } from "../api/types";
import { useGameSession } from "../state/useGameSession";

export type GamePhase =
  | "pre-session"
  | "in-town"
  | "on-trail"
  | "arrival";

export interface GamePhaseState {
  phase: GamePhase;
  hasSession: boolean;
  isOnTrail: boolean;
  isArrivalPending: boolean;
}

/**
 * Derives the current game phase from session state.
 * The frontend never invents game state — it only reads what the backend provides.
 *
 * Phases:
 * - pre-session: no session loaded
 * - in-town: session exists, no active journey
 * - on-trail: journey is Active or Interrupted
 * - arrival: journey is Completed, awaiting acknowledgement
 */
export function useGamePhase(): GamePhaseState {
  const { session } = useGameSession();

  return useMemo(() => {
    if (!session) {
      return {
        phase: "pre-session" as const,
        hasSession: false,
        isOnTrail: false,
        isArrivalPending: false,
      };
    }

    const journey = session.journey;

    if (!journey) {
      return {
        phase: "in-town" as const,
        hasSession: true,
        isOnTrail: false,
        isArrivalPending: false,
      };
    }

    if (journey.status === JourneyStatus.Completed) {
      return {
        phase: "arrival" as const,
        hasSession: true,
        isOnTrail: false,
        isArrivalPending: true,
      };
    }

    // Active or Interrupted — both mean the player is on the trail
    return {
      phase: "on-trail" as const,
      hasSession: true,
      isOnTrail: true,
      isArrivalPending: false,
    };
  }, [session]);
}
