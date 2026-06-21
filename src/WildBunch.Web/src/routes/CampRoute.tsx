import { StartGamePanel } from "../components/StartGamePanel";
import { RouteHeader } from "../shell/RouteHeader";
import { resolveRoute } from "../shell/routes";
import { useHashRoute } from "../shell/useHashRoute";
import { useGameSession } from "../state/GameSessionContext";
import { formatGameStatus } from "../ui/formatters";

export function CampRoute() {
  const { navigate } = useHashRoute();
  const { session, gameId, loading, resetToken, notice, error, startNewGame, reloadCurrentGame } =
    useGameSession();

  return (
    <div className="route route--camp">
      <RouteHeader route={resolveRoute("/")} />

      {session ? (
        <section className="panel continue-card">
          <div className="panel-head">
            <h2>Continue the hunt</h2>
            <span className="panel-subtitle">{formatGameStatus(session.status)}</span>
          </div>
          <p className="route-header__copy">
            {session.player.name} is on the trail — day {session.clock.day}, turn {session.clock.turn}.
          </p>
          <div className="button-row">
            <button type="button" className="button" onClick={() => navigate("/hunt")}>
              Resume hunt
            </button>
            <button type="button" className="button button--ghost" onClick={() => navigate("/case")}>
              Open case file
            </button>
          </div>
        </section>
      ) : null}

      <section className="panel panel--wide">
        <div className="panel-head">
          <h2>{gameId ? "Current session" : "Start a new hunt"}</h2>
        </div>
        <StartGamePanel
          session={session}
          busy={loading}
          gameId={gameId}
          resetToken={resetToken}
          onStartGame={async (request) => {
            await startNewGame(request);
            navigate("/hunt");
          }}
          onRefresh={async () => {
            await reloadCurrentGame();
          }}
        />
        {notice ? <div className="notice">{notice}</div> : null}
        {error ? <div className="error">{error}</div> : null}
      </section>
    </div>
  );
}
