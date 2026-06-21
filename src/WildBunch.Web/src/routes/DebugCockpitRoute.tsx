import { useCallback, useState } from "react";
import { StartGamePanel } from "../components/StartGamePanel";
import { TravelRoutesPanel } from "../components/TravelRoutesPanel";
import { CockpitOverlayFrame } from "../components/CockpitOverlayFrame";
import { CaseFileSurface } from "../components/CaseFileSurface";
import { FieldReportPanel } from "../components/FieldReportPanel";
import { LogPanel } from "../components/LogPanel";
import { AvailableActionsPanel } from "../components/AvailableActionsPanel";
import { formatGameStatus } from "../ui/formatters";
import { useGameSession } from "../state/GameSessionContext";

export function DebugCockpitRoute() {
  const {
    session,
    journal,
    gameId,
    currentTown,
    cockpitMode,
    loading,
    notice,
    error,
    resetToken,
    startNewGame,
    reloadCurrentGame,
    handleTravelTurnResult,
    handleTravel,
    handleReset,
    storeOffers,
    storeOffersLoading,
    handleBuyOffer,
  } = useGameSession();
  const [isCaseFileOpen, setIsCaseFileOpen] = useState(false);
  const openCaseFile = useCallback(() => setIsCaseFileOpen(true), []);
  const closeCaseFile = useCallback(() => setIsCaseFileOpen(false), []);

  const gameStateLabel = session
    ? `${formatGameStatus(session.status)} | ${cockpitMode === "travel" ? "Travel diary" : "Cockpit"}`
    : "Idle";

  return (
    <div className="cockpit">
      <header className="hero">
        <div>
          <p className="eyebrow">Wild Bunch · Dev tools</p>
          <h1>Field cockpit</h1>
          <p className="hero-copy">
            A dense developer cockpit over the existing game loop: start a hunt, travel between towns, read the board, and keep the case file in view. Not a player-facing route.
          </p>
        </div>
        <div className="hero-metrics">
          <span className="metric">
            <strong>{session ? session.player.name : "No active hunt"}</strong>
            <small>Player</small>
          </span>
          <span className="metric">
            <strong>{session ? gameStateLabel : "Idle"}</strong>
            <small>Status</small>
          </span>
          <span className="metric">
            <strong>{session ? `Day ${session.clock.day}, Turn ${session.clock.turn}` : "-"}</strong>
            <small>Clock</small>
          </span>
        </div>
      </header>

      <main className="layout">
        <section className="panel panel--wide">
          <div className="panel-head">
            <h2>{gameId ? "Current session" : "Start a new hunt"}</h2>
            <div className="panel-actions">
              <button
                type="button"
                className="button button--ghost"
                onClick={openCaseFile}
                disabled={!journal && !loading}
              >
                Open case file
              </button>
              <button
                type="button"
                className="button button--ghost"
                onClick={() => {
                  setIsCaseFileOpen(false);
                  handleReset();
                }}
              >
                Reset
              </button>
            </div>
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

          {session ? (
            <FieldReportPanel
              session={session}
              currentTown={currentTown}
              storeOffers={storeOffers}
              storeOffersLoading={storeOffersLoading}
              busy={loading}
              gameId={gameId ?? session.id}
              onBuyOffer={handleBuyOffer}
              onTurnResult={handleTravelTurnResult}
            />
          ) : null}
        </section>

        <AvailableActionsPanel />

        <TravelRoutesPanel gameId={gameId ?? session?.id ?? null} session={session} busy={loading} onTravel={handleTravel} />

        <LogPanel journal={journal} sessionLogEntries={session?.logEntries ?? []} />
      </main>

      <CockpitOverlayFrame
        open={isCaseFileOpen}
        eyebrow="Case file"
        title="Investigation board"
        description="A read-only summary of player-known clues, suspects, and warrants."
        onClose={closeCaseFile}
      >
        <CaseFileSurface journal={journal} loading={loading} error={error} />
      </CockpitOverlayFrame>
    </div>
  );
}
