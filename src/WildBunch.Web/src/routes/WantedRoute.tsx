import { WantedPosterSurface } from "../components/WantedPosterSurface";
import { RouteHeader } from "../shell/RouteHeader";
import { resolveRoute } from "../shell/routes";
import { useGameSession } from "../state/GameSessionContext";

export function WantedRoute() {
  const {
    journal,
    gameId,
    loading,
    busyMode,
    canReadWantedPosters,
    handleReadWantedPosters,
  } = useGameSession();

  const posters = journal?.caseFile.wantedPosters ?? [];

  return (
    <div className="route route--wanted">
      <RouteHeader
        route={resolveRoute("/wanted")}
        actions={
          <button
            type="button"
            className="button"
            onClick={handleReadWantedPosters}
            disabled={!gameId || loading || !canReadWantedPosters}
          >
            {busyMode === "reading" ? "Reading..." : "Read wanted posters"}
          </button>
        }
      />
      <section className="panel panel--wide route-surface">
        {journal ? (
          <WantedPosterSurface wantedPosters={posters} />
        ) : (
          <p className="muted">Wanted posters appear here once a hunt is loaded.</p>
        )}
      </section>
    </div>
  );
}
