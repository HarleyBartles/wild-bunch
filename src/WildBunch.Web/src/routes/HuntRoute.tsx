import { AvailableActionsPanel } from "../components/AvailableActionsPanel";
import { FieldReportPanel } from "../components/FieldReportPanel";
import { useGameSession } from "../state/useGameSession";

export function HuntRoute() {
  const {
    session,
    currentTown,
    storeOffers,
    storeOffersLoading,
    loading,
    gameId,
    handleBuyOffer,
    handleTravelTurnResult,
  } = useGameSession();

  if (!session) {
    return (
      <section className="panel panel--wide">
        <p className="muted">Start a hunt from Camp to begin the field report.</p>
      </section>
    );
  }

  return (
    <>
      <section className="panel panel--wide">
        <div className="panel-head">
          <h2>Field report</h2>
        </div>
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
      </section>

      <AvailableActionsPanel />
    </>
  );
}
