import { useEffect } from "react";
import { useLocation } from "@tanstack/react-router";
import { useGamePhase } from "../hooks/useGamePhase";
import { useSetDevSurface, type DevSurface } from "../dev/DevSurfaceContext";

/**
 * Maps the current game phase + URL route to a DevSurface value
 * and pushes it into DevSurfaceContext. Replaces the mapping
 * that lived in GameFlowRouter before its removal.
 */
export function useDevSurfaceSync(): void {
  const { phase } = useGamePhase();
  const location = useLocation();
  const setDevSurface = useSetDevSurface();

  useEffect(() => {
    const surface = deriveDevSurface(phase, location.pathname);
    setDevSurface(surface);
  }, [phase, location.pathname, setDevSurface]);
}

function deriveDevSurface(phase: string, pathname: string): DevSurface {
  if (phase === "pre-session" || phase === "setup" || phase === "prologue" || phase === "town-selection") {
    return "pre-session";
  }
  if (phase === "on-trail") {
    return "trail";
  }
  if (phase === "in-town") {
    if (pathname === "/town/store") return "store";
    if (pathname === "/town/sheriff") return "sheriff";
    if (pathname === "/town/saloon") return "saloon";
    if (pathname === "/town/trailhead") return "trailhead";
    return "town";
  }
  return "pre-session";
}
