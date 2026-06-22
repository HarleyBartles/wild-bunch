import { StartGamePanel } from "../components/StartGamePanel";
import { useGameSession } from "../state/useGameSession";

export function CampRoute() {
  const { session, loading, gameId, resetToken, startNewGame, reloadCurrentGame, notice, error } =
    useGameSession();

  return (
    <section className="panel panel--wide">
      <div className="panel-head">
        <h2>{gameId ? "Current session" : "Start a new hunt"}</h2>
      </div>

      <StartGamePanel
        session={session}
        busy={loading}
        gameId={gameId}
        resetToken={resetToken}
        onStartGame={startNewGame}
        onRefresh={async () => {
          await reloadCurrentGame();
        }}
      />

      {notice ? <div className="notice">{notice}</div> : null}
      {error ? <div className="error">{error}</div> : null}
    </section>
  );
}
