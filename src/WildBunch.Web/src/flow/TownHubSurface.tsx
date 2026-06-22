import { useGameSession } from "../state/useGameSession";
import { AvailableActionKind } from "../api/types";
import type { TownPlace } from "./GameFlowRouter";
import { StorePlace } from "./places/StorePlace";
import { SheriffPlace } from "./places/SheriffPlace";
import { SaloonPlace } from "./places/SaloonPlace";
import { TravelPrepSurface } from "./TravelPrepSurface";

interface TownHubSurfaceProps {
  activePlace: TownPlace;
  onPlaceChange: (place: TownPlace) => void;
}

export function TownHubSurface({ activePlace, onPlaceChange }: TownHubSurfaceProps) {
  const { session, currentTown, actions } = useGameSession();

  if (!session) {
    return null;
  }

  // If a place is active, render the place surface with a back button
  if (activePlace === "store") {
    return <StorePlace onLeave={() => onPlaceChange(null)} />;
  }
  if (activePlace === "sheriff") {
    return <SheriffPlace onLeave={() => onPlaceChange(null)} />;
  }
  if (activePlace === "saloon") {
    return <SaloonPlace onLeave={() => onPlaceChange(null)} />;
  }
  if (activePlace === "trailhead") {
    return <TravelPrepSurface onBack={() => onPlaceChange(null)} />;
  }

  // Otherwise render the town hub with place cards
  const hasStore = actions.some((a) => a.kind === AvailableActionKind.BuySupplies);
  const hasSheriff =
    actions.some((a) => a.kind === AvailableActionKind.ReadWantedPosters) ||
    actions.some((a) => a.kind === AvailableActionKind.CheckSheriffRecords);
  const hasSaloon = actions.some((a) => a.kind === AvailableActionKind.LookAroundSaloon);
  const hasTrailhead = actions.some((a) => a.kind === AvailableActionKind.Travel);

  const townName = currentTown?.name ?? session.player.currentTownId;

  return (
    <div className="flow-surface flow-surface--town-hub">
      <div className="town-hub-header">
        <h1>{townName}</h1>
        <p className="town-hub-lead">Where to next?</p>
      </div>
      <div className="town-hub-grid">
        {hasStore ? (
          <button
            type="button"
            className="place-card"
            onClick={() => onPlaceChange("store")}
          >
            <div className="place-card__icon" aria-hidden="true">📦</div>
            <div className="place-card__body">
              <strong>Store</strong>
              <p>Buy supplies, food, and gear.</p>
            </div>
          </button>
        ) : null}
        {hasSheriff ? (
          <button
            type="button"
            className="place-card"
            onClick={() => onPlaceChange("sheriff")}
          >
            <div className="place-card__icon" aria-hidden="true">⭐</div>
            <div className="place-card__body">
              <strong>Sheriff Office</strong>
              <p>Read wanted posters and check records.</p>
            </div>
          </button>
        ) : null}
        {hasSaloon ? (
          <button
            type="button"
            className="place-card"
            onClick={() => onPlaceChange("saloon")}
          >
            <div className="place-card__icon" aria-hidden="true">🥃</div>
            <div className="place-card__body">
              <strong>Saloon</strong>
              <p>Look around, gather gossip, confront a suspect.</p>
            </div>
          </button>
        ) : null}
        {hasTrailhead ? (
          <button
            type="button"
            className="place-card place-card--trailhead"
            onClick={() => onPlaceChange("trailhead")}
          >
            <div className="place-card__icon" aria-hidden="true">🐎</div>
            <div className="place-card__body">
              <strong>Hit the trail</strong>
              <p>Ride to the next town.</p>
            </div>
          </button>
        ) : null}
      </div>
    </div>
  );
}
