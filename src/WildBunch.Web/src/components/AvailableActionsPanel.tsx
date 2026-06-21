import { AvailableActionKind } from "../api/types";
import { useGameSession } from "../state/GameSessionContext";
import { formatActionKind } from "../ui/formatters";

export function AvailableActionsPanel() {
  const {
    session,
    wantedPosters,
    declaredWantedIdentityHandle,
    setDeclaredWantedIdentityHandle,
    actions,
    gameId,
    busyMode,
    loading,
    canReadWantedPosters,
    canInspectNoticeBoard,
    canCheckLocalRecords,
    canFollowTelegraphLeads,
    canGatherLocalGossip,
    canLookAroundSaloon,
    canConfrontSaloonPersonOfInterest,
    handleReadWantedPosters,
    handleInspectNoticeBoard,
    handleCheckLocalRecords,
    handleFollowTelegraphLeads,
    handleGatherLocalGossip,
    handleLookAroundSaloon,
    handleConfrontSaloonPersonOfInterest,
  } = useGameSession();

  const selectedWantedPoster =
    wantedPosters.find((poster) => poster.posterId === declaredWantedIdentityHandle) ?? wantedPosters[0] ?? null;

  return (
    <section className="panel">
      <div className="panel-head">
        <h2>Available actions</h2>
        <span className="panel-subtitle">{actions.length} fetched</span>
      </div>
      <div className="stack">
        {session?.activeSaloonPersonOfInterest ? (
          <div className="action-row">
            <div>
              <strong>Person of interest spotted</strong>
              <p>{session.activeSaloonPersonOfInterest.descriptor} is waiting in the saloon.</p>
              {wantedPosters.length > 0 ? (
                <label className="field" style={{ marginTop: "0.75rem" }}>
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
                <p className="muted">Read wanted posters to choose the identity you want to declare.</p>
              )}
            </div>
            <button
              type="button"
              className="button"
              onClick={handleConfrontSaloonPersonOfInterest}
              disabled={!gameId || loading || !canConfrontSaloonPersonOfInterest}
            >
              {busyMode === "investigating"
                ? "Taking in..."
                : selectedWantedPoster
                  ? `Take ${session.activeSaloonPersonOfInterest.descriptor} to sheriff as ${selectedWantedPoster.targetDisplayName}`
                  : `Take ${session.activeSaloonPersonOfInterest.descriptor} to sheriff`}
            </button>
          </div>
        ) : null}
        {actions.length > 0 ? (
          actions.map((action) => (
            <div key={`${action.kind}-${action.label}`} className="action-row">
              <div>
                <strong>{action.label}</strong>
                <p>{formatActionKind(action.kind)}</p>
              </div>
              {action.kind === AvailableActionKind.ReadWantedPosters ? (
                <button
                  type="button"
                  className="button"
                  onClick={handleReadWantedPosters}
                  disabled={!gameId || loading || !canReadWantedPosters}
                >
                  {busyMode === "reading" ? "Reading..." : "Read wanted posters"}
                </button>
              ) : action.kind === AvailableActionKind.InspectNoticeBoard ? (
                <button
                  type="button"
                  className="button"
                  onClick={handleInspectNoticeBoard}
                  disabled={!gameId || loading || !canInspectNoticeBoard}
                >
                  {busyMode === "investigating" ? "Inspecting..." : "Inspect notice board"}
                </button>
              ) : action.kind === AvailableActionKind.CheckSheriffRecords ? (
                <button
                  type="button"
                  className="button"
                  onClick={handleCheckLocalRecords}
                  disabled={!gameId || loading || !canCheckLocalRecords}
                >
                  {busyMode === "investigating" ? "Checking..." : "Check local records"}
                </button>
              ) : action.kind === AvailableActionKind.FollowTelegraphLeads ? (
                <button
                  type="button"
                  className="button"
                  onClick={handleFollowTelegraphLeads}
                  disabled={!gameId || loading || !canFollowTelegraphLeads}
                >
                  {busyMode === "investigating" ? "Following..." : "Follow telegraph leads"}
                </button>
              ) : action.kind === AvailableActionKind.GatherLocalGossip ? (
                <button
                  type="button"
                  className="button"
                  onClick={handleGatherLocalGossip}
                  disabled={!gameId || loading || !canGatherLocalGossip}
                >
                  {busyMode === "investigating" ? "Gathering..." : "Gather local gossip"}
                </button>
              ) : action.kind === AvailableActionKind.LookAroundSaloon ? (
                <button
                  type="button"
                  className="button"
                  onClick={handleLookAroundSaloon}
                  disabled={!gameId || loading || !canLookAroundSaloon}
                >
                  {busyMode === "investigating" ? "Looking..." : "Look around saloon"}
                </button>
              ) : null}
            </div>
          ))
        ) : (
          <p className="muted">Actions will appear here after a game loads.</p>
        )}
      </div>
    </section>
  );
}
