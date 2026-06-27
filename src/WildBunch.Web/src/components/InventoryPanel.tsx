import styled from "styled-components";
import type { InventoryCapabilitiesDto, InventoryDto, InventoryItemDto } from "../api/types";
import { formatCanteenState, formatCapabilityLabel, formatHorseTravelState, formatItemKind } from "../ui/formatters";
import {
  StatusCard,
  StatList,
  Stack,
  ItemCard,
} from "./ui/sharedStyled";

const TagRow = styled.div`
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-top: 10px;
`;

const Tag = styled.span`
  padding: 5px 9px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid var(--border);
  font-size: 0.76rem;
  font-weight: 600;
  color: var(--muted);
`;

const ItemList = styled(Stack)`
  margin-top: 16px;
`;

const ItemDetailLine = styled.p`
  margin: 4px 0 0;
  font-size: 0.88rem;
  color: var(--muted);
`;

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
      <ItemList>
        {inventory.items.map((item: InventoryItemDto) => (
          <ItemCard key={`${item.kind}-${item.horseState?.hunger ?? "none"}-${item.canteenState?.charges ?? "none"}`}>
            <strong>
              {formatItemKind(item.kind)} x {item.quantity}
            </strong>
            <ItemDetailLine>
              {[
                item.horseState ? `Horse: ${formatHorseTravelState(item.horseState)}` : null,
                item.canteenState ? `Canteen: ${formatCanteenState(item.canteenState)}` : null,
              ]
                .filter(Boolean)
                .join(" | ") || "No travel state"}
            </ItemDetailLine>
          </ItemCard>
        ))}
      </ItemList>
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
