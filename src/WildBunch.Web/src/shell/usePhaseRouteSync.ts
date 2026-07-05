import { useEffect, useRef } from "react";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { useGamePhase } from "../hooks/useGamePhase";
import { useGameSession } from "../state/useGameSession";

/**
 * Reconciles the URL with the backend-derived game phase.
 * Backend transitions drive navigation — when the phase changes,
 * the hook navigates to the matching route if the URL doesn't already match.
 *
 * When transitioning from on-trail to in-town (arrival), navigates to
 * /town?arrived=1 so TownHubSurface can show the arrival notice.
 *
 * Skips sync while the session query is still loading, so stale URLs
 * (e.g. deep-linked /town/store with no session yet) are not redirected
 * to / until we know whether a session exists.
 */
export function usePhaseRouteSync(): void {
  const { phase, hasSession } = useGamePhase();
  const { sessionLoading } = useGameSession();
  const location = useLocation();
  const navigate = useNavigate();
  const prevPhaseRef = useRef(phase);

  useEffect(() => {
    // Skip sync while the session is still being fetched from the backend.
    // This prevents redirecting a stale deep-linked URL before we know
    // whether the player has a session.
    if (sessionLoading) {
      return;
    }

    const expectedPrefix = phaseToUrlPrefix(phase);
    if (!expectedPrefix) {
      return;
    }

    const currentPath = location.pathname;
    if (currentPath === expectedPrefix || currentPath.startsWith(expectedPrefix + "/")) {
      prevPhaseRef.current = phase;
      return;
    }

    // When transitioning from on-trail to in-town, set ?arrived=1 so
    // TownHubSurface shows the arrival notice.
    const isArrival = prevPhaseRef.current === "on-trail" && phase === "in-town";
    void navigate({
      to: expectedPrefix,
      search: isArrival ? { arrived: "1" } : undefined,
    });
    prevPhaseRef.current = phase;
  }, [phase, hasSession, sessionLoading, location.pathname, navigate]);
}

function phaseToUrlPrefix(phase: string): string | null {
  switch (phase) {
    case "pre-session":
    case "setup":
    case "prologue":
    case "town-selection":
      return "/";
    case "in-town":
      return "/town";
    case "on-trail":
      return "/trail";
    default:
      return null;
  }
}
