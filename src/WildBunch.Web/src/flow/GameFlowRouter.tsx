import { useEffect, useState } from "react";
import { useGamePhase } from "../hooks/useGamePhase";
import { useSetDevSurface } from "../dev/DevSurfaceContext";
import type { DevSurface } from "../dev/DevSurfaceContext";
import { PreSessionSurface } from "./PreSessionSurface";
import { TownHubSurface } from "./TownHubSurface";
import { TrailFlowSurface } from "./TrailFlowSurface";

export type TownPlace = "store" | "sheriff" | "saloon" | "trailhead" | null;

const placeToSurface: Record<Exclude<TownPlace, null>, DevSurface> = {
  store: "store",
  sheriff: "sheriff",
  saloon: "saloon",
  trailhead: "trailhead",
};

export function GameFlowRouter() {
  const { phase } = useGamePhase();
  const [activePlace, setActivePlace] = useState<TownPlace>(null);
  const setDevSurface = useSetDevSurface();

  useEffect(() => {
    if (phase === "pre-session" || phase === "setup" || phase === "prologue" || phase === "town-selection") {
      setDevSurface("pre-session");
    } else if (phase === "on-trail") {
      setDevSurface("trail");
    } else if (phase === "in-town") {
      setDevSurface(activePlace ? placeToSurface[activePlace] : "town");
    }
  }, [phase, activePlace, setDevSurface]);

  useEffect(() => {
    setActivePlace(null);
  }, [phase]);

  switch (phase) {
    case "pre-session":
    case "setup":
    case "prologue":
    case "town-selection":
      return <PreSessionSurface />;
    case "in-town":
      return (
        <TownHubSurface
          activePlace={activePlace}
          onPlaceChange={setActivePlace}
        />
      );
    case "on-trail":
      return <TrailFlowSurface />;
    default:
      return <PreSessionSurface />;
  }
}
