import { useEffect } from "react";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { useGamePhase } from "../hooks/useGamePhase";
import { useGameSession } from "../state/useGameSession";

/**
 * Reconciles the URL with the backend-derived game phase.
 * Backend transitions drive navigation — when the phase changes,
 * the hook navigates to the matching route if the URL doesn't already match.
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
      return;
    }

    void navigate({ to: expectedPrefix });
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
