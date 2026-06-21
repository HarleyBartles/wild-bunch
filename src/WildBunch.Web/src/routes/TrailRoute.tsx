import { TravelPanel } from "../components/TravelPanel";
import { TravelRoutesPanel } from "../components/TravelRoutesPanel";
import { RouteHeader } from "../shell/RouteHeader";
import { resolveRoute } from "../shell/routes";
import { useHashRoute } from "../shell/useHashRoute";
import { useGameSession } from "../state/GameSessionContext";

export function TrailRoute() {
  const { navigate } = useHashRoute();
  const { session, gameId, loading, notice, error, handleTravel, handleTravelTurnResult } =
    useGameSession();

  if (!session) {
    return (
      <div className="route route--trail">
        <RouteHeader route={resolveRoute("/trail")} />
        <section className="panel empty-state">
          <h2>No active hunt</h2>
          <p className="route-header__copy">Make camp and start a hunt before planning the trail.</p>
          <div className="button-row">
            <button type="button" className="button" onClick={() => navigate("/")}>
              Go to camp
            </button>
          </div>
        </section>
      </div>
    );
  }

  const resolvedGameId = gameId ?? session.id;

  return (
    <div className="route route--trail">
      <RouteHeader route={resolveRoute("/trail")} />

      {notice ? <div className="notice">{notice}</div> : null}
      {error ? <div className="error">{error}</div> : null}

      {session.journey ? (
        <TravelPanel
          gameId={resolvedGameId}
          session={session}
          busy={loading}
          onTurnResult={handleTravelTurnResult}
        />
      ) : null}

      <TravelRoutesPanel gameId={resolvedGameId} session={session} busy={loading} onTravel={handleTravel} />
    </div>
  );
}
