import type { InventoryCapabilitiesDto, InventoryDto, InventoryItemDto } from "../api/types";
import { formatCanteenState, formatCapabilityLabel, formatHorseTravelState, formatItemKind } from "../ui/formatters";

interface InventoryPanelProps {
  inventory: InventoryDto;
}

export function InventoryPanel({ inventory }: InventoryPanelProps) {
  return (
    <article className="status-card">
      <h3>Inventory</h3>
      <dl className="stat-list">
        <div>
          <dt>Cash</dt>
          <dd>${inventory.wallet.cash.toFixed(2)}</dd>
        </div>
        <div>
          <dt>Horse state</dt>
          <dd>{formatHorseTravelState(inventory.horseState)}</dd>
        </div>
        <div>
          <dt>Canteen</dt>
          <dd>{formatCanteenState(inventory.canteenState)}</dd>
        </div>
        <div>
          <dt>Loadout items</dt>
          <dd>{inventory.items.length}</dd>
        </div>
        <div>
          <dt>Capabilities</dt>
          <dd>{Object.values(inventory.capabilities).filter(Boolean).length}</dd>
        </div>
      </dl>
      <div className="stack">
        {inventory.items.map((item: InventoryItemDto) => (
          <div key={`${item.kind}-${item.horseState?.hunger ?? "none"}-${item.canteenState?.charges ?? "none"}`} className="compact-item">
            <strong>
              {formatItemKind(item.kind)} x {item.quantity}
            </strong>
            <p>{[item.horseState ? `Horse: ${formatHorseTravelState(item.horseState)}` : null, item.canteenState ? `Canteen: ${formatCanteenState(item.canteenState)}` : null].filter(Boolean).join(" · ") || "No travel state"}</p>
          </div>
        ))}
      </div>
      <div className="tag-row">
        {Object.entries(inventory.capabilities).map(([key, enabled]) => (
          <span key={key} className="tag">
            {formatCapabilityLabel(key as keyof InventoryCapabilitiesDto)}: {enabled ? "Yes" : "No"}
          </span>
        ))}
      </div>
    </article>
  );
}
