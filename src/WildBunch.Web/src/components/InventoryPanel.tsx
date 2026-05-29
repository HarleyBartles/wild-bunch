import type { InventoryCapabilitiesDto, InventoryDto, InventoryItemDto } from "../api/types";
import { formatCapabilityLabel, formatHorseCondition, formatItemKind } from "../ui/formatters";

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
          <dt>Horse condition</dt>
          <dd>{inventory.horseCondition === null ? "None" : formatHorseCondition(inventory.horseCondition)}</dd>
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
          <div key={`${item.kind}-${item.horseCondition ?? "none"}`} className="compact-item">
            <strong>
              {formatItemKind(item.kind)} x {item.quantity}
            </strong>
            <p>
              {item.horseCondition === null ? "No condition" : `Condition: ${formatHorseCondition(item.horseCondition)}`}
            </p>
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
