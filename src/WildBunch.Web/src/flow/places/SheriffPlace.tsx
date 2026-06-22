import { useGameSession } from "../../state/useGameSession";
import { WantedPosterSurface } from "../../components/WantedPosterSurface";

interface SheriffPlaceProps {
  onLeave: () => void;
}

export function SheriffPlace({ onLeave }: SheriffPlaceProps) {
  const {
    session,
    wantedPosters,
    loading,
    busyMode,
    canReadWantedPosters,
    canCheckLocalRecords,
    handleReadWantedPosters,
    handleCheckLocalRecords,
    notice,
    error,
  } = useGameSession();

  if (!session) {
    return null;
  }

  return (
    <div className="flow-surface flow-surface--place">
      <div className="place-header">
        <button type="button" className="back-button" onClick={onLeave}>
          ← Back to town
        </button>
        <h1>Sheriff Office</h1>
      </div>
      <div className="place-body">
        <section className="panel">
          <div className="panel-head">
            <h2>Wanted posters</h2>
          </div>
          <div className="stack">
            <button
              type="button"
              className="button"
              onClick={handleReadWantedPosters}
              disabled={loading || !canReadWantedPosters}
            >
              {busyMode === "reading" ? "Reading..." : "Read wanted posters"}
            </button>
            {wantedPosters.length > 0 ? (
              <WantedPosterSurface wantedPosters={wantedPosters} />
            ) : (
              <p className="muted">No wanted posters read yet.</p>
            )}
          </div>
        </section>
        <section className="panel">
          <div className="panel-head">
            <h2>Local records</h2>
          </div>
          <div className="stack">
            <button
              type="button"
              className="button"
              onClick={handleCheckLocalRecords}
              disabled={loading || !canCheckLocalRecords}
            >
              {busyMode === "investigating" ? "Checking..." : "Check local records"}
            </button>
          </div>
        </section>
        {notice ? <p className="flow-notice">{notice}</p> : null}
        {error ? <p className="flow-error">{error}</p> : null}
      </div>
    </div>
  );
}
