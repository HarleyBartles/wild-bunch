import { useGameSession } from "../state/useGameSession";
import { StartGamePanel } from "../components/StartGamePanel";

export function PreSessionSurface() {
  const { session, loading, gameId, resetToken, notice, error, startNewGame, reloadCurrentGame } = useGameSession();

  return (
    <div className="flow-surface flow-surface--pre-session">
      <div className="flow-hero">
        <h1>Wild Bunch</h1>
        <p className="flow-hero__lead">
          Track a culprit across frontier towns. Read the posters, follow the clues, and bring them in.
        </p>
      </div>
      <StartGamePanel
        session={session}
        busy={loading}
        gameId={gameId}
        resetToken={resetToken}
        onStartGame={startNewGame}
        onRefresh={reloadCurrentGame}
      />
      {notice ? <p className="flow-notice">{notice}</p> : null}
      {error ? <p className="flow-error">{error}</p> : null}
    </div>
  );
}
