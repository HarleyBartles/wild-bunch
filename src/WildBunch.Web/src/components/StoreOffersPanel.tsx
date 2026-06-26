import { useEffect, useState } from "react";
import type { StoreOfferDto, TownStoreOffersDto } from "../api/types";
import { formatItemKind, formatStoreOfferAvailability, formatStoreVendorType } from "../ui/formatters";
import {
  StatusCard,
  StatList,
  Stack,
  ItemCard,
  Muted,
  Field,
  Button,
} from "./ui/sharedStyled";

interface StoreOffersPanelProps {
  storeOffers: TownStoreOffersDto | null;
  loading: boolean;
  busy: boolean;
  onBuyOffer: (offer: StoreOfferDto, quantity: number) => Promise<void>;
}

function quantityKey(offer: StoreOfferDto) {
  return `${offer.vendorType}-${offer.itemKind}`;
}

function StoreOfferCard({
  offer,
  disabled,
  onBuyOffer,
}: {
  offer: StoreOfferDto;
  disabled: boolean;
  onBuyOffer: (offer: StoreOfferDto, quantity: number) => Promise<void>;
}) {
  const [quantity, setQuantity] = useState("1");

  useEffect(() => {
    setQuantity("1");
  }, [offer.itemKind, offer.vendorType]);

  async function handleBuy() {
    const parsedQuantity = Number.parseInt(quantity, 10);
    await onBuyOffer(offer, Number.isFinite(parsedQuantity) ? parsedQuantity : 1);
  }

  return (
    <ItemCard>
      <strong>
        {offer.displayName} ${offer.price.toFixed(2)}
      </strong>
      <p style={{ margin: "4px 0 0", fontSize: "0.88rem", color: "var(--muted)" }}>
        {formatItemKind(offer.itemKind)} - {formatStoreVendorType(offer.vendorType)}
      </p>
      <p style={{ margin: "2px 0 0", fontSize: "0.84rem", color: "var(--muted)" }}>
        {formatStoreOfferAvailability(offer.availability)} - {offer.sourceNote}
      </p>
      <div style={{ display: "flex", gap: "10px", marginTop: "12px", alignItems: "flex-end" }}>
        <Field as="label" style={{ flex: 1 }}>
          <span>Quantity</span>
          <input
            type="number"
            min="1"
            step="1"
            value={quantity}
            onChange={(event) => setQuantity(event.target.value)}
            disabled={disabled}
          />
        </Field>
        <Button
          type="button"
          onClick={handleBuy}
          disabled={disabled || offer.availability !== 0}
        >
          Buy
        </Button>
      </div>
    </ItemCard>
  );
}

export function StoreOffersPanel({ storeOffers, loading, busy, onBuyOffer }: StoreOffersPanelProps) {
  return (
    <StatusCard>
      <h3>Store offers</h3>
      {loading && storeOffers === null ? <Muted>Loading town offers...</Muted> : null}
      {!loading && storeOffers === null ? <Muted>Town catalog unavailable.</Muted> : null}
      {storeOffers ? (
        <>
          <StatList>
            <div>
              <dt>Town</dt>
              <dd>{storeOffers.townName}</dd>
            </div>
            <div>
              <dt>Town id</dt>
              <dd>{storeOffers.townId}</dd>
            </div>
            <div>
              <dt>Catalog</dt>
              <dd>{storeOffers.available ? "Available" : "Unavailable"}</dd>
            </div>
            <div>
              <dt>Source</dt>
              <dd>{storeOffers.sourceNote}</dd>
            </div>
          </StatList>
          <Stack style={{ marginTop: "16px" }}>
            {storeOffers.offers.length > 0 ? (
              storeOffers.offers.map((offer) => (
                <StoreOfferCard
                  key={quantityKey(offer)}
                  offer={offer}
                  disabled={busy || loading}
                  onBuyOffer={onBuyOffer}
                />
              ))
            ) : (
              <Muted>No store offers are available in this town.</Muted>
            )}
          </Stack>
        </>
      ) : null}
    </StatusCard>
  );
}
