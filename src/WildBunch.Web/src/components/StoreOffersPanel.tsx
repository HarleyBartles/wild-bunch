import { useEffect, useState, type FormEvent } from "react";
import styled from "styled-components";
import type { StoreOfferDto, TownStoreOffersDto } from "../api/types";
import { StatusCard, Stack, Muted, Button } from "./ui/sharedStyled";

const OfferRow = styled.div`
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
`;

const OfferName = styled.span`
  font-weight: 600;
`;

const OfferPrice = styled.span`
  color: var(--muted);
`;

const OfferSpacer = styled.span`
  flex: 1;
`;

const QuantityInput = styled.input`
  width: 64px;
  padding: 6px 8px;
  border-radius: 8px;
  border: 1px solid var(--border-strong);
  background: rgba(0, 0, 0, 0.25);
  color: var(--text);
  font-size: 0.9rem;
`;

const QuantityError = styled.span`
  flex-basis: 100%;
  color: var(--danger, #d16d62);
  font-size: 0.82rem;
`;

const OfferList = styled(Stack)`
  gap: 6px;
`;

interface StoreOffersPanelProps {
  storeOffers: TownStoreOffersDto | null;
  loading: boolean;
  busy: boolean;
  onBuyOffer: (offer: StoreOfferDto, quantity: number) => void;
}

function quantityKey(offer: StoreOfferDto) {
  return offer.itemKind;
}

function StoreOfferRow({
  offer,
  disabled,
  onBuyOffer,
}: {
  offer: StoreOfferDto;
  disabled: boolean;
  onBuyOffer: (offer: StoreOfferDto, quantity: number) => void;
}) {
  const [quantity, setQuantity] = useState("1");
  const [quantityError, setQuantityError] = useState("");

  useEffect(() => {
    setQuantity("1");
  }, [offer.itemKind]);

  function handleBuy(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsedQuantity = Number(quantity);
    if (!quantity.trim() || !Number.isSafeInteger(parsedQuantity) || parsedQuantity < 1) {
      setQuantityError("Enter a whole number of at least 1.");
      return;
    }

    setQuantityError("");
    onBuyOffer(offer, parsedQuantity);
  }

  return (
    <OfferRow as="form" noValidate onSubmit={handleBuy}>
      <OfferName>{offer.displayName}</OfferName>
      <OfferPrice>${offer.price.toFixed(2)}</OfferPrice>
      <OfferSpacer />
      <QuantityInput
        type="number"
        min="1"
        step="1"
        aria-label={`Quantity for ${offer.displayName}`}
        aria-invalid={Boolean(quantityError)}
        aria-describedby={quantityError ? `quantity-error-${offer.itemKind}` : undefined}
        value={quantity}
        onChange={(event) => {
          setQuantity(event.target.value);
          setQuantityError("");
        }}
        disabled={disabled}
      />
      <Button type="submit" disabled={disabled}>
        Buy
      </Button>
      {quantityError ? (
        <QuantityError id={`quantity-error-${offer.itemKind}`} role="alert">
          {quantityError}
        </QuantityError>
      ) : null}
    </OfferRow>
  );
}

export function StoreOffersPanel({
  storeOffers,
  loading,
  busy,
  onBuyOffer,
}: StoreOffersPanelProps) {
  return (
    <StatusCard>
      {loading && storeOffers === null ? <Muted>Loading town offers...</Muted> : null}
      {!loading && storeOffers === null ? <Muted>Town catalog unavailable.</Muted> : null}
      {storeOffers ? (
        <OfferList>
          {storeOffers.offers.length > 0 ? (
            storeOffers.offers.map((offer) => (
              <StoreOfferRow
                key={quantityKey(offer)}
                offer={offer}
                disabled={busy || loading}
                onBuyOffer={onBuyOffer}
              />
            ))
          ) : (
            <Muted>No goods are currently offered at this store.</Muted>
          )}
        </OfferList>
      ) : null}
    </StatusCard>
  );
}
