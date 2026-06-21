import { AvailableActionsPanel } from "../components/AvailableActionsPanel";
import { FieldReportPanel } from "../components/FieldReportPanel";
import { RouteHeader } from "../shell/RouteHeader";
import { resolveRoute } from "../shell/routes";
import { useHashRoute } from "../shell/useHashRoute";
import { useGameSession } from "../state/GameSessionContext";

export function HuntRoute() {
  const { navigate } = useHashRoute();
  const {
    session,
    gameId,
    currentTown,
    loading,
    notice,
    error,
    storeOffers,
    storeOffersLoading,
    handleBuyOffer,
    handleTravelTurnResult,
  } = useGameSession();

  if (!session) {
    return (
      <div className="route route--hunt">
        <RouteHeader route={resolveRoute("/hunt")} />
        <section className="panel empty-state">
          <h2>No active hunt</h2>
          <p className="route-header__copy">Make camp and start a hunt to take field actions.</p>
          <div className="button-row">
            <button type="button" className="button" onClick={() => navigate("/")}>
              Go to camp
            </button>
          </div>
        </section>
      </div>
    );
  }

  return (
    <div className="route route--hunt">
      <RouteHeader route={resolveRoute("/hunt")} />

      {notice ? <div className="notice">{notice}</div> : null}
      {error ? <div className="error">{error}</div> : null}

      <div className="route-grid">
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
        <AvailableActionsPanel />
      </div>
    </div>
  );
}
