import type { TownStoreOffersDto, StoreOfferDto } from "../api/types";
import { formatItemKind, formatStoreOfferAvailability, formatStoreVendorType } from "../ui/formatters";

interface StoreOffersPanelProps {
  storeOffers: TownStoreOffersDto | null;
  loading: boolean;
}

function StoreOfferCard({ offer }: { offer: StoreOfferDto }) {
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
    </div>
  );
}

export function StoreOffersPanel({ storeOffers, loading }: StoreOffersPanelProps) {
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
              storeOffers.offers.map((offer) => <StoreOfferCard key={`${offer.vendorType}-${offer.itemKind}`} offer={offer} />)
            ) : (
              <p className="muted">No store offers are available in this town.</p>
            )}
          </div>
        </>
      ) : null}
    </article>
  );
}
