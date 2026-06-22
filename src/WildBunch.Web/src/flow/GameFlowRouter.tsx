import { useState } from "react";
import { useGamePhase } from "../hooks/useGamePhase";
import { PreSessionSurface } from "./PreSessionSurface";
import { TownHubSurface } from "./TownHubSurface";
import { TrailFlowSurface } from "./TrailFlowSurface";
import { ArrivalSurface } from "./ArrivalSurface";

export type TownPlace = "store" | "sheriff" | "saloon" | "trailhead" | null;

export function GameFlowRouter() {
  const { phase } = useGamePhase();
  const [activePlace, setActivePlace] = useState<TownPlace>(null);

  switch (phase) {
    case "pre-session":
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
    case "arrival":
      return <ArrivalSurface />;
    default:
      return <PreSessionSurface />;
  }
}
