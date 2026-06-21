import { WantedPosterSurface } from "../components/WantedPosterSurface";
import { useGameSession } from "../state/useGameSession";

export function WantedRoute() {
  const { wantedPosters } = useGameSession();

  return (
    <section className="panel panel--wide">
      <div className="panel-head">
        <h2>Wanted posters</h2>
        <span className="panel-subtitle">{wantedPosters.length} posted</span>
      </div>
      <WantedPosterSurface wantedPosters={wantedPosters} />
    </section>
  );
}
