import styled from "styled-components";
import { useGameSession } from "../../state/useGameSession";
import { StoreOffersPanel } from "../../components/StoreOffersPanel";
import { InventoryPanel } from "../../components/InventoryPanel";
import { FlowSurface, BackButton } from "../../components/ui/sharedStyled";

const PlaceHeader = styled.header`
  display: grid;
  gap: 12px;
  padding: 24px 0 4px;

  h1 {
    margin: 0;
  }
`;

const PlaceBody = styled.div`
  display: grid;
  gap: 20px;
`;

interface StorePlaceProps {
  onLeave: () => void;
}

export function StorePlace({ onLeave }: StorePlaceProps) {
  const { session, storeOffers, storeOffersLoading, loading, handleBuyOffer } = useGameSession();

  if (!session) {
    return null;
  }

  return (
    <FlowSurface $variant="place">
      <PlaceHeader>
        <BackButton type="button" onClick={onLeave}>
          ← Back to town
        </BackButton>
        <h1>Store</h1>
      </PlaceHeader>
      <PlaceBody>
        <StoreOffersPanel
          storeOffers={storeOffers}
          loading={storeOffersLoading}
          busy={loading}
          onBuyOffer={handleBuyOffer}
        />
        <InventoryPanel inventory={session.inventory} />
      </PlaceBody>
    </FlowSurface>
  );
}
