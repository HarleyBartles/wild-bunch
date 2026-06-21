import { CaseFileSurface } from "../components/CaseFileSurface";
import { useGameSession } from "../state/useGameSession";

export function CaseFileRoute() {
  const { journal, loading, error } = useGameSession();

  return (
    <section className="panel panel--wide">
      <div className="panel-head">
        <h2>Case file</h2>
        <span className="panel-subtitle">Investigation board</span>
      </div>
      <p className="muted">A read-only summary of player-known clues, suspects, and warrants.</p>
      <CaseFileSurface journal={journal} loading={loading} error={error} />
    </section>
  );
}
