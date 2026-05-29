import { useEffect, useState } from "react";
import type { StoreOfferDto, TownStoreOffersDto } from "../api/types";
import { formatHorseCondition, formatItemKind, formatStoreOfferAvailability, formatStoreVendorType } from "../ui/formatters";

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
    <div className="compact-item">
      <strong>
        {offer.displayName} ${offer.price.toFixed(2)}
      </strong>
      <p>
        {formatItemKind(offer.itemKind)} - {formatStoreVendorType(offer.vendorType)}
      </p>
      <p>
        {formatStoreOfferAvailability(offer.availability)} - {offer.sourceNote}
      </p>
      {offer.horseCondition !== null ? <p>Horse condition: {formatHorseCondition(offer.horseCondition)}</p> : null}
      <div className="button-row">
        <label className="field" style={{ flex: 1 }}>
          <span>Quantity</span>
          <input
            type="number"
            min="1"
            step="1"
            value={quantity}
            onChange={(event) => setQuantity(event.target.value)}
            disabled={disabled}
          />
        </label>
        <button type="button" className="button" onClick={handleBuy} disabled={disabled || offer.availability !== 0}>
          Buy
        </button>
      </div>
    </div>
  );
}

export function StoreOffersPanel({ storeOffers, loading, busy, onBuyOffer }: StoreOffersPanelProps) {
  return (
    <article className="status-card">
      <h3>Store offers</h3>
      {loading && storeOffers === null ? <p className="muted">Loading town offers...</p> : null}
      {!loading && storeOffers === null ? <p className="muted">Town catalog unavailable.</p> : null}
      {storeOffers ? (
        <>
          <dl className="stat-list">
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
          </dl>
          <div className="stack">
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
              <p className="muted">No store offers are available in this town.</p>
            )}
          </div>
        </>
      ) : null}
    </article>
  );
}
