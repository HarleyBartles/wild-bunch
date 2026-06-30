import styled from "styled-components";
import type { InventoryDto, InventoryItemDto } from "../api/types";
import { formatCanteenState, formatHorseTravelState, formatItemKind } from "../ui/formatters";
import {
  StatusCard,
  StatList,
  Stack,
  ItemCard,
} from "./ui/sharedStyled";

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
      </StatList>
      <ItemList>
        {inventory.items.map((item: InventoryItemDto) => (
          <ItemCard key={`${item.kind}-${item.horseState?.hunger ?? "none"}-${item.canteenState?.charges ?? "none"}`}>
            <strong>
              {formatItemKind(item.kind)} x {item.quantity}
            </strong>
            {item.horseState || item.canteenState ? (
              <ItemDetailLine>
                {[
                  item.horseState ? `Horse: ${formatHorseTravelState(item.horseState)}` : null,
                  item.canteenState ? `Canteen: ${formatCanteenState(item.canteenState)}` : null,
                ]
                  .filter(Boolean)
                  .join(" | ")}
              </ItemDetailLine>
            ) : null}
          </ItemCard>
        ))}
      </ItemList>
    </StatusCard>
  );
}
