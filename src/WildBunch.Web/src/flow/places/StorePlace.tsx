import styled from "styled-components";
import { useNavigate } from "@tanstack/react-router";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useGameSession } from "../../state/useGameSession";
import { useTownStoreOffers } from "../../hooks/useTownStoreOffers";
import { StoreOffersPanel } from "../../components/StoreOffersPanel";
import { InventoryPanel } from "../../components/InventoryPanel";
import { FlowSurface, BackButton, FlowNotice, FlowError } from "../../components/ui/sharedStyled";
import { buyStoreItem } from "../../api/wildBunchApi";
import type { StoreOfferDto } from "../../api/types";
import { gameSessionQueryKey } from "../../state/gameSessionQuery";
import { refreshGameResources } from "../../state/gameResourceQueries";

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
  const { session, gameId, currentTown, loading } = useGameSession();

  if (!session) {
    return null;
  }

  const townId = currentTown?.id ?? null;
  return (
    <StorePlaceForPlaythrough
      key={`${gameId ?? "no-game"}:${townId ?? "no-town"}`}
      session={session}
      gameId={gameId}
      townId={townId}
      loading={loading}
    />
  );
}

function StorePlaceForPlaythrough({
  session,
  gameId,
  townId,
  loading,
}: {
  session: NonNullable<ReturnType<typeof useGameSession>["session"]>;
  gameId: string | null;
  townId: string | null;
  loading: boolean;
}) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { storeOffers, loading: storeOffersLoading } = useTownStoreOffers(gameId, townId);
  const purchaseMutation = useMutation({
    mutationFn: ({
      activeGameId,
      activeTownId,
      offer,
      quantity,
    }: {
      activeGameId: string;
      activeTownId: string;
      offer: StoreOfferDto;
      quantity: number;
    }) => buyStoreItem(activeGameId, activeTownId, { itemKind: offer.itemKind, quantity }),
    onSuccess: async (result) => {
      queryClient.setQueryData(
        gameSessionQueryKey(result.currentSession.id),
        result.currentSession,
      );
      await refreshGameResources(queryClient, result.currentSession.id);
    },
  });
  const handleBuyOffer = (offer: StoreOfferDto, quantity: number) => {
    if (!gameId || !townId) {
      return;
    }
    purchaseMutation.mutate({ activeGameId: gameId, activeTownId: townId, offer, quantity });
  };

  const purchaseResult = purchaseMutation.data;
  const purchaseNotice = purchaseResult?.success ? purchaseResult.message : "";
  const purchaseError =
    purchaseResult && !purchaseResult.success
      ? purchaseResult.message
      : purchaseMutation.isError
        ? purchaseMutation.error instanceof Error
          ? purchaseMutation.error.message
          : "Unable to buy the selected item."
        : "";

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
          busy={loading || purchaseMutation.isPending}
          onBuyOffer={handleBuyOffer}
        />
        <InventoryPanel inventory={session.inventory} />
        {purchaseNotice ? <FlowNotice>{purchaseNotice}</FlowNotice> : null}
        {purchaseError ? <FlowError>{purchaseError}</FlowError> : null}
      </PlaceBody>
    </FlowSurface>
  );
}
