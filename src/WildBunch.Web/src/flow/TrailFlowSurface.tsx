import { useGameSession } from "../state/useGameSession";
import { TravelPanel } from "../components/TravelPanel";

export function TrailFlowSurface() {
  const { session, gameId, loading, handleTravelTurnResult } = useGameSession();

  if (!session || !gameId) {
    return null;
  }

  return (
    <div className="flow-surface flow-surface--trail">
      <div className="trail-lock-banner" role="status">
        You're on the trail. No turning back until you reach your destination.
      </div>
      <TravelPanel
        gameId={gameId}
        session={session}
        busy={loading}
        onTurnResult={handleTravelTurnResult}
      />
    </div>
  );
}
