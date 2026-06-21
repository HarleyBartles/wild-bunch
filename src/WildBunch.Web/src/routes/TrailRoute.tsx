import { TravelPanel } from "../components/TravelPanel";
import { TravelRoutesPanel } from "../components/TravelRoutesPanel";
import { useGameSession } from "../state/useGameSession";

export function TrailRoute() {
  const { session, gameId, loading, handleTravel, handleTravelTurnResult } = useGameSession();

  return (
    <>
      {session?.journey ? (
        <section className="panel panel--wide">
          <div className="panel-head">
            <h2>Active journey</h2>
          </div>
          <TravelPanel
            gameId={gameId ?? session.id}
            session={session}
            busy={loading}
            onTurnResult={handleTravelTurnResult}
          />
        </section>
      ) : null}

      <TravelRoutesPanel gameId={gameId ?? session?.id ?? null} session={session} busy={loading} onTravel={handleTravel} />
    </>
  );
}
