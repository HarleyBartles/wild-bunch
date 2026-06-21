import { CaseFileSurface } from "../components/CaseFileSurface";
import { RouteHeader } from "../shell/RouteHeader";
import { resolveRoute } from "../shell/routes";
import { useGameSession } from "../state/GameSessionContext";

export function CaseFileRoute() {
  const { journal, loading, error } = useGameSession();

  return (
    <div className="route route--case">
      <RouteHeader route={resolveRoute("/case")} />
      <section className="panel panel--wide route-surface">
        <CaseFileSurface journal={journal} loading={loading} error={error} />
      </section>
    </div>
  );
}
