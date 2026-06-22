import { useGameSession } from "../state/useGameSession";

export function ArrivalSurface() {
  const { session, loading, handleAcknowledgeArrival } = useGameSession();

  if (!session || !session.journey) {
    return null;
  }

  const journey = session.journey;
  const destinationName = journey.destinationTownName;
  const daysTravelled = journey.daysTravelled;

  return (
    <div className="flow-surface flow-surface--arrival">
      <div className="arrival-card">
        <h1>You've arrived in {destinationName}</h1>
        <p className="arrival-lead">
          You put {daysTravelled} day{daysTravelled === 1 ? "" : "s"} of trail behind you.
        </p>
        <button
          type="button"
          className="button button--primary"
          onClick={() => void handleAcknowledgeArrival()}
          disabled={loading}
        >
          {loading ? "Stepping into town..." : "Step into town"}
        </button>
      </div>
    </div>
  );
}
