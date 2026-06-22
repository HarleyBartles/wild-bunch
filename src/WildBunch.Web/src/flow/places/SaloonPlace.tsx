import { useGameSession } from "../../state/useGameSession";

interface SaloonPlaceProps {
  onLeave: () => void;
}

export function SaloonPlace({ onLeave }: SaloonPlaceProps) {
  const {
    session,
    wantedPosters,
    declaredWantedIdentityHandle,
    setDeclaredWantedIdentityHandle,
    loading,
    busyMode,
    gameId,
    selectedWantedPoster,
    canLookAroundSaloon,
    canGatherLocalGossip,
    canConfrontSaloonPersonOfInterest,
    handleLookAroundSaloon,
    handleGatherLocalGossip,
    handleConfrontSaloonPersonOfInterest,
    notice,
    error,
  } = useGameSession();

  if (!session) {
    return null;
  }

  const personOfInterest = session.activeSaloonPersonOfInterest;

  return (
    <div className="flow-surface flow-surface--place">
      <div className="place-header">
        <button type="button" className="back-button" onClick={onLeave}>
          ← Back to town
        </button>
        <h1>Saloon</h1>
      </div>
      <div className="place-body">
        <section className="panel">
          <div className="panel-head">
            <h2>Saloon floor</h2>
          </div>
          <div className="stack">
            <button
              type="button"
              className="button"
              onClick={handleLookAroundSaloon}
              disabled={loading || !canLookAroundSaloon}
            >
              {busyMode === "investigating" ? "Looking..." : "Look around"}
            </button>
            <button
              type="button"
              className="button"
              onClick={handleGatherLocalGossip}
              disabled={loading || !canGatherLocalGossip}
            >
              {busyMode === "investigating" ? "Gathering..." : "Gather gossip"}
            </button>
          </div>
        </section>
        {personOfInterest ? (
          <section className="panel">
            <div className="panel-head">
              <h2>Person of interest</h2>
            </div>
            <div className="stack">
              <p>
                <strong>{personOfInterest.descriptor}</strong> is waiting in the saloon.
              </p>
              {wantedPosters.length > 0 ? (
                <label className="field">
                  <span>Declare wanted identity</span>
                  <select
                    value={declaredWantedIdentityHandle}
                    onChange={(event) => setDeclaredWantedIdentityHandle(event.target.value)}
                    disabled={loading}
                  >
                    {wantedPosters.map((poster) => (
                      <option key={poster.posterId} value={poster.posterId}>
                        {poster.targetDisplayName}
                      </option>
                    ))}
                  </select>
                </label>
              ) : (
                <p className="muted">Read wanted posters at the Sheriff Office to choose the identity you want to declare.</p>
              )}
              <button
                type="button"
                className="button"
                onClick={handleConfrontSaloonPersonOfInterest}
                disabled={!gameId || loading || !canConfrontSaloonPersonOfInterest}
              >
                {busyMode === "investigating"
                  ? "Taking in..."
                  : selectedWantedPoster
                    ? `Take ${personOfInterest.descriptor} to sheriff as ${selectedWantedPoster.targetDisplayName}`
                    : `Take ${personOfInterest.descriptor} to sheriff`}
              </button>
            </div>
          </section>
        ) : null}
        {notice ? <p className="flow-notice">{notice}</p> : null}
        {error ? <p className="flow-error">{error}</p> : null}
      </div>
    </div>
  );
}
