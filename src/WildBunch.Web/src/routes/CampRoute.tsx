import { StartGamePanel } from "../components/StartGamePanel";
import { Panel, PanelHead, Notice, Error } from "../components/ui/sharedStyled";
import { useGameSession } from "../state/useGameSession";

export function CampRoute() {
  const { session, loading, gameId, resetToken, startNewGame, reloadCurrentGame, notice, error } =
    useGameSession();

  return (
    <Panel $wide>
      <PanelHead>
        <h2>{gameId ? "Current session" : "Start a new hunt"}</h2>
      </PanelHead>

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

      {notice ? <Notice>{notice}</Notice> : null}
      {error ? <Error>{error}</Error> : null}
    </Panel>
  );
}
