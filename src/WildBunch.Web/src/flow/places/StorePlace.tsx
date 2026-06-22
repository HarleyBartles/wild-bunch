import { useGameSession } from "../../state/useGameSession";
import { StoreOffersPanel } from "../../components/StoreOffersPanel";
import { InventoryPanel } from "../../components/InventoryPanel";

interface StorePlaceProps {
  onLeave: () => void;
}

export function StorePlace({ onLeave }: StorePlaceProps) {
  const { session, storeOffers, storeOffersLoading, loading, handleBuyOffer } = useGameSession();

  if (!session) {
    return null;
  }

  return (
    <div className="flow-surface flow-surface--place">
      <div className="place-header">
        <button type="button" className="back-button" onClick={onLeave}>
          ← Back to town
        </button>
        <h1>Store</h1>
      </div>
      <div className="place-body">
        <StoreOffersPanel
          storeOffers={storeOffers}
          loading={storeOffersLoading}
          busy={loading}
          onBuyOffer={handleBuyOffer}
        />
        <InventoryPanel inventory={session.inventory} />
      </div>
    </div>
  );
}
