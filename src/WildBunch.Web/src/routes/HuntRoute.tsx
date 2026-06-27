import { AvailableActionsPanel } from "../components/AvailableActionsPanel";
import { FieldReportPanel } from "../components/FieldReportPanel";
import { Panel, PanelHead, Muted } from "../components/ui/sharedStyled";
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
      <Panel $wide>
        <Muted>Start a hunt from Camp to begin the field report.</Muted>
      </Panel>
    );
  }

  return (
    <>
      <Panel $wide>
        <PanelHead>
          <h2>Field report</h2>
        </PanelHead>
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
      </Panel>

      <AvailableActionsPanel />
    </>
  );
}
