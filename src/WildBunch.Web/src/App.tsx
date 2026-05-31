import { buyStoreItem } from "./api/wildBunchApi";
import { AvailableActionKind, type TownStoreOffersDto } from "./api/types";
import { StartGamePanel } from "./components/StartGamePanel";
import { TravelRoutesPanel } from "./components/TravelRoutesPanel";
import { CaseFilePanel } from "./components/CaseFilePanel";
import { FieldReportPanel } from "./components/FieldReportPanel";
import { LogPanel } from "./components/LogPanel";
import { formatActionKind, formatGameStatus } from "./ui/formatters";
import { useCurrentGameSession } from "./hooks/useCurrentGameSession";
import { useTownStoreOffers } from "./hooks/useTownStoreOffers";

export default function App() {
  const {
    session,
    journal,
    actions,
    gameId,
    currentTown,
    cockpitMode,
    busyMode,
    loading,
    notice,
    error,
    resetToken,
    canReadWantedPosters,
    canInspectNoticeBoard,
    canCheckSheriffRecords,
    canFollowTelegraphLeads,
    canGatherLocalGossip,
    startNewGame,
    reloadCurrentGame,
    handleTravelTurnResult,
    handleTravel,
    handleReadWantedPosters,
    handleInspectNoticeBoard,
    handleCheckSheriffRecords,
    handleFollowTelegraphLeads,
    handleGatherLocalGossip,
    handleReset,
    setSession,
    setNotice,
    setError,
  } = useCurrentGameSession();

  const { storeOffers, loading: storeOffersLoading, refreshStoreOffers } = useTownStoreOffers(
    gameId,
    currentTown?.id,
  );

  async function handleBuyOffer(offer: TownStoreOffersDto["offers"][number], quantity: number) {
    if (!gameId || !currentTown?.id) {
      return;
    }

    setNotice("");
    setError("");

    try {
      const result = await buyStoreItem(gameId, currentTown.id, {
        vendorType: offer.vendorType,
        itemKind: offer.itemKind,
        quantity,
      });

      setSession(result.currentSession);

      if (result.success) {
        await reloadCurrentGame(gameId);
        refreshStoreOffers();
        setNotice(result.message);
        setError("");
        return;
      }

      setNotice("");
      setError(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to buy the selected item.");
    }
  }

  const gameStateLabel = session ? `${formatGameStatus(session.status)} | ${cockpitMode === "travel" ? "Travel diary" : "Cockpit"}` : "Idle";

  return (
    <div className="app-shell">
      <header className="hero">
        <div>
          <p className="eyebrow">Wild Bunch</p>
          <h1>Field cockpit</h1>
          <p className="hero-copy">
            A thin command surface over the existing game loop: start a hunt, travel between towns, read the board, and keep the case file in view.
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
              <button type="button" className="button button--ghost" onClick={handleReset}>
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

        <section className="panel">
          <div className="panel-head">
            <h2>Available actions</h2>
            <span className="panel-subtitle">{actions.length} fetched</span>
          </div>
          <div className="stack">
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
                      onClick={handleCheckSheriffRecords}
                      disabled={!gameId || loading || !canCheckSheriffRecords}
                    >
                      {busyMode === "investigating" ? "Checking..." : "Check sheriff records"}
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
                  ) : null}
                </div>
              ))
            ) : (
              <p className="muted">Actions will appear here after a game loads.</p>
            )}
          </div>
        </section>

        <TravelRoutesPanel gameId={gameId ?? session?.id ?? null} session={session} busy={loading} onTravel={handleTravel} />

        <CaseFilePanel journal={journal} />

        <LogPanel journal={journal} sessionLogEntries={session?.logEntries ?? []} />
      </main>
    </div>
  );
}
