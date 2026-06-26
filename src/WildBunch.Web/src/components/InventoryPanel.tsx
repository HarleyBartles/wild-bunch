import type { InventoryCapabilitiesDto, InventoryDto, InventoryItemDto } from "../api/types";
import { formatCanteenState, formatCapabilityLabel, formatHorseTravelState, formatItemKind } from "../ui/formatters";
import {
  StatusCard,
  StatList,
  Stack,
  ItemCard,
  TagRow,
  Tag,
} from "./ui/sharedStyled";

interface InventoryPanelProps {
  inventory: InventoryDto;
}

export function InventoryPanel({ inventory }: InventoryPanelProps) {
  return (
    <StatusCard>
      <h3>Inventory</h3>
      <StatList>
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
      </StatList>
      <Stack style={{ marginTop: "16px" }}>
        {inventory.items.map((item: InventoryItemDto) => (
          <ItemCard key={`${item.kind}-${item.horseState?.hunger ?? "none"}-${item.canteenState?.charges ?? "none"}`}>
            <strong>
              {formatItemKind(item.kind)} x {item.quantity}
            </strong>
            <p style={{ margin: "4px 0 0", fontSize: "0.88rem", color: "var(--muted)" }}>
              {[
                item.horseState ? `Horse: ${formatHorseTravelState(item.horseState)}` : null,
                item.canteenState ? `Canteen: ${formatCanteenState(item.canteenState)}` : null,
              ]
                .filter(Boolean)
                .join(" | ") || "No travel state"}
            </p>
          </ItemCard>
        ))}
      </Stack>
      <TagRow>
        {Object.entries(inventory.capabilities).map(([key, enabled]) => (
          <Tag key={key}>
            {formatCapabilityLabel(key as keyof InventoryCapabilitiesDto)}: {enabled ? "Yes" : "No"}
          </Tag>
        ))}
      </TagRow>
    </StatusCard>
  );
}
