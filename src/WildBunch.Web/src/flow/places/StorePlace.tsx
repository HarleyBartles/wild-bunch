import styled from "styled-components";
import { useNavigate } from "@tanstack/react-router";
import { useGameSession } from "../../state/useGameSession";
import { StoreOffersPanel } from "../../components/StoreOffersPanel";
import { InventoryPanel } from "../../components/InventoryPanel";
import { FlowSurface, BackButton, FlowNotice, FlowError } from "../../components/ui/sharedStyled";

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

export function StorePlace() {
  const navigate = useNavigate();
  const { session, storeOffers, storeOffersLoading, loading, handleBuyOffer, notice, error } = useGameSession();

  if (!session) {
    return null;
  }

  return (
    <FlowSurface $variant="place">
      <PlaceHeader>
        <BackButton type="button" onClick={() => void navigate({ to: "/town" })}>
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
        {notice ? <FlowNotice>{notice}</FlowNotice> : null}
        {error ? <FlowError>{error}</FlowError> : null}
      </PlaceBody>
    </FlowSurface>
  );
}
