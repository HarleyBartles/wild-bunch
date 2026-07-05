import { useMemo } from "react";
import { JourneyStatus, StartFlowPhase } from "../api/types";
import { useGameSession } from "../state/useGameSession";

export type GamePhase =
  | "pre-session"
  | "setup"
  | "prologue"
  | "town-selection"
  | "in-town"
  | "on-trail";

export interface GamePhaseState {
  phase: GamePhase;
  hasSession: boolean;
  isOnTrail: boolean;
}

/**
 * Derives the current game phase from session state.
 * The frontend never invents game state — it only reads what the backend provides.
 *
 * Phases:
 * - pre-session: no session loaded
 * - setup: session exists but setup not yet complete (should not normally occur)
 * - prologue: setup complete, prologue not yet viewed
 * - town-selection: prologue viewed, town not yet selected
 * - in-town: game started, no active journey
 * - on-trail: journey is Active, Interrupted, or Completed
 *   (Completed means the player sees the last day's resolution and
 *   acknowledges arrival via TrailFlowSurface before transitioning to town)
 */
export function useGamePhase(): GamePhaseState {
  const { session } = useGameSession();

  return useMemo(() => {
    if (!session) {
      return {
        phase: "pre-session" as const,
        hasSession: false,
        isOnTrail: false,
      };
    }

    if (session.startFlowPhase !== StartFlowPhase.GameStarted) {
      if (session.startFlowPhase === StartFlowPhase.SetupComplete) {
        return { phase: "prologue" as const, hasSession: true, isOnTrail: false };
      }
      if (session.startFlowPhase === StartFlowPhase.PrologueViewed ||
          session.startFlowPhase === StartFlowPhase.StartingTownSelected) {
        return { phase: "town-selection" as const, hasSession: true, isOnTrail: false };
      }
      return { phase: "pre-session" as const, hasSession: false, isOnTrail: false };
    }

    const journey = session.journey;

    if (!journey) {
      return { phase: "in-town" as const, hasSession: true, isOnTrail: false };
    }

    // Active, Interrupted, or Completed — all mean the player is on the trail.
    // Completed shows the arrival/acknowledge view inside TrailFlowSurface.
    return { phase: "on-trail" as const, hasSession: true, isOnTrail: true };
  }, [session]);
}
